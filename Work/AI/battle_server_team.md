## 目录

- [[#Q1: RoomKindReplay 回放模式下是怎么把战报发给客户端的]]
- [[#Q2: MonsterArenaBattleLogData 里每帧的战报是怎么存的]]
- [[#Q3: ModeKindMonsterArena = 13 怪物竞技场是怎么回放服务器战报的]]
- [[#Q4: ModeKindMonsterArena = 13 客户端怎么回放战报]]
- [[#Q5: 怪物擂台赛程为何严格串行 — AddNewBattle/AddMonster/SendRpc 循环细节]]
- [[#Q6: PreprocessLogData 怎么确保服务器战报按顺序一帧帧播放]]
- [[#Q7: SyncData 里 BattleData.SetFrameTime 为什么每包都改 frameTime]]

---

## Q1: RoomKindReplay 回放模式下是怎么把战报发给客户端的

### 整体流程

`RoomKindReplay`(值 14, `internal/battle/battle_room.go:123`)是一种观战回放模式：战报不是实时算出来的，而是**从预先存好的战报数据里按帧读出来重新广播**给观战玩家。

### 1. 战报数据加载(创建房间时)

`newBattleRoom`(`internal/battle/battle_room.go:188`)在 Kind 为 `RoomKindReplay` 时：

- 把 `brpb.ReplayInfo.RoleData` 包装进 `br.replayPlayers`(观战玩家集合,和真正参战的 `br.players` 区分,`battle_room.go:219-231`)。
- 通过 `data.GetMonsterArenaBattleLogData(activityID, battleLogID)` 拉取已落地的战报,填充 `br.logsData`(`*SaveBattleLogData`),包含:
  - `Logs map[uint32][][]int32` —— 帧号 → 多条 log 的 int32 数组
  - `ScenePB map[uint32]*pb.BattleSceneData` —— 帧号 → 场景增量数据
  - `WinGroupId / GroupIds / LastFrame`
- 代码位置:`battle_room.go:233-247`。

### 2. 帧循环驱动

`br.startLoop()`(`battle_room_loop.go:223`)启动 goroutine,每帧调用 `updateAndBroadcastFrame()`(`battle_room_loop.go:19`):

```
mgr.update()        // 推进帧号
br.broadcastFrame() // 按 Kind 分发广播
```

### 3. 广播分发

`broadcastFrame`(`battle_room_loop.go:65`)按 Kind 分支:

```go
case RoomKindReplay:
    if br.isPlayLog {
        br.broadcastFrameSendPlayerReplay()
    }
```

关键开关 `isPlayLog`:回放房间在初始化后由外部逻辑置 true 才会真正开始播放(参考 `battle_mgr.go:340` 与 `battle_mgr_raid.go:491`)。回放结束在 `checkReplayBattleEnd`(`battle_mgr_raid_arena1.go:118-130`) 把它重置为 false。

### 4. `broadcastFrameSendPlayerReplay` 核心逻辑

位置:`battle_room_loop.go:126-150`。

1. `scenePb, logDataArr := br.getOutLogsData(br.mgr.frameNum)` —— 从 `br.logsData` 里把当前帧的数据复制一份出来(`battle_room.go:674`)。
   - `scenePb = br.logsData.ScenePB[curFrameNum]`
   - `logArr = br.logsData.Logs[curFrameNum]`,再 copy 成 `[]*BattleLogData`,`sendRoleArr = nil`(全员可收)。
2. 遍历 `br.GetReplayPlayers()` —— 只发给观战玩家,不发给原战斗的 `br.players`。
3. `logs := br.filterLogs(logDataArr, player)` —— 按 `sendRoleArr` 过滤(此处 nil 表示都发);源码见 `battle_room_loop.go:38`。
4. 组包:
   - `scenePb == nil`:`pb.OutGetBattleLogSimple{Logs, FrameNum, SceneMode}`,协议 `BattleAID_GET_BATTLE_LOG_SIMPLE`。
   - `scenePb != nil`:`pb.OutGetBattleLog{Logs, Scene}`,协议 `BattleAID_GET_BATTLE_LOG`(带场景数据,通常是有怪物状态变化的帧)。
5. 通过 `br.sendDataWithPlayer(roleID, netAgentID, netConnID, pkt)` 单播。

### 5. 实际发送

`sendDataWithPlayer`(`battle_room_loop.go:210`):

```go
agentSess := session.GetAgentSession(agentID)
pkt.SetConnID(connID)
agentSess.Write(pkt)
```

即通过 agent 会话写入,带上玩家所在连接的 connID。

### 6. 结束广播

`checkReplayBattleEnd`(`battle_mgr_raid_arena1.go:118`):
- 当 `mgr.frameNum > mgr.br.logsData.LastFrame`,置 `br.isPlayLog = false`,`stopLoop(true)`(`stopLoop` 在停广播前还会再做一次 `broadcastFrame` 把末尾包补发,`battle_room_loop.go:284`),最后 `rpcBattleEnd` 通知 game 服。

### 关键差异对比

| 模式                          | 数据来源                     | 广播对象                   | 广播函数                             |
| --------------------------- | ------------------------ | ---------------------- | -------------------------------- |
| 普通战斗                        | 实时计算 `getOutBattleLog`   | `br.players`(参战玩家)     | `broadcastFrameSendPlayer`       |
| 怪物擂台 `RoomKindMonsterArena` | 实时计算 + 同时存 `logsData`    | `br.replayPlayers`(观战) | `broadcastFrameSendPlayerArena`  |
| 回放 `RoomKindReplay`         | **从 `br.logsData` 按帧读取** | `br.replayPlayers`(观战) | `broadcastFrameSendPlayerReplay` |

### 一句话总结

回放模式下房间创建时从存档(`MonsterArenaBattleLogData`)拉到完整 `logsData`,startLoop 后每帧按 `frameNum` 从 `logsData.Logs / ScenePB` 里读对应帧战报,过滤后用 `OutGetBattleLog(Simple)` 协议单播给 `replayPlayers`,跑到 `LastFrame` 后停广播并 `rpcBattleEnd`。

---

## Q2: MonsterArenaBattleLogData 里每帧的战报是怎么存的

### 1. 落地的数据结构

`internal/data/monster_arena.go:487` `MonsterArenaBattleLog`：

```go
type MonsterArenaBattleLog struct {
    InstanceID  uint32
    MonsterInfo []*MosterArenaMonsterBattleInfo // [左/右两组怪]
    // 战报
    WinGroupId uint32
    LastFrame  uint32                            // 战报总帧数
    GroupIds   []uint32
    ScenePB    map[uint32]*pb.BattleSceneData    // 帧号 → 当帧场景增量
    Logs       map[uint32][][]int32              // 帧号 → 当帧 N 条 log，每条是一个 int32 数组
}
```

要点：**以帧号为 key**，一帧可以有多条 log；每条 log 是紧凑的 `[]int32`，第 0 位是 action 类型，后面是 payload，客户端按相同协议解码。

### 2. 单条 log 的二进制布局

`internal/battle/battle_mgr_log.go:283`：

```go
type BattleLogData struct {
    arr         []int32         // 头部固定 + payload
    sendRoleArr []*BattleRole   // nil = 广播；非 nil = 只发指定玩家(序列化时不带，仅服务器内过滤用)
}
```

- 头部长度 `sizeOfBattleLogHead = 1`(`battle_mgr_log.go:13`)，`arr[logIndexAction=0]` 写入 `BattleLogAction`(`battle_mgr_log.go:38` 起，0=BattleStart、3=Attack、6=Skill、7=Value(伤害)、10=Die、12=Ballistic …)。
- 后续每个 action 自己定 payload 格式，例如 `addLogBattleStart` 在 `arr[1]=Kind`，`arr[2]=monsterGroupID`，`arr[3..]=roleKey`(`battle_mgr_log.go:386`)。
- 浮点字段统一通过 `getNetSend32/64`(`battle_mgr_net.go:36`) 乘 `constNetAccufacy` 量化成 int。

### 3. 一帧 log 的产生（运行时缓冲区）

`BattleMgrLog`(`battle_mgr_log.go:289`)持有 `datasArr []*BattleLogData`，战斗逻辑里所有 `addLogXxx`(吟唱/攻击/移动/技能/伤害/死亡/弹道/复活/特效/相机/事件…) 都走 `newLogData(action, size, roleArr)` + `appendLog(ld)` 写进当前帧缓冲。

每帧结算时 `battleMgrNet.getOutBattleLog(mgr)`(`battle_mgr_net.go:51`)：

1. `logs, isShowPlayerData, isShowMonsterData, isShowSceneData, showRoleID, isShowStatistic = mgr.log.getLogDatas()` —— 把缓冲拿走并 `clearLogDatas()`(`battle_mgr_log.go:335`)，相当于一帧的快照。
2. 如果几个开关都没置(`isSimpleLog`)，scenePb 直接返回 nil，**只发 logs**。
3. 否则按需要组 `BattleSceneData`(场景/玩家/怪/统计)。

### 4. 写入 logsData（MonsterArena 实时跑战时）

`broadcastFrameSendPlayerArena`(`battle_room_loop.go:153`)：

```go
scenePb, logDataArr := battleMgrNet.getOutBattleLog(br.mgr)
br.addLogLogData(br.mgr.frameNum, scenePb, logDataArr)  // 先存进 br.logsData
// 再广播给观战者 br.replayPlayers
```

`addLogLogData`(`battle_room.go:705`)：

- 懒初始化 `br.logsData = &SaveBattleLogData{ScenePB: map, Logs: map}`。
- 遍历 `logDataArr`：每条 `logData.arr` **复制一份**(避免后续被覆盖)，`append` 到 `br.logsData.Logs[curFrameNum]`(`[][]int32`)。注意 `sendRoleArr` 不入库——回放阶段一律走广播，过滤行为不保留。
- 若 `scenePb != nil && len(scenePb.MonsterDatas) > 0`：**只挑 MonsterDatas** 深拷贝成新的 `BattleSceneData{SceneMode: RoomKindReplay, FrameNum, MonsterDatas}`，存到 `br.logsData.ScenePB[curFrameNum]`（其它字段如 Players、Statistic 不存——回放不需要）。

PVE 版本 `addLogLogDataPVE`(`battle_room.go:742`)逻辑相同，多累计 `LogsNum`。

工具批量生成(`newBattleRoomArena`, `battle_room.go:346`)在 `isQuickCreateLog` 模式下让循环空跑 `update + broadcastFrame`，直到战斗自然结束，`br.logsData` 就被一帧一帧填满。

### 5. 转 RPC proto 回传

`newBattleRoomArena` 末尾(`battle_room.go:387`)把 `br.logsData` 装成 `pb.RPCBattleLogInfo`：

```
WinGroupId / GroupIds / LastFrame / ScenePB  ← 直接挂
Logs[frame] = &RPCBattleLogItem{
    D: []*RPCBattleLogArray{ {D: []int32{...}}, ... }   // 每条 log 一个 RPCBattleLogArray
}
```

proto 定义(`proto/pb/rpc/rpc_battle_team.proto:148`)：

```proto
message RPCBattleLogArray { repeated int32 d = 1; }
message RPCBattleLogItem  { repeated RPCBattleLogArray d = 1; }   // 一帧 N 条
message RPCBattleLogInfo  {
    uint32 winGroupId = 1;
    uint32 lastFrame  = 2;
    repeated uint32 groupIds = 3;
    map<uint32, BattleSceneData> scenePB = 4;     // 帧号 → 场景
    map<uint32, RPCBattleLogItem> logs   = 5;     // 帧号 → 多条 log
}
```

### 6. 回灌到 data 层

`MonsterArenaSendBattleRpc`(`internal/db/service/monster_arena.go:267`) 通过 gRPC `RpcBattleCreateArenaRoom` 让 battle 服跑出战报，拿到 `resp.LogInfo` 后调 `battleInfo.SetLogInfo(resp.LogInfo)`(`internal/data/monster_arena.go:527`):

```go
m.WinGroupId = logInfo.WinGroupId
m.LastFrame  = logInfo.LastFrame
m.GroupIds   = logInfo.GroupIds
m.ScenePB    = logInfo.ScenePB
m.Logs = make(map[uint32][][]int32, len(logInfo.Logs))
for k, v := range logInfo.Logs {
    arr := make([][]int32, 0, len(v.D))
    for _, vv := range v.D { arr = append(arr, vv.D) }   // 把 RPCBattleLogItem.D 平铺回 [][]int32
    m.Logs[k] = arr
}
```

这样运行时形态就和回放需要的 `SaveBattleLogData.Logs` 一致了。

### 7. 持久化与重载

- `loadMonsterArenaActivityBattleLogData`(`internal/data/monster_arena.go:440`)从 `etc/game/data/.../arena_battle_log/activity_*.json`(如 `activity_4_tw.json`)反序列化整个 `MonsterArenaActivityBattleLog`，挂到全局 `atomic.Value`。
- 在线服务 `GetMonsterArenaBattleLogData(activityID, battleID)` 直接取 `battleLog.BattleLog[battleID]`(`monster_arena.go:474`)。
- 回放房创建时 (`battle_room.go:234`) 把它读出来塞进 `br.logsData`，配合 Q1 的 `broadcastFrameSendPlayerReplay` 按 `frameNum` 单帧广播。

### 一句话总结

`MonsterArenaBattleLogData` 以**帧号为 key 的两张 map** 存战报：`Logs map[frame][][]int32` 一帧 N 条紧凑事件数组(action+payload)、`ScenePB map[frame]*BattleSceneData` 一帧场景增量(回放阶段只保留 MonsterDatas)。源头是战斗 `BattleMgrLog.datasArr` 缓冲 + 每帧 `getOutBattleLog` 取走、`addLogLogData` 深拷贝写入；通过 `RPCBattleLogInfo` 在服务间传递，最终落到 JSON 活动配置由 data 层热加载，回放时按帧号取出来重发。

## Q3: ModeKindMonsterArena = 13 怪物竞技场是怎么回放服务器战报的

### 结论

`ModeKindMonsterArena = 13` 在服务端对应 `data.InstanceTypeMonsterArenaTeam` / `battle.RoomKindMonsterArena`，它本身是“怪物竞技场战斗/生成战报”的房间类型，不是最终播放历史战报的房间类型。

真正给玩家回看已生成服务器战报时，Game 服创建的是 `InstanceTypeReplayTeam` / `RoomKindReplay = 14` 回放房间：`ReplayInfo` 只带 `activityID + battleLogID`，Battle 服从已加载的 `arena_battle_log` 数据里取 `Logs / ScenePB / LastFrame`，然后按帧广播给观战玩家。

### 1. 服务器战报是怎么生成出来的

GM/工具入口调用 Game 服 `GamePlayerMonsterArenaGenerateAllBattleLog`，最终进入 `internal/db/service/monster_arena.go` 的 `MonsterArenaGenerateAllBattleLog`。

生成每一场时会走：

```text
MonsterArenaGenerateAllBattleLog
  -> MonsterArenaSendBattleRpc
  -> RpcBattleCreateArenaRoom
  -> Battle.CreateArenaRoom
  -> newBattleRoomArena
```

关键点：`MonsterArenaSendBattleRpc` 给 Battle 服创建房间时传的是 `Kind: data.InstanceTypeMonsterArenaTeam`，也就是 13，并把两只怪物和词条放在 `ReplayInfo.MonsterGroups` 里。

Battle 服的 `newBattleRoomArena` 会开启一个临时房间，`isQuickCreateLog = true`，在循环里不断 `mgr.update()` + `broadcastFrame()`。对于 `RoomKindMonsterArena`，`broadcastFrame()` 进入 `broadcastFrameSendPlayerArena()`：这里一边从实时战斗取 `battleMgrNet.getOutBattleLog`，一边调用 `br.addLogLogData(...)` 把每帧的 `Logs` 和 `ScenePB` 存进 `br.logsData`。

战斗结束后，`newBattleRoomArena` 把 `br.logsData` 组成 `RPCBattleLogInfo` 返回给 Game 服；Game 服通过 `battleInfo.SetLogInfo(resp.LogInfo)` 写回 `MonsterArenaActivityBattleLog`。最终 `handleGrpcGamePlayerMonsterArenaGenerateAllBattleLog` 把整份 `battleLog` marshal 成 JSON 返回给 GM/工具。

### 2. 运行时战报从哪里加载

Battle/Game 运行时不是现算每次回放，而是从 data 层加载好的 `arena_battle_log` JSON 里读。

加载入口在 `internal/data/monster_arena.go`：

- `loadMonsterArenaActivityBattleLogData(env)` 扫描 `arena_battle_log` 目录。
- 每个文件加载成 `MonsterArenaActivityBattleLog`。
- 查询时走 `GetMonsterArenaBattleLogData(activityID, battleID)`。

每场 `MonsterArenaBattleLog` 里保存：

- `InstanceID`：地图/副本 ID。
- `MonsterInfo`：两只怪物和词条。
- `WinGroupId / GroupIds / LastFrame`：胜者和帧范围。
- `ScenePB`：帧号到场景数据。
- `Logs`：帧号到战斗 log 数组。

### 3. 玩家点击回看时 Game 服做什么

客户端发 `MONSTERARENA_PLAY_REPLAY`，对应 `actionMonsterArenaPlayReplay`。

Game 服会做这些校验：

1. 活动必须开启。
2. `data.GetMonsterArenaBattleLogData(activityID, battleID)` 必须能拿到战报。
3. 向 Center 服 `RpcMonsterArenaGetBattleInfo` 查询该场战场状态。
4. `battleInfo.WinMonsterID != 0`，即比赛已经结束，未结束不能回看。
5. 玩家不能正在加入其它副本，也不能已有副本队伍。

通过后，Game 调用：

```text
PlayerCreateMonsterArenaReplayBattleTeam
  -> PlayerCreateReplayBattleTeam
```

这里注意类型已经切到 `data.InstanceTypeReplayTeam = 14`，`ReplayInfo` 只传：

```text
ActivityID  = 当前活动期数
BattleLogID = 客户端请求的 battleID
RoleData    = Game 服补入的观战玩家网络信息
```

Game 服还会把玩家的 `ReplayMapInfo` 记为“正在回放”：保存回放 `InstanceType` 和 Battle 服 `SID`，然后推 `PushCommonInstanceTeamInfo` 告诉客户端进入回放战斗。

### 4. Battle 服怎么把历史战报按帧播出来

Battle 服收到普通 `CreateRoom` 后进入 `newBattleRoom`。

当 `Kind == RoomKindReplay` 时：

1. 把 `ReplayInfo.RoleData` 包成 `br.replayPlayers`，这是回放观众。
2. 用 `ReplayInfo.ActivityID` 和 `ReplayInfo.BattleLogID` 调 `data.GetMonsterArenaBattleLogData(...)`。
3. 把已保存的 `battleLog.Logs / ScenePB / WinGroupId / GroupIds / LastFrame` 填进 `br.logsData`。

房间循环开始后，`broadcastFrame()` 对 `RoomKindReplay` 分支调用 `broadcastFrameSendPlayerReplay()`。这个函数每帧做：

```text
br.getOutLogsData(frameNum)
  -> 从 br.logsData.ScenePB[frameNum] 和 br.logsData.Logs[frameNum] 取本帧数据
  -> filterLogs
  -> 有 ScenePB 发 OutGetBattleLog
  -> 无 ScenePB 发 OutGetBattleLogSimple
  -> sendDataWithPlayer 单播给 replayPlayers
```

所以客户端看到的不是重新计算出来的新战斗，而是服务器保存过的帧日志按 `frameNum` 重放。

### 5. 回放怎么结束

`checkReplayBattleEnd()` 判断：

```text
mgr.frameNum > br.logsData.LastFrame
```

达到最后一帧后：

1. `br.isPlayLog = false`。
2. `addLogRoomDelete()` 通知客户端删房间。
3. `stopLoop(true)` 停止 Battle 房间循环。
4. `rpcBattleEnd()` 回调 Game 服 `GamePlayerReplayTeamEnd`。
5. Game 服清理玩家 `ReplayMapInfo`，并推 `PushReplayTeamEnd`。

### 6. 13 和 14 的区别

| 类型 | 服务端常量 | 作用 |
| --- | --- | --- |
| 13 | `InstanceTypeMonsterArenaTeam` / `RoomKindMonsterArena` | 怪物竞技场战斗本体；用于实时战斗、生成战报，也会在 `broadcastFrameSendPlayerArena` 中边播边存 `logsData`。 |
| 14 | `InstanceTypeReplayTeam` / `RoomKindReplay` | 纯回放房间；从已保存的 `MonsterArenaBattleLog` 读取 `logsData`，按帧发给客户端。 |

### 关键代码位置

- `internal/data/instance_team.go`：`InstanceTypeMonsterArenaTeam = 13`，`InstanceTypeReplayTeam = 14`。
- `internal/battle/battle_room.go`：`RoomKindMonsterArena = 13`，`RoomKindReplay = 14`；`newBattleRoom` 在回放房间里加载 `br.logsData`。
- `internal/db/service/monster_arena.go`：`MonsterArenaSendBattleRpc` 用 13 创建生成战报房间，并接收 `RPCBattleLogInfo`。
- `internal/battle/battle_room.go`：`newBattleRoomArena` 快速跑完战斗并返回完整战报。
- `internal/service/game/monster_arena.go`：`handleMonsterArenaPlayReplay` 处理客户端回看请求。
- `internal/db/service/replay.go`：`PlayerCreateMonsterArenaReplayBattleTeam` 把怪物竞技场战报转成 14 号回放房间。
- `internal/battle/battle_room_loop.go`：`broadcastFrameSendPlayerReplay` 按帧读取历史战报并下发。
- `internal/battle/battle_mgr_raid_arena1.go`：`checkReplayBattleEnd` 检查 `LastFrame` 并结束回放。

---

## Q3: MonsterArenaSendBattleRpc 是干嘛

### 一句话定位

`internal/db/service/monster_arena.go:267` 的 `MonsterArenaSendBattleRpc`，是**怪物擂台("Monster Arena")活动的"单场战斗生成战报"的发起方**：它从服务发现挑一台 battle 服，把指定 `battleID` 的两组怪信息打成 RPC 发过去，让 battle 服离线跑完整场对战，**把生成出的战报回写到 `MonsterArenaActivityBattleLog`**。

### 源码

```go
func MonsterArenaSendBattleRpc(battleLogInfo *data.MonsterArenaActivityBattleLog, battleID uint32) error {
    battleSID, _ := discovery.PickRaidBattleRPCHost()      // 1. 选 battle 服
    if battleSID == 0 {
        return errors.New("BattleNoSID")
    }
    battleInfo := battleLogInfo.GetBattle(battleID)        // 2. 取这场比赛的左右两组怪
    resp, err := client.RpcBattleCreateArenaRoom(battleSID, &pb.RPCBattleRoom{
        Roomid: uint64(battleID),                          // 用 battleID 当房间 ID
        Mapid:  battleInfo.InstanceID,
        Kind:   data.InstanceTypeMonsterArenaTeam,         // 走擂台房逻辑
        ReplayInfo: &pb.RPCBattleReplayInfo{
            MonsterGroups: battleInfo.GetArenaMonsterInfoForBattle(), // [左怪+词条, 右怪+词条]
        },
    })
    if err != nil { return errors.WithMessage(err, "PlayerCreateMonsterArenaBattleTeam") }
    if resp.Result != pb.RespCode_Ok { return errors.Errorf(...) }
    battleInfo.SetLogInfo(resp.LogInfo)                    // 3. 把战报回灌进活动数据
    return nil
}
```

### 三步拆解

1. **挑战斗服** —— `discovery.PickRaidBattleRPCHost()`(`internal/discovery/service.go:558`)从 raidbattle 服务发现池里挑一个 SID。
2. **同步 RPC 让对端跑战斗** —— `client.RpcBattleCreateArenaRoom`(`internal/rpc/client/battle_team.go:78`)走 gRPC `BattleTeamService.CreateArenaRoom`，对端是 `internal/service/battle/grpc_battle_team.go:60` 的 `CreateArenaRoom`，它进 `battle.CreateArenaRoom`(`battle_room_mgr.go:118`) → `newBattleRoomArena`(`battle_room.go:346`)。后者在 `isQuickCreateLog=true` 的快速模式下空跑 `update + broadcastFrame`，**不广播给客户端**，只把每帧 `(scenePb, logDataArr)` 通过 `addLogLogData` 一帧一帧累积进 `br.logsData`，战斗自然结束后把 `logsData` 装成 `RPCBattleLogInfo` 返回（流程见 Q2 的 4~5 节）。
3. **回灌活动表** —— `battleInfo.SetLogInfo(resp.LogInfo)` (`internal/data/monster_arena.go:527`)把帧战报 (`map[frame]RPCBattleLogItem.D`) 平铺成 `[][]int32` 放进 `MonsterArenaBattleLog`，同时把 `WinGroupId / LastFrame / GroupIds / ScenePB` 也回填。**这一步很关键**——后续淘汰赛要靠它的 `WinGroupId` 决定胜者，并把胜者作为下一场的输入；回放房 (`Q1`) 也是从这里读出战报推给客户端的。

### 它在谁手里被调用

调用方都在 `MonsterArenaGenerateAllBattleLog`(同文件，`internal/db/service/monster_arena.go:12`)——这是**整套活动的赛程生成器**，按淘汰赛树一场一场调 `MonsterArenaSendBattleRpc`：

| 活动类型 | 场次 | 来源 |
|---------|------|------|
| Type1 单怪 16 强 | 1-4 (8 选 4) | 直接用 8 只怪两两配对，词条池 `Pool8ID` |
| | 5-6 (4 选 2) | 由 1+2、3+4 的胜者推上来，新词条池 `Pool4ID` |
| | 7 (决赛) | 5+6 胜者，词条池 `Pool2ID` |
| Type2 14 天 (含玩家组) | 11-14 | 8 玩家怪两两配对 |
| | 15-16 | 11+12、13+14 胜者 |
| | 17 | 15+16 胜者 |
| | 18 (总决赛) | 7 (怪冠军) vs 17 (玩家怪冠军)，**不再加词条** |

每生成一场新对阵 (`AddNewBattle` + `AddMonster`)，就立即 `MonsterArenaSendBattleRpc` 把它打到 battle 服跑出战报、回填胜者，**下一轮才知道谁能晋级**——所以是**严格串行**的(每场都要等上一场战报回来才能确定下一场出阵)。

最外层入口在 `internal/service/game/grpc_handle_game_common_instance.go:973` 的 `handleGrpcGamePlayerMonsterArenaGenerateAllBattleLog`，对应 GM/玩家发起"生成本期擂台所有战报"的 RPC `GamePlayerMonsterArenaGenerateAllBattleLog`。

### 和回放(Q1)、存储(Q2)的衔接

- 调用方向：game 服→(MonsterArenaSendBattleRpc)→battle 服(离线跑战斗)→(RPCBattleLogInfo)→game 服(SetLogInfo)→`MonsterArenaActivityBattleLog`→落 JSON 到 `arena_battle_log/activity_*.json`。
- 回放时：客户端请求观战 → 创建 `RoomKindReplay` 房 → `newBattleRoom` 调 `GetMonsterArenaBattleLogData(activityID, battleID)` 取出当年生成好的战报 → 按帧广播(Q1)。

### 一句话总结

`MonsterArenaSendBattleRpc` = "活动开赛前**离线烧战报**" 的胶水函数：选一台战斗服，把第 N 号擂台对阵的双方怪+词条丢过去，等它快速跑完整场对战，把战报回填进活动数据，供后续淘汰赛决胜者用、也供日后玩家回放用。

---

## Q4: ModeKindMonsterArena = 13 客户端怎么回放战报

> 本题对应 Unity Lua 客户端（`/Users/gexianglin/aaboli/main/Assets/Scripts/LuaScript/`）。服务器侧流程见 Q3。

### 结论

客户端不区分"实时打"和"回放"的协议入口——服务器侧不论怎么算出战报，最终都是通过 `OutGetBattleLog` / `OutGetBattleLogSimple` 两个协议下发。客户端拿到包后**先缓冲 → 按服务器帧号排序 → 映射成本地战斗时间 → 按 ActionKind 分发**。怪物竞技场和回放只是 `sceneMode` 字段不同（`ModeKindMonsterArena = 13` vs `ModeKindMonsterWar = 14`），分发表里有专属的 `800~809` 段 Action 走 `HandleLogBattleArena` 处理。

### 1. 两个客户端常量

`Assets/Scripts/LuaScript/Data/BattleData/_Battle_Kind.lua:18-19`：

```lua
ModeKindMonsterArena = 13, --怪物竞技场（实时打 / 生成战报阶段）
ModeKindMonsterWar  = 14, --重放（玩家点回看的观战房）
```

判别：`BattleSceneData.IsKindMonsterArenaOrReplay()`（`BattleSceneData.lua:1671`）—— 两个 mode 任一为真。

### 2. 进入回放房的路径

服务器侧 Game 服创建 `RoomKindReplay = 14` 房间后会推 `PushCommonInstanceTeamInfo`，客户端流程：

```
InstanceService.PushCommonInstanceTeamInfo                (InstanceService.lua:514)
  → InstanceManager.PushCommonInstanceTeamInfo            (InstanceManager.lua:835)
  → InstanceManager.SetInstanceTeamData                   (InstanceManager.lua:307)
  → InstanceManager.CallTypeManagerFunc(mapType,"OnReplayBegin", ...)
  → MonsterWarManager.OnReplayBegin(type)                 (MonsterWarManager.lua:447)
       └─ BattleService.GetBattleData(type)               // 主动拉初始场景
```

`MonsterWarManager.SetReplayState(true)`（`MonsterWarManager.lua:443`）会把客户端标成"正在回放"，UI/退出走 `QuitReplay`（行 459）；`IsReplayBattle()`（行 473）是查询入口。`InstanceManager.ReplayInstanceTypeArr` 把 `MonsterWar=14` 也包含进去——重放类型走统一回放分支。

### 3. 协议入口（这才是核心）

`Assets/Scripts/LuaScript/Service/BattleService.lua`：

| 协议 | AID | 回调 | 行号 |
|---|---|---|---|
| 初始场景 | `AID_GET_BATTLE_DATA` | `OutGetBattleData` | 149 |
| 战报（带场景增量） | `AID_GET_BATTLE_LOG` | `GetBattleLog_CallBack` | 209 |
| 战报（不带场景） | `AID_GET_BATTLE_LOG_SIMPLE` | `GetBattleLogSimple_CallBack` | 231 |

`GetBattleLog_CallBack`（209）：

```lua
BattleMapComponent.DataManager.SetMapData_BattleLogMapId(data.scene.mapID)
BattleSceneData.BattleLogData(sceneData, sceneData.sceneMode, sceneData.teamState)  -- 场景增量入库
BattleAnalysis.PreprocessLogData(data.logs, sceneData.sceneMode,
                                 sceneData.frameNum, sceneData.roomID)              -- 战报入缓冲
-- statistic 单独走 BattleStaticsData.ParseBattleStatistic
```

`GetBattleLogSimple_CallBack`（231）：只调 `PreprocessLogData`（没场景增量，对应服务器 `scenePb == nil` 的帧）。

这两个回调对回放和实时战斗**完全一样**——所以 Q1 描述的服务器侧 `OutGetBattleLog(Simple)` 推下来，客户端走的就是这两条路径。

### 4. 三段流水线：缓冲 → 排序 → 解析

#### 4.1 PreprocessLogData（缓冲，1475）

```lua
function BattleAnalysis.PreprocessLogData(logsTab, sceneMode, curServerFrameNum, roomID)
    table.insert(preprocessDatas, {
        roomID = roomID, sceneMode = sceneMode,
        curServerFrameNum = curServerFrameNum, logsTab = logsTab,
    })
end
```

只入队、不解析——避免在网络回调里做重活，且为按帧号排序留余地（多个包乱序到达时能纠正）。

#### 4.2 主循环驱动（1450）

```lua
function BattleAnalysis.UpdateAction()
    UpdateActionData(false)     -- 把已到期的 actionData 执行掉
    CheckNetSteady()            -- 网络稳定性检测
    HandleLogUpdate()           -- HandleLogBattle/Effect/MuNote 自身的 tick
    SyncPreprocessDatas()       -- 把缓冲的战报包按服务器帧号排序后逐一解析
end
```

`SyncPreprocessDatas`（1438）：

```lua
table.sort(preprocessDatas, function(a,b) return a.curServerFrameNum < b.curServerFrameNum end)
for _, v in ipairs(preprocessDatas) do
    BattleAnalysis.SyncData(v.logsTab, v.sceneMode, v.curServerFrameNum, v.roomID)
end
preprocessDatas = {}
```

#### 4.3 SyncData（解析 + 时间映射，1490）

```lua
local curFrameTime    = curServerFrameNum * BattleSceneData.frameRate
local triggerFrameTime = curFrameTime - BattleSceneData.joinRoomFrameTime
BattleData.SetFrameTime(triggerFrameTime)

for _, v in ipairs(logsTab) do
    local index = v.data[BattleAnalysis.FUN_KEY_INDEX]    -- v.data[1] 是 ActionKind
    local actionData = {
        logKind = sceneMode, actionIndex = index,
        time    = triggerFrameTime,                       -- 决定何时被 UpdateActionData 出队
        limitTime = Time.realtimeSinceStartup,            -- 用于战报解析超时检测
        data    = v.data,
    }
    if     index == MOVE_POS      then AddMoveAction(actionData)
    elseif index == DISPLACEMENT  then AddDisplacementAction(actionData)
    elseif index == ASSAULT       then AddAssaultAction(actionData)
    elseif index == FORCE_REQUEST_BATTLE_SCENE_DATA2 then
        HandleLogBattle.OnForceRequestBattleSceneData2(); return
    elseif index == ROOM_DELETE then
        UpdateActionData(true)                            -- 强制把积压跑完，避免死亡动画丢
        BattleAnalysis.RefreshLogTime(); return
    end
    table.insert(actionDatas, actionData)                 -- 其余 action 进队列
end
```

要点：
- **服务器帧号 → 本地战斗时间**：`triggerFrameTime = serverFrameNum * frameRate - joinRoomFrameTime`，回放/实时战斗共用同一套换算，所以"按服务器帧重放"就是"按 actionData.time 出队"。
- **5 个分支立即处理**：移动/位移/突进要排程到自己的子系统，强制取场景和房间删除则同步执行；其余统一入 `actionDatas` 等待 `UpdateActionData` 按时间出队。
- **`actionDatas` 的出队条件**（`UpdateActionData`，1374）：`BattleData.GetFrameTime() >= info.time` 时 `SyncActionData(data)`；积压超 `logDelayDistanceTime = 15s` 会主动 `GameNet.Disconnect`（PetSquad/Roguelike/mapId 10003 例外）。

### 5. ActionKind 分发与 Arena 专属段

`BattleAnalysis.ActionInfos` 是 `actionIndex → { handle, ... }` 的查表（`BattleAnalysis.lua:284-388` 通用段，`896-941` Arena 段）。`SyncActionData` 根据 `actionData.actionIndex` 拿 `handle` 调用。

**Arena 专属（800~809），全部在 `_HandleLogBattleArena.lua`**：

| ActionKind | 函数 | 行 | 说明 |
|---|---|---|---|
| 800 ARENA_READY | `OnArenaReady` | 61 | 入场倒计时 |
| 801 ARENA_START | `OnArenaStart` | 82 | 战斗开打 |
| 802 ARENA_END | `OnArenaBattleEnd` | 103 | 战斗结束 |
| 803 ARENA_LOG | `OnArenaLog` | 137 | 结果文本日志 |
| 804 ARENA_TIME_LINE | `OnArenaTimeline` | 38 | 播放过场 Timeline |
| 805 ARENA_SHAKE_CAMERA | `OnArenaShakeCamera` | 119 | 屏幕震动 |
| 806 ARENA_Skill | `OnArenaSkill` | 122 | 技能释放同步（区别于普通战斗的 Skill） |
| 807 MONSTER_POWER_BOSS | `OnMonsterPowerBossInfo` | 187 | 最强魔物 Boss 信息（也走这条管线） |
| 808 MONSTER_POWER_BOSS_REPLACE | `OnReplaceMonsterOnAllyDeath` | 217 | 失败方上场换怪 |
| 809 MONSTER_POWER_BOSS_SKILL | `OnMonsterPowerBossSkill` | 228 | Boss 技能 |

通用段（攻击、技能、伤害、死亡、弹道、特效、镜头…）回放和实时打共用 `HandleLogBattle`/`HandleLogBattleEffect`，没有 Arena 分支。

### 6. BattleSceneData 增量怎么落

`BattleSceneData.BattleLogData(data, sModel, tState)`（`BattleSceneData.lua:802`）每次 `GetBattleLog_CallBack` 都会被调一次（带场景的帧才有）：

```lua
BattleSceneData.TeamState = tState
AddRoomDatas(data.players)        -- 房间角色基础信息（uid, key, level…）→ roomDataArr
AddSceneDatas(data.playerDatas)   -- 角色场景数据（HP、Buff、位置…）→ scenePlayerArr
AddSceneDatas(data.monsterDatas)  -- 怪物场景数据（这才是 Arena 关心的）
BattleSceneData.UpdateMonsterSceneData()  -- 触发怪物视图侧更新
```

服务器侧 Q1 提过：回放房在 `addLogLogData` 阶段**只挑了 `MonsterDatas` 存进 `ScenePB`**——所以回放包到客户端时，`data.playerDatas` 一般为空，`data.monsterDatas` 才有内容，落到 `scenePlayerArr` 后驱动怪物 HP Bar/Buff/位置同步。

实际怪物视图侧的应用：HP 走 `OnSyncHP`（ActionKind 15）→ `BattlePlayer:SetCurrentHp`；Buff 走 124 段 → `HandleLogBattleEffect`；死亡走 10 → `player:SetDie(dieKind)`。这些都不是 Arena 专属，回放就是把原始战斗里发生过的事再触发一遍。

### 7. 回放专属：跳过断网检测

`CheckNetSteady`（1402）会读 `logTime`（上次收到战报的时间），超 `logMissDistanceTime = 5s` 没收到就 `GameNet.Disconnect` 重连。但**回放模式直接 return**：

```lua
if BattleSceneData.IsKindMonsterArenaOrReplay() then return end
if BattleSceneData.IsKindMonsterPower()        then return end
if BattleSceneData.IsKindPetSquad()            then return end
if BattleSceneData.IsKindRoguelike()           then return end
```

原因：回放战报是服务器从存档逐帧推的，节奏可能很均匀但也可能停顿（比如服务端 `stopLoop` 前的补包），不应该被当作"网络断"重连。

### 8. 回放结束

服务器侧 `checkReplayBattleEnd` 跑到 `LastFrame` 后 `rpcBattleEnd` → Game 服 `PushReplayTeamEnd`。客户端：

```
MonsterWarManager.OnReplayEnd(force)            (MonsterWarManager.lua:451)
  ├─ force=true → MonsterWarManager.QuitReplay  (行 459)
  │     └─ SetReplayState(false)
  │     └─ 关 EventMainView/BattleMainView/BattleTickView/BattleWinnerView
  │     └─ TeamService.InBattleTeamLogin → Server_InGetMainSceneInfo → 开回 EventMainView
  └─ force=false → SendMsg(ViewEvent.MonsterWarReplayEnd)  // 等 UI 自己处理
```

`QuitReplayOnRelogin`（行 477）是重连场景下的清理：只关 UI、置 `replayState = false`，不走完整退出流程。

### 9. 全链路一图

```
（服务器侧）OutGetBattleLog / OutGetBattleLogSimple
       │
       ▼
BattleService.GetBattleLog_CallBack / GetBattleLogSimple_CallBack
       │  ├─ BattleSceneData.BattleLogData(sceneData,...)   ← 场景增量（仅 OutGetBattleLog 有）
       │  └─ BattleAnalysis.PreprocessLogData(logs, sceneMode, frameNum, roomID)
       │         └─ preprocessDatas[] ← 入缓冲
       ▼
（主循环 tick） BattleAnalysis.UpdateAction()
       ├─ UpdateActionData(false)              ← 把已到期 actionData 出队执行 SyncActionData
       ├─ CheckNetSteady()                     ← Arena/Replay 直接跳过
       ├─ HandleLogUpdate()                    ← HandleLogBattle / Effect / MuNote 自身 tick
       └─ SyncPreprocessDatas()
              ├─ table.sort by curServerFrameNum
              └─ for each → BattleAnalysis.SyncData(...)
                    ├─ triggerFrameTime = frameNum * frameRate - joinRoomFrameTime
                    ├─ BattleData.SetFrameTime(triggerFrameTime)
                    └─ for each log:
                          ├─ MOVE_POS / DISPLACEMENT / ASSAULT → 立即排程到子系统
                          ├─ FORCE_REQUEST_BATTLE_SCENE_DATA2 → 立即执行 + return
                          ├─ ROOM_DELETE → 强制清掉积压 + return
                          └─ 其他 → 入 actionDatas[]（等 UpdateActionData 按 time 出队）
       ▼
SyncActionData(data) → ActionInfos[data.actionIndex].handle(data)
       ├─ 通用：HandleLogBattle.OnAttack / OnSkill / OnDie / OnSyncHP ...
       └─ Arena 段 800-809：HandleLogBattleArena.OnArenaReady / Start / End / Timeline / Skill ...
```

### 10. 关键文件清单

| 文件 | 关键符号/行号 |
|---|---|
| `Assets/Scripts/LuaScript/Data/BattleData/_Battle_Kind.lua` | `ModeKindMonsterArena=13`、`ModeKindMonsterWar=14`（18-19） |
| `Assets/Scripts/LuaScript/Data/BattleData/BattleSceneData.lua` | `RefreshSceneData`（743）、`BattleLogData`（802）、`IsKindMonsterArenaOrReplay`（1671） |
| `Assets/Scripts/LuaScript/Service/BattleService.lua` | `OutGetBattleData`（149）、`GetBattleLog_CallBack`（209）、`GetBattleLogSimple_CallBack`（231） |
| `Assets/Scripts/LuaScript/Service/InstanceService.lua` | `PushCommonInstanceTeamInfo`（514） |
| `Assets/Scripts/LuaScript/Plane/Instance/InstanceManager.lua` | `SetInstanceTeamData`（307）、`CallTypeManagerFunc`（280）、`PushCommonInstanceTeamInfo`（835）、`ReplayInstanceTypeArr` |
| `Assets/Scripts/LuaScript/Plane/Instance/MonsterWar/MonsterWarManager.lua` | `SetReplayState`（443）、`OnReplayBegin`（447）、`OnReplayEnd`（451）、`QuitReplay`（459）、`IsReplayBattle`（473） |
| `Assets/Scripts/LuaScript/Plane/BattlePlane/BattleNet/BattleAnalysis.lua` | `ActionInfos`（284-388, 896-941）、`UpdateActionData`（1374）、`CheckNetSteady`（1402）、`SyncPreprocessDatas`（1438）、`UpdateAction`（1450）、`PreprocessLogData`（1475）、`SyncData`（1490） |
| `Assets/Scripts/LuaScript/Plane/BattlePlane/BattleNet/_HandleLogBattleArena.lua` | `OnArenaTimeline`（38）、`OnArenaReady`（61）、`OnArenaStart`（82）、`OnArenaBattleEnd`（103）、`OnArenaShakeCamera`（119）、`OnArenaSkill`（122）、`OnArenaLog`（137）、`OnMonsterPowerBossInfo`（187）、`OnReplaceMonsterOnAllyDeath`（217）、`OnMonsterPowerBossSkill`（228） |

### 一句话总结

客户端不区分"实时"和"回放"：服务器侧 Q1 推下来的 `OutGetBattleLog(Simple)` 通过 `BattleService` 两个回调进入，先被 `PreprocessLogData` 缓冲；主循环 `BattleAnalysis.UpdateAction` 把缓冲按服务器帧号排序后 `SyncData`，按 `frameNum * frameRate` 算出本地触发时间挂到 `actionDatas`，再由 `UpdateActionData` 按时间出队、按 `ActionKind` 走 `HandleLogBattle` / `HandleLogBattleArena`（800~809 Arena 专属段）；场景增量走 `BattleSceneData.BattleLogData` 落到怪物状态。回放和实时打的唯一差异是 `sceneMode` 字段以及 `CheckNetSteady` 在 Arena/Replay 模式下被跳过。

---

## Q5: 怪物擂台赛程为何严格串行 — AddNewBattle/AddMonster/SendRpc 循环细节

### 一句话答案

下一场对阵的双方怪 ID **必须从上一轮战报回填的 `WinGroupId` 推导**(`GetWinMonsterInfo` 读 `m.WinGroupId`)，而 `WinGroupId` 只能由 `MonsterArenaSendBattleRpc` 同步等 battle 服跑完战斗后 `SetLogInfo` 写入。所以 `MonsterArenaGenerateAllBattleLog` 整个赛程生成是**单 goroutine 顺序循环**：每场必须等上一场战报回来才能进入下一场——**没有任何 goroutine、batch、流水线**。

### 1. 数据结构定下的依赖关系

`internal/data/monster_arena.go:487`：

```go
type MonsterArenaBattleLog struct {
    MonsterInfo []*MosterArenaMonsterBattleInfo  // 出阵的两只怪(每只带词条)
    WinGroupId  uint32                           // 战斗后回填的胜者 monsterID
    ScenePB / Logs / LastFrame / GroupIds        // 战报本体
}
```

胜者读取函数(`monster_arena.go:506`)：

```go
func (m *MonsterArenaBattleLog) GetWinMonsterInfo() *MosterArenaMonsterBattleInfo {
    winMonsterID := m.WinGroupId   // ★ 关键：必须先有 WinGroupId
    for _, mi := range m.MonsterInfo {
        if mi.MonsterID == winMonsterID { return mi }
    }
    return nil
}
```

**`WinGroupId` 的来源链**：battle 服跑完战斗 → `arenaBattleFinish`(`battle_mgr_raid_arena1.go:65`)调 `mgr.br.setWinGroupId(int32(winBoss.group.conf.ID))`(`battle_mgr_raid_arena1.go:78`) → `RpcFinishBattleEnd` 退出 quickCreateLog 循环 → `newBattleRoomArena` 把 `br.logsData.WinGroupId` 装进 `RPCBattleLogInfo.WinGroupId` 返回 → game 服 `SetLogInfo` 写回 → 此时 `GetWinMonsterInfo` 才能拿到值。

### 2. 赛程生成器的循环骨架

`internal/db/service/monster_arena.go:12` `MonsterArenaGenerateAllBattleLog` 是个**普通同步函数**(无 goroutine)，按 battleID 从小到大逐场跑：

```go
// 第一轮：8 选 4 (battleID 1-4)
for battleID := uint32(1); battleID <= 4; battleID++ {
    battleLogInfo.AddNewBattle(battleID, instanceID)          // ① 创建空槽
    for _, monsterID := range []uint32{monsters[2*(b-1)], monsters[2*(b-1)+1]} {
        // 直接用预设的 8 只怪，词条从 Pool8ID 随机
        battleLogInfo.AddMonster(battleID, monsterID,
            entryData.RandomEntry(globalConfData.Ability8Num, nil))  // ② 双方入场
    }
    if err := MonsterArenaSendBattleRpc(battleLogInfo, battleID); err != nil {
        return nil, err                                       // ③ 同步跑战斗、回填 WinGroupId
    }
}

// 第二轮：4 选 2 (battleID 5-6) —— 输入依赖第一轮的 WinGroupId
for battleID := uint32(5); battleID <= 6; battleID++ {
    battleLogInfo.AddNewBattle(battleID, instanceID)
    lastBattleIDs := []uint32{1, 2}                           // 5 号 = 1+2 胜者
    if battleID == 6 { lastBattleIDs = []uint32{3, 4} }       // 6 号 = 3+4 胜者
    for _, lastBattleID := range lastBattleIDs {
        winMonsterInfo := battleLogInfo.GetBattle(lastBattleID).GetWinMonsterInfo()
        if winMonsterInfo == nil {                            // ★ 上一轮没 SetLogInfo 这里直接报错
            return nil, errors.Errorf("WinMonsterInfoNotFound: %d", lastBattleID)
        }
        // 用上一轮胜者的怪 ID + 它原有词条 + 从 Pool4ID 加 4 强词条
        randomEntry := entryData.RandomEntry(globalConfData.Ability4Num, winMonsterInfo.MonsterEntry)
        monsterEntry := append([]uint32{}, winMonsterInfo.MonsterEntry...)
        monsterEntry = append(monsterEntry, randomEntry...)
        battleLogInfo.AddMonster(battleID, winMonsterInfo.MonsterID, monsterEntry)
    }
    MonsterArenaSendBattleRpc(battleLogInfo, battleID)
}

// 第三轮：决赛 (battleID 7) —— 依赖 5+6 胜者，Pool2ID 加 2 强词条
// ... 同模式

// Type2 14 天活动追加：
// 11-14 (玩家选的另 8 只怪 8 选 4)
// 15-16 (11+12, 13+14 胜者 → 4 选 2)
// 17    (15+16 胜者 → 2 选 1，玩家组冠军)
// 18    (7 vs 17，怪冠军 vs 玩家组冠军，★ 不再加词条)
```

### 3. 三种依赖等级

| battleID | 怪来源 | 词条来源 | 依赖 |
|----------|--------|---------|------|
| 1-4 / 11-14 | 入参 `monsters[]` / `players[]` 配对 | `Pool8ID` 随机 8 个 | **无**(根节点) |
| 5-6 / 15-16 | 前一轮 `WinGroupId` | 上一轮词条 + `Pool4ID` 加 4 个 | 各依赖 2 场 |
| 7 / 17 | 前一轮 `WinGroupId` | 上一轮词条 + `Pool2ID` 加 2 个 | 依赖 2 场 |
| 18 | 7 + 17 胜者 | **不加新词条**(`monster_arena.go:251`) | 跨大组依赖 |

注意 1-4 和 11-14 内部**理论上**可以并行(没有相互依赖)，但代码里是顺序 for 循环 + 同步 RPC——估计是为了简化错误处理和避免 battle 服并发抖动。

### 4. 严格串行的代码证据

#### (a) `MonsterArenaSendBattleRpc` 是同步阻塞

`internal/db/service/monster_arena.go:267`：

```go
resp, err := client.RpcBattleCreateArenaRoom(battleSID, &pb.RPCBattleRoom{...})
// ↑ 这一行同步阻塞，直到 battle 服跑完整场战斗(可能上百帧 update + broadcast)才返回
if err != nil { return ... }
battleInfo.SetLogInfo(resp.LogInfo)   // 同步回填 WinGroupId
```

`client.RpcBattleCreateArenaRoom`(`internal/rpc/client/battle_team.go:78`)是普通同步 gRPC 调用,**没有超时控制以外的异步包装**。

#### (b) battle 服侧是 `for-loop` 跑完，不返回中间结果

`internal/battle/battle_room.go:346` `newBattleRoomArena`：

```go
br.loop = true
for br.isQuickCreateLog {              // ★ 同步循环
    br.mgr.update()                    // 推一帧
    br.broadcastFrame()                // 这里走 broadcastFrameSendPlayerArena
}
// 跳出循环时 isQuickCreateLog 被 RpcFinishBattleEnd 改成 false
// 此时 br.logsData.WinGroupId 已经填好(见 setWinGroupId)

logInfo := &pb.RPCBattleLogInfo{
    WinGroupId: uint32(br.logsData.WinGroupId),   // 出帧打包返回
    ...
}
```

`isQuickCreateLog` 的退出条件：`RpcFinishBattleEnd`(`battle_mgr_raid_arena1.go:102`)在战斗 `arenaStateWait` 倒计时结束时把它置 false——也就是战斗自然分出胜负后才退出。

#### (c) 同步循环的代价

整个 `MonsterArenaGenerateAllBattleLog` 在调用方线程里依次跑：
- Type1: **7 场** × 单场耗时 (= 战斗实际帧数 / 100帧每秒 / FrameTick 加速) ≈ 几百毫秒到几秒
- Type2: **15 场** 串起来更久

因为是 GM/工具触发的离线赛程生成,串行延迟可接受,不需要并行化。

### 5. 如果中途某场 RPC 失败会怎样

```go
err := MonsterArenaSendBattleRpc(battleLogInfo, battleID)
if err != nil {
    return nil, err   // 整个赛程生成放弃
}
```

任意一场失败就 **abort 全流程**，前面已经跑过的场次会在 `battleLogInfo` 里留下半成品(`MonsterInfo` 已填、`Logs/WinGroupId` 未填)，但因为返回 `nil, err`，调用方拿不到也不会落盘——重试整个 `MonsterArenaGenerateAllBattleLog` 即可。代码里**没有断点续跑/幂等**机制。

### 6. 为何不能并行

| 场次 | 能否并行 |
|------|---------|
| 1-4 之间 | 理论上可以(无相互依赖)，但代码选择顺序 |
| 5-6 与 1-4 | **不能**(5 号要等 1+2 战报)|
| 7 与 5-6 | **不能**(7 号要等 5+6 战报)|
| 11-14 与 1-4 | 理论上可以(两组完全独立)，代码也选择顺序 |
| 18 与 7/17 | **不能**(总决赛依赖两边冠军)|

总之只要某场战斗的**怪 ID 来自 `GetWinMonsterInfo`**，它就必须排在依赖场次的 `SetLogInfo` 之后。

### 7. 流程图

```
┌─ game 服 ─────────────────────────────────────┐
│  MonsterArenaGenerateAllBattleLog (同步)      │
│                                                │
│  for battleID in [1,2,3,4,5,6,7, (11..18)]:   │
│    AddNewBattle(battleID, instanceID)         │
│    AddMonster(battleID, 双方怪, 词条)         │
│       ↑                                        │
│       └ 从 GetWinMonsterInfo(上一场) 取得      │
│    ┌─→ MonsterArenaSendBattleRpc ──同步RPC──┐ │
│    │                                        │ │
│    │   battle 服 newBattleRoomArena:        │ │
│    │     for br.isQuickCreateLog {          │ │
│    │       update + broadcastFrame          │ │
│    │       addLogLogData → br.logsData      │ │
│    │     }                                  │ │
│    │     setWinGroupId(winBoss)             │ │
│    │     return RPCBattleLogInfo            │ │
│    │                                        │ │
│    └── SetLogInfo(resp.LogInfo) ←───────────┘ │
│         (此时 WinGroupId 才可被下一场读到)    │
└────────────────────────────────────────────────┘
```

### 一句话总结

赛程是**单 goroutine 同步 for 循环**：每场 `AddNewBattle` 建槽、`AddMonster` 入怪、`MonsterArenaSendBattleRpc` 同步阻塞等 battle 服把战斗跑完并把 `WinGroupId` 写回，下一场才能用 `GetWinMonsterInfo` 拿到胜者作为入场怪——这条数据依赖链让"并行化"无从下手。

---

## Q6: PreprocessLogData 怎么确保服务器战报按顺序一帧帧播放

### 结论

`PreprocessLogData` 本身**只是个无脑入队**，"按帧顺序"是由后面四道关**联合**保证的。客户端的播放节奏**完全由服务器推送频率决定**——服务器推一包，客户端把 `frameTime` 跳到该帧并把这帧的所有 log 一起入队、按入队顺序在下一 tick 出队执行。

### 1. PreprocessLogData 自己什么都不做（`BattleAnalysis.lua:1475`）

```lua
function BattleAnalysis.PreprocessLogData(logsTab, sceneMode, curServerFrameNum, roomID)
    table.insert(preprocessDatas, {
        roomID = roomID, sceneMode = sceneMode,
        curServerFrameNum = curServerFrameNum, logsTab = logsTab,
    })
end
```

故意只入队不解析——把"排序时机"推迟到本 tick 拿全所有包之后再做。

### 2. 第 1 道关：包级排序 — SyncPreprocessDatas（1438）

主循环 `BattleAnalysis.UpdateAction` (1450) 每 tick 调一次：

```lua
table.sort(preprocessDatas, function(a,b) return a.curServerFrameNum < b.curServerFrameNum end)
for _, v in ipairs(preprocessDatas) do
    BattleAnalysis.SyncData(v.logsTab, v.sceneMode, v.curServerFrameNum, v.roomID)
end
preprocessDatas = {}
```

`OutGetBattleLog`（AID=5）和 `OutGetBattleLogSimple`（AID=1）走两条不同回调通路，同 tick 可能多包乱序到达——`table.sort` 按 `curServerFrameNum` 升序统一重排。

### 3. 第 2 道关：跨 tick 倒退保护 — SyncData 入口（1494）

```lua
local oldServerFrameNum    -- upvalue：记最近处理过的帧号
function BattleAnalysis.SyncData(logsTab, sceneMode, curServerFrameNum, roomID)
    if oldServerFrameNum and curServerFrameNum and oldServerFrameNum > curServerFrameNum then
        print("err!!!!!oldServerFrameNum > curServerFrameNum!!!!!!!", ...)
        return    -- 直接丢，防 stale 包
    end
    ...
    oldServerFrameNum = curServerFrameNum    -- 行 1528
end
```

`table.sort` 只管同 tick 内顺序；跨 tick 如有更老包姗姗来迟（如重连后服务器补发），这里拒绝。

### 4. 第 3 道关：帧号 → 本地时间映射，按时间出队

#### 4.1 进房时定零点（`BattleSceneData.lua:664-673`）

```lua
data.frameRate = data.frameRate or 15
BattleSceneData.frameRate          = 1 / data.frameRate            -- ★ 变量名叫 frameRate，实际装的是"每帧时长"
BattleSceneData.joinRoomFrameTime  = data.frameNum * frameRate     -- 进房那帧绝对时间（零点）
BattleData.SetTime(joinRoomFrameTime - 1)                          -- 本地时间放早 1s，让首包能立即触发
```

#### 4.2 每包给所有 log 打统一时间戳（`SyncData` 1499-1525）

```lua
local curFrameTime     = curServerFrameNum * BattleSceneData.frameRate    -- 该帧绝对时间
local triggerFrameTime = curFrameTime - BattleSceneData.joinRoomFrameTime -- 相对进房的时间
BattleData.SetFrameTime(triggerFrameTime)                                 -- ★ 全局 frameTime 推到此包

for _, v in ipairs(logsTab) do        -- ★ 按服务器写入顺序遍历
    local actionData = {
        actionIndex = v.data[1],
        time        = triggerFrameTime,   -- ★ 同包所有 log 共用同一时间
        limitTime   = Time.realtimeSinceStartup,
        data        = v.data,
    }
    -- 5 类特殊立即排程（MOVE_POS/DISPLACEMENT/ASSAULT/FORCE_REQUEST.../ROOM_DELETE）；其余：
    table.insert(actionDatas, actionData)   -- ★ 按顺序入主队列
end
```

#### 4.3 主队列按时间出队（`UpdateActionData` 1374）

```lua
local time = BattleData.GetFrameTime()    -- 当前 frameTime
while true do
    local info = actionDatas[index]
    if Time.realtimeSinceStartup - info.limitTime >= 15 then    -- 第 4 道：超时熔断
        GameNet.Disconnect(false, "UpdateActionData", 0); break
    end
    if time >= info.time or isClear then
        SyncActionData(table.remove(actionDatas, index))         -- 到点了，执行
    else
        index = index + 1                                        -- 没到，跳过留队
    end
end
```

`BattleData.GetFrameTime()` (`BattleData.lua:1216`) 返回 `frameTime - delayAyncFrameTime`，**不是**本地真实时间——每次 `SyncData` 处理新包时 `SetFrameTime` 会被刷新到该包的帧时间。

### 5. 第 4 道关：积压熔断（行 273）

```lua
local logDelayDistanceTime = 15    -- 单条 action 在队列 15s 还没执行
```

超过即 `GameNet.Disconnect` 重新拉——这是最后的保险，承认"卡住了，重连吧"。

### 6. 关键编排：UpdateAction 调用顺序（1450）

```lua
function BattleAnalysis.UpdateAction()
    UpdateActionData(false)     -- ① 先用"上次"的 frameTime 把残留出队
    CheckNetSteady()            -- ② 断网检测（Arena/Replay 跳过）
    HandleLogUpdate()
    SyncPreprocessDatas()       -- ③ 本 tick 新到的包：排序+解析+SetFrameTime+入 actionDatas
end
```

**①在③之前**：本 tick 解析出的新 action 会在**下 tick** 才被执行（因为 ① 用的是上一 tick 留下的 frameTime）。这种"延迟一帧"换来的是：保证同帧内所有包都被一并准备好、不存在"半帧"状态。

### 7. 多层时序总结表

| 层级 | 谁负责 | 解决什么 |
|---|---|---|
| 包顺序 | `SyncPreprocessDatas` 的 `table.sort` | 同 tick 内不同包乱序到达 |
| 跨 tick 倒退 | `SyncData` 的 `oldServerFrameNum` 检查 | 重连后服务器补发更老包 |
| 同包内 log 顺序 | `for _, v in ipairs(logsTab)` + 同 `time` + FIFO 入 `actionDatas` | 同帧内 N 条 log（攻击→伤害→死亡）必须按写入顺序 |
| 执行节奏 | `BattleData.GetFrameTime() >= info.time` | 一条 log 必须等到对应"服务器帧时间"到达才执行 |
| 兜底 | `logDelayDistanceTime=15s` | 卡死时主动重连 |

### 8. ROOM_DELETE 的特殊处理（`SyncData` 1520）

```lua
elseif index == BattleAnalysis.ActionKind.ROOM_DELETE then
    UpdateActionData(true)        -- ★ 强制把积压的 actionDatas 全部立即执行
    BattleAnalysis.RefreshLogTime(); return
end
```

房间删除是终止信号，必须把残留全跑完——避免怪物最后的死亡动画因为时间没到而被跳过，也防止 `CheckNetSteady` 误判断线。

### 一句话总结

`PreprocessLogData` 只是缓冲入口，真正的顺序由"**SyncPreprocessDatas 按 frameNum 排序 + SyncData 拒绝倒退 + 同帧 log 共用 triggerFrameTime 并按数组顺序入队 + UpdateActionData 按 GetFrameTime 出队 + 15s 超时熔断**"五件事联合保证。客户端不主动控速，节奏完全由服务器推送频率驱动；同 tick 解析的 action 会在下一 tick 执行（"延迟一帧"换全包一致性）。

---

## Q7: SyncData 里 BattleData.SetFrameTime 为什么每包都改 frameTime

### 先澄清：是每包一次，不是每条 actionData 一次

`SetFrameTime` 在 `SyncData` 里调一次，**在 `for _, v in ipairs(logsTab) do` 循环之前**（`BattleAnalysis.lua:1501`）：

```lua
function BattleAnalysis.SyncData(logsTab, sceneMode, curServerFrameNum, roomID)
    ...
    local curFrameTime     = curServerFrameNum * BattleSceneData.frameRate
    local triggerFrameTime = curFrameTime - BattleSceneData.joinRoomFrameTime
    BattleData.SetFrameTime(triggerFrameTime)        -- ★ 循环外，每包一次

    for _, v in ipairs(logsTab) do                   -- ★ 同包内所有 log 共用同一 triggerFrameTime
        local actionData = { ..., time = triggerFrameTime, ... }
        table.insert(actionDatas, actionData)
    end
end
```

同包内所有 log 共享同一 `frameNum`，所以共享同一 `triggerFrameTime`，没必要逐条改。

### 1. frameTime 是"已收到的最新服务器帧"的阶梯锚点，不是本地推进的时间

`BattleData` 有两个时间字段：

| 字段 | Set 路径 | 推进方式 | 含义 |
|---|---|---|---|
| `time` (`GetTime`) | `BattleData.UpdateFrame` (`BattleData.lua:1155`) `time += deltaTime*timeScale` | 本地 tick 自增 | 本地游戏时间，连续推进 |
| `frameTime` (`GetFrameTime`) | **只在 `SyncData` 里 set** | **不会自动推进** | 客户端"已知"的最新服务器帧时间 |

`frameTime` 是个**阶梯函数**：服务器推一包，台阶往上跳一级；不推就停在原地。客户端不假装"服务器又走了几帧"，只承认"我看到过的最新帧"。

### 2. 为什么用阶梯而不让客户端自己推进 frameTime

如果客户端 `frameTime += deltaTime`，会出三类问题：

- **网络抖动放大**：本地 60fps 但服务器推 15Hz，客户端会"跑得比服务器快"，到下一包到达时本地 frameTime 已超过新包 triggerFrameTime，UpdateActionData 一窝蜂消费，丢掉细粒度时序。
- **断线/卡顿后无法对齐**：客户端卡 2s，本地推进的 frameTime 也卡 2s，但服务器实际跑了 30 帧战报；恢复后无法按正确速度回放。
- **回放节奏失控**：回放模式下服务器自己控下发节奏（Q1：`broadcastFrameSendPlayerReplay` 按 `frameNum` 推），如果客户端自走 frameTime，就和服务器节奏脱钩。

**让 `frameTime` 完全由 SetFrameTime 驱动** = "服务器推多快客户端就播多快"，天然继承服务器节奏，客户端不做速率匹配。

### 3. SetFrameTime 真正起作用的地方：UpdateActionData 的出队阀门

```lua
local time = BattleData.GetFrameTime()         -- 当前阀门高度
while ... do
    if (time >= info.time or isClear) then
        SyncActionData(table.remove(...))      -- info.time <= 阀门 → 放行
    else
        index = index + 1                      -- info.time > 阀门 → 留队
    end
end
```

SetFrameTime 的语义就是**"把阀门抬高到这一包的帧时间"**。同 tick sort 后依次 `SyncData(10) → SyncData(11) → SyncData(12)`，阀门被抬 3 次，最终停在 12——下一 tick UpdateActionData 一次性消费完 3 个包的所有 actionData，按入队顺序执行。

### 4. 为什么放在循环外、每包一次（而不是"一 tick set 一次最大值"）

理论上一 tick 只 set 一次最大值结果也一样（因为 UpdateActionData 在 SyncPreprocessDatas 之后不调用）。但放在 SyncData 循环外、每包一次有两个好处：

#### 好处 A：让包内立即执行的 action 拿到正确的"当前帧时间"

`SyncData` 内有 5 类 action 不入队，而是立即执行：

```lua
if     index == MOVE_POS      then AddMoveAction(actionData)
elseif index == DISPLACEMENT  then AddDisplacementAction(actionData)
elseif index == ASSAULT       then AddAssaultAction(actionData)
elseif index == FORCE_REQUEST_BATTLE_SCENE_DATA2 then
    HandleLogBattle.OnForceRequestBattleSceneData2(); return
elseif index == ROOM_DELETE then
    UpdateActionData(true)            -- ★ 直接调出队！
    BattleAnalysis.RefreshLogTime(); return
end
```

特别是 `ROOM_DELETE` 在循环内直接调 `UpdateActionData(true)`——这里 `BattleData.GetFrameTime()` 必须是**当前这一包**的帧时间，不能是上一包的。`AddMoveAction` 等也可能用 frameTime 算插值起点。所以 SetFrameTime 必须在进 for 循环之前完成。

#### 好处 B：单包语义自洽

每个 `SyncData(packet)` 是个完整语义单元：阀门抬到该包帧时间 + 该包 logs 排程进队列。这样即使将来在循环外加新逻辑（比如统计某帧解析了多少 action），也能基于 `GetFrameTime()` 拿到对的"当前帧"。

### 5. 为什么不在 ipairs(logsTab) 循环内逐条 SetFrameTime

因为同包内所有 log 的 `triggerFrameTime` **必然相同**（共享同一 `curServerFrameNum`）。循环内 set 是冗余写入——结果一样但 N 次函数调用 + 表赋值的开销。放在循环外一次，是正常的提循环不变量。

### 关键代码位置

| 位置 | 作用 |
|---|---|
| `BattleAnalysis.lua:1499-1501` | `SyncData` 循环外算 triggerFrameTime + SetFrameTime |
| `BattleAnalysis.lua:1502-1525` | 循环内每 log 共用 triggerFrameTime 作为 `actionData.time` |
| `BattleAnalysis.lua:1520-1523` | ROOM_DELETE 立即调 `UpdateActionData(true)`——依赖刚 set 的 frameTime |
| `BattleAnalysis.lua:1374-1399` | `UpdateActionData` 按 `BattleData.GetFrameTime() >= info.time` 出队 |
| `BattleData.lua:1210-1219` | `SetFrameTime` / `GetFrameTime` 实现，`GetFrameTime` 返回 `frameTime - delayAyncFrameTime` |
| `BattleData.lua:1155-1163` | `UpdateFrame` 只推进 `time`，不动 `frameTime` |
| `BattleSceneData.lua:667-671` | 进房时 `frameRate = 1/data.frameRate`、`joinRoomFrameTime = frameNum*frameRate`、`SetTime(joinRoomFrameTime-1)` |

### 一句话总结

`frameTime` 是"已知最新服务器帧"的**阶梯式时间锚点**，不自动推进，**每包 set 一次**是为了：① 让 `UpdateActionData` 的出队阀门跟随服务器节奏抬升；② 让同包内立即执行的 action（MOVE / DISPLACEMENT / ASSAULT / FORCE_REQUEST / ROOM_DELETE）能读到本包的帧时间。同包内多条 log 共享同一帧号，所以 SetFrameTime 放在 `for` 循环外、每包一次就够了——逐 actionData set 是冗余。
