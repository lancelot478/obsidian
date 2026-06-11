# bbolt 项目分析与问答记录

## 目录

- [[#Q1: bbolt 项目分析]]
- [[#Q2: 内存映射（mmap）零拷贝读取详细介绍]]
- [[#Q3: 从头学习 bbolt 的学习路径]]
- [[#Q4: 阶段 1 实操 黑盒 demo + cmd/bbolt CLI]]
- [[#Q5: demo 输出逐段详解]]
- [[#Q6: db.beginRWTx() 详解]]
- [[#Q7: mmap 在 bbolt 中的全部使用点]]

---

## Q1: bbolt 项目分析

### 1. 项目概述

**bbolt** 是由 etcd 社区维护的高性能嵌入式键值数据库，2016 年从 Ben Johnson 的 BoltDB 项目 fork 而来，目标是为 Go 生态提供可靠、稳定且高效的嵌入式数据库引擎。

**主要特性：**
- 纯 Go 实现，无外部依赖
- 基于 LMDB（Howard Chu）的架构设计
- 支持 ACID 语义的完整序列化事务
- 无锁 MVCC，单写者多读者模型
- 内存映射文件（mmap）优化读取性能
- 自动数据库恢复，无需显式恢复步骤

**项目应用：** etcd 将 bbolt 作为后端存储引擎，支持超过 1TB 的生产数据库，被 Shopify、Heroku 等公司使用。

---

### 2. 核心架构

#### 2.1 存储模型

bbolt 采用**单文件 B+ 树存储模型**，所有数据持久化到一个单一数据文件中：

- **B+ 树结构**：用于快速查找和范围扫描
- **页面式管理**：数据库按 4KB 页面为单位存储（可配置）
- **内存映射（mmap）**：整个数据文件通过操作系统 mmap 映射到虚拟内存，零拷贝读取
- **Copy-on-Write（CoW）**：读写事务间通过 CoW 实现隔离

#### 2.2 页面设计

数据库中有 4 种页面类型，定义在 `internal/common/page.go`：

```go
const (
    BranchPageFlag   = 0x01  // 分支页：B+ 树中间节点
    LeafPageFlag     = 0x02  // 叶子页：存储实际键值对
    MetaPageFlag     = 0x04  // 元数据页：数据库元信息
    FreelistPageFlag = 0x10  // 空闲列表页：跟踪可重用页面
)
```

**页面结构：**
- **Meta Page（元数据页）**：存储数据库版本、校验和、根 bucket 引用、freelist 位置等
- **Branch Page（分支页）**：B+ 树中间节点，每个元素包含 key 和指向子页的 page ID
- **Leaf Page（叶子页）**：B+ 树叶节点，存储实际的键值对
- **Freelist Page（空闲列表页）**：维护已删除但可重用的页面 ID 列表

#### 2.3 事务模型

bbolt 实现了 **MVCC（多版本并发控制）** 和 **单写者多读者（SWMR）** 模型：

| 事务类型 | 启动方式 | 并发度 |
|---|---|---|
| 只读事务 | `DB.View()` 或 `DB.Begin(false)` | 无限制并发 |
| 读写事务 | `DB.Update()` 或 `DB.Begin(true)` | 同时最多一个 |
| 批量写事务 | `DB.Batch()` | 合并多个并发 Update() |

**关键特性：**
- 每个事务具有一致的数据库视图（事务开始时的快照）
- 读写事务持有 `rwlock` 互斥锁确保序列化
- 旧的只读事务保持打开时，相应页面无法回收
- 两阶段提交保证 crash-safety

---

### 3. 关键概念与组件

#### 3.1 核心对象

| 对象 | 说明 | 关键文件 |
|------|------|--------|
| **DB** | 数据库实例，代表单个磁盘文件 | db.go |
| **Tx** | 事务对象，提供读写操作接口 | tx.go |
| **Bucket** | 键值对集合，可嵌套 | bucket.go |
| **Cursor** | 用于遍历 B+ 树的迭代器 | cursor.go |
| **Node** | 内存中的反序列化页面 | node.go |

#### 3.2 Freelist 管理

Freelist 在 `internal/freelist/` 目录实现，支持两种后端：

- **Array Type**：简单但在大型数据库中性能衰减明显
- **HashMap Type**：更快的分配速度，推荐用于大型数据库

**关键操作：**
- `Allocate()`：分配 N 个连续页面
- `Free()`：归还页面给 freelist
- `ReleasePendingPages()`：释放已闭合只读事务持有的页面

#### 3.3 锁机制

DB 结构中的锁定策略（db.go）：

```go
rwlock   sync.Mutex   // 只允许一个写入者
metalock sync.Mutex   // 保护 meta page 访问
mmaplock sync.RWMutex // 保护 mmap 重映射操作
statlock sync.RWMutex // 保护统计信息访问
```

---

### 4. 目录结构

```
bbolt/
├── cmd/bbolt/              # 命令行工具
│   ├── command/            # 各子命令实现（info, check, stats 等）
│   └── main.go
├── internal/
│   ├── common/             # 通用数据结构（Page, Meta, Inode 等）
│   ├── freelist/           # 空闲列表实现（array 和 hashmap）
│   ├── btesting/           # 测试框架
│   ├── guts_cli/           # 数据库内部检查工具
│   ├── surgeon/            # 数据库修复工具
│   └── tests/              # 集成测试
├── errors/                 # 错误定义
├── tests/
│   ├── dmflakey/           # 块设备故障模拟
│   ├── failpoint/          # 故障注入点
│   ├── robustness/         # 鲁棒性测试
│   └── utils/              # 测试工具函数
├── scripts/                # 构建和分析脚本
├── CHANGELOG/              # 版本更新记录
├── db.go, tx.go, bucket.go, cursor.go, node.go  # 核心实现
└── go.mod, Makefile        # 构建配置
```

---

### 5. 使用方式示例

#### 5.1 基本打开和关闭

```go
import bolt "go.etcd.io/bbolt"

db, err := bolt.Open("my.db", 0600, nil)
if err != nil {
    log.Fatal(err)
}
defer db.Close()
```

#### 5.2 读写事务

```go
// 读写事务
err := db.Update(func(tx *bolt.Tx) error {
    b := tx.Bucket([]byte("MyBucket"))
    return b.Put([]byte("answer"), []byte("42"))
})

// 只读事务
err = db.View(func(tx *bolt.Tx) error {
    b := tx.Bucket([]byte("MyBucket"))
    v := b.Get([]byte("answer"))
    fmt.Printf("Value: %s\n", v)
    return nil
})
```

#### 5.3 遍历键值对

```go
db.View(func(tx *bolt.Tx) error {
    b := tx.Bucket([]byte("MyBucket"))
    c := b.Cursor()
    for k, v := c.First(); k != nil; k, v = c.Next() {
        fmt.Printf("key=%s, value=%s\n", k, v)
    }
    return nil
})
```

#### 5.4 嵌套 bucket

```go
db.Update(func(tx *bolt.Tx) error {
    root := tx.Bucket([]byte("accounts"))
    b, _ := root.CreateBucketIfNotExists([]byte("user_123"))
    return b.Put([]byte("name"), []byte("Alice"))
})
```

---

### 6. 特性与限制

#### 6.1 优势

- **简洁 API**：Get/Put/Delete 等基本操作
- **完整事务**：ACID 语义，序列化隔离级别
- **无后台进程**：嵌入式设计，无额外依赖
- **快速读取**：mmap 零拷贝，特别适合读密集场景
- **单文件部署**：便于备份和迁移

#### 6.2 限制与注意事项

**性能限制：**
- 随机写入性能较低（>10,000 写/秒时考虑 LevelDB）
- B+ 树导致频繁的随机页面访问，SSD 推荐
- 长时间只读事务阻止页面回收，导致数据库文件增长

**数据约束：**
- Key 最大 32KB，Value 最大 2GB
- 字节切片返回值仅在事务期间有效
- 同一 goroutine 中避免嵌套事务（可能死锁）

**系统限制：**
- 单进程独占锁，多进程无法并发写入
- Mmap 文件需适应进程虚拟地址空间（32 位系统受限）
- 数据库文件按小端序存储，不同架构间无法直接迁移
- 无法回收删除数据占用的磁盘空间（Freelist 重用）

**已知问题：**
- Linux ext4 fast commit 特性可能导致数据损坏（需内核 5.10.94+、5.15.17+ 补丁）
- 写入长度为 0 的值会返回空 `[]byte{}` 而非 nil
- 初始化阶段突然断电可能损坏空白数据库

---

### 7. 命令行工具

bbolt 提供丰富的 CLI 工具（cmd/bbolt）用于数据库管理和调试：

| 命令 | 功能 |
|------|------|
| `info` | 显示数据库基本信息（页面大小等） |
| `buckets` | 列出所有 bucket |
| `check` | 验证数据库完整性 |
| `stats` | 统计页面使用情况和 B+ 树信息 |
| `pages` | 列出所有页面及其类型 |
| `page` | 以可读格式显示单个页面内容 |
| `keys` | 列出 bucket 中的所有 key |
| `get` | 获取指定 key 的值 |
| `compact` | 压缩数据库（复制到新文件并去碎片） |
| `bench` | 性能基准测试 |
| `surgery` | 数据库修复工具（修复 meta 页、freelist 等） |

---

### 8. 提交流程详解

bbolt 的事务提交是精心设计的两阶段过程（tx.go）：

1. **Spill 阶段**：递归序列化内存中的节点到脏页
2. **Allocate 阶段**：为 freelist 分配新页面
3. **Write 阶段**：将脏页写入磁盘并 fsync
4. **Meta Write 阶段**：写入新元数据页并 fsync

这种设计保证即使在 crash 发生时，部分写入的数据页也不会被应用，因为指向它们的 meta 页未被写入。

---

### 9. 代码规模与质量

- **总代码量**：约 14,725 行 Go 代码
- **核心文件**：37 个 .go 文件
- **测试覆盖**：完整的单元测试和集成测试
- **依赖最小**：仅依赖 Go 标准库和部分 etcd 相关包

---

### 总结

bbolt 是一个经过生产验证、设计精良的嵌入式数据库库，适合对可靠性、简洁性有高要求的 Go 应用。其 B+ 树结构、MVCC 设计和 mmap 优化使其成为 etcd 等系统的理想选择，但随机写入性能和文件不可压缩是其主要限制。

---

## Q2: 内存映射（mmap）零拷贝读取详细介绍

### 1. 总体思路：把整个 .db 文件映射成一段只读字节数组

bbolt 在 `DB` 结构里持有三个核心字段（`db.go:127-129`）：

```go
dataref []byte                   // mmap 出来的原始 []byte（用于 munmap）
data    *[common.MaxMapSize]byte // 把同一段内存重解释成超大数组指针
datasz  int                      // 当前映射的字节数
```

- `dataref`：`unix.Mmap` 返回的原生切片，保留它是为了之后 `unix.Munmap` 还能拿到正确的 base/len
- `data`：把同一块虚拟内存强转成 `*[MaxMapSize]byte`（amd64 上是 256TB），让 Go 编译器允许任意偏移下标 `db.data[pos]`，无须切片再切片
- `datasz`：真实可用的映射长度

---

### 2. mmap 调用本身（`bolt_unix.go:55`）

```go
func mmap(db *DB, sz int) error {
    b, err := unix.Mmap(int(db.file.Fd()), 0, sz,
        syscall.PROT_READ,                  // 只读映射
        syscall.MAP_SHARED|db.MmapFlags)    // 共享：与文件保持一致
    ...
    err = unix.Madvise(b, syscall.MADV_RANDOM) // 提示内核：随机访问，不要预读
    db.dataref = b
    db.data    = (*[common.MaxMapSize]byte)(unsafe.Pointer(&b[0]))
    db.datasz  = sz
}
```

**三个关键决策：**

| 标志 | 作用 |
|---|---|
| `PROT_READ` | mmap 区域只读。写入数据**不**走脏页回写——bbolt 改用显式 `pwrite`。这样避免对 mmap 写入触发 SIGBUS / SIGSEGV，也让 fsync 语义可控 |
| `MAP_SHARED` | 与文件系统页缓存共享同一份物理页（这是「零拷贝」的根基），fsync 写入对本进程立即可见 |
| `MADV_RANDOM` | B+ 树查找是随机访问，关闭 Linux 默认的预读启发式（128KB），避免无意义的脏读 |

**Windows 实现**（`bolt_windows.go:67-103`）：使用 `CreateFileMapping` + `MapViewOfFile`，语义对等；区别是 Windows 必须**先把文件 truncate 到映射大小**，否则映射失败。

---

### 3. 零拷贝读取：通过指针运算定位 page

页定位只是一次乘法 + 一次类型转换（`db.go:1130-1133`）：

```go
func (db *DB) page(id common.Pgid) *common.Page {
    pos := id * common.Pgid(db.pageSize)
    return (*common.Page)(unsafe.Pointer(&db.data[pos]))
}
```

返回的 `*common.Page` **直接指向 mmap 区域**：
- 没有 `read(2)` 系统调用
- 没有 buffer copy
- 没有 GC 分配

B+ 树叶节点里的 key/value 切片同样是指向 mmap 的 sub-slice——这就是 README 里说的 "zero-copy"。

**代价**：这些切片的生命周期被绑定到事务上。事务结束后 mmap 可能被 remap，所以 bbolt 反复强调不要把 `Get` 返回的字节带出事务。

---

### 4. mmap 大小怎么算（不是文件多大映射多大）

`db.mmapSize` 实现指数增长（`db.go:581-612`）：

```
size <= 32KB  → 32KB
size <= 64KB  → 64KB
...
size <= 1GB   → 1GB
size  > 1GB   → 按 MaxMmapStep (1GB) 取整向上
最大          → MaxMapSize
```

**各架构 MaxMapSize：**

| 架构 | 上限 |
|---|---|
| amd64 / arm64 / ppc64 / s390x / riscv64 / loong64 | 256TB |
| mips64 | 512GB |
| 386 / arm / ppc / mips | 1~2GB |

**为什么映射比文件还大？** mmap 在 Linux 上只是建立 VMA，并**不会立刻分配物理页或扩大文件**——访问到再触发缺页才真正映射。预先开一段大地址空间，可避免每次写事务文件长大就重映射、读事务被打断。这也是 `Options.InitialMmapSize` 的作用：把起始大小拉高，让长期运行的读事务不被 remap 阻塞（`db.go:1353-1367`）。

> 32 位架构 `MaxMapSize=2GB` 是虚拟地址空间硬限制，因此 bbolt 在 32 位上单库上限就是 2GB。

---

### 5. 增长时的 remap 流程（`db.go:456-553`）

写事务发现需要的页超过当前 `datasz` 时会调用 `db.mmap(minsz)`：

1. **`db.mmaplock.Lock()`**：写锁，同时阻塞所有读事务（读事务在 `Begin` 时拿读锁）
2. **`rwtx.root.dereference()`**：让当前写事务里仍指向旧 mmap 的 node 把数据 memcpy 到堆，因为 munmap 之后那些指针就失效了
3. **`db.munmap()` → `mmap(db, newSize)`**：先解映射再重新映射
4. **重新拿 `meta0/meta1`** 指针并 `Validate()`

`mmaplock` 是这个机制的关键——它使 read tx 和 remap 互斥，但读事务彼此并行。所以官方建议把 `InitialMmapSize` 调大以避免被打断。

---

### 6. 写为什么不走 mmap（重要）

`Tx.write()`（`tx.go:520-572`）把脏页通过 `pwrite` 直接写到文件 fd：

```go
if _, err := tx.db.ops.writeAt(buf, offset); err != nil { ... }
...
fdatasync(tx.db)
```

**不复用只读 mmap 写的两个原因：**

1. mmap 区域是 `PROT_READ`，写会 SIGSEGV——这是刻意的，避免 bug 直接踩坏数据文件
2. 写入路径需要严格控制 fsync 时机（CoW + meta page 双写以保证 crash safe），让脏页回写交给内核会丧失对落盘顺序的控制

> **核心 IO 模型**：读零拷贝、写显式 pwrite + fsync。写入完成后内核会让 mmap 区域的页缓存与新写入保持一致（MAP_SHARED 语义），下一次读就能直接看到新数据，无需重新映射。

---

### 7. 可选的 mlock

`Options.Mlock=true` 时，每次 mmap 后会调用 `mlock` 把整段映射钉在物理内存里（`db.go:625-633`），避免被 swap 出去，对延迟敏感的场景有用，代价是常驻物理内存。

---

### 8. 一句话总结

bbolt 把数据文件以 `PROT_READ | MAP_SHARED` 整体映射进虚拟地址空间，B+ 树遍历就是指针算术 `(*Page)(&db.data[pgid*pageSize])`，cursor 返回的 key/value 切片直接指向页缓存——读路径**零拷贝、零分配、零 syscall**；写路径反过来走显式的 `pwrite + fdatasync`，靠 `mmaplock` 协调 remap 与读事务，靠几何增长的映射尺寸 + 大 `InitialMmapSize` 减少 remap 频率。

---

## Q3: 从头学习 bbolt 的学习路径

### 学习思路总览

```
阶段0 心智模型 → 阶段1 黑盒会用 → 阶段2 磁盘格式 → 阶段3 mmap+IO
   → 阶段4 事务/MVCC/锁 → 阶段5 Bucket/Cursor/Node
   → 阶段6 Freelist → 阶段7 崩溃恢复 & 工具链
```

核心原则：**先读到结果（Get），再读写入（Put/Commit），最后读崩溃恢复**。每阶段都先看磁盘格式 → 再看内存结构 → 最后看代码流。

---

### 阶段 0：建立心智模型（半天）

不要直接打开 `db.go`。先记住四个词：

| 概念 | 一句话理解 |
|---|---|
| **mmap** | 文件以只读方式整体映射进进程地址空间，读 = 解地址 |
| **B+ 树** | 所有 KV 都活在 B+ 树叶子页里；中间页只放路由 key |
| **CoW** | 改一个页 = 分配新页 + 写新页，旧页保留给当前读事务 |
| **MVCC / SWMR** | 单写者 + 多读者，靠"切换 meta 页指针"完成原子提交 |

读：`README.md`（Bolt 章节）+ `doc.go`，不超过 1 小时。

---

### 阶段 1：黑盒会用（半天）

**目标**：能写 demo、能用 CLI 看 db 文件。

**实操**：

1. 写一个 demo：`Update` 创 bucket、写 KV；`View` + `Cursor.First/Next` 遍历
2. 用 `cmd/bbolt`：

   ```
   bbolt info my.db        # 页大小
   bbolt buckets my.db     # 顶层 bucket
   bbolt stats my.db <bk>  # B+ 树统计
   bbolt pages my.db       # 所有页类型
   bbolt page my.db 0      # dump meta page
   ```

3. 用 `bbolt page my.db 2` 看一个叶子页，肉眼看出 KV 物理布局

**只看**：`doc.go`、`bucket.go` 里 `Get/Put` 的函数签名。**先不读实现**。

---

### 阶段 2：磁盘格式（1 天）

这是 bbolt 最值得先吃透的一层——**之后所有代码都是在操作这些字节**。

| 文件 | 看什么 |
|---|---|
| `internal/common/page.go` | `Page` 头（id, flags, count, overflow），4 种 PageFlag |
| `internal/common/meta.go` | meta 页：magic / version / pageSize / root pgid / freelist pgid / txid / checksum |
| `internal/common/inode.go` | leaf/branch 页里 inode 的偏移布局（key/value 长度 + 偏移） |

**关键问题**（先自己试着回答）：

- 为什么 meta 页有两份（pgid=0 和 pgid=1）？
- branch page element 和 leaf page element 物理布局有什么差异？
- 一个超过单页大小的 value 怎么存（`overflow` 字段）？

**实操**：用 `bbolt page` 命令 dump 一个 meta、一个 branch、一个 leaf，对着 `page.go` 把字段画一张图。

---

### 阶段 3：mmap + IO 双通道（半天）

**目标**：理解 bbolt "读走 mmap、写走 pwrite" 的非对称模型。

| 文件 | 看什么 |
|---|---|
| `bolt_unix.go` | `mmap` / `munmap`（PROT_READ + MAP_SHARED + MADV_RANDOM） |
| `bolt_windows.go` | Windows 实现的差异（必须先 truncate） |
| `db.go:454-553` | `db.mmap` 重映射的完整流程 |
| `db.go:581-612` | `mmapSize` 几何增长策略 |
| `db.go:1130-1133` | `db.page()` 指针运算定位页 |
| `tx.go:520-572` | `tx.write()` 写路径走 `pwrite + fdatasync` |

可参考已有笔记 [[#Q2: 内存映射（mmap）零拷贝读取详细介绍]]。

---

### 阶段 4：事务 / MVCC / 锁（1.5 天，核心）

**目标**：理解一次 `Update` 从 `Begin` 到 `Commit` 的完整流。

**阅读顺序：**

1. `db.go` 中的锁定义和 `DB.Begin / beginTx / beginRWTx`
   - 4 把锁：`rwlock`（写互斥）、`metalock`（meta 页保护）、`mmaplock`（remap 与读事务互斥）、`statlock`
   - 读事务持有 `mmaplock.RLock` 直到 `Tx.Rollback`
   - 写事务持有 `rwlock` 直到 `Commit/Rollback`

2. `tx.go` 中的 `Tx.Commit`，按四阶段对照阅读：

   | 阶段 | 函数 | 作用 |
   |---|---|---|
   | Spill | `bucket.spill` → `node.spill` | 内存 node 树序列化成脏页，必要时分裂 |
   | Allocate freelist | `db.allocate` 给 freelist 分配新页 | 旧 freelist 页释放，新 freelist 写入 |
   | Write | `tx.write` | 所有脏页 `pwrite` + `fdatasync` |
   | Meta Write | `tx.writeMeta` | 写新 meta 页（txid+1）+ `fdatasync` |

3. 思考：为什么 meta 写在最后单独 fsync？回答这个问题就理解了 bbolt 的 crash safety

**关键问题**：

- 提交过程中任何一步崩溃，会发生什么？（提示：meta 双备份 + checksum 选 txid 较大且 valid 的那个）
- 为什么读事务能与写事务并行？（提示：CoW，旧页未被 freelist 回收）

---

### 阶段 5：Bucket / Cursor / Node（1.5 天）

**目标**：理解 B+ 树是怎么"长"出来的。

| 文件 | 重点 |
|---|---|
| `bucket.go` | `Bucket` 结构、嵌套 bucket、inline bucket（小 bucket 内嵌父叶子节点） |
| `cursor.go` | `seek` / `First` / `Next` / `Prev` 的 `elemRef` 栈维护 |
| `node.go` | `node` 是脏页的内存表示；`spill` 做分裂、`rebalance` 做合并 |

**阅读顺序**：先 `cursor.go`（最容易，纯读路径）→ 再 `bucket.Get/Put`（依赖 cursor）→ 最后 `node.spill / rebalance`（最绕）。

**关键问题**：

- 什么是 inline bucket？为什么需要它？
- node 树和 page 是什么关系？什么时机互转？（提示：读时按需 `node()`，提交时 `spill()`）
- B+ 树分裂在 bbolt 里发生在哪个函数、什么时机？

---

### 阶段 6：Freelist（半天）

**目标**：理解"删除的页去哪了，什么时候才能复用"。

| 文件 | 看什么 |
|---|---|
| `internal/freelist/freelist.go` | `Interface` 抽象 + 共有逻辑 |
| `internal/freelist/shared.go` | `pending`（按 txid 分组的待释放页） |
| `internal/freelist/array.go` | 简单实现：有序 pgid 数组扫连续段 |
| `internal/freelist/hashmap.go` | 大库优化：以"连续段长度"为 key 的 map |

**关键问题**：

- 为什么释放的页要先进 `pending`，不能立刻进 `free`？（答：可能仍有读事务持有它）
- `ReleasePendingPages` 在何时触发？（答：写事务开始时 + 旧只读事务最小 txid 推进时）
- 为什么"长读事务会让 db 文件膨胀"？

---

### 阶段 7：崩溃恢复 & 工具链（按需）

| 模块 | 用途 |
|---|---|
| `tx_check.go` | `Tx.Check` 在线一致性检查（页引用、freelist、桶链路） |
| `internal/guts_cli` | 底层 page 解析工具，`bbolt page` 的实现 |
| `internal/surgeon` | 数据库修复工具（重建 freelist、覆盖 meta 等） |
| `tests/robustness`、`tests/failpoint` | 故障注入测试，最能体现"哪些不变量必须守住" |
| `compact.go` | 复制重建 db、清理碎片 |

读 `tests/robustness/` 里的 case，能反过来验证你前几阶段对"不变量"的理解。

---

### 学习建议

**节奏**：阶段 0–3 大约 2–3 天；阶段 4–6 是硬核区，每阶段慢慢啃；阶段 7 可以跳读。

**两条原则**：

1. **先读测试再读实现**：`*_test.go` 是最好的"用法手册"。看完 `bucket_test.go` 再读 `bucket.go`
2. **看一个数据结构 → 立刻 dump 一个真实 db 验证**：理论 + 实物对照，比单纯读代码效率高一个量级

**常见弯路**：

- 一上来就读 `db.go` 1455 行 → 推荐反过来，先读 100 行的 `internal/common/meta.go`
- 跳过 mmap 直接读事务 → 不理解 `db.page(pgid)` 是怎么"凭空"拿到 page 的
- 把 `node` 当成 `Page` 的同义词 → 它们一个在堆、一个在 mmap，spill/rebalance 是它们的桥

---

## Q4: 阶段 1 实操 黑盒 demo + cmd/bbolt CLI

### 1. demo 程序

文件位置：`bbolt/examples/learn/main.go`，覆盖以下场景：

- `Update` 创 bucket + 批量写 KV
- `View` 单点 `Get`
- `Cursor.First/Next` 顺序遍历
- `Cursor.Seek` 定位 + 正向遍历
- 嵌套 bucket 写读

```go
package main

import (
	"fmt"
	"log"
	"os"

	bolt "go.etcd.io/bbolt"
)

const dbPath = "learn.db"

func main() {
	_ = os.Remove(dbPath)

	db, err := bolt.Open(dbPath, 0600, nil)
	if err != nil {
		log.Fatalf("open: %v", err)
	}
	defer db.Close()

	mustWrite(db)
	mustReadOne(db)
	mustIterate(db)
	mustNested(db)
}

func mustWrite(db *bolt.DB) {
	err := db.Update(func(tx *bolt.Tx) error {
		b, err := tx.CreateBucketIfNotExists([]byte("users"))
		if err != nil {
			return err
		}
		seed := map[string]string{
			"alice": "engineer", "bob": "designer",
			"charlie": "pm", "dave": "sre", "eve": "intern",
		}
		for k, v := range seed {
			if err := b.Put([]byte(k), []byte(v)); err != nil {
				return err
			}
		}
		return nil
	})
	if err != nil { log.Fatalf("write: %v", err) }
}

func mustReadOne(db *bolt.DB) {
	_ = db.View(func(tx *bolt.Tx) error {
		v := tx.Bucket([]byte("users")).Get([]byte("alice"))
		fmt.Printf("Get(alice) => %q\n", v)
		return nil
	})
}

func mustIterate(db *bolt.DB) {
	_ = db.View(func(tx *bolt.Tx) error {
		c := tx.Bucket([]byte("users")).Cursor()
		// 顺序遍历
		for k, v := c.First(); k != nil; k, v = c.Next() {
			fmt.Printf("  %-8s -> %s\n", k, v)
		}
		// Seek 定位
		for k, v := c.Seek([]byte("c")); k != nil; k, v = c.Next() {
			fmt.Printf("  seek-> %-8s -> %s\n", k, v)
		}
		return nil
	})
}

func mustNested(db *bolt.DB) {
	_ = db.Update(func(tx *bolt.Tx) error {
		root, _ := tx.CreateBucketIfNotExists([]byte("accounts"))
		sub, _ := root.CreateBucketIfNotExists([]byte("u_001"))
		_ = sub.Put([]byte("name"), []byte("Alice"))
		_ = sub.Put([]byte("email"), []byte("alice@example.com"))
		return nil
	})
}
```

运行：`cd examples/learn && go run .`，会在当前目录生成 `learn.db`。

---

### 2. 关键 API 摘要

| API | 何时用 |
|---|---|
| `bolt.Open(path, mode, opts)` | 打开/创建 db；进程级独占（flock） |
| `db.Update(fn)` | 读写事务，函数返回 nil 自动提交，否则回滚 |
| `db.View(fn)` | 只读事务，多个并发不阻塞 |
| `tx.CreateBucketIfNotExists(name)` | 幂等创建 bucket |
| `bucket.Put / Get / Delete` | 单点 KV 操作；返回的 []byte 仅事务期内有效 |
| `bucket.Cursor()` | 顺序遍历或区间扫 |
| `cursor.First / Next / Prev / Last / Seek` | 字典序导航 |
| `bucket.ForEach(fn)` | Cursor 的语法糖 |

**重要"踩坑提醒"**：

- `Get` / `Cursor` 返回的 []byte **指向 mmap 区域**，事务结束后可能被 remap，**不要把它带出 `View/Update`**。需要保留就 `append([]byte{}, v...)` copy 一份
- `Update` 的回调里 panic 或 return error 会回滚，但**不要在回调里启动 goroutine 操作同一 db**
- 同一 goroutine 不要嵌套 `Update`（互斥锁会自死锁）

---

### 3. cmd/bbolt CLI 实操

> 在 bbolt 仓库根目录运行（路径是相对 demo 生成的 db 文件）

#### 3.1 基本观察

```bash
go run ./cmd/bbolt info     examples/learn/learn.db
go run ./cmd/bbolt buckets  examples/learn/learn.db
go run ./cmd/bbolt keys     examples/learn/learn.db users
go run ./cmd/bbolt get      examples/learn/learn.db users alice
```

输出：

```
Page Size: 16384

accounts
users

alice
bob
charlie
dave
eve

engineer
```

#### 3.2 看页面（重点教学点）

```bash
go run ./cmd/bbolt pages examples/learn/learn.db
```

```
ID   TYPE       ITEMS  OVRFLW
0    meta       0
1    meta       0
2    leaf       1
3    leaf       2
4    free
5    free
6    freelist   2
```

观察点：

- **两份 meta 页（pgid=0, 1）** —— 双备份用于 crash recovery
- **`leaf` 页**装实际数据
- **`freelist` 页**记录哪些页可重用
- 出现 `free` 是因为多次写事务后，旧的 root/freelist 页被释放回收

#### 3.3 看 meta 页内容

```bash
go run ./cmd/bbolt page examples/learn/learn.db 0
```

```
Page ID:    0
Page Type:  meta
Version:    2
Page Size:  16384 bytes
Root:       <pgid=4>     # 当前根 bucket 所在页
Freelist:   <pgid=5>     # freelist 页
HWM:        <pgid=6>     # 已分配的最高 pgid
Txn ID:     2
Checksum:   789b474bf36f28fe
```

**这一页就是 bbolt 的"锚"**：所有访问都从 meta 出发，找到 root bucket → B+ 树查找。两份 meta 中选 `Txn ID` 较大且 checksum 校验通过的那份。

#### 3.4 stats：看到 inline bucket

```bash
go run ./cmd/bbolt stats examples/learn/learn.db users
```

```
Tree statistics
    Number of keys/value pairs: 5
    Number of levels in B+tree: 1
Bucket statistics
    Total number of buckets: 1
    Total number on inlined buckets: 1 (100%)
    Bytes used for inlined buckets: 145
```

**关键现象**：`users` bucket 被 **inline** 进父叶子页里——它本身没有独立的 page，而是作为一条 value 嵌在根 bucket 的 leaf 里。bbolt 对小 bucket 的优化，避免每个小 bucket 都浪费一页。

#### 3.5 一致性检查

```bash
go run ./cmd/bbolt check examples/learn/learn.db
# 输出: OK
```

`check` 会遍历 B+ 树、验证 freelist、确认每页只被一处引用。日常调试和写故障注入测试时常用。

---

### 4. 这阶段你应该能回答的 5 个问题

1. `db.Update` 和 `db.View` 的并发模型差异？（写互斥、读并发）
2. `Cursor.First/Next` 返回的 []byte 拿出事务后会怎样？（数据会失效）
3. 为什么 `pages` 命令会列出 2 个 meta 页？（双备份 + 选 txid 大者）
4. inline bucket 是什么？什么时候触发？（小 bucket 内嵌父叶子页，省一页）
5. `Page Size: 16384` 是哪里决定的？（操作系统默认页大小，bbolt 默认对齐到 OS pagesize）

---

## Q5: demo 输出逐段详解

把 `examples/learn/main.go` 的输出拆成 5 段，每段对应一个 bbolt 内部机制。

---

### 1. `== wrote 5 KVs into bucket 'users' ==`

对应代码：

```go
db.Update(func(tx *bolt.Tx) error {
    b, _ := tx.CreateBucketIfNotExists([]byte("users"))
    for k, v := range seed { // map[string]string
        b.Put([]byte(k), []byte(v))
    }
    return nil
})
```

**幕后流程：**

| 步骤 | 发生的事 |
|---|---|
| `db.Update` 入口 | 拿 `rwlock`（写互斥），开启写事务 `Tx`；复制当前 meta 页作为本事务起点 |
| `CreateBucketIfNotExists` | 在根 bucket 里查找 key=`users`；找到就拿出已有 bucket，找不到就分配新 bucket header（仍在内存 node，没落盘） |
| 5 次 `Put` | cursor 找插入点 → 把 KV 插入 leaf node 的 inode 列表 → 标记 node dirty |
| 回调返回 nil | 触发 `Tx.Commit` 四阶段：spill → allocate → write → meta write |

**重要细节**：

- Go `map[string]string` 的迭代顺序**随机**，但写到 bbolt 后键**按字典序**——B+ 树叶子要求 key 有序，每次 `Put` 二分查找 + 有序插入
- 5 个 `Put` 是**一个事务**：要么全成功（meta 切换），要么全失败（回调返 error 或 panic 时回滚）

---

### 2. `== Get(alice) => "engineer" ==`

```go
v := tx.Bucket([]byte("users")).Get([]byte("alice"))
```

**幕后流程：**

```
View 开始
  ↓ 拿 mmaplock.RLock + metalock 拷贝 meta（不阻塞其他读）
  ↓ tx.Bucket("users")：在根 bucket 的 leaf 里二分找到 "users" 这条记录
  ↓ 这条记录的 value 是子 bucket 的元数据（root pgid 或 inline body）
  ↓ Get("alice")：cursor 从子 bucket root 出发，B+ 树搜索（branch 二分 → leaf 二分）
  ↓ 命中 → 返回 inode 里 value 的 []byte
```

**关键点**：

- `Get` 返回的 `[]byte` **直接指向 mmap**（参见 [[#Q2: 内存映射（mmap）零拷贝读取详细介绍]]）—— 0 拷贝、0 分配
- 但代价：这片切片只在 `View` 回调内有效。**`fmt.Printf` 同步打印安全**，但绝不能存到外部变量给后面用
- 这次 `users` bucket 是 inline 的（Q4 stats 的 `inlined buckets: 100%`）：`users` 没有自己的 page，整个 bucket 的 leaf 数据嵌在根 bucket 的 leaf value 里——`Get("alice")` 实际是在**这块 inline body 内**做二分

---

### 3. `== iterate via Cursor.First/Next ==`

```
alice    -> engineer
bob      -> designer
charlie  -> pm
dave     -> sre
eve      -> intern
```

**为什么是这个顺序**：

- 严格按字典序：`alice < bob < charlie < dave < eve`
- **不是插入顺序**（map range 是乱的），是 B+ 树天然有序
- bbolt 没有"插入顺序"概念；要按时间排，自己用时间戳 / 自增 id 当 key

**Cursor 内部状态**：

```go
type Cursor struct {
    bucket *Bucket
    stack  []elemRef  // 维护从 root 到当前 element 的路径
}
```

| 操作 | 实现 |
|---|---|
| `First()` | 从 root 一路走最左 → 把每层 (page, index=0) 压栈 → 返回栈顶 leaf 的第一个 inode |
| `Next()` | 栈顶 index++；越界就 pop 上一层、index++，再下钻到最左叶子；都越界返回 nil |

所以 `Next` **不需要重新搜索**，O(1) 摊销代价——这是 bbolt 范围扫描快的根本原因。

5 个 KV 全在一个 leaf page（stats 显示 `levels in B+tree: 1`），这 5 次 `Next` 都是同 page 内 index++。

---

### 4. `== Seek('c') 之后正向 ==`

```
charlie  -> pm
dave     -> sre
eve      -> intern
```

```go
for k, v := c.Seek([]byte("c")); k != nil; k, v = c.Next() { ... }
```

**Seek 语义**：返回 **第一个 key ≥ target** 的 (k, v)（即 lower_bound）。

- target=`"c"`，二分找到 `"bob" < "c" < "charlie"` → 命中 `charlie`
- 之后 `Next` 顺着叶子页继续走

**前缀扫描的标准模式**：

```go
prefix := []byte("c")
for k, v := c.Seek(prefix); k != nil && bytes.HasPrefix(k, prefix); k, v = c.Next() {
    // 只处理 c 开头的 key
}
```

注意：

- `Seek` 找不到精确匹配**不返回 nil**，而是返回**下一个更大的 key**——前缀扫描必须自己加 `bytes.HasPrefix` 终止条件
- 找不到任何 ≥ target 的 key 才返回 nil（target 比所有 key 都大时）

---

### 5. `== nested bucket accounts/u_001 ==`

```
email  -> alice@example.com
name   -> Alice
```

```go
// 写
root, _ := tx.CreateBucketIfNotExists([]byte("accounts"))
sub, _  := root.CreateBucketIfNotExists([]byte("u_001"))
sub.Put([]byte("name"),  []byte("Alice"))
sub.Put([]byte("email"), []byte("alice@example.com"))

// 读
sub := root.Bucket([]byte("u_001"))
sub.ForEach(func(k, v []byte) error { ... })
```

**几个要点**：

1. **嵌套 bucket = bucket 套 bucket**：父 bucket 的 leaf 里有一条特殊记录，key=`u_001`、flag 标 `BucketLeafFlag`、value 存子 bucket 元数据（root pgid 或 inline body）
2. **物理上没有"路径"**：bbolt 不像文件系统存 `/accounts/u_001/`。要拿子 bucket 必须 `tx.Bucket("accounts").Bucket("u_001")` **逐层拆**
3. **输出仍是字典序**：`email < name`，再次验证"任何 bucket 的 ForEach/Cursor 都是有序的"
4. **存活范围**：子 bucket 句柄（`sub`）只在当前事务有效；事务结束必须重新拿
5. **inline 也适用**：`u_001` 才 2 个字段，必然是 inline bucket（嵌在 `accounts` 的 leaf value 里）

---

### 6. 总结对照表

| 输出段 | 触发的 bbolt 子系统 | 关键不变量 |
|---|---|---|
| `wrote 5 KVs` | 写事务 + B+ 树插入 + dirty node | 一个事务原子提交 |
| `Get(alice)` | 读事务 + cursor 二分 + mmap 切片 | 切片只在事务期内有效 |
| `Cursor.First/Next` | cursor 栈维护 | 字典序 + O(1) 摊销 Next |
| `Seek('c')` | cursor 二分定位 | lower_bound 语义，不精确匹配也返回更大键 |
| `nested bucket` | bucket-in-leaf + inline bucket | 嵌套必须逐层 `Bucket()` |

---

### 7. 练手小实验（强烈推荐）

把 `seed` 换成 100 个 KV（甚至 10000 个），重跑后观察：

```bash
go run ./cmd/bbolt stats examples/learn/learn.db users
```

观察点：

- `Number of levels in B+tree` 是否从 1 变成 2 → **B+ 树长高**
- `inlined buckets: 100%` 是否变成 0% → **inline → 独立页**的切换
- `Number of logical leaf pages` 出现非零值 → bucket 终于占用独立 page

这能直观看到 inline 阈值切换和 B+ 树分裂，比读源码快得多。

---

## Q6: db.beginRWTx() 详解

`beginRWTx` 只有 30 多行，但每一行对应一个核心设计决策——理解它就理解了 bbolt 的写事务模型。

### 1. 源码（`db.go:839-872`）

```go
func (db *DB) beginRWTx() (*Tx, error) {
    if db.readOnly {
        return nil, berrors.ErrDatabaseReadOnly
    }
    db.rwlock.Lock()                          // 写互斥（持续到 Commit/Rollback）
    db.metalock.Lock()                        // 保护 db 元字段
    defer db.metalock.Unlock()

    if !db.opened {
        db.rwlock.Unlock()
        return nil, berrors.ErrDatabaseNotOpen
    }
    if db.data == nil {
        db.rwlock.Unlock()
        return nil, berrors.ErrInvalidMapping
    }

    t := &Tx{writable: true}
    t.init(db)
    db.rwtx = t
    db.freelist.ReleasePendingPages()
    return t, nil
}
```

---

### 2. 逐步详解

#### 步骤 1：`db.readOnly` 检查

只读模式下打开 db 时拿的是**共享 flock**，多个进程能同时打开但谁都不能写。直接拒绝。

#### 步骤 2：`db.rwlock.Lock()` —— 写互斥的根本

```go
db.rwlock.Lock()  // 这把锁不在本函数内释放！
```

bbolt **「同一时刻只有一个写事务」** 的实现机制。

- **持有时长**：从这一行开始，**一直到 `Tx.Commit` 或 `Tx.Rollback` 调用 `tx.close()`** 才释放（`tx.go:357: tx.db.rwlock.Unlock()`）
- **影响**：第二个 goroutine 调用 `db.Update` 会**阻塞**在 `rwlock.Lock()` 直到第一个事务结束
- **粒度**：粗到「事务级」——bbolt 牺牲并发换设计简单和 MVCC 正确性

> 这是 bbolt 写吞吐上限的根源：单写者 + fsync 延迟。要高写吞吐用 LSM。

#### 步骤 3：`db.metalock.Lock()` —— 保护 DB 元字段

```go
db.metalock.Lock()
defer db.metalock.Unlock()  // 立即在函数退出时释放
```

**`metalock` 保护的是 `DB` 结构体字段**：`db.opened`、`db.data`、`db.rwtx`、`db.freelist`。

**对比 `beginTx`（只读）的拿锁顺序**（`db.go:792-836`）：

```go
db.metalock.Lock()       // 先 meta
db.mmaplock.RLock()      // 后 mmap 读锁
```

**`beginRWTx` 的顺序**：

```go
db.rwlock.Lock()         // 1. 写互斥
db.metalock.Lock()       // 2. meta
// 注意：写事务不主动拿 mmaplock
```

写事务**不拿 mmaplock**——它需要扩容文件、可能触发 remap，这时**自己**会去拿 `mmaplock.Lock()`（写锁）。如果在这里就拿了读锁，后面 remap 时会自死锁。

**锁顺序总规则**：

```
rwlock → mmaplock → metalock
```

#### 步骤 4-5：状态检查 + 失败时**单独**释放 rwlock

```go
if !db.opened {
    db.rwlock.Unlock()       // 必须显式释放 rwlock
    return nil, berrors.ErrDatabaseNotOpen
}
if db.data == nil {
    db.rwlock.Unlock()
    return nil, berrors.ErrInvalidMapping
}
```

`metalock` 有 `defer` 自动释放，但 `rwlock` 不能 defer——正常路径要把它陪到 Commit/Rollback。所以失败路径必须手动还 rwlock，否则 db 永远写死锁。

#### 步骤 6：`t.init(db)` —— 事务的快照初始化

`Tx.init`（`tx.go:47-65`）做的事：

```go
func (tx *Tx) init(db *DB) {
    tx.db = db
    tx.pages = nil

    // (1) 拷贝 meta：MVCC 快照的核心
    tx.meta = &common.Meta{}
    db.meta().Copy(tx.meta)

    // (2) 拷贝 root bucket header
    tx.root = newBucket(tx)
    tx.root.InBucket = &common.InBucket{}
    *tx.root.InBucket = *(tx.meta.RootBucket())

    // (3) 写事务专属
    if tx.writable {
        tx.pages = make(map[common.Pgid]*common.Page)
        tx.meta.IncTxid()
    }
}
```

**三个关键动作**：

| 动作 | 意义 |
|---|---|
| `db.meta().Copy(tx.meta)` | **MVCC 快照**：拿当前两份 meta 中 txid 较大且 valid 的那份，深拷贝给本事务 |
| 拷贝 `RootBucket` | 同样是快照——本事务对根 bucket 的修改不污染其他事务 |
| `tx.pages = map[Pgid]*Page{}` | **脏页缓存**：写事务对页的修改先存这里，commit 时统一 `pwrite` 落盘 |
| `tx.meta.IncTxid()` | txid+1。**只是本事务内部副本的 txid**，不影响 db。直到 commit 写新 meta 页时才生效 |

> 读事务也走 `Tx.init`，但跳过 `tx.pages` 和 `IncTxid`。

#### 步骤 7：`db.rwtx = t` —— DB 持有当前写事务的引用

需要用到的地方：

1. **mmap remap 时**（`db.go:504`）—— 写事务里指向旧 mmap 的 node 要先 `dereference` 成堆数据：
   ```go
   if db.rwtx != nil {
       db.rwtx.root.dereference()
   }
   ```
2. **freelist allocate 时** —— 知道当前 txid，标记新分配页归属
3. **stats / 调试** —— 区分「有/无活动写事务」

`tx.close()` 里清空：`tx.db.rwtx = nil`（`tx.go:356`）。

#### 步骤 8：`db.freelist.ReleasePendingPages()` —— 关键的「时机」设计

**功能**：把 freelist 里 `pending`（按 txid 暂存的"待释放页"）中**所有早于最小活跃只读事务的 txid** 的页，正式归还到 `free`，让它们可被分配。

**为什么不在上一次 Commit 时做？** Commit 时 `pending` 里的页可能仍被某个长读事务持有，释放给新写事务用会被读事务"看到"未来的数据，破坏快照隔离。

**为什么放在新写事务开始时做？** 此时刚拿到 metalock，能原子地：

1. 知道当前所有活跃只读事务的最小 txid（`db.freelist.readonlyTXIDs`）
2. 把所有 `tid < minReadTxid` 的 pending 页转 free
3. 新分配从这些 free 里挑

**实现**（`freelist/shared.go:141`）：

```go
func (t *shared) ReleasePendingPages() {
    sort.Sort(txIDx(t.readonlyTXIDs))
    minid := common.Txid(math.MaxUint64)
    if len(t.readonlyTXIDs) > 0 {
        minid = t.readonlyTXIDs[0]   // 最早的活跃只读事务
    }
    if minid > 0 {
        t.release(minid - 1)         // 释放比它早的所有 pending
    }
    for _, tid := range t.readonlyTXIDs {
        t.releaseRange(minid, tid-1) // 释放只读事务"之间"的空隙
        minid = tid + 1
    }
    t.releaseRange(minid, math.MaxUint64)
}
```

> 这就是「**长时间只读事务会让 db 文件膨胀**」的根源——只要有一个老旧的只读事务挂着，所有比它新的 pending 页都不能 release。

---

### 3. 锁的全生命周期

```
T0: db.Update(fn) 入口
T1:   beginRWTx
        rwlock.Lock()           ────────────┐
        metalock.Lock()  ──┐                │
        ...               │ defer 立即释放  │
        metalock.Unlock() ─┘                │
        rwtx = t                            │ 写事务持续整个生命周期
T2:   user fn(tx)                           │
        - tx.Bucket / Put / Delete          │
        - 期间可能触发 db.mmap()             │
            mmaplock.Lock() ────┐           │
            munmap+mmap         │ remap     │
            mmaplock.Unlock() ──┘           │
T3:   tx.Commit                             │
        - spill / write / writeMeta         │
        - tx.close()                        │
            rwtx = nil                      │
            rwlock.Unlock() ────────────────┘
```

---

### 4. beginRWTx vs beginTx 对照

| 维度 | beginRWTx（写） | beginTx（读） |
|---|---|---|
| 数量限制 | 同时只能 1 个 | 同时无限多个 |
| 拿的锁 | `rwlock`（写）+ `metalock`（短）| `metalock`（短）+ `mmaplock.RLock`（长）|
| 锁释放时机 | rwlock 在 close() 里 | mmaplock.RUnlock 在 close() 里 |
| `tx.pages` | 分配脏页 map | nil |
| `tx.meta.IncTxid()` | 是 | 否 |
| 是否注册到 db | `db.rwtx = t` | `freelist.AddReadonlyTXID(txid)` |
| `ReleasePendingPages()` | **是**（每次开新写事务） | 否 |
| 失败回退 | 手动 `rwlock.Unlock()` | 手动 `mmaplock.RUnlock()` + `metalock.Unlock()` |

**最大对称性**：写事务持有 `rwlock` 阻其他写事务；读事务持有 `mmaplock.RLock` 阻 remap。两者**不互相阻塞**——这就是 SWMR 的实现。

---

### 5. 常见踩坑

#### 坑 1：同 goroutine 嵌套 `Update` → 死锁

```go
db.Update(func(tx *bolt.Tx) error {
    return db.Update(func(tx2 *bolt.Tx) error { // 卡死在 rwlock.Lock()
        ...
    })
})
```

`sync.Mutex` 在 Go 里**不可重入**。第二次 `rwlock.Lock()` 在同一 goroutine 上必死锁。

#### 坑 2：`Update` 回调里启 goroutine 用 tx

```go
db.Update(func(tx *bolt.Tx) error {
    go func() {
        tx.Bucket(...).Put(...) // tx 可能已被关闭！
    }()
    return nil
})
```

`tx` 不可跨 goroutine。回调 return 后 `tx.close()` 立即执行。

#### 坑 3：长读事务阻塞 mmap remap

写事务 remap 时 `mmaplock.Lock()` 要等所有读事务结束。如果有一个开了 30 分钟的 `View`，**整个 db 写入挂起 30 分钟**。

→ 解决：设大 `Options.InitialMmapSize`，让 remap 极少发生。

#### 坑 4：`Begin(true)` 后必须显式 `Commit/Rollback`

```go
tx, _ := db.Begin(true)
defer tx.Rollback() // 必须！否则 rwlock 永远不释放
```

`db.Update` 包了这一层，所以更推荐用 `Update`。

---

### 6. 一句话总结

`beginRWTx` 通过 **「拿 rwlock → 拿 metalock 短临界区 → 拷贝 meta 做快照 → 注册到 db.rwtx → 回收 pending 页」** 五步完成写事务初始化；其中 rwlock 跨函数持有保证写者互斥，meta 拷贝实现 MVCC 隔离，`ReleasePendingPages` 选在「新写事务开始」这个时机做 GC 是为了让旧读事务的快照不被破坏。

---

## Q7: mmap 在 bbolt 中的全部使用点

按"辐射半径"分十大类梳理 mmap 在仓库里所有代码触点。

### 总览

```
平台 mmap 实现 (bolt_*.go)
        ↓
DB 结构体 mmap 字段 (dataref/data/datasz/mmaplock/MmapFlags)
        ↓
入口：Open 初始 mmap / 写时 remap / Close munmap
        ↓
读路径：db.page → tx.page → bucket
        ↓
remap 前：node/bucket.dereference
        ↓
协调：mmaplock RWMutex
        ↓
开关：Mlock / MmapFlags / InitialMmapSize
        ↓
常量：MaxMapSize / MaxMmapStep
```

---

### 1. 平台 mmap 实现层（5 份相似文件）

| 文件 | 平台 | 关键差异 |
|---|---|---|
| `bolt_unix.go:55` | Linux/macOS/FreeBSD | `unix.Mmap(fd, 0, sz, PROT_READ, MAP_SHARED)` + `MADV_RANDOM` |
| `bolt_windows.go:67` | Windows | `CreateFileMapping` + `MapViewOfFile`，**必须先 truncate** |
| `bolt_aix.go:60` | AIX | 同 unix 结构，编译标签隔离 |
| `bolt_solaris.go:58` | Solaris | 同上 |
| `bolt_android.go:58` | Android | 同上 |

每份只暴露 `mmap(db, sz)` / `munmap(db)`，平台细节被屏蔽。

---

### 2. DB 结构体里的 mmap 字段（`db.go:127-148`）

```go
dataref  []byte                    // 原始 mmap 切片，munmap 用
data     *[common.MaxMapSize]byte  // 同一段内存的「巨大数组」视图
datasz   int                       // 当前映射字节数
MmapFlags int                      // 用户自定义 mmap flags
mmaplock sync.RWMutex              // 保护 remap 与读事务的并发
```

- `dataref` 给 `unix.Munmap`（要原始 base+len）
- `data` 给 `db.page(pgid)`（任意偏移指针运算）
- 同一块虚拟内存被两种类型指着——绕开 Go 切片下标检查的常见手法

---

### 3. 入口：`Open` 时初始映射（`db.go:297`）

```go
if err = db.mmap(options.InitialMmapSize); err != nil {
    _ = db.close()
    return nil, err
}
```

整个 db 第一次 mmap：

- 文件 truncate / `InitialMmapSize` / 32KB 三者取大值
- 映射后立即从 `db.data` 读 `pgid=0,1` 的 meta 页并 `Validate()`
- 失败回滚 `db.close()`

---

### 4. mmap 尺寸策略：`db.mmapSize`（`db.go:581-612`）

不是文件多大映射多大，而是**指数增长**：

```
size <= 32KB  → 32KB
size <= 64KB  → 64KB
...
size <= 1GB   → 1GB
size  > 1GB   → 按 MaxMmapStep (1GB) 取整向上
封顶          → MaxMapSize（架构相关）
```

**为什么超额映射**：mmap 在 Linux 上只建 VMA，不立即占物理页或扩文件——预占地址空间避免每次写就 remap。

---

### 5. 写事务扩容触发 remap（`db.go:1182-1213`）

写事务 `db.allocate(txid, count)` 发现 freelist 不够、要扩到文件末尾时：

```go
if minsz >= db.datasz {
    if err := db.mmap(minsz); err != nil { ... }
}
```

**这是 remap 的唯一业务触发点**——平时不会主动 remap。

`db.mmap(minsz)` 完整流程（`db.go:456-553`）：

1. `db.mmaplock.Lock()`（写锁，阻塞所有读事务）
2. `db.rwtx.root.dereference()`（先把内存 node 引用拷到堆）
3. `db.munmap()`
4. `mmap(db, newSize)`
5. 重新读 meta0/meta1 + Validate

---

### 6. 读路径：`db.page` / `tx.page`（`db.go:1130, tx.go:629`）

**所有 B+ 树读路径的高频用户**：

```go
func (db *DB) page(id common.Pgid) *common.Page {
    pos := id * common.Pgid(db.pageSize)
    return (*common.Page)(unsafe.Pointer(&db.data[pos]))
}

func (tx *Tx) page(id common.Pgid) *common.Page {
    if tx.pages != nil {
        if p, ok := tx.pages[id]; ok {  // 当前事务的脏页（堆上）
            return p
        }
    }
    return tx.db.page(id)               // 回退 mmap（零拷贝）
}
```

| 事务类型 | 行为 |
|---|---|
| 读事务 | 永远走 `db.page`，零拷贝 |
| 写事务 | 先查 `tx.pages` 自己的脏页，没有再回退 mmap——**MVCC 本地修改可见性** |

调用方：`bucket.go`、`cursor.go`、`node.go`、`tx_check.go`——所有 B+ 树遍历都收敛到这。

---

### 7. Bucket 中的 mmap 直接引用（`bucket.go:128-135`）

```go
if b.tx.writable && !unaligned {
    child.InBucket = &common.InBucket{}
    *child.InBucket = *(*common.InBucket)(unsafe.Pointer(&value[0]))  // 写：拷贝
} else {
    child.InBucket = (*common.InBucket)(unsafe.Pointer(&value[0]))    // 读：直接指 mmap
}
```

注释一针见血：**「Read-only transactions can point directly at the mmap entry.」**

读事务里的 `Bucket` 真的指向 mmap 字节，零拷贝零分配——这就是为什么 `View` 回调返回的 []byte 不能带出事务。

---

### 8. `dereference`：remap 之前的"内存逃逸"（`node.go:463, bucket.go:920`）

```go
func (n *node) dereference() {
    if n.key != nil {
        key := make([]byte, len(n.key))
        copy(key, n.key)
        n.key = key
    }
    for i := range n.inodes {
        inode := &n.inodes[i]
        key := make([]byte, len(inode.Key()))
        copy(key, inode.Key())
        inode.SetKey(key)
        // value 同理
    }
    // 递归 children
}
```

**为什么必须做**：写事务 commit 中可能 remap，旧 mmap 区域被销毁。如果 node 里的 `[]byte` 还指着旧 mmap，访问会 SIGSEGV。

**触发链**：`db.mmap → db.rwtx != nil → db.rwtx.root.dereference()`（`db.go:504`）→ `Bucket.dereference` 递归 `node.dereference`。

> mmap 设计里**最容易被忽视的复杂度**：零拷贝读爽，remap 前要把所有跨 mmap 的引用主动「逃逸到堆」。

---

### 9. `mmaplock` 协调机制（`db.go:147`）

| 持有者 | 锁类型 | 时长 |
|---|---|---|
| 读事务 `beginTx` (`db.go:801`) | `RLock` | 整个事务生命周期 |
| `db.mmap` 重映射 (`db.go:457`) | `Lock` | 重映射期间 |
| `db.Close` (`db.go:701`) | `Lock` | 关闭期间 |

- 多个读事务 RLock 共存 → 并发读
- 写事务自身**不持** mmaplock → 不阻塞读事务
- 只有 remap 才持写锁 → 读事务和写事务**唯一**的交互点

「长读事务会阻塞 remap」的物理机制就在这——它持 RLock，remap 拿不到写锁。

---

### 10. Close：`db.munmap`（`db.go:721`）

```go
db.rwlock.Lock()
db.metalock.Lock()
db.mmaplock.Lock()  // 等所有读事务结束
defer ...
return db.close()
// db.close 内部：
//   db.munmap()  → unix.Munmap(db.dataref)
//   置 dataref/data/datasz 为 nil/0
```

**三把锁全拿才 close**——保证关闭时既无写也无读。

---

### 11. Rollback 容错：mmap 失败的处理（`tx.go:329-340`）

```go
if tx.writable {
    tx.db.freelist.Rollback(tx.meta.Txid())
    // mmap 失败时 data/dataref/datasz 可能已被清空
    if tx.db.data != nil {
        if !tx.db.hasSyncedFreelist() {
            tx.db.freelist.NoSyncReload(tx.db.freepages())
        } else {
            tx.db.freelist.Reload(tx.db.page(tx.db.meta().Freelist()))
        }
    }
}
```

写事务 commit 中途 mmap 失败 → `db.data == nil` → 跳过 reload，等 db 重新 Open。最后一道防线。

---

### 12. `Mlock` 选项：把 mmap 钉在物理内存（`mlock_unix.go`）

```go
func mlock(db *DB, fileSize int) error {
    sizeToLock := fileSize
    if sizeToLock > db.datasz {
        sizeToLock = db.datasz
    }
    return unix.Mlock(db.dataref[:sizeToLock])
}
```

**触发点**：

- `Options.Mlock=true` 打开时
- 每次 `db.mmap` 后重新 `mlock`
- 文件增长时 `mrelock`（旧 munlock + 新 mlock）

**作用**：保证 db 文件不被换出 swap，延迟敏感场景用。代价是物理内存常驻。

---

### 13. `MmapFlags` 选项：用户附加 flag（`db.go:84, 1351`）

```go
unix.Mmap(fd, 0, sz, PROT_READ, syscall.MAP_SHARED|db.MmapFlags)
```

典型用途：

```go
import "golang.org/x/sys/unix"

bolt.Open("my.db", 0600, &bolt.Options{
    MmapFlags: unix.MAP_POPULATE,  // Linux：立即把所有页读进 page cache
})
```

`MAP_POPULATE` 适合"启动后立刻全表扫"，避免随机访问触发缺页。

---

### 14. `MaxMapSize` / `MaxMmapStep` 常量

```go
// internal/common/types.go:10
const MaxMmapStep = 1 << 30  // 1GB

// internal/common/bolt_<arch>.go
const MaxMapSize = ...
```

| 架构 | MaxMapSize |
|---|---|
| amd64 / arm64 / ppc64 / s390x / riscv64 / loong64 | 256TB |
| mips64 | 512GB |
| 386 / arm / ppc / mips | 1~2GB |

`*[MaxMapSize]byte` 这个类型本身就用了 `MaxMapSize`：编译期保证下标合法范围足够大。

---

### 15. 测试中的 mmap 覆盖

| 测试 | 检验什么 |
|---|---|
| `db_test.go:458 TestDB_Open_InitialMmapSize` | 大 InitialMmapSize 不阻塞写事务 |
| `db_test.go:1632 TestDB_MaxSizeExceededCanOpenWithHighMmap` | InitialMmapSize > MaxSize 时仍可打开（非 Windows） |
| `db_test.go:1657...` | Windows 下 mmap 不应推测性扩文件 |
| `tx_test.go:701` | 写事务超过 mmap 限制的行为 |

---

### 16. 一图收束

```
mmap 触点
├── 平台层（5 份）        bolt_*.go:mmap/munmap
├── 字段（5 个）          db.dataref/data/datasz/mmaplock/MmapFlags
├── 入口（2 个）
│   ├── Open 时           db.go:297  db.mmap(InitialMmapSize)
│   └── 写时扩容          db.go:1206 db.mmap(minsz)
├── 尺寸策略              db.mmapSize  几何增长
├── 读路径（高频）
│   ├── db.page           零拷贝定位
│   ├── tx.page           dirty + mmap fallback
│   └── bucket child      读事务直接指 mmap
├── remap 前的逃逸        node/bucket.dereference
├── 锁协调                mmaplock RWMutex
├── 关闭                  db.Close → db.munmap
├── 容错                  tx.rollback 处理 mmap 失败
├── 用户开关
│   ├── Mlock             mlock_unix.go  常驻内存
│   ├── MmapFlags         MAP_POPULATE 等
│   └── InitialMmapSize   预占地址空间
└── 常量                  MaxMapSize / MaxMmapStep
```

---

### 17. 一句话总结

bbolt 把 mmap 当成"全局只读字节数组"用，**所有 B+ 树读路径都收敛到 `db.page` 的指针运算**；而 mmap 的可变性（remap）通过 **`mmaplock` + `dereference`** 两把武器与所有读事务和内存 node 解耦。Mlock / MmapFlags / InitialMmapSize 是用户能调的三个钮，分别管"内存常驻"、"额外 flag"、"预占地址空间"。
