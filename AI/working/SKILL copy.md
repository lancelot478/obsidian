---
name: skill-entry-model
description: Use when configuring game skill modules via the EntryModel Excel table. Understands the table column meanings, module chaining logic, and generates CSV output directly copyable into Excel.
---

# Skill Entry Model — 技能模块表配置指南

## 核心原则

**本文档提供框架性理解，具体 typ 的字段语义以代码为准。**

遇到不确定的情况，必须查阅以下代码：
- `internal/battle/battle_entry_action.go` — `runAction()` 函数：kind=1 的行为分发
- `internal/battle/battle_entry_event.go` — `isPassEvent()` 函数：kind=2/3/4/5 的事件判定
- `internal/battle/battle_entry_condition.go` — 条件类型定义和检查逻辑
- `internal/battle/battle_entry_type.go` — 所有 typ 常量定义及注释
- `internal/battle/battle_entry_target.go` — target 字段各索引含义（`BattleEntryTargetTypeKind`、`BattleEntryTargetOrderKind`、`BattleEntryTargetFilterKind`）
- `internal/battle/battle_entry_data.go` — data 字段函数类型（`BattleEntryDataFun`）：buff/条件加成类 typ 的 data[0] 含义
- `internal/battle/battle_entry_time.go` — time 字段含义
- `internal/data/skill_entry_model.go` — `EntryModel` 结构体字段定义
- `etc/game/data/ConfigCN/skill_entry_model_*.json` — 大量真实配置示例参考

**字段的实际语义可能因 typ 不同而完全不同**（如 `dataCondition` 在某些 typ 下不是条件而是存储临时数据），务必以代码逻辑为准。

---

## 前置确认（处理任何需求前必须执行）

**在开始配置前，必须明确以下信息：**

1. **技能Excel路径**（必须）：用于读取所有技能的 desc/value 字段作为需求来源。使用 openpyxl 读取，怪物技能表路径通常为 `j技能相关/g怪物技能.xlsx`，表头行为第2行（index=1），id字段为第1列。

2. **技能组ID范围**（必须）：明确本次要处理哪些技能组ID（如 961601~961608），用于从Excel中筛选对应行。

3. **输出方式**（可选，有默认值）：
   - **默认**：写入项目根目录 `./skill-model/<技能组ID>.csv`（若目录不存在则创建）
   - **可选**：直接在对话中输出CSV内容
   - 若用户未说明，使用默认方式，无需额外询问

**Excel读取代码模板：**
```python
import openpyxl, sys
sys.stdout.reconfigure(encoding='utf-8')
wb = openpyxl.load_workbook(r'<路径>', read_only=True, data_only=True)
ws = wb.worksheets[0]
headers = None
results = []
for i, row in enumerate(ws.iter_rows(values_only=True)):
    if i == 1:
        headers = list(row)
    if i < 2:
        continue
    val = row[0]
    if val is not None and isinstance(val, (int, float)) and <id_min> <= int(val) <= <id_max>:
        results.append(dict(zip(headers, row)))
```

---

## 工作流程

### 阶段一：需求分析（批量，先于任何配置）

1. **读取Excel**：用 openpyxl 读取所有目标技能行，提取 id/name/desc/value1~7 等关键字段
2. **整体理解**：对所有技能的描述统一分析，识别技能间的交互关系（如共享状态、互相触发）
3. **主动提问**：遇到不理解的描述立即列出问题，**不猜测**；可要求参考已有实现（如"是否参考 96XXXX 的实现？"）
4. **分类汇总**：将所有技能分为两类并告知用户：
   - **A类 — 可直接生成CSV**：所需 typ/handler 已存在于代码中，entry model 配置完整可用
   - **B类 — 需先占位+服务端实现**：依赖不存在的 typ/事件/handler，需新增服务端代码后才能完整配置
5. **等待用户确认**：用户确认分类和处理顺序后再进入阶段二

### 阶段二：逐技能配置

对每个技能按以下步骤执行：

6. **查阅同类示例**：从 `skill_entry_model_*.json` 中找相似技能参考
7. **查代码确认行为**：对不熟悉的 typ，用 `runAction`/`isPassEvent` 定位具体实现
8. **处理缺失实现**：若所需实现不存在，**不自行决定**：
   - 描述现状 + 列出可选方案 + **等待用户决策**
