## 目录

[[#Q1:寻路算法详解]]
[[#Q2 路径串联机制详解]]
 [[#Q3 当前寻路的性能瓶颈 & 优化思路]]
  [[#Q4 服务器通知客户端 AI 事件的完整流程]]
### 一、前置知识：地图数据模型

在深入寻路之前，先理解数据模型：

```
世界坐标 (float32 X, Y, Z)
    │
    │ getNormalizeVal(val) = int32(val * 10)  ← 精度 0.1 米
    ▼
归一化坐标 (int32 normalizeX, normalizeZ)
    │
    │ getMapPosKeyWithNormalizeXz(x, z)  ← 高32位=X, 低32位=Z (负Z特殊编码)
    ▼
posKey (int64) ← 全局唯一的格子ID
    │
    │ MapNavData.mapDataDic[posKey]
    ▼
MapData { Pos: Vector3, GroundKind }  ← 存在=可行走, nil=墙/障碍
```

**关键点**：地图是一个 **离散网格（Grid）**，精度 `0.1` 米。每个格子用 `posKey` 唯一标识，如果 `mapDataDic[posKey]` 存在则可行走，不存在则视为墙壁。

**路径点（BlockPathInfo）**：每个地图区块（`BattleMgrBlock`）上预埋了一系列**有序路径点**（从配置 `MapPathConf.PosKeyArr` 加载），每隔 `constPathPointDis=3` 米插值生成更密集的点，存储在 `pathInfoArr []BlockPathInfo` 中。路径点是一条**有方向的线串**，首尾相连形成从区块头到区块尾的路线。

---

### 二、`computeNavSeekPath()` 完整流程

```go
func (mgr *BattleMgr) computeNavSeekPath(role, sourcePos, targetPos) (*NavSeekPath, BattleComputeNavKind)
```

该函数的目标：给定起点 `sourcePos` 和终点 `targetPos`，返回一个由 posKey 序列组成的路径 `NavSeekPath`。

---

#### 第1步：判断是否可直达（射线检测）

```go
isArrive := getDisWithPos(sourcePos, targetPos) < dataPointDis  // < 0.5米？已到达
isRaycast, _, _, targetPosKey := mgr.isRaycastWithPosKey(sourcePos, targetPos)
```

**`isRaycastWithPosKey(pos1, pos2)` 工作原理：**

```
1. 获取起点、终点的 MapData，任一为 nil → 不可达
2. 计算两点距离 dis
3. 如果 dis > dataNavPointRange(15米) → 超出射线范围，不可达
4. 计算方向 dir = normalize(pos2 - pos1)
5. 从 pos1 沿 dir 方向，每隔 1 米检测一个点：
   for i := 0; i <= int(dis); i++ {
       tmpPos = pos1 + dir * i
       if getMapDataWithWorldPos(tmpPos) == nil → 遇到墙，不可达
   }
6. 全部通过 → 两点间无障碍，可直达
```

**如果可直达或已到达**：直接返回只含终点 posKey 的路径，无需复杂寻路。

```go
if isRaycast || isArrive {
    navSeekPath := newNavSeekPath(nil, targetPosKey)
    if isArrive { navSeekPath.Finish() }
    return navSeekPath, compmteNavKindRaycast
}
```

---

#### 第2步：收集起点和终点的"可达路径点"

```go
nearNavSeekPath := newNearNavSeekPath(isForward)  // isForward = 玩家组=true

// 从起点出发，对所有路径点做射线检测，找出起点能直达的路径点
_, startLocalPathDic, _, _ := mgr.getRaycastMapPathPointArr(sourcePos, targetPos, ...)

// 从终点出发，对所有路径点做射线检测，找出终点能直达的路径点
targetMapPathDic, _, _, _ := mgr.getRaycastMapPathPointArr(targetPos, sourcePos, ...)
```

**`getRaycastMapPathPointArr(sourcePos, pos2)` 工作原理：**

```
pathInfoArr := 当前区块的路径点数组
nextPathInfoArr := 下一个区块的路径点数组（如果存在）

对每个路径点 v:
    isRaycast, dis, _, _ := mgr.isRaycastWithPosKey(sourcePos, v.WorldPos)
    如果射线通过 → 将该路径点记录到可达字典中
        mapRaycastPathDic[v.WorldPosKey] = dis     // 到该路径点的距离
        mapRaycastLocalPathDic[v.WorldPosKey] = v.WorldPosKey  // 路径点key

同时记录距离最近的可达路径点 minPosKey
```

**结果**：
- `startLocalPathDic`：起点能直达的所有路径点（及其 posKey）
- `targetMapPathDic`：终点能直达的所有路径点（及其距离）

这两个集合是寻路的"入口"和"出口"。

---

#### 第3步：沿路径点链搜索最短路径

```go
for _, v := range startLocalPathDic {
    startNavSeekPath := newNavSeekPath(nil, v)   // 以每个起点可达路径点为起始
    mgr.checkNearNavSeekPath(nearNavSeekPath, startNavSeekPath, targetMapPathDic, targetPos)
}
```

**`checkNearNavSeekPath()` 是核心递归搜索，工作原理：**

```
输入：
  - nearNavSeekPath: 记录全局最优路径
  - lastNavSeekPath: 当前已走过的路径链
  - targetMapPathDic: 终点可达路径点集合
  - isForward: 搜索方向（玩家=前进，怪物=后退）

算法：
  1. 获取当前路径点所在区块 blo
  2. 确定当前路径点在路径数组中的位置

  3. 判断是否到达区块边界：
     - 如果是前进方向 && 当前是最后一个路径点 → 跳到下一区块的第一个路径点
     - 如果是后退方向 && 当前是第一个路径点 → 跳到上一区块的最后一个路径点

  4. 否则在区块内沿方向取下一个路径点：
     - 前进: pathInfoArr[i+1]
     - 后退: pathInfoArr[i-1]

  5. 得到 nextPathInfo 后，构建新的 navSeekPath（链式记录路径）

  6. 检查 nextPathInfo 是否在终点可达集合中：
     if targetMapPathDic[nextPathInfo.WorldPosKey] != 0 {
         // 找到了！计算总距离 = 路径链长度 + 终点到该路径点距离
         navSeekPath.Dis += targetMapPathDic[nextPathInfo.WorldPosKey]
         // 如果比当前最优更短，更新最优路径
         if nearNavSeekPath.Dis == -1 || navSeekPath.Dis < nearNavSeekPath.Dis {
             nearNavSeekPath = navSeekPath  // 记录最短路径
         }
     } else {
         // 还没到出口，继续递归搜索
         mgr.checkNearNavSeekPath(nearNavSeekPath, navSeekPath, targetMapPathDic, targetPos)
     }
```

**本质**：这是一个**单向链式搜索**——沿着预铺路径点数组，按方向（前进/后退）逐点推进，直到找到"终点能直达"的某个路径点为止。对每个起点可达路径点都做一次搜索，最终取距离最短的路径。

---

#### 第4步：补充终点信息并返回

```go
if nearNavSeekPath.NavSeekPath == nil {
    return nil, compmteNavKindNoWay   // 无路可达
}
nearNavSeekPath.NavSeekPath.addFinBlockInfo(targetPosKey)  // 追加终点 posKey
return nearNavSeekPath.NavSeekPath, compmteNavKindPass
```

---

### 三、NavSeekPath 数据结构

```go
type NavSeekPath struct {
    posKeyArr       []int64      // 路径 posKey 序列: [起点可达路径点, 中间路径点..., 终点可达路径点, 终点]
    index           int          // 当前走到第几个点
    Dis             int          // 路径总长度（路径点数 + 终点距离）
    WorldPosKey     int64        // 最后一个路径点的 key
    WorldPos        *Vector3     // 最后一个路径点坐标
    LastNavSeekPath *NavSeekPath // 链表指针（构建时用）
}
```

构建过程是链式追加：每次 `newNavSeekPath(lastNavSeekPath, worldPosKey)` 把新路径点 append 到 `posKeyArr`。

---

### 四、图解完整示例

```
地图示意（俯视）:
  ████████████████████████████
  █                          █
  █   A(起点)     ██████     █
  █               ██████     █
  █   P1----P2----P3----P4   █   ← 路径点数组（有序排列）
  █               ██████     █
  █               ██████     █
  █                    B(终点)█
  ████████████████████████████

第1步 - 射线检测 A→B：
  A 到 B 中间有墙 (██████)，射线检测失败，不能直达。

第2步 - 收集可达路径点：
  从 A 发射射线：A 能直达 P1、P2 → startLocalPathDic = {P1, P2}
  从 B 发射射线：B 能直达 P3、P4 → targetMapPathDic = {P3: dis3, P4: dis4}

第3步 - 链式搜索：
  搜索1：从 P1 出发 → P1→P2→P3（P3 在 targetMapPathDic 中！）
         路径 = [P1, P2, P3]，距离 = 3 + dis3
  搜索2：从 P2 出发 → P2→P3（P3 在 targetMapPathDic 中！）
         路径 = [P2, P3]，距离 = 2 + dis3  ← 更短！

  选取最短的搜索2。

第4步 - 追加终点：
  最终路径 posKeyArr = [P2_key, P3_key, B_key]

角色移动时沿 posKeyArr 依次移动：A → P2 → P3 → B
  每一步的实际移动由 getRadiusAvoidPos() 做碰撞避障。
```

---

### 五、性能特点与设计取舍

| 特性           | 说明                                                              |
| ------------ | --------------------------------------------------------------- |
| **非通用图搜索**   | 不是 A*/Dijkstra，而是利用路径点的**有序线性排列**做方向性遍历                         |
| **O(P) 复杂度** | P 为区块路径点总数，每次搜索最多遍历所有路径点一次                                      |
| **射线检测门槛**   | `dataNavPointRange=15米` 限制了射线检测距离，超出15米的路径点不会被考虑                |
| **路径点密度**    | 每 `constPathPointDis=3米` 生成一个路径点，平衡精度与性能                        |
| **到达判定**     | `dataPointDis=0.5米` 内判定到达当前路径点，推进到下一个                           |
| **方向性搜索**    | 玩家前进(forward)、怪物可后退，搜索方向由 `isForward` 控制                        |
| **适用场景**     | 线性关卡（主线推图、副本走廊）非常高效；开放地图（如西游）需要更大的 `dataNavPointRangeWuKong=50` |


**核心设计思路**：地图是线性关卡设计（一条主路线穿过多个区块），路径点天然有序排列，所以不需要复杂的图搜索——只要找到"起点能直达哪个路径点"和"终点能直达哪个路径点"，然后沿路径点数组串联起来就是最优路径。


## Q2: 路径串联机制详解

### 一、调用入口

```go
// computeNavSeekPath() 中的调用：
for _, v := range startLocalPathDic {              // 遍历起点能直达的每个路径点
    startNavSeekPath := newNavSeekPath(nil, v)      // 以它为起始，构造第一个路径节点
    mgr.checkNearNavSeekPath(nearNavSeekPath, startNavSeekPath, targetMapPathDic, targetPos)
}
```

`startLocalPathDic` 中可能有多个起点可达路径点（比如 P1、P2），对**每一个**都发起一次搜索，最终取最短的。

---

### 二、`newNavSeekPath()` —— 链式构建路径

```go
func newNavSeekPath(lastNavSeekPath *NavSeekPath, worldPosKey int64) *NavSeekPath {
    navSeekPath := &NavSeekPath{}
    navSeekPath.LastNavSeekPath = lastNavSeekPath

    if lastNavSeekPath == nil {
        // 首个节点：创建新的 posKeyArr，放入第一个路径点
        navSeekPath.posKeyArr = make([]int64, 1)
        navSeekPath.posKeyArr[0] = worldPosKey
    } else {
        // 后续节点：在上一个路径的 posKeyArr 尾部追加新的 posKey
        navSeekPath.posKeyArr = append(lastNavSeekPath.posKeyArr, worldPosKey)
        navSeekPath.Dis = lastNavSeekPath.Dis   // 继承已有距离
    }

    navSeekPath.WorldPosKey = worldPosKey
    navSeekPath.Dis++                            // 每经过一个路径点，距离+1
    navSeekPath.WorldPos = getPosWithPosKey(worldPosKey)
    return navSeekPath
}
```

**关键**：`posKeyArr` 是一个 `[]int64` 切片。每次 `newNavSeekPath` 都在 `lastNavSeekPath.posKeyArr` 基础上 `append` 新的 posKey。所以 `posKeyArr` 就是**从起点到当前位置的完整路径点序列**。

**注意**：这里有一个 Go 的微妙点——`append(lastNavSeekPath.posKeyArr, worldPosKey)` 如果底层数组容量够，会共享底层数组。但因为搜索是单向递归（不回溯），所以不会出错。

---

### 三、`checkNearNavSeekPath()` —— 逐行拆解

```go
func (mgr *BattleMgr) checkNearNavSeekPath(
    nearNavSeekPath *NearNavSeekPath,   // 全局最优路径记录器（传引用，所有搜索共享）
    lastNavSeekPath *NavSeekPath,       // 当前已构建的路径链（包含从起点到当前点的所有 posKey）
    targetMapPathDic map[int64]int,     // 终点能直达的路径点集合 {posKey: 距离}
    targetPos *geometry.Vector3,
) {
```

#### 步骤 1：定位当前路径点所在区块

```go
    blo := mgr.getBloWithPosZ(lastNavSeekPath.WorldPos.Z)
    // 根据当前路径点的 Z 坐标，找到它所属的地图区块（BattleMgrBlock）
    if blo == nil { return }
```

#### 步骤 2：判断当前点是否在区块边界

```go
    lastWorldPosKey := lastNavSeekPath.WorldPosKey    // 当前路径点的 posKey

    var endPosKey int64
    if nearNavSeekPath.IsForward {
        // 前进方向：区块边界 = 路径数组的最后一个点
        endPosKey = blo.getPathInfoArr()[len(blo.getPathInfoArr())-1].WorldPosKey
    } else {
        // 后退方向：区块边界 = 路径数组的第一个点
        endPosKey = blo.getPathInfoArr()[0].WorldPosKey
    }

    isLinkNextBlock := endPosKey == lastWorldPosKey
    // 如果当前点 == 区块边界点，说明要跨区块了
```

#### 步骤 3A：跨区块——跳到相邻区块

```go
    var nextPathInfo *BlockPathInfo
    if isLinkNextBlock {
        if nearNavSeekPath.IsForward {
            // 前进方向：跳到下一个区块的第一个路径点
            bloNext := blo.nextBlo
            if bloNext != nil {
                blo = bloNext
                nextPathInfo = blo.getPathInfoArr()[0]     // 下一区块的起点
            }
        } else {
            // 后退方向：跳到上一个区块的最后一个路径点
            bloLast := blo.lastBlo
            if bloLast != nil && mgr.isMapBlockUsed(bloLast) {
                blo = bloLast
                len := len(blo.getPathInfoArr())
                if len > 0 {
                    nextPathInfo = blo.getPathInfoArr()[len-1]  // 上一区块的终点
                }
            }
        }
```

#### 步骤 3B：区块内——沿路径数组取下一个点

```go
    } else {
        pathInfoArr := blo.getPathInfoArr()
        pathInfoLen := len(pathInfoArr)

        if nearNavSeekPath.IsForward {
            // 前进：在路径数组中找到当前点，取 i+1
            for i := 0; i < pathInfoLen-1; i++ {
                if pathInfoArr[i].WorldPosKey == lastWorldPosKey {
                    nextPathInfo = pathInfoArr[i+1]    // ← 就是取数组中的下一个！
                    break
                }
            }
        } else {
            // 后退：在路径数组中找到当前点，取 i-1
            for i := pathInfoLen - 1; i > 0; i-- {
                if pathInfoArr[i].WorldPosKey == lastWorldPosKey {
                    nextPathInfo = pathInfoArr[i-1]    // ← 取数组中的上一个
                    break
                }
            }
        }
    }
```

**这就是"串联"的核心**：路径点在 `pathInfoArr` 中是**有序排列**的（从区块入口到出口），串联就是沿着数组下标 +1（或 -1）取下一个点。跨区块时通过 `blo.nextBlo` / `blo.lastBlo` 链表跳转。

#### 步骤 4：构建新的路径节点

```go
    if nextPathInfo == nil { return }   // 到头了，无路可走

    // 关键：将 nextPathInfo 追加到路径中
    navSeekPath := newNavSeekPath(lastNavSeekPath, nextPathInfo.WorldPosKey)
    // ↑ 这会在 lastNavSeekPath.posKeyArr 尾部 append nextPathInfo 的 posKey
    // 此时 navSeekPath.posKeyArr = [起始路径点, ..., 上一个点, nextPathInfo]
```

#### 步骤 5：检查是否到达"出口"

```go
    isTargetMapPath := targetMapPathDic[nextPathInfo.WorldPosKey] != 0
    // 检查当前点是否在"终点可达路径点集合"中

    if isTargetMapPath {
        // ✅ 找到了！这个路径点能被终点直达
        navSeekPath.Dis += targetMapPathDic[nextPathInfo.WorldPosKey]
        // Dis = 路径链长度(路径点个数) + 终点到该路径点的射线距离

        // 如果比当前全局最优更短，则更新
        if nearNavSeekPath.Dis == -1 || navSeekPath.Dis < nearNavSeekPath.Dis {
            nearNavSeekPath.Dis = navSeekPath.Dis
            nearNavSeekPath.NavSeekPath = navSeekPath   // 记录这条最优路径
        }
    } else {
        // ❌ 还没到出口，继续递归搜索下一个路径点
        mgr.checkNearNavSeekPath(nearNavSeekPath, navSeekPath, targetMapPathDic, targetPos)
    }
}
```

---

### 四、图解串联过程

假设地图有两个区块，路径点排列如下：

```
区块A (blo):        P1 --- P2 --- P3 --- P4
                                           ↕ (blo.nextBlo / blo.lastBlo)
区块B (bloNext):    P5 --- P6 --- P7 --- P8
```

`pathInfoArr` 数据结构：
- 区块A 的 `pathInfoArr = [P1, P2, P3, P4]`（有序数组）
- 区块B 的 `pathInfoArr = [P5, P6, P7, P8]`（有序数组）
- 区块间链接：`区块A.nextBlo = 区块B`，`区块B.lastBlo = 区块A`

**场景**：起点 S 能直达 P2，终点 T 能直达 P7。搜索方向 isForward=true。

```
递归调用栈（前进方向）：

调用1: checkNearNavSeekPath(lastNavSeekPath = {posKeyArr: [P2]})
  → 当前点 P2 在区块A，不在边界
  → pathInfoArr[1]=P2，取 pathInfoArr[2]=P3
  → newNavSeekPath → posKeyArr = [P2, P3]
  → P3 在 targetMapPathDic 中？❌ 不在
  → 递归 ↓

调用2: checkNearNavSeekPath(lastNavSeekPath = {posKeyArr: [P2, P3]})
  → 当前点 P3 在区块A，不在边界
  → pathInfoArr[2]=P3，取 pathInfoArr[3]=P4
  → newNavSeekPath → posKeyArr = [P2, P3, P4]
  → P4 在 targetMapPathDic 中？❌ 不在
  → 递归 ↓

调用3: checkNearNavSeekPath(lastNavSeekPath = {posKeyArr: [P2, P3, P4]})
  → 当前点 P4 在区块A，P4 == endPosKey(区块A最后一个点)
  → isLinkNextBlock = true！
  → 跳到 bloNext(区块B)，取 pathInfoArr[0] = P5
  → newNavSeekPath → posKeyArr = [P2, P3, P4, P5]
  → P5 在 targetMapPathDic 中？❌ 不在
  → 递归 ↓

调用4: checkNearNavSeekPath(lastNavSeekPath = {posKeyArr: [P2, P3, P4, P5]})
  → 当前点 P5 在区块B，不在边界
  → pathInfoArr[0]=P5，取 pathInfoArr[1]=P6
  → newNavSeekPath → posKeyArr = [P2, P3, P4, P5, P6]
  → P6 在 targetMapPathDic 中？❌ 不在
  → 递归 ↓

调用5: checkNearNavSeekPath(lastNavSeekPath = {posKeyArr: [P2, P3, P4, P5, P6]})
  → 当前点 P6 在区块B，不在边界
  → pathInfoArr[1]=P6，取 pathInfoArr[2]=P7
  → newNavSeekPath → posKeyArr = [P2, P3, P4, P5, P6, P7]
  → P7 在 targetMapPathDic 中？✅ 是的！距离=dis7
  → Dis = 6(路径点数) + dis7(终点射线距离)
  → 更新 nearNavSeekPath.NavSeekPath = {posKeyArr: [P2, P3, P4, P5, P6, P7]}
  → 返回 ↑↑↑↑↑
```

最终在 `computeNavSeekPath()` 中追加终点：
```go
nearNavSeekPath.NavSeekPath.addFinBlockInfo(targetPosKey)
// posKeyArr = [P2, P3, P4, P5, P6, P7, T]
```

**角色实际移动**：`S → P2 → P3 → P4 → P5 → P6 → P7 → T`
（每步由 `getRadiusAvoidPos()` 做碰撞避障）

---

### 五、总结：串联的本质

```
串联 = 在预排序的路径点数组中，按下标 +1/-1 逐个取下一个路径点，
       到达区块边界时通过链表跳到相邻区块继续取，
       每取一个就 append 到 posKeyArr，
       直到取到的路径点出现在"终点可达集合"中为止。
```

**它不是图搜索**，而是**线性数组遍历**——因为路径点本身就是一条有序路线，"串联"就是沿着这条预铺好的路线走。效率极高，O(P)，P 是需要遍历的路径点个数。代价是只能沿预铺路线走，不支持开放式自由寻路。



## 这段代码的逐行详解

```go
_, startLocalPathDic, _, _ := mgr.getRaycastMapPathPointArr(sourcePos, targetPos, "computeNavSeekPath")
targetMapPathDic, _, _, _ := mgr.getRaycastMapPathPointArr(targetPos, sourcePos, "computeNavSeekPath")
```

### 先看 `getRaycastMapPathPointArr` 的4个返回值

```go
func getRaycastMapPathPointArr(sourcePos, pos2) (
    mapRaycastPathDic    map[int64]int,    // 返回值1：{路径点posKey → 射线距离(int)}
    mapRaycastLocalPathDic map[int64]int64, // 返回值2：{路径点posKey → 路径点posKey}（就是自己映射自己）
    minPosKey            int64,             // 返回值3：最近可达路径点的posKey
    minWorldPos          *Vector3,          // 返回值4：最近可达路径点的坐标
)
```

这个函数做的事：从 `sourcePos` 对**所有路径点**发射射线，找出 `sourcePos` 能无障碍直达的路径点。返回值1和返回值2的**key 完全一样**，都是可达路径点的 posKey，只是 value 不同：
- 返回值1 的 value = **距离**（从 sourcePos 到该路径点多少米）
- 返回值2 的 value = **posKey 本身**（用来后续构建 NavSeekPath 的初始节点）

### 两次调用取了不同的返回值

```go
// 第1次调用：以【起点】为圆心，找起点能直达的路径点
// 只取第2个返回值 startLocalPathDic (map[posKey]posKey)
_, startLocalPathDic, _, _ := mgr.getRaycastMapPathPointArr(sourcePos, targetPos, ...)

// 第2次调用：以【终点】为圆心，找终点能直达的路径点
// 只取第1个返回值 targetMapPathDic (map[posKey]距离)
targetMapPathDic, _, _, _ := mgr.getRaycastMapPathPointArr(targetPos, sourcePos, ...)
```

**为什么取不同的返回值？因为用途不同：**

| 变量 | 取的返回值 | value 含义 | 用途 |
|---|---|---|---|
| `startLocalPathDic` | 返回值2 | posKey（值=key） | 作为搜索**起始点**，只需要 posKey 来构建初始 NavSeekPath |
| `targetMapPathDic` | 返回值1 | 射线距离 | 作为搜索**终止判定**，需要距离来计算总路径长度 |

### `for` 循环：对每个起始点发起搜索

```go
for _, v := range startLocalPathDic {
    // v 是一个 posKey（起点能直达的某个路径点的 posKey）
    startNavSeekPath := newNavSeekPath(nil, v)
    // 构建初始路径节点：posKeyArr = [v]，Dis = 1

    mgr.checkNearNavSeekPath(nearNavSeekPath, startNavSeekPath, targetMapPathDic, targetPos)
    // 从这个路径点出发，沿路径点数组逐个往前走
    // 每走一步检查：当前路径点是否在 targetMapPathDic 中？
    //   - 在 → 终点能直达这个路径点！路径找到了，计算总距离，更新最优
    //   - 不在 → 继续走下一个路径点
}
```

**假设起点能直达 P2 和 P3，会执行两次循环：**

```
循环1: 从 P2 出发 → P2→P3→P4→...→P7(命中!) → 距离 = 6 + dis(T→P7)
循环2: 从 P3 出发 → P3→P4→...→P7(命中!) → 距离 = 5 + dis(T→P7)  ← 更短
```

两次都会尝试更新 `nearNavSeekPath`，最终保留距离最短的那条。

### 最后判断

```go
if nearNavSeekPath.NavSeekPath == nil {
    return nil, compmteNavKindNoWay
}
```

如果所有搜索都没找到"终点可达路径点"（比如起点和终点在完全不连通的区块），返回 `nil`，表示**无路可达**。

---

### 用一张图总结整段逻辑

```
              起点 S                              终点 T
                │                                   │
    getRaycastMapPathPointArr(S)       getRaycastMapPathPointArr(T)
    射线扫描所有路径点                    射线扫描所有路径点
                │                                   │
                ▼                                   ▼
     startLocalPathDic                    targetMapPathDic
     {P2: P2_key, P3: P3_key}           {P6: 3米, P7: 2米}
     "起点能直达哪些路径点"               "终点能直达哪些路径点及距离"
                │
                │  for 循环：每个起始路径点都尝试
                ▼
     ┌─ 从P2出发: P2→P3→P4→P5→P6 ← 在targetMapPathDic中! Dis=5+3=8
     │
     └─ 从P3出发: P3→P4→P5→P6 ← 在targetMapPathDic中! Dis=4+3=7 ✅最短
                                                                    │
                                     addFinBlockInfo(T_posKey)      │
                                                                    ▼
                                     最终路径: [P3, P4, P5, P6, T]
```

**核心思路总结**：起点侧需要的是**哪些路径点可以作为入口**（只要 posKey），终点侧需要的是**哪些路径点可以作为出口 + 出口到终点的距离**（用于比较哪条路最短），中间的串联就是沿路径点数组按顺序走。


## Q3 :当前寻路的性能瓶颈 & 优化思路

### 当前方案的瓶颈分析

| 瓶颈              | 位置                            | 复杂度      | 说明                                   |
| --------------- | ----------------------------- | -------- | ------------------------------------ |
| **射线全扫描**       | `getRaycastMapPathPointArr`   | O(P × D) | 对**每个路径点**都做一次射线检测（P个点，每次射线扫D格），调用两次 |
| **线性查找 posKey** | `checkNearNavSeekPath`        | 每步 O(P)  | 在 `pathInfoArr` 中线性遍历找当前 posKey 的下标  |
| **递归遍历**        | `checkNearNavSeekPath`        | O(N)     | 最差情况遍历全部路径点才命中出口                     |
| **多起点重复搜索**     | `for range startLocalPathDic` | ×M       | M个起始点各做一次完整遍历，路径高度重叠                 |

---

### 优化思路（由易到难）

#### 思路1：给路径点加索引，消除线性查找

**问题**：`checkNearNavSeekPath` 里每步都要 `for i := range pathInfoArr { if posKey == lastWorldPosKey }` 线性找下标。

**方案**：给每个 `BlockPathInfo` 记住自己在数组中的下标（已有 `PathIndex` 字段），再建一个 `map[int64]int`（posKey → 数组下标）。找下一个点就是 `pathInfoArr[index+1]`，O(1)。

```go
// 预处理（initPathInfo 时）
posKeyIndexMap map[int64]int  // posKey → 在 pathInfoArr 中的下标

// checkNearNavSeekPath 中
index := blo.posKeyIndexMap[lastWorldPosKey]
nextPathInfo = pathInfoArr[index+1]   // 直接取，不用遍历
```

**收益**：递归中每步从 O(P) 降到 O(1)。改动最小。

---

#### 思路2：只对最近的路径点做射线，而非全扫描

**问题**：`getRaycastMapPathPointArr` 对所有路径点都做射线检测，但大部分路径点离 source 很远，射线必然失败。

**方案**：先用 `PathIndex`（路径点下标）做**粗筛**——找到距离 sourcePos 最近的路径点（二分查找或空间索引），只对附近 ±K 个路径点做射线检测。

```go
// 找最近路径点（可用 getNearPoint 已有的逻辑）
nearIndex := findNearestPathIndex(sourcePos)
// 只对附近范围做射线
for i := max(0, nearIndex-K); i <= min(len-1, nearIndex+K); i++ {
    isRaycast, dis := isRaycastWithPosKey(sourcePos, pathInfoArr[i].WorldPos)
    ...
}
```

**收益**：射线检测从 O(P) 降到 O(K)，K 一般取 5~10 就够。对你们线性关卡效果特别好。

---

#### 思路3：预计算路径点邻接关系，直接用下标差计算距离

**问题**：整个搜索过程（起点→入口路径点→出口路径点→终点）其实就是找"入口下标"和"出口下标"之间的距离，中间所有路径点都是固定的。

**方案**：把路径点统一编号（跨区块连续编号），寻路就变成：

```
1. 找起点最近的可达路径点 → 得到 startIndex
2. 找终点最近的可达路径点 → 得到 endIndex
3. 路径 = pathInfoArr[startIndex ... endIndex]（直接切片）
4. 距离 = |endIndex - startIndex| × constPathPointDis
```

不需要递归 `checkNearNavSeekPath`，直接**数组切片**就是路径。

**收益**：寻路核心从递归 O(N) 降到 O(1)。前提是路径点跨区块连续编号（初始化时把所有区块的路径点串成一个全局数组或给每个点一个全局序号）。

---

#### 思路4：缓存上次路径，增量更新

**问题**：每帧都重新计算整条路径（`refreshNavPath` 每帧调用），但大部分情况下目标只移动了一小步。

**方案**：
- 缓存上一帧的 `NavSeekPath`
- 新帧只检查：目标是否还在同一个路径点附近？起点是否还在路径上？
- 如果是 → 复用路径，只裁剪已走过的部分
- 如果不是 → 才重新寻路

```go
if ai.navSeekPath != nil && !targetMoved && isStillOnPath(ai.pos, ai.navSeekPath) {
    return ai.navSeekPath  // 复用
}
// 否则重新计算
```

**收益**：大幅减少寻路调用频率，90%+ 的帧可以复用。

---

### 推荐优先级

```
投入产出比排序：
1. 思路1（索引查找）   → 改动极小，消除热点循环
2. 思路4（路径缓存）   → 改动小，减少调用频率
3. 思路2（近邻射线）   → 改动中等，减少射线计算量
4. 思路3（全局编号）   → 改动较大，但能把寻路降到 O(1)，彻底解决
```

你们是线性关卡结构，路径点天然有序，**思路3最适合**——给路径点跨区块统一编号后，寻路就退化成"找两个下标然后切片"，几乎零开销。但改动面最大。如果想快速见效，先做 **思路1 + 思路4** 组合，能解决大部分性能问题。



## Q4: 服务器通知客户端 AI 事件的完整流程

### 一、整体架构：帧驱动 + 战报收集 + 广播

```
┌─────────────── 服务器每帧（1/15秒）──────────────┐
│                                                    │
│  BattleRoom.updateAndBroadcastFrame()              │
│    │                                               │
│    ├─ 1. mgr.update()          ← 执行帧逻辑        │
│    │     ├─ updateStateBattle()                     │
│    │     │   ├─ mgr.updateAI()                     │
│    │     │   │   ├─ updateMonsterAI()  ← 怪物AI    │
│    │     │   │   └─ updatePlayerAI()   ← 玩家AI    │
│    │     │   └─ ...                                │
│    │     │                                         │
│    │     │   在 AI 执行过程中，各种事件发生时         │
│    │     │   调用 mgr.log.addLogXxx() 写入战报       │
│    │     │                                         │
│    │     └─ （战报数据暂存在 mgr.log.datasArr 中）   │
│    │                                               │
│    └─ 2. br.broadcastFrame()   ← 广播战报          │
│          ├─ getOutBattleLog()  ← 收割本帧所有战报    │
│          │   └─ log.getLogDatas() → 返回 datasArr  │
│          │      └─ clearLogDatas() ← 清空，下帧重新收集│
│          │                                         │
│          └─ for 每个在线玩家:                        │
│               filterLogs()     ← 过滤该玩家可见的战报│
│               MakePacket()     ← 打成 protobuf 包   │
│               sendDataWithPlayer() ← TCP 推送       │
└────────────────────────────────────────────────────┘
```

---

### 二、战报数据格式

每条 AI 事件被编码为一个 `BattleLogData`，本质是一个 `[]int32` 数组：

```
┌──────────────────────────────────────────────┐
│  arr[0]  = Action (事件类型枚举)               │  ← logActionPos / logActionAttack / ...
│  arr[1]  = roleKey (角色唯一标识)              │
│  arr[2..] = 事件参数（位置/技能ID/伤害值等）    │
└──────────────────────────────────────────────┘
```

**常见 AI 事件 Action 枚举：**

| Action 枚举 | 值 | 含义 | 携带数据 |
|---|---|---|---|
| `logActionPos` | 2 | 角色位置同步 | roleKey, X, Y, Z, groundKind, posKind |
| `logActionAttack` | 3 | 攻击动作 | roleKey, animState, animSpeed, animTime, effectKind |
| `logActionWalk` | 4 | 开始行走 | roleKey |
| `logActionSkill` | 5 | 释放技能 | roleKey, skillID, animState, animSpeed, releaseTime |
| `logActionDie` | 6 | 死亡 | roleKey, dieKind |
| `logActionDamage` | 7 | 受伤数值 | roleKey, damage, damageType |
| `logActionBattleStart` | 20 | 进入战斗 | roomKind, monsterGroupID, roleKeys... |
| `logActionBattleEnd` | 21 | 战斗结束 | roomKind, isWin, bossID |
| `logActionChant` | 30 | 吟唱技能 | roleKey, skillID, chantTime, animState |
| `logActionIdle` | 1 | 待机 | roleKey |
| `logActionSprint` | 10 | 冲刺 | roleKey |

**坐标精度**：float → int32 编码时乘以 `constNetAccufacy=100`（即精度 0.01 米），通过 `getNetSend32()` 转换。

---

### 三、AI 如何产生事件

以角色移动为例，调用链：

```
updatePlayerAI() / updateMonsterAI()
  └─ role.ai.update()                    ← 每帧执行角色AI
       └─ ai.refreshNavPath()            ← 刷新寻路
            └─ ai.movePos()              ← 沿路径移动
                 └─ 位置变化时:
                      mgr.log.addLogPos(role, logPosKindSeek)
                      // 写入: [action=2, roleKey, X*100, Y*100, Z*100, groundKind, posKind]
```

以攻击为例：

```
role.ai.update()
  └─ ai.refreshTarget()                 ← 刷新攻击目标
       └─ ai.checkSkill()               ← 检查技能CD
            └─ ai.releaseSkill()         ← 释放技能
                 ├─ mgr.log.addLogAttack(role, animState, ...)
                 │  // 写入: [action=3, roleKey, animState, animSpeed, animTime]
                 └─ 命中后:
                      mgr.log.addLogDamage(target, damage, ...)
                      // 写入: [action=7, targetKey, damage, damageType]
```

---

### 四、两种协议包

根据是否有场景数据变化，选择不同的协议包：

```go
if scenePb == nil {
    // 轻量包：只有战报日志（大多数帧走这条路径）
    OutGetBattleLogSimple {
        Logs:      []Int32Array    // 每个元素是一条 BattleLogData.arr
        FrameNum:  uint32          // 当前帧号
        SceneMode: int32           // 房间类型
    }
    → 协议: ProtoMID_BATTLE / BattleAID_GET_BATTLE_LOG_SIMPLE
} else {
    // 完整包：战报日志 + 场景快照（角色加入/离开/属性变化时）
    OutGetBattleLog {
        Logs:  []Int32Array
        Scene: BattleSceneData {   // 包含玩家列表、怪物列表、地图块等
            Players, PlayerDatas, MonsterDatas, Blocks, ...
        }
    }
    → 协议: ProtoMID_BATTLE / BattleAID_GET_BATTLE_LOG
}
```

**大多数帧**只发 `OutGetBattleLogSimple`（轻量包），只有场景数据变化时（如 `isShowPlayerData`、`isShowMonsterData` 被标记为 true）才发完整包。

---

### 五、战报过滤机制

```go
func filterLogs(logDataArr, player) {
    for _, v := range logDataArr {
        if v.sendRoleArr == nil {
            // sendRoleArr == nil → 广播给所有人
            append(logs, v)
        } else {
            // sendRoleArr != nil → 只发给指定玩家
            for _, k := range v.sendRoleArr {
                if k.id == playerID { append(logs, v) }
            }
        }
    }
}
```

- `sendRoleArr = nil`：**全局广播**（位置、攻击、伤害等所有人可见的事件）
- `sendRoleArr = [role]`：**定向推送**（如某些 buff 特效、宠物能量只发给拥有者）

---

### 六、总结

```
服务器通知客户端 AI 事件的本质：

1. 帧循环驱动（15帧/秒）
2. AI 执行产生事件 → 调用 mgr.log.addLogXxx() 追加到 datasArr
3. 帧结束时 broadcastFrame() 收割 datasArr
4. 每条事件编码为 []int32 数组（Action + 参数）
5. 打包成 protobuf（OutGetBattleLogSimple 或 OutGetBattleLog）
6. 通过 agent 连接 TCP 推送给每个在线玩家
7. 客户端收到后，按 Action 枚举解析 []int32，驱动角色表现
```

**设计特点**：不是"每个事件即时推送"，而是**帧对齐批量广播**——一帧内所有 AI 事件收集完毕后，打成一个包一次性推送，保证客户端按帧同步播放。



