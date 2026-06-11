# Goroutine 笔记

## 目录

- [[#Q1: goroutine 真的零成本吗？runtime 真的全兜底吗？]]
- [[#Q2: 推荐几个高 star、成熟的开源 Go 项目]]
- [[#Q3: 什么是分布式 SQL 数据库？]]
- [[#Q4: Raft 是什么？]]

---

## Q1: goroutine 真的零成本吗？runtime 真的全兜底吗？

> 用户判断：goroutine 创建成本约 2KB 栈，会自动增长；GMP 内置 work-stealing；缓存行、堆分配 runtime 全兜了。

**结论：方向对，但"全兜了"是错觉。runtime 解决的是"易用性"，没解决"正确性和性能上限"。**

### 一、2KB 栈 + 自动增长：便宜但不免费

- 初始栈 2KB（Go 1.4 之后），存在 `g` 结构本身 + 栈空间，合计约 2~4KB。
- **栈增长 = 整段栈拷贝**：当栈不够时，runtime 会分配一块 2 倍大的新栈，把旧栈逐字节拷过去，并修复栈上所有指针（stack copy + pointer adjustment）。
  - 递归深、局部大数组、热路径上反复触发栈增长 → 拷贝成本不可忽略。
  - 表现为 CPU profile 里出现 `runtime.morestack` / `runtime.copystack`。
- 栈**不会主动收缩到比当前更小**（GC 时才有机会 shrink），一个跑过深栈的 goroutine 会"记住"较大的栈占用。
- 百万 goroutine 场景：哪怕每个只占 8KB，也是 8GB 起步，远不是"零成本"。

### 二、GMP + work-stealing：好用但有边界

- work-stealing 解决的是**就绪 G 的负载均衡**，不解决以下问题：
  1. **长任务 goroutine 霸占 P**：Go 1.14 之前没有抢占式调度，纯 CPU 循环会阻塞同一 P 上的其他 G；现在基于信号抢占，但成本不为零，且仍有边界情况。
  2. **syscall 阻塞会 detach M**：阻塞型 syscall 会触发 `handoffp`，runtime 创建/复用新的 M 接管 P。频繁阻塞 syscall → M 数量膨胀 → 线程切换开销 + 内存浪费。
  3. **GOMAXPROCS 默认 = CPU 核数**，但容器场景下经常拿到宿主机核数（除非用 `automaxprocs`），导致严重过度并行、上下文切换爆炸。
  4. **全局队列锁竞争**：work-stealing 本地队列满了才入全局队列，但触发后会有 `sched.lock` 争用。
  5. **goroutine 泄漏**：调度器不会帮你关掉一个永远阻塞在 channel 上的 G，泄漏的 G 会持续占栈、阻止 GC 回收其引用对象。

### 三、缓存行、堆分配：runtime **没有**完全兜底

- **False sharing 不会自动消失**：runtime/编译器**不会**为你的 struct 自动 padding。高并发计数器、per-CPU 结构需要你自己用 `[64]byte` padding 或 `sync/atomic` + 分片。
  - 标准库里 `sync.Pool`、`runtime.mheap` 等是手工 padding 的，业务代码不会自动获得这种优化。
- **逃逸分析不完美**：
  - `interface{}` 装箱、闭包捕获、返回局部指针、`fmt.Sprintf` 这类都会让对象逃逸到堆。
  - `go func(){ ... }()` 本身会让闭包捕获的变量逃逸（goroutine 生命周期超过当前栈帧）。
- **GC 压力**：goroutine 多 → 栈扫描成本 ↑（每个 G 的栈都要扫）；分配多 → write barrier + mark 阶段 CPU 占用 ↑。GC 调优旋钮就 `GOGC`、`GOMEMLIMIT` 几个，复杂场景仍要靠减少分配。
- **STW 仍然存在**：mark termination、sweep termination 阶段是 STW，虽然短（百微秒级），但延迟敏感场景仍要算账。

### 四、真正需要你自己负责的事

| 维度 | runtime 兜底 | 你要做的 |
|---|---|---|
| G 调度 | ✅ work-stealing | 控制 G 数量（worker pool / semaphore），别无限 `go func()` |
| 栈管理 | ✅ 自动增长 | 避免递归过深、大局部对象 |
| syscall | ✅ M handoff | 用非阻塞 IO / netpoller，少 cgo |
| 内存分配 | ✅ mcache/mcentral | 减少逃逸，复用对象（`sync.Pool`） |
| 并发安全 | ❌ | mutex / channel / atomic 自己选 |
| 缓存行 | ❌ | 自己 padding，避免 false sharing |
| 生命周期 | ❌ | `context` 控制取消，避免泄漏 |
| GOMAXPROCS | ❌（容器场景） | 用 `uber-go/automaxprocs` |

### 五、一句话

> runtime 让你**敢**写 10 万个 goroutine，但不保证你**应该**这么写。 廉价不等于免费，自动不等于最优。

---

## Q2: 推荐几个高 star、成熟的开源 Go 项目

> 按"学什么 / 用什么"分层，不堆名单。每个项目附**为什么值得看**和**重点目录**。

### 一、想学"工程化代码组织"（先从这里开始）

| 项目 | Star | 看什么 |
|---|---|---|
| **[spf13/cobra](https://github.com/spf13/cobra)** | ~37k | Go CLI 事实标准。看 `command.go` 怎么把命令树 + flag + 自动补全做得这么干净。k8s、gh、hugo、helm 都用它。 |
| **[cli/cli](https://github.com/cli/cli)** （GitHub CLI） | ~37k | 用 cobra 搭出的真实大型 CLI，分包清晰：`pkg/cmd/<verb>` 一个文件一个子命令，是企业 Go 项目布局的范本。 |
| **[gohugoio/hugo](https://github.com/gohugoio/hugo)** | ~75k | 静态站点生成器。模板引擎、并发文件处理、插件机制都很值得读。 |

### 二、想学"高性能网络 / Web 框架"

| 项目 | Star | 看什么 |
|---|---|---|
| **[gin-gonic/gin](https://github.com/gin-gonic/gin)** | ~78k | 代码量小（~5k 行核心），radix tree 路由实现是经典。**读完能完整理解一个 HTTP 框架。** |
| **[labstack/echo](https://github.com/labstack/echo)** | ~30k | 设计比 gin 更"接口化"，看中间件链怎么组合。 |
| **[gofiber/fiber](https://github.com/gofiber/fiber)** | ~34k | 基于 fasthttp，是研究"为什么 net/http 慢、怎么绕过 GC"的好对象。 |
| **[caddyserver/caddy](https://github.com/caddyserver/caddy)** | ~60k | 模块化 Web 服务器，看 ACME 自动续证、模块注册机制。 |

### 三、想学"分布式系统 / 一致性"

| 项目 | Star | 看什么 |
|---|---|---|
| **[etcd-io/etcd](https://github.com/etcd-io/etcd)** | ~48k | Raft 的工业实现典范。**raft 库 (`go.etcd.io/raft/v3`) 单独可读**，远比 hashicorp/raft 注释多。 |
| **[hashicorp/raft](https://github.com/hashicorp/raft)** | ~9k | 另一个 Raft 实现，consul/nomad 在用。和 etcd raft 对比着看收获很大。 |
| **[nats-io/nats-server](https://github.com/nats-io/nats-server)** | ~17k | 消息系统，JetStream 的存储层 + 集群协议是亮点，代码风格非常 Go-idiomatic。 |

### 四、想学"存储引擎"

| 项目 | Star | 看什么 |
|---|---|---|
| **[etcd-io/bbolt](https://github.com/etcd-io/bbolt)** | ~9k | **强烈推荐入门 KV 存储读这个**。单文件 mmap B+Tree，~5k 行可以读完，理解 page、COW、事务。 |
| **[dgraph-io/badger](https://github.com/dgraph-io/badger)** | ~15k | LSM-Tree 的 Go 实现，对照 RocksDB 看。 |
| **[pingcap/tidb](https://github.com/pingcap/tidb)** | ~38k | NewSQL，SQL parser + 分布式事务 + MVCC，工业级复杂度。 |
| **[cockroachdb/cockroach](https://github.com/cockroachdb/cockroach)** | ~31k | 同上，代码质量极高，注释和设计文档非常详细。 |

### 五、想学"容器 / 云原生底层"

| 项目 | Star | 看什么 |
|---|---|---|
| **[kubernetes/kubernetes](https://github.com/kubernetes/kubernetes)** | ~115k | 太大，**别从主仓入门**。从 `client-go`、`controller-runtime`、`kubelet` 单独切入。 |
| **[containerd/containerd](https://github.com/containerd/containerd)** | ~18k | 比 Docker 更适合读的容器运行时，分层清晰：runtime / snapshotter / content store。 |
| **[moby/moby](https://github.com/moby/moby)** | ~70k | Docker daemon，历史包袱重，主要看 API/CLI 设计。 |

### 六、想学"可观测性 / 监控"

| 项目 | Star | 看什么 |
|---|---|---|
| **[prometheus/prometheus](https://github.com/prometheus/prometheus)** | ~58k | TSDB 实现（`tsdb/` 目录）+ PromQL 解析执行，单独看任一部分都受益。 |
| **[grafana/loki](https://github.com/grafana/loki)** | ~24k | "日志版 Prometheus"，看怎么用对象存储做廉价日志系统。 |
| **[jaegertracing/jaeger](https://github.com/jaegertracing/jaeger)** | ~21k | 分布式追踪，OpenTelemetry 风格代码。 |

### 七、工具 / 网络代理（实用 + 易读）

| 项目 | Star | 看什么 |
|---|---|---|
| **[fatedier/frp](https://github.com/fatedier/frp)** | ~90k | 内网穿透。**多路复用、协议设计、控制流/数据流分离**讲得很清楚，中等规模适合通读。 |
| **[v2fly/v2ray-core](https://github.com/v2fly/v2ray-core)** | ~30k | 协议栈插件化设计的范本。 |
| **[jesseduffield/lazygit](https://github.com/jesseduffield/lazygit)** | ~57k | TUI 应用范本，看 `gocui` 怎么用。 |

### 八、必读"导航"仓库

- **[avelino/awesome-go](https://github.com/avelino/awesome-go)** ~135k — Go 生态全景图，找库先来这里。
- **[golang-standards/project-layout](https://github.com/golang-standards/project-layout)** ~50k — **注意：这不是官方标准**，是社区约定，争议较大，看看就好，别奉为圭臬。
- **[uber-go/guide](https://github.com/uber-go/guide)** ~13k — Uber 的 Go 风格指南，比官方 effective_go 更新更实用。

### 我的推荐阅读顺序

1. **第一周**：`bbolt`（5k 行读完，学存储） + `gin`（5k 行读完，学 Web 框架）
2. **第二周**：`cobra` + `cli/cli`（学怎么组织一个真实项目）
3. **第三周开始挑一个深扎**：
   - 想搞**基础设施** → etcd raft
   - 想搞**业务后端** → caddy 或 nats-server
   - 想搞**数据库** → cockroachdb 单模块（如 `pkg/kv`）
   - 想搞**云原生** → containerd

> 数字是约值（截至 2026 年 6 月），star 会变，但相对量级稳定。

---

## Q3: 什么是分布式 SQL 数据库？

### 一句话定义

**分布式 SQL 数据库 = 关系数据库的 SQL + ACID 事务，跑在一个可以水平扩展的分布式集群上。**

也常叫 **NewSQL**，定位是"既要 SQL 的强一致和易用，又要 NoSQL 的横向扩展能力"。

### 一、为什么会出现这类数据库

传统单机数据库到分布式有三条路线，前两条都有明显代价：

| 方案 | 问题 |
|---|---|
| **单机扩到极致**（MySQL 主从 / PG 主从） | 写只能走主库，单机磁盘/CPU/内存是天花板 |
| **分库分表**（ShardingSphere、MyCat） | 业务侵入：分片键改不了；跨片事务/JOIN 几乎做不了；DDL、扩容、平衡是噩梦 |
| **NoSQL**（Cassandra、MongoDB、HBase） | 放弃 SQL、放弃跨行事务、放弃强一致，换来扩展性 |

**分布式 SQL** 想拿到 SQL + ACID + 水平扩展三件套，代价是**延迟变高**（跨节点 RPC）和**架构复杂**。

### 二、核心能力清单

1. **SQL 协议兼容**：通常兼容 MySQL（如 TiDB、OceanBase）或 PostgreSQL（如 CockroachDB、YugabyteDB），应用几乎零改造接入。
2. **自动分片（Sharding/Region）**：表按主键 range 或 hash 自动切分成 region/tablet，分布到多个节点，**用户不需要指定分片键**。
3. **多副本强一致**：每个分片 3/5 副本，用 **Raft 或 Paxos** 同步，挂一台不丢数据、不停服。
4. **分布式事务**：跨节点 ACID，常见模型：
   - **2PC**（两阶段提交）
   - **Percolator**（Google 论文，TiDB 用的就是这个）
   - **Spanner 风格**（基于 TrueTime 的外部一致性）
5. **全局时钟**：保证事务顺序，几种实现：
   - **TSO**（中心化时间戳，TiDB 的 PD）
   - **HLC**（混合逻辑时钟，CockroachDB）
   - **TrueTime**（Google 自家原子钟 + GPS，Spanner）
6. **分布式 SQL 优化器**：要考虑数据位置（把计算下推到存储节点，减少网络传输）。
7. **在线扩缩容**：加机器后自动 rebalance，业务无感。

### 三、典型架构（以 TiDB 为例）

```
        ┌─────────────────────────────┐
        │  TiDB Server（SQL 层，无状态）│  ← 解析 SQL、做优化、生成执行计划
        └──────────────┬──────────────┘
                       │ kv 请求
        ┌──────────────▼──────────────┐
        │  TiKV（分布式 KV 存储）       │  ← 数据真正存这里，自动分片
        │  每个 Region 三副本 + Raft   │
        └──────────────┬──────────────┘
                       │
        ┌──────────────▼──────────────┐
        │  PD（Placement Driver）      │  ← 元数据管理、TSO、调度
        └─────────────────────────────┘
```

**计算与存储分离**是普遍架构（TiDB、Aurora、PolarDB 都是）。

### 四、代表产品对比

| 产品 | 出身 | 协议 | 共识 | 特点 |
|---|---|---|---|---|
| **Google Spanner** | Google | SQL | Paxos | 始祖，基于 TrueTime；外部不可用，AlloyDB/Cloud Spanner 才能用 |
| **CockroachDB** | 美国 | PostgreSQL | Raft | Spanner 开源仿品，去中心化（无 PD） |
| **TiDB** | PingCAP（中国） | MySQL | Raft | HTAP（OLTP + OLAP），TiFlash 列存副本 |
| **YugabyteDB** | 美国 | PostgreSQL/CQL | Raft | 文档+SQL 双模 |
| **OceanBase** | 阿里 | MySQL/Oracle | Paxos 变种 | TPC-C 世界纪录，金融场景多 |
| **TDSQL-C / PolarDB** | 腾讯/阿里 | MySQL/PG | 共享存储 | 严格说是"云原生数据库"，存算分离但不是 share-nothing |

### 五、什么时候**应该**用 / **不应该**用

**应该用：**
- 数据量超过单机容量（TB ~ PB 级）
- 需要跨行/跨表 ACID 事务
- 想保留 SQL 生态（ORM、BI 工具、报表）
- 高可用要求（金融、电商核心）

**不应该用：**
- 数据量 < 几百 GB，单机 MySQL/PG 完全够，别给自己找麻烦
- 微秒级延迟（HFT、广告竞价），分布式 RTT 跨不过去
- 纯 OLAP / 大宽表分析：用 ClickHouse、Doris、StarRocks
- KV/缓存场景：用 Redis

### 六、和"分库分表 + 中间件"的核心区别

| 维度 | 分库分表中间件 | 分布式 SQL |
|---|---|---|
| 分片键 | 业务定，且基本改不了 | 系统自动管理 |
| 跨片事务 | 弱（XA 性能差） | 原生支持 ACID |
| 跨片 JOIN | 几乎不可用 | 支持（虽然慢） |
| 扩容 | 手动迁移数据 | 自动 rebalance |
| 运维复杂度 | 高（业务+DBA 都要懂） | 集中化 |
| 性能上限 | 单片不受影响，单查询快 | 跨节点必有延迟开销 |

### 一句话总结

> 分布式 SQL 把"水平扩展"从业务层（分库分表）下沉到存储层，**让数据库自己解决分布式问题**，业务只管写 SQL。代价是延迟、运维复杂度、对网络的强依赖。

---

## Q4: Raft 是什么？

### 一句话定义

**Raft 是一种分布式共识算法（consensus algorithm），让一组机器对"日志序列"达成一致，即使部分节点宕机也能继续工作。** 出自斯坦福 2014 年论文《In Search of an Understandable Consensus Algorithm》，目标是**比 Paxos 更好理解**。

> "共识"的本质：让多台机器对"发生了哪些操作、按什么顺序发生"达成一致。这是构建强一致分布式系统（数据库、配置中心、消息队列）的基石。

### 一、解决什么问题

想象你要做一个 KV 存储，想要：
- **高可用**：挂一台不能停服
- **强一致**：客户端写完后再读，必须读到刚写的值
- **不丢数据**：commit 了就不能丢

单机做不到（挂了就完）；多机怎么保证 3 台数据都一样？这就是 Raft 解决的问题：**让 3/5 台机器表现得像一台机器**。

### 二、核心模型：复制状态机（Replicated State Machine）

```
   客户端 → ┌──────┐ → ┌──────┐ → ┌──────┐
            │ Log  │   │ Log  │   │ Log  │
            └──┬───┘   └──┬───┘   └──┬───┘
               ▼          ▼          ▼
            [状态机]   [状态机]   [状态机]
              (KV)      (KV)      (KV)
```

只要**每台机器按相同顺序执行相同的日志**，最终状态机的状态必然一致。
Raft 的工作就是：**保证所有节点的日志一模一样**。

### 三、三种角色 + 任期（Term）

| 角色 | 职责 |
|---|---|
| **Leader** | 唯一处理客户端写请求，复制日志给 Follower |
| **Follower** | 被动接收 Leader 的日志和心跳 |
| **Candidate** | 选举中间态：Follower 超时后变 Candidate，发起选举 |

- **任期（Term）**：递增的整数，相当于"第几届 Leader"。每次选举 term+1。
- 节点收到比自己 term 大的消息 → 立刻退回 Follower。
- term 是 Raft 安全性的逻辑时钟，所有 RPC 都带 term。

### 四、Raft 拆成三个子问题

#### 1. Leader Election（选主）

- Follower 在 **election timeout**（150~300ms 随机）内没收到 Leader 心跳 → 自己变 Candidate
- Candidate：term+1，给自己投一票，并行发 `RequestVote` 给所有节点
- 收到多数派（N/2+1）选票 → 成为 Leader，开始发心跳
- 同时多个 Candidate → 选票分裂 → 等待新一轮超时再选（随机超时降低再次冲突概率）

**关键**：随机超时 = Raft 简单可用的核心设计之一。

#### 2. Log Replication（日志复制）

```
客户端写 →  Leader 写本地 log（未 commit）
            ↓
            并行发 AppendEntries 给所有 Follower
            ↓
            多数派写入成功 → Leader commit → 应用到状态机 → 回客户端
            ↓
            下次 AppendEntries 带上新的 commitIndex
            → Follower 也 commit、应用到状态机
```

- 日志条目：`(term, index, command)`
- **commit 条件**：当前 term 的日志被多数派复制 → 可以 commit
- **apply**：commit 之后，按 index 顺序应用到状态机

#### 3. Safety（安全性）

Raft 用几条规则保证**已 commit 的日志永不丢失**：

| 规则 | 作用 |
|---|---|
| **Election Restriction** | Candidate 必须拥有所有已 commit 的日志，才能当选（投票方比较 lastLogTerm/lastLogIndex） |
| **Log Matching Property** | 如果两节点某条日志 `(term, index)` 相同，那这之前的日志全相同 |
| **Leader Only Appends** | Leader 永不覆盖/删除自己的日志，只追加 |
| **不 commit 旧 term 的日志** | 新 Leader 即使复制了上一任的日志到多数派，也不能直接 commit，要等本任新日志被 commit 才间接 commit（防止 Figure 8 那种回滚） |

### 五、只有两个核心 RPC

| RPC | 用途 |
|---|---|
| **RequestVote** | Candidate 拉票 |
| **AppendEntries** | Leader 复制日志 + 心跳（entries 为空就是心跳） |

外加可选的 **InstallSnapshot**（用于落后太多的 Follower 快速追赶）。

### 六、和 Paxos 的对比

| | Paxos | Raft |
|---|---|---|
| 理解难度 | 烧脑（原论文以晦涩著称） | 工程师一周能看懂 |
| 角色 | Proposer/Acceptor/Learner（流动） | Leader/Follower/Candidate（明确） |
| 日志顺序 | 多 Proposer 可乱序提交 | 强制 Leader 串行化 |
| 工业实现 | Chubby、Spanner、OceanBase | etcd、TiKV、Consul、CockroachDB、Nomad、RethinkDB |
| 实现难度 | 高（Multi-Paxos 工程化坑多） | 中（标准实现已成熟） |

**结论：新项目几乎都用 Raft，老项目（Google 系）用 Paxos 变种。**

### 七、Raft 不解决什么

容易误解的几点：

1. **Raft ≠ 高性能**：Leader 是瓶颈，所有写都过 Leader，单 group 写吞吐有上限。**横向扩展靠 Multi-Raft**（把数据切片，每片一个 Raft 组，TiKV、CockroachDB 都是这么干）。
2. **Raft ≠ 读一致性自动满足**：默认 Follower 读可能读到旧数据。要强一致读要么走 Leader（再加 ReadIndex 优化），要么用 Lease Read。
3. **Raft ≠ 解决拜占庭问题**：假设节点只会宕机/网络分区，不会作恶。区块链场景要用 PBFT/HotStuff。
4. **Raft ≠ 跨数据中心强一致**：跨 DC 部署延迟巨大，多数派要等最远的 DC 响应。要么牺牲延迟，要么搞 Multi-Raft + 就近部署。

### 八、工业实现推荐阅读顺序

1. **[hashicorp/raft](https://github.com/hashicorp/raft)** —— 代码量适中，consul/nomad 在用，**适合先读**
2. **[etcd-io/raft](https://github.com/etcd-io/raft)** —— TiKV/CockroachDB 都基于它改造，**生产级、注释最详细**，但抽象层次更高（不带网络/存储，纯算法库）
3. 论文《In Search of an Understandable Consensus Algorithm》—— 18 页可读完，强烈推荐看 Figure 8

### 一句话总结

> Raft = **强 Leader + 任期 + 日志复制 + 多数派投票**，用"牺牲灵活性换可理解性"的设计哲学，成为了 2015 年之后分布式系统的事实标准共识算法。