9. **规划模块结构**：启动模块 → 事件监听 → 判断 → 行为的链式结构
10. **分配模块ID**：根据技能组ID分配模块ID段（见ID分配规则）
11. **逐行填写**：按列定义填写字段，空字段留白
12. **输出CSV**：含完整表头；按确认的输出方式执行（写文件或直接输出）

---

## CSV 表头（必须完整输出，字段顺序固定）

```
id,remarks,time,target,kind,typ,dataCondition,data,triggerChance,trigger,nextChance,next,refresh,buff,effect,buff_icon,hide_buff_time,shader_type,layer_default,layer_max,cool,use_num,is_clear_model,bind,elseTrigChance,elseTrig,refresh_action,isRapid,not_pass_trigger,is_clear_not_next,is_sync_move_action_model
```

---

## 数组格式规则（Excel填写）

| 维度 | 分隔符 | 示例 | 含义 |
|------|--------|------|------|
| 一维数组 | `,` | `100001,100002` | trigger两个模块 |
| 二维数组 | 组内`,`，组间`@` | `1,0@2,1` | `[[1,0],[2,1]]` |
| 空 | 留白 | | 不填该字段 |

---

## 列详解

### id — 模块ID（整数，唯一）

**ID分配规则**：

技能组ID为 `97953` 时：
- `9795301~9795310`：**启动模块**，技能释放瞬间激活，通常是长期存在的事件监听器
- `9795311`起：**后续模块**，由启动模块通过 trigger/next/bind 串联激活

---

### remarks — 备注
人类可读注释，不影响逻辑。JSON字段名为 `remarks`。

---

### time — 时间 `[duration_ms, num, rate_ms, delay]`（必填4个值）

| 索引 | 字段 | 含义 | 说明 |
|------|------|------|------|
| [0] | duration | 持续时间(ms) | `-1`=永久 / `0`=立即结束 / 负数=ModelSign引用 |
| [1] | num | 执行次数 | `-1`=无限 / `1`=执行一次 |
| [2] | rate | 执行间隔(ms) | `0`=无间隔 / 正数=循环间隔 |
| [3] | delay | 是否延迟 | `0`=不延迟 / `1`=延迟一个间隔再首次执行 |

**常用组合：**
| time值 | 用途 |
|--------|------|
| `0,1,0,0` | 立刻执行一次（one-shot） |
| `-1,-1,0,0` | 永久存在无间隔（被动监听）|
| `-1,-1,3000,0` | 永久存在每3秒执行 |
| `-96,1,0,0` | 持续时间=技能参数6，执行一次（buff持续）|
| `-1,1,0,0` | 永久存在执行一次（常用于事件触发器）|

---

### target — 目标 `[targetType, data, order, num, filter]`

定义见 `internal/battle/battle_entry_target.go`。

| 索引 | 字段 | Go类型 | 含义 |
|------|------|--------|------|
| [0] | targetType | `BattleEntryTargetTypeKind` | 目标类型（必填）|
| [1] | data | int32 | 附加数据（范围类型时=范围半径米数，支持 ModelSign 负数引用技能参数）|
| [2] | order | `BattleEntryTargetOrderKind` | 排序/选择方式 |
| [3] | num | int32 | 选取数量（0=全部；支持 ModelSign 负数，如 -91~-97 引用技能参数）|
| [4] | filter | `BattleEntryTargetFilterKind` | 过滤条件（目前仅 `1`=过滤上个模块新模块的目标）|

**targetType 常用值（`BattleEntryTargetTypeKind`）：**
| 值 | 含义 |
|----|------|
| 1 | 自身 |
| 2 | 己方队伍（含自身）|
| 3 | 敌方队伍 |
| 4 | 当前目标 |
| 5 | 上个模块的目标 |
| 6 | 事件来源 |
| 9 | 事件目标 |
| 12 | 自身半径内敌方（data=半径米数）|
| 15 | 己方（不含自身）|
| 23 | 自己的召唤物 |
| 50 | 上个记录模块的最后一个目标 |

完整类型列表见 `battle_entry_target.go` 中 `BattleEntryTargetTypeKind` 常量块（100+个）。

