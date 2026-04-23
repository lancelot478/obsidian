# Battle Type 问答记录

## 目录
- [[#Q1: entryTypeSummon (825) 召唤模块如何实现，dataCondition 和 data 参数分别是什么意思]]
- [[#Q2: 每个板块前方每秒筑壁垒+10点，每个玩家使壁垒进度-8点，如何实现]]
- [[#Q3: entryTypeRecoverEnergyVal (1100) 复苏能量值模块介绍]]
- [[#Q4: entryTypeRoleStandAreaDetect (10124) 角色站立区域检测模块介绍]]
- [[#Q5: 壁垒系统+充能系统完整设计方案]]
- [[#Q6: 实现壁垒板块管理(10125)+带效率衰减充能(10126)两个action]]
- [[#Q7: entryTypeDragon21EnergyVal(1743) 和 entryTypeDragon21EnergyValChanged(1744) 如何实现，dataCondition 和 data 参数含义]]
- [[#Q8: entryTypeRandomVal(10045) 根据规则随机值模块介绍]]
- [[#Q9: entryTypeTimeWait(1) 时间等待触发模块介绍]]
- [[#Q10: 壁垒颜色变化战报用哪个type实现]]
- [[#Q11: Battle Entry Data 的 Time 字段含义枚举]]
- [[#Q12: 在某个板块播放特效应该用哪个type实现]]
- [[#Q13: addLogShowCommonAreaEffect 如何实现板块常驻特效]]
- [[#Q14: addLogEffectStart 如何配置type和参数发送常驻特效]]
- [[#Q15: Battle Model 的 Buff 字段枚举含义]]
- [[#Q16: 新增 10070 opType=12 按板块方向+半径定位特效]]
- [[#Q17: Time 字段 delay 配置为什么不生效]]
- [[#Q18: addLogEffectObjShow 和 entryTypeEffectObjShow(10097) 特效分层显示介绍]]
- [[#Q19: entryTypeSetLayerNum(10013) 设置模块层数介绍]]
- [[#Q20: 10070 多次执行的 Time 配置方法]]
- [[#Q21: 随机点名+壁垒颜色判定+分支执行的type实现思路]]
- [[#Q22: 10045 actionRandomVal 能否根据指定记录模块的值进行分支]]
- [[#Q23: checkParam modelSign 负数常量完整枚举]]
- [[#Q24: Target 字段结构详解及 3,0,23,-93 配置解析]]
- [[#Q25: entryTypeTriggerMod (10051) 根据值触发模块完整介绍]]
- [[#Q26: actionTriggerMod case 36 如何把 areaIndex 传给红色分支 modId]]
- [[#Q27: entryTypeSaveTargetArea (10054) 详细介绍及如何对特定板块目标造成伤害]]
- [[#Q28: entryTypeDamageHit (100) 核心伤害模块详解]]
- [[#Q29: type=100 配置 @2%目标最大生命值+@3%自身攻击力 的 Data 写法]]
- [[#Q30: battle attr 哪里获取 player 的最大生命值属性]]
- [[#Q31: entryTypeDamageHitOther (97) 值类型伤害模块详解]]
- [[#Q32: type 97 配置 dataCondition 9,212,-92 和 data 0,4,-91,1 的含义解析]]
- [[#Q33: type 97 (entryTypeDamageHitOther) 打印日志流程及字段详解]]
- [[#Q34: type 97 最终伤害信息打印及 hitDamageVal 完整扣血流程]]
- [[#Q35: Target 3,0,15,1 选人流程解析]]
- [[#Q36: WorldBoss 本 Boss 当前目标是谁及如何打印]]
- [[#Q37: WorldBoss 打印目标未生效原因及正确打印位置]]
- [[#Q38: WorldBoss 旋转跟随当前目标的时机及相关 Type]]
- [[#Q39: addLogPos 移动日志发送时机及触发场景]]
- [[#Q40: 如何让 Effect 跟随 Target 移动并显示在头顶]]
- [[#Q41: Target 为 169,0 时选择目标的完整流程]]
- [[#Q42: entryTypeFinDamageHitExtra (306) 受到最终伤害额外详解]]
- [[#Q43: entryTypeIsDragon21EnergyValChange (30192) 龙21怪物能量变动判定详解]]
- [[#Q44: entryTypeBuffModelWithInitPos (10074) 有初始坐标的buff模块详解]]
- [[#Q45: dataCondition 9,212,-92 与 9,212,-91@12,1 在 type 97 时的区别]]
- [[#Q46: type 97 的 dataCondition 是什么用途]]
- [[#Q47: actionBarrierAreaAdd 新增类型参数支持多种 areaIndex 来源]]
- [[#Q48: type=97 配置 @1% target最大生命值+@2% source攻击力 的 Data 写法]]
- [[#Q49: 用 entryTypeDragon21EnergyValChanged(1744) 实现充能+镇龙柱阻止+红色壁垒效率衰减]]
- [[#Q50: entryTypeSetModTargetPos (284) 设置模块 TargetPos 位置 Data 字段各枚举详解]]


---

## Q1: entryTypeSummon (825) 召唤模块如何实现，dataCondition 和 data 参数分别是什么意思

### 实现位置
- **Action 执行**: `internal/battle/battle_entry_action_buff.go:945` — `actionTypeSummon(mod)`
- **Event 触发**: `internal/battle/battle_entry_event.go:803` — `entryWhenSummon(mod, eventData)`

### Data 参数 (`mod.conf.Data`)

| 索引        | 参数           | 说明                                                                   |
| --------- | ------------ | -------------------------------------------------------------------- |
| `Data[0]` | `summonKind` | 召唤类型，决定召唤物的出生位置计算方式                                                  |
| `Data[1]` | `summonID`   | 怪物表ID。为0时召唤镜像(roleKindSummonPlayer)，>0时读怪物表数据(roleKindSummonMonster) |
| `Data[2]` | `offsetX`    | X轴偏移量（不同summonKind下含义不同）                                             |
| `Data[3]` | `offsetZ`    | Z轴偏移量（不同summonKind下含义不同）                                             |
| `Data[4]` | (可选)         | 是否需要附加额外模块，1=需要                                                      |
| `Data[5]` | (可选)         | 额外模块ID `extModID1`，当Data[4]=1时生效                                     |

**注意**：`Data[1]`、`Data[2]`、`Data[3]` 都经过 `mod.checkParam()` 处理，支持动态参数替换。

### summonKind 召唤类型详解

| summonKind | 说明                                              | offsetX/offsetZ 含义           |
| ---------- | ----------------------------------------------- | ---------------------------- |
| `0`        | **本地坐标** — 基于施法者位置+朝向的偏移                        | 本地坐标偏移                       |
| `1`        | **世界坐标** — 绝对坐标                                 | 世界坐标 X/Z                     |
| `2`        | **围绕坐标** — 围绕施法者的预设点位（从 `SummonOffsetPosArr` 取） | 不使用                          |
| `3`        | **世界坐标（不受地图限制）** — 不纠结可行走区域，不移动                 | 归一化后的 X/Z                    |
| `4`        | **上个模块target位置偏移** — 取上一个模块的目标位置                | 偏移量                          |
| `5`        | **敌方队伍中心** — 在敌方玩家队伍中心点召唤                       | 单人时的偏移                       |
| `6`        | **围绕坐标2** — 使用 `SummonOffsetPosArr1` 预设点位       | 不使用                          |
| `7`        | **世界坐标（精度0.01）**                                | X/Z * 0.01                   |
| `8`        | **世界坐标（不移动）** — 设置坐标后不允许移动                      | X/Z * 0.01                   |
| `9`        | **世界坐标（占位模式）** — 使用 `SummonOffsetPosArr2`，有位置管理 | 不使用                          |
| `10`       | **施法者位置加减** — 无需目标，直接在自身位置偏移                    | 直接加减                         |
| `11`       | **世界坐标（占位模式2）** — 使用 `SummonOffsetPosArr3`      | 不使用                          |
| `12`       | **指定板块最外圈随机位置**                                 | offsetX=板块配置id, offsetZ=区域索引 |
| `13`       | **施法者位置（精度0.01）** — 无需目标                        | X/Z * 0.01                   |
| `14`       | **世界坐标（精度0.01）** — 无需目标                         | X/Z * 0.01                   |
| `15`       | **世界坐标（精度0.01+位置管理）** — 按召唤数量分配点位               | X/Z * 0.01                   |
| `16`       | **世界坐标（精度0.01+不移动）** — 无需目标                     | X/Z * 0.01                   |

### ConditionData 参数

| ConditionData[i][0] | 说明                                |
| ------------------- | --------------------------------- |
| `99`                | 计算召唤物所在板块区域ID（动态计算）               |
| `999`               | 直接指定板块区域索引为 `ConditionData[i][1]` |
|                     |                                   |

### 核心限制
- **全局召唤上限**: `constBattleSummonLimit`（整个阵营的召唤物总数）
- **单体召唤上限**: `constBattleSummonSourceLimit`（单个单位召唤出的数量），Boss怪可通过 `summonMonsterNumDic` 配置独立上限
- 特殊豁免：模块ID `9579560` 不受单体上限限制

### Event 触发（当召唤时触发，type=825）

用于其他模块监听"有东西被召唤"事件：
- **Data[0]** = `modelID`：要监听的模块ID
- 匹配逻辑：触发模块ID == modelID，或者被召唤出的怪物ID == modelID

### 完整执行流程

1. 检查全局/单体召唤数量限制
2. 根据 `summonKind` 计算召唤位置
3. 判断 `summonID`：0=镜像玩家，>0=读怪物表
4. 调用 `group.addSummon()` 创建召唤物
5. 若 `Data[4]=1`，给召唤物附加额外模块 `Data[5]`
6. 处理 `ConditionData`（板块信息）
7. 触发 `entryTypeSummon` 事件通知其他模块
8. 记录战斗日志

---

## Q2: 每个板块前方每秒筑壁垒+10点，每个玩家使壁垒进度-8点，如何实现

### 需求分析
- 每秒每个板块壁垒值 +10
- 每个站在该板块上的玩家使壁垒进度 -8
- 即：`板块壁垒值 += 10 - (该板块玩家数 * 8)`
- 壁垒值达到阈值时触发效果（如阻挡通行）

### 现有最接近的模块：entryTypeRoleStandAreaDetect (10124)

位置：`internal/battle/battle_entry_action_10000.go:3150`

**10124 的逻辑**：
- 遍历所有板块，统计每个板块的玩家数量
- 有玩家且达到触发人数 → 计数+1
- 计数达到阈值 → 触发对应板块模块并重置
- 无玩家 → 清零计数

**10124 不能直接满足的原因**：
- 10124 是"有人才计数"，无人清零
- 需求是"无论有没有人都+10，有人则-8"（增减对抗机制）
- 10124 无法实现负向抵消

### 推荐方案：新增 entryType

参考 10124 新增一个 action 类型，Data 参数设计：

| 索引 | 参数 | 说明 | 示例值 |
|------|------|------|--------|
| `Data[0]` | 记录模块ID | commonInt32 存各板块壁垒值 | recordModId |
| `Data[1]` | 板块数量 | n | 4 |
| `Data[2]` | 每次基础增速 | 无论有无玩家都加 | 10 |
| `Data[3]` | 每个玩家削减值 | 有玩家时减 | 8 |
| `Data[4]` | 壁垒上限 | 达到时触发模块 | 100 |
| `Data[5]` | 触发模块起始ID | 触发ID = 起始ID + 板块下标 | baseModId |

核心伪代码：
```
每秒执行：
  遍历每个板块 i (0 ~ n-1):
    playerCount = 统计板块i上的存活玩家数
    壁垒值[i] += 基础增速 - (playerCount * 削减值)
    壁垒值[i] = clamp(壁垒值[i], 0, 上限)
    if 壁垒值[i] >= 上限:
      触发模块(起始ID + i)
      壁垒值[i] = 0  // 触发后重置
```

需配合每秒循环模块（time 配置为每秒触发）驱动执行。

---

## Q3: entryTypeRecoverEnergyVal (1100) 复苏能量值模块介绍

### 概述

`entryTypeRecoverEnergyVal` (1100) 是**复苏能量值**模块，属于"充能类"通用框架的一员。它与 1060(充能)、1896、1901、1910 等类型共用同一个 action 函数 `actionTypeChargeEnergyVal`，用于给玩家设置一个能量值（如复苏进度），并通过 UI 按钮交互触发复苏效果。

### 实现位置

| 功能 | 位置 |
|------|------|
| **Action 分发** | `battle_entry_action.go:1438` — 统一路由到 `actionTypeChargeEnergyVal` |
| **Action 执行** | `battle_entry_action.go:8248` — `actionTypeChargeEnergyVal(mod)` |
| **客户端按钮处理** | `battle_entry_action.go:8227` — `actionHandleCommonLogicOpType9(role)` |

### Data 参数 (`mod.conf.Data`)

#### 作为 Action 执行时（`actionTypeChargeEnergyVal`）

| 索引 | 参数 | 说明 |
|------|------|------|
| `Data[0]` | `setVal` | 初始/设置的能量值 |
| `Data[1]` | `maxValue` | 能量最大值（经 `checkParam` 处理，支持动态参数） |
| `Data[2]` | `recordModID` | 记录模块ID，客户端点击按钮时触发此模块 |
| `Data[3]` | `sendSkillIconLogModID` | UI战报模块ID，用于同步技能图标/UI显示 |

执行逻辑：
```
mod.setMaxVal(maxValue)   // 设置能量上限
mod.setVal(setVal)        // 设置当前能量值
触发 Data[3] 模块          // 发送UI同步战报
```

#### 客户端点击复苏按钮时（`actionHandleCommonLogicOpType9`，OpType=9）

| 使用参数 | 说明 |
|---------|------|
| `Data[2]` — `recordModID` | 按钮点击后触发的模块ID |
| `mod.getVal()` | 判断能量是否满足（需 == 1） |

执行逻辑：
```
找到 target==当前玩家 的 1100 模块
if 目标已死亡 → 不执行
if 能量值 != 1 → 不执行（能量不足）
触发 Data[2] 指定的模块     // 执行复苏效果
```

### 关联类型

| 类型 | 值 | 说明 |
|------|-----|------|
| `entryTypeRecoverEnergyVal` | 1100 | 复苏能量值（本体） |
| `entryTypeClickRecoverBtn` | 1102 | 点击复苏按钮事件（事件触发） |
| `entryTypeRecoverEnergyLog` | 1104 | 复苏能量值变化战报 |

### 1102 点击复苏按钮 (`actionTypeClickRevocerBtn`)

**Data 参数**：

| 索引 | 参数 | 说明 |
|------|------|------|
| `Data[0]` | `recordModID` | 记录模块ID，查找能量是否满足 |
| `Data[1]` | `triggerModId` | 能量满足时触发的模块（给 lastMod.source） |
| `Data[2]` | `triggerModId2` | 能量满足时触发的第二个模块（给 source） |

执行逻辑：
```
查找 lastMod.target 上的 recordModID 模块
if 模块的 val >= 1:
  触发 Data[1] 模块（给 lastMod.source）
  触发 Data[2] 模块（给 source）
  广播 entryTypeClickRecoverBtn 事件
```

### 1104 复苏能量战报 (`actionTypeSkillEnergySyncLog`)

**Data 参数**：

| 索引 | 参数 | 说明 |
|------|------|------|
| `Data[0]` | `recordModId` | 记录模块ID，查找对应的能量模块 |
| `Data[1]` | `tp` | 战报类型（1=复苏值） |

执行逻辑：找到 target 上的 recordModId 模块，发送能量同步战报给客户端。

### 完整交互流程

1. **初始化**：1100 模块执行 `actionTypeChargeEnergyVal`，设置能量初始值和上限，同步UI
2. **能量积累**：通过其他模块（如定时器/事件）修改 1100 模块的 val 值
3. **战报同步**：1104 模块在能量变化时发送战报给客户端
4. **按钮点击**：客户端点击复苏按钮 → `actionHandleCommonLogicOpType9` → 检查 1100 模块的 val 是否为1（满能量）
5. **执行复苏**：1102 模块检查能量足够后，触发实际复苏效果模块，并广播 `entryTypeClickRecoverBtn` 事件

---

## Q4: entryTypeRoleStandAreaDetect (10124) 角色站立区域检测模块介绍

### 概述

`entryTypeRoleStandAreaDetect` (10124) 是**角色站立区域检测**模块，用于周期性检测各板块上是否有足够数量的玩家站立，当某板块上的玩家数达标且持续时间达到阈值时，触发该板块对应的效果模块。

### 实现位置

| 功能 | 位置 |
|------|------|
| **Action 分发** | `battle_entry_action.go:1355` — 路由到 `actionRoleStandAreaDetect` |
| **Action 执行** | `battle_entry_action_10000.go:3150` — `actionRoleStandAreaDetect(mod)` |

### Data 参数 (`mod.conf.Data`)

| 索引        | 参数                 | 说明                                                     |
| --------- | ------------------ | ------------------------------------------------------ |
| `Data[0]` | `recordModId`      | 记录模块ID，使用其 `commonInt32` 存储各板块的累计计数（key=板块下标，val=执行次数） |
| `Data[1]` | `areaCount`        | 板块数量 n                                                 |
| `Data[2]` | `threshold`        | 触发时间阈值 m（每次执行+1，累计达到 m 则触发），支持动态参数                     |
| `Data[3]` | `triggerNum`       | 触发人数阈值 k（板块上的存活玩家数 >= k 才开始计数），支持动态参数                  |
| `Data[4]` | `triggerBaseModId` | 触发模块起始ID（实际触发模块ID = 起始ID + 板块下标 i）                     |

### ConditionData

本模块**未使用** ConditionData。

### 核心执行逻辑

```
每次执行（通常配合定时器每秒触发）：

1. 统计每个板块上的存活玩家数量
   - 遍历敌方阵营角色
   - 筛选条件：isKindPlayer() && !isDie()
   - 按 role.ai.navAreaInfo.areaIndex 归类到对应板块

2. 遍历每个板块 i (0 ~ areaCount-1)：
   if 板块i上的玩家数 < triggerNum:
     清零该板块计数 → delete(commonInt32, i)
   else:
     计数+1 → commonInt32[i]++
     if commonInt32[i] >= threshold:
       触发模块(triggerBaseModId + i)
       重置计数 → commonInt32[i] = 0
```

### 数据存储方式

使用**记录模块**（`Data[0]` 指定）的 `commonInt32` map 存储各板块的累计计数：
- **key**: 板块下标 `int32`（0 ~ n-1）
- **value**: 该板块玩家持续站立的累计执行次数

### 关键特性

1. **有人才计数，无人清零**：板块上玩家数不足时，该板块计数立即清零（`delete`），不是递减
2. **触发后重置**：达到阈值触发模块后，计数归零重新开始
3. **按板块独立触发**：每个板块独立计数，触发的模块ID = `triggerBaseModId + 板块下标`，实现分板块差异化效果
4. **需定时驱动**：本模块本身只是单次执行逻辑，需要配合 `time` 配置（如每秒触发）来实现持续检测

### 典型配置示例

假设需要"每个板块站满2人持续3秒后触发效果"：

| 参数 | 值 | 说明 |
|------|-----|------|
| `Data[0]` | recordModId | 记录模块 |
| `Data[1]` | 4 | 4个板块 |
| `Data[2]` | 3 | 持续3次（配合每秒触发=3秒） |
| `Data[3]` | 2 | 需要2个玩家 |
| `Data[4]` | baseModId | 触发模块起始ID |

板块0触发 `baseModId+0`，板块1触发 `baseModId+1`，以此类推。

---

## Q5: 壁垒系统+充能系统完整设计方案

### 需求拆解

| 编号 | 需求 | 类别 |
|------|------|------|
| A | 每秒每板块壁垒进度+10，每个玩家使进度-8 | 壁垒系统 |
| B | 进度>0壁垒增长，进度<0壁垒减少 | 壁垒系统 |
| C | 达到上限@1时停止增长 | 壁垒系统 |
| D | 壁垒进度>50红色，<50绿色 | 壁垒UI |
| E | 常规每秒充能@2点，充满@3触发增伤buff（@5值，@4持续时间） | 充能系统 |
| F | 镇龙柱buff期间停止自然充能 | 充能系统 |
| G | 场上每存在一个红色壁垒(>50)，充能效率降低20% | 壁垒↔充能联动 |

### 推荐方案：新增2个 action 类型

#### 类型1：壁垒板块进度管理（新增 entryType，例如 10125）

**职责**：每秒更新所有板块壁垒值，发送颜色状态战报

**Data 参数设计**：

| 索引 | 参数 | 说明 | 示例 |
|------|------|------|------|
| `Data[0]` | `recordModId` | 记录模块ID，`commonInt32` 存各板块壁垒值 | recordModId |
| `Data[1]` | `areaCount` | 板块数量 n | 4 |
| `Data[2]` | `addPerSec` | 每秒基础增速 | 10 |
| `Data[3]` | `reducePerPlayer` | 每个玩家削减值 | 8 |
| `Data[4]` | `maxVal` | 壁垒上限 @1 | 100 |
| `Data[5]` | `colorThreshold` | 红/绿色阈值 | 50 |
| `Data[6]` | `colorLogModId` | 颜色变化战报模块ID（用于通知客户端红/绿切换） | colorLogModId |

**伪代码**：
```
每秒执行：
  redCount = 0  // 统计红色壁垒数量，存到 recordMod.val 供充能模块读取
  遍历板块 i (0 ~ areaCount-1):
    playerCount = 统计板块i上存活玩家数
    delta = addPerSec - (playerCount * reducePerPlayer)
    壁垒值[i] += delta
    壁垒值[i] = clamp(壁垒值[i], 0, maxVal)

    // 颜色状态判断
    oldColor = recordMod.dataArr[i]  // 0=绿, 1=红
    newColor = 壁垒值[i] > colorThreshold ? 1 : 0
    if oldColor != newColor:
      recordMod.dataArr[i] = newColor
      触发 colorLogModId (携带板块i和颜色信息)  // 通知客户端

    if newColor == 1:
      redCount++

  recordMod.val = redCount  // 存红色壁垒数量，供充能模块读取
```

**存储方式**：
- `recordMod.commonInt32[i]` — 板块 i 的壁垒值（int32）
- `recordMod.dataArr[i]` — 板块 i 的颜色状态（0绿/1红）
- `recordMod.val` — 当前红色壁垒数量（供充能模块读取）

#### 类型2：带效率衰减的充能系统（新增 entryType，例如 10126）

**职责**：每秒充能，受红色壁垒数影响效率，充满触发增伤buff

**Data 参数设计**：

| 索引 | 参数 | 说明 | 示例 |
|------|------|------|------|
| `Data[0]` | `chargePerSec` | 每秒基础充能值 @2 | 5 |
| `Data[1]` | `maxEnergy` | 最大能量值 @3 | 100 |
| `Data[2]` | `buffModId` | 充满时触发的增伤buff模块ID | buffModId |
| `Data[3]` | `buffDuration` | buff持续时间 @4 (帧/毫秒) | 300 |
| `Data[4]` | `buffVal` | buff增伤值 @5 | 50 |
| `Data[5]` | `barrierRecordModId` | 壁垒记录模块ID（读取红色壁垒数量） | 同上面的 recordModId |
| `Data[6]` | `blockBuffType` | 阻止充能的buff类型（镇龙柱buff） | blockBuffType |
| `Data[7]` | `energyLogModId` | 能量变化战报模块ID | logModId |

**伪代码**：
```
每秒执行：
  // 1. 检查镇龙柱buff是否存在
  if source身上存在 blockBuffType 类型的buff:
    return  // 停止自然充能

  // 2. 读取红色壁垒数量
  barrierRecordMod = 查找 barrierRecordModId 模块
  redCount = barrierRecordMod.val

  // 3. 计算衰减后的充能效率
  efficiency = 1.0 - redCount * 0.2
  efficiency = max(efficiency, 0)  // 防止负值

  // 4. 充能
  actualCharge = chargePerSec * efficiency
  mod.val += actualCharge
  发送能量变化战报(energyLogModId)

  // 5. 充满判断
  if mod.val >= maxEnergy:
    mod.val = 0  // 重置能量
    触发 buffModId (duration=buffDuration, val=buffVal)
    // buff模块负责增伤效果
```

### 模块关系图

```
[每秒定时器 time]
    │
    ├──→ [10125 壁垒板块管理]
    │       ├── 统计各板块玩家数
    │       ├── 更新壁垒值 (+10 - 玩家数*8)
    │       ├── 判断颜色(>50红/<50绿)
    │       ├── 触发颜色战报 → 客户端切换红/绿特效
    │       └── 写入 redCount → recordMod.val
    │
    └──→ [10126 充能系统]
            ├── 检查镇龙柱buff → 有则跳过
            ├── 读取 recordMod.val → redCount
            ├── 效率 = 1 - redCount * 0.2
            ├── 充能 += chargePerSec * 效率
            ├── 发送能量战报 → 客户端更新能量条
            └── 充满 → 触发增伤buff模块 → 重置能量
                         │
                         └──→ [增伤buff] 持续@4时间，增伤@5值
```

### 与现有模块的对比

| 对比项 | 10124 (角色站立检测) | 新方案 10125 (壁垒管理) |
|--------|---------------------|----------------------|
| 计数方式 | 有人+1，无人清零 | 有人减，无人加（增减对抗） |
| 值域 | 0~threshold 整数计数 | 0~maxVal 壁垒值 |
| 触发逻辑 | 达阈值触发一次 | 持续更新，颜色变化时通知 |
| 额外输出 | 无 | 红色壁垒数量 → 供充能模块读取 |

| 对比项 | 453 (真理充能) | 新方案 10126 (效率充能) |
|--------|---------------|---------------------|
| 充能方式 | 按百分比增长 | 按固定值 * 效率增长 |
| 效率衰减 | 无 | 按红色壁垒数量降低20%/个 |
| buff阻断 | 无 | 检查特定buff存在则停止 |
| 充满效果 | 无 | 触发增伤buff |

### 实现建议

1. **壁垒模块 (10125)** 参考 `actionRoleStandAreaDetect` (`battle_entry_action_10000.go:3150`)，复用其板块遍历和 `commonInt32` 存储模式
2. **充能模块 (10126)** 参考 `actionAddChargeVal` (`battle_entry_action.go:6665`) 的充能模式 + `getGlobalModelArr` 检查buff存在的模式
3. **增伤buff** 使用现有buff框架即可，不需新增类型
4. **颜色战报** 参考 `addLogBattleValChanged` (`battle_mgr_log.go:2044`) 或新增专用战报
5. 两个模块都配合 `time` 配置为每秒触发

---

## Q6: 实现壁垒板块管理(10125)+带效率衰减充能(10126)两个action

### 修改文件

| 文件 | 修改内容 |
|------|---------|
| `battle_entry_type.go:1413-1414` | 新增 `entryTypeBarrierAreaManage = 10125`、`entryTypeChargeWithEfficiency = 10126` 常量 |
| `battle_entry_action.go:1357-1360` | 新增 case 分发到对应 action 函数 |
| `battle_entry_action_10000.go:3207+` | 实现 `actionBarrierAreaManage` 和 `actionChargeWithEfficiency` 两个函数 |

### actionBarrierAreaManage (10125) 实现

```go
// Data参数:
// [0] recordModId      记录模块ID
// [1] areaCount        板块数量
// [2] addPerSec        每秒基础增速(10)
// [3] reducePerPlayer  每个玩家削减值(8)
// [4] maxVal           壁垒上限 @1
// [5] colorThreshold   红/绿阈值(50)
// [6] colorLogModId    颜色变化战报模块ID

// 存储方式:
// recordMod.commonInt32[i] — 板块i壁垒值
// recordMod.dataArr[i]    — 板块i颜色状态(0绿/1红)
// recordMod.val           — 红色壁垒总数
```

**核心逻辑**：
1. 统计各板块存活玩家数（`getRoleGroupArr` + `navAreaInfo.areaIndex`）
2. 遍历板块：`壁垒值[i] += addPerSec - 玩家数 * reducePerPlayer`，clamp [0, maxVal]
3. 颜色判断：`>colorThreshold` → 红(1)，`<=colorThreshold` → 绿(0)
4. 颜色变化时触发 `colorLogModId` 通知客户端
5. 统计红色壁垒数写入 `recordMod.val`

### actionChargeWithEfficiency (10126) 实现

```go
// Data参数:
// [0] chargePerSec       每秒充能 @2
// [1] maxEnergy          最大能量 @3
// [2] buffModId          充满触发的增伤buff模块ID
// [3] barrierRecordModId 壁垒记录模块ID（读红色壁垒数）
// [4] blockBuffType      镇龙柱buff类型（存在则停充）
// [5] energyLogModId     能量变化战报模块ID
```

**核心逻辑**：
1. `hasGlobalActionModelArr(blockBuffType, source)` 检查镇龙柱buff → 有则 return
2. 读取壁垒记录模块的 `val` → 红色壁垒数 `redCount`
3. `efficiency = 1.0 - redCount * 0.2`，clamp >= 0
4. `mod.val += chargePerSec * efficiency`
5. 触发能量战报模块
6. `mod.val >= maxEnergy` → 重置为0，触发增伤buff模块

### 编译验证

`go build ./internal/battle/...` 编译通过，无错误。

---

## Q7: entryTypeDragon21EnergyVal(1743) 和 entryTypeDragon21EnergyValChanged(1744) 如何实现，dataCondition 和 data 参数含义



| 类型常量                                | 值    | 含义        |
| ----------------------------------- | ---- | --------- |
| `entryTypeDragon21EnergyVal`        | 1743 | 龙21 能量    |
| `entryTypeDragon21EnergyValChanged` | 1744 | 龙21 能量改变  |

### Data 和 ConditionData 通用说明

来自配置结构体 `data.EntryModel`（`internal/data/skill_entry_model.go:12`）：

| 字段 | Go类型 | JSON字段名 | 说明 |
|------|--------|-----------|------|
| `Data` | `[]int32` | `"data"` | **主参数数组**，定义模块"做什么"及具体数值 |
| `ConditionData` | `[][]int32` | `"dataCondition"` | **附加条件/扩展数据**，二维数组，用于额外条件判断或特殊逻辑分支 |

### entryTypeDragon21EnergyVal (1743) 实现

**位置**：`battle_entry_action_buff_monster.go:1576` — `actionDragon21EnergyVal(mod)`

**功能**：初始化龙21能量值

**Data 参数**：

| 索引 | 参数 | 说明 |
|------|------|------|
| `Data[0]` | `eventKind` | 事件类型ID，初始化完成后触发此事件 |
| `Data[1:]` | 传给 `battleEntryData.getData()` | 计算能量最大值 `maxVal` |

**执行逻辑**：
```
1. maxVal = getData(mod, Data[1:])     // 计算能量上限
2. mod.setMaxVal(maxVal)               // 设置最大值
3. mod.setVal(1)                       // 初始能量设为1
4. 发送能量变化战报                       // addLogBattleValChanged
5. 触发 Data[0] 指定的事件              // triggerEvent(eventKind, ...)
```

**ConditionData**：本模块未使用。

**注意**：`entryTypeDreamButterflyEnergyVal` 和 `entryTypeMonsterBlizzardVal` 共用同一个 action 函数。

### entryTypeDragon21EnergyValChanged (1744) 实现

**位置**：`battle_entry_action_buff_monster.go:1591` — `actionDragon21EnergyValChanged(mod)`

**功能**：修改龙21能量值（加/减/刷新）

**Data 参数**：

| 索引 | 参数 | 说明 |
|------|------|------|
| `Data[0]` | `eventKind` | 事件类型ID，能量变化后触发此事件 |
| `Data[1]` | `kind` | 操作类型：0=减，1=加，2=刷新（重置为0） |
| `Data[2]` | `actionKind` | 目标模块类型，用 `getGlobalActionModelArr` 查找目标身上的能量模块 |
| `Data[3:]` | 传给 `getData()` | 计算变化值 `addVal` |

**执行逻辑**：
```
1. 根据 kind 计算变化值:
   kind=0 → addVal = -getData(Data[3:])   // 减少
   kind=1 → addVal = +getData(Data[3:])   // 增加
   kind=2 → addVal = 0                    // 刷新（只触发战报，不改值）

2. 查找 target 身上所有 actionKind 类型的模块 modArr

3. 遍历 modArr 中每个能量模块 v:
   if addVal == 0:
     仅发送战报，不修改值
   else:
     result = v.getVal() + addVal
     result = clamp(result, 1, v.getMaxVal())  // 最小值为1，最大值为maxVal
     v.setVal(result)
     发送能量变化战报
     触发 eventKind 事件
```

**ConditionData**：本模块未使用。

**注意**：`entryTypeDreamButterflyEnergyValChanged` 和 `entryTypeMonsterBlizzardValChanged` 共用同一个 action 函数。

### 1743 + 1744 协作流程

```
[初始化阶段]
  1743 (EnergyVal) 执行:
    → 设置 maxVal（能量上限）
    → 设置 val = 1（初始能量）
    → 触发 eventKind 事件通知

[战斗过程中]
  1744 (EnergyValChanged) 被触发:
    → 找到 target 身上 actionKind 类型的模块（即 1743 创建的能量模块）
    → 对其 val 进行 加/减/刷新 操作
    → val 钳制在 [1, maxVal] 范围
    → 触发 eventKind 事件 → 其他模块可监听能量变化做后续逻辑
```

### 与 entryTypeSummon (825) 的关系

两者无直接关联，分属不同系统：
- **825 (Summon)**：召唤物系统，创建新战斗单位
- **1743/1744 (Dragon21Energy)**：Boss能量系统，管理能量值的初始化和增减

但在实际战斗中可能间接配合，例如龙21 Boss的某些能量变化可能触发召唤行为。

---

## Q8: entryTypeRandomVal(10045) 根据规则随机值模块详细介绍

### 概述

`entryTypeRandomVal` (10045) 是**根据规则随机值**模块，结果保存在 `mod.val` 中。支持多种随机策略（kind 1~14），可排除当前值、排除已有模块、按权重随机、条件分支等。

### 实现位置

| 功能 | 位置 |
|------|------|
| **Action 分发** | `battle_entry_action.go:1313` — 路由到 `actionRandomVal` |
| **Action 执行** | `battle_entry_action_10000.go:1256` — `actionRandomVal(mod)` |

### Data 参数 (`mod.conf.Data`)

| 索引 | 参数 | 说明 |
|------|------|------|
| `Data[0]` | `kind` | 随机类型（1~14） |
| `Data[1]` | `isTrigger` | 1=结束后触发trigger事件，2=触发筛选模块（`addChanceModWithElse`） |
| `Data[2:]` | 随机参数 | 各 kind 不同，详见下方 |

### kind 分类总览

| kind | 说明 | Data[2:] 格式 | 结果存储 |
|------|------|-------------|---------|
| **1** | 加权随机，**排除当前val** | `[id1, w1, id2, w2, ...]` | `mod.val = 随机id` |
| **2** | 加权随机，触发模块 | `[id1, w1, id2, w2, ...]` | `mod.val = 中选下标` |
| **3** | 根据敌方带特定模块的角色所在板块匹配 ConditionData | `[configIndex, modId, limitNum]` | `mod.val = 匹配的condition下标` |
| **4** | 随机延迟触发（定时器模式） | `[minTime, maxTime, _, triggerModId]` | 内部计时，到时触发 |
| **5** | 加权随机，**排除自身已有模块** | `[id1, w1, ...]` | `mod.val = 随机id` |
| **6** | 从前4个中随机，排除当前val | `[v0, v1, v2, v3]` | `mod.val = 随机值` |
| **7** | 直接赋值 | `[val]` | `mod.val = Data[2]` |
| **8** | 排除自身已有行为模块+排除当前val | `[actionType, id1, w1, ...]` | `mod.val = 随机id` |
| **9** | 随机选择与记录模块下标不同的 | `[recordModId, id1, w1, ...]` | `recordMod.val = 中选下标` |
| **10** | 加权随机，结果存到记录模块 | `[recordModId, id1, w1, ...]` | `recordMod.val = 中选下标` |
| **11** | 同9，下标按 `i/2` 计算 | `[recordModId, id1, w1, ...]` | `mod.val = 中选下标` |
| **12** | 设置记录模块的val | `[recordModId, setVal]` | `recordMod.val = setVal` |
| **13** | 加权随机，先设val再触发模块 | `[id1, w1, ...]` | `mod.val = 中选下标` |
| **14** | 条件分支：值>0触发mod1，否则触发mod2 | `[checkVal, mod1, mod2]` | 触发对应模块 |

### 各 kind 详细说明

#### kind=1：加权随机，排除当前 val

- **Data[2:]** = `[id1, w1, id2, w2, ...]`（id+权重对）
- 排除 `mod.val` 等于的那个 id，从剩余中加权随机
- 结果存 `mod.val = 随机id`
- `isTrigger=2` 时额外触发中选模块

**用途**：随机选一个与上次不同的技能/板块

#### kind=2：加权随机，触发模块

- **Data[2:]** = `[id1, w1, id2, w2, ...]`
- 不排除任何，直接加权随机
- **立即触发** 中选模块（`addChanceModWithElse`）
- 结果存 `mod.val = 中选下标`（不是 id）

**用途**：随机触发一组模块中的一个

#### kind=3：根据敌方角色所在板块匹配 ConditionData

- **Data[2:]** = `[configIndex, modId, limitNum]`
  - `configIndex`：CircleConfig 板块配置索引
  - `modId`：筛选条件——敌方角色身上必须有此模块
  - `limitNum`：最少需要几个符合条件的角色
- **ConditionData** = `[[areaA, areaB], [areaA, areaB], ...]` 匹配表
- 逻辑：
  1. 找敌方阵营中带有 `modId` 模块的角色
  2. 不足 `limitNum` 个则 return
  3. 取这些角色的板块ID（`getCircleAreaId`），降序排列
  4. 遍历 ConditionData 匹配 `[areaIds[0], areaIds[1]]`
  5. `mod.val = 匹配到的 ConditionData 下标`

**用途**：根据两个特定角色的板块位置组合，确定一个策略分支

#### kind=4：随机延迟触发（定时器模式）

- **Data[2:]** = `[minTime, maxTime, _, triggerModId]`
- 首次执行：随机生成 `[minTime, maxTime]` 之间的等待时间，存入 `mod.dataArr[0]`，计数器 `mod.dataArr[1]=0`
- 后续每次执行：计数器 +1，达到等待时间后触发 `triggerModId`，清空 dataArr
- 需要配合 `Time` 配置每秒调用

**用途**：随机倒计时后触发某个技能

#### kind=5：加权随机，排除 source 身上已有的模块

- **Data[2:]** = `[id1, w1, id2, w2, ...]`
- 用 `hasGlobalModIdModelArr` 检查 source 身上是否已有该模块 id
- 已有的排除，从剩余中加权随机
- 结果存 `mod.val = 随机id`
- `isTrigger=2` 时额外触发中选模块

**用途**：防止重复叠加同一个 buff/技能

#### kind=6：从前4个值中随机，排除当前 val

- **Data[2:]** = `[v0, v1, v2, v3]`
- 随机 0~3 下标取值，如果等于当前 `mod.val` 则重新随机（循环直到不同）
- 结果存 `mod.val`

**用途**：简单的4选1，不重复

#### kind=7：直接赋值

- **Data[2]** = 要设置的值
- `mod.val = Data[2]`

**用途**：手动设置值，不做随机

#### kind=8：排除已有行为模块 + 排除当前 val

- **Data[2]** = `actionType`（要检查的行为类型）
- **Data[3:]** = `[id1, w1, id2, w2, ...]`
- 先查 source 身上所有 `actionType` 类型的全局模块，取它们 `Data[0]` 对应的 id 加入排除列表
- 再排除当前 `mod.val`
- 从剩余中加权随机
- `isTrigger=2` 时额外触发中选模块

**用途**：排除当前正在执行的行为模块对应的选项

#### kind=9：随机选择与记录模块下标不同的

- **Data[2]** = `recordModId`
- **Data[3:]** = `[id1, w1, id2, w2, ...]`
- 排除 `recordMod.val` 对应的下标（按原始数组 index `i` 比较）
- 结果存到 **`recordMod.val = 中选下标`**（注意存到记录模块）
- `isTrigger=2` 时额外触发中选模块

**用途**：用记录模块跨模块共享"上次选了哪个"，避免连续重复

#### kind=10：加权随机，结果存到记录模块

- **Data[2]** = `recordModId`
- **Data[3:]** = `[id1, w1, id2, w2, ...]`
- 不排除，直接加权随机
- **立即触发** 中选模块
- `recordMod.val = 中选下标`

**用途**：随机触发+把结果存到记录模块供其他模块读取

#### kind=11：同 kind=9，下标按 `i/2` 计算

- 与 kind=9 逻辑一致，但排除和存储的下标用 `i/2` 而非 `i`
- 结果存到 **`mod.val`**（不是 recordMod）
- `isTrigger=2` 时额外触发中选模块

#### kind=12：设置记录模块的 val

- **Data[2]** = `recordModId`
- **Data[3]** = `setVal`（支持 `checkParam` 动态参数）
- `recordMod.val = setVal`

**用途**：直接设置记录模块的值，不做随机

#### kind=13：加权随机，先设 val 再触发模块

- **Data[2:]** = `[id1, w1, id2, w2, ...]`
- 加权随机，**先** `mod.val = 中选下标`，**后** 触发中选模块

**与 kind=2 的区别**：kind=2 先触发后设 val，kind=13 先设 val 后触发。后续模块通过 lastMod 链读 `mod.val` 时 kind=13 能拿到正确值。

#### kind=14：条件分支

- **Data[2:]** = `[checkVal, mod1, mod2]`
- `checkVal` 经 `checkParam` 处理后 >0 → 触发 `mod1`，否则 → 触发 `mod2`

**用途**：二选一条件分支

### isTrigger 收尾逻辑

所有 kind 执行完后：
- `isTrigger == 1`：调用 `mod.addTrigger()` 触发 trigger 事件链
- `isTrigger == 2`：在各 kind 内部已通过 `addChanceModWithElse` 触发（不是所有 kind 都支持）

### 特殊用途：板块定位

在 `battle_entry_target.go:3580`，目标系统会沿 `lastMod` 链向上查找 `entryTypeRandomVal` 类型的模块，取其 `val` 作为板块ID：

```go
if tempMod.getTyp() == entryTypeRandomVal {
    areaId = tempMod.getVal()
    break
}
```

这意味着 10045 的随机结果可以被后续模块（如召唤、板块伤害）读取用于目标定位。

---

## Q9: entryTypeTimeWait(1) 时间等待触发模块介绍

### 概述

`entryTypeTimeWait` (1) 是**时间等待触发**模块。它在 `emptyEntryActions` 中（`battle_entry_action.go:39`），**没有 action 逻辑**——`runAction` 时直接 return。

它的作用是**纯粹作为一个"存在"一段时间的占位符/定时器模块**，利用 `Time` 配置控制存活周期，模块的**存在与消失**驱动其他模块逻辑。

### 实现位置

| 功能 | 位置 |
|------|------|
| **类型定义** | `battle_entry_type.go:63` |
| **空 Action 注册** | `battle_entry_action.go:39` — `emptyEntryActions` |
| **Action 执行** | `battle_entry_action.go:754` — 命中 `emptyEntryActions` 后直接 return |

### Time 参数（驱动模块生命周期）

| Time 索引 | 参数 | 说明 |
|---|---|---|
| `Time[0]` | `duration` | 持续时间(ms)，-1=永久 |
| `Time[1]` | `num` | 执行次数，-1=无限 |
| `Time[2]` | `rate` | 执行间隔(ms) |
| `Time[3]` | `delay` | 是否延迟执行 |

模块每帧 `update(deltaTime)` 累加运行时间，当 `runTime >= duration` 时自动 `clear()` 销毁（`battle_entry_model.go:919`）。

### Data 参数的作用

虽然 `entryTypeTimeWait` 本身不执行 action，但 `Data` 仍然有用：

1. **作为标记/状态载体**：其他模块可以通过 `getGlobalModelArr(entryTypeTimeWait, ...)` 检测它是否存在，判断"某个时间窗口是否在进行中"
2. **配合 Trigger/Next**：通过配置表的 `trigger`（触发链）在模块开始时触发其他模块，模块结束时（duration 到期 `clear()`）通过 `onRemoveHook` 触发清除事件
3. **Buff 表现**：配合 `buff`、`buff_icon`、`effect` 等字段，给客户端显示持续时间的buff图标/特效
4. **被其他模块读取**：`Data` 中存储的值可以被其他模块通过 `lastMod` 链或 `checkParam` 读取

### 典型用途

```
场景：Boss蓄力3秒，期间需要标记状态

配置:
{
  "typ": 1,                    // entryTypeTimeWait
  "time": [3000, 1, 0, 0],    // 持续3000ms
  "trigger": [免疫控制模块ID],   // 开始时触发免疫
  "buff_icon": 图标ID,         // 客户端显示蓄力图标
  "data": [参数值]             // 供其他模块读取
}

效果：
  模块存活3秒 → 其他模块检测到 entryTypeTimeWait 存在 → 执行特殊逻辑
  3秒后自动销毁 → 触发清除事件 → 后续模块接管
```

### 与其他模块的对比

| 特性 | entryTypeTimeWait (1) | 普通 action 模块 |
|------|----------------------|----------------|
| action 执行 | 无（空操作） | 有具体逻辑 |
| Time 驱动 | 仅控制存活时长 | 控制执行次数和间隔 |
| 主要价值 | 存在性检测 + 触发链 | 执行具体行为 |
| Data 用途 | 存储参数供外部读取 | 作为 action 的执行参数 |

简单说：**`entryTypeTimeWait` 是一个"定时器/占位符"**，它不做事，但它的存在与消失可以驱动其他模块。

---

## Q10: 壁垒颜色变化战报用哪个type实现

### 背景

10125 壁垒板块管理模块中，颜色变化时需要通知客户端。原设计用 `colorLogModId` 通过 `addModelIDArr` 触发另一个模块发战报，但该模块应该用什么 type？

### 现有战报方案对比

| 方案 | 函数 | 传输内容 | 是否需要额外模块 |
|------|------|---------|---------------|
| `addLogBattleValChanged` | `battle_mgr_log.go:2045` | `val`, `maxVal`, `typ`, `roleKey` | 不需要，但 val 会与 redCount 冲突 |
| `addLogEffectAnimChange` | `battle_mgr_log.go:2062` | `modKey`, `kind`, `state` 三个 int32 | **不需要** |
| `addLogEffectObjShow` | `battle_mgr_log.go:2071` | `modKey`, `kind`, `val1`, `val2` 四个 int32 | 不需要 |
| 触发 1104/1061 等 Log 模块 | 各自的 action | 查找 recordMod 发 val | 需要额外配模块 |

### 推荐：直接用 `addLogEffectAnimChange`

**不需要额外的 `colorLogModId` 配置**，在10125代码中直接调用：

```go
if oldColor != newColor {
    if int32(len(recordMod.dataArr)) > i {
        recordMod.dataArr[i] = newColor
    }
    // modKey=板块索引, kind=颜色状态(0绿/1红), state=当前壁垒值
    source.mgr.log.addLogEffectAnimChange(int32(i), int32(newColor), recordMod.commonInt32[i])
}
```

客户端用 `logActionEffectAnimChanged` 接收，从三个参数中读取板块索引、颜色、壁垒值。

### 理由

1. `addLogEffectAnimChange` 正好传3个 int32，满足"板块索引+颜色+壁垒值"需求
2. 不需要额外配一个模块ID，减少配置复杂度
3. 不与 `recordMod.val`（存 redCount）冲突
4. 语义上"特效状态改变"与"壁垒颜色变化"吻合

---

## Q11: Battle Entry Data 的 Time 字段含义枚举

### 字段定义

`Time` 字段定义在 `internal/data/skill_entry_model.go` 的 `EntryModel` 结构体中，类型为 `[]int32`，固定 4 个元素。

配置加载时会校验 `len(Time) >= 4`，不足 4 个元素会报错（`skill_entry_model.go:148-152`）。

### 索引含义（定义于 `internal/battle/battle_entry_time.go:4-8`）

| 索引 | 常量名 | 含义 | 单位 | 特殊值 |
|------|--------|------|------|--------|
| 0 | `timeIndexDuration` | 持续时间 | 毫秒(ms) | `-1` = 无时间限制 |
| 1 | `timeIndexNum` | 执行次数 | 次 | `-1` = 无次数限制 |
| 2 | `timeIndexRate` | 执行间隔 | 毫秒(ms) | `0` = 自动计算 |
| 3 | `timeIndexDelay` | 是否延迟执行 | — | `>0` 触发延迟 |

### 处理函数 `getTimeConf`（`battle_entry_time.go:27-50`）

**单位转换**：
- `duration` 和 `rate` 在内部会乘以 `0.001`，从毫秒转换为秒
- 值为 `-1` 时保持不变，代表"无限"

**自动计算逻辑**：
- 若 `rate > 0 && num == 0`：自动计算 `num = duration / rate`（根据间隔推算次数）
- 若 `num > 0 && rate == 0`：自动计算 `rate = duration / num`（根据次数推算间隔）

**延迟处理**：
- 若 `delay > 0`：调用 `mod.resetInterval()` 重置间隔倒计时，模块创建后先等待一个 interval 再首次执行

### 设置函数 `setEntryModelTime`（`battle_entry_time.go:77-91`）

```
1. 解析4个参数: duration, num, rate, delay = getTimeConf(mod, timeConf)
2. setEntryModelTimeDuration(mod, duration)  // 设置持续时间
3. setEntryModelTimeRate(mod, rate)          // 设置执行间隔
4. setEntryModelTimeNum(mod, num)            // 设置执行次数
5. setEntryModelTimeDelay(mod, delay)        // 设置延迟标记
6. if delay > 0: mod.resetInterval()        // 延迟时重置间隔
7. if IsResetNumLimit(): num = duration / rate  // 重算次数限制
```

### 模块运行时如何使用 Time

在 `battle_entry_model.go:877` 的 `update(deltaTime)` 中：

```
每帧调用:
  runTime += deltaTime             // 累加运行时间
  updateInterval(deltaTime)        // 更新间隔倒计时

  // 间隔到期 → 执行 action
  if intervalCountdown <= 0:
    执行 runAction()
    重置 intervalCountdown = rate
    已执行次数++

  // 次数耗尽 → 销毁
  if num != -1 && 已执行次数 >= num:
    clear()

  // 时间耗尽 → 销毁
  if duration != -1 && runTime >= duration:
    clear()
```

### 常见配置示例

| 配置 | Time 数组 | 含义 |
|------|-----------|------|
| 每秒执行，持续10秒 | `[10000, 10, 1000, 0]` | 持续10s，每1s执行1次，共10次，立即开始 |
| 每秒执行，无限持续 | `[-1, -1, 1000, 0]` | 无时间限制，无次数限制，每1s执行 |
| 延迟1秒后执行1次 | `[1000, 1, 0, 1]` | 持续1s，执行1次，延迟执行 |
| 执行1次立即生效 | `[0, 1, 0, 0]` | 持续0s，执行1次，无间隔，立即执行 |
| 动态伤害延迟 | `[damageTime*1000, 1, 0, 1]` | 动态持续时间，执行1次，延迟 |
| 多段伤害 | `[duration, count, 200, 0]` | 每200ms一次，共count次 |

---

## Q12: 在某个板块播放特效应该用哪个type实现

### 两个主要方案

#### 方案一：`entryTypeShowAreaEffect` (86) — 在指定坐标播特效

适合在**指定位置**播放一个独立特效（非buff），常用于怪物预警。实现位于 `battle_entry_action.go:2618`。

**kind=1（以施法者位置为中心）：**

| 索引 | 参数 | 说明 |
|------|------|------|
| `Data[0]` | `kind` | `1` |
| `Data[1]` | `effectId` | 特效资源ID |
| `Data[2]` | `time` | 持续时间（客户端会 *0.001） |
| `Data[3]` | `angle` | 角度 |

**kind=2（指定世界坐标）：**

| 索引 | 参数 | 说明 |
|------|------|------|
| `Data[0]` | `kind` | `2` |
| `Data[1]` | `effectId` | 特效资源ID |
| `Data[2]` | `time` | 持续时间 |
| `Data[3]` | `posX` | X坐标（*0.01精度） |
| `Data[4]` | `posZ` | Z坐标（*0.01精度） |
| `Data[5]` | `angle` | 角度 |

客户端收到 `logActionAreaEffect` 战报：`sourceKey, kind, effectId, time, posX, posY, posZ, angle`。

#### 方案二：`entryTypeUpdateBuffEffect` (87) — 按板块索引播特效

适合在**指定板块**上播特效，需要关联一个技能模块（buff特效）。特效在模块**移除时触发**（`onRemoveHookFun`）。实现位于 `battle_entry_action.go:2528`。

**kind=2（直接指定板块索引）：**

| 索引 | 参数 | 说明 |
|------|------|------|
| `Data[0]` | `kind` | `2` |
| `Data[1]` | `configId` | 配置ID（特效配置） |
| `Data[2]` | `areaIndex` | 直接指定的板块索引 |
| `Data[3]` | `skillModId` | 关联的技能模块ID（取其 modKey 发给客户端） |

**kind=3（取上个模块目标的板块）：**

| 索引 | 参数 | 说明 |
|------|------|------|
| `Data[0]` | `kind` | `3` |
| `Data[1]` | `configId` | 配置ID |
| `Data[2]` | （未使用） | — |
| `Data[3]` | `skillModId` | 关联的技能模块ID |

area 自动取 `lastMod.target.ai.navAreaInfo.areaIndex`。

客户端收到 `logActionCommonAreaEffect` 战报：`sourceKey, kind, modKey, configId, areaIndex`。

### 选择建议

| 场景 | 推荐 | 原因 |
|------|------|------|
| 在固定世界坐标播特效 | **86 kind=2** | 直接指定 X/Z，简单直接 |
| 在施法者位置播特效 | **86 kind=1** | 自动取 source 位置 |
| 在指定板块索引播特效 | **87 kind=2** | 按 areaIndex 定位，适合板块系统 |
| 在目标所在板块播特效 | **87 kind=3** | 自动读取目标的 areaIndex |

如果需求是"在某个板块播特效"，**推荐用 87 kind=2**，直接在 `Data[2]` 填板块索引。注意 87 是在模块移除时触发（`onRemoveHookFun`），需要配合 `Time` 控制模块存活时间。

---

## Q13: addLogShowCommonAreaEffect 如何实现板块常驻特效

### 核心机制

`logActionCommonAreaEffect`（日志action=124）的注释为：

```go
logActionCommonAreaEffect BattleLogAction = 124 // 通用预警特效 需要一直显示的预警特效
```

客户端收到后会**常驻显示**该特效，直到关联的技能模块被移除。

### 实现方式：两个模块配合

**1. 技能模块（特效载体）** — 持续存在，决定特效的生命周期

```json
{
  "id": 9300541,
  "typ": 1,
  "time": [-1, -1, 1000, 0],
  "effect": 特效资源ID
}
```

- `Time[-1,-1,...]` 让模块永久存活
- `effect` 字段配置客户端特效资源
- 该模块的运行时 `modKey` 会传给客户端，客户端用它关联特效的显示/销毁

**2. Type 87 模块（定位器）** — 告诉客户端把特效放在哪个板块

```json
{
  "typ": 87,
  "time": [100, 1, 0, 0],
  "data": [2, 13, 1, 9300541]
}
```

- `Time` 控制模块何时移除（87 在 `onRemoveHookFun` 移除时才发战报）
- 移除时发送 `addLogShowCommonAreaEffect(sourceKey, modKey, kind=2, configId=13, areaIndex=1)`
- 客户端收到后，将模块 9300541 的特效常驻显示在板块 1 上

### modelKey 的作用

`modelKey` 是技能模块 9300541 在运行时分配的实例唯一键（`v.key`），传给客户端用于：

1. **关联特效生命周期** — 客户端通过 `modKey` 把特效绑定到该模块实例，模块清除时客户端同步移除特效
2. **定位更新** — 如果再发一次携带相同 `modKey` 的战报，客户端可以把特效移到新板块，而非创建新特效

### 流程

```
87模块创建 → Time到期移除 → onRemoveHookFun触发
  → 找到source身上的模块9300541，取其modKey
  → 发送 logActionCommonAreaEffect 战报
  → 客户端在板块1常驻显示特效

9300541模块被清除时 → 客户端移除特效
```

常驻的关键：特效跟随的是技能模块 9300541 的生命周期，87 只是告诉客户端"把特效放到哪个板块"。

---

## Q14: addLogEffectStart 如何配置type和参数发送常驻特效

### 触发条件

`addLogEffectStart` 不是通过特定的 `typ` 触发，而是一个**通用机制**——任何模块只要配置了 `effect` 或 `buff_icon` 字段，在模块创建/刷新时自动发送。

```go
// battle_entry_model.go:1508
func (mod *BattleEntryModel) isLogEffectToClient() bool {
    return mod.getConf().HasEffect() || mod.getConf().HasBuffIcon()
}
```

即配置表中 `effect != 0` 或 `buff_icon > 0` 时，模块自动调用 `addLogEffectStart`。

### 战报内容（logActionEffectStart，共20个字段）

| 索引 | 内容 | 说明 |
|------|------|------|
| +0 | `mod.key` | 模块实例key |
| +1 | `effect` | 特效资源ID |
| +2 | `skiData.conf.ID` | 技能ID |
| +3 | `target.key` | 目标角色key |
| +4 | `BuffIcon` | buff图标ID |
| +5 | `layerNum` | 层数 |
| +6 | `Typ` | 模块类型 |
| +7 | `source.key` | 来源角色key |
| +8~10 | `pos X/Y/Z` | 目标位置 |
| +11 | `duration` | 持续时间 |
| +12 | `runTime` | 已运行时间 |
| +13 | `mod.conf.ID` | 模块配置ID |
| +14 | `lastMod.key` | 上个模块key（需 `IsLastModKey` 配置） |
| +15~18 | 初始位置参数 | 仅 `typ=10074` 时使用 |

### 实现常驻特效的配置

#### 方式一：最简单 — 用 typ:1（时间等待）+ effect

```json
{
  "id": 模块ID,
  "typ": 1,
  "time": [-1, -1, 1000, 0],
  "effect": 特效资源ID,
  "data": []
}
```

- `effect` 非0 → 自动触发 `addLogEffectStart`，客户端播放特效
- `time: [-1,-1,...]` → 模块永不销毁，特效常驻
- 模块被 `clear()` 时自动调用 `addLogEffectEnd`，客户端移除特效

#### 方式二：需要指定初始位置 — 用 typ:10074（entryTypeBuffModelWithInitPos）

```json
{
  "typ": 10074,
  "effect": 特效资源ID,
  "time": [-1, -1, 1000, 0],
  "data": [1, posX, posZ, angle]
}
```

`addLogEffectStart` 中对 `typ=10074` 的特殊处理：

| Data[0] | 含义 | Data[1:] |
|---------|------|----------|
| `1` | 直接指定坐标 | `posX, posZ, angle` → 写入战报 +16, +17, +18 |
| `2` | 读取风向模块决定角度 | `windModID` + 4个方向angle |
| `3` | 读取记录模块方向决定坐标 | `recordModID` + 多组 `posX, posZ, angle` |

战报 +15 位设为 `1` 标记有初始位置，客户端据此设定特效的初始坐标而非默认放在目标身上。

### 两种常驻特效对比

| 特性 | `addLogEffectStart`（effect字段） | `addLogShowCommonAreaEffect`（type 87） |
|------|------|------|
| 定位方式 | 跟随 target 角色位置，或 10074 指定坐标 | 按板块索引定位 |
| 生命周期 | 模块存活期间常驻 | 关联的技能模块存活期间常驻 |
| 适用场景 | 角色身上的buff特效 | 场景板块上的区域特效 |
| 配置复杂度 | 简单，只需 `effect` 字段 | 需两个模块配合 |

---

## Q15: Battle Model 的 Buff 字段枚举含义

### 字段定义

`Buff` 字段定义在 `internal/data/skill_entry_model.go` 的 `EntryModel` 结构体中，类型为 `uint32`，对应枚举类型 `BattleEntryModelBuff`（`battle_entry_type.go:1850`）。

### 枚举值

| 值 | 常量 | 含义 | 说明 |
|---|---|---|---|
| `-1` | `modelBuffAll` | 全部（查询用） | 仅用于 `getModelArrWithTargetBuffKind` 查询时匹配所有 buff/debuff，不用于配置 |
| `0` | `modelBuffNil` | 无 | 非buff模块，不参与buff相关逻辑（不触发buff事件、死亡时不清除） |
| `1` | `modelBuffBuff` | 增益buff | 参与buff事件触发、死亡时清除、可被驱散等 |
| `2` | `modelBuffDebuff` | 减益debuff | 同上，属于负面效果 |
| `3` | `modelShowBuffEffect` | 显示buff特效 | 用于 `getModelArrWithExtraBuff` 查询额外buff特效，不算真正的buff/debuff |
| `4` | `modelMustShowBuffEffect` | 强制显示buff特效 | 特殊：**目标死亡时不清除**（`battle_entry.go:705,1217`），确保特效在角色死后仍然显示 |

### 关键行为差异

- **0（nil）**：不触发buff事件，死亡不清除
- **1/2（buff/debuff）**：触发 `triggerEventModRefreshOrAdd` 等buff事件，目标死亡时清除，`isBuffOrDebuff()` 为 true
- **3（showBuffEffect）**：不算 buff/debuff，仅作为额外特效显示，死亡时清除
- **4（mustShowBuffEffect）**：死亡时**不清除**，即使目标死了也能添加和保留，适合需要在死亡角色身上持续显示的特效（如复苏光圈）

### 相关判断函数（`battle_entry_model.go:753-777`）

| 函数 | 逻辑 |
|------|------|
| `isBuff()` | `buff == 1` |
| `isDebuff()` | `buff == 2` |
| `isBuffOrDebuff()` | `buff == 1 \|\| buff == 2` |
| `isNotNilBuffKind()` | `buff != 0` |
| `isMustShowBuffKind()` | `buff == 4` |
| `isNilBuffKind()` | `buff == 0` |

---

## Q16: 新增 10070 opType=12 按板块方向+半径定位特效

### 需求

在指定板块方向、距离自身（source）一定半径的位置上放置特效。现有 10070 的 opType 都不支持"给定板块配置+板块索引+半径→自动计算世界坐标"。

### 实现

在 `actionPostBuffModelEffectPos`（`battle_entry_action.go`）末尾新增 `opType == 12` 分支。

代码
```go
	effectModID := mod.getConf().Data[1]  
    areaConfIdx := mod.getConf().Data[2]  
    areaIdx := mod.getConf().Data[3]  
    radius := float64(mod.getConf().Data[4]) * 0.01  
    posY := int32(0)  
    if len(mod.getConf().Data) > 5 {  
       posY = mod.getConf().Data[5]  
    }  
    rotY := int32(0)  
    if len(mod.getConf().Data) > 6 {  
       rotY = mod.getConf().Data[6]  
    }  
  
    source := mod.source  
    target := mod.getTarget()  
    if source == nil {  
       return  
    }  
  
    // areaIndex=-1 时从 target 自动检测板块  
    if areaIdx < 0 {  
       if target == nil {  
          return  
       }  
       areaIdx = int32(mod.ent.tar.getCircleAreaId(source, target, areaConfIdx))  
       if areaIdx < 0 {  
          return  
       }  
    }  
  
    // 获取板块配置，计算板块中心角度  
    config := data.GetCircleConfig(areaConfIdx)  
    anglesArr := config[2:]  
    if int(areaIdx) >= len(anglesArr) {  
       return  
    }  
  
    var centerAngle float64  
    if areaIdx == 0 {  
       centerAngle = float64(anglesArr[0]) / 2  
    } else {  
       centerAngle = (float64(anglesArr[areaIdx-1]) + float64(anglesArr[areaIdx])) / 2  
    }  
  
    // 将 source→configStart 方向旋转 centerAngle 度，取 radius 距离的位置  
    sourcePos := source.ai.getPos()  
    startX := config[0]  
    startZ := config[1]  
    dx := float64(startX - sourcePos.X)  
    dz := float64(startZ - sourcePos.Z)  
    rad := centerAngle * math.Pi / 180  
    rotX := dx*math.Cos(rad) + dz*math.Sin(rad)  
    rotZ := -dx*math.Sin(rad) + dz*math.Cos(rad)  
    length := math.Sqrt(rotX*rotX + rotZ*rotZ)  
    pos := geometry.NewVector3(  
       sourcePos.X+float32(radius*rotX/length),  
       sourcePos.Y,  
       sourcePos.Z+float32(radius*rotZ/length),  
    )  
  
    TestLog(TestLogOpType12Pos, mod.getConf().ID, effectModID, areaConfIdx, areaIdx, radius,  
       sourcePos.X, sourcePos.Y, sourcePos.Z,  
       startX, startZ,  
       centerAngle,  
       pos.X, pos.Y, pos.Z)  
  
    // 发送位置变更战报（opType=1 让客户端按绝对坐标处理）  
    modArr := mod.ent.getGlobalModelArrByEntryTypeAndModId(entryKindSkill, source, uint32(effectModID))  
    if len(modArr) > 0 {  
       effectMod := modArr[0]  
       netX := battleMgrNet.getNetSend32(pos.X)  
       netZ := battleMgrNet.getNetSend32(pos.Z)  
       effectMod.ent.mgr.log.addLogBuffModelEffectPosChange(  
          effectMod, 12,  
          netX,  
          posY,  
          netZ,  
          rotY,  
       )  
       TestLog(TestLogOpType12PosOK, effectModID, effectMod.getConf().ID, netX, netZ)  
    } else {  
       TestLog(TestLogOpType12PosFail, effectModID)  
    }  
```
### Data 参数

```
Data[0] = 12            // opType
Data[1] = effectModID   // 要定位的特效模块ID
Data[2] = areaConfIdx   // 板块配置索引（GetCircleConfig 的 index）
Data[3] = areaIndex     // 目标板块索引（-1 = 从 target 自动检测）
Data[4] = radius        // 半径（*0.01 精度，如 500 = 5.0 世界单位）
```

### 核心计算逻辑

```
1. 读取参数：effectModID, areaConfIdx, areaIdx, radius(*0.01)
2. areaIdx == -1 时，通过 getCircleAreaId(source, target, areaConfIdx) 自动检测 target 所在板块
3. 获取板块配置 GetCircleConfig(areaConfIdx) → [startX, startZ, angle0, angle1, ...]
4. 计算板块中心角度:
   - area 0: centerAngle = angles[0] / 2
   - area i: centerAngle = (angles[i-1] + angles[i]) / 2
5. 计算参考方向角度: refAngle = atan2(startZ - sourceZ, startX - sourceX) * 180/π
6. 世界角度: worldAngle = refAngle + centerAngle
7. 世界坐标: pos = getCenterRadiusPos(sourcePos, radius, worldAngle)
8. 发送战报: addLogBuffModelEffectPosChange(effectMod, opType=1, netX, 0, netZ)
```

### 复用函数

| 函数 | 文件 | 用途 |
|------|------|------|
| `data.GetCircleConfig(index)` | `internal/data/battle_conf.go:388` | 获取板块角度边界配置 |
| `getCenterRadiusPos(pos, radius, angle)` | `battle_vector3.go:114` | 从中心+半径+角度计算世界坐标 |
| `getCircleAreaId(source, target, idx)` | `battle_entry_target.go:3214` | 检测 target 所在板块索引 |
| `battleMgrNet.getNetSend32(float32)` | `battle_mgr_net.go:36` | float32 → int32 网络编码（*100精度） |

### 客户端处理

发送给客户端的是 `addLogBuffModelEffectPosChange` opType=1（绝对坐标），客户端无需新增处理逻辑，按已有的绝对坐标定位即可。

### 配置示例

```json
{
  "typ": 10070,
  "time": [-1, 1, 0, 0],
  "data": [12, 9300541, 1, 2, 500]
}
```

含义：将特效模块 9300541 定位到板块配置1的板块2方向、距离 source 5.0 世界单位处。

---

## Q17: Time 字段 delay 配置为什么不生效

### 问题

配置 `"time": [0, 1, 0, 500]` 期望延迟 500ms 触发，但实际没有延迟。

### 原因分析

`delay`（Time[3]）字段本身只是被存储起来，**实际延迟机制靠的是 `resetInterval()` 把 `rate` 加到 `intervalCountdown` 上**。

关键代码在 `battle_entry_time.go:83`：

```go
func setEntryModelTime(mod *BattleEntryModel, timeConf []int32) {
    duration, num, rate, delay := getTimeConf(mod, timeConf)
    // ...
    if delay > 0 {
        mod.resetInterval()  // intervalCountdown += rate
    }
}
```

`resetInterval()` 的实现（`battle_entry_model.go:484`）：

```go
func (mod *BattleEntryModel) resetInterval() {
    if mod.getRate() <= 0 {
        return
    }
    mod.intervalCountdown += mod.getRate()
}
```

而判断是否可以执行的 `isRunRateLimit()`（`battle_entry_model.go:960`）：

```go
func (mod *BattleEntryModel) isRunRateLimit() bool {
    return mod.intervalCountdown > 0
}
```

当 `rate = 0` 时：
1. `resetInterval()` 里 `rate <= 0` 直接 return，`intervalCountdown` 仍为 0
2. `isRunRateLimit()` 返回 false，不会阻塞执行
3. 所以 delay 标记虽然设了，但没有实际延迟效果

### delay 的本质

**`delay` 不是独立的延迟时长**，它只是一个标记："首次执行也要等一个 interval 周期"。真正控制等待时长的是 `rate`（Time[2]）。

### 正确配置方式

如果想延迟 500ms 触发一次：

```json
"time": [0, 1, 500, 0]
```

- `duration=0`, `num=1`, `rate=500`（500ms）, `delay=0`
- 自动计算：`num>0 && rate==0` 不满足，`rate>0 && num==0` 也不满足，直接使用原值
- rate=500ms 作为执行间隔，首次触发前等待 500ms

或者让 rate 和 delay 一致：

```json
"time": [500, 1, 500, 500]
```

- `duration=500ms`, `num=1`, `rate=500ms`, `delay=500`
- delay>0 触发 `resetInterval()`，把 rate(500ms) 加到 intervalCountdown
- 首次执行需要等 intervalCountdown 倒计时到 0，即等待 500ms

### 总结

| 配置 | 效果 |
|------|------|
| `[0, 1, 0, 500]` | delay 标记生效但 rate=0 导致无实际延迟，**立即触发** |
| `[0, 1, 500, 0]` | rate=500ms 作为间隔，首次等 500ms 后触发 |
| `[500, 1, 500, 500]` | delay+rate 配合，首次等 500ms 后触发 |

---

## Q18: addLogEffectObjShow 和 entryTypeEffectObjShow(10097) 特效分层显示介绍

### 概述

`addLogEffectObjShow` 是通知客户端改变特效显示的战报函数，对应日志 action `logActionEffectObjShow` (83)。服务端通过 `entryTypeEffectObjShow` (10097) action 触发，可以根据游戏状态动态改变特效的分层/阶段显示。

### 战报函数

**位置**：`battle_mgr_log.go:2071`

```go
func (log *BattleMgrLog) addLogEffectObjShow(modKey int32, kind int32, val1 int32, val2 int32)
```

**战报字段**（`logActionEffectObjShow`，共 4 个数据字段）：

| 字段 | 索引 | 含义 |
|------|------|------|
| `modKey` | `+0` | 目标特效模块的运行时 key |
| `kind` | `+1` | 类型（区分不同处理逻辑，供客户端分支） |
| `val1` | `+2` | 自定义值1 |
| `val2` | `+3` | 自定义值2 |

客户端收到 `logActionEffectObjShow` (83) 后，根据 `kind` + `val1/val2` 更新特效显示。

### Action 实现：entryTypeEffectObjShow (10097)

**位置**：`battle_entry_action_10000.go:2293` — `actionEffectObjShow(mod)`

**类型定义**：`battle_entry_type.go:1390` — 注释"对应模块特效显示 阴阳怪器使用"

**分发**：`battle_entry_action.go:1328` — `case entryTypeEffectObjShow → actionEffectObjShow(mod)`

### Data 参数

#### kind=1（计算板块点数）— 目前唯一实现

| 索引 | 参数 | 说明 |
|------|------|------|
| `Data[0]` | `modId` | 目标特效模块配置 ID（通过 `getGlobalModelArrByModelId` 找运行时实例） |
| `Data[1]` | `kind` | `1` — 计算板块点数模式 |
| `Data[2]` | `tpe` | 计算用的 `BattleEntryType` |
| `Data[3]` | `configIndex` | 板块配置索引 |
| `Data[4]` | `areaIdA` | 板块A索引 |
| `Data[5]` | `areaIdB` | 板块B索引 |

**执行逻辑**：
```
1. 在 target 身上查找 modId 对应的运行时模块 → 取其 key
2. 调用 calculatePoints 分别计算板块A和板块B的点数
3. addLogEffectObjShow(modKey, kind=1, numA, numB)
4. 客户端根据 numA/numB 更新特效显示（如阴阳两侧的点数）
```

### 根据 recordModId 值修改特效分层：扩展 kind=2

若需求是"读取某个记录模块的 val，通知客户端切换特效分层"，可在 10097 中新增 `kind=2`：

**Data 参数设计**：

| 索引 | 参数 | 说明 |
|------|------|------|
| `Data[0]` | `modId` | 目标特效模块配置 ID |
| `Data[1]` | `kind` | `2` — 读取记录模块值模式 |
| `Data[2]` | `recordModId` | 记录模块 ID（读取其 val） |

**伪代码**：
```go
if kind == 2 {
    recordModId := uint32(dataArr[2])
    recordModArr := ent.getGlobalModelArrByModelId(recordModId, modelKindAction, target)
    if len(recordModArr) > 0 {
        val := int32(recordModArr[0].getVal())
        modKey := int32(modelArr[0].key)
        target.mgr.log.addLogEffectObjShow(modKey, kind, val, 0)
    }
}
```

**客户端处理**：收到 `kind=2, val1=记录模块的值`，根据 `val1` 切换特效的分层/阶段显示。

### 与其他特效通知方式的对比

| 方式 | logAction | 传输内容 | 适用场景 |
|------|-----------|---------|---------|
| `addLogEffectAnimChange` | 82 | `modKey, kind, state`（3个值） | 简单状态切换（如红/绿） |
| `addLogEffectObjShow` | 83 | `modKey, kind, val1, val2`（4个值） | 需要传两个自定义值的分层显示 |
| `addLogEffectStart` | 13 | 完整20字段含 `layerNum` | 模块创建/刷新时的完整特效信息 |
| `entryTypeChangeModelEffectId` (10003) | — | 替换特效资源ID | 完全切换不同特效资源 |

---

## Q19: entryTypeSetLayerNum(10013) 设置模块层数介绍

### 概述

`entryTypeSetLayerNum` (10013) 是**设置模块层数**的通用 action，可以对目标身上指定模块的 `layerNum`（当前层数）、`layerMax`（最大层数）、`layerDefault`（默认层数）进行增加、减少或直接赋值操作，并可选同步特效给客户端。

### 实现位置

| 功能 | 位置 |
|------|------|
| **类型定义** | `battle_entry_type.go:1310` |
| **Action 分发** | `battle_entry_action.go:1243` — `case entryTypeSetLayerNum → actionSetLayerNum(mod)` |
| **Action 执行** | `battle_entry_action_10000.go:274` — `actionSetLayerNum(mod)` |

### Data 参数

| 索引 | 参数 | 说明 |
|------|------|------|
| `Data[0]` | `modelID` | 目标模块配置 ID |
| `Data[1]` | `modifyTarget` | 修改目标：`0`=当前层数，`1`=最大层数，`2`=默认层数 |
| `Data[2]` | `overflowCheck` | 溢出控制：`0`=允许超出限制，`1`=受默认值和最大值 clamp |
| `Data[3]` | `howTo` | 操作方式：`0`=减少，`1`=增加，`2`=直接设为 |
| `Data[4]` | `checkSource` | `1`=需要来源匹配（只修改同一来源的模块），`0`=不需要 |
| `Data[5]` | `isSync` | `1`=修改后同步特效给客户端（调用 `syncModelShow`），`0`=不同步 |
| `Data[6:]` | `newVal` | 数值（经 `getData` 处理，支持动态参数） |

### 执行逻辑

```
1. 在 target 身上查找 modelID 对应的所有模块
2. 若 checkSource=1，跳过来源不匹配的模块
3. 读取原值:
   modifyTarget=0 → val = model.getLayerNum()      // 当前层数
   modifyTarget=1 → val = model.getLayerNumMax()    // 最大层数
   modifyTarget=2 → val = model.getLayerNumDefault() // 默认层数
4. 操作:
   howTo=0 → val -= newVal   // 减少
   howTo=1 → val += newVal   // 增加
   howTo=2 → val = newVal    // 直接设为
5. 赋新值:
   modifyTarget=0 → model.setLayerNum(val)
   modifyTarget=1 → model.setAddMaxLayerNum(val)
   modifyTarget=2 → model.setAddLayerNum(val)
6. overflowCheck=1 时:
   val = clamp(val, layerDefault, layerMax)
   model.setLayerNum(val)
7. isSync=1 时:
   syncModelShow(model) → 重新发送 addLogEffectStart → 客户端刷新特效
```

### 关键细节

- **`setLayerNum`** 会触发 `onLayerNumChange()`，若配置了 `LayerLimit`，达到限制时会 `addTrigger()` + `clear()`
- **`isSync=1`**（Data[5]）是特效分层的关键：调用 `syncModelShow` → 内部调用 `addLogEffectStart` → 客户端收到新的 `layerNum`（日志字段 +5）刷新特效显示
- **`getData`** 处理 `Data[6:]`，支持动态参数（如从其他模块读取值）

### 配置示例

#### 示例1：直接设置 layerNum=2 并同步客户端

```json
{
  "typ": 10013,
  "time": [0, 1, 0, 0],
  "data": [9300541, 0, 0, 2, 0, 1, 2]
}
```

含义：把模块 9300541 的当前层数直接设为 2，不限溢出，不检查来源，同步客户端。

#### 示例2：层数+1，受范围限制

```json
{
  "typ": 10013,
  "time": [0, 1, 0, 0],
  "data": [9300541, 0, 1, 1, 0, 1, 1]
}
```

含义：把模块 9300541 的当前层数 +1，clamp 在 [default, max] 范围内，同步客户端。

#### 示例3：层数-1，不同步

```json
{
  "typ": 10013,
  "time": [0, 1, 0, 0],
  "data": [9300541, 0, 0, 0, 0, 0, 1]
}
```

含义：把模块 9300541 的当前层数 -1，不限溢出，不同步客户端。

### 相关 layerNum 类型一览

| 类型 | 值 | 说明 |
|------|-----|------|
| `entryTypeSetLayerNum` | 10013 | 设置模块层数（通用） |
| `entryTypeSetLayerNum1` | 10053 | 10013 加强版 |
| `entryTypeSetLayerNum2` | 10049 | 设置层数，和刷新模块逻辑相同 |
| `entryTypeSetLayerNum3` | 10050 | 根据来源模块层数修改 |
| `entryTypeSetLayerNum4` | 10063 | 修改模块层数 |
| `entryTypeChangeLayerNum` | 10012 | 修改模块层数 |
| `entryTypeChangeModMaxLayerNum` | 10007 | 修改指定 type 模块的最大层数 |
| `entryTypeChangeModMaxLayerNumEx` | 10029 | 修改最大层数（支持搜索来源/目标） |
| `entryTypeRandomInitLayerNum` | 1075 | 随机初始化模块层数 |
| `entryTypeRunModelByLayerLimit` | 10113 | 根据层数启动模块 |
| `entryTypeWhenLayerNum` | 20002 | 当模块层数满足条件时触发（事件） |
| `entryTypeIsCheckModelLayerNum` | 30003 | 检查目标层数是否满足（条件） |

### 用于特效分层的流程

```
10013 修改 layerNum → setLayerNum(val)
  → isSync=1 → syncModelShow(model)
    → addLogEffectStart(model)
      → 日志字段 +5 = layerNum
        → 客户端读取 layerNum 切换特效分层显示
```

---

## Q21: 随机点名+壁垒颜色判定+分支执行的type实现思路

### 需求描述

> 随机点1名玩家，@1秒之后进行判定（点名特效）
> - 如果被点名角色所在的板块是【红色壁垒】，则对板块上所有的目标造成 @2% 最大生命值 + @3% 攻击力的伤害
> - 如果被点名角色所在的板块是【绿色壁垒】，则镇龙柱进行一次充能，能量值为 @4

### 整体模型链设计

需要 5~6 个模型串联，核心流程：**点名 → 延迟 → 判定板块颜色 → 分支执行**

### 第1步：随机点名

使用 `entryTypeRandomSelectRoleNotInRecord` (1072)

- 从存活玩家中随机选1名，存入 `commonRole` map
- 同时给被点名角色挂一个点名特效 buff（通过 `buffModId` 参数触发）
- `dataArr` 配置：`[recordModId, buffType, 特效buffModId]`

### 第2步：延迟 @1 秒

用 `entryTypeTimeWait` (1)，等待 @1 秒后触发判定模型。

### 第3步：判定板块颜色（关键 — 条件分支）

这一步需要做：
1. 取被点名角色的 `areaIndex`（从 `role.ai.navAreaInfo.areaIndex` 获取）
2. 读壁垒记录模型的 `commonInt32[areaIndex]`，判断是否 >= 红色阈值
3. **红色** → 触发伤害模型链；**绿色** → 触发充能模型链

**推荐方案 — 新增一个 type**（最清晰）

新增类似 `entryTypeCheckBarrierColorBranch` 的类型：

```go
func actionCheckBarrierColorBranch(mod *BattleEntryModel) {
    dataArr := mod.getDataArr()
    recordModId := dataArr[0]        // 点名记录模型ID
    barrierRecordModId := dataArr[1] // 壁垒记录模型ID
    redThreshold := dataArr[2]       // 红色壁垒阈值
    redModelId := dataArr[3]         // 红色分支模型ID
    greenModelId := dataArr[4]       // 绿色分支模型ID
    roleKey := dataArr[5]            // commonRole中的key

    // 1. 从点名记录中取被点名角色
    recMod := mod.ent.getGlobalModelArrByModelId(recordModId)
    role := recMod.commonRole[roleKey]

    // 2. 取角色所在板块index
    areaIndex := role.ai.navAreaInfo.areaIndex

    // 3. 读壁垒值
    barrierMod := mod.ent.getGlobalModelArrByModelId(barrierRecordModId)
    barrierVal := barrierMod.commonInt32[areaIndex]

    // 4. 分支
    if barrierVal >= redThreshold {
        // 红色壁垒 → 伤害
        mod.ent.addModelIDArr(role, []uint32{redModelId}, mod, mod.getSkiData())
    } else {
        // 绿色壁垒 → 充能
        mod.ent.addModelIDArr(mod.getSource(), []uint32{greenModelId}, mod, mod.getSkiData())
    }
}
```

**备选方案 — 复用 `addChanceModWithElse` 模式**

如果壁垒颜色可以转化为"概率"判定（比如红色=100%触发，绿色=0%触发），可以在判定前先算出 TriggerChance，再用 else 分支。但比较绕，不如新增 type 直观。

### 第4步：红色分支 — 伤害

- **目标选择**：用板块类目标类型（如 `targetTypeKindSelfCircleArea_X` 系列，或 `targetSameRectangleAreaIDWithLastModelTarget`），获取被点名角色所在板块的所有单位
- **伤害类型**：`entryTypeDamageHit`
- **伤害公式**：@2% 最大生命值 + @3% 攻击力
  - 可能需要拆成两个伤害模型叠加，或者在伤害 action 里支持混合公式

### 第5步：绿色分支 — 镇龙柱充能

- 使用 `entryTypeChargeEnergyVal` (1060) 或 `entryTypeSetChargeEnergyVal` (1062)
- 充能值 = @4
- source 设为镇龙柱（boss自身）

### 配置数据结构总结

```
模型1: 点名    → type=1072, dataArr=[recordModId, buffType, 特效buffId]
模型2: 延迟    → type=1(TimeWait), 时间=@1秒, 触发模型3
模型3: 判定    → type=新增, dataArr=[recordModId, barrierRecordModId, 
                                     redThreshold, redModelId, greenModelId, roleKey]
模型4: 伤害    → type=DamageHit, target=同板块所有单位, 
                 公式=@2%maxHp+@3%atk
模型5: 充能    → type=1060/1062, val=@4
```

**核心难点在第3步的条件判定，建议新增一个 type 最干净。其他步骤都有现成的 type 可以复用。**

---

## Q22: 10045 actionRandomVal 能否根据指定记录模块的值进行分支

### 问题

`actionRandomVal` (10045) 能实现根据指定记录模块的某个值进行分支选项吗？

### 结论

**现有 kind 不能直接实现，但通过新增一个 kind（如 kind=15）可以最小改动完成。**

### 分析：case 14 最接近但不够

```go
case 14:
    _, selectVal := mod.checkParam(values[0])
    if selectVal > 0 {
        mod.addChanceModWithElse(mod, 100, []uint32{mod1}, nil)  // 分支A
    } else {
        mod.addChanceModWithElse(mod, 100, []uint32{mod2}, nil)  // 分支B
    }
```

case 14 能做**二选一分支**，但瓶颈在 `checkParam`：它只能解析预定义的 `modelSign` 常量（技能参数 -91~-97、吟唱时间 -90、层数 -303 等），**不能按 modelId 读取指定记录模块的 `val` 或 `commonInt32`**。

### 壁垒判定需求差在哪

需要的判定链：
1. 取被点名角色的 `areaIndex`（动态，取决于玩家站位）
2. 读壁垒记录模块的 `commonInt32[areaIndex]`（按区域索引查值）
3. 判断是否 >= 红色阈值 → 分支

这三步中，步骤1和2是 `checkParam` 和现有所有 kind 都做不到的 —— 涉及"从另一个记录模块按动态索引读值"。

### 推荐方案：在 10045 中新增 kind=15

在 `actionRandomVal` 的 switch 中新增一个 case，复用 10045 框架，不需要注册新的 type 常量：

```go
case 15: // 根据指定记录模块中 commonInt32[被点名角色areaIndex] 的值分支
    source := mod.source
    if source == nil { return }
    ent := source.mgr.ent
    
    // 取被点名角色
    rollCallRecordModId := uint32(dataArr[2])
    roleKey := uint32(dataArr[3])
    rollCallModArr := ent.getGlobalModelArrByModelId(rollCallRecordModId, modelKindAction, source)
    if len(rollCallModArr) == 0 { return }
    role := rollCallModArr[0].commonRole[roleKey]
    if role == nil { return }
    
    // 取角色所在板块
    areaIndex := ent.tar.getCircleAreaId(source, role, dataArr[4])
    
    // 从壁垒记录模块读该板块的壁垒值
    barrierRecordModId := uint32(dataArr[5])
    barrierModArr := ent.getGlobalModelArrByModelId(barrierRecordModId, modelKindAction, source)
    if len(barrierModArr) == 0 { return }
    barrierVal := barrierModArr[0].commonInt32[int32(areaIndex)]
    
    // 阈值判定 → 分支
    _, threshold := mod.checkParam(dataArr[6])
    mod1 := uint32(dataArr[7])  // 红色分支
    mod2 := uint32(dataArr[8])  // 绿色分支
    if float64(barrierVal) >= threshold {
        mod.addChanceModWithElse(mod, 100, []uint32{mod1}, nil)
    } else {
        mod.addChanceModWithElse(mod, 100, []uint32{mod2}, nil)
    }
```

### Data 配置

```
Data[0] = 15                    // kind
Data[1] = 0/1/2                 // isTrigger
Data[2] = 点名记录模型ID
Data[3] = commonRole的key
Data[4] = configIndex (getCircleAreaId的板块配置索引)
Data[5] = 壁垒记录模型ID
Data[6] = 红色阈值 (支持@参数)
Data[7] = 红色分支模型ID
Data[8] = 绿色分支模型ID
```

### 优势

复用 10045 框架，只新增一个 case，不需要注册新的 type 常量，改动最小。

---

## Q23: checkParam modelSign 负数常量完整枚举

### 概述

Battle Entry Data 的 Time 字段是 `[]int32` 数组，固定4个位置：

| 索引 | 常量 | 含义 | 单位 |
|------|------|------|------|
| `[0]` | `timeIndexDuration` | 持续时间 | ms（-1=无限） |
| `[1]` | `timeIndexNum` | 执行次数 | 次（-1=无限） |
| `[2]` | `timeIndexRate` | 执行间隔 | ms |
| `[3]` | `timeIndexDelay` | 延迟执行 | 标志位 |

每个位置都经过 `checkParam` 处理，正数当字面值使用（ms 单位自动 *0.001 转秒），负数走 modelSign 逻辑动态取值。Time 以外的字段（如 Data）同样可以通过 checkParam 使用这些 sign。

### checkParam 处理流程

```
checkParam(val)
  ├─ checkParamRaw(val)
  │   ├─ skiData == nil → 返回原始 val
  │   ├─ 匹配 BattleEntryModel 级 sign（-200~-303）→ 直接计算
  │   └─ default → skiData.checkParam(val)
  │       ├─ 匹配技能参数 sign（-51~-104）→ 从技能数据取值
  │       └─ default → 返回原始 val
  └─ ChangeModelParamValue(sign, val) → 检查参数修改模块叠加
```

### modelSign 完整枚举

#### BOSS/活动类

| 值 | 常量 | 含义 |
|----|------|------|
| `-51` | `modelSignBossMonsterElementVal` | 活动怪物伤害系数（BigrushElement） |
| `-52` | `modelSignBossMonsterElementValMusic` | 活动怪物伤害系数（MusicElement） |

#### 自动释放参数

| 值 | 常量 | 含义 |
|----|------|------|
| `-61` | `modelSignReleaseVal1` | 技能自动释放参数1 |

#### 来源技能参数（-71 ~ -77）

| 值 | 常量 | 含义 |
|----|------|------|
| `-71` | `modelSignSourceSkiDataVal1` | 来源技能（parentSkillData）参数1 |
| `-72` ~ `-77` | `modelSignSourceSkiDataVal2~7` | 同理，参数2~7 |

#### 延迟/弹道（-80, -81）

| 值 | 常量 | 含义 |
|----|------|------|
| `-80` | `modelSignDamageDelay` | 延迟表现时间（showConf.DamageDelayJob 按职业索引取值） |
| `-81` | `modelSignBallisticTime` | 弹道飞行时间（= 距离 / 弹道速度 / ballisticTime系数） |

#### 运算符号（-83 ~ -89）

| 值 | 常量 | 含义 |
|----|------|------|
| `-83` | `modelSignFinishFlag` | 结束标记 |
| `-84` | `modelSignLeftBracket` | 左括号 `(` |
| `-85` | `modelSignRightBracket` | 右括号 `)` |
| `-86` | `modelSignDiv` | 除法 `/` |
| `-87` | `modelSignSub` | 减法 `-` |
| `-88` | `modelSignAdd` | 加法 `+` |
| `-89` | `modelSignMul` | 乘法 `*` |

#### 吟唱/时间类（-90, -105, -213）

| 值 | 常量 | 含义 | 特殊逻辑 |
|----|------|------|----------|
| `-90` | `modelSignChaneTime` | 吟唱时间 | 多重施法时返回0（非怪物角色） |
| `-105` | `modelSignChaneTime2` | 吟唱时间（纯净版） | 不受多重施法影响 |
| `-213` | `modelSignChaneTime1` | 吟唱时间 + 延迟常量 | chantTime + constChantSkillEffectDelayTime |

#### 技能参数（-91 ~ -97）

| 值 | 常量 | 含义 |
|----|------|------|
| `-91` | `modelSignValue1` | 技能参数1（skiData.Value1） |
| `-92` ~ `-97` | `modelSignValue2~7` | 技能参数2~7（-97为固定伤害） |

#### 技能冷却（-98, -104）

| 值 | 常量 | 含义 |
|----|------|------|
| `-98` | `modelSignCool` | 技能冷却（配置值） |
| `-104` | `modelSignRealCool` | 技能实际冷却（经属性计算后） |

#### 运算函数（-100 ~ -103）

| 值 | 常量 | 含义 |
|----|------|------|
| `-100` | `modelSignClamp` | 限制取值范围 [min, max] |
| `-101` | `modelSignMax` | 取最大值 |
| `-102` | `modelSignMin` | 取最小值 |
| `-103` | `modelSignAbs` | 取绝对值 |

#### 引导/持续类（-200 ~ -212）

| 值 | 常量 | 含义 |
|----|------|------|
| `-200` | `modelSignKeepTime` | 引导时间 |
| `-201` | `modelSignKeepEffectTime` | 引导时间 + 特效延迟常量 |
| `-202` | `modelSignModTimeEventDataEnergy` | 根据技能消耗能量计算持续时间 |
| `-203` | `modelSignModTimeEventDataSkillKeepTime` | 根据技能引导时间计算持续时间 |
| `-204` | `modelSignModTimeByLayer` | 模块层数 + 1 |
| `-205` | `modelSignDamageRateDecrease` | 上一攻击模块 atkData.val |
| `-206` | `modelSignLastModLayer` | 上一模块层数 |
| `-207` | `modelSignSkillWarningTime` | 技能预警时间（怪物技能用） |
| `-208` | `modelSignLastModEventDataIsSkillDataChantTime` | 上一模块 eventData(SkillData) 的吟唱时间 |
| `-209` | `modelSignLastModEventDtatDurationTime` | 上一模块 eventData 模块的持续时间 |
| `-210` | `modelSignLastModEventDataInt32ArraySecIndexVal` | 上一模块 eventData([]int) 第2位 |
| `-211` | `modelSignKeepSubDelayDameageTime` | 引导时间 - 伤害延迟时间 |
| `-212` | `modelSignLastModLeftTime` | 上一模块剩余时间（duration - runTime） |

#### 音乐系统（-214, -215）

| 值 | 常量 | 含义 |
|----|------|------|
| `-214` | `modelSignMusicTranspositionTime` | 变调时间 |
| `-215` | `modelSignMusicModulationValTime` | 变奏时间 |

#### 特殊功能类（-300 ~ -303）

| 值 | 常量 | 含义 |
|----|------|------|
| `-300` | `modelSignCoordinationRate` | 协力百分比 |
| `-301` | `modelSignLastSummonModelLostIndex` | 上一个召唤模块的槽位索引（特效召唤物用） |
| `-302` | `modelSignCalcRecordedAreaID` | 从记录模块读 areaID + baseEffID（板块特效定位，Data[0]=baseEffID, Data[1]=recordModId） |
| `-303` | `modelSignTargetModelLayerNum` | 指定模块ID的层数（Data[0] 指定 modelId，通过 getGlobalModelArrByModelId 查找） |

#### 技能参数+偏移（-911 ~ -971）

| 值 | 常量 | 含义 |
|----|------|------|
| `-911` | `modelSignValue11` | 技能参数1 + modParameterOffset |
| `-921` ~ `-971` | `modelSignValue21~71` | 同理，各参数 + offset |

---

## Q24: Target 字段结构详解及 3,0,23,-93 配置解析

### Target 字段结构

Target 是 `[]int32` 数组，固定5个位置（后面可省略）：

| 索引 | 常量 | 含义 | 说明 |
|------|------|------|------|
| `[0]` | `targetIndexType` | 目标类型 | 决定从哪个阵营/范围获取候选目标 |
| `[1]` | `targetIndexData` | 类型附加数据 | 不同 type 有不同含义（半径、modId、怪物类型等） |
| `[2]` | `targetIndexOrder` | 排序方式 | 对候选目标排序（随机、最近、血量最低等） |
| `[3]` | `targetIndexNum` | 目标数量上限 | 支持 checkParam 负数动态取值 |
| `[4]` | `targetIndexFilter` | 目标过滤 | 额外过滤条件 |

### 执行流程

```
getTargetArr(source, modelID, lastMod, skiData)
  1. getTargetType(tarConf) → 根据 type 获取候选目标池
  2. getOrderTargetArr(targetArr, tarConf) → 根据 order 排序
  3. getTargetFilter(targetArr, tarConf) → 根据 filter 过滤
  4. getTargetNum(targetArr, tarConf) → 根据 num 截取目标数量
```

### 目标类型枚举（targetIndexType 常用值）

| 值 | 常量 | 含义 |
|----|------|------|
| 1 | `targetTypeKindSelf` | 自身 |
| 2 | `targetTypeKindFriend` | 己方队伍 |
| 3 | `targetTypeKindEnemy` | 敌方队伍 |
| 4 | `targetTypeKindNow` | 当前目标 |
| 5 | `targetTypeKindLast` | 上个模块的目标 |
| 6 | `targetTypeKindEventSource` | 事件来源 |
| 9 | `targetTypeKindEventTarget` | 事件目标 |
| 10 | `targetTypeKindSelfSectorEnemy` | 前方扇形180度敌军 |
| 12 | `targetTypeKindSelfRadiusEnemy` | 自身半径敌方 |
| 15 | `targetTypeKindFriendNotSelf` | 己方队伍（不含自身） |
| 20 | `targetTypeKindSelectTarget` | 选人 |
| 23 | `targetTypeKindSelfSummon` | 自己的召唤物 |
| 24 | `targetTypeKindSelfSectorEnemy_45` | 前方扇形45度敌军 |
| 25 | `targetTypeKindSelfSectorEnemy_60` | 前方扇形60度敌军 |
| 32 | `targetTypeKindSelfSectorEnemy_90` | 前方扇形90度敌军 |

### 排序方式枚举（targetIndexOrder 常用值）

| 值 | 常量 | 含义 |
|----|------|------|
| 0 | — | 不排序 |
| 2 | `targetOrderKindNear` | 最近优先 |
| 3 | `targetOrderKindFar` | 最远优先 |
| 5 | `targetOrderKindMelee` | 近战优先 |
| 6 | `targetOrderKindRemote` | 远程优先 |
| 7 | `targetOrderKindHpRateMin` | 血量百分比最低优先 |
| 8 | `targetOrderKindHpRateMax` | 血量百分比最高优先 |
| 14 | `targetOrderKindTargetNear` | 距离上一模块目标最近 |
| 15 | `targetOrderKindRandomArr` | 随机排序 |
| 23 | `targetOrderKindRandomArrRemoveTarget` | 随机排序（排除当前目标） |

### 目标数量（targetIndexNum）

- 正数：直接作为数量上限
- 0：不限制
- 负数：走 `checkParam` 动态取值（如 -93 = 技能参数3）

### 配置 `[3, 0, 23, -93]` 解析

| 索引 | 值 | 含义 |
|------|------|------|
| `[0]` type | `3` | 敌方队伍所有存活单位 |
| `[1]` data | `0` | 无附加数据 |
| `[2]` order | `23` | 随机排序，排除当前目标 |
| `[3]` num | `-93` | `modelSignValue3` = 技能参数3决定数量 |

**总结：从敌方队伍中，排除当前目标后随机选取，数量由技能参数3决定。**

---

## Q25: entryTypeTriggerMod (10051) 根据值触发模块完整介绍

### 概述

10051 是**条件分支触发器**：根据动态值（运行次数、板块ID、记录模块数据等）从 Data 数组中选取对应模块ID触发。所有 kind 最终都走 `mod.addChanceModWithElse(mod, 100, items, nil)`。

### Data 通用结构

```
Data[0] = kind（分支类型）
Data[1] = 取值参数（具体含义随 kind 变化）
Data[2+] = 待触发的模块ID列表
```

### kind 枚举

| kind | 含义                        | Data 结构                                                      | 分支依据                                           |
| ---- | ------------------------- | ------------------------------------------------------------ | ---------------------------------------------- |
| 1    | 按 checkParam 值截取前N个       | `[1, valParam, modIds...]`                                   | `checkParam(Data[1])` 得数量                      |
| 2    | 按上上个模块运行次数                | `[2, _, modIds...]`                                          | `lastMod.lastMod.getRunCount()`                |
| 3    | 按上个模块 val 值（支持向上查找指定type） | `[3, valParam, modIds...]`                                   | `lastMod.getVal()`，val>1000时向上找匹配type的模块       |
| 4    | 按敌方存活数量                   | `[4, _, modIds...]`                                          | `len(敌方存活)`                                    |
| 5    | 按目标所在板块ID                 | `[5, configIndex, modIds...]`                                | `getCircleAreaId(source, target, configIndex)` |
| 6    | 首次取目标板块，再次镜像翻转            | `[6, configIndex, modIds...]`                                | 首次=板块ID，再次=(ID+3)%6                            |
| 7    | 按指定板块人数                   | `[7, configIndex, areaId, modIds...]`                        | 板块上敌方人数                                        |
| 8    | 按行为模块参数                   | `[8, entryType, modIds...]`                                  | 每个行为模块的Data[0]作为index                          |
| 9    | 按指定技能模块层数                 | `[9, modId, modIds...]`                                      | `target身上modId模块的layerNum`                     |
| 10   | kind=6变体，记录模块ID由配置指定      | `[10, configIndex, targetModId, modIds...]`                  | 指定模块的val做镜像                                    |
| 11   | 按记录模块dataArr值（多个）         | `[11, recordModId, modIds...]`                               | 遍历dataArr每个值作index                             |
| 12   | 按记录模块dataArr切片            | `[12, recordModId, sliceIdx, idxType, modIds...]`            | idxType=1前N个/2后N个                              |
| 13   | 记录模块尾部弹出                  | `[13, selectNum, isRemove, recordModId, modIds...]`          | 尾部弹出值%3作index                                  |
| 14   | 按运行次数+记录模块                | `[14, recordModId, modIds...]`                               | `recordMod.dataArr[runCount]`                  |
| 15   | 按自身运行次数直接选取               | `[15, modIds...]`                                            | `mod.getRunCount()`                            |
| 20   | 按西游板块areaID               | `[20, _, modIds...]`                                         | `target.ai.areaID`                             |
| 21   | 按navAreaInfo板块索引          | `[21, _, modIds...]`                                         | `target.ai.navAreaInfo.areaIndex`              |
| 22   | val累计递增                   | `[22, _, modIds...]`                                         | `mod.getVal()`每次+1                             |
| 23   | 按记录模块dataArr[0]           | `[23, recordModId, modIds...]`                               | `recordMod.dataArr[0]`                         |
| 24   | 当前板块有无玩家                  | `[24, _, modId]`                                             | 有人触发/无人不触发                                     |
| 25   | 板块有人+commonInt32判定        | `[25, areaId, recordModId, modId]`                           | commonInt32[areaId]>0触发并清零                     |
| 26   | 按板块人数二选一                  | `[26, configIndex, areaId, noMod, hasMod]`                   | 无人/有人                                          |
| 27   | 按记录模块val偏移                | `[27, recordModId, baseModId]`                               | 触发baseModId+val                                |
| 28   | 按记录模块dataArr[0]+额外        | `[28, recordModId, extraMod, modIds...]`                     | dataArr[0]作index+额外模块                          |
| 29   | 按记录模块值+方向                 | `[29, recordModId, dirModIds...]`                            | `Data[2+dir]+dataArr[0]`                       |
| 30   | 按上N层模块Data值               | `[30, lastTime, valIndex, baseMod]`                          | 向上lastTime层的Data[valIndex]                     |
| 31   | kind=13扩展（可配maxArea）      | `[31, selectNum, isRemove, recordModId, maxArea, modIds...]` | index%maxArea                                  |
| 32   | 按checkParam值偏移            | `[32, valParam, baseModId]`                                  | 触发baseModId+val-1                              |
| 33   | 按目标板块+镜像                  | `[33, modId, mirrorIndex]`                                   | 触发两个：modId+idx和modId+(idx+mirror)%12           |
| 34   | 按记录模块dataArr带延迟           | `[34, recordModId, baseMod, delayTime]`                      | commonInt32[i]>=delayTime才触发                   |
| 35   | 按记录模块dataArr值分组           | `[35, recordModId, groupMods...]`                            | `Data[2+val-1]+i`                              |

---

## Q26: actionTriggerMod case 36 如何把 areaIndex 传给红色分支 modId

### 问题

在 `actionTriggerMod` 的 case 36 中，会通过 `getCircleAreaId` 计算出 `areaIndex`（目标所在板块），然后根据壁垒记录模块的 `commonInt32[areaIndex]` 与阈值比较来决定走绿色/红色分支。

需求：红色分支对应的模块要"对板块上所有目标造成 @2%最大生命值+@3%攻击力的伤害"，它需要知道当前的 `areaIndex` 来定位板块上的目标。如何把 case 36 计算的 `areaIndex` 传给红色分支的 modId？

### 答案

#### 核心机制：`mod.setVal()` + `lastMod.getVal()`

战斗系统已有成熟的 mod 链值传递机制：
- **当前 mod** 通过 `mod.setVal(value)` 保存一个 float64 值到 `mod.val`
- **子 mod**（通过 trigger/items 触发的下一层 mod）的 `lastMod` 指向当前 mod，通过 `mod.lastMod.getVal()` 即可读取

#### 代码修改

在 `battle_entry_action_10000.go` case 36 中，在分支判断之前加一行 `mod.setVal()`：

```go
case 36:
    source := mod.source
    target := mod.target
    if source == nil || target == nil {
        return
    }
    ent := source.mgr.ent
    configIndex := dataArr[1]
    areaIndex := int32(ent.tar.getCircleAreaId(source, target, configIndex))
    if areaIndex < 0 {
        return
    }
    barrierModId := uint32(dataArr[2])
    barrierModArr := ent.getGlobalModelArrByModelId(barrierModId, modelKindAction, source)
    if len(barrierModArr) == 0 {
        return
    }
    barrierVal := barrierModArr[0].commonInt32[areaIndex]
    _, threshold := mod.checkParam(dataArr[3])

    mod.setVal(float64(areaIndex))  // ← 关键：把 areaIndex 存到 mod.val

    if float64(barrierVal) >= threshold {
        items = append(items, uint32(dataArr[4]))  // 绿色分支
    } else {
        items = append(items, uint32(dataArr[5]))  // 红色分支
    }
```

#### 红色分支 mod 取值方式

红色分支 modId 对应的模块被触发后，`lastMod` 自动指向 case 36 所在的 mod，有两种取值路径：

**方式一：通过 10054 目标选择（推荐）**

红色分支 mod 配合 `actionSaveTargetArea`(10054) 使用，配置 `kind=2`：

```go
// 10054 actionSaveTargetArea 中 kind=2 的分支
case 2:
    area = mod.lastMod.getVal()  // 直接从上一层 mod 拿到 areaIndex
```

10054 拿到 area 后通过 `mod.setVal(area)` 继续向下传递，后续伤害模块即可基于板块索引筛选目标。

**方式二：在 action 中直接读取**

如果红色分支 mod 的 action 函数直接需要 `areaIndex`：

```go
area := mod.lastMod.getVal()  // 取到 case 36 存的 float64(areaIndex)
```

#### 传递链路图

```
actionTriggerMod (case 36)
  ├─ 计算 areaIndex = getCircleAreaId(source, target, configIndex)
  ├─ mod.setVal(float64(areaIndex))         ← 存值
  └─ items = [dataArr[5]]                   ← 红色分支 modId
       │
       ▼ addChanceModWithElse → addModelIDArr → startModel → newBattleEntryModel(lastMod=当前mod)
       │
  红色分支 mod (lastMod 指向 case36 的 mod)
       ├─ 目标选择 10054 kind=2 → area = mod.lastMod.getVal()   ← 取值
       └─ 或 action 中直接 mod.lastMod.getVal()                 ← 取值
```

#### 已有先例

- **case 6**（行1642）：`mod.setVal(curAreaId)` 保存板块ID，后续通过 `modArr[0].getVal()` 读取
- **10054 kind=2**（行542）：`area = mod.lastMod.getVal()` 从上层 mod 读取保存的板块值
- **case 7（10054值）**（行545-558）：沿 lastMod 链向上查找 `entryTypeSaveTargetArea` 类型的 mod 取值

| 步骤 | 位置 | 操作 |
|------|------|------|
| 1 | case 36 | `mod.setVal(float64(areaIndex))` |
| 2 | addChanceModWithElse | 把 items 中的 modId 加入执行队列，lastMod=当前mod |
| 3 | 红色分支mod | `mod.lastMod.getVal()` 取到 areaIndex |
| 4 | 10054 kind=2 | `area = mod.lastMod.getVal()` → `mod.setVal(area)` 继续传递 |

---

## Q27: entryTypeSaveTargetArea (10054) 详细介绍及如何对特定板块目标造成伤害

### 模块定位

10054 (`actionSaveTargetArea`) 本身**不造成伤害、不选目标**。它是一个**中间桥梁模块**，唯一职责是：计算板块索引 → `mod.setVal(area)` 存值，供后续模块通过 `lastMod.getVal()` 读取。

代码位置：`battle_entry_action_10000.go:509-567`

### Data 配置格式

```
Data: [kind, configIndex, isTrigger, extra]
```

| 位置 | 字段 | 说明 |
|------|------|------|
| Data[0] | `kind` | 板块计算方式（见下方枚举） |
| Data[1] | `configIndex` | CircleConfig 配置索引（几分区配置） |
| Data[2] | `isTrigger` | 可选，=1 首次执行存值后触发 Trigger；后续执行直接触发 Trigger |
| Data[3] | 额外参数 | kind=6 时为固定板块值，kind=7 时为偏移量 |

### kind 枚举详解

| kind | 含义 | 计算方式 |
|------|------|---------|
| 0 (default) | 当前目标所在板块 | `getCircleAreaId(source, target, configIndex)` |
| 1 | 技能锁定目标所在板块 | `getCircleAreaId(source, skillLockTarget, configIndex)` |
| **2** | **从上层模块取值** | `area = mod.lastMod.getVal()` |
| 3 | 锁定目标板块 -1 | `getCircleAreaId` 后 `getConfCircleAreaId(area, -1, configIndex)` |
| 4 | 锁定目标板块 +1 | `getCircleAreaId` 后 `getConfCircleAreaId(area, +1, configIndex)` |
| 5 | 锁定目标板块 +2 | `getCircleAreaId` 后 `getConfCircleAreaId(area, +2, configIndex)` |
| 6 | 直接使用配置值 | `area = Data[3]`（固定板块ID） |
| 7 | 沿 lastMod 链查找 10054 的值 + 偏移 | 向上遍历 lastMod 链找最近的 `entryTypeSaveTargetArea`，取其 val 后 `getConfCircleAreaId(area, Data[3], configIndex)` |

### isTrigger 机制

- `Data[2] = 1` 时：
  - 首次执行（`runCount=0`）：计算板块 → `setVal(area)` → 然后 `addTrigger()` 触发 Trigger
  - 后续执行（`runCount>=1`）：跳过计算，直接 `addTrigger()`
- `Data[2]` 不填或 `=0` 时：只计算存值，不触发 Trigger

### 如何对特定板块上的人造成伤害

10054 存好板块值后，关键在于**伤害模块的 Target 配置**来选人。有以下几种方案：

#### 方案一：CircleArea 目标选择（圆形分区选敌）

使用 `targetTypeKindSelfCircleArea_X` 系列目标类型（如 type=60/62/64/66 等），配合 Target 的 data 字段传特殊负值。

**Target data 字段特殊值含义（在 `getCircleAreaTargetArr` 中处理）：**

| data 值 | 含义 | 代码 |
|---------|------|------|
| `-7` | 直接取 `lastMod.getVal()` 作为板块索引 | `areaId = lastMod.getVal()` |
| `-2` | 沿 lastMod 链查找 10045 的 val | 向上找 `entryTypeRandomVal` |
| `-3` | 取 lastMod.target 所在板块 | `getCircleAreaId(source, lastModTarget, index)` |
| `-1` | 取 skillLockTarget 所在板块 | `getCircleAreaId(source, skillLockTarget, index)` |
| `>=1` | 固定板块（值 -1 后作为索引） | `areaId -= 1` |

**配置示例（4分区）：**

```
模块A: 10054 (保存板块)
  Data: [2, 4, 1]          // kind=2 从lastMod取值, configIndex=4, isTrigger=1
  Trigger: [模块B_ID]

模块B: 伤害模块
  Target: [66, -7, 0, 0]   // type=66(SelfCircleArea_4), data=-7(取lastMod.val)
  Data: [伤害公式]          // @2%最大生命值 + @3%攻击力
```

type=66 即 `targetTypeKindSelfCircleArea_4`，以 BOSS 为圆心、4分区配置选敌方单位。data=-7 让它从 `lastMod.getVal()` 获取板块索引。

#### 方案二：navAreaInfo 目标选择

如果板块是 navAreaInfo 类型（非圆形分区），使用 `targetTypeKindLastModValNavAreaInfoIndex`(166)：

```
模块A: 10054 (保存板块)
  Data: [2, 0, 1]
  Trigger: [模块B_ID]

模块B: 伤害模块
  Target: [166, 0, 0, 0]    // 166: 上个模块val所在板块的所有敌人
  Data: [伤害公式]
```

166 的实现(`battle_entry_target.go:673`)：
```go
areaID := int32(lastMod.getVal())
for _, role := range battleRoles {
    if role.ai.navAreaInfo.areaIndex == areaID {
        roleArr = append(roleArr, role)
    }
}
```

#### 方案三：跳过 10054，伤害模块直接读 lastMod.val

如果上层模块（如 case 36）已经 `setVal(areaIndex)`，且中间不需要额外逻辑，伤害模块可直接挂在上层模块的 Trigger 上：

```
case 36 → mod.setVal(areaIndex)
  └─ 红色分支: 伤害模块
       Target: [66, -7, 0, 0]    // 直接从 lastMod(case36) 读 val
```

### 结合 case 36 的完整配置示例

```
10051 case 36 (actionTriggerMod)
  ├─ 计算 areaIndex, mod.setVal(float64(areaIndex))
  ├─ 绿色分支 dataArr[4]: 壁垒值 >= 阈值，不触发伤害
  └─ 红色分支 dataArr[5]: → 10054 模块
       ├─ Data: [2, 4, 1]           // kind=2 从lastMod取, configIndex=4, isTrigger=1
       ├─ setVal(area)              // 继续传递板块索引
       └─ Trigger: [伤害模块]
            ├─ Target: [66, -7, 0, 0]  // 板块上所有敌人
            └─ Data: [@2%最大生命值 + @3%攻击力 伤害公式]
```

数据流：
```
case36: setVal(areaIndex=2) → 10054 kind=2: lastMod.getVal()=2 → setVal(2)
  → 伤害模块 Target data=-7: lastMod.getVal()=2 → 筛选板块2所有敌人 → 造成伤害
```

### 相关目标类型速查

| Target type 值 | 常量名 | 用途 |
|----------------|--------|------|
| 60 | `targetTypeKindSelfCircleArea_1` | 配置1分区选敌（含当前目标） |
| 66 | `targetTypeKindSelfCircleArea_4` | 配置4分区选敌（含当前目标） |
| 67 | `targetTypeKindSelfCircleArea_4_NotExistSelf` | 配置4分区选敌（屏蔽当前目标） |
| 123 | `targetTypeKindAreaInfoIndex` | navAreaInfo 指定板块选敌 |
| 165 | `targetTypeKindCurrentTargetNavAreaInfoIndex` | 当前目标所在 navAreaInfo 板块选敌 |
| **166** | `targetTypeKindLastModValNavAreaInfoIndex` | **lastMod.val 所在 navAreaInfo 板块选敌** |

## Q28: entryTypeDamageHit (100) 核心伤害模块详解

### 枚举定义

```go
entryTypeDamageHit BattleEntryType = 100 // 100受到伤害(元素~物理1魔法2~倍率)
```

这是战斗系统中**最核心的伤害模块**，负责计算并施加一次伤害。

代码位置：`battle_entry_action.go:2068`（入口）、`battle_entry_action.go:2138`（核心逻辑 `exeDamageHitWithTarget`）

### Data 参数结构

```
Data: [暴击, 元素类型, 固定伤害值, 倍率参数...]
```

| 索引 | 字段 | 说明 |
|------|------|------|
| `Data[0]` | **暴击 (crit)** | 通过 `checkParam` 解析，控制本次攻击的暴击率修正 |
| `Data[1]` | **元素类型 (element)** | `0`=默认(取角色自身攻击元素)，`1`=物理，`2`=魔法，其他=对应元素枚举 |
| `Data[2]` | **固定伤害值 (damageAbs)** | 通过 `checkParam` 解析，附加的固定伤害数值 |
| `Data[3:]` | **技能倍率 (power)** | 通过 `battleEntryData.getData()` 解析，支持复杂的 data 公式计算 |

### 执行流程

调用链：`actionDamageHit(mod)` → `exeDamageHit(mod, dataArr)` → `exeDamageHitWithTarget(mod, target, dataArr)`

#### 1. 确定攻击来源

```go
source := mod.getSource()
if source.isKindPet() {
    source = source.sourcePet      // 宠物 → 取主人
} else if source.isKindMount() {
    source = source.sourceMount    // 坐骑 → 取主人
} else if source.isKindCat() {
    source = source.sourceCat      // 猫 → 取主人
}
```

#### 2. 解析参数

```go
_, crit := mod.checkParam(dataArr[0])           // 暴击率
element := getElement(source, dataArr[1])        // 元素类型
_, damageAbs := mod.checkParam(dataArr[2])       // 固定伤害
power := battleEntryData.getData(mod, dataArr[3:]) // 技能倍率（复杂公式）
```

**元素解析规则 (`getElement`)**：
- 如果 element 值 = 0 (`damageElementDefault`)，取角色自身攻击元素 `source.getAttackElement()`
- 否则使用配置值直接转换为 `BattleDamageElement` 枚举

#### 3. 构建攻击数据

```go
atkData := newAtkDataWithDamage(source, target, mod, element)
```

创建 `BattleAtkData` 结构，包含来源、目标、模块引用、元素类型等基础信息。

#### 4. 补跳修正 (atkFixRate)

```go
atkData.atkFixRate = mod.atkFixRate
mod.atkFixRate = 0  // 用完立即清除，防止继承给下个模块
```

当模块最后一次运行时距离结束还有剩余时间，`CheckExtraRunMod` 会计算补跳系数，100 类型本身不触发额外运行，但会传递该系数。

#### 5. 记录攻击次数

```go
atkData.damNum = getDamageNum(mod, atkData)
```

`getDamageNum` 内部累加技能数据的伤害计数 (`skilData.addDamageCount(1)`)，返回当前是第几次攻击。

#### 6. 叠加技能附加值

```go
skiDta := mod.getSkiData()
if skiDta != nil {
    power += source.attr.getAttrValBySkill(skiDta, entryTypeAttrSelfDamageRate)   // 技能伤害倍率加成
    damageAbs += source.attr.getAttrValBySkill(skiDta, entryTypeAttrSelfDamageVal) // 技能固定伤害加成
}
```

#### 7. 减伤处理

```go
if atkData.isReduction {
    _, rate := mod.checkParam(atkData.reduction)
    power = rate * 0.01 * power
    damageAbs = rate * 0.01 * damageAbs
}
```

若存在减伤标记，将倍率和固定伤害同时乘以减伤系数。

#### 8. 填充并执行伤害

```go
atkData.critRate = int32(crit)
atkData.skillPow = power
atkData.damageAbs = damageAbs
atkData.damKind = mod.getConf().DamageType
target.dam.hitDamage(atkData)
```

最终调用 `hitDamage` 对目标造成伤害。

### 相关变体类型

| Type | 枚举名 | 与100的区别 |
|------|--------|-------------|
| **97** | `entryTypeDamageHitOther` | 直接使用 data 属性计算 val，不走标准伤害公式 |
| **99** | `entryTypeDamageHitOther1` | 类似97，但特殊行为减伤(305/306/851)生效 |
| **109** | `entryTypeDamageWithDivide` | 类似100，但根据指定模块数值拆分伤害 |
| **111** | `entryTypeDamageHitParseAbs` | 类似100，区别是 abs 解析方式不同，且伤害值会乘以模块层数 `mod.getLayerNum()` |
| **113** | `entryTypeDamageHitWithNormal` | 逻辑与100完全一样，但将 `isConsideredAsOtherDmgKind` 设为0，标记为普攻类型伤害 |

### 在其他模块中的角色

- **`getLastModAtkData`** (`battle_entry_data.go:819`)：向上追溯 lastMod 链时，遇到 type=100 会取出其 `atkData` 作为上一次攻击数据。若 kind 为 `modelKindDamageAfter` 则从 `eventData` 取，否则从 `mod.getAtkData()` 取
- **`CheckExtraRunMod`** (`battle_entry_model.go:934`)：type=100 在额外执行检查中为空 case，不执行额外动作
- **`getLastModKindTypeVal`** (`battle_entry_data.go:1535`)：弹射衰减计算时，以 type=100 的模块作为伤害基准值来源

### 配置示例

```json
{
  "Type": 100,
  "Data": [0, 0, 0, 1, 150],
  "Target": [3, 0, 0, 0]
}
```

含义：
- `Data[0]=0`：暴击率使用默认值
- `Data[1]=0`：元素类型取角色自身攻击元素
- `Data[2]=0`：无额外固定伤害
- `Data[3:]=1,150`：通过 `battleEntryData.getData` 公式计算倍率（具体含义取决于 data sign 解析规则）

## Q29: type=100 配置 @2%目标最大生命值+@3%自身攻击力 的 Data 写法

### 需求

造成伤害 = 目标最大生命值的2% + 自身攻击力的3%

### Data 倍率公式解析原理

type=100 的 `Data[3:]` 部分通过 `battleEntryData.getData()` → `calcData()` 解析，使用**逆波兰表达式（栈式计算）**。

数据被 `modelSignFinishFlag(-83)` 或运算符（`-88`加、`-87`减、`-89`乘、`-86`除）分割成多个段，每段的第一个值是 `dataFun` 函数编号，后续值是参数。

### 关键 dataFun 枚举

| dataFun 值 | 常量名 | 公式 | 取值角色 |
|------------|--------|------|----------|
| `212` | `dataFunMaxHp` | `target.maxHp * val * 0.01` | **目标** |
| `1212` | `dataFunMaxHpSource` | `source.maxHp * val * 0.01` | **自身** |
| `208` | `dataFunAtk` | `target.atk * val * 0.01` | **目标** |
| `1208` | `dataFunAtkSource` | `source.atk * val * 0.01` | **自身** |
| `4` | `dataFunRoleAtk` | `role.atk * val * 0.01`（1个参数取目标，2个参数取来源） | 看参数数量 |

### 运算符枚举

| 值 | 常量名 | 含义 |
|----|--------|------|
| `-88` | `modelSignAdd` | 加法 |
| `-87` | `modelSignSub` | 减法 |
| `-89` | `modelSignMul` | 乘法 |
| `-86` | `modelSignDiv` | 除法 |
| `-84` | `modelSignLeftBracket` | 左括号 |
| `-85` | `modelSignRightBracket` | 右括号 |

### 配置答案

```json
{
  "Type": 100,
  "Data": [0, 0, 0, 212, 2, -88, 1208, 3]
}
```

各字段含义：

| 位置 | 值 | 含义 |
|------|-----|------|
| `Data[0]` | `0` | 暴击率使用默认 |
| `Data[1]` | `0` | 元素取角色自身攻击元素 |
| `Data[2]` | `0` | 无额外固定伤害 |
| `Data[3:]` | `212, 2, -88, 1208, 3` | 倍率公式 |

### 倍率公式解析过程

```
输入: [212, 2, -88, 1208, 3]

第一段: [212, 2]
  → dataFun=212 (dataFunMaxHp), 参数=[2]
  → target.maxHp * 2 * 0.01
  → 结果: 目标最大生命值的2%

运算符: -88 (modelSignAdd, 加法)

第二段: [1208, 3]
  → dataFun=1208 (dataFunAtkSource), 参数=[3]
  → source.atk * 3 * 0.01
  → 结果: 自身攻击力的3%

最终: target.maxHp * 2% + source.atk * 3%
```

### 更多公式示例

```
// 目标最大生命值5%
Data[3:] = [212, 5]

// 自身攻击力150%
Data[3:] = [1208, 150]

// 目标最大生命值2% + 自身攻击力3%
Data[3:] = [212, 2, -88, 1208, 3]

// (目标最大生命值2% + 自身攻击力3%) * 固定值50%
Data[3:] = [-84, 212, 2, -88, 1208, 3, -85, -89, 2, 50]
// 括号内先算加法，再乘以 dataFunRate(val=50) → 50*0.01
```

---

## Q30: battle attr 哪里获取 player 的最大生命值属性

### 调用链路

```
getMaxHp(isBattle)                          // battle_role_attr.go:545  入口
  ├── getBaseMaxHp()                        // battle_role_attr.go:199  基础HP
  │     └── Player: conf.getLvHp() + conf.getJobBaseHp()
  │           ├── getLvHp()  → conf.lv.LvHP         // battle_role_conf.go:224  等级HP
  │           └── getJobBaseHp() → conf.base.JobBaseHP  // battle_role_conf.go:240  职业基础HP
  ├── getBaseVal(entryTypeMaxHp, entryTypeMaxHpRate, baseMaxHp, isBattle, nil)
  │     // battle_role_attr_fun.go:799  通用属性公式
  │     ├── 角色属性值 = 基础HP + getBuffVal(entryTypeMaxHp)
  │     ├── 角色百分比 = (100 + getBuffVal(entryTypeMaxHpRate)) * rateAttrDic[MaxHpRate] * 0.01
  │     ├── 额外百分比 = (100 + getExtraMaxHpRate()) * 0.01
  │     ├── 宠物HP   = Σ pet.attr.getMaxHp()
  │     ├── 神灵HP   = Σ god.attr.getMaxHp()
  │     └── 返回: (角色属性值 * 角色百分比 + 宠物HP) * 额外百分比
  ├── += getOtherRoleAtk100Hp()             // 其他角色攻击力转化HP
  └── *= conf.base.JobHP                    // 职业HP系数 (仅 isBattle=true)
```

### 关键文件

| 文件 | 行号 | 说明 |
|------|------|------|
| `battle_role_attr.go` | 545 | `getMaxHp()` 入口函数 |
| `battle_role_attr.go` | 199 | `getBaseMaxHp()` 各角色类型基础HP计算 |
| `battle_role_attr_fun.go` | 799 | `getBaseVal()` 通用属性公式 |
| `battle_role_attr_extra.go` | 14 | `getExtraMaxHpRate()` 额外生命值% |
| `battle_role_conf.go` | 224 | `getLvHp()` 等级HP配置 |
| `battle_role_conf.go` | 240 | `getJobBaseHp()` 职业基础HP配置 |

### Player 最大HP完整公式

```
MaxHp = ((基础HP + HP_buff) * HP百分比 + 宠物HP + 神灵HP) * 额外HP百分比 + atk100Hp) * 职业HP系数

其中:
  基础HP     = LvHP + JobBaseHP                          (等级HP + 职业基础HP)
  HP_buff    = getBuffVal(entryTypeMaxHp)                (buff固定值加成)
  HP百分比   = (100 + buff百分比) * rateAttrDic * 0.01     (百分比加成)
  额外HP百分比 = (100 + ExtraMaxHpRate_buff) * 0.01       (额外百分比加成)
  宠物HP     = 所有宠物 getMaxHp() 之和
  神灵HP     = 所有神灵 getMaxHp() 之和
  atk100Hp   = getOtherRoleAtk100Hp()                    (攻击力转化HP)
  职业HP系数  = conf.base.JobHP                           (仅战斗中生效)
```

### 不同角色类型的 getBaseMaxHp 差异

| 角色类型 | 基础HP来源 |
|----------|-----------|
| Player / SummonPlayer | `conf.getLvHp() + conf.getJobBaseHp()` |
| Monster / SummonMonster | `monsterLv表HP * mo.HP * hpRatio * raidRate * serverRate * sandRate`，HP表根据怪物类型(Normal/Elite/Boss)区分 |
| Pet | `basePet.HP + lvPet.HP + stagePet.HP` |
| God | `getGodBaseAttr(entryTypeMaxHp)` |

---

## Q31: entryTypeDamageHitOther (97) 值类型伤害模块详解

### 定义

```go
entryTypeDamageHitOther BattleEntryType = 97 // 与 100相似，直接使用data属性取到val
```

### 核心定位

Type 97 是一种 **值类型伤害 (Val Damage)**，与 type 100 (`entryTypeDamageHit`) 的最大区别在于：
- **Type 100**：基于攻击力倍率计算伤害（暴击率、元素、倍率 → 经过完整公式计算）
- **Type 97**：**直接通过 `data` 属性获取伤害数值**，不走攻击力倍率公式，伤害值由配置的 `Data` 字段通过 `battleEntryData.getData()` 计算得出

### 执行流程

调用入口 (`battle_entry_action.go:797`)：
```go
case entryTypeDamageHitOther:
    actionDamageHitOther(mod, damageKindlNormal)
```

传入 `damageKindlNormal`（正常的值类型伤害，吃一些增益）。

### `actionDamageHitOther` 函数逻辑 (`battle_entry_action.go:2232-2267`)

**1. 确定攻击来源 (source)**
- 如果来源是宠物/坐骑/猫，则回溯到其主人（sourcePet / sourceMount / sourceCat）

**2. 确定元素属性 (element)**
- `Data[0] == -1`：通过 `dataFunGetAtkEle` 动态获取攻击元素
- `Data[0] != -1`：调用 `getElement(source, Data[0])` 获取元素，若为 `damageElementDefault` 则取 source 的攻击元素

**3. 构建攻击数据 `BattleAtkData`**
- 调用 `newAtkDataWithDamage(source, target, mod, element)`
- 初始化：`skillPow=100`, `critRate=0`, `damNum=1`
- 检查是否弹道攻击（上个模块是 `entryTypeBallistic`）
- 调用 `checkDamageArr` 核对 atkData 属性
- 创建统计数据 `newBattleRoleDamageStatistics`
- 检查普攻闪避 `checkNormalAttackMiss`

**4. 计算伤害值 (val)**
```go
val := battleEntryData.getData(mod, dataArr[1:]) // 从 Data 第二项开始取值
atkData.setVal(val + atkData.collateralDamage)    // 加上附带伤害
```
- 关键：直接用 `getData` 从配置中解析出伤害数值，而非通过攻击力 × 倍率计算

**5. 获取攻击次数**
```go
atkData.damNum = getDamageNum(mod, atkData) // 累计技能伤害次数
```

**6. 根据 damageKind 分发处理**

| damageKind | 值 | 调用方法 | 说明 |
|---|---|---|---|
| `damageKindlNormal` | 1 | `hitDamageVal(atkData)` | 正常值伤害，吃一些增益 |
| `damageKindlActivity` | 2 | `hitDamageValSpecial(atkData)` | 特殊值伤害，先乘最终伤害系数 |
| `damageKindlSceneBuff` | 3 | `hitDamageValBySceneBuff(atkData)` | 只受场景buff影响 |
| `damageKindGod` | 4 | `hitDamageValByGod(atkData)` | 神明伤害 |

Type 97 走 `damageKindlNormal` → `hitDamageVal`。

### Data 配置格式

```
Data[0]   → 元素类型（-1 = 动态获取攻击元素, 0 = 默认/跟随source, 其他 = 指定元素）
Data[1:]  → 通过 battleEntryData.getData() 解析的伤害数值表达式
```

### `hitDamageVal` 处理链 (`battle_role_damage.go:988+`)

1. **前置检查**：目标死亡 / 无敌时间 → 跳过
2. **帧伤害次数限制**：`checkFrameDamageNumLimit`
3. **召唤怪血量限制**检查
4. **普攻闪避**处理（val 置 0，触发特殊事件）
5. **值伤分支**：
   - `isValDamageNoBuff = true`：纯值伤，直接用 val
   - 否则：
     - `computeHitValBuff`：受击 buff 加成
     - 伤害上限 `maxDamage` 检查
     - `computeSharingChainDamage`：分担链伤害
     - `computeShield`：护盾计算
     - `damageOrRecoverFloating`：浮动值
6. **外层减伤**（乘法叠加）：
   - 种族减伤 `raceHitRate`
   - 房间减伤 `roomHitRate`
   - 区域减伤 `regionHitRate`
   - 秘宝减伤 `treasureHitRate` + 固定值 `treasureHitAbs`
   - 坐骑减伤 `mountHitRate` + 固定值 `mountHitAbs`
   - 公式：`damageVal = val × ((100 - 各项减伤率之和) × 0.01) - 固定减伤值`
7. **统计填充**：`fillRateValueBuff`, `fillDamageValAndEle`
8. **防御计算**：`computeDefensive`
9. **战报日志**：`addLogValue`
10. **神明 DPS 统计**
11. **炸弹检测**：`computeBomb`（炸弹则伤害归零）
12. **真无敌检测**：玩家/非玩家分别检查 `entryTypeRealInvincible` / `entryTypeTargetRealInvincible`
13. **伤害收集**：`checkCollectDamage` 系列（BD7 tag 检查后）

### 相关类型家族

| 类型 | 值 | 说明 | damageKind |
|------|-----|------|------------|
| **97** | `entryTypeDamageHitOther` | 值类型伤害，从 data 取 val | `damageKindlNormal` |
| **99** | `entryTypeDamageHitOther1` | 同 97 但只受特定行为减伤(305/306/851) | `damageKindlActivity` |
| **100** | `entryTypeDamageHit` | 标准攻击，基于倍率公式 | 完整攻击流程 |
| **102** | `entryTypeDamageGodHit` | 神明伤害，与 97 类似 | `damageKindGod` |
| **112** | `entryTypeFixedDamage` | 固定值伤害，只受场景 buff | `damageKindlSceneBuff` |

### 关联的增减伤模块

- **307** (`entryTypeFinDamageValMake`)：造成值97伤害的最终增伤
- **308** (`entryTypeFinDamageValHit`)：受到值97伤害的最终减伤

### 与 Type 100 的关键差异总结

| 维度 | Type 97 | Type 100 |
|------|---------|----------|
| 伤害来源 | Data 字段直接取值 | 攻击力 × 倍率公式 |
| 暴击率 | 默认 `critRate=0` | 配置中指定暴击率 |
| 技能倍率 | 不使用 | `power` 参数控制 |
| 走的伤害函数 | `hitDamageVal` | `calcHitDamage` → `hitDamage` |
| 增减伤模块 | 307/308 | 305/306 等 |
| 适用场景 | 固定/计算值伤害 | 基于角色属性的常规攻击 |

---

## Q32: type 97 配置 dataCondition 9,212,-92 和 data 0,4,-91,1 的含义解析

### dataCondition: `[9, 212, -92]`

模块的**触发条件**，在模块执行前检查是否满足。

| 位置 | 值 | 含义 |
|------|-----|------|
| `[0]` = 9 | `conditionUseSkillType` | 条件类型：**当前正在使用的技能类型** |
| `[1]` = 212 | skillType 参数 | 技能类型值 212 |
| `[2]` = -92 | `modelSignValue2` | **技能参数2** (`skiData.getSkillValue2()`)，作为概率值 `prdValue` |

逻辑 (`battle_entry_condition.go:636-651`)：
1. 获取目标当前使用的技能 `skiData`
2. `prdValue` = 技能参数2 的值（通过 `checkParam(-92)` 解析）
3. 如果 `prdValue < 100`，则有 `prdValue%` 概率返回 false
4. 最终判断 `skillType == 1 && skiData.isKeepSkill()`

### data: `[0, 4, -91, 1]`

type 97 的**伤害数据**配置。

根据 `actionDamageHitOther` 逻辑：
- `Data[0]` = 元素类型
- `Data[1:]` = 通过 `battleEntryData.getData()` 解析的伤害值表达式

| 位置 | 值 | 含义 |
|------|-----|------|
| `[0]` = 0 | 元素类型 | `damageElementDefault` = **默认元素**，取 source 角色的攻击元素 |
| `[1]` = 4 | `dataFunRoleAtk` | 数据函数类型：**攻击力 × x × 0.01** |
| `[2]` = -91 | `modelSignValue1` | **技能参数1** (`skiData.getSkillValue1()`) 作为倍率 x |
| `[3]` = 1 | 额外参数 | `len(dataArr) > 1` 为 true，取 **source（来源）** 的攻击力 |

### 伤害计算公式

```
val = source.攻击力 × 技能参数1 × 0.01
```

对应 `getDataAtkRate` (`battle_entry_data.go:428-451`)：
```go
atk := role.attr.getAtk(true, mod.atkData)  // source 的攻击力
_, val := mod.checkParam(constVal)            // constVal = -91 → 技能参数1
return atk * val * 0.01                       // 攻击力 × 技能参数1 × 0.01
```

### 整体含义

- **条件**：目标正在使用技能类型 212，且有技能参数2的概率触发
- **效果**：造成 **来源攻击力 × 技能参数1%** 的值类型伤害，元素跟随来源攻击元素
- 伤害走 `hitDamageVal` 正常值伤害流程（buff 加成、护盾、减伤等）

---

## Q33: type 97 (entryTypeDamageHitOther) 打印日志流程及字段详解

### 日志调用链

```
actionDamageHitOther(mod, damageKindlNormal)
  → newAtkDataWithDamage(source, target, mod, element)   // 创建攻击数据
  → battleEntryData.getData(mod, dataArr[1:])             // 计算攻击值
  → target.dam.hitDamageVal(atkData)                      // 执行伤害
      → mgr.log.addLogValue(mgr.statistic, atkData)      // 打印日志
```

### addLogValue 日志内容（battle_mgr_log.go:527）

日志类型为 `logActionValue`，固定 11 个字段：

| 字段索引 | 内容 | 说明 |
|---------|------|------|
| `[0]` | `sourceKey` | 来源角色 key（神明则取 owner 的 key） |
| `[1]` | `skillID` | 技能ID（突破技取父技能ID） |
| `[2]` | `skillLevel` | 技能等级 |
| `[3]` | `target.key` | 目标角色 key |
| `[4]` | `val & 0x3FFFFFFF` | 伤害值低30位 |
| `[7]` | `val >> 30` | 伤害值高位 |
| `[8]` | `lenNum` | 伤害数值长度标记 |
| `[9]` | `effectKind` | 攻击命中特效类型 |
| `[5]` | `valType` | 伤害/回复类型（见下方位标记） |
| `[6]` | `skillBelong` | 技能归属 |
| `[10]` | `skillBelong` | 技能归属（重复写入） |

### valType 位标记

伤害时基础值为 `logValueTypeDamage`，可叠加以下标记：

| 标记 | 含义 |
|------|------|
| `logValueTypeMiss` | 未命中 |
| `logValueTypeCrit` | 暴击 |
| `logValueTypeBuff` | 有 buff 加成 |
| `logValueTypeBlock` | 格挡 |
| `logValueTypeShield` | 护盾吸收 |
| `logValueTypeImmune` | 免疫 |
| `logValueTypeThorns` | 反伤 |
| `logValueTypeElf` | 麦粉/坐骑伤害 |
| `logValueTypeCraft` | 锻造伤害 |
| `logValueTypeDeity` | 神力伤害 |
| `logValueTypePerfectBlock` | 完美格挡 |

回复时基础值为 `logValueTypeRecover`，暴击时叠加 `logValueTypeCrit`。

### 统计埋点

在 `addLogValue` 内同时更新统计数据：

- **玩家造成伤害**：`stat.addHurt(sourceId, skillID, skillLevel, val, ...)`
- **神明/战宠造成伤害**：`stat.addHurt(ownerId, skillID, skillLevel, val, ...)`
- **玩家受到伤害**：`stat.addDef(targetId, val)`
- **玩家回复**：`stat.addHeal(sourceId, skillID, skillLevel, val, ...)`

### 伤害值拆分方式

伤害值 val 被拆成高低位存储：
- 低位：`int32(uint64(val) & 0x3FFFFFFF)` — 取低 30 位
- 高位：`int32(uint64(val) >> 30)` — 取高位
- 客户端需将高低位拼合还原完整伤害值

---

## Q34: type 97 最终伤害信息打印及 hitDamageVal 完整扣血流程

### hitDamageVal 中的两处日志

| 时间点 | 日志函数 | 位置 | 包含信息 |
|--------|---------|------|----------|
| buff/护盾计算后 | `addLogValue` | battle_role_damage.go:1046 | source、target、skillID、伤害值val（含暴击/格挡等标记） |
| 实际扣血后 | `addLogHp` | battle_role_damage.go:1174 | target 当前 HP、最大 HP |
| 扣血后 | `damageStatistics.addStatistic` | battle_role_damage.go:1186 | 来源ID、伤害统计数据 |

### addLogValue 与最终扣血 damageVal 的区别

`addLogValue` 在第 1046 行调用，此时 `atkData.val` 是经过 buff 减伤、护盾、秘宝减伤等计算后的值。但在此之后还有多轮处理可能修改 damageVal：

1. **炸弹（bomb）**：`computeBomb` 可能将 damageVal 设为 0
2. **真无敌**：`entryTypeRealInvincible` / `entryTypeTargetRealInvincible` 可能将 damageVal 设为 0
3. **房间类型特殊处理**：大暴走、桎梏、音乐、年等模式可能将 damageVal 设为 0
4. **限血逻辑**：`computeLimitHp` / `computeLimitHp2` 可能限制最终扣血量
5. **GM无敌**：`GMInvincibleTag` 会把扣掉的血加回来

### 如何打印最终伤害

在 `battle_role_damage.go` 约第 1136 行位置加入：

```go
print(testLogStr, "hitDamageVal final damageVal:", damageVal,
    " source:", atkData.source.key,
    " target:", role.key,
    " hp:", role.attr.hp,
    " hpAfter:", role.attr.hp - damageVal)
```

其中 `testLogStr = "@-------------------------------@\n"` 定义在 `battle_config.go:113`，是异常专用日志标识。

### 注意事项

- 第 1136 行当前存在调试代码 `damageVal = 10`，会把所有最终伤害强制设为 10，需要删除才能看到真实伤害
- `addLogValue` 记录的是 buff 计算后的伤害值，不等于最终扣血值
- `addLogHp` 记录的是扣血后的 HP 状态，间接反映最终伤害

---

## Q35: Target 3,0,15,1 选人流程解析

### Target 数组格式

`Target = [Type, Data, Order, Num, Filter...]`

对应 `3,0,15,1`：

| 索引 | 值 | 字段 | 含义 |
|------|-----|------|------|
| `[0]` Type | **3** | `targetTypeKindEnemy` | 敌方队伍 |
| `[1]` Data | **0** | — | 无额外参数 |
| `[2]` Order | **15** | `targetOrderKindRandomArr` | 随机排序 |
| `[3]` Num | **1** | 数量限制 | 取 1 个 |

### 执行步骤

1. **选敌方全体**（Type=3）：调用 `source.getRoleGroupArr(true, false)` 获取所有敌方角色
2. **随机排序**（Order=15）：调用 `getOrderTargetByRandom(targetArr, true, source)` 将目标数组随机打乱，**当前锁定目标会被移到最后一位**（优先选其他人）
3. **截取1个**（Num=1）：`targetArr[:1]` 取排序后的第一个，范围设为 `entryRangeSingle`

### 最终效果

从敌方队伍中随机选 1 个目标，优先选非当前锁定目标的敌人。如果敌方只有 1 个角色则直接选那个人；如果有多个，当前 AI 锁定目标被排到最后，大概率不会选到它。

---

## Q36: WorldBoss 本 Boss 当前目标是谁及如何打印

### WorldBoss 目标选择逻辑

WorldBoss（`RoomKindMonsterPowerWorldBoss = 17`）属于 `isRoomKindMonsterPower()` 类型，Boss 选目标走 `refreshMonsterTargetRaid`（`battle_role_ai_fun.go:193`）中的特殊分支：

```go
if mgr.br.isRoomKindMonsterArena() || mgr.br.isRoomKindMonsterPower() {
    allEnemy := mgr.raid.getFactionBattleRoleArr(role.group, true, true)
    if len(allEnemy) > 0 {
        return allEnemy[0]
    }
    return nil
}
```

即获取敌对阵营所有角色（Boss 排第一位），取第一个作为目标。不走普通副本的"顶怪数分配"逻辑。

### 获取 Boss 当前目标

通过 `role.ai.getTarget()` 获取（`battle_role_ai.go:547`），返回 `targetDataArr` 最后一个元素的 `target` 字段。

### 打印方式

```go
// 在任意位置打印 Boss 当前目标
target := boss.ai.getTarget()
if target != nil {
    print(testLogStr, "Boss currentTarget: targetId:", target.id,
        " targetKey:", target.key, " targetJobKind:", target.attr.jobKind)
} else {
    print(testLogStr, "Boss currentTarget: nil")
}
```

### 关键字段

| 字段 | 说明 |
|------|------|
| `target.id` | 玩家/角色 ID |
| `target.key` | 战斗内唯一 key |
| `target.attr.jobKind` | 职业类型 |
| `target.attr.jobType` | 职能（Tank/DPS等） |

### 已有的目标切换日志

`setTarget`（`battle_role_ai.go:497`）中，每次目标切换时会调用 `mgr.log.addLogLock(role, target, 0, 0)` 同步战报给客户端，也可在此处加 print 观察 Boss 的目标切换。

---

## Q37: WorldBoss 打印目标未生效原因及正确打印位置

### 未打印的原因

在 `refreshMonsterTargetRaid` 中加的 print 没触发，是因为 `refreshBattleTarget`（`battle_role_ai_fun.go:59`）有**多层提前返回**：

1. **第 61~63 行**：目标被锁定（`isLock`）且存活 → 直接返回
2. **第 72~85 行**：目标存活且满足攻击范围/猎人标记/强制锁定 → 直接返回
3. **第 92~95 行**：怪物组且目标存活、非西游模式 → `return target`

Boss 一旦有了存活目标，就在这些提前返回处就返回了，根本不会进入 `refreshMonsterTargetRaid`，所以里面的 print 不会执行。

### 正确的打印位置

**方式1**：在 `refreshBattleTarget` 最终返回处（第 107 行之后）：

```go
ai.setAutoTarget(target)
if role.isKindMonsterBoss() {
    if target != nil {
        print(testLogStr, "WorldBoss refreshTarget: bossId:", role.id,
            " targetId:", target.id, " targetKey:", target.key)
    } else {
        print(testLogStr, "WorldBoss refreshTarget: bossId:", role.id, " target: nil")
    }
}
return target
```

**方式2**：在 `setTarget`（`battle_role_ai.go:523`）`addLogLock` 之后，捕获所有目标切换：

```go
if ai.role.isKindMonsterBoss() {
    print(testLogStr, "Boss setTarget: bossId:", ai.role.id,
        " targetId:", target.id, " targetKey:", target.key)
}
```

注意：方式1只在首次选目标或目标死亡时触发；方式2在任何目标切换时触发（但相同目标不会重复设置，`setTarget` 第 498 行有去重判断）。

---

## Q38: WorldBoss 旋转跟随当前目标的时机及相关 Type

### 核心结论

Boss 的旋转**不是自动跟随**的，而是由技能配置中的特定 type 模块驱动。服务端通过 `addLogRotate` / `addLogSpecialRotate` 发送旋转战报给客户端执行。

### 旋转相关 Type

| Type | 名称 | 说明 |
|------|------|------|
| **891** | `entryTypeRotate` | Boss 自己旋转。首次根据 Data[0] 确定方向（1=当前AI目标位置，2=技能位置，3=世界坐标，4=模块目标位置），后续帧按角度累加 |
| **953** | `entryTypeSmoothLookAt` | 平滑跟随，每帧按速度插值转向 commonRole[0] 的位置 |
| **954** | `entryTypeLookAtImmediate` | 立即朝向模块目标，无过渡 |
| **885** | `entryTypeSetTargetLookAt` | 设置目标朝向指定世界坐标 |
| **10103** | `entryTypeSpecialSmoothRotate` | Boss 根据当前 AI 目标所在板块旋转到指定方向，同时设置 targetAreaId |

### 触发旋转的具体时机

1. **技能释放前/中**（891/954）：技能模块中挂载旋转 type，Boss 释放技能时面向目标
2. **持续追踪**（953）：作为持续运行模块，每帧按速度平滑转向目标
3. **板块旋转**（10103）：根据 `source.ai.getTarget()` 所在板块旋转到对应方向
4. **目标切换**：`setTarget` 发送 `addLogLock` 战报，客户端可能调整朝向
5. **移动时**：服务端只发位置日志 `addLogPos`，不发旋转日志，客户端根据位移方向自行处理朝向

---

## Q39: addLogPos 移动日志发送时机及触发场景

### 发送链路

`setPos` → `refreshLogPos` → `addLogPos`

### 距离阈值过滤（refreshLogPos, battle_role_ai.go:1181）

| 状态 | 阈值 | 常量 |
|------|------|------|
| 玩家组 或 战斗状态 | 0.2 米 | `logPosBattleDis` |
| 索敌/非战斗状态 | 0.8 米 | `logPosSeekDis` |
| `logPosKindNil`(99) | — | 不发送，直接跳过 |

只有新位置与上次日志位置距离 ≥ 阈值时才发送。

### logPosKind 类型

| 值 | 名称 | 说明 |
|----|------|------|
| 0 | Normal | 正常移动 |
| 1 | Sprint | 冲刺移动 |
| 2 | Skill | 技能移动 |
| 3 | Teleport | 瞬间移动 |
| 4 | Area | 区块特殊位移 |
| 5 | Lock | 路线特殊位移 |
| 99 | Nil | 不发位置信息 |

### 所有触发 setPos 的场景

| 场景 | logPosKind | 是否发日志 | 调用位置 |
|------|-----------|-----------|---------|
| 正常寻路移动 `movePos` | Normal(0) | 是 | battle_role_ai.go:1124 |
| 区域位移 `pathMoveArea` | Area(4) | 是 | battle_role_ai_nav.go:223 |
| 锁定路线位移 `pathMoveLock` | Lock(5) | 是 | battle_role_ai_nav.go:192 |
| 技能中移动 | Skill(2) | 是 | refreshLogPos 前覆盖 |
| 冲刺移动（玩家） | Sprint(1) | 是 | movePos 中判断 speedType |
| 瞬移（召回/重生/沙漠） | Teleport(3) | 是 | 多处 |
| 内部位置修正 actionRefreshTargetPos | Nil(99) | 不发 | 多处 |
| buff 移动 setActionMovePos | Nil(99) | 不发 | battle_entry_action_buff.go |

### 对 WorldBoss 的影响

Boss 战斗状态下阈值为 0.2 米。站桩 Boss 不移动则无移动日志，朝向完全依赖旋转 type（891/953/954/10103）。

## Q40: 打印模块 96160451 最终旋转的 target

### 模块 96160451 配置

| 字段 | 值 | 含义 |
|------|------|------|
| id | 96160451 | 模块 ID |
| time | [0, 1, 0, 0] | 延迟=0, 执行1次, 间隔=0, 持续=0 |
| target | [3, 0, 5, 1] | type=3(敌方), data=0, order=5(近战优先), num=1 |
| kind | 1 | action 模块 |
| typ | 1 | entryTypeTimeWait 时间等待触发 |
| next | [96160452] | 下一个模块 |

### target 配置解析 `[3, 0, 5, 1]`

| 索引 | 字段 | 值 | 含义 |
|------|------|------|------|
| 0 | targetIndexType | 3 | targetTypeKindEnemy — 敌方队伍全体 |
| 1 | targetIndexData | 0 | 无额外数据 |
| 2 | targetIndexOrder | 5 | targetOrderKindMelee — 近战单位优先排序 |
| 3 | targetIndexNum | 1 | 取 1 个目标 |

### 目标解析流程

1. `getTargetArr` (`battle_entry_target.go:367`) 入口
2. `getTargetType` — type=3 取敌方全体 (`source.getRoleGroupArr(true, false)`)
3. `checkRoleNil` — 移除 nil 和不可选中目标
4. `getOrderTargetArr` — order=5 近战优先排序
5. `getTargetFilter` — 过滤（无额外 filter）
6. `getTargetNum` — num=1 取第 1 个
7. `TargetMatchingFilteringAction` — 最终过滤

### 添加的调试打印

**文件**: `internal/battle/battle_entry.go`
**函数**: `getModelArrWithModelID`
**位置**: `getTargetArr` + `TargetMatchingFilteringAction` 之后，模块创建之前

```go
if modelID == 96160451 && len(targetArr) > 0 {
    for _, v := range targetArr {
        print("模块96160451最终旋转target: key=", v.key, " pos=", v.ai.getPos().X, ",", v.ai.getPos().Z, "\n")
    }
}
```

打印输出包含目标的 `key`（角色唯一标识）和位置坐标 `(X, Z)`。

---

## Q40: 如何让 Effect 跟随 Target 移动并显示在头顶

### 关键配置字段

**1. 模块的 `Buff` 字段**

| Buff 值 | 名称 | 效果 |
|---------|------|------|
| 0 | `modelBuffNil` | 不显示 buff 特效 |
| 1 | `modelBuffBuff` | 跟随目标的增益特效 |
| 2 | `modelBuffDebuff` | 跟随目标的减益特效 |
| 3 | `modelShowBuffEffect` | 显示 buff 特效 |
| 4 | `modelMustShowBuffEffect` | 强制显示 buff 特效 |

**2. `addLogEffectStart` 发送给客户端的关键字段**

| 索引 | 字段 | 说明 |
|------|------|------|
| [3] | targetKey | 客户端根据此 key 找到目标角色，挂载特效跟随 |
| [7] | sourceKey | 来源角色 |
| [6] | type | 模块 type，客户端据此判断特效挂载方式 |
| [8-10] | targetPos | 特效初始位置 X,Y,Z |
| [4] | buffIcon | buff 图标 ID |

**3. `Effect` 字段**

特效资源 ID，挂载点（头顶/脚底/身体中心）由客户端特效预制体的挂载点节点决定。

### 配置方式

- **跟随移动**：`Buff = 1 或 2`，客户端将特效绑定到 `targetKey` 对应角色上自动跟随
- **显示在头顶**：客户端特效预制体挂载点配置为角色的 `headTop` 骨骼节点
- **固定位置不跟随**：`Buff = 0`，特效放在 `targetPos` 位置

---

## Q41: Target 为 169,0 时选择目标的完整流程

### 配置含义

- `Target[0] = 169` → `targetTypeKindSelfCircleArea_41`：以自身为圆心画圆，取配置41号区块选敌方单位，计算当前目标
- `Target[1] = 0` → data 字段，表示区块索引（1-based）

### 完整流程

#### 1. `getTargetArr` 入口 (`battle_entry_target.go:367`)

```
tarConf = [169, 0, ...]
targetType = 169 = targetTypeKindSelfCircleArea_41
```

#### 2. `getTargetType` (`battle_entry_target.go:923`)

命中 `targetTypeKindSelfCircleAreas` map（line 931）：

```go
confData = {areaConfId: 41, invert: false, notTarget: false}
entryRange = entryRangeMany

roleArr = source.getRoleGroupArr(true, false)  // 获取敌方单位列表

targetArr = getCircleAreaTargetArr(
    index=41,        // 圆形配置ID
    skiData,
    source,
    roleArr,         // 敌方全部单位
    tarConf[1]=0,    // data字段（区块索引，1-based）
    notTarget=false, // 不屏蔽当前目标
    lastMod,
    invert=false,    // 不取反
)
```

#### 3. `getCircleAreaTargetArr` (`battle_entry_target.go:3558`)

```go
dataCofig = data.GetCircleConfig(41)   // 获取第41号圆形区域配置
cPos = source.ai.getPos()              // 自身位置
circlePos = Vector3(cPos.X, 0, cPos.Z) // 以自身为圆心

// tarConf(参数) = 0
_, areaId = skiData.checkParam(0)      // 对0无特殊处理，返回 (false, 0.0)
// areaId = 0.0

// 进入 switch default 分支:
areaId -= 1  → areaId = -1.0

// 关键判断:
if areaId < 0 {
    return targetArr  // 返回空数组！
}
```

**结论：data=0 时，areaId 经过 `default: areaId -= 1` 变为 -1，直接返回空目标数组。**

#### 4. 回到 `getTargetArr` (line 408-417)

`targetArr` 为空，后续步骤（checkRoleNil、checkCurrentMonsterGroup、getOrderTargetArr、getTargetFilter、getTargetNum）全部跳过，最终返回 `(空数组, entryRangeMany, nil, false)`。

### data 字段说明

| data 值 | 含义 |
|---------|------|
| `0` | 无区块，经 `-1` 变为 -1，返回空目标 |
| `1` | 第0区块（0-based index=0） |
| `2` | 第1区块（0-based index=1） |
| `-1` | 取锁定目标所在区域 |
| `-2` | 上个模块的 val 值 |
| `-3` | 上个模块目标所在板块 |
| `-4/-5/-6` | 取锁定目标区域 ±1/±2（仅配置4生效） |
| `-7` | 上个模块的 val 值（另一种方式） |
| `-8/-9/-10` | 当前目标板块 ±1（仅配置13生效） |
| `-15` | 埃及尔小怪使用，以召唤源为圆心 |
| `-16` | 伐木机，取上个模块 Data[0] |

### 正常选人流程（data > 0 时）

当 `areaId >= 0` 时，`getCircleAreaTargetArr` 会：
1. 从配置41获取圆形区域参数：起始点坐标 `(dataCofig[0], dataCofig[1])` 和各角度区间 `dataCofig[2:]`
2. 计算自身到起始点的距离作为半径 `radiusVal`
3. 遍历 `roleArr` 中每个敌方单位：
   - 判断该单位是否在圆内（距离 ≤ radiusVal）
   - 计算该单位相对圆心的角度
   - 判断角度是否在指定区块的角度区间 `(startAngleLimit, endAngleLimit]` 内
4. 符合条件的单位加入 `targetArr` 返回

---

## Q42: entryTypeFinDamageHitExtra (306) 受到最终伤害额外详解

### 定义

`battle_entry_type.go:251`

```go
entryTypeFinDamageHitExtra BattleEntryType = 306 // 受到最终伤害额外 --最终*301行为 乘算
```

### 性质

- **被动 Action**（passiveActionDic），不会主动执行，而是在伤害计算流程中被查询
- 挂在**受击方**（target）身上，用于修改受到的最终伤害
- 与 305（造成最终伤害额外）对称：305 挂在攻击方，306 挂在受击方

### 数据配置

| 字段 | 含义 |
|------|------|
| `Data` | 通过 `battleEntryData.getData()` 计算出增减百分比值 |
| `ConditionData` | 条件判断，满足才生效 |
| `TeamPass` | 0=仅自身来源的伤害生效，非0=全队共享 |
| `layerNum` | 层数，效果值 = data × layerNum |

### 伤害公式中的位置

在 `computeDamage` (`battle_role_damage.go:1617-1620`) 中：

```go
finalMake, finalExtra, finalHit, other := dam.getMakeHitFinDamage(source.attr, target.attr, atkData)
// finalMake  = 305 造成最终伤害额外 (挂在source)
// finalExtra = 306 受到最终伤害额外 (挂在target) ← 就是这个
// finalHit   = 301 受到最终伤害减免
// other      = 造成最终伤害其他

finDamage = (1.0 + finalMake) * (1.0 - finalHit) * (finalExtra) * (other)
```

306 的值已经是乘算系数，直接参与 `finDamage` 的乘法运算。

### 计算逻辑 (`getMakeFinDamageExtra`)

`battle_role_attr.go:889-969`：

1. 遍历所有 type=306、kind=action 的模块
2. **按技能组（skillGroup）分组** — 同组内加算，不同组间乘算
3. **TeamPass 判断**（line 899-914）：当 `TeamPass==0` 且来源非怪物时，只有伤害来源是模块挂载者本人才生效
4. 满足 ConditionData 条件时，累加 `data × layerNum` 到对应技能组
5. 同调率（1855）也会叠加到 306 计算中
6. 最终各技能组的值乘算在一起：`val = Π(groupVal × 0.01)`

### 示例

假设目标身上有 2 个 306 模块：
- 技能组A: val = 20 → 组内合计 100 + 20 = 120
- 技能组B: val = 30 → 组内合计 100 + 30 = 130

最终 `finalExtra = (120 × 0.01) × (130 × 0.01) = 1.2 × 1.3 = 1.56`，即受到伤害额外增加 56%。

### 与相关 type 的关系

| Type | 名称 | 挂载方 | 计算方式 |
|------|------|--------|---------|
| 301 | 受到最终伤害减免 | target | 减算 `(1 - finalHit)` |
| 305 | 造成最终伤害额外 | source | 乘算，同306逻辑 |
| **306** | **受到最终伤害额外** | **target** | **乘算 `finalExtra`** |
| 1855 | 同调率(易伤) | target | 叠加到306一起计算 |

---

## Q43: entryTypeIsDragon21EnergyValChange (30192) 龙21怪物能量变动判定详解

### 定义

`battle_entry_type.go:1676`

```go
entryTypeIsDragon21EnergyValChange BattleEntryType = 30192 // 龙21 怪物能量变动
```

### 性质

- **事件判断型**（eventJudge），用于 ConditionData 中作为条件
- 判断目标身上指定能量模块的值是否达标/不达标
- 与 30195（车炮台）、30194（奸诈侯爵）共用判断函数 `eventIsCarTurretVal`

### 数据配置 (ConditionData)

| 索引 | 参数 | 含义 |
|------|------|------|
| `Data[0]` | `index` | 能量模块的索引标识（匹配能量模块 Data[0]） |
| `Data[1]` | `kind` | 判定类型：1=达标，2=不达标，其他=直接通过 |
| `Data[2]` | `entryTyp` | 可选，指定要查询的能量 action type。不填默认查 1723，填了查指定 type |

### 判断逻辑 (`eventIsCarTurretVal`)

`battle_entry_event.go:3947-3979`

1. 获取 target
2. 读取 Data[0]=index, Data[1]=kind
3. 确定能量模块类型：Data[2] 存在则取 Data[2]，否则默认 1723
4. 在 target 身上查找该类型的 action 模块，匹配 Data[0]==index
5. 根据 kind 判断：
   - kind=1: `val >= maxVal` → true（达标）
   - kind=2: `val < maxVal` → true（不达标）
   - 其他: → true（直接通过）

### 关联 type

| Type | 名称 | 作用 |
|------|------|------|
| 1743 | `entryTypeDragon21EnergyVal` | 龙21能量存储模块（action），初始化能量值和上限 |
| 1744 | `entryTypeDragon21EnergyValChanged` | 龙21能量变更模块（action），加/减/刷新能量值 |
| **30192** | `entryTypeIsDragon21EnergyValChange` | 本模块，判定能量是否达标（事件判断） |

### 典型配置示例

判断龙21能量已满：`Data = [0, 1, 1743]`
判断龙21能量未满：`Data = [0, 2, 1743]`

### 工作流程

```
1743 初始化能量 → 1744 增减能量（触发事件）→ 条件链中 30192 判断达标 → 执行后续模块
```

---

## Q44: entryTypeBuffModelWithInitPos (10074) 有初始坐标的buff模块详解

### 定义

`battle_entry_type.go:1368`

```go
entryTypeBuffModelWithInitPos BattleEntryType = 10074 // 有初始坐标的buff模块
```

### 性质

- **buff 型 action**（`buffActionDic`），与普通 buff 特效模块一样挂载在角色身上
- 区别：在发送 `addLogEffectStart` 战报时，额外携带**初始坐标信息**
- **不能**在运行中动态改变特效位置，只在生成时一次性指定

### 战报附加字段

`battle_mgr_log.go:1062-1205`，当 `mod.typ == entryTypeBuffModelWithInitPos` 时：

```
ld.arr[15] = 1         // 标记有初始坐标
ld.arr[16] = x坐标
ld.arr[17] = z坐标
ld.arr[18] = 朝向角度

[4,41,0,0,0,0,0,72,0,0,144,0,0,216,0,0,288]
```

### Data[0] kind 枚举

| kind | 含义              | Data 参数                           |
| ---- | --------------- | --------------------------------- |
| 1    | 固定坐标            | Data[1]=x, Data[2]=z, Data[3]=朝向  |
| 2    | 根据风向模块选朝向       | Data[1]=风向模块ID, Data[2~5]=4个方向的朝向 |
| 3    | 根据记录模块方向选坐标     | Data[1]=记录模块ID, 后按方向4组(x,z,朝向)    |
| 4    | 根据圆形区域ID选坐标(5区) | Data[1]=区域配置index, 后按区域5组(x,z,朝向) |
| 5    | 根据圆形区域ID选坐标(6区) | 同4，支持6个区域                         |
| 6    | 目标X坐标或按板块选X     | Data[1]=z, Data[3:]=各板块X坐标数组      |
| 7    | 从记录模块dataArr选坐标 | Data[1]=记录模块ID, Data[2]=索引        |

### 与动态改位置的区别

| 功能 | Type | 说明 |
|------|------|------|
| 生成特效时指定初始位置 | **10074** | 一次性，创建时的位置 |
| 运行中改变特效位置 | 10070 (opType=12) | `addLogBuffModelEffectPosChange`，动态修改 |

需要先生成再动态改位置时，需 10074 + 10070(opType12) 配合使用。

---

## Q45: dataCondition 9,212,-92 与 9,212,-91@12,1 在 type 97 时的区别

### 1. `@` 是条件分隔符

`ConditionData` 在 Go 中类型为 `[][]int32`，`@` 是配置层分隔符，将多个条件数组分开：

| 配置写法 | 解析为 Go 结构 | 条件数量 |
|----------|--------------|---------|
| `9,212,-92` | `[[9, 212, -92]]` | 1个条件 |
| `9,212,-91@12,1` | `[[9, 212, -91], [12, 1]]` | 2个条件（AND关系） |

多个条件在 `Check()` (`battle_entry_condition.go:2316`) 中是 **AND** 关系，逐个判定，任一不通过即失败：

```go
for i := 0; i < len(dataArr); i++ {
    data.condition = dataArr[i]
    data.funType = dataArr[i][0]
    result := checkFun(data)
    if !result {
        return false
    }
}
return true
```

### 2. 条件9: conditionUseSkillType（当前使用的技能类型）

```go
// battle_entry_condition.go:636
func conditionCheckUseSkillType(data *CheckConditionData) bool {
    skillType := condition[1]                     // = 212
    _, prdValue := mod.checkParam(condition[2])   // checkParam(-92) 或 checkParam(-91)
    target := atkData.target
    skiData := target.ski.getNowSkill()
    if skiData == nil { return false }
    if prdValue < 100 && battleRander.Int31n(100) < int32(prdValue) {
        return false   // 概率判定
    }
    return (skillType == 1 && skiData.isKeepSkill())
}
```

`condition[2]` 的 `-92` / `-91` 通过 `checkParam` 解析为技能动态参数值：

| modelSign | 常量名 | 含义 | 解析为 |
|-----------|--------|------|--------|
| **-91** | `modelSignValue1` | 技能参数1 | `skiData.getSkillValue1()` |
| **-92** | `modelSignValue2` | 技能参数2 | `skiData.getSkillValue2()` |

解析后的值作为 `prdValue` 用于**概率判定**：
- 若 `prdValue < 100`：有 `prdValue%` 的概率使条件**失败**（不触发）
- 若 `prdValue >= 100`：概率判定跳过，必定通过概率关卡

最终还需满足 `skillType == 1 && skiData.isKeepSkill()`（技能类型为持续技能）。

### 3. 条件12: conditionSkillElementKind（技能元素类型）

```go
// battle_entry_condition.go:701
func conditionCheckSkillElementKind(data *CheckConditionData) bool {
    skillData := data.skillData
    if skillData == nil { return false }
    eleSkillKind := uint32(condition[1])   // = 1
    return skillData.conf.ElementKind == eleSkillKind
}
```

`12,1` 表示：判断当前技能的 **ElementKind 是否等于 1**。

### 4. 两种配置对比

| 对比项 | `9,212,-92` | `9,212,-91@12,1` |
|--------|-------------|-------------------|
| 概率来源 | 技能参数2（value2） | 技能参数1（value1） |
| 额外条件 | 无 | 技能元素类型必须 == 1 |
| 条件数量 | 单条件 | 双条件 AND |
| 限制程度 | 较宽松 | 更严格（多了元素类型门槛） |

### 5. 实际效果差异

- **`9,212,-92`**：type 97 伤害生效仅需满足条件9（概率值取自技能参数2）
- **`9,212,-91@12,1`**：type 97 伤害生效需同时满足：
  1. 条件9通过（概率值取自技能参数1）
  2. 当前技能的元素类型（ElementKind）== 1

第二种配置的应用场景：只在释放**特定元素类型的技能**时，该 type 97 伤害才有机会生效，并且概率参数来源也不同。

---

## Q46: type 97 的 dataCondition 是什么用途

### 结论

type 97 (`entryTypeDamageHitOther`) 的 `dataCondition` 是**事件触发层的前置条件过滤器**。条件全部通过才会执行 `actionDamageHitOther`，否则跳过不造成伤害。

### 判定时机

`dataCondition` **不是**在 `actionDamageHitOther` 内部检查的，而是在模块被事件触发时、执行 action 之前检查。

以挂在事件300（造成最终伤害）为例，调用链为：

```
事件触发 → eventFinDamageMake() → conditionFun.ConditionCheck(mod, atkData, skillData, nil)
                                        ↓
                                  遍历 mod.conf.ConditionData
                                  每个条件数组调用 checkFun()
                                  全部通过 → return true → 执行 actionDamageHitOther
                                  任一失败 → return false → 跳过，不执行 action
```

### 代码位置

事件判定入口（`battle_entry_event.go:735`）：

```go
func (eve BattleEntryEvent) eventFinDamageMake(mod *BattleEntryModel, eventData interface{}) bool {
    atkData := eventData.(*BattleAtkData)
    skillData := atkData.getSkill()
    isTrigger := conditionFun.ConditionCheck(mod, atkData, skillData, nil)
    return isTrigger
}
```

条件遍历（`battle_entry_condition.go:2316`）：

```go
for i := 0; i < len(dataArr); i++ {
    data.condition = dataArr[i]
    data.funType = dataArr[i][0]
    result := checkFun(data)
    if !result {
        return false   // 任一条件不通过即失败
    }
}
return true
```

### 总结

| 要点 | 说明 |
|------|------|
| 用途 | 事件触发时的**前置条件过滤** |
| 判定时机 | action 执行**之前** |
| 多条件关系 | AND，全部通过才执行 |
| 不通过的结果 | 模块 action 被跳过，type 97 不造成伤害 |
| 通过的结果 | 正常执行 `actionDamageHitOther`，造成伤害 |

---

## Q47: actionBarrierAreaAdd 新增类型参数支持多种 areaIndex 来源

### 需求

为 `actionBarrierAreaAdd` 增加一个类型参数 `dataArr[4]`，根据不同类型值决定 `areaIndex` 的来源：

| 类型值 | areaIndex 来源 | 说明 |
|--------|----------------|------|
| `1`（默认） | `target.ai.navAreaInfo.areaIndex` | 原有逻辑，取目标角色所在板块 |
| `2` | `dataArr[5]` | 取配置中指定的板块值 |
| `3` | `int32(lastMod.getVal())` | 从上一个模块的 val 值读取 |

### Data 参数完整结构

| 索引 | 参数 | 说明 |
|------|------|------|
| `dataArr[0]` | `recordModId` | 记录模块ID（与1986共用同一个记录模块） |
| `dataArr[1]` | `addVal` | 增加值X（支持checkParam） |
| `dataArr[2]` | `maxVal` | 壁垒上限（与1986保持一致，支持checkParam） |
| `dataArr[3]` | 特效基础模块ID | 与1986保持一致，用于刷新特效层级 |
| `dataArr[4]` | `areaType` | **新增**：类型参数（1/2/3），不配置时默认为1 |
| `dataArr[5]` | 板块值 | **新增**：类型为2时读取的配置板块值 |

### 实现代码

**文件**: `internal/battle/battle_entry_action.go` — `actionBarrierAreaAdd` 函数

```go
// 根据类型参数决定areaIndex来源
areaType := int32(1)
if len(dataArr) > 4 {
    areaType = dataArr[4]
}
var areaIndex int32
switch areaType {
case 1:
    areaIndex = target.ai.navAreaInfo.areaIndex
case 2:
    if len(dataArr) > 5 {
        areaIndex = dataArr[5]
    }
case 3:
    lastMod := mod.getLastMod()
    if lastMod == nil {
        return
    }
    areaIndex = int32(lastMod.getVal())
default:
    areaIndex = target.ai.navAreaInfo.areaIndex
}
```

### 向后兼容

- 若 `dataArr` 长度不超过4（未配置类型参数），`areaType` 默认为1，走原有的 `target.ai.navAreaInfo.areaIndex` 逻辑
- 类型3时若 `lastMod` 为 nil 则直接 return，防止空指针

---

## Q48: type=97 配置 @1% target最大生命值+@2% source攻击力 的 Data 写法

### type 97 (entryTypeDamageHitOther) Data 结构

`actionDamageHitOther` 解析 Data 的方式：
- `Data[0]` → 攻击元素（0=无, -1=自动取攻击元素）
- `Data[1:]` → 传给 `getData()` 做表达式计算，得到伤害值

### getData 表达式计算原理

`getData` 内部使用类似逆波兰表达式的方式解析：
1. 先用 `splitCalcDataSigns` 分离前置符号
2. 再用 `calcData` 按运算符（`-88`加, `-87`减, `-89`乘, `-86`除）和操作数进行计算
3. 每个操作数由 `getVal(mod, data1, dataArr)` 解析，`data1` 是函数类型枚举，后续是参数

### 关键函数枚举值

| 枚举值 | 常量名 | 含义 | 计算公式 |
|--------|--------|------|----------|
| `212` | `dataFunMaxHp` | target 最大血量 | `target.maxHp * val * 0.01` |
| `1212` | `dataFunMaxHpSource` | source 最大血量 | `source.maxHp * val * 0.01` |
| `4` | `dataFunRoleAtk` | 攻击力 | `role.atk * val * 0.01` |

### 运算符枚举值

| 值 | 常量名 | 含义 |
|----|--------|------|
| `-88` | `modelSignAdd` | 加法 `+` |
| `-87` | `modelSignSub` | 减法 `-` |
| `-89` | `modelSignMul` | 乘法 `*` |
| `-86` | `modelSignDiv` | 除法 `/` |
| `-84` | `modelSignLeftBracket` | 左括号 `(` |
| `-85` | `modelSignRightBracket` | 右括号 `)` |

### Data 配置写法

```
Data: [元素, 212, @1, -88, 4, @2, 1]
```

逐项解释：

| 位置 | 值 | 含义 |
|------|-----|------|
| `[0]` | 元素 | 攻击元素（0=无, -1=自动取攻击元素） |
| `[1]` | `212` | `dataFunMaxHp` — 取 **target** 最大血量 |
| `[2]` | `@1` | 百分比系数（内部 `* 0.01`） |
| `[3]` | `-88` | `modelSignAdd` — 加法运算符 |
| `[4]` | `4` | `dataFunRoleAtk` — 取攻击力 |
| `[5]` | `@2` | 百分比系数（内部 `* 0.01`） |
| `[6]` | `1` | `len(dataArr) > 1` 时取 **source** 攻击力（不填则取 target） |

### 计算过程

1. `212, @1` → 调用 `getDataMaxHp`：`target.maxHp * @1 * 0.01`
2. `-88` → 加法运算符
3. `4, @2, 1` → 调用 `getDataAtkRate`：`source.atk * @2 * 0.01`（第3个参数 `1` 表示取 source）

最终结果 = `target.maxHp * @1% + source.atk * @2%`

### getDataAtkRate 的 source/target 选择逻辑

```go
func (dat BattleEntryData) getDataAtkRate(mod *BattleEntryModel, dataArr []int32) float64 {
    constVal := dataArr[0]
    var role *BattleRole
    if len(dataArr) > 1 {
        role = mod.getSource()  // 有第2个参数 → 取 source
    } else {
        role = mod.getTarget()  // 无第2个参数 → 取 target
    }
    atk := role.attr.getAtk(true, mod.atkData)
    _, val := mod.checkParam(constVal)
    return atk * val * 0.01
}
```

### getDataMaxHp 的计算逻辑

```go
func (dat BattleEntryData) getDataMaxHp(mod *BattleEntryModel, dataArr []int32, isSource bool) float64 {
    _, val := mod.checkParam(dataArr[0])
    // ...
    attr := role.attr.getMaxHp(true)
    retVal := attr * val * 0.01 + val2  // val2 为可选固定值加成
    return retVal
}
```

- `dataFunMaxHp`(212) → `isSource=false` → 取 target
- `dataFunMaxHpSource`(1212) → `isSource=true` → 取 source

---

## Q49: 用 entryTypeDragon21EnergyValChanged(1744) 实现充能+镇龙柱阻止+红色壁垒效率衰减

### 需求描述

1. 常规情况下，每秒充能 @2 点，能量充满时提高对 boss 造成的伤害，持续一段时间
2. 镇龙柱 buff 持续期间，停止每秒的自然充能
3. 每当场上存在一个【红色壁垒】，每秒的充能效率降低 20%

### 相关模块关系

| 类型 | ID | 作用 |
|------|------|------|
| `entryTypeDragon21EnergyVal` | 1743 | 初始化能量条（设 maxVal） |
| `entryTypeDragon21EnergyValChanged` | 1744 | 改变能量值（加/减/刷新） |
| `entryTypeIsDragon21EnergyValChange` | 30192 | 能量变动后触发的事件 |
| `entryTypeChargeWithEfficiency` | 1987 | 带效率衰减的充能系统（最终实现） |

### 1744 的 Data 配置格式

```
dataArr[0]: eventKind  → 能量变化后触发的事件类型 (30192)
dataArr[1]: kind       → 0=减, 1=加, 2=刷新
dataArr[2]: actionKind → 要找哪个action模块改它的val (1743)
dataArr[3:]: value     → 改变量 (支持 checkParam, 如 @2)
```

### 1744 内部流程

位置: `battle_entry_action_buff_monster.go:1591` — `actionDragon21EnergyValChanged`

```
1. 根据 actionKind(dataArr[2]) 找到 target 身上的 1743 模块
2. 根据 kind 计算 addVal（0=减, 1=加, 2=刷新）
3. 修改 val，clamp 到 [1, maxVal]
4. 调 addLogBattleValChanged 发战报
5. triggerEvent(eventKind) 触发后续事件
```

### 用 1744 实现三个需求的思路

**需求1 — 每秒充能 @2：**
配一个定时器模块每秒触发一个 1744 模块：
```
1744.Data = [30192, 1, 1743, @2参数...]
```
1744 找到 1743 能量模块 → 每秒 +@2 → 到满自动 clamp → 充满后通过 30192 事件触发增伤 buff

**需求2 — 镇龙柱 buff 期间停止充能：**
在定时器或 1744 模块上配 condition，判断 source 身上是否存在镇龙柱 buff 类型模块，存在则不执行

**需求3 — 红色壁垒效率衰减：**
**1744 单独无法处理**。原因：1744 的 addVal 从 dataArr 固定读取（`battleEntryData.getData`），不支持动态乘以效率系数

### 最终方案：使用 1987 (entryTypeChargeWithEfficiency)

因为 1744 不支持动态效率衰减，所以实现了独立的 1987 类型，内聚了全部逻辑。

**1987 的 Data 配置：**
```
dataArr[0]: 每秒基础充能值 (@2)
dataArr[1]: 最大能量值 (@3)
dataArr[2]: 充满时触发的增伤buff模块ID
dataArr[3]: 壁垒记录模块ID（读取红色壁垒数量）
dataArr[4]: 阻止充能的buff类型（镇龙柱buff）
dataArr[5]: 能量变化战报模块ID
```

**1987 执行流程** (`battle_entry_action.go:15365`):
```go
1. 检查镇龙柱buff → hasGlobalActionModelArr(blockBuffType, source) → 存在则 return
2. 读壁垒记录模块的 val → 红色壁垒数量 (barrierModArr[0].getVal())
3. 计算效率: efficiency = 1.0 - redCount * 0.2 (每个红壁垒降20%, 最低0)
4. 实际充能: actualCharge = chargePerSec * efficiency
5. mod.val += actualCharge, 设 maxVal
6. addModelIDArr 发能量变化战报
7. 充满判断: val >= maxEnergy → 清零 + 触发增伤buff模块
```

### 1987 vs 1744 对比

| 特性 | 1744 | 1987 |
|------|------|------|
| 充能值 | 从 dataArr 固定读取 | 支持动态计算(×效率) |
| 镇龙柱检查 | 需外部 condition | 内置检查 |
| 红色壁垒衰减 | 不支持 | 内置支持 |
| 充满触发buff | 需外部事件链 | 内置判断 |
| val修改目标 | 找其他模块(1743)的val | 修改自身val |

### 如果要在 1744 中支持效率衰减

可在 `actionDragon21EnergyValChanged` 新增 kind=3 分支：
```go
case 3: // 带效率衰减的充能
    baseVal := battleEntryData.getData(mod, dataArr[4:])
    barrierModId := dataArr[3]
    barrierModArr := mod.ent.getGlobalModelArrByModelId(uint32(barrierModId), modelKindAction, target)
    redCount := float64(0)
    if len(barrierModArr) > 0 {
        redCount = barrierModArr[0].getVal()
    }
    efficiency := 1.0 - redCount*0.2
    if efficiency < 0 {
        efficiency = 0
    }
    addVal = baseVal * efficiency
```
但这样会增加 1744 的复杂度，不如保持 1987 独立实现

## Q50: entryTypeSetModTargetPos (284) 设置模块 TargetPos 位置 Data 字段各枚举详解

### 基本信息

- **Type**: 284
- **枚举名**: `entryTypeSetModTargetPos`
- **功能**: 设置模块的 TargetPos 位置
- **函数**: `actionSetModTargetPos`
- **文件**: `battle_entry_action.go:5732`

### 数据结构

- `Data[0]` = **type**（枚举，决定取位置的方式）
- `Data[1]` = **isActionRun**（1 = 设置完位置后立即执行 `actionRun`）
- `Data[2...]` = 各 type 的附加参数

### 通用逻辑

- 除了 `type=8` 外，**只执行一次**（`runCount > 0` 时直接走 `actionRun` 跳过）
- `type=8` 可以多次执行

### Data[0] 各枚举详解

#### type = 1：敌方队伍中心点
- 取 source 的**所有敌方玩家**的坐标，计算中心点
- 将 TargetPos 设为该中心点
- 无需额外参数

#### type = 2：来源前方指定距离
- 取 source 的当前目标方向，沿该方向偏移 `Data[2]` 的距离
- `Data[2]` = 距离值
- Y 轴保持与 source 一致
- 如果没有目标，TargetPos 设为 source 自身位置

#### type = 3：上级模块（lastMod）的 TargetPos
- 取 `lastMod` 的 TargetPos
- `Data[2]`（可选）= 向上追溯层数（0 = 直接 lastMod，1 = lastMod.lastMod，以此类推）

#### type = 4：绝对世界坐标
- `Data[2]` = X 坐标
- `Data[3]` = Z 坐标
- `Data[4]`（可选）= 1 时，X/Z 会乘以 0.01（即配的是厘米值，转换为米）

#### type = 5：世界坐标 + 区块偏移
- `Data[2]` = 基础 offset，加上 `raid.areaIdOffset`（超过 3 则减 3 循环）
- `Data[3:]` = 坐标对数组，每 2 个为一组 `[X, Z, X, Z, ...]`
- 根据计算出的 offset 取对应的坐标对，乘以 0.01 转换

#### type = 6：根据目标区域 ID 取世界坐标
- `Data[2]` = 传给 `raid.checkTargetAreaId` 的参数，返回一个 offset
- `Data[3:]` = 坐标对数组（同 type 5）
- 根据 offset 索引取坐标对

#### type = 7：根据上级模块目标的板块索引取世界坐标
- 取 `lastMod.target` 的 `navAreaInfo.areaIndex` 作为 offset
- `Data[2:]` = 坐标对数组
- 根据 areaIndex 索引取坐标对
- **只执行一次**

#### type = 8：同 type 7，但可多次执行
- 逻辑与 type 7 完全一致
- 区别：type 8 不受 `runCount > 0` 限制，每次触发都会重新计算位置

### 总结表

| type | 含义 | 额外参数 | 多次执行 |
|------|------|----------|----------|
| 1 | 敌方玩家中心点 | 无 | 否 |
| 2 | source 朝目标方向偏移 | `Data[2]`=距离 | 否 |
| 3 | lastMod 的 TargetPos | `Data[2]`=追溯层数 | 否 |
| 4 | 绝对世界坐标 | `Data[2]`=X, `Data[3]`=Z, `Data[4]`=是否/100 | 否 |
| 5 | 世界坐标+区块偏移 | `Data[2]`=base offset, `Data[3:]`=坐标对 | 否 |
| 6 | 目标区域ID索引坐标 | `Data[2]`=区域参数, `Data[3:]`=坐标对 | 否 |
| 7 | lastMod目标板块索引坐标 | `Data[2:]`=坐标对 | 否 |
| 8 | 同7，可重复执行 | `Data[2:]`=坐标对 | **是** |
