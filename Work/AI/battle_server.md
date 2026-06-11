# Battle Type 问答记录

## 目录
- [[#Q1: entryTypeSummon (825) 召唤模块如何实现，dataCondition 和 data 参数分别是什么意思]]
- [[#Q2: entryTypeRecoverEnergyVal (1100) 复苏能量值模块介绍]]
- [[#Q3: entryTypeRoleStandAreaDetect (10124) 角色站立区域检测模块介绍]]
- [[#Q4: entryTypeRandomVal(10045) 根据规则随机值模块详细介绍]]
- [[#Q5: Battle Entry Data 的 Time 字段含义枚举]]
- [[#Q6: Battle Model 的 Buff 字段枚举含义]]
- [[#Q7: entryTypeSetLayerNum(10013) 设置模块层数介绍]]
- [[#Q8: checkParam modelSign 负数常量完整枚举]]
- [[#Q9: Target 字段结构详解及 3,0,23,-93 配置解析]]
- [[#Q10: entryTypeTriggerMod (10051) 根据值触发模块完整介绍]]
- [[#Q11: entryTypeSaveTargetArea (10054) 详细介绍及如何对特定板块目标造成伤害]]
- [[#Q12: entryTypeDamageHit (100) 核心伤害模块详解]]
- [[#Q13: entryTypeDamageHitOther (97) 值类型伤害模块详解]]
- [[#Q14: entryTypeBuffModelWithInitPos (10074) 有初始坐标的buff模块详解]]
- [[#Q15: entryTypeSetModTargetPos (284) 设置模块 TargetPos 位置 Data 字段各枚举详解]]
- [[#Q16: Refresh 字段枚举含义与生效逻辑]]
- [[#Q17: BattleEntryData.getData() 方法详解与表达式解析流程]]
- [[#Q18: BattleEntryData.calcData() 方法原理详解]]
- [[#Q19: BattleEntryModel.checkParam() 方法详解与参数替换链路]]
- [[#Q20: entryTypeChangeModSkillData2 (10150) 有什么用]]
- [[#Q21: entryTypeLayerTriggerAndClear (1990) 中 addModelIDArr(...) 的目的]]
- [[#Q23: 93006901/10/11/12/13/20/21/22/23/30/40/41/45/46 这组模块如何实现“随机点名连线 + 活性猛毒跳转/断线回能”]]
- [[#Q24: 幽魂形态 / 实体形态 进入区域停留2秒后切换状态的实现思路]]
- [[#Q25: entryTypeRoleStandAreaDetect (10124) 的 data=92013620,9,-91,1,92013623 是什么意思]]
- [[#Q26: is_clear_model 这个字段详细解释]]
- [[#Q29: modelKindAction、modelKindEvent、modelKindCheck 三者区别]]

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

## Q2: entryTypeRecoverEnergyVal (1100) 复苏能量值模块介绍

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

## Q3: entryTypeRoleStandAreaDetect (10124) 角色站立区域检测模块介绍

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

## Q4: entryTypeRandomVal(10045) 根据规则随机值模块详细介绍

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

## Q5: Battle Entry Data 的 Time 字段含义枚举

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

## Q6: Battle Model 的 Buff 字段枚举含义

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

## Q7: entryTypeSetLayerNum(10013) 设置模块层数介绍

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

## Q8: checkParam modelSign 负数常量完整枚举

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

## Q9: Target 字段结构详解及 3,0,23,-93 配置解析

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

## Q10: entryTypeTriggerMod (10051) 根据值触发模块完整介绍

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

## Q11: entryTypeSaveTargetArea (10054) 详细介绍及如何对特定板块目标造成伤害

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

## Q12: entryTypeDamageHit (100) 核心伤害模块详解

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

## Q13: entryTypeDamageHitOther (97) 值类型伤害模块详解

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

## Q14: entryTypeBuffModelWithInitPos (10074) 有初始坐标的buff模块详解

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

## Q15: entryTypeSetModTargetPos (284) 设置模块 TargetPos 位置 Data 字段各枚举详解

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

---

## Q16: Refresh 字段枚举含义与生效逻辑

### 定义位置

- `Refresh` 字段的枚举定义在 [battle_entry_model.go](/Users/gexianglin/zserver/internal/battle/battle_entry_model.go#L13)
- **不在** `battle_entry_type.go`

### 核心语义

`Refresh` 控制的是：

- 当一个新模块准备加入时
- 如果场上已经有“相同/相似”的旧模块
- 到底是直接新增、刷新旧模块，还是只做叠值处理

真实生效入口在 [battle_entry.go](/Users/gexianglin/zserver/internal/battle/battle_entry.go#L692) 的 `isAddModel(...)`。

### 枚举含义

| 值 | 名称 | 含义 |
|---|---|---|
| `0` | `notRefresh` | 不刷新，直接新增模块 |
| `1` | `normalRefresh` | 按 `source + target + modelID` 刷新已有模块 |
| `2` | `refreshByTypeAndTarget` | 按 `target + typ` 刷新已有模块 |
| `3` | `notRefreshAddVal` | 不刷新，但通常用于值累加语义 |
| `4` | `refreshBySourceAndType` | 按 `source + target + typ` 刷新已有模块 |
| `5` | `refreshBySourceWithType` | 按 `source + typ` 刷新已有模块，不看 target |
| `6` | `normalRefreshWithoutRestRunTime` | 和 `1` 一样按 `source + target + modelID` 刷新，但不重置 `runTime` |
| `7` | `refreshByTypeAndTargetAndId` | 按 `target + typ + modelID` 刷新已有模块 |

### 每种 Refresh 对应的匹配方式

代码在 [battle_entry.go](/Users/gexianglin/zserver/internal/battle/battle_entry.go#L710)：

- `1 / 6`：`getSameModelWithSourceAndTargetAndModelID`
- `2`：`getSameModelWithTargetAndType`
- `4`：`getSameModelWithSourceAndTargetAndType`
- `5`：`getSameModelWithSourceAndType`
- `7`：`getSameModelWithTargetAndTypeId`

### 刷新命中旧模块后会发生什么

如果命中旧模块，逻辑在 [battle_entry.go](/Users/gexianglin/zserver/internal/battle/battle_entry.go#L724)：

1. 触发 `WhenModRefresh / WhenModRefreshOrAdd` 事件
2. 大多数类型会把旧模块 `runTime` 重置为 `0`
3. `runCount` 重置为 `1`
4. `layerNum` 会执行 `addLayerNum()`
5. 如果是 buff/debuff，还会继续走层数检查事件
6. 如果 `RefreshAction == 1`，还会重新执行一次 `runAction(v)`

### `RefreshAction` 的含义

`RefreshAction` 是另一层控制，不等同于 `Refresh`。

判断在 [battle_entry_model.go](/Users/gexianglin/zserver/internal/battle/battle_entry_model.go#L814)：

```go
func (mod *BattleEntryModel) isRefreshModAction() bool {
    return mod.getConf().RefreshAction == 1
}
```

含义是：

- `Refresh`：决定“找到谁来刷新”
- `RefreshAction`：决定“刷新到旧模块后，要不要再重跑一次行为逻辑”

### 一句话记法

- 想控制“是否复用旧模块、按谁匹配旧模块”，看 `Refresh`
- 想控制“刷新后要不要再执行 action”，看 `RefreshAction`

---

## Q17: BattleEntryData.getData() 方法详解与表达式解析流程

### 方法位置

- 方法定义在 [battle_entry_data.go](/Users/gexianglin/zserver/internal/battle/battle_entry_data.go#L395)

```go
func (dat BattleEntryData) getData(mod *BattleEntryModel, dataArr []int32) float64 {
	signArr, validData := splitCalcDataSigns(dataArr)
	calcVal := dat.calcData(mod, validData)
	calcVal = calcDataSignOper(mod, signArr, calcVal)
	return calcVal
}
```

### 一句话作用

`getData()` 是 battle 配表里 **Data 数值表达式的统一解释入口**。

它负责把一串 `[]int32` 配置解析成最终 `float64` 数值，主要分三步：

1. 先拆出“后处理符号” `signArr`
2. 再计算主体表达式 `validData`
3. 最后把后处理规则作用到结果上

### 整体流程

#### 第 1 步：拆出后处理符号

调用 [battle_entry_data.go](/Users/gexianglin/zserver/internal/battle/battle_entry_data.go#L2384) 的 `splitCalcDataSigns(...)`

它只会在 **数组开头** 识别这几种“结果修饰符”：

- `modelSignClamp`
- `modelSignMax`
- `modelSignMin`
- `modelSignAbs`

识别到后，会把这些前缀放进 `signArr`，剩下的放进 `validData`。

#### 第 2 步：计算主体表达式

调用 [battle_entry_data.go](/Users/gexianglin/zserver/internal/battle/battle_entry_data.go#L2414) 的 `calcData(...)`

这一步是真正的表达式求值器，负责处理：

- 各种 `dataFunXXX`
- `+ - * /`
- 括号
- 多段表达式拼接

它最终返回一个基础计算结果 `calcVal`。

#### 第 3 步：对结果做修饰

调用 [battle_entry_data.go](/Users/gexianglin/zserver/internal/battle/battle_entry_data.go#L2334) 的 `calcDataSignOper(...)`

这一步不是重新算表达式，而是对上一步的结果做收尾处理，比如：

- 限制区间
- 强制最小值
- 强制最大值
- 取绝对值

### `splitCalcDataSigns()` 具体做了什么

方法在 [battle_entry_data.go](/Users/gexianglin/zserver/internal/battle/battle_entry_data.go#L2384)。

它从左到右扫描 `dataArr`，只要遇到这些前缀符号就继续吃：

- `modelSignClamp`：长度 3
  - 结构：`-100, min, max`
- `modelSignMax`：长度 2
  - 结构：`-101, max`
- `modelSignMin`：长度 2
  - 结构：`-102, min`
- `modelSignAbs`：长度 1
  - 结构：`-103`

一旦读到不是这几种的值，就停止前缀提取。

#### 例子

如果配置是：

```text
-100, 1, 999, 4, -93, 1, -88, 212, -92
```

那么会被拆成：

- `signArr = [-100, 1, 999]`
- `validData = [4, -93, 1, -88, 212, -92]`

也就是：

- 先正常计算 `4,-93,1 + 212,-92`
- 最后再把结果 clamp 到 `[1, 999]`

### `calcData()` 是怎么计算表达式的

方法在 [battle_entry_data.go](/Users/gexianglin/zserver/internal/battle/battle_entry_data.go#L2414)。

它本质上做了两层工作：

#### 第一层：中缀转后缀

它会把原始表达式里的：

- `(` `)`
- `+`
- `-`
- `*`
- `/`

转换成更容易计算的后缀表达式形式。

对应符号常量在 `BattleEntryModelSign`：

- `modelSignLeftBracket = -84`
- `modelSignRightBracket = -85`
- `modelSignDiv = -86`
- `modelSignSub = -87`
- `modelSignAdd = -88`
- `modelSignMul = -89`
- `modelSignFinishFlag = -83`

这里用 `modelSignFinishFlag` 当“一个 dataFun 表达式片段结束”的分隔符。

#### 第二层：执行后缀表达式

后缀表达式执行时：

- 普通数据片段先收集到 `temp`
- 碰到 `FinishFlag` 时，调用 `dat.getVal(mod, temp[0], temp[1:])`
- 得到一个 `float64` 压入栈
- 碰到 `+ - * /` 时，从栈里弹两个值做运算，再把结果压回去

最后栈顶值就是整个表达式结果。

### `getVal()` 在这里扮演什么角色

方法在 [battle_entry_data.go](/Users/gexianglin/zserver/internal/battle/battle_entry_data.go#L157)。

它负责把一个“片段”解释成具体数值。

比如：

- `1, x` -> 默认值
- `3, x` -> `x * 0.01`
- `4, -93, 1` -> 来源攻击力 * 参数3%
- `212, -92` -> 目标最大血量 * 参数2%

所以 `calcData()` 并不直接认识所有业务含义，真正的业务取值都在 `getVal()` 的大 switch 里。

### `calcDataSignOper()` 做了什么

方法在 [battle_entry_data.go](/Users/gexianglin/zserver/internal/battle/battle_entry_data.go#L2334)。

它按顺序处理 `signArr`，对最终值做修饰：

- `modelSignClamp`
  - 把结果限制在 `[min, max]`
- `modelSignMax`
  - 结果超过 `max` 时压到 `max`
- `modelSignMin`
  - 结果低于 `min` 时抬到 `min`
- `modelSignAbs`
  - 取绝对值

注意这里的参数也会走 `mod.checkParam(...)`，所以 `min/max` 也可以是动态参数，不一定是写死常量。

### 为什么要拆成这三步

这样设计有几个明显好处：

1. **表达式主体和结果修饰分离**
   - 先算公式
   - 再统一做 clamp/min/max/abs

2. **配置能力强**
   - `dataFunXXX` 负责取业务值
   - `+ - * / ()` 负责拼公式
   - 前缀 sign 负责做结果限制

3. **便于复用**
   - 97、100、治疗、护盾、特殊玩法模块都能复用这套解释器

### 一个直观例子

假设有一段：

```text
4,-93,1,-88,212,-92
```

可以理解成：

```text
source.atk * 参数3%
+
target.maxHp * 参数2%
```

如果前面再加：

```text
-100,1,999
```

就变成：

```text
clamp(
  source.atk * 参数3% + target.maxHp * 参数2%,
  1,
  999
)
```

这正是 `getData()` 的完整执行方式：

1. 拆出 `clamp`
2. 算主体表达式
3. 把结果限制到区间内

### 总结

`BattleEntryData.getData()` 可以理解成 battle 配表里的“小型公式解释器入口”：

- `splitCalcDataSigns()`：拆出结果修饰符
- `calcData()`：解析并计算主体表达式
- `getVal()`：解释每一个 `dataFun` 片段
- `calcDataSignOper()`：对最终结果做 clamp/min/max/abs 收尾

如果配表里是一串 `Data` 数值公式，最终几乎都会走到这里变成一个 `float64`。

---

## Q18: BattleEntryData.calcData() 方法原理详解

### 方法位置

- 方法定义在 [battle_entry_data.go](/Users/gexianglin/zserver/internal/battle/battle_entry_data.go#L2414)

```go
func (dat BattleEntryData) calcData(mod *BattleEntryModel, dataArr []int32) float64
```

### 一句话作用

`calcData()` 是 `getData()` 里的**主体表达式求值器**。  
它负责把一串 battle 配表表达式：

- `dataFunXXX`
- `+ - * /`
- `()`

解析并计算成最终的 `float64`。

### 它不是做什么的

先把边界说清楚：

- 它**不处理**开头那几个后处理符号：
  - `clamp`
  - `max`
  - `min`
  - `abs`
- 这些是 `getData()` 外层先拆给 `splitCalcDataSigns()`，最后再由 `calcDataSignOper()` 做的

所以 `calcData()` 只管：

- “主体公式本身怎么计算”

### 整体原理

它本质上分成两段：

1. **把中缀表达式转成后缀表达式**
2. **再用栈执行后缀表达式**

这是一个很经典的表达式解释器写法。

### 第 1 段：中缀转后缀

代码在 [battle_entry_data.go](/Users/gexianglin/zserver/internal/battle/battle_entry_data.go#L2416) 开始。

它维护两个东西：

- `stack`
  - 暂存操作符和括号
- `newDataArr`
  - 输出转换后的“后缀表达式”

它识别的运算符号来自 `BattleEntryModelSign`：

- `modelSignLeftBracket = -84`
- `modelSignRightBracket = -85`
- `modelSignDiv = -86`
- `modelSignSub = -87`
- `modelSignAdd = -88`
- `modelSignMul = -89`
- `modelSignFinishFlag = -83`

#### 普通数据怎么处理

如果读到的不是括号也不是四则运算符，就直接塞进 `newDataArr`：

```go
default:
    newDataArr = append(newDataArr, data)
```

这些“普通数据”本质上是：

- 一个 `dataFun id`
- 加上它后面的参数

例如：

```text
4,-93,1
```

表示一个完整片段，后面遇到 `FinishFlag` 才会真正结算。

#### 为什么会插入 `FinishFlag`

这段实现里最关键的设计是：

- 一遇到括号或运算符
- 先往 `newDataArr` 里插一个 `modelSignFinishFlag`

例如：

```go
newDataArr = append(newDataArr, int32(modelSignFinishFlag))
```

它的意义是：

- 把前面已经读完的一段 `dataFun + 参数` 片段收口
- 让第二阶段知道“这里应该调用一次 `getVal()`”

也就是说，这套表达式不是按单个数字 token 求值，而是按“片段”求值。

### 括号怎么处理

#### 左括号 `(`

遇到左括号时：

1. 先插入一个 `FinishFlag`
2. 再把左括号压到 `stack`

```go
case modelSignLeftBracket:
    newDataArr = append(newDataArr, int32(modelSignFinishFlag))
    stack.PushBack(sign)
```

#### 右括号 `)`

遇到右括号时：

1. 先插入一个 `FinishFlag`
2. 把 `stack` 里直到最近左括号之间的操作符全部弹出，追加到 `newDataArr`
3. 左括号自己丢掉

这就是典型的“括号出栈”逻辑。

### 运算符怎么处理

遇到 `+ - * /` 时：

1. 先插入一个 `FinishFlag`
2. 把 `stack` 里直到左括号为止的操作符全部弹出来
3. 再把当前运算符压栈

代码在 [battle_entry_data.go](/Users/gexianglin/zserver/internal/battle/battle_entry_data.go#L2438)。

这里有一个很重要的实现特征：

- 它**没有做乘除高于加减**的优先级区分
- 只要遇到新的运算符，就把栈里已有运算符一直弹到左括号为止

所以它的优先级规则更接近：

- **括号优先**
- **同层内运算符按出现顺序结算**

这点很关键，配表时不能想当然按普通数学优先级理解。

### 第一阶段结束后做了什么

扫描完整个 `dataArr` 后：

1. 再补一个 `FinishFlag`
2. 把 `stack` 里剩余的运算符全部弹到 `newDataArr`

这样 `newDataArr` 就变成了一份可以直接求值的后缀表达式。

### 第 2 段：执行后缀表达式

代码在 [battle_entry_data.go](/Users/gexianglin/zserver/internal/battle/battle_entry_data.go#L2470) 开始。

这里重新利用 `stack`，但语义变了：

- 第一段的 `stack` 存的是“操作符”
- 第二段的 `stack` 存的是“已经算出来的 float64 值”

另外还有一个：

- `temp []int32`
  - 用来临时收集一个完整的 `dataFun` 片段

#### 遇到普通数据

先把它塞进 `temp`：

```go
default:
    temp = append(temp, data)
```

#### 遇到 `FinishFlag`

说明一个片段读完了，这时：

1. 如果 `temp` 为空，跳过
2. 否则调用：

```go
val := dat.getVal(mod, temp[0], temp[1:])
```

3. 得到这个片段的 `float64`
4. 压入值栈
5. 清空 `temp`

这里就是把：

```text
4,-93,1
```

这种片段真正解释成一个数值的地方。

### 遇到运算符时怎么做

碰到 `+ - * /` 时，会调用：

- [battle_entry_data.go](/Users/gexianglin/zserver/internal/battle/battle_entry_data.go#L2500) 的 `getStackValue(...)`

它会：

1. 从值栈顶弹出 `val1`
2. 再弹出 `val2`
3. 根据运算符计算结果
4. 把结果重新压栈

### `getStackValue()` 的运算顺序

这里也有个很重要的细节。

代码是：

```go
val1 := stack.Back()
stack.Remove(val1)
val2 := stack.Back()
stack.Remove(val2)
```

然后：

- 加法：`val1 + val2`
- 减法：`val1 - val2`
- 乘法：`val1 * val2`
- 除法：`val1 / val2`

也就是说它是按：

- **栈顶值 op 次栈顶值**

来算的，不是常见教材里“次栈顶 op 栈顶”那种写法。

所以阅读这套后缀逻辑时，要以它的实际实现为准，不要直接套标准公式想象。

### 最终返回值

当整份 `newDataArr` 都执行完后：

- 取值栈顶元素
- 转成 `float64`
- 作为 `calcData()` 的结果返回

如果栈是空的，就返回 `0`。

### 一个简化理解模型

你可以把 `calcData()` 想成三层：

1. **分词层**
   - 普通整数流
   - 里面混着 `dataFun`、参数、运算符、括号

2. **片段层**
   - 把 `dataFun + 参数` 视为一个“可求值片段”
   - 用 `FinishFlag` 切段

3. **表达式层**
   - 片段先通过 `getVal()` 变成数值
   - 数值再通过 `+ - * / ()` 做组合

### 它和 `getData()` 的关系

关系可以这样看：

- `getData()`
  - 负责总调度
- `splitCalcDataSigns()`
  - 拆前缀修饰符
- `calcData()`
  - 计算主体公式
- `calcDataSignOper()`
  - 对最终值做收尾修饰

也就是说：

- `calcData()` 是“算主体”
- 不是“算全部”

### 配表时最该注意的点

1. `calcData()` 处理的是 **主体表达式**
2. `clamp / min / max / abs` 不在这里算
3. 表达式片段真正落值靠 `getVal()`
4. 括号能改变结合顺序
5. 同层运算符的优先级实现要以代码为准，不能完全按普通数学经验脑补

### 总结

`calcData()` 的本质就是 battle 配表系统里的 **表达式执行引擎**：

- 第一阶段把中缀表达式改写成后缀结构
- 第二阶段按后缀表达式用栈求值
- 每个 `dataFun` 片段通过 `getVal()` 变成具体数值
- 最终得到一个主体计算结果，交回 `getData()` 继续做后处理

---

## Q19: BattleEntryModel.checkParam() 方法详解与参数替换链路

### 方法位置

- `checkParam()` 定义在 [battle_entry_model.go](/Users/gexianglin/zserver/internal/battle/battle_entry_model.go#L1020)
- `checkParamRaw()` 定义在 [battle_entry_model.go](/Users/gexianglin/zserver/internal/battle/battle_entry_model.go#L1025)
- `ChangeModelParamValue()` 定义在 [battle_entry_model.go](/Users/gexianglin/zserver/internal/battle/battle_entry_model.go#L1507)

```go
func (mod *BattleEntryModel) checkParam(val int32) (bool, float64) {
    state, getVal := mod.checkParamRaw(val)
    return state, mod.ChangeModelParamValue(val, getVal)
}
```

### 一句话作用

`checkParam()` 是 battle 配表里 **单个参数值解析的统一入口**。

它负责把一个 `int32` 参数解释成最终可用的 `float64`，同时告诉调用方：

- 这个值是不是“动态参数/特殊参数”
- 最终该取多少

### 返回值含义

返回值是：

```go
(bool, float64)
```

含义分别是：

- `bool`
  - 表示这个参数是否被识别成了“特殊参数/动态参数”
- `float64`
  - 表示最终解析后的值

常见理解方式：

- `false, 原值`
  - 说明它不是特殊 sign，基本就是普通常量
- `true, 计算值`
  - 说明它命中了某个 `modelSignXXX`，已经被替换成动态结果

### 它的完整链路

`checkParam()` 实际分两步：

1. `checkParamRaw(val)`
   - 先把原始参数解释成基础值
2. `ChangeModelParamValue(val, getVal)`
   - 再检查这个参数是否需要被“参数修改模块”二次修正

也就是说：

- `checkParamRaw()` 负责“**这个 sign 本身是什么意思**”
- `ChangeModelParamValue()` 负责“**这个 sign 的结果要不要再被别的模块改动**”

### 第一步：`checkParamRaw()` 做了什么

方法在 [battle_entry_model.go](/Users/gexianglin/zserver/internal/battle/battle_entry_model.go#L1025)。

#### 先拿技能上下文

```go
skiData := mod.getSkiData()
if skiData == nil {
    return false, float64(val)
}
```

这一步很关键：

- 如果当前模块没有 `skiData`
- 那就直接把参数当普通常量返回

也就是说很多 `-91/-92/-93` 这类技能参数替换，前提是模块本身有技能上下文。

#### 然后按 `BattleEntryModelSign` 分派

后面是一个很大的：

```go
switch BattleEntryModelSign(val)
```

它会把负数 sign 解析成具体运行时数值。

例如：

- `modelSignSkillWarningTime`
  - 取技能预警时间
- `modelSignChaneTime`
  - 取吟唱时间
- `modelSignBallisticTime`
  - 根据距离和弹道速度算弹道时间
- `modelSignKeepTime`
  - 取引导时长
- `modelSignModTimeEventDataEnergy`
  - 根据 eventData 里的能量算持续时间
- `modelSignLastModLayer`
  - 取上一个模块层数
- `modelSignLastModLeftTime`
  - 取上一个模块剩余时间

这些都属于：

- **把一个配置里的 sign 常量**
- **替换成当前战斗上下文里的动态值**

#### 默认分支

如果没命中特殊 case，就走：

```go
default:
    return skiData.checkParam(val)
```

这说明：

- `BattleEntryModel.checkParamRaw()` 只处理一部分 battle 级 sign
- 另一大部分，尤其是技能参数类（例如 `-91 ~ -97`），会继续交给 `BattleSkillData.checkParam(...)`

所以整个参数系统是分层的：

1. 模块级 `checkParamRaw()`
2. 技能级 `skiData.checkParam()`

### 第二步：`ChangeModelParamValue()` 做了什么

方法在 [battle_entry_model.go](/Users/gexianglin/zserver/internal/battle/battle_entry_model.go#L1507)。

它的作用不是“解析 sign”，而是：

- 看当前场上有没有“参数改值模块”
- 如果有，并且正好命中了这个参数 sign
- 就对刚才算出来的值再做一次加减修正

代码结构是：

```go
check, valMod := mod.checkModelParamChangeCondition()
if !check || valMod == nil {
    return value
}
```

说明：

- 没命中改值条件，就原样返回

#### 命中后怎么判断是不是要改这个参数

```go
modData := valMod.getConf().Data
signKey := modData[0] == sign
if !signKey {
    return value
}
```

也就是说，改值模块的 `Data[0]` 要明确指定“它改哪一个 sign”。

#### 加还是减

```go
isAdd := modData[1] == 1 // 0减少 1增加
offset := battleEntryData.getData(valMod, modData[2:])
```

这里的含义是：

- `Data[1] = 1`
  - 表示增加
- `Data[1] = 0`
  - 表示减少
- `Data[2:]`
  - 不是写死常量，而是又走了一次 `battleEntryData.getData(...)`
  - 说明“改多少”本身也可以是一个完整表达式

最后：

```go
if isAdd {
    return value + offset
} else {
    return value - offset
}
```

### 这意味着什么

`checkParam()` 不是简单的“负数替换”。

它实际上是一个两层系统：

1. **基础替换**
   - sign -> 当前战斗上下文值
2. **二次修正**
   - 某些模块可以继续改这个参数的最终结果

所以同一个 `-91`：

- 在不同技能上会先解析出不同基础值
- 在不同战斗状态下还可能再被参数改值模块进一步调整

### 一个直观例子

假设配置里写了：

```text
-91
```

调用 `checkParam(-91)` 时，流程可能是：

1. `checkParamRaw(-91)`
   - 交给 `skiData.checkParam(-91)`
   - 得到“技能参数1”的实际值，比如 `300`
2. `ChangeModelParamValue(-91, 300)`
   - 检查场上是否有“修改参数1”的模块
   - 如果有，可能再加 `50`
3. 最终返回：

```text
true, 350
```

### 常见使用场景

这个方法在 battle 里几乎到处都会被用到，例如：

- 解析 `Data[]` 里的技能参数
- 解析伤害倍率参数
- 解析持续时间、间隔、次数
- 解析特效时间、弹道时间
- 解析上一个模块层数/剩余时间这类动态参数

所以可以把它视为：

- **battle 配表参数替换系统的统一入口**

### 和 `getData()` 的区别

两者很容易混：

- `checkParam()`
  - 解析**单个参数**
  - 输入一个 `int32`
  - 输出一个 `float64`

- `getData()`
  - 解析**整段表达式**
  - 输入一个 `[]int32`
  - 内部会反复调用 `checkParam()`

也就是说：

- `checkParam()` 是底层的“单参数解释器”
- `getData()` 是上层的“公式解释器”

### 总结

`BattleEntryModel.checkParam()` 的本质是：

- 先用 `checkParamRaw()` 把一个 battle 参数 sign 解析成运行时值
- 再用 `ChangeModelParamValue()` 检查是否需要被额外模块修正
- 最终返回一个 battle 逻辑真正使用的参数值

一句话记法：

- `checkParamRaw()`：这个参数原本代表什么
- `ChangeModelParamValue()`：这个参数结果还要不要再被改一次

---

## Q20: entryTypeChangeModSkillData2 (10150) 有什么用

### 基本作用

`10150` 的作用是：

- **把当前模块绑定的 `skiData` 改成另一条技能数据**
- 并且这个目标 `skillId` 不是写死取 `Data[0]`，而是先走 `checkParam(Data[0])`
- 所以它支持 `-91/-92/-93` 这类动态技能参数

定义在 [battle_entry_type.go](/Users/gexianglin/zserver/internal/battle/battle_entry_type.go#L1451)：

```go
entryTypeChangeModSkillData2 BattleEntryType = 10150 // 修改模块的skillData skillId 通过 checkParam(data[0]) 获取(支持-91/-92等技能参数)
```

### 执行位置

- 分发入口在 [battle_entry_action.go](/Users/gexianglin/zserver/internal/battle/battle_entry_action.go#L1244)
- 实际实现函数在 [battle_entry_action_10000.go](/Users/gexianglin/zserver/internal/battle/battle_entry_action_10000.go#L172)

```go
func actionChangeModSkiDataByCheckParam(mod *BattleEntryModel) {
    target := mod.getSource()
    dataArr := mod.getConf().Data
    _, sId := mod.checkParam(dataArr[0])
    skillId := uint32(sId)
    mod.setOldSkiDataKind()
    entryKind := mod.getSkiData().kind
    if len(dataArr) > 1 {
        entryKind = BattleEntryKind(dataArr[1])
    }
    skiData := newNoneEntry(target, entryKind, skillId)
    if skiData == nil {
        return
    }
    mod.setSkiData(skiData)
}
```

### 它到底改了什么

它改的是当前 `BattleEntryModel` 的：

- `mod.skiData`

不是：

- 直接放技能
- 直接改角色技能栏
- 直接触发伤害

而是让“这个模块后续在取技能上下文时”，改为使用新的技能。

这通常会影响后续所有依赖 `mod.getSkiData()` 的逻辑，例如：

- `checkParam(-91/-92/-93...)`
- `getData()` 里依赖技能参数的取值
- 技能归属类型 `entryKind`
- 某些按技能配置取倍率、元素、冷却、预警时间的行为

### Data 参数含义

#### `Data[0]`

- 目标 `skillId`
- 但不是简单常量，而是走：

```go
_, sId := mod.checkParam(dataArr[0])
```

所以这里可以写：

- 普通技能 ID
- `-91/-92/-93`
- 其他能通过 `checkParam()` 解析出来的动态值

#### `Data[1]`（可选）

- `entryKind`
- 如果不写，就沿用当前 `mod.getSkiData().kind`
- 如果写了，就强制把新建的技能上下文归属到指定 `BattleEntryKind`

代码是：

```go
entryKind := mod.getSkiData().kind
if len(dataArr) > 1 {
    entryKind = BattleEntryKind(dataArr[1])
}
```

### 它是怎么生效的

流程很短：

1. 先从 `Data[0]` 通过 `checkParam()` 算出目标 `skillId`
2. 记录旧的 `skiData.kind`
   - `mod.setOldSkiDataKind()`
3. 用 `newNoneEntry(target, entryKind, skillId)` 构造一份新的“空技能上下文”
4. 把当前模块的 `skiData` 替换成这份新数据

注意这里的：

```go
target := mod.getSource()
```

说明新技能上下文是挂在 **当前模块 source** 身上的，不是 target。

### 为什么需要这个 type

它的价值在于：

- 当前模块本来是由 A 技能触发出来的
- 但后续某段逻辑，希望它“按 B 技能的参数/归属/配置”继续往下算

这时候就不能只看原始 `skiData`，而是需要临时把模块上下文切换成另一条技能。

所以 `10150` 更像一个：

- **技能上下文切换器**

而不是直接效果模块。

### 和旧版“改技能数据”有什么区别

同类还有一条老逻辑在 [battle_entry_action_10000.go](/Users/gexianglin/zserver/internal/battle/battle_entry_action_10000.go#L150) 上面那段：

- 旧版是直接按固定 `skillId` 或技能组去改
- `10150` 的特点是：
  - `skillId` 通过 `checkParam(Data[0])` 动态取
  - 明确支持 `-91/-92` 这类技能参数驱动

所以它更适合：

- 技能 ID 不是写死
- 而是由当前技能参数、事件链、上游模块动态决定

### 一个真实样例

我在配置里扫到的一个样例是：

- [skill_entry_model_monster5.json](/Users/gexianglin/zserver/etc/game/data/ConfigTW/skill_entry_model_monster5.json)
  - `93006631`
  - `typ = 10150`
  - `data = [-94]`

这说明这条配置的意思就是：

- 把当前模块的 `skiData` 改成“技能参数4 对应的 skillId”

也就是：

- 不是固定切到某条技能
- 而是“切到当前技能参数4 指向的那条技能”

### 一句话总结

`10150` 的本质用途是：

- **动态切换当前模块的技能上下文**

最常见的理解方式是：

- 后续这段逻辑不要再按原技能算
- 改成按 `Data[0]` 解析出来的那条技能继续算

---



---


---

## Q26: is_clear_model 这个字段详细解释

### 字段定义

`is_clear_model` 对应配置结构里的：

- [skill_entry_model.go](/Users/gexianglin/zserver/internal/data/skill_entry_model.go)

```go
IsClearMod []uint32 `json:"is_clear_model"`
```

它不是一个单独的布尔值，而是一个 **长度至少为 3 的数组开关**。  
在 battle 代码里目前固定按下标读取三位：

- `IsClearMod[0]`
- `IsClearMod[1]`
- `IsClearMod[2]`

对应方法在：

- [battle_entry_model.go](/Users/gexianglin/zserver/internal/battle/battle_entry_model.go)

```go
func (mod *BattleEntryModel) isMustClearMod() bool {
	return mod.getConf().IsClearMod[0] == 1
}

func (mod *BattleEntryModel) isDieClearMod() bool {
	return mod.getConf().IsClearMod[1] == 1
}

func (mod *BattleEntryModel) isBattleEndClearMod() bool {
	return mod.getConf().IsClearMod[2] == 1
}
```

### 三个下标分别是什么意思

#### `is_clear_model[0]`

表示：

- **当同一技能链 / startModel 流程清理旧模块时，这个模块要不要被一起清掉**

对应名字是：

- `isMustClearMod()`

实际触发位置在：

- [battle_entry.go](/Users/gexianglin/zserver/internal/battle/battle_entry.go)

```go
if v.conf.ID == awakeModelID ||
	(v.isStateRun() && v.isMustClearMod() &&
		(source == v.getTarget() || (source.isKindPet() && source == v.source))) {
	isDestroy = true
}
```

也就是：

- 当一个角色重新启动一套模块链、切技能、切形态、重新挂同类机制时
- 之前已经存在的、且 `is_clear_model[0] = 1` 的模块
- 会被标记为 `destroy`

这类开关通常适合：

- 某个阶段性状态只能同时存在一份
- 新一轮开始时必须把旧状态先清掉
- 旧模块不能跨阶段保留

#### `is_clear_model[1]`

表示：

- **角色死亡时，这个模块要不要被清掉**

对应名字是：

- `isDieClearMod()`

实际触发位置在：

- [battle_entry.go](/Users/gexianglin/zserver/internal/battle/battle_entry.go)

```go
if mod.getTarget() == role {
	if mod.isBuffOrDebuff() || mod.isDieClearMod() || (mod.getTarget().isKindSummon()) && !mod.isMustShowBuffKind() {
		isDel = true
	}
}
```

这里要注意：

- 普通 `buff/debuff` 本来就常常会在死亡时被清
- `is_clear_model[1] = 1` 的意义更像是：
  - **就算这个模块本身不是普通 buff/debuff，也要求它在死亡时清掉**

适合的场景一般是：

- 某个只对活体有意义的机制标记
- 某个角色死亡后不能保留到复活的状态
- 某些逻辑模块虽然不是典型 buff，但死亡后必须失效

#### `is_clear_model[2]`

表示：

- **战斗结束时，这个模块要不要被统一清掉**

对应名字是：

- `isBattleEndClearMod()`

实际触发位置在：

- [battle_entry.go](/Users/gexianglin/zserver/internal/battle/battle_entry.go)

```go
for _, v := range modelArr {
	if v.isStateRun() && v.isBattleEndClearMod() {
		v.setStateDestroy()
	}
}
```

对应流程是：

- 战斗结束
- 遍历当前还在运行的模块
- 对 `is_clear_model[2] = 1` 的模块统一标记销毁

适合的场景一般是：

- 仅在本场战斗内有效的状态
- 战斗结算后不能残留到下一场的逻辑模块
- 某些房间机制、临时效果、UI 同步模块

### 它和 `clear()` / `next` 的关系

`is_clear_model` 决定的是：

- **这个模块在什么时机会被系统要求清除**

但模块真正被清掉后，是否继续走 `next`，还要看：

- [skill_entry_model.go](/Users/gexianglin/zserver/internal/data/skill_entry_model.go) 里的 `is_clear_not_next`
- [battle_entry_model.go](/Users/gexianglin/zserver/internal/battle/battle_entry_model.go) 里的 `isClearAddNext()`

也就是说：

- `is_clear_model` 管“什么时候该清”
- `is_clear_not_next` / `clear()` 相关逻辑管“清掉之后 next 还走不走”

这两者不是一回事。

### 一个很重要的实现细节

代码里直接按：

- `IsClearMod[0]`
- `IsClearMod[1]`
- `IsClearMod[2]`

取值，没有额外长度保护。  
所以从实现约束上说，配表时最好把 `is_clear_model` 始终配成 3 位，例如：

```json
"is_clear_model": [0, 1, 1]
```

不要配成长度不足的数组，否则理论上有越界风险。

### 配表可以这样记

你可以把它直接记成：

- 第 1 位：新流程启动/同类替换时要不要清
- 第 2 位：死亡时要不要清
- 第 3 位：战斗结束时要不要清

例如：

- `[1,0,0]`
  - 新一轮覆盖时清
  - 死亡不特意清
  - 战斗结束不特意清

- `[0,1,0]`
  - 死亡时清

- `[0,0,1]`
  - 战斗结束时清

- `[1,1,1]`
  - 三种时机都清

### 一句话总结

`is_clear_model` 是一个三位时机开关：

- `第 0 位` 控制 **重新启动同类流程时是否清掉旧模块**
- `第 1 位` 控制 **目标死亡时是否清掉模块**
- `第 2 位` 控制 **战斗结束时是否清掉模块**

它决定“什么时候该被系统清掉”，不直接决定“清掉后 next 是否继续执行”。


## Q29: modelKindAction、modelKindEvent、modelKindCheck 三者区别

### 问题

`modelKindAction`、`modelKindEvent`、`modelKindCheck` 这 3 个有什么区别？

### 回答

这三个都是 `BattleEntryModelKind`，定义在 `/Users/gexianglin/zserver/internal/battle/battle_entry_type.go`：

```go
const (
    modelKindAction       BattleEntryModelKind = iota + 1 // 1 行为
    modelKindEvent                                        // 2 事件中
    modelKindDamageBefore                                 // 3 伤害前
    modelKindDamageAfter                                  // 4 伤害后
    modelKindCheck                                        // 5 事件判断
    maxModelKind
)
```

模块创建时会从配置表的 `kind` 字段写入：

```go
mod.setKind(BattleEntryModelKind(conf.Kind))
```

也就是说配置里的 `"kind": 1/2/5` 决定了这个模块后续怎么运行。

### 一句话区别

- `modelKindAction = 1`：主动执行行为。模块被添加后立即/按间隔执行 `runAction(mod)`，用于真正做效果，例如伤害、召唤、记录、改层数、发战报。
- `modelKindEvent = 2`：事件监听器。模块本身挂在角色身上等待事件，事件发生时先判断 `isPassEvent`，通过后触发自己的 `trigger`。
- `modelKindCheck = 5`：定时事件判断器。模块自己按 `time` 周期跑，但跑的不是普通 action，而是调用事件判断逻辑 `triggerEventModel`，通过则走 `trigger`，不通过可走 `not_pass_trigger`。

### modelKindAction：行为模块

`Action` 是最常见的执行模块。模块添加时会进入 `BattleEntryModel.start()`，随后 `run()` 判断是 `modelKindAction`，就调用：

```go
runAction(mod)
mod.setLastRunTime(runTime)
mod.addRunCount()
```

相关代码在 `/Users/gexianglin/zserver/internal/battle/battle_entry_model.go`。

典型用途：

- 造成伤害：`typ=97`、`typ=100`
- 召唤：`typ=825`
- 记录目标：`typ=20`
- 改层数：`typ=10013`
- 添加/清除/刷新各种战斗状态

配置表现：

```json
"kind": 1,
"typ": 825
```

意思是：这是一个行为模块，执行时会走 `typ=825` 对应的召唤 action。

### modelKindEvent：事件模块

`Event` 模块不是添加后立即执行效果，而是作为监听器存在。事件发生时，系统通过：

```go
eve.triggerEvent(typ, modelKindEvent, source, target, eventData)
```

去找 `typ + kind` 匹配的模块：

```go
for _, v := range eve.ent.getModelArrByTypeAndKind(typ, kind) {
    if v.isStateRun() && v.getTarget() == source {
        eve.triggerEventModel(v, source, target, eventData)
    }
}
```

然后在 `triggerEventModel` 里做事件条件判断：

```go
isPassEvent := eve.isPassEvent(mod, eventData)
if isPassEvent {
    mod.setEventData(eventData)
    mod.addTrigger()
    mod.addRunCount()
} else if len(mod.conf.NotPassTrigger) != 0 {
    mod.setEventData(eventData)
    mod.addTriggerNotPass()
    mod.addRunCount()
}
```

典型用途：

- 战斗开始、战斗结束
- 释放技能时触发
- 受到伤害、造成伤害时触发
- 召唤物死亡时触发
- buff 添加、刷新、清除时触发

例如之前看过的：

```json
"kind": 2,
"typ": 20094,
"data": [5073204, 1],
"trigger": [93007729]
```

含义是：这是一个事件监听模块，监听“召唤物死亡类型”。当死亡的召唤物 ID 是 `5073204` 且死亡类型是 `1`，事件判断通过，触发 `93007729`。

### modelKindCheck：判断模块

`Check` 容易和 `Event` 混淆。它也会走 `triggerEventModel` 和 `isPassEvent`，但它不是等外部事件来触发，而是在自己的 `run()` 中主动周期性做一次事件判断：

```go
if isKindCheck {
    ent.getEve().triggerEventModel(mod, mod.getTarget(), mod.getSource(), nil)
}
```

所以 `Check` 更像“定时判定器”或“条件门”：

- 条件通过：走 `trigger`
- 条件不通过：如果配置了 `not_pass_trigger`，走 `not_pass_trigger`
- 可以配合 `time` 做持续检测

配置表现常见如下：

```json
"kind": 5,
"typ": 30000,
"data": [93005557],
"not_pass_trigger": [93005021]
```

含义是：这个模块不是普通 action，而是周期性执行 `typ=30000` 对应的事件判断逻辑。判断通过时走 `trigger`，判断失败时可走 `not_pass_trigger`。

代码里还专门防止 `Check` 写成死循环：

```go
if isKindCheck && len(mod.conf.NotPassTrigger) == 0 {
    printError(... "non-action model should not be executed in a loop" ...)
    break
}
```

这说明 `Check` 的设计预期就是“判断并分流”，尤其常搭配 `not_pass_trigger` 使用。

### 对比表

| Kind | 名称 | 触发方式 | 执行入口 | 主要用途 |
|---|---|---|---|---|
| `1` | `modelKindAction` | 添加模块后立即/按间隔执行 | `runAction(mod)` | 真正做行为：伤害、召唤、记录、改值、发日志 |
| `2` | `modelKindEvent` | 外部事件发生时触发 | `triggerEvent(..., modelKindEvent, ...)` → `isPassEvent` | 监听事件，通过后触发后续模块 |
| `5` | `modelKindCheck` | 自己按 `time` 周期主动判断 | `run()` → `triggerEventModel()` → `isPassEvent` | 定时条件判断，通过/不通过分别分流 |

### 配置选择建议

- 需要“马上执行某个效果”：用 `kind=1`。
- 需要“等某个战斗事件发生再触发”：用 `kind=2`。
- 需要“周期性检测某个条件是否满足，并按结果分支”：用 `kind=5`。

一个常见链路是：

```text
Action 先挂一个 Event 或 Check
Event 等外部事件发生
Check 自己定时判断条件
判断通过后再触发 Action 做真正效果
```

因此不要只看 `typ`，同一个 `typ` 在不同 `kind` 下含义会变：`kind` 决定“什么时候跑、由谁触发”，`typ` 决定“跑什么逻辑或判断什么事件”。