**order 排序方式（`BattleEntryTargetOrderKind`）：**
| 值 | 含义 |
|----|------|
| 2 | 距离最近 |
| 3 | 距离最远 |
| 4 | 当前目标 |
| 7 | 血量最低% |
| 8 | 血量最高% |
| 15 | 随机 |
| 20 | 第一位是我的当前目标 |
| 22 | 最后一位是上个模块的目标 |
| 23 | 随机（排除当前目标）|

完整列表见 `battle_entry_target.go` 中 `BattleEntryTargetOrderKind` 常量块。

**target 写法示例：**
| 填写 | 含义 |
|------|------|
| `1` | 自身 |
| `3` | 所有敌方 |
| `3,0,2,1` | 敌方中距离最近1个 |
| `3,0,15,3` | 敌方中随机3个 |
| `12,5` | 自身5米内全部敌方 |
| `12,10,2,1` | 自身10米内距离最近敌方1个 |
| `12,-92` | 自身内（半径=技能参数2）全部敌方 |
| `3,0,2,-91` | 敌方中距离最近 N 个（N=技能参数1）|

---

### kind 与 typ 的关系——核心机制

> **重要**：`kind` 和 `typ` 是两个独立维度，必须分开理解。

**`kind` 决定模块"何时被激活"（激活机制）：**

| 值 | 名称 | 激活时机 | 内部调用 |
|----|------|---------|---------|
| 1 | 行为 | 由时间系统驱动（time 控制频率），激活后执行 `runAction(typ)` | `runAction()` |
| 2 | 事件 | `triggerEvent(typ, modelKindEvent, source, ...)` 被调用时，本模块若 typ 和 target 匹配且 `isPassEvent()` 通过，则执行 trigger | `triggerEvent()` |
| 3 | 伤害前 | 伤害结算前，系统调用 `checkEventDamageBefore()` 触发，用 typ 匹配 | `triggerEvent(typ, modelKindDamageBefore, ...)` |
| 4 | 伤害后 | 伤害结算后，系统调用 `checkEventDamageAfter()` 触发，用 typ 匹配 | `triggerEvent(typ, modelKindDamageAfter, ...)` |
| 5 | 判断 | 被其他模块的 trigger 激活后，执行条件检查 | `isPassEvent()` 决定走 trigger 还是 not_pass_trigger |

**`typ` 决定模块"做什么/监听什么"（行为/事件类型）：**

- kind=1 时：`typ` 决定 `runAction` 执行哪个 action 函数
- kind=2/3/4/5 时：`typ` 决定监听哪个事件，`isPassEvent()` 用 typ 找到对应的条件检查函数

**kind=4 typ=300 的正确解读：**
- 这是一个**事件监听器**，监听"伤害后"时机的 typ=300（entryTypeFinDamageMake）事件
- 激活时不执行任何 action，而是通过 `dataCondition` 过滤，条件满足则激活 `trigger` 列表的模块
- 实际效果由 trigger 指向的 kind=1 模块执行

---

### kind=1 的 typ 分类（runAction 内部逻辑）

`runAction` 对 typ 的处理分为四档：

**① `buffEntryActions`（纯属性修改器）**  
typ = 200(最大血量)、201(攻击力)、202(防御力)、203(攻速%)...等所有基础/特殊属性 typ  
→ 模块存在期间，角色属性计算时自动读取此模块的值，调用 `actionConditionDataFun`  
→ `dataCondition` 用于限制在何种条件下此buff值生效

**② `conditionBuffEntryActions`（条件性加成修改器）**  
typ = 300(最终伤害%)、301(受最终伤害%)、305、306、310...等伤害/治疗加成 typ  
→ 同样调用 `actionConditionDataFun`，在伤害结算时被查询是否满足条件  
→ `dataCondition` 用于限制在何种条件下此加成生效（如"仅对暴击伤害有效"）

**③ `emptyEntryActions`（无行为，仅作为状态标记）**  
→ 模块存在但不执行任何代码，仅作为"是否存在该状态"的标记供其他模块查询

**④ 其他 typ（有具体 action 函数）**  
→ switch-case 分发到具体函数：造成伤害、治疗、释放技能、移动等

---

### typ 常用值参考

