# zserver Battle 服网络同步说明

时间：2026-05-22

## 目录
- [[#Q1: Battle 服网络同步整体链路是什么]]
- [[#Q2: 增量日志与快照是怎么配合完成同步的]]

---

## Q1: Battle 服网络同步整体链路是什么

## 结论

该项目 Battle 服的网络同步不是典型“客户端互发状态”或“锁步输入广播”，而是服务端权威的战斗帧循环：BattleRoom 每帧在服务端推进战斗逻辑，收集本帧产生的 BattleLogData，再通过 Agent 隧道把战报增量推给在线玩家。客户端输入，如释放技能、切目标、拾取物，会先走 Game/Team/CommonInstance 等服务校验与路由，最终以 gRPC 投递到 Battle 服的房间消息队列，由房间 goroutine 在帧循环中消费。

## 网络同步主链路

1. 客户端发请求到 Game 服。以释放技能为例，`handleReleaseBattleSkill` 先加载玩家、判断玩家当前在线场景；主线在线走 `RpcTeamReleaseBattleSkill`，副本在线走 `RpcCommonInstanceReleaseBattleSkill`。切目标、拾取也是类似模式。

2. Team 或副本队伍服务再把操作转成 Battle RPC。Battle 服的 `grpcBattleTeamService.ReleaseSkill`、`BattleTarget`、`BattlePickUp` 会找到房间，然后调用 `br.PacketAdd(...)`，把操作放进房间的 `packetArr` 队列。

3. 每个 BattleRoom 启动自己的循环 goroutine。循环每帧执行 `heartCheck()`、`handleAllRpcEvents()`、`updateAndBroadcastFrame()`，其中 `handleAllRpcEvents` 会一次性取出当前队列长度的 RPC 事件并执行，保证外部输入在房间循环线程内落地。

4. 战斗逻辑由 `BattleMgr.update()` 推进。它会递增 `frameNum`，根据状态执行 `updateStateBattle` 或 `updateStateMap`，更新 AI、事件、统计、技能和各种玩法状态。

5. 本帧同步数据不是直接发完整对象，而是先沉淀为 BattleMgrLog。技能、移动、伤害、死亡、复活、特效、UI 状态、玩法进度等逻辑在执行时调用大量 `addLog*` 方法，写入 `BattleLogData`。每条日志本质是一个 `[]int32`，首位是 `BattleLogAction`，后面是该 action 的参数。

6. 帧尾调用 `battleMgrNet.getOutBattleLog(mgr)` 取出本帧日志。若只是普通增量，返回 `OutGetBattleLogSimple`，包含 logs、frameNum、sceneMode；若需要补充快照，则返回 `OutGetBattleLog`，其中携带 `BattleSceneData`，包括玩家、怪物、地图块、进度、统计、FrameRate、RoomID 等。

7. BattleRoom 对每个在线真人玩家过滤日志并推送。`filterLogs` 会按 `sendRoleArr` 做私有日志过滤，例如只把某些宠物或神明能量相关日志发给所属玩家。最终 `sendDataWithPlayer` 根据玩家保存的 `netAgentID/netConnID` 找到 AgentSession，设置 connID 后 `agentSess.Write(pkt)` 推给具体客户端连接。

## 同步协议形态

客户端看到的战斗同步主要是 MID 10 Battle：

- `GET_BATTLE_LOG_SIMPLE`，AID 1：普通每帧增量战报，字段是 `logs/frameNum/sceneMode`。
- `GET_BATTLE_LOG`，AID 5：增量日志加场景快照，字段是 `logs/scene`。
- `GET_BATTLE_DATA`，AID 4：主动拉取当前战斗数据，用于进入战斗、重连或补状态。
- 输入类请求包括 `RELEASE_BATTLE_SKILL`、`CHANGE_BATTLE_TARGET`、`BATTLE_PICK_UP`、`BATTLE_USE_PICK_UP` 等，输入一般不会直接改变客户端状态，而是进入服务端房间逻辑，后续通过战报体现结果。

## 帧循环与一致性

BattleRoom 的同步粒度由 `FrameTick` 控制。每帧先处理 RPC 输入，再推进战斗，再广播战报。这个顺序很关键：外部输入不会在任意 goroutine 里直接改战斗对象，而是入队后由房间循环统一处理，减少并发写战斗状态的风险。

`BattleMgr.frameNum` 是逻辑帧号，每次 `BattleMgr.update()` 调 `addFrameNum()` 自增。同步包里也会带 `FrameNum` 和 `FrameRate`，客户端可以按帧号消费战报、对齐表现。

## Q2: 增量日志与快照是怎么配合完成同步的

BattleMgrLog 是同步的核心缓冲区。每帧逻辑产生的战报先放在 `datasArr`，`getLogDatas` 被取走时会 `defer clearLogDatas()` 清空，因此默认是“本帧增量”。当某些动作需要客户端刷新更大范围状态时，会打开 `isShowSceneData/isShowPlayerData/isShowMonsterData/isShowStatistic/showRoleID` 等标记，帧尾就会附带对应的 `BattleSceneData` 快照。

这套设计的好处是普通帧轻量，只发动作日志；异常、重连、换图、角色变化、统计更新等场景再夹带快照修正客户端状态。

### Q2.1 核心数据结构

增量日志的最小单位是 `BattleLogData`：

```go
type BattleLogData struct {
    arr         []int32
    sendRoleArr []*BattleRole
}
```

`arr[0]` 是 `BattleLogAction`，后续位置是该 action 的参数。例如移动、施法、伤害、死亡、复活、特效、UI 能量、玩法进度，都不是以“大对象状态”发给客户端，而是编码成一条条 `int32` 日志。客户端按 action id 解释参数并播放表现。

`sendRoleArr` 是单条日志的可见性过滤：为 `nil` 表示广播给所有可接收玩家；不为 `nil` 时，`filterLogs` 只把这条日志发给指定角色或其宠物/神明所属玩家。这样一些个人 UI、能量、私有表现不需要所有人都收到。

快照的载体是 `BattleSceneData`，协议字段包括：

| 字段 | 作用 |
|---|---|
| `players` | 房间玩家基础/养成/外观/技能信息 |
| `playerDatas` | 场景里的玩家战斗实体状态 |
| `monsterDatas` | 场景里的怪物/召唤物状态 |
| `frameNum/frameRate` | 当前服务端逻辑帧和帧率 |
| `blocks` | 地图块坐标、旋转、路线、物件组 |
| `progress/eventNow/saves/process` | 玩法进度、事件、存档点、步骤队列 |
| `teamState/battleTime/statistic` | 队伍状态、战斗时长、统计数据 |
| `mapID/roomID/param` | 地图、房间和玩法额外参数 |

### Q2.2 普通帧为什么只发增量日志

每帧结束时，`battleMgrNet.getOutBattleLog(mgr)` 调 `mgr.log.getLogDatas(mgr)` 取出本帧日志和若干快照开关：

```go
logs, isShowPlayerData, isShowMonsterData,
isShowSceneData, showRoleID, isShowStatistic := mgr.log.getLogDatas(mgr)
```

随后判断是否是简单日志：

```go
isSimpleLog := !isShowPlayerData &&
    !isShowMonsterData &&
    !isShowSceneData &&
    showRoleID == 0 &&
    !isShowStatistic
```

如果是简单日志，就返回 `scenePb == nil`。广播层会打 `OutGetBattleLogSimple`，只包含：

- `logs`
- `frameNum`
- `sceneMode`

这就是常规战斗帧的主路径。它把网络包压得很小，因为大多数帧只需要告诉客户端“发生了哪些动作”，不需要重复同步全量角色、怪物、地图块。

### Q2.3 什么时候夹带快照

只要本帧日志打开了任一快照开关，`getOutBattleLog` 就会返回非空 `scenePb`，广播层改发 `OutGetBattleLog`，包里同时带 `logs` 和 `scene`。

常见触发方式：

| 开关 | 含义 | 典型触发 |
|---|---|---|
| `isShowSceneData` | 同步完整场景快照 | 场景刷新、重建、需要强校正的大变更 |
| `isShowPlayerData` | 同步所有玩家房间/场景数据 | 玩家队伍整体变化、较大范围角色刷新 |
| `isShowMonsterData` | 同步怪物数据 | 怪物复活、刷怪、怪物组状态需要补齐 |
| `showRoleID` | 只同步某个玩家数据 | 玩家加入/移除/进化/升级/宠物/坐骑/猫刷新 |
| `isShowStatistic` | 同步战斗统计 | 战斗结束、阶段统计展示 |

代码上能看到几个典型例子：

- `addLogBattleEnd` 会调用 `showStatistic()`，战斗结束帧附带统计。
- `addLogReborn(role, isAddSceneData)` 在 `isAddSceneData` 为 true 时调用 `setShowMonsterData()`，复活时可能带怪物快照。
- `addLogPlayerAdd/Remove/Evolve/Lv` 会设置 `showRoleID = role.id`，让本帧只补这个玩家的快照。
- `addlogPetRefresh/addlogMountRefresh/addlogCatRefresh` 也会设置 `showRoleID`，用于刷新主战宠物、坐骑、猫猫模型相关数据。

### Q2.4 快照不是每次都全量

`BattleSceneData` 是一个大结构，但 `getOutBattleLog` 会按开关裁剪：

- `isShowSceneData == true`：调用 `getSceneDataPb(mgr, true)`，拿完整战斗快照。
- `isShowPlayerData == true`：只填 `Players` 和 `PlayerDatas`。
- `isShowMonsterData == true`：只填 `MonsterDatas`。
- `showRoleID > 0`：先构造场景数据，再只截取对应角色的 `Players` 和 `PlayerDatas`。
- `isShowStatistic == true`：只填 `Statistic`。

最后无论是不是完整快照，都会补上 `TeamState`、`FrameNum`、`FrameRate`、`SceneMode`、`RoomID` 等基础对齐信息。也就是说，“快照”在这里并不总是全量状态，而是按本帧需要裁剪后的状态补丁。

### Q2.5 增量日志的生命周期

一帧里的日志生命周期大致是：

1. 战斗逻辑执行过程中调用 `addLog*`，写入 `BattleMgrLog.datasArr`。
2. 帧尾 `broadcastFrameSendPlayer` 调 `battleMgrNet.getOutBattleLog(mgr)`。
3. `getLogDatas` 返回 `datasArr` 和快照开关，并 `defer clearLogDatas()`。
4. 广播层按玩家调用 `filterLogs`，过滤私有日志。
5. 根据是否有 `scenePb` 选择 `OutGetBattleLogSimple` 或 `OutGetBattleLog`。
6. 通过 `sendDataWithPlayer` 写回 AgentSession。
7. 本帧日志缓冲清空，下一帧重新积累。

这里的关键点是：`BattleMgrLog` 不是长期存储，而是实时房间每帧的临时同步缓冲。真正需要回放/保存的玩法会另走 `SaveBattleLogData`。

### Q2.6 回放/预生成战报里的日志保存

MonsterArena、MonsterPower、Replay 不是完全同一条实时广播路径。

MonsterArena 会在 `broadcastFrameSendPlayerArena` 中先调用 `getOutBattleLog`，再 `addLogLogData` 保存到 `br.logsData`。MonsterPower PVE/PVP/WorldBoss 可以先跑快速生成，把每帧日志和部分 `ScenePB` 存进 `SaveBattleLogData`，之后播放时通过 `getOutLogsData(curFrameNum)` 读出对应帧。

保存时会复制 `[]int32`，避免后续清理或复用影响已保存战报。保存的 `ScenePB` 目前重点保留 `MonsterDatas`，并把 `SceneMode` 设置为 Replay，说明这类快照主要服务回放播放，而不是实时房间完整状态恢复。

### Q2.7 客户端视角

客户端可以把同步包理解为两类：

```text
OutGetBattleLogSimple:
  frameNum + sceneMode + logs
  适合普通连续播放

OutGetBattleLog:
  scene + logs
  适合播放本帧动作，同时用 scene 修正/补齐状态
```

因此客户端不应该假设每一帧都有完整场景，也不应该只依赖快照驱动表现。正常情况下，它应该持续消费 `logs`；当包里带 `scene` 时，再用快照更新角色、怪物、地图块、进度或统计等状态。

### Q2.8 和 battle_server Q1/Q2 的关系

`battle_server.md` 的 Q1 讲 `entryTypeSummon`，其最后一步会“记录战斗日志”。这类召唤不是直接把新召唤物完整状态每帧广播，而是通过 `addLogActionSummon` 等日志通知客户端表现；必要时再附带怪物/角色快照补齐实体状态。

`battle_server.md` 的 Q2 讲 `entryTypeRecoverEnergyVal`，它的 1104 复苏能量战报最终会走 `addLogSkillEnergyClientSync` 一类日志。这正是“增量日志”的典型用途：服务端只同步某个 UI 能量值变化，不需要整帧全量快照。

所以 Q1/Q2 都可以作为理解本文件 Q2 的例子：Battle Entry 模块负责产生战斗语义，BattleMgrLog 负责把这些语义编码成客户端可播放/可修正的网络同步日志。

## 特殊模式

普通主线、副本等房间直接边算边播。MonsterPower、MonsterArena、Replay 有特殊路径：

- MonsterPower PVE/PVP/WorldBoss 可以先快速生成战报，保存到 `logsData`，再按帧播放给观战或在线玩家。
- MonsterArena 会把战报落到日志数据里，快速战斗时可不实时广播。
- Replay 模式读取已保存的战报帧并播放。
- QuickMode 下普通广播会跳过，避免快速结算时还把每帧表现发给客户端。

## 入口文件索引

- `/Users/gexianglin/zserver/internal/battle/battle_room_loop.go`：房间帧循环、广播、Agent 推送。
- `/Users/gexianglin/zserver/internal/battle/battle_room_packet.go`：外部 RPC 事件入队与房间内消费。
- `/Users/gexianglin/zserver/internal/battle/battle_mgr.go`：逻辑帧推进与战斗状态更新。
- `/Users/gexianglin/zserver/internal/battle/battle_mgr_log.go`：战报日志结构与每帧清理。
- `/Users/gexianglin/zserver/internal/battle/battle_mgr_net.go`：战斗快照和同步 pb 组装。
- `/Users/gexianglin/zserver/internal/service/game/battle.go`：客户端战斗输入在 Game 服的校验与路由。
- `/Users/gexianglin/zserver/internal/service/battle/grpc_battle_team.go`：Battle 服 gRPC 入口。
- `/Users/gexianglin/zserver/proto/pb/game/battle.proto.txt`：客户端 Battle MID 10 协议。

## 一句话模型

Battle 服做的是“服务端模拟，客户端播放”：客户端只提交操作意图，BattleRoom 在服务端权威帧里消费输入、推进战斗、生成战报，客户端按 `frameNum` 播放 `BattleLogAction` 与必要快照。
