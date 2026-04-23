# zserver 问答记录

## 目录

- [[#Q1: Actor 风格事件队列详细分析]]
- [[#Q4: BattleRoom 帧循环详解]]
- [[#Q5: 有界异步任务池 — Channel 当 Semaphore]]
- [[#Q6: 异步日志批量刷盘]]
- [[#Q7: 服务器-客户端连接架构]]
---

## Q1: Actor 风格事件队列详细分析

**涉及代码**：`internal/scene/mainteam/room.go`、`internal/battle/battle_room_packet.go`、`internal/operbattle/oper_battle_rpc.go`

### 三处实现的架构对比

| 维度 | `mainteam/Room` | `battle/BattleRoom` | `operbattle/BaseOperBattleTeam` |
|------|-----------------|---------------------|-------------------------------|
| 通道字段 | `eventList chan *scene.AsyncEvent` | `packetArr chan *RpcPacket` | `rpcDataChan chan OperBattleRpcData` |
| 容量 | **20** | **1024** | **1024** |
| 生产者 | `PutAsyncEvent()` | `PacketAdd()` | `AddBattleRpcData()` |
| 消费者 | *(当前无活跃消费者，handle已注释)* | `handleAllRpcEvents()` | `handleBattleRpcData()` |
| 消费位置 | Room.Loop() goroutine（状态机分支） | `startLoop()` goroutine 每帧调用 | `runLoop()` → `updateFrame()` 每帧调用 |
| 帧率 | 心跳驱动（秒级） | 15fps（~66ms/帧） | 可配置（`frameTick`） |
| 溢出处理 | `select default` → 返回 error | `select default` → **仅打日志，丢弃** | `select default` → **仅打日志，丢弃** |

### 核心模式

三处实现的本质都是 **单 goroutine 状态机 + 有缓冲 channel 收件箱**，即经典的 Actor/Mailbox 模型：

```
外部并发 goroutine (RPC handler / 网关)
        │
        ▼
  ┌─────────────┐
  │  chan 收件箱  │  ← PacketAdd / AddBattleRpcData / PutAsyncEvent
  └─────┬───────┘
        │ (串行消费)
        ▼
  ┌─────────────┐
  │ Loop goroutine│  ← 唯一消费者，独占状态
  │  handleAll   │
  │  update()    │
  │  broadcast() │
  └─────────────┘
```

### 妙处

1. **零锁状态修改**：房间/战斗的核心可变状态只在 loop goroutine 内读写，完全不需要互斥锁。`BattleRoom` 的 `lock` 只保护 `players map` 的外部查询，不保护战斗逻辑本身。
2. **实体 = 最小一致性单元**：每个房间对象是一个独立的 Actor，生命周期、帧循环、状态转换完全自包含。多个房间并行在各自 goroutine 中运行，彼此无共享状态。
3. **生产端极轻**：`PacketAdd` / `AddBattleRpcData` 只做一件事——塞 channel。RPC handler goroutine 不碰任何战斗状态，不持锁，不阻塞。
4. **批量消费**：`handleAllRpcEvents()` 用 `sz := len(br.packetArr)` 一次性取完当前积压的所有事件，保证同一帧内的所有 RPC 指令都在该帧生效（时序确定性）。
5. **对象池复用**（`mainteam/Room`）：`asyncEventPool = sync.Pool{...}` + `ae.Recycle()` 减少 GC 压力。`BattleRoom` 和 `OperBattle` 的 `RpcPacket` 未做对象池。

### 三处实现的差异

#### `battle/BattleRoom` — 最核心的帧循环

- 15fps 帧循环，每帧先排空 RPC 事件，再执行战斗逻辑和广播
- `packetDeal()` 是一个 30 路 switch 分发，覆盖所有战斗指令
- `packetArr` 容量 1024

#### `operbattle/BaseOperBattleTeam` — 泛化的玩法战斗框架

- 用 `SubOperBattleTeamer` 接口实现多态，不同玩法通过实现接口接入同一个 Actor 框架
- RPC 处理分两层：`handleCommonBattleRpcData()` 处理通用事件，`subBattleTeam.HandleBattleRpcData()` 处理玩法特有事件
- `funcHandler` 可注入测试桩，支持单测中用 `LoopForTest()` 同步运行 loop

#### `mainteam/Room` — 场景管理层

- loop 主要做心跳监控、在线检测、队长换选等管理逻辑
- `eventList` 容量只有 20，且当前无活跃消费者（`AsyncEventID` 常量为空，`handle()` 已注释）——是预留扩展点或已废弃的遗留设施
- 实际的外部写内部串行改模式体现在 `HandleTeamMember()` / `HandleTeamChangeMap()` 的 lock+flag 机制

### 可优化点

#### 1. `PacketAdd` 满了丢弃 — 战斗指令丢失风险

释放技能、玩家上线等关键事件丢失会导致玩家操作无响应、状态不同步。建议按 `key` 区分事件优先级，关键事件走阻塞写或独立高优通道。

#### 2. `handleAllRpcEvents()` 无单次上限 — 长尾帧风险

瞬间涌入大量事件时全部在单帧处理，会导致该帧执行时间远超 66ms。建议加 `maxEventsPerFrame` 上限，未处理事件留在 channel 下一帧消费。

#### 3. 容量经验值缺乏校准

建议对 `len(channel)` 做周期性 metric 采样，建立水位基线。overflow 日志升级为带 metric counter 的告警。`mainteam/Room.eventList` 容量 20 偏小，若启用需扩容。

#### 4. `RpcPacket` 缺少对象池

`BattleRoom.PacketAdd` 每次 `new(RpcPacket)`，在高帧率战斗中分配量不低，可补上 `sync.Pool` 减少 GC 压力。

### 总结

三处 channel 用法是项目里 Actor 模型的三个层级实现：`battle/BattleRoom` 是战斗帧循环心脏，`operbattle` 是泛化玩法框架，`mainteam/Room` 是场景管理层。共同构成 "外部并发投递 → channel 缓冲 → 单 goroutine 串行消费" 的一致架构。

---

## Q4: BattleRoom 帧循环详解

### 一、时间常量

`battle_config.go:11-14`：

| 模式 | 帧率 | 每帧时长 |
|------|------|----------|
| 普通战斗 | **15 FPS** | ~66.67ms |
| 魔物争霸 | **24 FPS** | ~41.67ms |

```go
constFrameRate        = 15
FrameTick             = time.Second / constFrameRate          // 66ms
FrameTickMonsterPower = time.Second / constFrameRateMonsterPower  // 41ms
```

### 二、帧计时器 FrameTime

`battle_frame.go`，四个字段记录帧的精确时间：

```go
type FrameTime struct {
    frameNum      int64      // 帧号（每帧+1）
    prevStartTime time.Time  // 上一帧开始时间
    curStartTime  time.Time  // 当前帧开始时间
    curEndTime    time.Time  // 当前帧结束时间
}
```

- `FrameStart()` — 帧号递增 + 记录 `curStartTime`
- `FrameEnd()` — 记录 `curEndTime`，把 `curStartTime` 存入 `prevStartTime`
- `ExecTime()` — 返回 `curEndTime - curStartTime`，即本帧执行耗时

### 三、循环启动 startLoop()

`battle_room_loop.go:223-281`，房间创建时自动调用，启动独立 goroutine：

```
newBattleRoom()
  → br.packetArr = make(chan *RpcPacket, 1024)
  → br.startLoop()     ← 启动循环
```

核心流程：

```go
func (br *BattleRoom) startLoop() {
    br.loop = true
    frameTick := FrameTick                                // 默认 66ms
    br.ctx, br.cancel = context.WithCancel(context.Background())

    go func() {
        defer recover()                                   // panic 保护
        time.Sleep(1 * time.Second)                       // 等待1秒让初始化完成

        for {
            select {
            case <-br.ctx.Done():                         // ← 收到关闭信号
                return                                    //    退出循环
            default:
                br.frameTime.FrameStart()                 // ① 记录帧开始
                br.heartCheck()                           // ② 心跳检查
                br.handleAllRpcEvents()                   // ③ 处理所有RPC事件
                br.updateAndBroadcastFrame()              // ④ 更新逻辑 + 广播战报
                br.frameTime.FrameEnd()                   // ⑤ 记录帧结束
                // 帧率调节：Sleep 补齐或告警
            }
        }
    }()
}
```

### 四、每帧 5 个阶段

```
┌──────────────────────────────────────────────────────────────────┐
│                        一帧 (~66ms)                              │
│                                                                  │
│  FrameStart ──→ heartCheck ──→ handleAllRpcEvents               │
│                                      │                           │
│                                      ▼                           │
│                          updateAndBroadcastFrame ──→ FrameEnd    │
│                                                                  │
│  ◄─── 执行耗时 ────►◄──── Sleep 补齐 ────►                       │
│  ◄───────────── frameTick (66ms) ──────────►                     │
└──────────────────────────────────────────────────────────────────┘
```

**① FrameStart** — `frameNum++`，记录当前时间

**② heartCheck** (`battle_room_loop.go:293-304`)
- 每帧递减 `heartTime`（减 `1.0/15` 秒）
- 倒计时归零 → `stopLoop()` + `DeleteRoom()`
- 心跳初始值 60 秒，由客户端发送 `BattleHeart` 事件刷新

**③ handleAllRpcEvents** (`battle_room_packet.go:136-141`)
```go
sz := len(br.packetArr)        // 快照当前队列长度
for i := 0; i < sz; i++ {
    br.packetDeal(<-br.packetArr)
}
```
`packetDeal()` 是 30 路 switch，覆盖所有战斗指令（技能释放、玩家上线、心跳、暂停、投降等）。

**④ updateAndBroadcastFrame** (`battle_room_loop.go:19-30`)
- `br.mgr.update()` — 执行战斗逻辑（AI、技能、伤害计算等）
- `br.broadcastFrame()` — 将本帧战报发给客户端，战报经 `filterLogs()` 过滤只发可见内容

**⑤ FrameEnd** — 记录结束时间

### 五、帧率调节

```go
if d := br.frameTime.ExecTime(); d < frameTick {
    time.Sleep(frameTick - d)     // 快了 → Sleep 补齐
} else if d > frameTick {
    zaplog.S.Warnf(...)           // 慢了 → 告警 + 输出战斗详情
}
```

魔物争霸模式可根据 `frameTickState` 在 15FPS / 24FPS 之间动态切换。

### 六、生产端 PacketAdd

```go
func (br *BattleRoom) PacketAdd(key uint32, pbData interface{}) {
    rpcPacket := &RpcPacket{key: key, pbData: pbData}
    select {
    case br.packetArr <- rpcPacket:   // 非阻塞写入
    default:
        zaplog.S.Errorf("overflow")   // 满了 → 丢弃 + 打日志
    }
}
```

### 七、关闭流程

```go
func (br *BattleRoom) stopLoop(isLastBroad bool) {
    if isLastBroad { br.broadcastFrame() }   // 发送最后一帧
    br.loop = false
    br.cancel()                               // ctx.Done() → goroutine 退出
}
```

### 八、完整生命周期

```
gRPC CreateRoom() → newBattleRoom() → startLoop()
                                          │
     ┌── goroutine ──────────────────────────────────┐
     │   Sleep(1s)                                    │
     │   for {                                        │
     │     FrameStart()           ← frameNum++        │
     │     heartCheck()           ← 60秒超时检查       │
     │     handleAllRpcEvents()   ← 排空channel       │
     │     mgr.update()           ← 战斗逻辑          │
     │     broadcastFrame()       ← 发送战报          │
     │     FrameEnd()                                 │
     │     Sleep(剩余时间)         ← 凑满66ms          │
     │   }                                            │
     └────────────────────────────────────────────────┘
            │
     stopLoop() ← DeleteRoom / heartCheck
            │
     cancel() → goroutine 退出
```

---

## Q5: 有界异步任务池 — Channel 当 Semaphore

**涉及代码**：`pkg/chat/chat.go`、`pkg/chat/event.go`

### 整体架构

两个文件使用相同模型：两条 channel 组合实现 "有界任务队列 + 并发闸门"。

```
外部调用 (SendMsg / AddEvent)
      │ 非阻塞写入
      ▼
┌─────────────┐
│  msgChan /  │  ← 任务队列（有缓冲，容量=maxBuf）
│  eventChan  │
└─────┬───────┘
      │ range 消费（单 goroutine）
      ▼
┌─────────────┐
│  execChan   │  ← 并发闸门/信号量（容量=maxExec）
│  chan struct │
└─────┬───────┘
      │ 获取令牌后 go func()
      ▼
┌─────────────┐
│  worker     │  ← 实际执行（调 LeanCloud API）
│  goroutine  │     defer <-execChan 归还令牌
└─────────────┘
```

### 信号量核心原理

```go
type MsgSender struct {
    msgChan  chan *MsgBody      // 任务队列
    execChan chan struct{}      // ← 信号量，容量 = 最大并发数
}
```

- `execChan <- struct{}{}` — 获取令牌（满了阻塞，限制并发）
- `<-execChan` — 归还令牌（放在 defer 中，防 panic 泄漏）

### 消费循环

```go
// chat.go loop()
for msgObj := range ms.msgChan {     // ① 取任务
    ms.execChan <- struct{}{}         // ② 获取令牌（阻塞=限流）
    go ms.sendToLeanCloud(msgObj)     // ③ 起 goroutine 执行
}
```

### 两套实现对比

| 维度 | MsgSender (chat.go) | AsyncEventMgr (event.go) |
|------|---------------------|--------------------------|
| 投递 | SendMsg — 满了打日志但返回 true | AddEvent — 满了返回 error |
| 执行 | sendToLeanCloud — 发消息 | handleEvent — 群组操作 |
| 重试 | 无 | 有，失败重新入队，最多5次 |
| 附加 | MsgLimiter 频率限制 | 无 |

### 优化空间

1. **SendMsg 返回值语义**：满了仍返回 true，调用方误以为成功。对比 AddEvent 已正确返回 error。
2. **固定 Worker 池**：当前每任务 go func()，可改为 N 个 worker 竞争消费 msgChan，省去 execChan 和 goroutine 创建开销。
3. **事件重试无退避**：失败后立即重新入队，可能打满 LeanCloud API。且同一群组的操作顺序性无法保证。

---

## Q6: 异步日志批量刷盘

**涉及代码**：`internal/xdlog/file.go`、`internal/xdlog/tapdb.go`

### 架构

三条 channel 协作的异步 sink 模式：

```
业务 goroutine → buffer(chan []byte) → loop goroutine → 磁盘/HTTP
                                         ↑ ticker 定时刷
                                         ↑ exit 退出信号
```

### 两级缓冲

1. `chan []byte`（跨 goroutine 队列，容量 100万~300万）
2. `bytes.Buffer`（攒批缓冲，满 200 条或 100 条自动刷）

### loop() 三路 select

```go
select {
case b := <-l.buffer:    // 取日志 → 攒批（满200条自动sync）
case <-ticker.C:         // 30秒/20秒定时 → sync + rotate
case <-l.exit:           // 退出 → drain全部 → sync → close
}
```

### FileWriter vs TapDBWriter

| 维度 | FileWriter | TapDBWriter |
|------|-----------|-------------|
| 刷盘周期 | 30秒 | 20秒 |
| 攒批上限 | 200条 | 100条 |
| 攒批格式 | 逐行 `\n` | JSON `{"data":[...]}` |
| 输出目标 | os.File 磁盘 | TapDB HTTP API |
| 重试 | 无 | 1次 |
| 日切轮转 | 有 | 无 |

### 设计亮点

1. **主流程零IO**：Write()只做channel写入
2. **阻塞式Close**：CAS防重复 + exitFlag同步等待，保证退出时日志刷完
3. **drain保障**：退出时排空channel，减少日志丢失
4. **Nop模式**：TapDB禁用时零开销

### 优化空间

1. channel缓冲过大（300万条≈570MB），积压到此量级说明下游已出问题
2. FileWriter用了O_SYNC，既然已异步写可去掉改用手动Fsync
3. TapDB JSON尾部逗号修复依赖字节检查，较脆弱

---

## Q7: 服务器-客户端连接架构

**涉及代码**：`3rdparty/zero/tcp/service.go`、`internal/app/agent/service.go`、`internal/session/agent/frontend.go`、`internal/session/agent/backend.go`、`pkg/packet/packet.go`、`pkg/packet/crypto.go`、`pkg/packet/packet_buffer.go`

### 一、三层网关架构总览

```
┌──────────┐         ┌──────────────────┐         ┌──────────────────┐
│  Client  │  TCP    │   Agent Server   │  TCP    │  Backend Server  │
│  (手机)  │ ◄─────► │   (网关/代理)     │ ◄─────► │  (Game/Login/    │
│          │  :9190  │                  │  :9191  │   Battle/Chat)   │
└──────────┘ 加密通道 └──────────────────┘ 明文通道 └──────────────────┘
                     前端监听  ↑  后端监听           gRPC :20011(服务间)
                              │
                         负载均衡
                         加解密
                         会话管理
                         广播分发
```

**Agent 是所有客户端连接的唯一入口**，它不处理任何业务逻辑，只做转发、加解密和会话管理。

### 二、连接建立全流程

#### Phase 1: TCP Accept

`3rdparty/zero/tcp/service.go:129-158`

```go
func (ts *Service) Serve(l net.Listener) {
    var tempDelay time.Duration
    for {
        conn, err := l.Accept()
        if err != nil {
            // 指数退避重试（5ms → 10ms → ... → 1s 上限）
            // 参考自 Go 标准库 net/http/server.go
            if ne, ok := err.(net.Error); ok && ne.Temporary() {
                tempDelay *= 2
                time.Sleep(tempDelay)
                continue
            }
            break
        }
        tempDelay = 0
        go ts.connHandler(conn)    // ← 每个连接一个 goroutine
    }
}
```

**关键点**：Accept 错误时的指数退避策略直接照搬了 Go 标准库 `http.Server`，防止在 fd 耗尽等临时错误下疯狂重试。

#### Phase 2: Agent 接入客户端

`internal/app/agent/service.go:31-45`

```go
func (as *AgentService) ServeFrontend(nc net.Conn) {
    // IP 白名单检查
    if !srvConf.Misc.IsWhiteAddr(addr) {
        nc.Close()
        return
    }
    agentsess.NewFrontendSession(nc, as.backendManager).Serve()
}
```

#### Phase 3: 后端服务注册

`internal/app/agent/service.go:48-68`

```go
func (as *AgentService) ServeBackend(nc net.Conn) {
    sess := agentsess.NewBackendSession(0, nc)
    sess.WaitRegister(as.backendManager)  // 等待注册包
    sess.CheckPing()                       // 启动心跳检测
    sess.Serve()                           // 进入消息循环
}
```

注册握手 (`backend.go:457-496`)：
```
Backend → Agent:  CmdRegister 包（含 ServiceID + BackendID）
Agent → Backend:  CmdRegisterACK 包（含 AgentID）
```

### 三、二进制包协议

`pkg/packet/packet.go`

#### 包头结构（最小 16 字节）

```
偏移    长度    字段            说明
─────────────────────────────────────────────────────
0x00    1B     Version         协议版本号
0x01    3B     DataSize        包总长度（24-bit 大端）
0x04    1B     HeadSize        包头长度（动态，最小16）
0x05    1B     DataFlag        标志位（见下）
0x06    2B     ProtoID         MID(1B) + AID(1B) 协议号
0x08    2B     ServiceID       目标服务ID / ConnID高位
0x0A    2B     BackendID       后端实例ID / ConnID低位
0x0C    4B     ConnID/Nonce    客户端连接ID
0x10    ...    Signature       可选签名（HMAC/SUM32/CRC32）
HeadSize...    DataLoad        Protobuf 业务数据（可能ZLIB压缩）
```

#### DataFlag 位域

```
bit 0:      ZLIB 压缩 (0/1)
bit 1-2:    加密方式 (0=无, 1=XOR, 2=RC4)
bit 3-4:    签名方式 (0=无, 1=HMAC-SHA1, 2=SUM32, 3=CRC32)
bit 5:      广播标志 (0=单播, 1=广播)
bit 6-7:    保留
```

#### 读包流程

`pkg/packet/packet_buffer.go:48-75`

```go
func (buf *PacketBuffer) ReadPacket(r io.Reader) (int, error) {
    var sizeHeader [4]byte
    io.ReadFull(r, sizeHeader[:4])                   // ① 读4字节头
    size := sizeHeader[3] + sizeHeader[2]<<8 + ...   // ② 解析包长
    buf.Alloc(size)                                   // ③ 从内存池分配
    buf.data[0:4] = sizeHeader                        // ④ 回填头部
    io.ReadFull(r, buf.data[4:size])                  // ⑤ 读剩余数据
    return size, nil
}
```

**关键点**：先读 4 字节获取长度 → 从 slab 内存池分配精确大小的 buffer → 再读剩余数据。避免了两次 `make([]byte)` 分配。

### 四、加解密机制

`pkg/packet/crypto.go`

| 通道 | 加密方式 | 原因 |
|------|---------|------|
| Client ↔ Agent | XOR 或 RC4 | 公网传输需加密 |
| Agent ↔ Backend | 无加密 | 内网通信，省 CPU |

#### XOR 加密（轻量级）

```go
func (c *xorCrypto) Encrypt(packet Packet) {
    c.encryptOrDecryptDataLoad(packet)   // ① 先加密数据体（用 MID/AID 做 secret）
    c.encryptOrDecryptHead(packet)       // ② 再加密包头（用 DataSize 做 secret）
    packet.SetDataFlag(FlagXOR)          // ③ 设置加密标志
}
```

XOR 加密的 secret 派生：
- 包头加密：从 `DataSize` 取 2 字节做 key，逐字节 XOR `packet[6:HeadSize]`
- 数据加密：从 `ProtoMID` + `ProtoAID` 取 2 字节做 key，逐字节 XOR 数据体

#### RC4 加密（较强）

```go
func (c *rc4Crypto) Encrypt(packet Packet) {
    c.encryptOrDecryptDataLoad(packet)   // RC4 流加密数据体
    c.encryptOrDecryptHead(packet)       // XOR 加密包头（与 XOR 模式相同）
    packet.SetDataFlag(FlagRC4)
}
```

**关键点**：Agent 收到客户端包后立即解密，转发给 Backend 时是明文；Backend 响应回 Agent 时也是明文，Agent 在发给客户端前才加密。

### 五、会话管理

#### FrontendSession（客户端会话）

`internal/session/agent/frontend.go:25-46`

```go
type FrontendSession struct {
    id         uint32                    // 连接ID（动态分配）
    connStatus uint32                    // Open/Close（原子操作）
    conn       *gozd.BaseConn           // TCP 连接
    sigClose   chan struct{}             // 关闭信号
    wrBuffer   chan *packet.PacketBuffer // 异步写队列（容量120）
    backend    *BackendSession           // 绑定的后端会话
    authStatus uint32                    // 认证状态
    signsecret []byte                    // 签名密钥
}
```

#### 64-bucket 分桶锁

```go
type FrontendSessionManager struct {
    cidCounter     uint32
    cidBuffer      chan uint32                // ID 回收池（容量1000）
    frontendGroups [64]*FrontendSessionGroup  // 64 个哈希桶
}

func (mgr *FrontendSessionManager) GetFrontendGroup(id uint32) *FrontendSessionGroup {
    return mgr.frontendGroups[id & 0x3f]   // 按连接ID哈希到桶
}
```

**关键点**：64 个桶各有独立 `sync.RWMutex`，万级连接下锁竞争降低到每桶百级。

#### 两种 SessionManager

| 类型 | 用途 | ID 回收 |
|------|------|---------|
| `persistFrontendSessionMgr` | Game/Battle/Chat 长连接 | 有，ID 可复用 |
| `transientFrontendSessionMgr` | Login 短连接 | 无，用完即弃 |

### 六、消息转发核心路径

#### 客户端 → 后端（上行）

`frontend.go:295-334` → `frontend.go:366-501`

```
FrontendSession.Serve()
  │
  ├─ ReadPacket()                    ① 读取二进制包
  │
  ├─ OnMessage(req)
  │   ├─ Decrypt(inPacket)           ② 解密
  │   ├─ IsValid() / IsCmdProto()    ③ 校验（禁止客户端发CMD包）
  │   ├─ checkSignature()            ④ 验签（SUM32/CRC32/HMAC）
  │   ├─ IsAuthed()                  ⑤ 检查认证状态
  │   └─ Forward(inPacket)           ⑥ 转发
  │       ├─ 按 ServiceID 查 BackendGroup
  │       ├─ 按 BackendID 查 BackendSession（或随机负载均衡）
  │       ├─ 首次绑定 / 服务切换时自动重绑
  │       ├─ pkt.SetConnID(s.id)     ⑦ 写入客户端连接ID
  │       └─ backend.Write(pkt)      ⑧ 明文转发
```

**关键点**：`Forward()` 中的服务切换逻辑 - 当客户端从 Login 切换到 Game 时，Agent 会自动解绑旧 Backend、绑定新 Backend、通知旧 Backend 客户端断开。

#### 后端 → 客户端（下行）

`backend.go:272-298` → `backend.go:429-449`

```
BackendSession.Serve()
  │
  ├─ ReadRequest()                   ① 读取包
  │
  └─ go OnMessage(req)              ② 异步处理（注意这里用了 go）
      │
      ├─ IsCmdProto()?
      │   ├─ CmdPing → OnPing()      心跳
      │   └─ CmdKickout → OnKickout() 踢人
      │
      ├─ IsBroadcast()?
      │   └─ broadcastFrontends()     ③-A 广播
      │       ├─ 解析 BroadcastClients 列表
      │       ├─ 为每个客户端复制一份包
      │       └─ 逐个 writeFrontend()
      │
      └─ writeFrontend(req)           ③-B 单播
          ├─ GetFrontendGroup(connID).Get(connID)  找客户端
          ├─ SetServiceID / SetBackendID           回写服务信息
          ├─ SetHeadSign()                         重新签名
          ├─ DefaultCrypto.Encrypt()               加密
          └─ frontendSess.WriteAsync(req)          写入异步队列
```

#### 异步写回（Frontend WriteLoop）

`frontend.go:531-559`

```go
func (s *FrontendSession) waitResponse() {
    for {
        select {
        case resp := <-s.wrBuffer:       // 从 120 容量队列取
            _, err := s.Write(resp.Bytes())  // 写入 TCP socket
            resp.Free()                      // 归还内存池
            if err != nil {
                s.conn.Close()
                return
            }
        case <-s.sigClose:               // 连接关闭信号
            return
        }
    }
}
```

**关键点**：读和写分离到两个 goroutine - `Serve()` 负责读，`waitResponse()` 负责写。通过 `wrBuffer` channel 解耦。

### 七、内存池管理

```go
// Agent 后端通道：512B slab, 32KB chunk, 2层, 8MB 预分配
tunnelPool = tunnel.NewSessionPool(
    slab.NewAtomPool(512, 32*1024, 2, 8*1024*1024),
    zeropool.NewBufReaderPool(1000, 8*1024),
)

// Agent 前端通道：128B slab, 1KB chunk, 2层, 1MB 预分配
frontendPool = tunnel.NewSessionPool(
    slab.NewAtomPool(128, 1024, 2, 1024*1024),
    zeropool.NewBufReaderPool(10000, 1024),
)
```

`PacketBuffer` 实现引用计数：

```go
func (buf *PacketBuffer) Free() {
    if buf.data != nil && buf.subRefCount() == 0 {
        buf.pool.Free(buf.data)   // 归还到 slab pool
        buf.data = nil
    }
}
```

### 八、心跳机制

| 通道 | 间隔 | 超时 | 方向 |
|------|------|------|------|
| Client ↔ Agent | 客户端主动 | 30 秒读超时 | 单向 |
| Agent ↔ Backend | 18 秒双向 Ping | 20 秒无响应断开 | 双向 |

### 九、负载均衡

`backend.go:590-610`

```go
func (bs *BackendGroup) GetSession(id uint32) *BackendSession {
    if id > 0 {
        return bs.backends[id]          // 指定后端 → 直接查找
    }
    idx := bs.backendRander.Uint32n(uint32(sz))  // 未指定 → 随机选
    return bs.backends[bs.allBackendIDs[idx]]
}
```

Login 服务 `backendID=0` → 随机分配；Game/Chat 服务 `backendID>0` → 精确路由。

### 十、完整消息生命周期

```
[1] 客户端 TCP Connect(:9190) → Agent Accept → NewFrontendSession
    ├─ go Serve()          ← 读循环
    └─ go waitResponse()   ← 写循环

[2] 客户端发送登录包（XOR/RC4 加密）
    │
[3] FrontendSession.ReadPacket()
    │  ├─ 读4字节头 → 解析长度
    │  └─ 从 slab pool 分配 → 读剩余数据
    │
[4] FrontendSession.OnMessage()
    │  ├─ Decrypt()        解密
    │  ├─ checkSignature() 验签
    │  └─ Forward()        转发
    │     ├─ 查 BackendGroup → GetSession(负载均衡)
    │     ├─ BindBackendSession() (首次)
    │     ├─ SetConnID(clientID)
    │     └─ backend.Write(明文)
    │
[5] Agent → Backend TCP(:9191) 明文传输
    │
[6] BackendSession.ReadRequest() → go OnMessage()
    │  └─ Router.Dispatch() → Module.Action.Handle()
    │     └─ 业务逻辑 → 返回 Protobuf Response
    │
[7] Backend → Agent 明文返回
    │
[8] BackendSession.OnMessage()
    │  ├─ GetFrontendGroup(connID).Get(connID)
    │  ├─ Encrypt()        加密
    │  └─ WriteAsync()     写入 wrBuffer channel
    │
[9] FrontendSession.waitResponse()
    │  ├─ <-wrBuffer
    │  ├─ Write(TCP socket)
    │  └─ Free(PacketBuffer)  归还内存池
    │
[10] 客户端收到加密响应 → 解密 → 处理
```

### 十一、关键设计亮点

1. **Agent 无状态转发**：Agent 不碰业务逻辑，纯做路由/加解密/广播分发，可以水平扩展
2. **读写分离双 goroutine**：每个客户端连接 2 个 goroutine（读 + 写），通过 channel 解耦，避免写阻塞影响读
3. **slab 内存池**：PacketBuffer 引用计数 + slab 预分配，避免频繁 GC
4. **分桶锁**：64 桶将万级连接的锁竞争降低到百级
5. **边界加密**：只在 Agent ↔ 客户端 边界加密，内网明文传输，省 CPU
6. **CMD 包禁止**：客户端不允许发送控制命令包（Ping/Register/Kickout），从协议层防注入
7. **服务切换自动重绑**：Login → Game 时自动解绑旧 Backend + 通知断开 + 绑定新 Backend

### 十二、可优化点

#### 1. XOR 加密过于薄弱

```go
// xor secret 仅来自 MID/AID（2字节）
secret := []byte{packet.GetProtoMID(), packet.GetProtoAID()}
```

XOR 密钥只有 2 字节，且直接派生自包头明文字段（MID/AID），等于**没有加密**。攻击者抓一个包就能推导出 key。建议：
- 生产环境统一使用 RC4
- 或者升级到 AES-GCM/ChaCha20（性能与 RC4 接近，安全性高得多）

#### 2. RC4 每包重建 Cipher

```go
func (c *rc4Crypto) encryptOrDecryptDataLoad(packet Packet) {
    p, _ := rc4.NewCipher(c.secret)      // 每包 new 一次
    p.XORKeyStream(packet[index:], packet[index:])
}
```

每个包都 `rc4.NewCipher()`，这不仅有性能开销，而且使用固定 key 的 RC4 本身已被认为不安全（相同明文产生相同密文）。正确做法是维持一个有状态的 RC4 流，或改用现代密码。

#### 3. BackendSession.OnMessage 用 `go` 丧失顺序性

```go
// backend.go:277
go s.OnMessage(inRequest)    // ← 每个响应都起 goroutine
```

后端返回的响应被异步分发，同一客户端的多个响应可能乱序到达。对于战斗战报等有序数据，可能导致客户端帧顺序错乱。建议对同一 `ConnID` 的响应保持顺序，可以按 `ConnID` hash 到固定 worker。

#### 4. WriteAsync 溢出直接丢弃

```go
// frontend.go:233-247
func (s *FrontendSession) WriteAsync(buf *packet.PacketBuffer) {
    select {
    case s.wrBuffer <- buf:
    default:
        buf.Free()
        zaplog.S.Errorf("write buffer full")   // ← 丢弃！
    }
}
```

`wrBuffer` 容量仅 120，高频广播时容易满。被丢弃的包可能是关键的战斗战报或状态同步。建议：
- 增大缓冲区
- 或者区分优先级，关键包（战报、状态同步）使用阻塞写
- 满了触发踢人而不是静默丢弃

#### 5. FrontendSession ID 回收池容量偏小

```go
cidBuffer = make(chan uint32, 1000)   // 仅 1000 个可回收 ID
```

高并发断开/重连时，ID 回收池满了溢出的 ID 就浪费了，`cidCounter` 会单调递增直到溢出 `uint32`。可适当增大池容量或改用 `sync.Pool`。

#### 6. 广播时逐个复制包

```go
// backend.go:411-427
for _, v := range clients {
    newReq := req.New()
    newReq.Alloc(dataSize)
    copy(newPacket, ...)          // 每个客户端复制一份完整包
    s.writeFrontend(newReq)
}
```

100 人广播 = 复制 100 份包。可优化为共享数据体 + 仅复制包头（因为只有 ConnID 和加密结果不同），减少内存分配和 copy。

#### 7. 心跳缺少 RTT 度量

当前心跳只做存活检测（超时断开），没有记录 RTT。可以在 Ping/Pong 中携带时间戳，用于监控网络质量和负载均衡决策。