**kind=1 属性修改器类（buffEntryActions，模块存在期间持续生效）：**
| typ | 说明 | data格式 |
|-----|------|---------|
| 200 | 最大血量+- | `1,-91` |
| 201 | 攻击力+- | `1,-91` |
| 202 | 防御力+- | `1,-91` |
| 203 | 攻速% | `1,-91` |
| 204 | 暴击率% | `1,-91` |
| 205 | 暴击伤害% | `1,-91` |
| 206 | 技巧值 | `1,-91` |
| 209 | 造成攻击力% | `1,-91` |
| 219 | 急速值 | `1,-91` |
| 250 | 全元素% | `1,-91` |
| 251 | 每秒回血 | `值` |
| 253 | 移速 | `1,-91` |
| 254 | 移速% | `1,-91` |

**kind=1 条件加成类（conditionBuffEntryActions，结算时按条件生效）：**
| typ | 说明 | data格式 |
|-----|------|---------|
| 300 | 造成最终伤害% 加算 | `1,-91` |
| 301 | 受到最终伤害% | `1,-91` |
| 305 | 造成最终伤害% 额外加算 | `1,-91` |
| 306 | 受到最终伤害% 乘算 | `1,-91` |
| 310 | 造成治疗效果% | `1,-91` |
| 311 | 受到治疗效果% | `1,-91` |
| 332 | 指定技能冷却% | `技能组ID,-91` |

**kind=1 执行类（有具体 action 函数）：**
| typ | 说明 | data格式 |
|-----|------|---------|
| 0 | 直接触发trigger，无具体行为 | 无 |
| 1 | 时间等待后触发next | 无 |
| 100 | 造成伤害 | `元素,倍率系数,...,-91` |
| 101 | 造成治疗（倍率） | `0,-91`不乘攻 / `1,-91`乘攻 |
| 258 | 技能冷却+- | `技能组ID,0,倍率,值` |
| 265 | 恢复怒气 | `值` |
| 267 | 能量变更% | `倍率,-91` |
| 337 | 释放技能 | `技能组ID` |
| 1844 | 给指定模块加层数 | `模块ID` |
| 1845 | 设置指定模块层数 | `模块ID,层数,1` |

**kind=2 事件监听类（typ=20xxx，监听具体游戏事件）：**
| typ | 说明 | data/dataCondition 附加限制 |
|-----|------|---------------------------|
| 20003 | 战斗开始 | data可指定队伍类型 |
| 20004 | 战斗结束 | 无 |
| 20006 | 目标死亡 | 无 |
| 20013 | 我释放技能时 | dataCondition 可限定技能类型(43) |
| 20015 | 释放技能后 | 同上 |
| 20018 | 血量低于某% | 无（事件本身带条件） |
| 20020 | 角色死亡 | 无 |
| 20023 | 造成buff时 | 无 |
| 20024 | 受到buff时 | 无 |
| 20041 | 护盾破碎 | 无 |
| 20042 | 净化buff时 | 无 |
| 20062 | 战斗中敌方死亡 | 无 |

**kind=3/4 伤害事件监听类（配合 dataCondition 过滤）：**
| typ | 说明 | 典型 dataCondition |
|-----|------|-------------------|
| 300 | 造成伤害时（伤害前/后均可用）| `1,2`=核心技能 / `2,1`=暴击 |
| 301 | 受到伤害时 | 同上 |
| 310 | 造成治疗时 | 无 |
| 311 | 受到治疗时 | 无 |

**kind=5 判断类（检查条件，走不同分支）：**
| typ | 说明 | data格式 |
|-----|------|---------|
| 30000 | 目标是否存在指定模块ID | `模块ID` |
| 30001 | 是否存在指定buff | `buffID` |
| 30003 | 目标模块层数是否达到N | `模块ID,层数,...` |
| 30007 | 是否已获得指定技能 | `技能组ID` |
| 30009 | 是否有技能在冷却 | `技能组ID` |
| 30012 | 能量是否满足 | `0大于/1小于,值` |

---

### dataCondition — 数据条件（注意：并非总是条件）

> **重要**：`dataCondition` 字段在大多数 typ 下用于条件检查，但某些特殊 typ 会将其用于存储其他数据（临时状态、参数等）。使用不熟悉的 typ 时，**必须查阅对应的 action 或 event 代码**确认其实际含义。

标准条件格式：`[[type,p1,p2,...], ...]`，多个条件组之间是 **AND 关系**。  
Excel填写：`1,0@2,1` = `[[1,0],[2,1]]`

