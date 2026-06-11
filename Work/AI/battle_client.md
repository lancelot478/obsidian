# 战斗客户端问答记录

## 目录
- [[#Q1: AddSkillSummon 客户端怎么处理服务器发回的召唤日志并在场景中召唤]]
- [[#Q2: 地龙位移后Boss怎么跟随玩家移动旋转]]
- [[#Q3: 客户端真机AB包特效不显示怎么排查]]
- [[#Q4: HandleLogBattleEffect.OnShowLineEffect(actionInfo, info) 详细介绍]]
- [[#Q5: EffectType = ROTATE 旋转弹道特效介绍]]
- [[#Q6: PerformanceSwitching 性能测试开关介绍]]

---

## Q1: AddSkillSummon 客户端怎么处理服务器发回的召唤日志并在场景中召唤

### 处理流程

```
服务器战报 (actionKind=38 SKILL_SUMMON)
  → BattleAnalysis 分发 (BattleAnalysis.lua:388)
  → HandleLogBattle.OnSkillSummon(actionInfo, info)   (_HandleLogBattle.lua:1377)
  → BattleSceneData.AddSkillSummom(keys)              (BattleSceneData.lua:1380)
  → BattleAsset.Load() 异步加载资源                     (BattleAsset.lua:351)
  → LoadMonsterFinish() 回调                           (BattleSceneData.lua:537)
  → CheckWaitCreateRole() 每帧检查队列                  (BattleSceneData.lua:642)
  → CreateRoleObj() 创建GameObject加入场景               (BattleSceneData.lua:409)
```

### 各阶段详细说明

#### 1. 服务器战报接收与分发
- 服务器发送 actionKind=38 (SKILL_SUMMON) 的战报
- `BattleAnalysis.lua:388` 注册了处理函数，index=3 表示数据从 data[3] 开始

#### 2. 解析召唤key (`_HandleLogBattle.lua:1377`)
```lua
function HandleLogBattle.OnSkillSummon(actionInfo, info)
    local keys = {}
    local count = #info.data
    for i = actionInfo.index, count do
        table.insert(keys, info.data[i])  -- 从位置3开始提取召唤实体key
    end
    BattleSceneData.AddSkillSummom(keys)
end
```

#### 3. 核心处理逻辑 (`BattleSceneData.lua:1380`)
`AddSkillSummom(keys)` 对每个key执行：
- `GetSceneDataWithKey(v)` 获取预加载的场景数据
- `GetBattlePlayer(v)` 检查 player 是否已存在
  - **不存在时**：
    - 有 roomData 且是 NPC → `BattlePlayer.GetPlayer()` 创建NPC召唤
    - 无 roomData → `BattlePlayer.GetMonster()` 创建怪物召唤，标记 `SetSummonMonster(true)`
    - 调用 `AddBattlePlayer()` 注册到战斗系统
    - 设置状态 `SCENE_DATA_STATE.LOADING`
  - **已存在时**：仅标记 `SetSummonMonster(true)`
- 最后调用 `BattleAsset.Load(roleArr, LoadMonsterFinish, ...)` 异步加载资源

#### 4. 资源加载回调 (`BattleSceneData.lua:537`)
```lua
local function LoadMonsterFinish(loadPlayerArr)
    for _, v in ipairs(loadPlayerArr) do
        table.insert(waitCreateMonster, v)  -- 放入等待创建队列
    end
end
```

#### 5. 每帧检查创建 (`BattleSceneData.lua:642`)
```lua
local function CheckWaitCreateRole()
    if (#waitCreateMonster > 0) then
        local role = table.remove(waitCreateMonster, 1)
        if(role:IsDie() and role:IsMonster()) then
            BattleSceneData.RemoveBattleRole(role)  -- 已死亡直接移除
        else
            local isCreated = CreateRoleObj({ role })
            if (isCreated) then
                SetTargetArr({ role })
                if role:IsMonster() and role:IsSummon() then
                    MsgMg.SendMsg(RoomEvent.SCENE_MONSTER_CHANGED, role) -- 通知UI
                end
            end
        end
    end
end
```

#### 6. 创建GameObject (`BattleSceneData.lua:409`)
`CreateRoleObj` 中：
- 怪物已死亡且是召唤怪 → 跳过创建
- 否则依次调用：
  - `v:CreateObj()` — 创建 Unity GameObject
  - `v:UpdateStartPointPos()` — 设置出生点
  - `BattleAction1.AddActionPlayer(v)` — 加入行动系统
  - `v:ShowObj()` — 显示
  - `sceneData:SetInfoState(SCENE_DATA_STATE.LOADED)` — 标记加载完成

### 关键类和方法

| 组件 | 文件 | 关键方法 |
|------|------|----------|
| HandleLogBattle | `_HandleLogBattle.lua` | `OnSkillSummon(actionInfo, info)` |
| BattleSceneData | `BattleSceneData.lua` | `AddSkillSummom(keys)`, `GetBattlePlayer(key)` |
| BattlePlayer | `BattlePlayer.lua` | `GetPlayer()`, `GetMonster()`, `CreateObj()` |
| BattlePlayerData | `BattlePlayerData.lua` | `SetSummonMonster(bool)`, `IsSummon()` |
| BattleAsset | `BattleAsset.lua` | `Load()`, `GetObj()` |

### 注意事项
- 如果 player 在 `CreateSceneData()` 阶段已被创建（如场景初始化时 scenePlayerArr 中已有该怪物数据），召唤战报到达时 `GetBattlePlayer()` 会返回已有 player，走 else 分支只标记 `SetSummonMonster(true)`，不会重复加载资源。此时 player 的 GameObject 创建依赖初始化时的资源加载流水线。

---

## Q2: 地龙位移后Boss怎么跟随玩家移动旋转

### MontserSelectAreaUIG (1100) 定义

`_HandleLogBattleEffect.lua:167`：
```lua
MontserSelectAreaUIG = 1100, -- 地龙位移
```

注册处理函数 (`_HandleLogBattleEffect.lua:1845`)：
```lua
[BattleEffectID.MontserSelectAreaUIG] = OnShowSelectAreaUI,
```

### 完整流程

```
MontserSelectAreaUIG(1100) → OnShowSelectAreaUI() 显示区域选择UI
  → 玩家选择区域 → OnSelectPathCallback()
  → BattleService.ChangeBattleTarget(areaIndex, 3/4) 通知服务器
  → 服务器处理后下发战报:
    → AREA_MOVE (ActionKind=35) — 开始移动
    → AREA_MOVE_END (ActionKind=36) — 移动结束，Boss跟随+旋转
```

### 第一阶段：显示选择UI

`BattleEffectRaid.lua:978-1015` `ShowSelectPathUI()`：
- 获取当前玩家区域：`local areaId = player:GetAreaId()`
- 更新按钮位置：`UpdateBtnPos(areaId, true)`
- 显示区域选择界面供玩家点击

### 第二阶段：玩家选择区域

`BattleEffectRaid.lua:477-665` `OnSelectPathCallback()`：
- 玩家点击目标区域按钮
- 调用 `BattleService.ChangeBattleTarget(tempIndex, 3)` 或 `BattleService.ChangeBattleTarget(tempIndex, 4)` 通知服务器
- 设置遮罩时间：`BattleEffectRaid.SetMaskTime(nil, maskTime, maskTime)`

### 第三阶段：AREA_MOVE 更新区域索引

`_HandleLogBattle.lua:1338-1355` `OnAreaMoveAction()`：
```lua
function HandleLogBattle.OnAreaMoveAction(actionInfo, info)
    local time = info.data[actionInfo.len + 1]
    local maxTime = info.data[actionInfo.len + 2]
    local curIndex = info.data[actionInfo.len + 3]
    BattleEffectRaid.SetMaskTime(curIndex, time, maxTime)
end
```

`BattleEffectRaid.lua:1276-1298` `SetMaskTime()`：
```lua
function BattleEffectRaid.SetMaskTime(_curIndex, _time, _maxTime)
    if (_curIndex ~= nil) then
        if (curIndex ~= _curIndex) then
            curIndex = _curIndex              -- 更新当前区域索引
            UpdateBtnPos(_curIndex, false)     -- 更新UI按钮位置
            player:SetAreaId(curIndex)         -- 同步区域ID到玩家
        end
    end
end
```

### 第四阶段：AREA_MOVE_END — Boss跟随+旋转（核心）

`_HandleLogBattle.lua:1357-1367` `OnAreaMoveEndAction()`：
```lua
function HandleLogBattle.OnAreaMoveEndAction(actionInfo, info)
    local key = info.data[actionInfo.len + 1]
    local player = BattleSceneData.GetBattlePlayer(key)
    if (player) then
        player:UpdateStartPointPos()      -- 1. 更新Boss位置到新区域
        player:LockAtTarget()             -- 2. Boss旋转面向玩家
        player:SetAreaMoveIngState(false)  -- 3. 结束移动状态
    end
end
```

**注意**：这里的 `key` 是 Boss 的实体 key，服务器通过战报告知客户端哪个 Boss 需要跟随移动。

### Boss 位置更新：UpdateStartPointPos

Boss 的位置来自 `sceneData` 中预设的出生点坐标，`UpdateStartPointPos()` 将 Boss 的 Transform 位置设置到新区域对应的坐标。坐标由服务器在场景初始化时通过 `sceneData.attrs` 下发。

### Boss 旋转朝向：LockAtTarget

`BattlePlayer.lua:1346-1375`：
```lua
function BattlePlayer:LockAtTarget(target)
    if (target == nil) then
        target = self.target              -- 获取当前目标(玩家)
    end
    if target == nil or target:IsAnimNil() then
        return
    end
    if (self:IsMonster()) then
        local conf = self:GetConf()
        if (conf.unlock_at == 1) then
            return                        -- 配置了 unlock_at=1 则不旋转
        end
    end
    local animTra = self:GetParentTra()
    if (animTra) then
        local targetPos = target:GetParentPos()   -- 获取玩家位置
        self.targetPos.x = targetPos.x
        self.targetPos.y = self.parentPos.y       -- Y轴保持自身高度
        self.targetPos.z = targetPos.z
        animTra:LookAt(self.targetPos)             -- Boss朝向玩家
    end
end
```

### 关键文件

| 文件 | 函数 | 作用 |
|------|------|------|
| `_HandleLogBattleEffect.lua` | `OnShowSelectAreaUI` | 显示区域选择UI |
| `BattleEffectRaid.lua` | `ShowSelectPathUI`, `OnSelectPathCallback`, `SetMaskTime` | 区域选择、回调、索引更新 |
| `_HandleLogBattle.lua` | `OnAreaMoveAction`, `OnAreaMoveEndAction` | 处理移动和移动结束战报 |
| `BattlePlayer.lua` | `UpdateStartPointPos`, `LockAtTarget` | Boss位置更新、旋转朝向 |
| `BattlePlayerData.lua` | `SetAreaId`, `GetAreaId` | 区域ID存取 |

### 总结

- **位置更新**：`UpdateStartPointPos()` 把 Boss 坐标设置到新区域对应的位置
- **旋转朝向**：`LockAtTarget()` 通过 `Transform:LookAt()` 让 Boss 面向玩家，Y轴保持自身高度不变
- **可配置跳过**：怪物配置 `unlock_at=1` 时不执行旋转
- **整体由服务器驱动**：客户端收到 `AREA_MOVE_END` 战报后才执行 Boss 的位置和旋转更新

---

## Q3: 客户端真机AB包特效不显示怎么排查

### 排查项

1. **引用贴图是否单独在场景中打包**
2. **引用模型是否勾选了 `isReadable`**


## BattleSkillEffectConfig 字段说明

这些字段是“一个技能在战斗表现里会用到哪些特效”的配置项。

示例：

```json
"level": 1,
"chantIds": [],
"releaseIds": [1961005],
"ballisticIds": [],
"hitIds": [],
"buffs": [],
"keepIds": [],
"keepReleaseIds": [],
"_help": ""
```

### `level`
当前这条配置对应的技能等级字段。

从现有读取逻辑看，它基本没有参与实际分支判断，更像是历史保留或预留字段。当前取配置主要还是按角色 `job + skillGroup` 或怪物 `skillId/group` 来找。

### `chantIds`
吟唱阶段特效列表。

触发时机：技能进入吟唱时播放。
典型用途：手上蓄力光效、脚底法阵、吟唱读条期间的特效。

### `releaseIds`
技能释放阶段特效列表。

触发时机：技能动作正式释放时播放。
典型用途：挥刀火光、出手爆发、抬手瞬间的释放特效。

如果配置为：

```json
"releaseIds": [1961005]
```

表示这个技能当前只有一个释放特效 `1961005`。

### `ballisticIds`
弹道特效列表。

触发时机：技能有飞行物或投射物时，根据第几段弹道取不同下标。
典型用途：箭、火球、飞剑、多段飞弹。

### `hitIds`
受击特效列表，通常只取第一个。

触发时机：技能命中目标时播放在目标身上。
典型用途：命中火花、爆炸 hit、受击闪光。

### `buffs`
这个技能会关联到的 buff 特效 ID 列表。

它更多用于汇总这个技能需要预加载哪些特效资源，以及场景里已有 buff 信息的记录；不是像 `releaseIds` 那样在放技能时直接 `Show()`。

你可以把它理解成：这个技能可能附带某些持续 buff，这些 buff 后面会走 `OnEffectShow/OnEffectHide` 那套 buff 战报链。

### `keepIds`
持续型技能的吟唱/维持阶段特效列表。

普通技能吟唱看 `chantIds`，持续技能吟唱看 `keepIds`。
典型用途：持续施法时角色身上的循环维持特效。

### `keepReleaseIds`
持续型技能释放阶段的特效列表。

普通技能释放看 `releaseIds`，持续技能释放看 `keepReleaseIds`。

### `_help`
纯备注字段，给策划或配置同学看的，运行时没有实际逻辑。

### 这条配置的实际含义
按这段示例配置，它表示：

- 没有吟唱特效
- 只有一个释放特效：`1961005`
- 没有弹道
- 没有受击特效
- 没有 buff 特效
- 也没有持续技能专用的维持或释放特效

也就是一个很“干净”的技能表现配置，基本只在出手那一刻播一次特效。


## HandleLogBattleEffect.OnBallisticPierce 穿透弹道实现

`HandleLogBattleEffect.OnBallisticPierce(actionInfo, info)` 用来处理“穿透型弹道”表现。它不是普通一次性命中后销毁的弹道，而是会持续朝某个方向飞行一段时间，并且在后续战报里不断同步方向或状态。

关键代码位置：
- `_HandleLogBattleEffect.lua` 中的 `OnBallisticPierce`
- `_HandleLogBattleEffect.lua` 中的 `CheckBallisticPierce`
- `BattleSkillEffect.InstPar(...)`

### 1. 读取战报参数
`OnBallisticPierce` 会从战报里取这些字段：

- `key`：这条穿透弹道的唯一 key
- `sKey`：谁发射的
- `skillId`：对应哪个技能
- `dirX / dirY / dirZ`：飞行方向向量
- `speed`：飞行速度
- `endTime`：持续时间
- `changedEffectId`：可覆盖默认弹道特效 ID
- `effExtEndTime`：额外延长时间

然后通过 `BattleSceneData.GetBattlePlayer(sKey)` 找到施法者 `player`。

### 2. 解析要播放的弹道特效 ID
默认会这样取：

```lua
data.effectId = BattleSkillEffectConfig.GetBallisticEffectConf(sKey, skillId, player:IsRole())
```

也就是从 `BattleSkillEffectConfig.json` 的 `ballisticIds` 里拿这条技能对应的弹道特效。

如果战报里 `changedEffectId > 0`，则直接覆盖：

```lua
if (changedEffectId > 0) then
    data.effectId = changedEffectId
end
```

### 3. 校验是否可播放
接着会取特效配置：

```lua
local eConf = EffectConfig.GetConf(data.effectId)
```

如果：
- `player:IsAnimNil()`
- 或 `eConf == nil`

就直接 return。

之后还会调用：

```lua
BattleSkillEffect.IsBlockEffect(data.effectId, player, player, data.endTime + data.effExtEndTime)
```

如果当前设置或场景规则屏蔽了这个特效，也直接不播。

### 4. 首次收到时：实例化弹道特效
如果这条 `key` 之前没有记录：

```lua
if (actionBallisticPierceArr[data.key] == nil) then
```

会创建一个回调 `fun(tra)`，并调用：

```lua
BattleSkillEffect.InstPar(data.effectId, player, player, data.endTime + data.effExtEndTime, nil, nil, fun)
```

这里的核心是：
- 用 `InstPar` 实例化弹道 prefab
- 实例化成功后，在回调里把运行时数据存进 `actionBallisticPierceArr[data.key]`

保存的内容包括：
- `player`
- `key`
- `effectId`
- `skillId`
- `dirVec3`
- `endTime`
- `speed`
- `tra`
- `obj`
- `flyObj`
- `headObj`
- `hitObj`

其中：
- `flyObj = GlobalFun.GetObj(tra, "fly")`
- `headObj = GlobalFun.GetObj(tra, "fly/head")`
- `hitObj = GlobalFun.GetObj(tra, "hit")`

也就是说，这类弹道 prefab 一般会拆成“飞行阶段”和“命中阶段”两个子节点。

初始化时会：
- 让特效朝 `dirVec3` 的方向朝向
- 显示 `flyObj`
- 隐藏 `hitObj`
- 把特效挂到场景节点下

### 5. 后续再次收到同一个 key：更新方向，不重复创建
如果这条穿透弹道已经存在：

```lua
else
    local info = actionBallisticPierceArr[data.key]
    info.dirVec3 = data.dirVec3
    local x, y, z = Transform.GetPosition(info.tra)
    local pos = info.dirVec3 + Vector3(x, y, z)
    info.tra:LookAt(pos)
end
```

这说明：
- 同一个 `key` 的后续战报不是重新播一个弹道
- 而是更新当前弹道的方向向量
- 让已有特效继续飞

所以它本质上是“单实例持续更新”的弹道系统。

### 6. 每帧推进：CheckBallisticPierce
`HandleLogBattleEffect.UpdateAction()` 里会调用 `CheckBallisticPierce()`。

这一步负责逐帧推进所有穿透弹道：
- 根据 `speed` 和 `BattleData.GetDeltaTime()` 计算位移
- 按 `dirVec3` 向前移动 `tra`
- 递减或比较 `endTime`
- 到时间后切换到命中/结束状态
- 回收或清理 `actionBallisticPierceArr[key]`

也就是说：
- `OnBallisticPierce` 负责“创建 / 更新方向”
- `CheckBallisticPierce` 负责“每帧飞行 / 结束回收”

### 7. 和普通弹道的区别
普通弹道通常走 `OnBallistic`，由 `BattleAction1.AddBallistic(data)` 管理。

穿透弹道的区别是：
- 由唯一 `key` 持续追踪
- 可以多次更新方向
- 不会每次战报都重新实例化
- 生命周期更像“持续存在的飞行体”

### 8. 总结
`OnBallisticPierce` 的实现思路可以概括成：

1. 从战报读取穿透弹道参数
2. 解析要用的弹道特效 `effectId`
3. 首次收到时实例化特效，并记录到 `actionBallisticPierceArr`
4. 再次收到同一个 `key` 时只更新方向
5. 在 `CheckBallisticPierce()` 中逐帧推进、朝向、结束和回收

所以它不是“播一次特效”，而是一个由战报驱动、按 `key` 持续维护的穿透飞行物表现系统。

---

## Q4: HandleLogBattleEffect.OnShowLineEffect(actionInfo, info) 详细介绍

### 作用概述

`HandleLogBattleEffect.OnShowLineEffect(actionInfo, info)` 是战斗客户端里处理“显示连线特效”战报的入口。

它的职责不是简单播一个 prefab，而是：

- 解析战报里下发的连线参数
- 找到连线起点和终点对应的玩家/怪物
- 根据 `kind` 选择不同的连线表现方式
- 创建或复用连线特效
- 把连线对象登记到 `lineEffectArr`
- 交给后续 `UpdateAction -> CheckLineEffectArr()` 每帧刷新线段两端坐标

对应位置：

- 主入口：
  [\_HandleLogBattleEffect.lua](/Users/gexianglin/aaboli/main/Assets/Scripts/LuaScript/Plane/BattlePlane/BattleNet/_HandleLogBattleEffect.lua:3438)
- 战报注册：
  [BattleAnalysis.lua](/Users/gexianglin/aaboli/main/Assets/Scripts/LuaScript/Plane/BattlePlane/BattleNet/BattleAnalysis.lua:694)
- 每帧更新：
  [\_HandleLogBattleEffect.lua](/Users/gexianglin/aaboli/main/Assets/Scripts/LuaScript/Plane/BattlePlane/BattleNet/_HandleLogBattleEffect.lua:4621)

### 战报字段结构

函数一开始按固定顺序从 `info.data` 里取值：

```lua
local kind = info.data[actionInfo.len + 1]
local effectId = info.data[actionInfo.len + 2]
local time = info.data[actionInfo.len + 3] * accuracy
local sourceKey = info.data[actionInfo.len + 4]
local targetKey = info.data[actionInfo.len + 5]
```

含义分别是：

- `kind`
  连线类型，决定后面走哪个分支
- `effectId`
  连线特效的 `EffectConfig` ID
- `time`
  持续时间，乘以 `accuracy`
- `sourceKey`
  战报来源对象 key
- `targetKey`
  战报目标对象 key

然后它还会继续把后续所有参数收进 `linePlayerKey`：

```lua
local index = actionInfo.len + 6
for i = index, count do
    table.insert(linePlayerKey, info.data[index])
    index = index + 1
end
```

这部分通常用来承载：

- 连线真正的起点 key
- 连线真正的终点 key
- 某些特殊分支的层级参数

也就是说：

- `sourceKey/targetKey`
  更像这条战报的“宿主”
- `linePlayerKey`
  才是实际决定线从谁连到谁的扩展参数

### 进入前的基础保护

函数先用 `sourceKey/targetKey` 拿两个战斗对象：

```lua
local source = BattleSceneData.GetBattlePlayer(sourceKey)
local player = BattleSceneData.GetBattlePlayer(targetKey)
```

如果任意一个不存在，或者动画对象没准备好，就直接返回：

```lua
if (source == nil or source:IsAnimNil() or player == nil or player:IsAnimNil()) then
    return
end
```

这层保护的意义是：

- 战报到了，但对象已经死了/退场了/还没创建完
- 这时不再尝试生成连线

### `kind == 7`：可复用、带层级控制的连线

这是最特殊的一类。

它会从 `linePlayerKey` 里取：

- `lineStartKey = linePlayerKey[1]`
- `lineEndKey = linePlayerKey[2]`
- `lineLayer = linePlayerKey[3]`

然后先查 `lineEffectArr` 里有没有同一对起终点的旧连线：

```lua
if v.key == lineStartKey * 1000000 + lineEndKey then
```

如果找到了旧连线：

- 不重新创建特效
- 直接重置位置和旋转
- `BattleSkillEffect.CheckLayerEffect(effectId, effectTra, lineLayer)`
- `BattleAction1.RefreshEffectTime(effectTra, time)`
- 重建 `lineInfo.lineRenders`
- 更新时间 `lineInfo.time = time`

这里的重点是“复用”：

- 同一对目标反复收到 kind 7 战报时，不会反复实例化
- 而是延长显示时长并切换线层

如果没找到旧连线：

- 调 `BattleSkillEffect.ShowLine(source, player, effectId, time)` 新建
- 从特效里取 `LV01/LV01`、`LV01/LV02` 这种子节点上的 `LineRenderer`
- 只保留和 `lineLayer` 相等的那一条

这里还有一个很关键的挂点处理：

```lua
local customSourceHPoint = eConfig.hPoint
if specialLineConfig[tostring(effectId)] then
    customSourceHPoint = specialLineConfig[tostring(effectId)]
end
```

当前 `specialLineConfig` 里有：

```lua
["1791930"] = 11
```

这表示：

- 某些连线特效的起点挂点不完全按 `EffectConfig.hPoint`
- 会被这张特殊表覆盖

### `kind == 6`：龙芯类特殊连线

这类逻辑注释写的是：

- “龙芯特殊特殊处理”

特点是：

- 用 `BattleSkillEffect.ShowLine(...)`
- 取子节点路径是 `LV01`、`LV02`
- 不做复用 key 匹配
- 起点挂点同样支持 `specialLineConfig` 覆盖

本质上也是双点连线，但资源结构和 `kind == 7` 不完全一样。

### `kind == 1`：只给主角看的连线

这段有一个额外限制：

```lua
if (kind == 1 and effectId > 0 and #linePlayerKey > 1 and player:IsMainRole()) then
```

也就是说这类连线只有在：

- `targetKey` 对应的 `player`
- 恰好是主角

时才会生成。

注释写的是：

- “史莱姆连线使用 特殊处理的 只能角色能看见”

所以它更像：

- 只给本机主视角看的警示线/机制线

### `kind == 2 / 3 / 5`：用 `ShowLine2` 的普通双点连线

这三种 `kind` 被合并在同一个分支里：

```lua
if ((kind == 2 or kind == 3 or kind == 5) and effectId > 0 and #linePlayerKey > 1) then
```

它们的关键差异是：

- 用的是 `BattleSkillEffect.ShowLine2(source, player, effectId, time)`

而 `ShowLine2` 实际对应：

```lua
BattleSkillEffect.InstPar(effectId, source, target, time, nil, true, nil, false)
```

相比 `ShowLine`，最后一个参数不同，说明底层 `InstPar` 在“是否使用某种 line 模式/挂点模式”上有区别。

这类分支会：

- 取 `lineStartKey`、`lineEndKey`
- 两端都按 `eConfig.hPoint` 取挂点
- 保存 `key = lineStartKey * 1000000 + lineEndKey`
- 放进 `lineEffectArr`

### `kind == 4`：另一种普通 `ShowLine`

`kind == 4` 跟 `kind == 2/3/5` 很像，但它用的是：

- `BattleSkillEffect.ShowLine(...)`

不是 `ShowLine2(...)`。

所以它和前面那组三类的主要区别，仍然是：

- 底层实例化模式不同

### `lineInfo` 里保存了什么

只要成功创建/复用，函数都会把运行时信息存进 `lineInfo`，核心字段包括：

- `effTra`
  连线特效根节点
- `lineRenders`
  这条线实际驱动的 `LineRenderer` 组件列表
- `startPlayerTra`
  起点挂点 Transform
- `endPlayerTra`
  终点挂点 Transform
- `time`
  剩余持续时间
- `playerObj`
  起点挂点的 `gameObject`
- `lineStartPlayer`
  起点玩家对象
- `lineEndPlayer`
  终点玩家对象
- `key`
  某些分支用于复用查找的组合 key

这些信息统一放进：

```lua
local lineEffectArr = {}
```

后续每帧刷新都靠这个数组。

### 真正让线“跟着人动”的地方

`OnShowLineEffect` 只负责建表和建对象，真正更新位置的是：

- [CheckLineEffectArr](/Users/gexianglin/aaboli/main/Assets/Scripts/LuaScript/Plane/BattlePlane/BattleNet/_HandleLogBattleEffect.lua:4621)

它在每帧 `UpdateAction()` 里被调用：

```lua
CheckLineEffectArr()
```

刷新逻辑是：

1. 检查剩余时间 `v.time`
2. 取起点挂点世界坐标
3. 取终点挂点世界坐标
4. 如果 `useTargetPos` 开启，则终点改为指定坐标
5. 如果任一端对象死亡，则把 `v.time = 0`
6. 否则对每个 `LineRenderer` 调：

```lua
line:SetPosition(0, linePosA)
line:SetPosition(1, linePosB)
```

还有一个小细节：

```lua
if v.playerObj.activeInHierarchy == false then
    line:SetPosition(0, linePosB)
end
```

表示：

- 如果起点挂点对象当前被隐藏了
- 就把起点也压到终点位置，避免画出一条错误的长线

### 连线什么时候结束

结束方式主要有两种：

1. 时间到了
   `BattleFun.CheckFrameTime(v.time)` 不再通过后，直接从 `lineEffectArr` 移除

2. 角色死了
   如果 `lineStartPlayer` 或 `lineEndPlayer` 死亡，会把 `v.time = 0`
   下一轮就会被移除

另外整场战斗收尾时：

- [HandleLogBattleEffect.Collect](/Users/gexianglin/aaboli/main/Assets/Scripts/LuaScript/Plane/BattlePlane/BattleNet/_HandleLogBattleEffect.lua:4718)

会直接：

```lua
lineEffectArr = {}
```

把连线状态全部清掉。

### 和其他连线接口的关系

`OnShowLineEffect` 不是孤立存在的，它旁边还注册了：

- `CHANGE_LINE_TARGET`
- `BREAK_LINE_EFFECT`

也就是说这套连线机制通常包含三步：

1. `SHOW_LINE_EFFECT`
   创建连线
2. `CHANGE_LINE_TARGET`
   改变连线目标/终点
3. `BREAK_LINE_EFFECT`
   主动断线

所以 `OnShowLineEffect` 是“起线入口”。

### 一句话总结

`HandleLogBattleEffect.OnShowLineEffect(actionInfo, info)` 的本质就是：

- **把战报里的“谁和谁之间要连哪种线、连多久、用哪种资源”解析出来**
- **创建或复用对应连线特效**
- **把连线两端挂点登记进 `lineEffectArr`**
- **再由每帧更新逻辑持续把 `LineRenderer` 两端吸附到对应角色/怪物身上**

---

## Q5: EffectType = ROTATE 旋转弹道特效介绍

### 基本定义

`EffectType.ROTATE` 定义在：

- [EffectConfig.lua](/Users/gexianglin/aaboli/main/Assets/Scripts/LuaScript/Plane/BattlePlane/BattleSkill/EffectConfig.lua:49)

```lua
EffectType = {
    CHANT = 1, -- 吟唱特效
    BALLISTIC = 2, -- 弹道特效
    FOLLOW = 3, -- 跟随特效
    NOT_FOLLOW = 4, -- 不跟随特效
    FOLLOW_TAIL = 5, -- 拖尾特效
    ROTATE = 6, -- 旋转弹道特效
    LINE = 7, -- 连线特效
}
```

一句话理解：

> `ROTATE` 是一种走“弹道入口”的特殊表现类型，但它不按普通弹道逐帧飞行插值，而是创建一个旋转/环绕类 prefab，等待到达时间结束后切换到命中特效。

### 入口流程

整体链路大致是：

```text
服务器战报/技能表现
  → HandleLogBattleEffect 解析弹道数据
  → BattleAction1.AddBallistic(data)
  → 读取 EffectConfig.GetConf(data.ballisticEffectId)
  → 如果 eConf.eType == EffectType.ROTATE
  → BattleAction1.AddRotate(data)
  → BattleSkillEffect.ShowRotate()
  → BattleSkillEffect.InstPar()
  → 加入 actionRotateArr
  → 每帧 ChecRotateArr() 检查到达时间
  → 隐藏 fly，显示 hit
```

关键分支在：

- [BattleAction1.lua](/Users/gexianglin/aaboli/main/Assets/Scripts/LuaScript/Plane/BattlePlane/BattleNet/BattleAction1.lua:533)

```lua
function BattleAction1.AddBallistic(data)
    local eConf = EffectConfig.GetConf(data.ballisticEffectId)
    if (eConf == nil) then
        return
    end
    if (eConf.eType == EffectType.ROTATE) then
        BattleAction1.AddRotate(data)
        return
    end
    if (eConf.eType ~= EffectType.BALLISTIC) then
        return
    end
    ...
end
```

所以从调用关系上看，`ROTATE` 属于弹道表现的一种特殊分支：外部仍然可以把它当作 ballistic effect 触发，但配置表里的 `eType` 决定它实际走 `AddRotate`。

### 创建旋转特效

核心创建逻辑在：

- [BattleAction1.lua](/Users/gexianglin/aaboli/main/Assets/Scripts/LuaScript/Plane/BattlePlane/BattleNet/BattleAction1.lua:495)
- [BattleSkillEffect.lua](/Users/gexianglin/aaboli/main/Assets/Scripts/LuaScript/Plane/BattlePlane/BattleSkill/BattleSkillEffect.lua:593)

`AddRotate(data)` 做的事情：

```lua
local rotateEffectTra = BattleSkillEffect.ShowRotate(effectId, player, target, arriveTime + 2)
```

然后从 prefab 里取几个固定节点：

```lua
info.flyObj = GlobalFun.GetObj(info.tra, "fly")
info.headObj = GlobalFun.GetObj(info.tra, "fly/head")
info.hitObj = GlobalFun.GetObj(info.tra, "hit")
```

初始状态：

- `fly` 显示
- `fly/head` 如果存在则显示
- `hit` 隐藏
- `info.time = arriveTime`
- 加入 `actionRotateArr`

也就是说，`ROTATE` prefab 通常需要有这些子节点：

```text
root
  ├─ fly
  │   └─ head    可选
  └─ hit
```

`fly` 表示旋转/飞行阶段，`hit` 表示命中阶段。

### 每帧更新与命中切换

每帧入口：

- [BattleAction1.UpdateAction](/Users/gexianglin/aaboli/main/Assets/Scripts/LuaScript/Plane/BattlePlane/BattleNet/BattleAction1.lua:487)

```lua
function BattleAction1.UpdateAction()
    CheckActionPlayer()
    CheckEffect()
    CheckBallistic()
    ChecRotateArr()
    CheckTeamOut()
    CheckTeamIn()
end
```

`ChecRotateArr()` 逻辑：

- [BattleAction1.lua](/Users/gexianglin/aaboli/main/Assets/Scripts/LuaScript/Plane/BattlePlane/BattleNet/BattleAction1.lua:305)

```lua
if BattleFun.CheckFrameTime(info.time) then
    info.time = BattleFun.GetFrameTime(info.time)
else
    GlobalFun.SetObj(info.hitObj, true)
    if (info.headObj ~= nil) then
        GlobalFun.SetObj(info.headObj, false)
    end
    info.player:PlayEffectSound(info.player, info.effectId, true)
    GlobalFun.SetObj(info.flyObj, false)
    table.remove(actionRotateArr, index)
end
```

含义：

- 到达时间未结束：持续扣 `info.time`
- 到达时间结束：
  - 显示 `hit`
  - 隐藏 `fly/head`
  - 播放命中音效
  - 隐藏 `fly`
  - 从 `actionRotateArr` 移除

注意这里不是像普通 `BALLISTIC` 那样持续刷新弹道位置，而是靠 prefab 自己的动画/节点表现来完成“旋转弹道”的视觉过程。

### 挂载位置

`ShowRotate()` 最终调用：

```lua
BattleSkillEffect.InstPar(effectId, source, target, desTime, nil, true, nil)
```

在 `InstPar()` 中，如果特效类型是：

```lua
EffectType.BALLISTIC or EffectType.ROTATE or EffectType.Chains
```

会改用 `source` 的动画节点和挂点：

```lua
animTra = source:GetAnimTra()
pointTra = GetTraByHangPointType(source, pointType)
```

也就是说，`ROTATE` 创建时是按释放者的挂点起始，而不是按目标挂点起始。

### 和普通 BALLISTIC 的区别

| 类型 | 入口 | 运动逻辑 | 命中表现 |
|------|------|----------|----------|
| `BALLISTIC` | `AddBallistic` | 进入 `actionBallisticArr`，后续 `RefreshBallistic()` 按起点/终点/曲线刷新位置 | 到达后触发普通弹道命中逻辑 |
| `ROTATE` | `AddBallistic` 中转到 `AddRotate` | 进入 `actionRotateArr`，不走普通弹道插值，主要依赖 prefab 自身表现 | 时间到后切 `fly → hit` |

### 特效可见性

`ROTATE` 被加入了 `notHideEffectKindTab`：

- [EffectConfig.lua](/Users/gexianglin/aaboli/main/Assets/Scripts/LuaScript/Plane/BattlePlane/BattleSkill/EffectConfig.lua:63)

```lua
notHideEffectKindTab[tostring(EffectType.ROTATE)] = 1
```

这表示它属于某些隐藏逻辑里“不按普通可隐藏类型处理”的表现。另一个地方也能看到：非战斗页签时，`BORN` 和 `ROTATE` 会被提前放行，不进入后面的普通屏蔽分支：

- [BattleSkillEffect.lua](/Users/gexianglin/aaboli/main/Assets/Scripts/LuaScript/Plane/BattlePlane/BattleSkill/BattleSkillEffect.lua:298)

```lua
if(effectType == EffectType.BORN or effectType == EffectType.ROTATE) then
    return false
end
```

这里要注意 `CheckEffect()` 的返回语义：返回 `true` 才表示拦截创建，返回 `false` 表示放行。因此这段逻辑的含义是：非战斗页签下，`ROTATE` 不走后面的普通屏蔽分支，而是允许继续创建。

### 排查注意点

如果 `EffectType.ROTATE` 不显示或显示异常，优先检查：

1. `EffectConfig.GetConf(effectId)` 是否存在，且 `eType == EffectType.ROTATE`
2. 是否真的从弹道入口进入了 `BattleAction1.AddBallistic(data)`
3. `data.ballisticEffectId` 是否为空，技能弹道配置是否正确返回了 effectId
4. prefab 是否包含 `fly` 和 `hit` 节点，`fly/head` 可选
5. `arriveTime` 是否正确，太短会立刻切到 `hit`，太长会一直停在 `fly`
6. `source`、`target` 是否存在，`InstPar()` 会依赖 source 挂点创建
7. `SettingsData.BlockEffect(data.sourceRole)` 是否屏蔽了释放者特效

### 一句话总结

`EffectType.ROTATE` 是“旋转弹道特效”：它通过弹道战报触发，但客户端识别到 `eType == ROTATE` 后会走 `AddRotate`，创建包含 `fly/hit` 节点的 prefab，并由 `actionRotateArr` 在到达时间结束时完成 `fly` 到 `hit` 的切换。

---

## Q6: PerformanceSwitching 性能测试开关介绍

### 结论

`PerformanceSwitching` 不是一个独立类，而是客户端调试面板 `TestView` 中的一组“性能测试开关”。它通过固定的 `PlayerPrefs` key 保存配置：

- [TestView.lua](/Users/gexianglin/aaboli/main/Assets/Scripts/LuaScript/Plane/TestView.lua:4)

```lua
local performanceKey = "PerformanceSwitchingKey"
```

启动/加载 `TestView.lua` 时，代码会读取 `PlayerPrefs.GetString("PerformanceSwitchingKey")`，把 JSON 解出来，然后逐项调用 `setPerformanceOption(k, v)` 生效：

- [TestView.lua](/Users/gexianglin/aaboli/main/Assets/Scripts/LuaScript/Plane/TestView.lua:219)

```lua
do
    local saveStr = PlayerPrefs.GetString(performanceKey, "") or ""
    if saveStr == "" then
        return
    end
    local options = json.decode(saveStr, 1, nil)
    for k, v in pairs(options) do
        setPerformanceOption(k, v)
    end
end
```

所以它的本质是：把外部保存下来的性能测试配置，在 Lua 启动时恢复到 `BattleTest` / `TestView` 的运行状态里。

### 调用入口

运行时还有一个外部入口：

- [TestView.lua](/Users/gexianglin/aaboli/main/Assets/Scripts/LuaScript/Plane/TestView.lua:211)

```lua
function TestView.OnSetOption(key, value)
    setPerformanceOption(key, value)
end
```

也就是说，外部工具/面板如果不想等下次启动，可以直接调：

```lua
TestView.OnSetOption("SceneLoad", true)
TestView.OnSetOption("StopBattle", true)
```

保存层面则需要把 JSON 写进 `PlayerPrefs` 的 `PerformanceSwitchingKey`。当前 Lua 侧只负责读取和应用，没有看到它在本文件里主动保存。

### 支持的开关

核心逻辑在 `setPerformanceOption`：

- [TestView.lua](/Users/gexianglin/aaboli/main/Assets/Scripts/LuaScript/Plane/TestView.lua:196)

```lua
local function setPerformanceOption(k, v)
    if k == "SceneLoad" then
        for key, _ in pairs(BattleTest.ActionConfig) do
            BattleTest.ActionConfig[key] = not v
        end
    elseif k == "LogOutput" then
        TestView.isLog = not v
        UnityEngine.Debug.unityLogger.filterLogType = UnityEngine.LogType.Log
    elseif k == "StopBattle" then
        TestView.isRunBattle = not v
    elseif k == "FastLogin" then
        TestView.fastLogin = v
    end
end
```

| key | value=true 的效果 | 影响对象 |
|---|---|---|
| `SceneLoad` | 关闭部分战斗表现加载/播放流程 | `BattleTest.ActionConfig` |
| `LogOutput` | 关闭 `TestView.isLog` | `TestView.isLog` |
| `StopBattle` | 暂停/阻断战斗运行相关处理 | `TestView.isRunBattle` |
| `FastLogin` | 开启快速登录流程 | `TestView.fastLogin` |

这里有一个容易踩坑的点：除了 `FastLogin`，其他几个选项基本都是“开关名表示要关闭某类成本”，所以代码里常见 `not v`。例如 `SceneLoad=true` 不是“开启场景加载”，而是把 `BattleTest.ActionConfig` 里的表现项都改成 `false`。

### SceneLoad：关闭战斗表现加载

默认表现配置在：

- [BattleTest.lua](/Users/gexianglin/aaboli/main/Assets/Scripts/LuaScript/Plane/BattlePlane/BattleTool/BattleTest.lua:37)

```lua
BattleTest.ActionConfig={
    --加载区块场景
    LoadBlock=true,
    --播放CG
    PlayCG= true,
    --显示Loading
    ShowLoading= true 
}
```

当 `SceneLoad=true` 时：

```lua
BattleTest.ActionConfig.LoadBlock = false
BattleTest.ActionConfig.PlayCG = false
BattleTest.ActionConfig.ShowLoading = false
```

主要影响：

- `LoadBlock=false`：战斗地图区块加载/卸载相关逻辑会被跳过，例如 `BattleMapBlockManager` 里直接 `return`。
- `PlayCG=false`：CG 播放逻辑会被跳过或提前结束，例如 `GameCgManager` 会看这个开关。
- `ShowLoading=false`：Loading 面板不打开，相关回调直接执行。`BattleMapAction.OpenLoadingPlane()` 会直接走 `_onShow` / `_onComplete`。

这个开关适合做“去掉场景/CG/Loading 影响后的战斗性能观测”，但它会改变真实进入战斗时的表现链路，不适合拿来验证完整用户流程。

### LogOutput：关闭测试日志输出

`LogOutput=true` 时：

```lua
TestView.isLog = false
UnityEngine.Debug.unityLogger.filterLogType = UnityEngine.LogType.Log
```

含义是关闭 `TestView.isLog` 这类 Lua 测试日志输出标记，减少日志刷屏对性能数据的干扰。

注意：这里设置 `filterLogType = UnityEngine.LogType.Log` 并不是“禁止所有 Unity 日志”。它更像是把 Unity logger 的过滤级别固定到 `Log`，真正控制测试日志的关键仍是 `TestView.isLog = false`。具体还要看各处打印是否检查了 `TestView.isLog`。

### StopBattle：停止战斗推进/网络稳定性检测

`StopBattle=true` 时：

```lua
TestView.isRunBattle = false
```

默认值在：

- [TestView.lua](/Users/gexianglin/aaboli/main/Assets/Scripts/LuaScript/Plane/TestView.lua:6)

```lua
TestView.isRunBattle = true
```

这个标记会被战斗相关逻辑读取。例如网络稳定性检查里，如果 `not TestView.isRunBattle` 就直接返回：

- [BattleAnalysis.lua](/Users/gexianglin/aaboli/main/Assets/Scripts/LuaScript/Plane/BattlePlane/BattleNet/BattleAnalysis.lua:1401)

```lua
local function CheckNetSteady()
    if logTime == nil or not TestView.isRunBattle then
        return
    end
```

`BattleService` 的战斗响应处理也会参考这个状态，避免在停止战斗时继续处理部分返回。

这个开关适合用来冻结/阻断战斗推进，观察静态场景、资源占用、渲染压力等。但开启后战斗时序不再是正常线上逻辑。

### FastLogin：跳过一部分登录/选角场景成本

`FastLogin=true` 时：

```lua
TestView.fastLogin = true
```

默认值：

- [TestView.lua](/Users/gexianglin/aaboli/main/Assets/Scripts/LuaScript/Plane/TestView.lua:18)

```lua
TestView.fastLogin = false
```

进入角色加载流程时会先判断：

- [BattleLogin.lua](/Users/gexianglin/aaboli/main/Assets/Scripts/LuaScript/Plane/BattlePlane/BattleLogin.lua:23)

```lua
function BattleLogin.LoadSceneWithCreateRole()
    if TestView.fastLogin then
        TestView.FastLogin()
        return
    end
```

`TestView.FastLogin()` 做的事情包括：

- 加载 `GlobalVariable.LevelLightSceneName`
- `BattleAsset.Load(nil, ...)`
- 预加载 `SelectRoleConfig.SceneLinkedBundleAssetNameTab`
- 初始化并暂停 `BattleMapTime`
- SDK 默认登录完成后调用 `SelectRolePlaneNet.StartGame()`

另外，在选角进入游戏后的清理逻辑里，`fastLogin` 会跳过部分选角场景销毁/卸载：

- [SelectRolePlaneNet.lua](/Users/gexianglin/aaboli/main/Assets/Scripts/LuaScript/Plane/SeleRolePlane/Net/SelectRolePlaneNet.lua:132)

```lua
if not TestView.fastLogin then
    GlobalFun.DestroyObj(SelectRoleLogic._sceneObj.gameObject, 0)
end
```

以及后续 `sceneshare`、`Level_xuanjue`、`Start` 等卸载流程也被 `if not TestView.fastLogin` 包住。

这个开关适合跳过登录/选角/场景切换带来的额外成本，更快进入战斗性能测试环境。

### 保存格式示例

`PerformanceSwitchingKey` 里保存的是 JSON 字符串，形态大致如下：

```json
{
  "SceneLoad": true,
  "LogOutput": true,
  "StopBattle": false,
  "FastLogin": true
}
```

读取后对应效果：

- `SceneLoad=true`：关闭 `LoadBlock`、`PlayCG`、`ShowLoading`
- `LogOutput=true`：关闭 `TestView.isLog`
- `StopBattle=false`：保持 `TestView.isRunBattle=true`
- `FastLogin=true`：开启快速登录

### 使用建议

如果目标是测纯战斗帧率/资源压力，常见组合是：

```json
{
  "SceneLoad": true,
  "LogOutput": true,
  "StopBattle": false,
  "FastLogin": true
}
```

这样可以尽量减少场景加载、CG、Loading、日志、登录流程对数据的影响，同时仍让战斗继续跑。

如果目标是观察静态场景或某个固定状态，可以再加：

```json
{
  "StopBattle": true
}
```

但要记住：`StopBattle=true` 后战斗推进和部分服务响应会被挡住，测出来的是“冻结状态”的表现，不是正常战斗体验。

### 一句话总结

`PerformanceSwitching` 是一套通过 `PlayerPrefs("PerformanceSwitchingKey")` 注入的性能测试配置。它集中控制战斗场景/CG/Loading、日志、战斗推进、快速登录四类开关，用来剥离非目标成本或快速进入测试状态；但其中多个开关会改变正常游戏流程，所以性能测试时要明确当前配置组合。
