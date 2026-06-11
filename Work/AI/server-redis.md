# server-redis

## 目录

- [[#Q1: redis. z:userinfo: 和 z:userid: 是 什么关系]]
- [[#Q2: 总结一下本项目redis的存储流程]]
- [[#Q3: atomic Mutex 本项目用了多种并发语言，分析适用性及注意点]]
- [[#Q4: 从项目参考，这样写肯定有他的理由，分析一下]]
- [[#Q5: atomic.Value 适合读多写少的整体替换，为什么 map/slice 要视作只读]]
- [[#Q6: chat_room.go 详细介绍聊天服的流程]]
- [[#Q7: NATS 是什么]]
- [[#Q8: type ChatLimitItem []uint32 和 IsOnlineFull 这种写法底层原理是什么]]
- [[#Q9: PlayerGetSessionAddress 详细介绍]]
- [[#Q10: NATS startSubHandle 详细介绍]]
- [[#Q11: agentID 和 connID 是什么，为什么要用两个值]]
- [[#Q12: 如果玩家跟多个人发起私聊，是怎么区分的]]
- [[#Q13: 私聊同时 Publish 到两个人的 Topic 后，是怎么发送给玩家的]]
- [[#Q14: internal/session/game.Session，它内嵌了 *tunnel.UserSession 详细介绍]]
- [[#Q15: agentsession usersession gamesession区别是什么，为什么要分这么多]]
- [[#Q16: if us.pktmetrics.enable != 0 { us.pktmetrics.update(time.Now(), us.agentID, us.connID, b) } 这是什么意思]]
- [[#Q17: agent 上报 connID 断开 -> AgentSession.DelUserSession(connID) -> UserSession.GetRoleID() -> 通知 game 业务 roleID 断线详细介绍一下]]
- [[#Q18: if inPacket.IsCmdProto() { s.OnCommand(inPacket) } else { s.userHandler.OnMessage(s, inPacket) } 这里是怎么收到OnCommand包的，怎么知道是断线，发起断线的地方是怎么传的]]


## Q1: redis. z:userinfo: 和 z:userid: 是 什么关系

### 结论

`z:userinfo:` 和 `z:userid:` 都以 `roleid` 为核心，但职责不同：

- `z:userinfo:$group:$roleid` 是角色基础数据本体，保存 `UserInfo` JSON，里面包含等级、职业、名字、区服、账号 ID、slot、在线/服务器状态等角色信息。
- `z:userid:$group:$roleid` 是索引/映射数据，用来从 `roleid` 反查账号维度信息，实际值由 `UserIDVal.Marshal` 写成 `zoneid:userid:slotid`。

所以它们不是互相替代关系，而是“角色数据”和“角色 ID 到账号槽位映射”的关系。

### Key 结构

- `z:userinfo:$group:$roleid`
  - Redis 类型：`STRING`
  - 数据模型：`model.UserInfo`
  - 代码位置：`internal/db/model/userinfo.go`
  - 用途：按 `roleid` 读取/保存角色基础信息。

- `z:userid:$group:$roleid`
  - Redis 类型：`STRING`
  - 数据模型：`model.UserIDItem`
  - 代码位置：`internal/db/model/userid.go`
  - 用途：按 `roleid` 反查 `zoneid/userid/slotid`。

### 创建和登录链路

1. 登录时会先根据 `zoneid + userid + slotid` 通过 `z:roleid:$zone:$userid:$slotid` 找到 `roleid`。
2. 如果找不到，会通过 `z:usercnt:$group` 自增分配新 `roleid`。
3. 分配新 `roleid` 时，`UserIDRepo.NewRoleID` 会同时写：
   - `z:userid:$group:$roleid -> zoneid:userid:slotid`
   - `z:roleid:$zone:$userid:$slotid -> roleid`
4. 拿到 `roleid` 后，角色数据本体通过 `PlayerRepo.GetPlayer/SetPlayer` 读写，其中 `UserInfo` 对应 `z:userinfo:$group:$roleid`。

### 排查建议

- 想确认某个 `roleid` 的角色数据是否存在，看 `z:userinfo:$group:$roleid`。
- 想确认 `roleid` 属于哪个账号、区服、槽位，看 `z:userid:$group:$roleid`。
- 想从账号定位角色，优先看 `z:roleid:$zone:$userid:$slotid`；`z:userid` 是反向索引。
- 如果 `z:userinfo` 存在但 `z:userid/z:roleid` 缺失，角色本体还在，但登录侧可能无法从账号槽位定位到这个角色。

## Q2: 总结一下本项目redis的存储流程

### 总体结论

本项目 Redis 的核心定位是“在线主存 + 跨服务共享状态 + 索引/排行/配置态缓存”。玩家实时数据主要先落 Redis，MySQL 通过 `DBSaveRepo` 做定时或触发式长期落地。业务层通常不直接拼 Redis 命令，而是通过 `internal/db/repo` 的仓储对象操作 `model` 对象。

### 初始化流程

1. 服务启动时先设置全局 group：`dbrepo.InitModelKey(inconf.Group)`，底层调用 `model.SetGroup`，后续多数 key 会带上 `$group`。
2. `dbrepo.InitRedisDB` 根据配置初始化三类 Redis 连接：
   - `Hosts` / Data DB：主要业务数据、玩家数据、房间、队伍、事件等。
   - `Metas` / Meta DB：区服、运营配置、公告、服务元数据等。
   - `Others` / Other DB：高 IO 或特殊隔离数据，例如部分排行榜/玩法数据。
3. 各服务在 `internal/app/*/app.go` 中按职责初始化 repo。以 game 服为例，会初始化 `UserIDRepo`、`AccountRoleInfoRepo`、`PlayerRepo`、各种 Team/Rank/Event/Name repo。

### 对象存储模型

1. Redis 对象统一实现 `rdhelper.Storager`：
   - `DbKey()`：生成 Redis key 或 hash field。
   - `Marshal()`：对象序列化为 bytes，多数业务模型使用 JSON。
   - `Unmarshal()`：从 bytes 反序列化回对象。
2. 底层 `rdhelper.RedisDB` 封装了常用命令：
   - `Set/Get/MSet`：字符串对象。
   - `HSet/HGet/HGetAll`：Hash 对象。
   - `ZAdd/ZRange/ZRem` 等：排行类 Sorted Set。
   - `SetNx/SetNxEx/Increment/Delete`：锁、计数器、临时状态、删除。

### 账号到角色的索引流程

1. 登录/创角侧先维护账号角色列表：
   - `z:roleserver:$zone:$userid`
   - Redis 类型：`HASH`
   - field：`slotid`
   - value：`AccountRoleInfo`
2. game 登录时根据 `zoneid + userid + slotid` 获取或创建 `roleid`：
   - 正向索引：`z:roleid:$zone:$userid:$slotid -> roleid`
   - 反向索引：`z:userid:$group:$roleid -> zoneid:userid:slotid`
   - 自增计数：`z:usercnt:$group`
3. 拿到 `roleid` 后，才进入玩家主数据读写流程。

### 玩家数据读写流程

1. 玩家不是一个大对象存一个 key，而是按模块拆成多个 Redis string：
   - `z:userinfo:$group:$roleid`
   - `z:props:$group:$roleid`
   - `z:equip:$group:$roleid`
   - `z:task:$group:$roleid`
   - `z:battle:$group:$roleid`
   - 其他模块同理。
2. 读取时优先走 `PlayerRepo` 本地缓存：
   - `GetPlayer`：缓存命中直接返回。
   - 缓存未命中时通过 `PlayerLoader` 从 Redis 按模块加载。
   - `GetPlayerFromDBFlag` 支持按 `PlayerFlag` 只加载部分模块，常用于跨服/战斗/好友等只读场景。
3. 业务修改玩家数据时，模型方法会调用 `SetDirty()` 或 `SetRedisDirty()` 标记脏数据。
4. 保存时调用 `PlayerRepo.SetPlayer(player)`：
   - 扫描所有模块。
   - 只收集 Redis dirty 的模块。
   - 清理 Redis dirty 标记。
   - 单模块用 `SET`，多模块用 pipeline `MSET/SET` 写回 Redis。
   - 写 Redis 失败时会清玩家本地缓存，避免继续使用可能未落库的旧对象。

### MySQL 落地流程

1. Redis 是在线主路径；MySQL 由 `DBSaveRepo` 负责长期落地。
2. 模型里同时维护 Redis dirty 和 MySQL dirty：
   - `SetDirty()` 同时标记 Redis 和 MySQL dirty。
   - `SetRedisDirty()` 只标记 Redis dirty，例如部分在线状态。
   - `SetMySQLDirty()` 只标记 MySQL dirty。
3. game 服启动时 `InitDBSaveRepo` 并初始化玩家相关 MySQL 表。
4. 登录后会调用 `SetDBSaveFlagWhenLogin()`，心跳里通过 `IsDBSaveWhenHeartbeat()` 判断是否执行 `DBSaveRepo.SetPlayer("heartbeat", player, false)`。
5. `DBSaveRepo.SetPlayer` 会读取玩家的 MySQL dirty 模块，在事务里按模块 `INSERT` 或 `UPDATE` 到对应 MySQL 表。

### 常见 Redis 数据类型分布

- `STRING`：玩家模块数据、账号索引、封禁、token、计数器、单对象状态。
- `HASH`：账号角色列表、部分列表型对象，以外层 key + field 存储。
- `ZSET`：排行榜类数据。
- `SET/LIST`：部分队列、集合、池子或在线/候选关系。

### 排查思路

1. 查玩家主数据：先从 `roleid` 定位 `z:userinfo:$group:$roleid`，再看具体模块 key。
2. 查账号到角色：看 `z:roleserver:$zone:$userid` 和 `z:roleid:$zone:$userid:$slotid`。
3. 查 roleid 归属：看 `z:userid:$group:$roleid`。
4. Redis 有、MySQL 没：可能还没触发 DBSave，先看心跳/登录落地逻辑。
5. MySQL 有、Redis 没：可能是 Redis 数据丢失或未回灌，需要看 `DBSaveRepo.GetPlayer` 或导入工具链路。

## Q3: atomic Mutex 本项目用了多种并发语言，分析适用性及注意点

### 总体结论

这里的“并发语言”按“并发原语/并发工具”理解。本项目 Go 并发原语使用很多，主要包括 `atomic`、`atomic.Value`、`Mutex/RWMutex`、`channel`、`sync.Once`、`sync.WaitGroup`、`sync.Map`、`singleflight`。整体使用方向是：简单状态用 `atomic`，复合状态和 map/slice 用锁，异步队列和限流用 channel，启动/关闭一次性动作使用 `sync.Once`，批量并发等待用 `WaitGroup`，配置热更新用 `atomic.Value`。

### atomic

适合：

- 连接状态、服务状态、ping 时间、轻量计数器。
- 玩家请求防重入，例如 `UserInfo.IsReq/ResetReq`、登录请求防重复。
- “只有一个 goroutine 能成功进入”的 CAS 闸门。

注意：

- `atomic` 只适合单字段状态，不能保护多个字段之间的一致性。
- 同一个字段不要混用普通读写和 atomic 读写。当前 `ResetReq()` 这类方法直接写 `u.isReq[i] = 0`，从严格数据竞争角度更建议统一改成 `atomic.StoreUint32`。
- CAS 防重入必须成对 `defer Reset...`，否则错误返回或 panic 后会把玩家卡在“请求中”。
- `atomic.Value` 适合读多写少的整体替换，例如配置表热更新；存进去的 map/slice 应视作只读，不要 Load 后原地修改。

### Mutex / RWMutex

适合：

- 保护 map、slice、list、缓存、房间成员、session 集合等复合状态。
- `RWMutex` 适合读多写少的 registry/session/rank/cache 等结构。
- `Mutex` 适合 LRU、随机数源、singleflight calls 这类写操作频繁或读也会改变状态的对象。

注意：

- Go 的 `Mutex/RWMutex` 不可重入，已经持有同一把锁时不要再调用会加同一把锁的方法。
- 锁内尽量不要做 RPC、Redis、MySQL、网络 IO，避免锁被长时间持有。
- 读锁下不能返回可被外部修改的 map/slice 指针，除非明确调用方只读。
- 需要同时保护两个相关字段时，优先用一把锁维护不变量，不要拆成多个 atomic 字段。

### channel

适合：

- 异步队列、事件缓冲、写包队列、匹配超时队列。
- 并发限流，例如 `execChan`、`semaphore := make(chan struct{}, n)`。
- 生命周期通知，例如 `sigClose chan struct{}`。

注意：

- 非阻塞写 channel 如果 default 丢弃，必须有日志、指标或业务可接受说明；例如匹配超时队列满时直接丢弃，风险是超时补偿丢失。
- 只有拥有发送生命周期的一方关闭 channel；多方发送时不要随意 close。
- semaphore 获取后应优先 `defer release`，避免中途 return/panic 后泄漏容量。
- 无缓冲 channel 用作“锁”很容易写错；项目里 `battle_interface.go` 的 `indexLock := make(chan bool)` 随后立即 close，会让所有接收立即通过，并不能真正串行保护共享变量。

### WaitGroup

适合：

- 批量 RPC、批量战斗计算、批量读取队伍信息后等待汇总。

注意：

- `Add` 要在启动 goroutine 前执行，goroutine 内第一时间 `defer Done()`。
- `WaitGroup` 只负责等待，不负责保护共享数据。
- 当前项目里有一些历史写法在多个 goroutine 中直接 `append` 同一个 slice，或并发写同一个 map，例如队伍列表聚合、战斗本地批跑结果汇总。这类应加锁、用 channel 汇总，或按下标预分配后每个 goroutine 写独立下标。

### sync.Once

适合：

- 关闭连接、关闭管理器、释放资源等只允许执行一次的动作。

注意：

- `Once.Do` 的函数如果 panic，该 `Once` 也会被认为已经执行过，恢复/重试语义要提前设计。
- `close(channel)`、`conn.Close()`、`Free()` 这类资源释放推荐统一包在 `Once.Do` 中。

### sync.Map

适合：

- 高并发读、key 相对稳定，或者“只增/只删少”的共享表，例如部分 chat room/player 缓存。

注意：

- 需要维护多个字段一致性、需要复杂遍历快照、需要容量控制时，普通 map + `RWMutex` 更清晰。
- `sync.Map` 只能保护 map 结构本身，value 内部对象仍然需要自己的并发保护。

### 项目落地建议

1. 玩家写流程：优先保持“单玩家同一时刻一个写请求”，用现有 `IsReq/ResetReq` 这种 CAS 防重入；涉及多个字段状态一致性时不要只靠 atomic。
2. 房间/队伍/会话：继续用 `RWMutex` 保护 map/slice；读写函数边界清晰，避免锁内 RPC。
3. 异步队列：channel 要明确满载策略，是丢弃、阻塞还是降级，并补观测。
4. 批量并发：`WaitGroup` + 共享 slice/map 时必须额外同步；推荐预分配结果数组按 index 写，或用结果 channel 单线程汇总。
5. 配置热更新：`atomic.Value` 存不可变快照，reload 时构建新对象整体替换。
6. 新增并发代码建议跑 `go test -race` 覆盖相关包，尤其是战斗批跑、队伍列表、聊天/匹配异步队列。

## Q4: 从项目参考，这样写肯定有他的理由，分析一下

### 结论

这个判断是对的：从项目上下文看，`IsReq()` 用 `atomic.CompareAndSwapUint32`、`ResetReq()` 直接 `u.isReq[i] = 0`，大概率不是完全随手写错，而是一种历史形成的“轻量请求门闩”写法。

它的核心意图不是保护一组复杂数据，而是防止同一个玩家的同类请求重复进入。真正承担并发正确性的点在入口 CAS：谁能把 `0 -> 1`，谁就进入；其它并发请求看到已经是 `1` 就返回。退出时拥有者只需要把门闩放回 `0`。

### 项目里支持这个判断的证据

- `UserInfo.isReq` / `isOperReq` 是未导出的运行时字段，不是 Redis/MySQL 持久化字段，语义是“当前是否正在处理请求”。
- 业务调用非常广，常见模式是：

```go
if player.UserInfo.IsReq() {
    return
}
defer player.UserInfo.ResetReq()
```

- 项目里 `UserInfo.IsReq/ResetReq` 这类调用很多，说明它已经成为玩家请求防重入的局部惯例。
- `git blame` 看历史，`IsReq` 早在 2023 年已有，2024 年改成 CAS，同时 `ResetReq` 保持普通赋值；后续多个请求类型继续沿用这个模式，说明它被复制成了本文件的局部风格。

### 可能的设计理由

1. **它是“门闩”，不是业务数据**

`isReq[i]` 只有两个值：`0` 表示没人在处理，`1` 表示处理中。它不参与数值计算，也不要求记录中间状态，所以开发者很可能只把它当成一个轻量 flag。

2. **入口 CAS 才是关键路径**

防重入最重要的是抢入口：`CompareAndSwapUint32(&flag, 0, 1)` 保证只有一个请求能成功进入。`ResetReq()` 是持有者退出时无条件释放，不需要再判断旧值是否为 `1`。

3. **工程上通常能稳定运行**

在常见 64 位平台上，对齐的 `uint32` 普通写通常不会出现“写半截”的问题。也就是说，从实际运行角度，它大概率不会因为 torn write 直接坏掉。

4. **这类代码可能不以 race detector 为主要门禁**

游戏服全量跑 `-race` 成本较高，也容易暴露大量历史并发噪音。项目可能更关注线上可观测行为：是否挡住重复请求、是否会卡请求、是否影响性能。

5. **历史复制成本低**

同一文件里一组 `IsXxxReq/ResetXxxReq` 连续排列，新增请求类型时很容易照着已有模式复制。后面新增的 `LoginTeam/Fashion/ClearExpiredOperation/TeamHost/Operation` 等都沿用了这个风格。

### 但它仍然不是严格规范写法

从 Go 数据竞争定义看，同一个地址如果一边用 atomic CAS 读写，另一边用普通赋值写，只要可能并发发生，仍然属于混用访问。race detector 也可能报这个位置。

项目里也有反例：`invite_play_api.go`、`invite_bonus_api.go`、`battle.go`、`room_ghost.go` 里类似 CAS 门闩的释放使用了 `atomic.StoreUint32`。这说明“释放用 atomic.Store”在本项目也是存在的，而且更接近严格并发规范。

### 取舍建议

如果只是理解现状，不应直接定性为严重 bug。它更像是一个“当前业务场景下大概率可用，但并发规范不干净”的历史写法。

如果以后刚好要改这块，建议把 `ResetReq()`、`ResetLoginReq()`、`ResetOfflineRewardReq()`、`ResetLoginTeamReq()`、`ResetFashionReq()`、`ResetClearExpiredOperationReq()`、`ResetTeamHostReq()`、`ResetCheckOperationLoginEventReq()`、`ResetCheckOperationMonopolyEventReq()` 统一改为 `atomic.StoreUint32(&..., 0)`。这个改动行为等价、风险低，也能和项目里其它 CAS 门闩写法对齐。

更准确地说：原写法有它的工程理由，但新代码不建议继续复制；要修时属于“并发规范清理”，不是必须立刻打补丁的线上故障。

## Q5: atomic.Value 适合读多写少的整体替换，为什么 map/slice 要视作只读

### 结论

`atomic.Value` 保证的是“把某个值整体发布出去”和“读到某个完整版本”的原子性，不保证这个值内部的 map、slice、指针对象在后续被并发修改时仍然安全。

所以它适合配置表热更新：加载新配置时先构造一份完整的新 map/slice/config 对象，构造完成后一次 `Store` 替换；业务读的时候一次 `Load` 拿到当前版本，只读使用。

### 为什么 map/slice 要只读

1. **Load 出来的是同一个底层对象**

`atomic.Value.Load()` 不是深拷贝。里面存的是 map、slice、struct 指针时，多个 goroutine Load 到的通常是同一份底层对象。

如果一个 goroutine Load 后改了 map，另一个 goroutine 同时读这个 map，就会变成普通 map 的并发读写问题，轻则数据竞争，重则直接触发 `fatal error: concurrent map read and map write`。

2. **atomic 只保护“引用替换”，不保护“引用指向的内容”**

可以把 `atomic.Value` 理解为原子替换一个指针：

```go
old := value.Load()
value.Store(newSnapshot)
```

这里安全的是 `old/newSnapshot` 这次切换。至于 `old` 指向的 map 里面某个 key 被改，或者 slice 的底层数组被 append/赋值，`atomic.Value` 不会替你加锁。

3. **原地修改会破坏快照语义**

热更新希望读者看到的是“旧版本完整配置”或“新版本完整配置”，不能看到半更新状态。

正确方式是 copy-on-write：

```go
old := conf.Load().(map[uint32]*Item)
next := make(map[uint32]*Item, len(old)+1)
for k, v := range old {
    next[k] = v
}
next[newID] = newItem
conf.Store(next)
```

错误方式是 Load 后直接改旧 map：

```go
m := conf.Load().(map[uint32]*Item)
m[newID] = newItem // 并发读写风险，也会污染旧快照
```

4. **slice 也有同样问题**

slice 头部只是 `ptr/len/cap`，真正数据在底层数组。Load 出来的多个 slice 可能共享同一个底层数组。对元素赋值或 append 复用底层数组时，读者可能看到被修改中的内容。

### 项目里的对应写法

项目里大量配置表使用 `atomic.Value`，例如 `internal/data/**`、`internal/gaia/**` 这类配置加载代码：`init/reload` 时构造新配置，再 `Store`；业务方法 `Load` 后只读查询。

`internal/chat/rpc.go` 里还有明确的 `copy on write` 注释：更新公共频道缓存时先 Load 旧 map，复制成新 map，改新 map，再 Store 回去。这就是 `atomic.Value` 搭配 map 的正确模型。

### 使用注意

- `Store` 的值应保持同一个具体类型；第一次存 `map[uint32]*Item`，后面就不要改成 `*Config` 或其它类型。
- 不要 `Store(nil)`。
- `Load` 后如果需要给外部返回 map/slice，最好明确“调用方只读”，否则返回副本。
- 如果必须频繁局部修改，就不适合只用 `atomic.Value`，应改用 `RWMutex`、`sync.Map` 或 actor/单线程更新模型。

## Q6: chat_room.go 详细介绍聊天服的流程

### 总体结论

`internal/chat/chat_room.go` 不是聊天服的入口，而是自研聊天模式下“单个聊天室房间”的内存对象。它负责：

- 注册房间到 Redis；
- 订阅该房间对应的 NATS topic；
- 把收到的消息放入本地消息队列；
- 遍历本房间在线玩家并通过 Agent session 推送；
- 定时清理失效玩家、上报房间人数。

完整链路要放在 `Application -> service/chat -> ChatRoomMgr -> ChatRoom -> ChatPlayer/Agent` 里看。

### 启动流程

1. `cmd/zchat/main.go` 启动 Chat 进程，进入 `internal/app/chat/app.go`。
2. `Application.OnInit()` 做基础初始化：
   - 加载配置表；
   - 初始化 Redis；
   - 初始化 `ChatConvRepo`、`ChatChanRepo`、`ChatPoolRepo`；
   - 初始化 session；
   - 注册 discovery；
   - 如果走自研聊天模式，则连接 NATS，并写入 `ChatPoolRepo.NatsClient`；
   - 启动 `reportChatServerStatus()`，周期性把 ChatServer 状态写 Redis。
3. `newGRPCServer()` 注册 `ChatService`，其中 `JoinChatRoom` / `LeaveChatRoom` 会进入 `internal/chat` 包。
4. `newChatServiceRouter()` 注册前端协议路由，处理登录聊天服、心跳等协议。

### 加入房间流程

客户端并不是直接调用 `chat_room.go`。大致流程是：

1. 客户端发 `JOIN_CHAT_ROOM` 到业务层。
2. `internal/service/chat/chat.go` 校验玩家、区服、公会、房间是否存在。
3. 如果客户端没有指定房间，业务层通过 `ChatPoolRepo.GetMostPlayerNumChatRoom()` 选择一个合适房间。
4. 业务层调用 `client.RpcJoinChatRoom(...)`，RPC 到目标 Chat 服。
5. Chat 服的 `grpcChatService.JoinChatRoom()` 调用 `internal/chat.JoinChatRoom(...)`。
6. `JoinChatRoom` 创建或刷新 `ChatPlayer`：
   - 新玩家：加入 `chatPlayerMgrs`，写 `ChatPlayerRegInfo` 到 Redis，并启动个人私聊订阅；
   - 旧玩家：先从旧公共/本服/公会房间移除，再刷新 `agentID/connID/guildID/nation/zone`。
7. 根据 `joinType` 选择房间管理器：
   - 公共频道：`cP[mergZoneID]`；
   - 本服频道：`cL[roleZoneID]`；
   - 大区频道：`ChatRoomMgrsRegion`；
   - 公会频道：`cG[mergZoneID]`，房间 ID 通常就是 `guildID`。
8. `ChatRoomMgr.GetRoom()` 找不到房间时调用 `createRoom()`，最终进入 `newChatRoom()`。

### newChatRoom 做了什么

`newChatRoom(roomID, kind, zoneID, nation)` 会创建一个 `ChatRoom`：

- `roomID`：频道 ID；
- `kind`：频道类型，例如公共、本服、大区、公会；
- `roomKey`：由 `nation/zone/kind` 拼出来；
- `playersV2 sync.Map`：当前房间在线玩家；
- `playersNum int64`：当前房间人数计数；
- `msgsChan chan string`：房间本地消息队列，容量 20；
- `disablePlayers chan uint32`：待清理玩家队列；
- `semaphore chan struct{}`：广播 goroutine 并发上限；
- `subs *nats.Subscription`：房间 NATS 订阅。

创建后它会：

1. 调 `ChatPoolRepo.RegChatRoom()` 把房间注册到 Redis；
2. 启动 `startSubHandle()` 订阅 NATS topic；
3. 启动 `startMsgLoop()` 消费本地消息队列；
4. 启动 `RoomTicker()` 定时清理和上报人数。

### 消息发送流程

以自研聊天模式为例：

1. 玩家发言进入 `internal/service/chat/chat.go`。
2. 业务层做等级、频率、脏字、信用分、黑名单、任务、历史等处理。
3. 根据消息类型构造 `roomkey` 和 topic：
   - 公共频道：`MakeChatRoomKeyForPublic(chatNation, mergZoneID)`；
   - 本服频道：`MakeChatRoomKeyForPublic(chatNation, roleZoneID)`；
   - 大区频道：`MakeChatRoomKeyForRegion(region)`；
   - 公会频道：`MakeChatRoomKeyForGuild(mergZoneID)`；
   - 私聊：`SubTypePersonal:MsgTypePrivate:roleID`。
4. 调 `ChatPoolRepo.NatsPublish(topic, msg)` 发布到 NATS。
5. 非私聊频道还会调用 `ChatPoolRepo.AddChatMsg()` 把聊天历史写 Redis list。

### ChatRoom 收到消息后怎么广播

`ChatRoom.startSubHandle()` 订阅 topic：

```go
subj := model.MakeChatSubTopic(cr.roomKey, cr.kind, cr.roomID)
cr.subs, err = ChatPoolRepo.NatsClient.SubscribeSync(subj)
```

循环 `NextMsg(time.Minute)` 收到 NATS 消息后，不直接广播，而是调用：

```go
cr.SendMsg(string(m.Data))
```

`SendMsg()` 把消息放入 `msgsChan`。如果队列满了，会记录 `SendMsg overflow`，这条消息不会阻塞等待。

`startMsgLoop()` 从 `msgsChan` 取消息，每条消息启动一个 worker，并用 `semaphore` 控制并发广播数量。

`broadcastMsgChan()` 遍历 `playersV2`：

- 如果玩家超过 60 秒没有心跳，写入 `disablePlayers` 等待清理；
- 否则调用 `ChatPlayer.sendMsg()` 推给玩家；
- 推送失败也写入 `disablePlayers`。

最终 `ChatPlayer.sendMsg()` 构造 `PushTeamChat`，通过 `session.GetAgentSession(cp.agentID)` 找到 Agent session，再按 `connID` 写回客户端连接。

### 心跳与清理

客户端心跳进入 Chat 服后，会调用 `HeartChatPlayerForChatServer()`：

- 校验 `agentID/connID` 是否与当前玩家一致；
- 更新 `ChatPlayer.heartTM`；
- 写 `ChatPlayerRegInfo` 到 Redis。

`RoomTicker()` 每 10 秒处理一次：

- 消费 `disablePlayers` 队列；
- 从房间 `playersV2` 删除玩家；
- 从 `chatPlayerMgrs` 删除玩家；
- 调 `RegChatRoomPlayersNum()` 上报当前房间人数到 Redis。

### 房间续约与服务状态

房间创建后，`ChatRoomMgr.createRoom()` 会通过 CAS 确保只启动一次 `startRenewAllChatRoomsLease()`。这个定时任务每 20 秒执行 `performTask()`，收集当前管理器下所有房间的 Redis key，然后调用 `RenewChatRoomLease()` 续约。

Chat 进程自身还会通过 `reportChatServerStatus()` 每分钟注册 `ChatServerInfo`，用于其它服务发现可用 Chat 节点。

### 关键存储与 topic

- 房间注册：`ChatRoomRegInfo`
- 房间到服务列表：`ChatServer2Rooms`
- 房间人数：`GetChatRoomPlayerNums(roomKey, roomID, roomType)`
- 聊天历史：`GetChatMsgHistoryKey(roomKey, roomID)`
- 玩家连接注册：`ChatPlayerRegInfo`
- NATS topic：`MakeChatSubTopic(roomkey, msgType, roomid)`，格式类似 `roomkey:msgType:roomid`

### 注意点

- `chat_room.go` 只覆盖自研聊天模式；如果 `Base.GetChatVersionSiwtch()` 开启，消息可能走 LeanCloud 分支，不走这套 NATS 房间广播。
- `msgsChan` 满时会丢消息并打日志，不会阻塞。
- `playersV2` 用 `sync.Map`，人数用 `atomic.AddInt64` 维护，避免每次 Range 统计。
- `msgs []string` 在 `broadcastMsgChan()` 中维护最近消息，但当前 `GetRoomMsgPb()` 没有返回 `Msgs`，主要历史还是 Redis list。
- `RoomTicker()` 负责清失效玩家，但 `heartTM`、`ChatPlayer` 多字段刷新没有显式锁，属于当前项目里偏实用主义的并发写法。

## Q7: NATS 是什么

### 结论

NATS 是一个轻量级消息中间件，主要提供发布/订阅模型。可以把它理解成一个“消息广播总线”：发送方把消息发布到某个 topic，所有订阅这个 topic 的消费者都能收到消息。

在本项目里，NATS 主要用于自研聊天系统的实时消息分发。它不负责长期存储聊天历史，聊天历史仍然落 Redis。

### 通用概念

NATS 的核心概念很少：

- `Connect`：服务进程连接 NATS server。
- `Publish(subject, data)`：往某个 subject/topic 发消息。
- `Subscribe(subject)`：订阅某个 subject/topic。
- `NextMsg()`：订阅者从 topic 收消息。

它适合低延迟、轻量级的服务间事件通知，例如聊天消息、在线状态、广播事件等。

### 本项目里 NATS 的角色

项目配置里 `chat_nats_host` 表示 NATS 地址，注释是“使用 nats 进行聊天消息队列处理”。

Chat/Game 启动时，如果走自研聊天模式，会：

```go
nc, err := nats.Connect(ncHost, nats.MaxReconnects(3))
repo.GetChatPoolRepo().SetDefaultNatsClient(nc)
```

也就是把 NATS 连接保存到 `ChatPoolRepo.NatsClient`。

### 聊天消息怎么用 NATS

1. 玩家发聊天消息。
2. 业务层根据消息类型算出 `roomkey` 和 `roomID`。
3. 拼出 NATS topic：

```go
model.MakeChatSubTopic(roomkey, msgType, roomID)
```

格式类似：

```text
roomkey:msgType:roomID
```

4. 发送方调用：

```go
ChatPoolRepo.NatsPublish(topic, msgBytes)
```

5. 对应 `ChatRoom` 创建时已经订阅了这个 topic：

```go
cr.subs = NatsClient.SubscribeSync(topic)
```

6. `ChatRoom` 收到消息后放入自己的 `msgsChan`，再广播给房间内的在线玩家。

### 为什么聊天服要用 NATS

因为同一个聊天房间的在线玩家可能分布在不同 Chat 节点上。发消息的业务方不应该关心“哪些玩家在哪台 Chat 服”。它只要往房间 topic 发布一条消息，所有订阅这个房间 topic 的 ChatRoom 都能收到，然后各自推给自己节点上的玩家。

这样可以解耦：

- 发送方只负责发布消息；
- ChatRoom 只负责订阅和广播；
- Redis 负责房间注册、人数、历史消息；
- Agent 负责最终连接到客户端。

### 和 Redis 的区别

- NATS：偏实时消息分发，消息来了就推，默认不是长期存储。
- Redis：偏状态和历史存储，例如房间注册、房间人数、聊天历史、玩家连接信息。

本项目里发一条公共聊天时，通常是：

- NATS 发布实时消息；
- Redis list 记录聊天历史；
- ChatRoom 收到 NATS 消息后推给在线玩家。

### 注意点

- 如果 NATS 没连接成功，自研聊天模式下 Chat/Game 启动会失败。
- `NatsPublish` 当前只判断 `NatsClient != nil`，没有处理 `Publish` 返回错误，属于简单封装。
- NATS topic 是聊天路由契约，`MakeChatSubTopic(roomkey, msgType, roomID)` 的拼法必须和订阅端保持一致。
- 它不替代 Redis，也不替代 Agent；它只解决“消息从发送方广播到订阅房间的 Chat 节点”这一段。

## Q8: type ChatLimitItem []uint32 和 IsOnlineFull 这种写法底层原理是什么

### 结论

`type ChatLimitItem []uint32` 的意思是：基于 `[]uint32` 定义一个新的命名类型。它的底层数据结构仍然是 slice，但因为它有了自己的类型名，所以可以给它挂方法。

所以：

```go
type ChatLimitItem []uint32

func (item ChatLimitItem) IsOnlineFull(num uint32) bool {
    return num >= item[3]
}
```

本质上是把“聊天室人数阈值配置”这个普通数组，包装成一个带业务方法的类型。

### 底层仍然是 slice

`ChatLimitItem` 的底层类型是 `[]uint32`。slice 底层可以理解为三元组：

```text
ptr + len + cap
```

也就是：

- `ptr` 指向底层数组；
- `len` 是当前长度；
- `cap` 是容量。

方法接收者 `item ChatLimitItem` 是值传递，但复制的只是 slice 头部，不会深拷贝底层数组。因此读取 `item[3]` 和读取原始 `[]uint32` 的第 4 个元素是同一回事。

### 为什么能调用 item.IsOnlineFull()

Go 没有类继承，但可以给“本包定义的命名类型”声明方法。

```go
func (item ChatLimitItem) IsOnlineFull(num uint32) bool
```

这里 `(item ChatLimitItem)` 叫方法接收者。它让 `ChatLimitItem` 类型拥有 `IsOnlineFull` 方法。

调用：

```go
limit.IsOnlineFull(1500)
```

可以粗略理解成：

```go
ChatLimitItem.IsOnlineFull(limit, 1500)
```

只是 Go 语法上写成了对象点方法的形式。

### 本项目里的配置来源

配置结构里字段是：

```go
ChanMemLimit []uint32
```

配置文件 `chat_params.json` 里类似：

```json
"ChanMemLimit": [100, 550, 1300, 1500]
```

读取配置后，`GetChatChanMemLimit()` 返回：

```go
func GetChatChanMemLimit() ChatLimitItem {
    c := defaultChatConf.Load().(*ChatConfig)
    return c.ChanMemLimit
}
```

虽然 `c.ChanMemLimit` 是 `[]uint32`，但它可以赋给返回类型 `ChatLimitItem`，因为二者底层类型相同，而且源类型 `[]uint32` 不是命名类型。

### 这样写的好处

不用在业务代码里到处写：

```go
limit := data.GetChatChanMemLimit()
if online >= limit[3] {
    // 满员
}
```

而是写成：

```go
limit := data.GetChatChanMemLimit()
if limit.IsOnlineFull(online) {
    // 满员
}
```

好处是：

- `item[3]` 这种魔法下标被封装起来；
- 业务代码更像在表达“是否满员”，而不是“比较第 4 个配置值”；
- 后续如果阈值判断规则变化，优先改 `ChatLimitItem` 方法，不用全局找下标判断。

### 注意点

- 这个类型没有额外字段，运行时成本和 `[]uint32` 基本一样。
- `item[3]` 依赖配置长度至少为 4，否则会 panic。
- 方法是值接收者，但 slice 底层数组共享；如果方法里修改 `item[i]`，会影响原底层数据。本项目这些方法只读，所以风险较低。
- 当前 `GetOnlineStatus()` 和 `IsOnlineFull()` 使用的阈值不完全一样：`GetOnlineStatus()` 中 `item[2]` 就返回 Full 状态，而 `IsOnlineFull()` 用的是 `item[3]`。结合配置 `[100, 550, 1300, 1500]` 看，更像是“显示状态”和“禁止进入”的两个不同阈值。

## Q9: PlayerGetSessionAddress 详细介绍

### 原代码

```go
func PlayerGetSessionAddress(roleID uint32) (*tunnel.Addr, bool) {
    sessionMgr := session.GetSessionManager
    roleSession, isFound := sessionMgr().GetSession(roleID)
    if isFound {
        return roleSession.GetAddress(), true
    }
    return nil, false
}
```

### 结论

这个函数的作用是：根据玩家 `roleID` 查当前 Game 服内存里的 TCP 会话，并返回这个玩家连接所在的 `AgentID + ConnID`。

返回的 `*tunnel.Addr` 是一个很小的地址结构：

```go
type Addr struct {
    AgentID uint32
    ConnID  uint32
}
```

它不是 IP 地址，而是项目内部的“客户端连接路由地址”：

- `AgentID`：玩家连在哪个 Agent 网关；
- `ConnID`：玩家在该 Agent 上是哪条客户端连接。

有了这个地址，Game/Chat/玩法服就能把消息准确推回这个玩家的客户端连接。

### 执行流程

1. 取 SessionManager：

```go
sessionMgr := session.GetSessionManager
```

这里没有括号，所以不是立即调用，而是把函数本身赋值给变量。下一行：

```go
sessionMgr()
```

才真正调用 `session.GetSessionManager()`。

这两行等价于：

```go
roleSession, isFound := session.GetSessionManager().GetSession(roleID)
```

2. 按 `roleID` 查询 Session：

```go
roleSession, isFound := sessionMgr().GetSession(roleID)
```

`SessionManager.GetSession()` 会根据 `roleID & sessionGroupMask` 找到分片的 `SessionGroup`，加读锁后从：

```go
map[uint32]*Session
```

里取玩家会话。

3. 找到则返回连接地址：

```go
return roleSession.GetAddress(), true
```

`roleSession` 是 `internal/session/game.Session`，它内嵌了 `*tunnel.UserSession`：

```go
type Session struct {
    *tunnel.UserSession
    RoleID uint32
    ...
}
```

所以 `roleSession.GetAddress()` 实际调用的是 `tunnel.UserSession.GetAddress()`，返回：

```go
&Addr{AgentID: us.agentID, ConnID: us.connID}
```

4. 找不到则返回：

```go
return nil, false
```

调用方必须检查第二个返回值，不能直接使用 `sessAddr.AgentID`。

### 这个地址什么时候写入

玩家登录/鉴权成功后，会调用 `SessionManager.AddSession(...)`。它会：

1. 根据 `agentID` 找到对应的 Agent session；
2. 根据 `connID` 找到 Agent 下的用户连接；
3. 调 `userSess.SetRoleID(roleID)`；
4. 把 `roleID -> Session` 放入 Game 服内存的 session map。

所以 `PlayerGetSessionAddress()` 查的是“当前 Game 服内存中是否存在这个玩家在线连接”。

### 本项目里为什么常用它

很多玩法服或跨服 RPC 需要知道玩家连接地址。例如：

- 加入聊天室时，如果客户端没有带 `AgentID/ConnID`，就用当前 session 地址补齐；
- 世界 Boss、组队副本、活动玩法创建队伍时，需要把玩家的连接地址传给队伍/战斗/跨服服务；
- 其它服务后续才能通过 `AgentID + ConnID` 精准推送给这个玩家。

典型用法：

```go
sessAddr, isFound := service.PlayerGetSessionAddress(msg.Roleid)
if !isFound {
    return BadParam
}
msg.AgentID, msg.ConnID = sessAddr.AgentID, sessAddr.ConnID
```

### 和在线状态的关系

这个函数只判断“session map 里有没有这个 roleID”，并不调用 `roleSession.IsOnline()`。

也就是说：

- `isFound == true`：说明内存里有这个玩家 session；
- 但不等价于心跳一定正常；
- 真正严格的 TCP 在线判断要结合 `Session.IsOnline()`，它会检查连接未关闭且心跳未超时。

所以这个函数更准确的语义是“获取玩家当前连接路由地址”，不是“强在线校验”。

### 注意点

- `session.GetSessionManager` 先赋给变量再调用，语义上没问题，但可读性不如直接写 `session.GetSessionManager().GetSession(roleID)`。
- 返回的是新的 `Addr` 指针，只包含两个 uint32，不会暴露整个 Session。
- 找不到 session 时返回 `nil, false`，调用方必须判断 `isFound`。
- 如果 session 存在但已经心跳超时，这个函数仍可能返回地址；是否需要额外 `IsOnline()` 要看业务场景。

## Q10: NATS startSubHandle 详细介绍

### 结论

`startSubHandle` 是自研聊天模式下的 NATS 消费循环。它的职责是：为某个房间或某个玩家订阅一个 NATS topic，持续从 topic 收消息，然后转交给本进程内存里的房间/玩家推送逻辑。

项目里有两个同名方法：

- `ChatRoom.startSubHandle()`：订阅房间 topic，收到消息后广播给房间里的所有本节点玩家。
- `ChatPlayer.startSubHandle()`：订阅个人私聊 topic，收到消息后只推给这个玩家。

### 房间订阅流程

`newChatRoom()` 创建房间时会启动：

```go
go func() {
    cr.startSubHandle()
}()
```

房间订阅的 topic 这样生成：

```go
subj := model.MakeChatSubTopic(cr.roomKey, cr.kind, cr.roomID)
```

`MakeChatSubTopic` 的格式是：

```go
fmt.Sprintf("%s:%d:%d", roomkey, msgType, roomid)
```

也就是：

```text
roomkey:msgType:roomID
```

例如公共频道、本服频道、大区频道、公会频道都会按这个规则生成 topic。

### 订阅 NATS

核心代码：

```go
cr.subs, err = dbrepo.GetChatPoolRepo().NatsClient.SubscribeSync(subj)
```

含义是：当前 ChatRoom 在 NATS 上同步订阅 `subj` 这个 topic。之后任何地方往同一个 topic `Publish` 消息，这个订阅都能收到。

发布端对应代码类似：

```go
ChatPoolRepo.NatsPublish(model.MakeChatSubTopic(roomkey, msgType, roomID), msgBytes)
```

所以发布和订阅能对上，靠的就是同一个 `MakeChatSubTopic` 规则。

### 关闭订阅

订阅成功后会启动一个 goroutine：

```go
go func() {
    <-cr.done
    cr.subs.Unsubscribe()
}()
```

它等待 `cr.done` 收到信号，然后取消 NATS 订阅。

不过当前代码里房间关闭链路不完整：`cr.done` 和 `cr.isClose` 有字段，但没有明显的统一关闭方法。也就是说，这更像是预留了关闭机制。

### 消费循环

房间订阅进入循环：

```go
for !cr.isClose {
    m, err := cr.subs.NextMsg(time.Minute)
    ...
    cr.SendMsg(string(m.Data))
}
```

`NextMsg(time.Minute)` 表示最多等 1 分钟收一条消息：

- 如果超时 `nats.ErrTimeout`，说明这 1 分钟没有新消息，直接 `continue`，继续等下一轮。
- 如果是其它错误，记录日志后继续。
- 如果收到消息，把消息内容转成 string，然后调用 `cr.SendMsg(...)`。

这里注意：收到 NATS 消息后并不直接遍历玩家广播，而是先进入本房间的 `msgsChan`。

### 为什么先 SendMsg 到 msgsChan

`SendMsg()` 做的是非阻塞入队：

```go
select {
case cr.msgsChan <- msg:
default:
    log overflow
}
```

这样 NATS 消费循环不会被广播耗时拖住。真正广播由 `startMsgLoop()` 消费 `msgsChan`：

```go
for msg := range cr.msgsChan {
    cr.semaphore <- struct{}{}
    go cr.workerbroadcastMsgChan(msg, cr.semaphore)
}
```

这里 `semaphore` 控制并发广播数量，避免同时启动太多 goroutine。

整体是两级解耦：

```text
NATS topic
  -> startSubHandle 收消息
  -> SendMsg 放入 msgsChan
  -> startMsgLoop 消费队列
  -> broadcastMsgChan 遍历房间玩家
  -> ChatPlayer.sendMsg 写回 Agent
```

### 私聊订阅流程

`ChatPlayer` 创建时也会启动：

```go
go cp.startSubHandle()
```

它订阅的 topic 是：

```go
subj := model.MakeChatSubTopic(data.SubTypePersonal, data.MsgTypePrivate, cp.roleID)
```

格式同样是三段：

```text
personal:private:roleID
```

发送私聊时，业务层会分别往发送者和接收者的个人 topic 发布：

```go
NatsPublish(personal/private/sendRoleID, msg)
NatsPublish(personal/private/receiverRoleID, msg)
```

所以在线玩家自己的 `ChatPlayer.startSubHandle()` 收到后，直接：

```go
cp.sendMsg(data.MsgTypePrivate, cp.roomID, string(m.Data))
```

私聊不需要房间广播，所以不走 `msgsChan`。

### 异常保护

两个 `startSubHandle` 开头都有 `defer recover`：

```go
defer func() {
    if r := recover(); r != nil {
        log stack
    }
}()
```

目的是避免订阅 goroutine panic 后把进程带崩，同时打印堆栈方便排查。

### 注意点

- `SubscribeSync` 依赖 `ChatPoolRepo.NatsClient` 已初始化；如果 NATS 没连上，自研聊天启动阶段就会失败。
- 房间 `NextMsg` 超时时间是 1 分钟；个人私聊是 30 分钟。
- `SendMsg()` 队列满时会丢消息并打 `SendMsg overflow` 日志。
- `NatsPublish` 当前没有处理 `Publish` 返回错误，只做了 `NatsClient != nil` 判断。
- 订阅 goroutine 是每个房间/玩家一个，房间和私聊订阅数量会随在线玩家和房间数增长。
- 当前关闭字段 `done/isClose/exitChan` 有使用，但房间维度的完整关闭链路不明显；玩家移除时会通过 `exitChan` 取消个人订阅。

## Q11: agentID 和 connID 是什么，为什么要用两个值

### 结论

`agentID + connID` 是项目内部定位一个客户端连接的两级地址。

- `agentID`：定位玩家连接在哪个 Agent 网关上。
- `connID`：定位玩家在这个 Agent 网关里的哪一条客户端连接。

两者合起来才表示一个具体客户端连接：

```go
type Addr struct {
    AgentID uint32
    ConnID  uint32
}
```

可以类比成：

```text
agentID = 哪栋楼
connID  = 这栋楼里的哪个房间
```

只知道房间号，不知道哪栋楼，找不到人；只知道哪栋楼，不知道房间号，也无法投递到具体玩家。

### agentID 是什么

`agentID` 是 Agent 网关的编号。

客户端不是直接连 Game/Chat 业务服，而是先连 Agent。业务服和 Agent 之间通过 tunnel 保持连接。每个 Agent 和后端建立连接并完成注册时，会通过 register-ack 告诉后端自己的编号：

```go
agentID := uint32(inPacket.GetBackendID())
s.SetID(agentID)
```

后端会把这个 Agent 连接保存成 `AgentSession`，之后可以通过：

```go
AgentManager.GetSession(agentID)
```

找到对应 Agent。

### connID 是什么

`connID` 是某个 Agent 内部的客户端连接编号。

同一个 Agent 上会挂很多客户端连接。Agent 转发客户端包给 Game/Chat 时，包里会带 `connID`。后端收到消息后，会用：

```go
agent.NewUserSession(pack.GetConnID(), ...)
```

在这个 AgentSession 下创建或获取对应的 `UserSession`。

`UserSession` 里会保存：

```go
agentID: agent.GetID()
connID:  connID
```

所以 `UserSession.GetAddress()` 返回的就是 `{AgentID, ConnID}`。

### 为什么不能只用 connID

因为 `connID` 只在单个 Agent 内有意义。

假设有两个 Agent：

```text
Agent 1: connID=1001
Agent 2: connID=1001
```

这两个连接完全可能同时存在。如果只传 `connID=1001`，后端不知道应该往 Agent 1 还是 Agent 2 发。

所以必须先用 `agentID` 定位 Agent，再用 `connID` 定位该 Agent 下的客户端连接。

### 为什么不能只用 agentID

因为一个 Agent 上有很多客户端连接。

只知道 `agentID=3`，只能找到第 3 个 Agent 网关，但不知道消息要发给这个 Agent 上的哪个玩家连接。

所以还需要 `connID`。

### 写回客户端时怎么用

推消息时通常是：

```go
agentSess := agentManager.GetSession(agentID)
outPacket.SetConnID(connID)
agentSess.Write(outPacket)
```

含义是：

1. `agentID` 找到对应的 Agent tunnel 连接；
2. 把 `connID` 写进 packet；
3. Agent 收到 packet 后，根据 `connID` 找到自己的客户端连接；
4. 最终把数据发给对应玩家。

### 登录时怎么绑定 roleID

玩家登录成功后，Game 服会调用：

```go
SessionManager.AddSession(roleID, agentID, connID, ...)
```

内部流程是：

1. `smr.sessmgr.GetSession(agentID)` 找 Agent；
2. `agentSess.GetUserSession(connID)` 找这个 Agent 下的用户连接；
3. `userSess.SetRoleID(roleID)` 把连接绑定到玩家；
4. Game 服内存保存 `roleID -> Session`。

之后 `PlayerGetSessionAddress(roleID)` 才能从 `roleID` 反查出 `{AgentID, ConnID}`。

### 本项目为什么需要这套两级地址

这是因为接入层和业务层解耦了：

```text
客户端 <-> Agent <-> Game/Chat/其它业务服
```

业务服不直接持有客户端 TCP 连接，只持有到 Agent 的 tunnel 连接。所以业务服要给玩家推消息时，必须告诉 Agent：

- 这条消息发给哪个 Agent；
- 这个 Agent 再转给哪条客户端连接。

因此 `agentID + connID` 就是跨服务推送玩家消息的最小路由信息。

### 注意点

- `agentID/connID` 表示连接地址，不等于玩家 ID。
- 玩家重连后，`connID` 可能变化，需要刷新 session。
- 如果 Agent 断开，旧的 `agentID/connID` 即使还在业务数据里，也可能已经不可用。
- 需要严格判断在线时，不能只看地址是否存在，还要结合 session 是否关闭、心跳是否超时。

## Q12: 如果玩家跟多个人发起私聊，是怎么区分的

### 结论

本项目私聊不是按“会话 ID”区分的，而是按双方 `roleID` 区分。

实时投递时，NATS topic 按“收件玩家 roleID”订阅；历史/列表保存时，`ChatInfo.PrivRoles` 按“对方 roleID”分桶。

所以玩家 A 同时和 B、C 私聊时：

- A 的私聊列表里会有 `ChatRole{RoleID: B}`；
- A 的私聊列表里也会有 `ChatRole{RoleID: C}`；
- B 的私聊列表里会有 `ChatRole{RoleID: A}`；
- C 的私聊列表里会有 `ChatRole{RoleID: A}`。

每个 `ChatRole` 内部都有自己的 `Msgs []string`，因此不会混在一起。

### 实时投递怎么区分

私聊发送时，消息入口里有：

```go
msg.Roleid   // 发送者
msg.Receiver // 接收者
```

当 `msg.MsgType == data.MsgTypePrivate` 时，服务端会分别往发送者和接收者的个人 topic 发布一份消息：

```go
NatsPublish(MakeChatSubTopic(SubTypePersonal, MsgTypePrivate, senderRoleID), msg)
NatsPublish(MakeChatSubTopic(SubTypePersonal, MsgTypePrivate, receiverRoleID), msg)
```

个人 topic 的格式仍然是：

```text
personal:private:roleID
```

这表示：每个在线玩家都有一个自己的私聊收件箱 topic。

如果 A 给 B 发消息：

```text
personal:private:A
personal:private:B
```

如果 C 给 A 发消息：

```text
personal:private:C
personal:private:A
```

A 会在自己的 `personal:private:A` topic 上收到来自不同人的私聊消息。区分是谁发来的，靠消息体里的 `RoleID` 和 `Receiver`。

### 消息体怎么区分双方

实时消息体是 `MsgContent` JSON，里面有：

```go
RoleID   // 发送者 roleID
Receiver // 接收者 roleID
Content  // 内容
Timestamp
Name/Avatar/Frame/Badge 等展示信息
```

客户端收到私聊推送后，可以根据：

- `RoleID == 自己`：这是自己发出的消息；
- `Receiver == 自己`：这是别人发给自己的消息；
- 会话对方 ID：
  - 如果 `RoleID == 自己`，对方是 `Receiver`；
  - 如果 `Receiver == 自己`，对方是 `RoleID`。

这样就能把消息归到“我和 B”或“我和 C”的聊天窗口。

### 历史记录怎么保存

玩家聊天数据结构里有：

```go
type ChatInfo struct {
    PrivRoles []*ChatRole // 私聊数据
}

type ChatRole struct {
    RoleID uint32   // 对方 roleID
    Msgs   []string // 我和这个人的消息列表
    Job/Sex/Avatar/Frame/Name/Badge
    IsRead uint32
}
```

也就是说，`PrivRoles` 是一个“私聊对象列表”。每一项代表一个对话对象。

### AddPrivRoleData 的分组逻辑

保存私聊历史时调用：

```go
AddPrivRoleData(roleID, ..., msg, name)
```

这里的 `roleID` 不是自己，而是“对方 roleID”。

函数内部会遍历 `ci.PrivRoles`：

```go
for i, v := range ci.PrivRoles {
    if v.RoleID == roleID {
        chatRole = v
        ci.PrivRoles = append(ci.PrivRoles[:i], ci.PrivRoles[i+1:]...)
        break
    }
}
```

如果已经有这个私聊对象，就拿出来；如果没有，就创建一个新的：

```go
chatRole = &ChatRole{RoleID: roleID}
```

然后把本条消息追加到这个对象自己的 `Msgs`：

```go
chatRole.Msgs = append(chatRole.Msgs, msg)
chatRole.Msgs = GetMsgs(chatRole.Msgs)
```

最后再把这个 `chatRole` append 回 `PrivRoles` 尾部，相当于把最近聊天对象放到列表末尾。

### 发送者和接收者各保存一份

A 给 B 发私聊时，A 侧会执行：

```go
player.ChatInfo.AddPrivRoleData(msg.Receiver, ..., chatMsgHis, targetName)
```

也就是 A 的列表里保存“对方是 B”的记录。

同时服务端会 RPC 到 B 所在 Game 服：

```go
RpcGameChatPrivateRole(receiveID=B, sendID=A, msg=...)
```

B 所在 Game 服处理时：

```go
player.ChatInfo.AddPrivRoleData(msg.SendID, ..., msg.Msg, senderName)
```

也就是 B 的列表里保存“对方是 A”的记录。

这样两边各自按“对方 roleID”维护自己的私聊历史。

### 举例

假设 A=1001，B=2002，C=3003。

A 给 B 发消息：

```text
A.PrivRoles[RoleID=2002].Msgs append msg
B.PrivRoles[RoleID=1001].Msgs append msg
```

A 给 C 发消息：

```text
A.PrivRoles[RoleID=3003].Msgs append msg
C.PrivRoles[RoleID=1001].Msgs append msg
```

A 登录/进房时，服务端会把 `PrivRoles` 返回给客户端：

```proto
repeated ChatRole privRoles = 9;
```

客户端看到两个 `ChatRole`：

```text
RoleID=2002 -> A 和 B 的消息
RoleID=3003 -> A 和 C 的消息
```

因此可以显示成两个不同私聊窗口。

### 注意点

- NATS 个人 topic 只负责“消息投递到某个玩家”，不负责区分这个玩家和谁聊天。
- 真正区分聊天对象的是消息体里的 `RoleID/Receiver`，以及持久化数据里的 `ChatRole.RoleID`。
- 当前实现不是全局会话 ID 模型，而是双方 roleID 模型。
- `PrivRoles` 有数量上限，超过 `data.GetPrivateChatRole()` 会移除最早的私聊对象。
- 每个 `ChatRole.Msgs` 也有条数上限，通过 `GetMsgs()` 裁剪。

## Q13: 私聊同时 Publish 到两个人的 Topic 后，是怎么发送给玩家的

### 问题代码

```go
dbrepo.GetChatPoolRepo().NatsPublish(model.MakeChatSubTopic(data.SubTypePersonal,
    msg.MsgType,
    player.UserInfo.RoleID),
    bytChatContent)
dbrepo.GetChatPoolRepo().NatsPublish(model.MakeChatSubTopic(data.SubTypePersonal,
    msg.MsgType,
    msg.Receiver),
    bytChatContent)
```

### 结论

这两次 `Publish` 的目的，是让发送者和接收者各收到一份私聊推送。

它不是“同一个玩家收到两次”，而是：

- 第一条发到发送者自己的个人 topic，用于发送者客户端回显；
- 第二条发到接收者自己的个人 topic，用于接收者客户端收到新消息。

每个在线玩家的 `ChatPlayer` 只订阅自己的个人 topic。

### 个人 topic 是怎么订阅的

玩家进入聊天服并创建 `ChatPlayer` 时，会启动：

```go
go cp.startSubHandle()
```

私聊订阅 topic：

```go
subj := model.MakeChatSubTopic(data.SubTypePersonal, data.MsgTypePrivate, cp.roleID)
```

格式类似：

```text
personal:private:roleID
```

所以：

- A 玩家订阅 `personal:private:A`；
- B 玩家订阅 `personal:private:B`；
- C 玩家订阅 `personal:private:C`。

A 给 B 发消息时，服务端发布到：

```text
personal:private:A
personal:private:B
```

于是：

- A 的 `ChatPlayer.startSubHandle()` 收到 A topic 的消息；
- B 的 `ChatPlayer.startSubHandle()` 收到 B topic 的消息；
- C 不会收到，因为 C 没订阅 A/B 的 topic。

### 收到 NATS 消息后怎么推给客户端

`ChatPlayer.startSubHandle()` 循环收消息：

```go
m, err := cp.subs.NextMsg(30 * time.Minute)
...
cp.sendMsg(data.MsgTypePrivate, cp.roomID, string(m.Data))
```

收到后直接调用 `cp.sendMsg()`。

私聊发送时，`sendMsg()` 会把 `roomID` 改成当前玩家自己的 `roleID`：

```go
if kind == data.MsgTypePrivate {
    roomID = cp.roleID
}
```

然后构造推送：

```go
outData := &pb.PushTeamChat{
    InstanceType: kind,
    TeamID:       uint64(roomID),
    Msg:          msg,
}
```

最后根据 `cp.agentID` 找到 Agent，根据 `cp.connID` 写回对应客户端连接：

```go
agentSess := session.GetAgentSession(cp.agentID)
pkt.SetConnID(cp.connID)
agentSess.Write(pkt)
```

也就是说：

```text
NATS personal topic
  -> 对应玩家的 ChatPlayer
  -> cp.sendMsg()
  -> AgentID 找 Agent
  -> ConnID 找客户端连接
  -> 推给玩家客户端
```

### 为什么发送者也要 Publish 一份

因为发送者也需要在自己的客户端看到这条消息。

如果只发给接收者：

```text
personal:private:B
```

B 能收到，但 A 的客户端不会通过同一套推送链路收到“自己发送成功后的聊天消息”。当前实现选择让发送者和接收者都通过 NATS 私聊推送收到同一份消息，客户端再根据消息体里的 `RoleID/Receiver` 判断这条消息是自己发的还是别人发来的。

### 如果接收者不在线会怎样

如果 B 不在线，就没有 B 的 `ChatPlayer` 订阅 `personal:private:B`，那么这条 NATS 实时推送不会被 B 消费。

但代码还会把私聊历史保存到双方 `ChatInfo.PrivRoles`：

- 发送者 A 本地保存 `RoleID=B` 的记录；
- 通过 `RpcGameChatPrivateRole` 到 B 所在 Game 服，保存 B 的 `RoleID=A` 的记录。

所以 B 之后上线/进聊天室时，可以从 `PrivRoles` 里拿到离线期间的私聊记录。

### 一个完整例子

A=1001，B=2002。

A 给 B 发私聊：

```text
Publish personal:private:1001
Publish personal:private:2002
```

如果两人都在线：

```text
A 的 ChatPlayer 收到 personal:private:1001
  -> sendMsg
  -> 推给 A 客户端

B 的 ChatPlayer 收到 personal:private:2002
  -> sendMsg
  -> 推给 B 客户端
```

两边收到的 `Msg` 内容相同，里面有：

```text
RoleID=A
Receiver=B
Content=...
```

客户端据此把它放到“A 和 B”的私聊窗口。

### 注意点

- topic 只决定“推给哪个玩家”，不决定“属于哪个私聊窗口”。
- 私聊窗口归属由消息体 `RoleID/Receiver` 和本地 `PrivRoles.RoleID` 判断。
- 如果同一个 roleID 因异常在多个 Chat 节点都有残留订阅，理论上可能重复推送；正常流程会在重连/离线时移除旧 `ChatPlayer` 和取消订阅。

## Q14: internal/session/game.Session，它内嵌了 *tunnel.UserSession 详细介绍

### 总体结论

`internal/session/game.Session` 可以理解成 game 服自己的“角色会话”，外面包了一层 `tunnel.UserSession` 的网络会话能力。

核心定义在 `internal/session/game/session.go`：

```go
type Session struct {
    *tunnel.UserSession
    RoleID      uint32
    PveMode     uint32
    HeartNum    uint32
    SnapshotNum uint32
    PkgVer      uint64
    ResVer      uint64
    LoginTime   int64
    HeartTime   int64
    ...
}
```

这里的内嵌不是继承，而是 Go 的匿名字段组合。效果是 `Session` 自动提升了 `UserSession` 的方法，所以代码里可以直接写：

```go
session.WritePacketAsync(data)
session.IsClosed()
session.GetConnID()
session.GetAddress()
```

实际调用的是 `pkg/tunnel/session_user.go` 里的 `GetAddress`、`GetConnID`、`IsClosed`、`WritePacketAsync` 等方法。

### 两层职责

`*tunnel.UserSession` 负责网络连接维度：

- `agentID`：连接来自哪个 agent。
- `connID`：客户端连接 ID。
- `connStatus`：连接是否关闭。
- `agent *AgentSession`：底层 agent 连接。
- `wrBuffer` / `writeLoop`：异步写包队列。
- `SetRoleID/GetRoleID`：让断线时能从 conn 找回 roleID。
- `WritePacket/WritePacketAsync/Kickout/Close`：发送、踢人、关闭连接。

`game.Session` 负责业务角色维度：

- `RoleID/UserID/ZoneID`：角色、账号、区服信息。
- `Devid/Devtype/HWDevid/EngVer`：设备与客户端信息。
- `PkgVer/ResVer`：版本信息。
- `LoginTime/HeartTime/HeartNum`：登录和心跳状态。
- `PveMode`：当前玩法场景。
- `Secret/SuperLoginToken`：登录校验、超级登录状态。

### 创建和绑定链路

1. 客户端消息经 agent 转发到 game，`internal/app/game/service.go` 的 `OnMessage` 里先按 `connID` 创建或复用 `tunnel.UserSession`：

```go
sess, _ := agent.NewUserSession(pack.GetConnID(), enableUserSessionMetrics)
```

2. 登录成功后，`sessionMgr.AddSession(...)` 用 `agentID + connID` 找到这个 `UserSession`，再绑定 `roleID`：

```go
userSess := agentSess.GetUserSession(connID)
userSess.SetRoleID(roleID)
```

3. 如果这个 `roleID` 已经有 `game.Session`，就调用 `setSession` 替换里面的 `UserSession`，相当于角色换了一条新连接。

4. 如果没有，就 `newSession` 创建一个新的 `game.Session`，保存到 `SessionManager` 的分组 map，key 是 `roleID`。

### 两个 roleID 的区别

这里其实有两个 roleID：

- `tunnel.UserSession.roleID`：tunnel 层的私有字段，主要用于断线回调时从连接找回角色。
- `game.Session.RoleID`：game 业务层自己的角色 ID，主要用于按角色查会话、心跳校验、在线状态、业务逻辑。

所以这句很关键：

```go
userSess.SetRoleID(roleID)
```

它把 tunnel 层连接和 game 角色关联起来。断线时 `OnDisconnect` 会先 `agent.DelUserSession(connID)`，再通过 `sess.GetRoleID()` 拿到角色 ID，最后通知业务层处理角色断线。

### 为什么内嵌的是指针

`UserSession` 里面有 channel、连接状态、agent 指针、异步写协程等运行时状态，必须共享同一个对象。用值拷贝会非常危险，也不符合它的语义。

所以这里内嵌的是：

```go
*tunnel.UserSession
```

而不是：

```go
tunnel.UserSession
```

### 使用注意点

- `Session.UserSession` 可能被 `setSession` 替换，表示同一个角色换了新连接。
- `Session.IsOnline()` 同时看 tunnel 是否关闭和业务心跳时间。
- `PushMessageAsync` 里能直接 `session.WritePacketAsync(data)`，就是因为内嵌方法提升。
- `UserSession` 不能为 nil，否则调用提升方法会 panic；当前 `AddSession` 里已经检查 `agentSess` 和 `userSess` 为空直接返回。

一句话总结：`tunnel.UserSession` 是“这条客户端连接怎么发包、怎么关闭、属于哪个 agent”，`game.Session` 是“这个 role 当前登录状态、设备、心跳、版本、玩法状态是什么”。内嵌指针让 game 会话直接复用 tunnel 会话的网络能力。

## Q15: agentsession usersession gamesession区别是什么，为什么要分这么多

### 总体结论

这三层不是重复设计，而是在表达三个不同粒度的会话：

```text
AgentSession  = game 后端和某个 agent 服务之间的一条 TCP 连接
UserSession   = 某个客户端连接在后端里的代理对象，挂在 AgentSession 下面
game.Session  = 某个 role 登录后的业务会话，内嵌 *tunnel.UserSession
```

也就是：

```text
一个 AgentSession
  -> 管理多个 UserSession
      -> 登录成功后，其中某个 UserSession 会绑定到一个 game.Session
```

### AgentSession

`AgentSession` 定义在 `pkg/tunnel/session_agent.go`，代表后端服务和 agent 服务之间的连接。

它关心的是服务间链路：

- agent 的 ID。
- backend/service ID。
- 后端与 agent 之间的 TCP 连接。
- 读包、写包、ping、关闭。
- 管理这个 agent 下面的所有 `UserSession`。

它不是某个玩家，而是一个网关连接。一个 agent 进程可能承载很多客户端连接，所以一个 `AgentSession` 下面会挂很多 `UserSession`。

### UserSession

`UserSession` 定义在 `pkg/tunnel/session_user.go`，代表某个客户端连接在后端里的代理对象。

它关心的是连接维度：

- 这个客户端连接属于哪个 `agentID`。
- 客户端连接 ID 是哪个 `connID`。
- 连接是否关闭。
- 如何把包写回 agent，再由 agent 发给客户端。
- 异步写队列 `wrBuffer`。
- tunnel 层保存的 `roleID`，用于断线时从连接反查角色。

`UserSession` 本身仍然偏网络层。它知道“这是 agent 上的某个 conn”，但不应该承载大量 game 业务字段。

### game.Session

`game.Session` 定义在 `internal/session/game/session.go`，代表某个角色在 game 服上的业务会话。

它关心的是角色业务维度：

- `RoleID/UserID/ZoneID`。
- 设备信息 `Devid/Devtype/HWDevid`。
- 客户端版本 `PkgVer/ResVer/EngVer`。
- 登录时间、心跳时间、心跳次数。
- 当前 PVE 模式。
- 登录 secret、超级登录 token。

它内嵌 `*tunnel.UserSession`，所以既能保存业务状态，也能直接调用 `WritePacket`、`WritePacketAsync`、`IsClosed`、`GetAddress` 等网络发送能力。

### 为什么要分这么多

因为生命周期不同。

- `AgentSession` 的生命周期是 agent 到 game 的服务间连接。
- `UserSession` 的生命周期是客户端 TCP 连接。
- `game.Session` 的生命周期是角色登录后的业务在线状态。

这三种生命周期不完全同步。例如同一个角色重连时，`game.Session` 可以保留 role 业务状态，但里面的 `UserSession` 会被替换成新的连接；某个 agent 断开时，它下面的所有 `UserSession` 都要关闭；某个客户端断开时，只关闭对应 `UserSession`，不一定马上删除 `game.Session`，因为还要保留一段离线/心跳/快照处理窗口。

还因为寻址方式不同。

- 服务间发包先找 `agentID`，定位到哪个 agent。
- 客户端发包再用 `connID`，定位到 agent 下的哪个客户端连接。
- 业务查在线角色时用 `roleID`，定位到哪个 `game.Session`。

如果只用一个结构体，会把服务连接、客户端连接、角色业务状态全混在一起，导致断线、重连、踢人、跨 agent 推送、在线统计都变得很难维护。

### 一个例子

玩家 roleID=1001 通过 agentID=3、connID=888 登录：

```text
AgentSession(3)
  -> UserSession(connID=888, agentID=3, roleID=1001)
      -> game.Session(RoleID=1001, UserID=..., ZoneID=..., HeartTime=...)
```

发消息给玩家时：

```text
roleID 找 game.Session
  -> 通过内嵌 UserSession 得到 agentID/connID
  -> 找到 AgentSession
  -> 写包给 agent
  -> agent 根据 connID 发给客户端
```

断线时：

```text
agent 上报 connID 断开
  -> AgentSession.DelUserSession(connID)
  -> UserSession.GetRoleID()
  -> 通知 game 业务 roleID 断线
```

### 一句话记忆

- `AgentSession`：后端连 agent 的“服务连接”。
- `UserSession`：agent 下某个客户端的“网络连接”。
- `game.Session`：role 登录后的“业务会话”。

分三层是为了让服务连接、客户端连接、角色业务状态各管各的生命周期和职责。

## Q16: if us.pktmetrics.enable != 0 { us.pktmetrics.update(time.Now(), us.agentID, us.connID, b) } 这是什么意思

### 总体结论

这段代码是在 `UserSession.writeLoop()` 里做“下行包统计”。它不影响发包主流程，只是在指标开关开启时，统计当前客户端连接写出去的包大小、包数量、平均大小和平均速率，并定期打印日志。

代码位置在 `pkg/tunnel/session_user.go`：

```go
if us.pktmetrics.enable != 0 {
    us.pktmetrics.update(time.Now(), us.agentID, us.connID, b)
}
```

意思是：

- `us.pktmetrics.enable != 0`：统计开关打开。
- `time.Now()`：本次写包时间。
- `us.agentID`：这个客户端连接属于哪个 agent。
- `us.connID`：agent 下的哪个客户端连接。
- `b`：刚刚写给 agent 的数据包。

### 所在流程

它位于异步写包循环里：

```go
case b := <-us.wrBuffer:
    _, err := us.agent.Write(b)
    if err != nil {
        zaplog.S.Errorf("client-%d: write, %v", us.connID, err)
    }
    if us.pktmetrics.enable != 0 {
        us.pktmetrics.update(time.Now(), us.agentID, us.connID, b)
    }
```

也就是说，包先从 `wrBuffer` 取出来，然后写给 agent。写完之后，如果启用了统计，就调用 `update` 记录这次包的信息。

### update 统计了什么

`userPayloadMetrics` 结构体里有这些字段：

```go
pktSize      int64 // 累计字节数
pktMinSize   int64 // 最小包大小
pktMaxSize   int64 // 最大包大小
pktAmount    int64 // 包数量
pktStartTime int64 // 开始统计时间
pktMeterTime int64 // 上次打印统计日志的时间
```

`update` 每收到一个包，会做几件事：

1. 计算当前包大小：`sz := int64(len(b))`。
2. 累加总字节数：`m.pktSize += sz`。
3. 累加包数量：`m.pktAmount++`。
4. 更新最小包大小、最大包大小。
5. 如果距离上次打印超过 `intervalsecs`，就输出一条统计日志。

日志格式大概是：

```go
zaplog.S.Infof("conn:%d#%d, %d/%d, %d/%d/%d, %d B/n, %d B/s", ...)
```

含义可以理解为：

```text
conn:agentID#connID
累计统计时长/累计包数量
最小包大小/最大包大小/累计字节数
平均每包字节数
平均每秒字节数
```

### enable 从哪里来

`UserSession` 创建时会传入 `enableMetrics`：

```go
func NewUserSession(connID uint32, agent *AgentSession, enableMetrics uint32) *UserSession {
    us := &UserSession{
        pktmetrics: newUserPayloadMetrics(enableMetrics, 5),
    }
}
```

game 服当前传的是：

```go
const enableUserSessionMetrics = 0
```

所以默认不启用统计，这段 `if` 通常不会进入。

### 为什么要加这个开关

这种统计属于可观测性逻辑，用来排查：

- 某个客户端是否被频繁推送大包。
- 下行流量是否异常。
- 某个连接平均包大小、每秒流量是否过高。
- agent 到客户端方向是否存在高流量问题。

默认关掉可以避免每个包都做统计和打日志带来的额外开销；排查流量问题时再打开。

### 一句话总结

这段代码的意思是：如果当前 `UserSession` 开启了包流量统计，就把这次发给客户端的数据包 `b` 计入统计，用于定期打印这个连接的下行包数量、大小和速率。

## Q17: agent 上报 connID 断开 -> AgentSession.DelUserSession(connID) -> UserSession.GetRoleID() -> 通知 game 业务 roleID 断线详细介绍一下

### 总体结论

这条链路是在处理“某个客户端连接断开”：

```text
agent 发现客户端 connID 断开
  -> 给 game 发 CmdDisconnect 命令包
  -> game 的 AgentSession.OnCommand 收到 CmdDisconnect
  -> 调用 internal/app/game/service.go 的 OnDisconnect
  -> AgentSession.DelUserSession(connID) 删除并关闭 UserSession
  -> 从 UserSession.GetRoleID() 拿到 roleID
  -> manager.OnRoleDisconnect(roleID, agentID, connID)
  -> game 业务把玩家置离线、取消匹配、通知聊天/好友、落 Redis/MySQL、打离线日志
```

这条链路的关键点是：agent 上报的是 `connID`，但 game 业务要处理的是 `roleID`。中间靠 `UserSession.roleID` 完成“连接 ID 到角色 ID”的反查。

### 第一步：AgentSession 收到 CmdDisconnect

`pkg/tunnel/session_agent.go` 的 `OnMessage` 会先判断收到的是普通业务包，还是 tunnel 命令包：

```go
if inPacket.IsCmdProto() {
    s.OnCommand(inPacket)
} else {
    s.userHandler.OnMessage(s, inPacket)
}
```

如果是命令包，进入 `OnCommand`：

```go
case packet.CmdDisconnect:
    s.userHandler.OnDisconnect(s, pack)
```

这里的 `s` 是 `AgentSession`，表示 game 后端和某个 agent 之间的连接；`pack` 里带着断开的客户端 `connID`。

### 第二步：game 服务 OnDisconnect 取出 connID

`internal/app/game/service.go` 里：

```go
func (s *remoteAgentBackendService) OnDisconnect(agent *tunnel.AgentSession, pack packet.Packet) {
    connID := pack.GetConnID()
    sess, deleted := agent.DelUserSession(connID)
    ...
}
```

这里的 `agent` 是当前上报断线的 `AgentSession`。

`connID := pack.GetConnID()` 表示：agent 告诉 game，自己下面某个客户端连接断了。

注意：此时只有 `connID`，还不知道是谁的角色断了，所以需要先去 `AgentSession` 管理的 `UserSession` 表里找。

### 第三步：AgentSession.DelUserSession(connID)

`pkg/tunnel/session_agent.go`：

```go
func (s *AgentSession) DelUserSession(connID uint32) (*UserSession, bool) {
    g := s.GetUserSessionGroup(connID)
    if u := g.GetAndDel(connID); u != nil {
        u.Close()
        return u, true
    }
    return nil, false
}
```

它做了三件事：

1. 根据 `connID & 0x3f` 找到对应的 `UserSessionGroup`。
2. 从 group 的 map 里删除这个 `connID` 对应的 `UserSession`。
3. 调用 `u.Close()` 把这个 `UserSession` 标记为关闭，并关闭 `sigClose`，让异步写循环退出。

所以 `DelUserSession` 既是删除索引，也是关闭连接代理对象。

如果返回 `sess == nil`，说明这个 `connID` 在 game 侧已经找不到对应的 `UserSession`，可能之前已经被删除过，或者这条断线消息对应的是未成功建立业务会话的连接。game 会打一条 warn 后直接返回。

### 第四步：UserSession.GetRoleID()

删除出来的 `sess` 是 `*tunnel.UserSession`，它里面有一个 tunnel 层保存的 `roleID`：

```go
func (us *UserSession) GetRoleID() uint32 {
    if us != nil {
        return us.roleID
    }
    return 0
}
```

这个 `roleID` 是登录成功时写进去的。登录链路里 `SessionManager.AddSession` 会做：

```go
userSess := agentSess.GetUserSession(connID)
userSess.SetRoleID(roleID)
```

这一步非常关键。因为断线时 agent 只知道 `connID`，不知道 game 业务里的 `roleID`。提前把 `roleID` 存到 `UserSession`，断线时才能从连接反查角色。

如果 `roleid == 0`，说明这个连接还没有完成角色登录绑定，game 不会走业务离线流程。

### 第五步：通知 game 业务 roleID 断线

`internal/app/game/service.go`：

```go
roleid := sess.GetRoleID()
...
if roleid > 0 {
    manager.OnRoleDisconnect(roleid, agent.GetID(), connID)
}
```

这里把三个值传给业务层：

- `roleid`：哪个角色断线。
- `agent.GetID()`：从哪个 agent 断开。
- `connID`：agent 下哪个客户端连接断开。

业务层不是只看 `roleid`，还会校验 `agentID/connID`，这是为了防止旧连接断线事件误伤新连接。

### 第六步：OnRoleDisconnect 的防误伤校验

`internal/manager/battle.go`：

```go
player, err := playerRepo.GetPlayer(roleid)
curAgentID, curConnID := player.UserInfo.GetNetID()
if agentid != curAgentID || connid != curConnID {
    return
}
```

这个判断非常重要。

例如玩家快速重连：

```text
旧连接：agentID=3, connID=888
新连接：agentID=4, connID=999
```

如果旧连接的断线消息晚到了，不能因为旧 `connID=888` 断开，就把新连接上的玩家置离线。所以业务层会拿玩家当前记录里的 `agentID/connID` 做比对：

- 如果断开的正是当前连接，才继续离线处理。
- 如果断开的是旧连接，直接 return。

### 第七步：真正的业务离线处理

校验通过后，`OnRoleDisconnect` 会做一串业务处理：

```go
player.UserInfo.SetOffline()
```

把玩家状态置为离线。

如果玩家还在匹配中：

```go
if player.UserInfo.IsInMatch() {
    service.PlayerRpcCancelMatch(player)
}
```

取消匹配。

然后处理一些玩法/队伍离线逻辑：

```go
service.FamilyTeamWhenOffline(player)
service.OperKitchenWhenOffline(player)
service.HomeCityTeamWhenOffline(player)
```

接着写回 Redis：

```go
err = playerRepo.SetPlayer(player)
```

然后广播玩家在线状态变化：

```go
service.PlayerRpcBroadcastPlayerOnlineStatus(player, service.BCPlayerOffline)
```

再让玩家离开聊天房间：

```go
service.PlayerLeaveChatRoom(roleid, 0)
```

之后还会补离线埋点、角色快照埋点，并执行离线 DBSave：

```go
repo.GetDBSaveRepo().SetPlayer("offline", player, false)
repo.GetDBSaveRepo().SetRoleEventInfo("offline", roleEvent)
```

### 为什么不直接 Del game.Session

这条链路里，`DelUserSession(connID)` 会删除 tunnel 层的 `UserSession`，但并没有立即 `session.GetSessionManager().DelSession(roleid)` 删除 game 业务层的 `game.Session`。

原因是 game 的 `Session` 还承载登录时间、心跳、快照判断、离线埋点等业务信息。当前代码在离线时还会读取旧 session：

```go
oldSess, ok := sessMgr.GetSession(roleid)
if ok {
    recOffline.SetInt("times", time.Now().Unix()-oldSess.LoginTime)
    recOffline.SetString("hw_devid", oldSess.HWDevid)
    ...
}
```

同时 `SessionManager` 还有空闲清理逻辑：

```go
IsMaxIdleReach(nowts)
```

也就是：网络连接先断，业务 session 可以短时间保留，用于离线处理、统计、快照和后续清理。

### 一句话总结

这条链路的本质是把“agent 上报的 connID 断开”转换成“game 业务里的 roleID 离线”：

```text
connID 是网络连接维度
roleID 是业务角色维度
UserSession 是中间桥梁
agentID/connID 校验用于防止旧断线事件误伤新登录
```

## Q18: if inPacket.IsCmdProto() { s.OnCommand(inPacket) } else { s.userHandler.OnMessage(s, inPacket) } 这里是怎么收到OnCommand包的，怎么知道是断线，发起断线的地方是怎么传的

### 总体结论

game 后端这里收到的 `OnCommand` 包，不是客户端直接发来的业务协议，而是 agent 和 backend 之间 tunnel 层的内部命令包。

断线包的完整方向是：

```text
客户端 TCP 断开
  -> agent 的 FrontendSession.Serve 读包失败或处理失败
  -> FrontendSession.NotifyBackendDisconnect()
  -> BackendSession.notifyFrontendClosed(connID)
  -> 构造 CmdDisconnect 命令包，写给 game 后端
  -> game 后端 AgentSession.Serve 读到这个包
  -> AgentSession.OnMessage
  -> inPacket.IsCmdProto() == true
  -> AgentSession.OnCommand
  -> pack.GetCmd() == packet.CmdDisconnect
  -> game.OnDisconnect
```

### IsCmdProto 怎么判断这是命令包

packet 头里有 `ProtoID` 字段，位置是：

```go
IdxProtoID = 0x06
```

命令 ID 定义在 `pkg/packet/packet.go`：

```go
const (
    CmdPing        = 0x0000
    CmdRegister    = 0x0001
    CmdDisconnect  = 0x0002
    CmdKickout     = 0x0003
    CmdRegisterACK = 0x0004
)
```

`IsCmdProto()` 的判断是：

```go
func (packet Packet) IsCmdProto() bool {
    // cmd proto[6-7]: 0x0000 ~ 0x00FF
    return (packet[IdxProtoID] == 0)
}
```

也就是说，只要 `ProtoID` 的高字节是 `0`，就认为这是 tunnel 命令包。比如：

```text
CmdDisconnect = 0x0002
高字节 = 0x00
低字节 = 0x02
```

所以 `IsCmdProto()` 会返回 `true`。

### 怎么知道是断线

`IsCmdProto()` 只知道“这是命令包”，不知道具体是什么命令。

真正判断“这是断线”的地方在 `AgentSession.OnCommand`：

```go
func (s *AgentSession) OnCommand(pack packet.Packet) {
    cmd := pack.GetCmd()
    switch cmd {
    case packet.CmdPing:
        s.OnPing(time.Now())
    case packet.CmdDisconnect:
        s.userHandler.OnDisconnect(s, pack)
    default:
        ...
    }
}
```

`pack.GetCmd()` 实际读取的也是 packet 头里的 `ProtoID`：

```go
func (packet Packet) GetCmd() uint16 {
    return binary.BigEndian.Uint16(packet[IdxProtoID : IdxProtoID+2])
}
```

所以当 agent 发来的包 `ProtoID = 0x0002` 时：

```text
IsCmdProto() == true
GetCmd() == CmdDisconnect
```

game 后端就知道这是客户端断线通知。

### 发起断线的地方在哪里

agent 侧客户端连接对象是 `FrontendSession`。它在 `internal/session/agent/frontend.go` 的 `Serve()` 循环里读客户端包：

```go
inRequest, err := s.ReadPacket()
if err == nil {
    err = s.OnMessage(inRequest)
    if err != nil {
        s.NotifyBackendDisconnect()
        break
    }
} else {
    s.NotifyBackendDisconnect()
    break
}
```

两种情况都会通知后端断线：

- `ReadPacket()` 失败，例如客户端 TCP 断开、EOF、网络错误。
- `OnMessage()` 返回错误，例如协议不合法、签名失败、未授权、找不到 backend 等。

`NotifyBackendDisconnect()` 里会调用当前绑定的 backend session：

```go
func (s *FrontendSession) NotifyBackendDisconnect() {
    if s.backend != nil && !s.backend.IsClosed() {
        err := s.backend.notifyFrontendClosed(s.id)
        ...
    }
}
```

这里的 `s.id` 就是 agent 给这个客户端分配的 frontend session ID，也就是后端看到的 `connID`。

### CmdDisconnect 包是怎么构造并传过去的

agent 侧 `BackendSession.notifyFrontendClosed`：

```go
func (s *BackendSession) notifyFrontendClosed(id uint32) error {
    pkt := cmdPacketPool.Get().(packet.Packet)
    pkt.SetConnID(id)
    pkt.SetProtoID(packet.CmdDisconnect)
    _, err := s.Write(pkt)
    cmdPacketPool.Put(pkt)
    return err
}
```

关键字段有两个：

```go
pkt.SetConnID(id)
pkt.SetProtoID(packet.CmdDisconnect)
```

含义是：

- `ConnID = id`：告诉 backend，agent 下哪个客户端连接断了。
- `ProtoID = CmdDisconnect`：告诉 backend，这是 tunnel 断线命令包。

然后 `s.Write(pkt)` 会把这个包写到 agent 和 game 后端之间的 TCP 连接上。game 后端的 `AgentSession.Serve()` 正在读这条连接，所以会收到这个包。

### connID 为什么就是 FrontendSession.id

客户端第一次转发到 backend 时，agent 会把 frontend session 绑定到某个 backend：

```go
func (s *FrontendSession) BindBackendSession(backend *BackendSession) {
    s.id = backend.GetFrontendSessionID()
    s.backend = backend
    backend.GetFrontendGroup(s.id).Add(s)
}
```

转发客户端业务包给后端前，会设置：

```go
pkt.SetConnID(s.id)
```

所以 backend/game 看到的 `connID`，就是 agent 这边的 `FrontendSession.id`。

断线时也传同一个 `s.id`：

```go
s.backend.notifyFrontendClosed(s.id)
```

这样 game 后端才能用同一个 `connID` 找到之前创建的 `UserSession`。

### 还有哪些地方会触发断线通知

除了客户端真实断开，代码里还有两个常见触发点：

1. 客户端切换绑定的 backend 或 service。

```go
if serviceID != oldServiceID || (backendID != 0 && backendID != oldBackendID) {
    s.NotifyBackendDisconnect()
    s.UnBindBackendSession()
}
```

这表示当前 frontend session 要从旧 backend 解绑，解绑前先通知旧 backend 这个 `connID` 断开。

2. backend 主动踢人。

backend 发 `CmdKickout` 给 agent，agent 的 `BackendSession.OnKickout` 会关闭 frontend session，然后再通知 backend 断线：

```go
frontendSess.Close()
frontendSess.NotifyBackendDisconnect()
frontendSess.UnBindBackendSession()
```

### 客户端能直接发 CmdDisconnect 吗

正常不允许。

agent 收到客户端业务包后，会检查：

```go
if inPacket.IsCmdProto() {
    return errInvalidProtocol
}
```

也就是说，客户端发来的命令包会被 agent 拒绝。`CmdDisconnect` 是 agent/backend tunnel 内部协议，不是客户端业务协议。

### 一句话总结

`OnCommand` 包是 agent 自己构造并发给 game 后端的 tunnel 内部包。断线时 agent 把 `connID` 写进包头，把 `ProtoID` 设置成 `CmdDisconnect(0x0002)`；game 后端收到后先用 `IsCmdProto()` 判断这是命令包，再用 `GetCmd()` 判断具体是断线命令，最后进入 `OnDisconnect`。