条件类型完整定义见 `internal/battle/battle_entry_condition.go` 开头常量块。

**高频条件参考（以代码为准，以下为常见用法）：**
| 类型 | 说明 | 常见参数 |
|------|------|---------|
| 1 | 伤害类型 | `1,0`=普攻/`1,1`=技能/`1,2`=核心/`1,4`=战术 |
| 2 | 是否暴击 | `2,1`=是暴击 |
| 16 | 目标血量% | `16,50,0`=目标血量<50% |
| 21 | 空白条件（总是满足）| `21` |
| 24 | 来源存在指定模块 | `24,模块ID` |
| 28 | 目标存在指定模块 | `28,模块ID,层数,1` |
| 33 | 非某伤害类型 | `33,0`=非普攻 |
| 34 | 来源血量% | `34,0,50`=来源血量<50% |
| 36 | 是否是锁定目标 | `36,1` |
| 43 | 技能类型 | `43,1`=核心/`43,2`=战术 |
| 45 | 角色存在指定模块 | `45,模块ID` |
| 51 | 来源不存在模块 | `51,模块ID` |
| 200 | 伤害标签 | `200,102`=召唤物伤害 |

若条件类型不在上述列表，查 `battle_entry_condition.go` 中对应常量。

---

### data — 数据
行为参数，具体含义由 typ 决定。

**buffEntryActions / conditionBuffEntryActions 类 typ 的 data 格式：**  
`[BattleEntryDataFun, param1, param2, ...]`  
第一个元素是函数类型（`BattleEntryDataFun`，定义在 `internal/battle/battle_entry_data.go`），决定如何用后续参数计算最终值。常用值：
| BattleEntryDataFun | 含义 |
|--------------------|------|
| 1 | 直接取值（default）|
| 2 | 取负值（negative）|
| 3 | × 0.01（百分比）|
| 4 | 攻击力 × param × 0.01 |
| 25 | 基础值 + 层数 × 增量（`a1 + layer * a2`）|

其他 typ 的 data 格式完全由对应 action 函数决定，**必须查代码**。

data 数组各元素支持以下负数 ModelSign（`BattleEntryModelSign`，定义在 `battle_entry_type.go`）：

| 负数值 | 含义 |
|-------|------|
| -91 | 技能参数1（val1）|
| -92 | 技能参数2（val2）|
| -93 | 技能参数3（val3）|
| -94 | 技能参数4（val4）|
| -95 | 技能参数5（val5）|
| -96 | 技能参数6（val6）|
| -97 | 技能参数7，固定伤害 |
| -98 | 技能冷却时间 |
| -90 | 吟唱时间 |
| -200 | 引导时间 |
| -204 | 当前模块层数 |

---

### triggerChance — 事件概率%
`0~100` 整数，也可填 ModelSign（如 `-93`=技能参数3作为概率）。

100 = 必触发；0 = 不触发（直接走 elseTrig）。

---

### trigger — 事件触发
逗号分隔的模块ID列表。`triggerChance` 命中时激活这些模块。

---

### nextChance — 结束概率%
模块结束（时间/次数耗尽）时激活 next 的概率，`0~100`。

---

### next — 结束模块
模块生命周期结束时触发的模块ID列表。

---

### refresh — 刷新策略

Go类型：`BattleEntryModelRefreshType`，定义在 `internal/battle/battle_entry_model.go`。

| 值 | 说明 |
|----|------|
| 0 | 不刷新（允许多实例并存，不重置时间）|
| 1 | 正常刷新（重置时间，只保留一个实例）|
| 2 | 按类型+目标刷新 |
| 3 | 不刷新但累加 val 值 |
| 4 | 按来源+类型刷新 |
| 5 | 按来源+类型刷新（变体）|
| 6 | 正常刷新但不重置运行时 |
| 7 | 按类型+目标+ID刷新 |

---

### buff / effect / buff_icon / hide_buff_time / shader_type

`buff` 字段对应 Go类型 `BattleEntryModelBuff`，定义在 `internal/battle/battle_entry_type.go`。

| 字段 | 说明 |
|------|------|
| buff | 见下表 |
| effect | 特效资源ID（`0`=无特效）|
| buff_icon | buff图标ID（`0`=无图标，有值则客户端显示图标）|
| hide_buff_time | `1`=隐藏buff倒计时显示 |
| shader_type | 角色着色器类型，通常留空 |

**buff 值（`BattleEntryModelBuff`）：**
| 值 | 含义 |
|----|------|
| -1 | 全部（all） |
| 0 | 无buff标记 |
| 1 | 正面buff |
| 2 | 负面buff（debuff）|
| 3 | 显示buff特效 |
| 4 | 必定显示buff特效 |

---

### layer_default / layer_max — 层数

- `layer_default`：模块初始层数，通常 `1`，支持 ModelSign 负数
- `layer_max`：最大层数上限，通常与 `layer_default` 相同
- 若需要层数叠加（如叠buff），`layer_max` 设为最大叠加数

---

### cool — CD模式
`1`=本模块使用技能CD（time 中的持续时间将等于技能CD时间），否则留空。

---

### use_num — 可使用次数
模块总共能被激活的次数，超出后自动清除。`0`或空=无限。

常用：`use_num=1` 配合 `time=-1,1,0,0` = 永久存在但只能触发一次。

---

### is_clear_model — 清除模块
**必须是3个槽位**，格式：`0,0,0`。

填写要在本模块激活时清除的其他模块ID，不用的槽位填 `0`。

示例：`0,100050,100051` = 激活时清除模块100050和100051。

---

### bind — 绑定生命周期
绑定模块ID列表。本模块消失/清除时，这些模块也会被一起清除。

---

### elseTrigChance / elseTrig — 否则触发

当 `triggerChance` **概率未命中**时，以 `elseTrigChance` 概率触发 `elseTrig` 中的模块。

> **注意**：这不是"条件不满足时触发"，而是纯概率的else分支。  
> 示例：`triggerChance=50, trigger=[A], elseTrigChance=100, elseTrig=[B]` = 50%概率→A，另50%概率→B

---

### refresh_action — 刷新行为
`1`=模块刷新时重新执行一次行为（如buff刷新时立即重新应用效果）。

---

### isRapid — 受极速影响
`1`=模块的时间（duration、rate）会受角色极速属性影响（时间缩短）。

---

### not_pass_trigger — 条件不满足触发
**仅用于 kind=5（判断）**：条件不满足时触发的模块ID列表。

---

### is_clear_not_next — 移除时next行为
| 值 | 说明 |
|----|------|
| 空/0 | 正常，移除时执行next |
| 1 | 移除时不执行next（净化效果，阻断后续链）|
| 2 | 移除时执行next（反净化保证链路）|

---

### is_sync_move_action_model — 客户端UI同步
`1`=此模块同步给客户端UI显示（如位移类模块）。

---

## 模块串联逻辑

### 完整链路图

```
技能释放
    │
    ▼
【启动模块】kind=2, typ=20013 (释放技能时)
time=-1,-1,0,0  永久监听
    │ 100% trigger
    ▼
【判断模块】kind=5, typ=30000 (检查模块是否存在)
time=0,1,0,0  立刻判断一次
    ├─ 条件满足 → trigger → 【效果A】
    └─ 条件不满足 → not_pass_trigger → 【效果B】

【效果模块】kind=1, typ=305 (加成)
time=-96,1,0,0  持续=技能参数6
    │ 结束时 nextChance=100
    ▼
回到下一个监听模块 or 结束
```

### 关键规律

1. **启动模块（kind=2）**：`time=-1,-1,0,0`，永久监听，是整个技能的入口
2. **判断模块（kind=5）**：`time=0,1,0,0`，立刻判断一次，然后通过 trigger/not_pass_trigger 分发
3. **行为模块（kind=1）**：执行实际效果，time控制持续时间和频率
4. **伤害监听（kind=4）**：`time=-1,-1,0,0`，造成伤害时触发，用于被动加成

---

## 常见完整技能模式

### 模式1：被动属性加成（战斗开始后永久生效）

```csv
id,remarks,time,target,kind,typ,dataCondition,data,triggerChance,trigger,nextChance,next,refresh,buff,effect,buff_icon,hide_buff_time,shader_type,layer_default,layer_max,cool,use_num,is_clear_model,bind,elseTrigChance,elseTrig,refresh_action,isRapid,not_pass_trigger,is_clear_not_next,is_sync_move_action_model
9795301,战斗开始监听,-1,-1,0,0,1,2,20003,,100,9795310,,,,1,,,,,,,,1,1,0,0,0,,,,,
9795310,攻击力加成,-96,1,0,0,1,1,201,1,-91,,,100,9795301,1,1,,1,,1,1,0,0,0,1,0,0,,,,,
```

### 模式2：释放核心技能时触发一次额外伤害

```csv
9795301,释放技能监听,-1,-1,0,0,1,2,20013,43 1,100,9795310,,,,1,,,,,,,,1,1,0,0,0,,,,,
9795310,额外伤害,0,1,0,0,3,1,305,1,-91,,,100,9795301,1,1,,1,,1,1,0,0,0,1,0,0,,,,,
```
> 注：dataCondition 里 `43 1` 在CSV中用空格还是逗号取决于你的导入方式，实际配置是 `[[43,1]]`

### 模式3：造成伤害时概率触发效果（伤害被动）

```csv
9795301,造成伤害监听,-1,-1,0,0,1,4,300,,50,9795310,,1,,,,,,1,1,0,0,0,,,,,,,,
9795310,概率触发buff,-96,1,0,0,1,1,305,1,-91,,,,1,1,特效ID,1,,1,1,0,0,0,1,0,0,,,,,
```

### 模式4：条件判断（检查层数后分支）

```csv
9795301,战斗开始,-1,-1,0,0,1,2,20003,,100,9795310,,,,1,,,,,,,,1,1,0,0,0,,,,,
9795310,层数判断,0,1,0,0,1,5,30003,层数模块ID 3,,100,9795320,,,,1,,,,,,,,1,1,0,0,0,,9795330,,
9795320,满足条件效果,0,1,0,0,3,1,100,0,3,0,1,-91,,,100,9795301,0,,1,,1,1,0,0,0,1,0,0,,,,,
9795330,不满足时跳回,-1,1,0,0,1,5,30000,等待模块ID,,100,9795310,,,,1,,,,,,,,1,1,0,0,0,,9795301,,
```

### 模式5：循环dot效果（每隔N秒对敌人造成伤害）

```csv
9795301,战斗开始,-1,-1,0,0,1,2,20003,,100,9795310,,,,1,,,,,,,,1,1,0,0,0,,,,,
9795310,dot buff,-96,1,0,0,3,1,1,,,100,9795311,1,1,特效ID,1,,1,1,0,0,0,1,0,0,,,,,
9795311,循环伤害,-96,-1,3000,0,3,1,100,0,3,0,1,-91,,,,1,,1,1,0,0,0,1,0,0,,,,,
```

### 模式6：bind绑定生命周期

```csv
9795310,主buff,-96,1,0,0,1,1,201,1,-91,,,,1,1,图标ID,,,,1,1,0,0,0,1,0,0,9795311,,,
9795311,绑定子模块,-96,1,0,0,1,1,203,1,-92,,,,1,,,,,1,1,0,0,0,1,0,0,,,,,
```
> bind=9795311 表示 9795310 消失时自动清除 9795311

---

## 注意事项

**格式硬性要求：**
- `time` 必须填4个值（缺少会加载报错）
- `is_clear_model` 必须3个槽位，不用填 `0,0,0`
- `layer_default` 和 `layer_max` 通常配套填，普通buff填 `1,1`

**行为语义：**
- 负数 ModelSign（-91~-97）引用技能表对应参数，策划在技能表填具体数值
- 同一技能的所有模块放在同一个JSON文件（如 `skill_entry_model_job1000.json`）
- 多个 trigger 模块会**并行**激活，不是顺序执行
- kind=2/3/4 的监听模块通常配合 `time=-1,-1,0,0` 永久存在
- `dataCondition` / `data` 的具体语义**因 typ 而异**，不清楚时必须查代码

**查代码的时机：**
1. 不熟悉的 `typ` → 在 `battle_entry_type.go` 找常量注释，再在 `runAction` 或 `isPassEvent` 找实现
2. 不确定某个 typ 的 `data` 格式 → 找对应 `action*()` 函数查参数读取逻辑
3. 不确定 `dataCondition` 是条件还是临时数据 → 找对应 action 函数查字段用途
4. 参考已有配置 → 搜 `skill_entry_model_*.json` 找相同 typ 的模块样本
