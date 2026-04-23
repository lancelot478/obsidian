# 战斗客户端问答记录

## 目录
- [[#Q1: AddSkillSummon 客户端怎么处理服务器发回的召唤日志并在场景中召唤]]
- [[#Q2: 召唤怪物的模型名(bodyId/assetId)从哪里读的]]
- [[#Q3: 特效(如FX_DragonPillarWorldBoss1_skill1_3_2_YingJi)是怎么生成的]]
- [[#Q4: 板块常驻特效在哪里处理]]
- [[#Q5: AREA_EFFECT time=-1 为什么不显示特效]]
- [[#Q6: 客户端怎么知道地图的area index]]
- [[#Q7: 客户端怎么移动area index]]
- [[#Q8: 哪些战报触发 CheckLayerEffect 控制特效分层显隐]]
- [[#Q9: OnEffectShow (EFFECT_SHOW ActionKind=13) 详细介绍]]
- [[#Q10: EFFECT_OBJ_SET (ActionKind=83) 介绍]]
- [[#Q11: OnEffectShow 添加的调试日志]]
- [[#Q12: CheckLayerEffect flag 枚举含义]]
- [[#Q13: 地龙位移后Boss怎么跟随玩家移动旋转]]
- [[#Q14: man_dragon21EnergyVal UI显示由哪个战报控制]]

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

## Q2: 召唤怪物的模型名(bodyId/assetId)从哪里读的

### 数据来源链路

```
sceneData.uid (如 112101)
  → BattleMonsterConfig.GetMonsterConfig(112101)   -- 查怪物配置表
  → conf.model = 9610                              -- 配置表里的模型ID
  → self.assetId = 9610
  → GetBodyId() → 无时装 → return self.assetId = 9610
  → BattleAsset.GetObj(9610) → 加载模型资源
```

### 赋值逻辑 (`BattlePlayerData.lua:50-69`)

对于 Monster (kind=1)：
```lua
self.id = sceneData.uid           -- 112101
self.job = sceneData.uid
self.jobId = sceneData.uid
local conf = BattleMonsterConfig.GetMonsterConfig(self.uid)
self.conf = conf
self.assetId = conf.model         -- 9610, 从怪物配置表的model字段读取

-- 变身状态下使用变身模型
if (sceneData:HasTransfiguration()) then
    self.assetId = sceneData:GetMonsterModel()
end
-- 审核模式下替换为统一模型
if(GameMain:IsServerModeReview() and BattleFun.ServerModeReviewMonsterId > 0) then
    self.assetId = BattleFun.ServerModeReviewMonsterId
end
```

### bodyId 的获取 (`BattlePlayerFashion.lua:999-1006`)
```lua
function BattlePlayer:GetBodyId()
    local suitKey = AvatarAssetConfig.GetAssetKeyIdArr()[AvatarAssetConfig.AssetPartType.SUIT]
    local suitId = self[suitKey]
    if suitId == 0 or not suitId then
        return self.assetId    -- 无时装，返回assetId
    end
    return suitId              -- 有时装，返回时装ID
end
```

### 总结
- **uid** = `sceneData.uid`，怪物配置ID
- **assetId** = `BattleMonsterConfig.GetMonsterConfig(uid).model`，怪物配置表的 model 字段
- **bodyId** = `GetBodyId()`，优先返回时装 suitId，没有时装则返回 assetId

---

## Q3: 特效(如FX_DragonPillarWorldBoss1_skill1_3_2_YingJi)是怎么生成的

### 核心结论
**特效名字不是代码拼出来的，是配置表 `EffectConfig.json` 里直接配好的。**

### 完整流程

```
技能释放
  → BattleSkillEffectConfig 查技能配置，得到 effectId（如 17919307）
  → EffectConfig.GetConf(17919307) 查特效配置表
  → 得到 eName = "FX_DragonPillarWorldBoss1_skill1_3_2_YingJi"
  → BattleSkillEffect.InstPar() 实例化
  → GlobalAsset.Load("fx/FX_DragonPillarWorldBoss1_skill1_3_2_YingJi") 加载预制体
  → 挂到目标挂点上显示
```

### 配置数据示例 (`EffectConfig.json`)
```json
"17919307": {
    "eName": "FX_DragonPillarWorldBoss1_skill1_3_2_YingJi",
    "eType": 10,
    "hPoint": 2,
    "delayTime": 0,
    "isSetChildNode": false,
    "isAnimBuffTime": 1,
    "isRandomDir": false,
    "_help": "龙21-移花接木-木属性板块特效",
    "id": "17919307"
}
```

### 关键代码

#### 1. 配置读取 (`EffectConfig.lua`)
```lua
-- :1800 获取特效配置
function EffectConfig.GetConf(effId)
    return effectDic[tostring(effId)]
end

-- :1804 获取特效名
function EffectConfig.GetEffectName(effectId)
    local conf = EffectConfig.GetConf(effectId)
    if (conf) then
        return conf.eName
    end
    return nil
end
```

#### 2. 技能特效ID映射 (`BattleSkillEffectConfig.lua`)
- `BattleSkillEffectConfig.GetReleaseEffectConfs()` (:243) — 根据技能获取释放阶段的 effectId 数组
- 技能配置中包含：`chantIds`(吟唱)、`releaseIds`(释放)、`ballisticIds`(弹道)、`hitIds`(命中)、`buffs`(Buff)

#### 3. 实例化 (`BattleSkillEffect.lua:318`)
```lua
function BattleSkillEffect.InstPar(effectId, source, target, destroyTime, pos, ...)
    local eConfig = EffectConfig.GetConf(effectId)
    local effectName = replaceEffectName or eConfig.eName
    local obj = GetEffectObj(effectName, eConfig.maxNum)  -- 从对象池获取/加载
    -- 设置挂点、位置、销毁时间等
end
```

#### 4. 资源加载路径
```lua
local effectPath = "fx/" .. effectName
-- 如: "fx/FX_DragonPillarWorldBoss1_skill1_3_2_YingJi"
local abTab, ab = GlobalAsset.Load(effectPath, nil)
```

### 关键文件

| 文件 | 作用 |
|------|------|
| `EffectConfig.json` | effectId → {eName, eType, hPoint...} 映射 |
| `BattleSkillEffectConfig.json` | 技能skillGroup → effectId数组 映射 |
| `EffectConfig.lua` | 读取特效配置，`GetConf()`, `GetEffectName()` |
| `BattleSkillEffectConfig.lua` | 读取技能特效配置，`GetReleaseEffectConfs()` |
| `BattleSkillEffect.lua` | 特效实例化、对象池管理，`InstPar()`, `Show()` |

### 挂点类型 (EffectConfig.lua HangPointType)
- 1: 头部 (Head)
- 2: 脚底 (Feet)
- 3: 中心 (Center)
- 6: 胸部 (Chest)
- 11: 武器特效点
- 12: 弹道武器发射点

---

## Q4: 板块常驻特效在哪里处理

### 核心结论

板块常驻特效有两条路径：
1. **AREA_EFFECT (ActionKind=119)** — `OnAreaWarningEffect`，适合有时限的区域特效
2. **BUFF_EFFECT (ActionKind=124)** — `OnAreaWarningEffectUpdate` / `OnEffectShow`，适合常驻特效（通过 OnEffectShow 创建，OnEffectHide 移除）

### AREA_EFFECT 路径 (`_HandleLogBattleEffect.lua:4016`)

```lua
function HandleLogBattleEffect.OnAreaWarningEffect(actionInfo, info)
    local modelKey = info.data[actionInfo.len + 1]   -- 特效模型key（用于标识和复用）
    local sourceKey = info.data[actionInfo.len + 2]   -- 来源key
    local kind = info.data[actionInfo.len + 3]        -- 特效种类
    local effectId = info.data[actionInfo.len + 4]    -- 特效配置ID
    local time = info.data[actionInfo.len + 5]        -- 持续时间（帧数）
    local angle = info.data[actionInfo.len + 6]       -- 角度
    local posX = info.data[actionInfo.len + 7]        -- 位置X
    local posZ = info.data[actionInfo.len + 8]        -- 位置Z
end
```

- `modelKey` 用于在 `areaWarningEffectDic` 中标识和复用特效
- `time` 控制持续时间，**time=-1 会导致特效不显示**（见 Q5）
- 适用场景：预警圈、范围指示器等有明确持续时间的区域特效

### BUFF_EFFECT 路径 — 常驻特效推荐方案

对于需要常驻显示的特效，使用 EFFECT_SHOW/EFFECT_HIDE 配对：
- **EFFECT_SHOW (ActionKind=13)**：`OnEffectShow` 创建特效并存入 `buffEffectInfoDic`
- **EFFECT_HIDE (ActionKind=14)**：`OnEffectHide` 从 `buffEffectInfoDic` 移除并销毁特效
- 特效生命周期由服务器控制，不受 time 参数限制

### 关键文件

| 文件 | 函数 | 作用 |
|------|------|------|
| `_HandleLogBattleEffect.lua` | `OnAreaWarningEffect` | AREA_EFFECT 处理 |
| `_HandleLogBattleEffect.lua` | `OnEffectShow` | 创建常驻特效 |
| `_HandleLogBattleEffect.lua` | `OnEffectHide` | 移除常驻特效 |
| `EffectConfig.lua` | `areaWarningEffect` | 区域特效配置表 |

---

## Q5: AREA_EFFECT time=-1 为什么不显示特效

### 根本原因

`AREA_EFFECT` 路径中，`time=-1` 会导致特效被立即销毁，无法显示。

### 具体原因链

1. **`BattleFun.CheckFrameTime(time)` 返回 false**
   - 当 `time <= 0` 时，`CheckFrameTime` 判定为无效时间，跳过特效创建
   - 即使创建了，`endTime = curTime + time` 计算出的 `endTime` 为负数或0

2. **`AddEffectInfo` 中的销毁逻辑**
   ```lua
   -- endTime <= 0 或 endTime <= curTime 时，特效立即被标记为过期
   -- 下一帧检查时直接销毁
   ```

3. **设计意图**：AREA_EFFECT 是为有时限的区域预警设计的，不支持 time=-1 的"永久"语义

### 解决方案

如果需要常驻特效，不要用 `AREA_EFFECT`，改用 `EFFECT_SHOW` (ActionKind=13) + `EFFECT_HIDE` (ActionKind=14) 路径。特效的生命周期由服务器通过 SHOW/HIDE 配对控制，不依赖 time 参数。

---

## Q6: 客户端怎么知道地图的area index

### 数据来源

area index 完全由服务器驱动，客户端从两个地方获取：

#### 1. 初始化阶段 — `sceneData.attrs`

```lua
-- BattlePlayerData.lua:95
for _, v in ipairs(sceneData.attrs) do
    if v.kind == 1 then
        self.areaId = v.param1    -- kind=1 表示区域ID属性
    end
end
```

在 `CreateSceneData()` 阶段，服务器发送的场景数据中 `attrs` 数组包含 `kind=1` 的条目，`param1` 即为初始 area index。

#### 2. 战斗中更新 — AREA_MOVE 战报 (ActionKind=35)

```lua
-- BattleEffectRaid.lua:1276 SetMaskTime()
-- 收到 AREA_MOVE 战报后更新 curIndex
curIndex = newAreaIndex
```

服务器发送 `AREA_MOVE` (ActionKind=35) 战报通知客户端当前区域发生变化，客户端更新 `curIndex`。

#### 3. 玩家操作触发 — 区域选择回调

```lua
-- BattleEffectRaid.lua:477 OnSelectPathCallback()
-- 玩家点击移动按钮后，客户端计算目标 area index 发给服务器
```

玩家在UI上选择移动方向后，客户端根据当前 index 和方向计算目标 index，发送给服务器，服务器验证后通过 AREA_MOVE 战报确认。

---

## Q7: 客户端怎么移动area index

### 移动坐标来源

area 移动的目标坐标全部由服务器提供，客户端不自行计算坐标。

#### 服务器下发坐标的战报

1. **DISPLACEMENT (ActionKind=29)** — 位移战报
   - 服务器直接给出目标世界坐标 (x, y, z)
   - 客户端执行位移动画到目标位置

2. **Instance 战报** — 实例化/刷新位置
   - 包含实体的新坐标信息
   - 客户端直接设置位置

3. **AREA_MOVE (ActionKind=35)** — 区域移动
   - 更新 `curIndex` 为新区域
   - 触发区域切换动画

4. **AREA_MOVE_END (ActionKind=36)** — 区域移动结束
   - 标记移动完成

#### 移动动画 (`BattleEffectRaid.lua:1164`)

```lua
-- RoleRotateSurroundCenterSync() 处理环形移动动画
-- 客户端根据服务器给的目标位置，插值执行移动动画
```

### 总结

客户端是纯表现层：
- 服务器决定移动到哪里（坐标）
- 服务器决定移动到哪个区域（index）
- 客户端只负责执行动画和更新显示

---

## Q8: 哪些战报触发 CheckLayerEffect 控制特效分层显隐

### 核心结论

`BattleSkillEffect.CheckLayerEffect` 在 **2个战报** 的处理函数中被调用，共 **4个调用点**。

### 触发战报总览

| 战报 | ActionKind | 处理函数 | layer 来源 |
|------|------------|----------|-----------|
| **EFFECT_SHOW** | 13 | `OnEffectShow` | 战报数据中的 layer 字段 |
| **SHOW_LINE_EFFECT** | 121 | `OnShowLineEffect` | 连线参数数组的第3个元素 (linePlayerKey[3]) |

### 1. EFFECT_SHOW (ActionKind=13) — `OnEffectShow`

#### 调用点 A — line 2297：已存在的特效升级层级

```lua
-- buffEffectInfoDic 里已有该 effectKey，且 layer > 1
if (info ~= nil and layer > 1) then
    BattleSkillEffect.CheckLayerEffect(effectId, info.effectTra, layer)
    return  -- 不重新创建特效，只更新层级
end
```

场景：服务器再次发送同一个 effectKey 的 EFFECT_SHOW 战报，但 layer 值变大了（如 buff 叠层从1→2→3）。不会重建特效，只更新子节点显隐。

#### 调用点 B — line 2439：新创建或重新显示特效

```lua
GlobalFun.SetObj(info.effectObj, true)
BattleSkillEffect.CheckLayerEffect(effectId, info.effectTra, layer)
```

场景：特效首次创建或之前被隐藏后重新显示，创建完成后立即根据 layer 设置子节点状态。

### 2. SHOW_LINE_EFFECT (ActionKind=121) — `OnShowLineEffect`

#### 调用点 C — line 3464：已存在的连线特效更新层级

```lua
if lineInfo then
    local effectTra = lineInfo.effTra
    BattleSkillEffect.CheckLayerEffect(effectId, effectTra, lineLayer)
    -- lineLayer 来自 linePlayerKey[3]
end
```

#### 调用点 D — line 3483：新创建连线特效

```lua
local effectTra = BattleSkillEffect.ShowLine(source, player, effectId, time)
if (effectTra) then
    BattleSkillEffect.CheckLayerEffect(effectId, effectTra, lineLayer)
end
```

连线特效的 `lineLayer` 来自服务器战报数据 `info.data` 中连线参数数组的第3个元素。

### CheckLayerEffect 分层逻辑 (`BattleSkillEffect.lua:467`)

通过 `EffectConfig.IsLayerEffect(effId)` 查 `layerEffectArr` 表获取 flag 值。effectId 不在表中则跳过。

特效预制体内部需包含 `LV01`、`LV02`、`LV03`... 子节点：

| flag | 行为 | 示例 (layer=2) |
|------|------|----------------|
| **1** | 精确匹配：只显示 `idx == layer` | LV01隐, **LV02显**, LV03隐 |
| **2** | 累加显示：显示 `idx <= layer` | **LV01显, LV02显**, LV03隐 |
| **3** | 条件累加：显示 `idx <= layer` 且 `layer > 1` | **LV01显, LV02显**, LV03隐 (layer=1时全隐) |
| **4** | 文本显示：设置 `Loop/Text` 文字为 layer 值 | Text = "2" |

### 配置方式

`layerEffectArr` 在 `EffectConfig.lua:93-281` 中硬编码：
```lua
layerEffectArr["1920831"] = 2   -- flag=2 累加显示
layerEffectArr["1790871"] = 1   -- flag=1 精确匹配
layerEffectArr["1795044"] = 3   -- flag=3 条件累加
layerEffectArr["1795256"] = 4   -- flag=4 文本显示（移动步数）
```

### 重要说明

**没有专门的"修改层级"战报**。层级变化是通过再次发送 EFFECT_SHOW 战报（相同 effectKey，新 layer 值）来实现的，走调用点 A 的分支。

---

## Q9: OnEffectShow (EFFECT_SHOW ActionKind=13) 详细介绍

### 战报触发

由 **EFFECT_SHOW (ActionKind=13)** 战报触发，服务器通知客户端显示一个 Buff 特效。代码位于 `_HandleLogBattleEffect.lua:2184-2463`。

### 第一阶段：解析参数 (line 2185-2203)

服务器一共发送 **19个参数**：

| 序号 | 字段 | 说明 |
|------|------|------|
| 1 | `effectKey` | 特效唯一标识，用于存入 `buffEffectInfoDic` |
| 2 | `effectId` | 特效配置ID，对应 `EffectConfig.json` |
| 3 | `skillId` | 来源技能ID |
| 4 | `targetKey` | 目标实体key（特效挂在谁身上） |
| 5 | `buffIcon` | Buff图标ID，>0 时显示头顶图标 |
| 6 | `layer` | 层级/叠层数，用于 CheckLayerEffect |
| 7 | `buffType` | Buff类型（如眩晕329等） |
| 8 | `sourceKey` | 来源实体key（谁释放的） |
| 9-11 | `x, y, z` | 特效世界坐标（×accuracy 转换） |
| 12 | `maxBuffTime` | Buff最大持续时间 |
| 13 | `curBuffTime` | Buff当前剩余时间 |
| 14 | `modId` | 模块ID |
| 15 | `buffEffectKey` | 关联Buff特效key（用于链接类特效） |
| 16 | `needForceSetPos` | =1 时强制设置位置 |
| 17-18 | `setX, setZ` | 强制设置的坐标 |
| 19 | `eulerAnglesY` | 强制设置的Y轴旋转角 |

### 第二阶段：前置检查与早退 (line 2204-2276)

```
解析参数
  → BattleStaticsData.ReceiveBuffInfo()    统计记录
  → player == nil?                         → return（目标不存在）
  → buffType == 329 且是主角?               → SetMainBuffState → return（特殊UI状态）
  → buffType > 0 且是主角?                  → SetBuffState（更新技能栏Buff状态）
  → buffType == Dizzy?                     → PlayStunAnim()（播放眩晕动画）
  → 特效屏蔽检查                            → 被屏蔽则 return（但Boss的buffIcon仍显示）
  → buffIcon > 0?                          → ShowBuff 显示头顶图标
  → IsRoosterEffect?                       → ShowRooster → return（变身鸡）
  → IsTransformEffect?                     → ShowTransform → return（变身特效）
  → effectId == 0?                         → return（无特效只有Buff状态）
  → effConf == nil?                        → return（配置不存在）
  → effConf.eType < BUFF?                  → return（特效类型必须是BUFF或FOLLOW_BUFF）
```

#### 特效屏蔽逻辑 (line 2223-2253)
- `EffectConfig.IsAvoidEffectArr(effectId)` — 白名单特效，不屏蔽
- `IsSelfOnlyEffect` — 怪物Buff只有主角能看到，非主角直接 return
- `SettingsData.BlockEffect(player)` — 玩家设置了屏蔽其他玩家特效
- **被屏蔽时**：如果目标是Boss且有buffIcon，仍然显示头顶图标（不显示特效本体）

### 第三阶段：特效已存在 + layer > 1 → 只更新层级 (line 2289-2300)

```lua
local modKey = tostring(effectKey)
local info = buffEffectInfoDic[modKey]
if (info ~= nil and layer > 1) then
    -- 可选：强制刷新位置
    if needForceSetPos == 1 and EffectConfig.IsRefreshPosEffect(effectId) then
        Transform.SetPosition(info.effectTra, setX, ...)
        TransformExtension.SetEulerAngles(info.effectTra, ...)
    end
    BattleSkillEffect.CheckLayerEffect(effectId, info.effectTra, layer)
    player:PlayerLayerEffectSound(source, effectId, layer)
    return  -- 不重建，直接返回
end
```

**场景**：Buff叠层。服务器对同一个 effectKey 再次发 EFFECT_SHOW，layer 从1变成2、3...。不销毁重建，只切换 LV01/LV02/LV03 子节点显隐。

### 第四阶段：创建新特效 (line 2301-2323)

```lua
if (info == nil) then
    local effectName = EffectConfig.GetEffectMapName(sourceKey, effectId) or effConf.eName
    info = {}
    info.key = effectKey
    info.effectId = effectId
    info.player = player
    info.effectName = effectName
    info.progressEffect = effConf.isProgress
    info.effectTra = BattleSkillEffect.ShowBuff(source, player, effectId, nil, effectName)
    -- ShowBuff 内部: 从对象池获取或加载预制体 → 挂到目标挂点
    if (info.effectTra == nil) then return end
    info.againEffect = false
    info.effectObj = info.effectTra.gameObject
    info.initPos = { x=initX, y=initY, z=initZ }
    buffEffectInfoDic[modKey] = info   -- 存入全局字典
end
```

首次出现该 effectKey 时，创建新的 info 表并存入 `buffEffectInfoDic`。

### 第五阶段：特效类型分支处理 (line 2325-2437)

根据 `effConf.eType` 分三种类型：

#### A. Chains 链接类型 (line 2326-2377)
- 护盾链接效果
- 获取 `LineRenderer` 组件
- 设置 source 和 target 两端的连线端点
- 支持触手配置 (`TentacleEffectConf`)、怪物连线配置 (`MonsterLineConf`)

#### B. BUFF 不跟随类型 (line 2380-2413)
- `eType == EffectType.BUFF`
- 特效不跟随角色移动，放在世界坐标位置
- 处理旋转（`IsNotRotateEffectArr` 可跳过）
- 处理偏移位置（`IsFlagEffectArr`、`GetNotFollowBuffEffectOffset`）
- 骑乘状态特殊处理
- 不跟随Y轴（`IsNotFollowSourcePosYEffect`）

#### C. FOLLOW_BUFF 跟随类型 (line 2414-2426)
- `eType == EffectType.FOLLOW_BUFF`
- `SetBuffEffectPos` 将特效挂到角色挂点上
- 跟随角色移动

#### 三种类型的共同后处理 (line 2427-2436)
```lua
SetPenetrateAreaEffect()     -- 穿透区域特效处理
CheckFallEffect()            -- 坠落特效（如陨石）
CheckCountDownEffect()       -- 倒计时特效
PlayEffectSound()            -- 音效（非首次时播放）
```

### 第六阶段：最终设置 (line 2438-2462)

```lua
GlobalFun.SetObj(info.effectObj, true)                    -- 显示特效
BattleSkillEffect.CheckLayerEffect(effectId, info.effectTra, layer)  -- 分层显隐
player:PlayerLayerEffectSound(source, effectId, layer)     -- 分层音效
ResetEffectLayer(effectId, info.effectObj)                  -- 重置Unity Layer

-- 特殊BuffIcon处理
if (effectId > 0 and buffIcon > 0 and specialBuffIconId[...]) then
    CheckSpecialBuffIconId(...)
end

-- 相机特效（主角专属，挂到相机上）
if player:IsMainRole() and IsCameraEffect(effectId) then
    GlobalFun.SetParent(tra, cameraTra, true)
end

-- 强制位置设置
if needForceSetPos == 1 then
    Transform.SetPosition(info.effectTra, setX, ...)
    TransformExtension.SetEulerAngles(info.effectTra, ...)
end
```

### 完整流程图

```
EFFECT_SHOW 战报 (ActionKind=13)
  │
  ├─ 解析19个参数
  ├─ 统计记录 (BattleStaticsData)
  │
  ├─ player 不存在? → return
  ├─ buffType==329 主角? → SetMainBuffState → return
  ├─ buffType>0 主角? → SetBuffState + SetBuffState
  ├─ buffType==Dizzy? → PlayStunAnim
  │
  ├─ 特效屏蔽检查
  │   ├─ 白名单 → 不屏蔽
  │   ├─ 仅自己可见 且非主角? → return
  │   └─ 玩家设置屏蔽? → Boss头顶图标仍显示 → return
  │
  ├─ buffIcon>0? → 显示头顶图标
  ├─ 变身鸡? → return
  ├─ 变身特效? → return
  ├─ effectId==0? → return
  ├─ 配置不存在? → return
  ├─ eType < BUFF? → return (类型错误)
  │
  ├─ buffEffectInfoDic 已有 且 layer>1?
  │   └─ 只更新层级 (CheckLayerEffect) → return
  │
  ├─ buffEffectInfoDic 没有?
  │   └─ 创建新 info, ShowBuff 加载特效, 存入字典
  │
  ├─ 按 eType 分支:
  │   ├─ Chains → 链接连线
  │   ├─ BUFF → 世界坐标位置
  │   └─ FOLLOW_BUFF → 挂角色身上
  │
  └─ 最终:
      ├─ SetObj 显示
      ├─ CheckLayerEffect 分层
      ├─ ResetEffectLayer Unity层级
      ├─ 相机特效挂载
      └─ 强制位置设置
```

### 配对战报 OnEffectHide (ActionKind=14, line 2472)

用于移除特效，从 `buffEffectInfoDic` 中删除并销毁 GameObject。与 OnEffectShow 配对使用控制特效的完整生命周期。

### 关键数据结构

#### buffEffectInfoDic 存储的 info 表
```lua
info = {
    key = effectKey,           -- 特效唯一标识
    effectId = effectId,       -- 特效配置ID
    player = player,           -- 目标 BattlePlayer
    effectName = effectName,   -- 特效资源名
    progressEffect = bool,     -- 是否进度特效
    effectTra = Transform,     -- Unity Transform
    effectObj = GameObject,    -- Unity GameObject
    initPos = {x, y, z},       -- 初始位置
    againEffect = bool,        -- 是否非首次（控制音效）
}
```

---

## Q10: EFFECT_OBJ_SET (ActionKind=83) 介绍

### 用途

控制特效内部子节点的显隐，注释说明目前用于**阴阳怪器**。

### 处理函数

`HandleLogBattleEffect.OnEffectSetObject` (`_HandleLogBattleEffect.lua:4885`)

### 参数

| 序号 | 字段 | 说明 |
|------|------|------|
| 1 | `effectKey` | 特效唯一标识，从 `buffEffectInfoDic` 查找对应特效 |
| 2 | `kind` | 操作类型，目前只有 `kind==1` |
| 3 | `val1` | 第一组子节点显示数量 |
| 4 | `val2` | 第二组子节点显示数量 |

### 逻辑

```lua
function HandleLogBattleEffect.OnEffectSetObject(actionInfo, info)
    local effectKey = info.data[actionInfo.len + 1]
    local kind = info.data[actionInfo.len + 2]
    local val1 = info.data[actionInfo.len + 3]
    local val2 = info.data[actionInfo.len + 4]
    local effectInfo = buffEffectInfoDic[tostring(effectKey)]
    if effectInfo and not GlobalFun.IsNull(effectInfo.effectTra) then
        if kind == 1 then
            UpdatePoints(effectInfo.effectTra, "Model/Points01", val1)
            UpdatePoints(effectInfo.effectTra, "Model/Points02", val2)
        end
    end
end
```

**kind==1** 时，对特效预制体下的两组子节点分别更新：
- `Model/Points01` — 用 val1 控制
- `Model/Points02` — 用 val2 控制

### UpdatePoints 函数 (line 4874)

```lua
local function UpdatePoints(effectTra, pointName, val)
    local pointTra = GlobalFun.GetTra(effectTra, pointName)
    if pointTra then
        for i = 1, 6 do
            local pTra = GlobalFun.GetObj(pointTra, "FX_" .. i)
            if pTra then
                GlobalFun.SetObj(pTra, val >= i)  -- i <= val 则显示，否则隐藏
            end
        end
    end
end
```

遍历 `FX_1` 到 `FX_6` 共6个子节点，**val >= i 则显示**。

例如 `val1=3`：
- `Model/Points01/FX_1` → 显示 (3>=1)
- `Model/Points01/FX_2` → 显示 (3>=2)
- `Model/Points01/FX_3` → 显示 (3>=3)
- `Model/Points01/FX_4` → 隐藏 (3<4)
- `Model/Points01/FX_5` → 隐藏
- `Model/Points01/FX_6` → 隐藏

### 与 CheckLayerEffect 的区别

| | CheckLayerEffect | UpdatePoints |
|--|---|---|
| 触发战报 | EFFECT_SHOW(13), SHOW_LINE_EFFECT(121) | EFFECT_OBJ_SET(83) |
| 子节点命名 | `LV01`, `LV02`... | `FX_1`, `FX_2`... |
| 配置驱动 | `layerEffectArr` 中的 flag 决定显隐策略 | 固定累加策略 (val >= i) |
| 分组 | 无分组 | 两组 Points01/Points02 分别控制 |
| 最大层数 | 无限制 (遍历到找不到为止) | 固定6层 |

### 前置条件

特效必须先通过 `EFFECT_SHOW` 战报创建并存入 `buffEffectInfoDic`，否则 `effectInfo` 为 nil 直接跳过。

---

## Q11: OnEffectShow 添加的调试日志

### 修改文件

`_HandleLogBattleEffect.lua` — `HandleLogBattleEffect.OnEffectShow` 函数 (line 2184)

### 日志标签

所有日志使用 `[OnEffectShow]` 前缀。

### 日志位置与内容

| 位置 | 触发条件 | 日志内容 |
|------|----------|----------|
| 入口 (参数解析后) | 每次进入 | `effectKey, effectId, skillId, targetKey, sourceKey, layer, buffType, buffIcon, pos, needForceSetPos` |
| player 检查 | `player == nil` 或 `IsAnimNil()` | `return: player不存在或AnimNil, targetKey` |
| buffType==329 | 主角 + buffType==329 + effectId>0 | `return: buffType==329 主角特殊UI, effectKey` |
| SelfOnlyEffect | 怪物Buff + 非主角 | `return: SelfOnlyEffect且非主角, effectKey, effectId` |
| 特效屏蔽 | `SettingsData.BlockEffect` 返回 true | `return: 特效被屏蔽, effectKey, effectId` |
| 变身鸡 | `IsRoosterEffect` | `return: RoosterEffect变身鸡, effectKey` |
| 变身特效 | `IsTransformEffect` | `return: TransformEffect变身, effectKey, effectId` |
| effectId==0 | effectId 为 0 | `return: effectId==0, effectKey` |
| 配置为空 | `effConf == nil` 或 `eName` 为空 | `return: effConf为nil或eName为空, effectKey, effectId, skillId` |
| eType 错误 | `eType < EffectType.BUFF` | `return: eType类型错误, effectKey, effectId, eType` |
| 层级更新 | `info ~= nil and layer > 1` | `特效已存在且layer>1, 只更新层级, effectKey, effectId, layer` |
| ShowBuff 失败 | `ShowBuff` 返回 nil | `return: ShowBuff返回nil, effectKey, effectId, effectName` |
| 新建特效 | `info == nil` 首次创建 | `新建特效, effectKey, effectId, effectName, eType` |
| 复用特效 | `info ~= nil` 且 `layer <= 1` | `特效已存在复用, effectKey, effectId, layer` |
| 完成显示 | 走完全部流程 | `完成显示, effectKey, effectId, layer, eType` |

### 用途

用于排查特效不显示的问题，通过日志可以精确定位 OnEffectShow 在哪个分支提前返回，或确认特效是否成功创建。

---

## Q12: CheckLayerEffect flag 枚举含义

### 配置位置

`EffectConfig.lua:92-96` 注释 + `layerEffectArr` 表（line 96-281）

```lua
-- 1:有多少层显示多少层
-- 2:每层叠加的特效显示
-- 3:层数大于1 有多少层显示多少层
-- 4:根据层数修改Loop/Text的值
local layerEffectArr = {} --多层特效开关
```

### 代码实际逻辑 (`BattleSkillEffect.lua:467`)

以代码 `CheckLayerEffect` 的实际行为为准：

| flag | 注释原文 | 实际行为 | 判断条件 |
|------|----------|----------|----------|
| **1** | 有多少层显示多少层 | **精确匹配**：只显示 `idx == layer` 的节点 | `idx == layer` |
| **2** | 每层叠加的特效显示 | **累加显示**：显示所有 `idx <= layer` 的节点 | `idx <= layer` |
| **3** | 层数大于1 有多少层显示多少层 | **条件累加**：累加但 layer=1 时全隐 | `idx <= layer and layer > 1` |
| **4** | 根据层数修改Loop/Text的值 | **文本模式**：不控制LV子节点，设置文字 | 设置 `Loop/Text.text = layer` |

### 显隐示例（假设预制体有 LV01-LV05）

#### flag=1 精确匹配

| layer | LV01 | LV02 | LV03 | LV04 | LV05 |
|-------|------|------|------|------|------|
| 1 | 显示 | 隐藏 | 隐藏 | 隐藏 | 隐藏 |
| 2 | 隐藏 | 显示 | 隐藏 | 隐藏 | 隐藏 |
| 3 | 隐藏 | 隐藏 | 显示 | 隐藏 | 隐藏 |

#### flag=2 累加显示

| layer | LV01 | LV02 | LV03 | LV04 | LV05 |
|-------|------|------|------|------|------|
| 1 | 显示 | 隐藏 | 隐藏 | 隐藏 | 隐藏 |
| 2 | 显示 | 显示 | 隐藏 | 隐藏 | 隐藏 |
| 3 | 显示 | 显示 | 显示 | 隐藏 | 隐藏 |

#### flag=3 条件累加

| layer | LV01 | LV02 | LV03 | LV04 | LV05 |
|-------|------|------|------|------|------|
| 1 | **隐藏** | 隐藏 | 隐藏 | 隐藏 | 隐藏 |
| 2 | 显示 | 显示 | 隐藏 | 隐藏 | 隐藏 |
| 3 | 显示 | 显示 | 显示 | 隐藏 | 隐藏 |

### flag=1 与 flag=3 的关键区别

- **flag=1**：layer=1 时显示 LV01
- **flag=3**：layer=1 时**全部隐藏**（多了 `layer > 1` 条件），layer≥2 才开始显示

flag=3 适用于"1层时不需要分层特效，2层以上才出现视觉变化"的场景。

### 配置示例

```lua
layerEffectArr["1920831"] = 2   -- 累加显示
layerEffectArr["1790871"] = 1   -- 精确匹配
layerEffectArr["1795044"] = 3   -- 条件累加（最强魔物头顶）
layerEffectArr["1795256"] = 4   -- 文本显示（沙蝎移动步数）
```

---

## Q13: 地龙位移后Boss怎么跟随玩家移动旋转

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

## Q14: man_dragon21EnergyVal UI显示由哪个战报控制

### 战报来源

由 **CHANGED_DOMB_VAL (ActionKind=80)** 战报控制，BuffType kind 为 **Dragon21EnergyVal (1743)**。

### UI 组件定义

`_BattleEffectData.lua:756-761`：
```lua
pool.dragon21EnergyObj = GlobalFun.GetObj(pool.tra, "man_dragon21EnergyVal")
pool.dragon21EnergyTra = pool.dragon21EnergyObj.transform
pool.dragon21EnergyMaskImg = GlobalFun.GetImg(pool.dragon21EnergyTra, "fill")
pool.dragon21EnergyEffectObj = GlobalFun.GetObj(pool.dragon21EnergyTra, "fill_full")
pool.dragon21EnergyMaskText = GlobalFun.GetText(pool.dragon21EnergyTra, "tex_fill",true)
```

### 数据存储

`BattlePlayerData.lua:140`：
```lua
self.monsterDragon21EnergyVal = 0      -- 初始值为0
self.monsterDragon21EnergyMaxVal = 0
```

BuffType 枚举（`BattleSkillEnum.lua:57`）：
```lua
Dragon21EnergyVal = 1743, -- 龙21能量值
```

### 战报处理链路

`_HandleLogBattle.lua:1470` `OnBattleValChanged()`：
```lua
-- ActionKind=80 战报参数
local key = info.data[actionInfo.len + 1]      -- Boss实体key
local val = info.data[actionInfo.len + 2] * accuracy  -- 当前能量值
local maxVal = info.data[actionInfo.len + 3]   -- 最大能量值
local kind = info.data[actionInfo.len + 4]     -- BuffType类型
local skillId = info.data[actionInfo.len + 5]  -- 技能ID

-- kind == 1743 (Dragon21EnergyVal) 分支 (_HandleLogBattle.lua:1542)
elseif BattleSkillEnum.BuffType.Dragon21EnergyVal == kind then
    player:SetMonsterDragon21EnergyVal(val)
    player:SetMonsterDragon21EnergyMaxVal(maxVal)
    if (player:IsNotAnimNil()) then
        BattleEffect.ShowBossHp(player)    -- 刷新Boss HP UI（包含能量条）
    end
```

### UI 显示逻辑

`BattleEffect.lua:2956-2997` `ShowBossHp()` 内部：

```lua
local dragon21EnergyVal = player:GetMonsterDragon21EnergyVal()
local defaultDragonObjList = {pool.dragon21EnergyObj, pool.dragon22EnergyObj, pool.dragon23EnergyObj}
local instanceID = BattleMapComponent.DataManager.MapData.GetInstanceId()
local index = BattleEffectConst.DragonWordBossUI[instanceID]

-- 根据副本instanceID决定显示哪个UI（dragon21/22/23三选一）
for i,v in ipairs(defaultDragonObjList) do
    if index~=nil and index == i then
        GlobalFun.SetObj(v, dragon21EnergyVal > 0)   -- 值>0才显示
    else
        GlobalFun.SetObj(v, false)                    -- 其余隐藏
    end
end

-- 值>0时更新进度条
if (dragon21EnergyVal > 0) then
    local maxDef = player:GetMonsterDragon21EnergyMaxVal()
    local efillAmount = dragon21EnergyVal / maxDef
    -- 根据index选择更新dragon21/22/23对应的fill和文本
end
```

### 初始化显示控制

1. **初始默认隐藏**：`monsterDragon21EnergyVal` 初始值为 `0`，`ShowBossHp` 中 `dragon21EnergyVal > 0` 为 false，UI 隐藏
2. **首次显示触发**：服务器发送第一条 `ActionKind=80, kind=1743` 战报且 `val > 0` 时，`SetMonsterDragon21EnergyVal(val)` 赋值后调用 `ShowBossHp`，UI 显示
3. **副本匹配**：`BattleEffectConst.DragonWordBossUI[instanceID]` 根据副本 ID 决定显示 dragon21/22/23 中的哪一个，不在表中则全部隐藏

### 总结

- **战报**：`CHANGED_DOMB_VAL (ActionKind=80)`，kind=`1743`
- **初始化**：默认隐藏，完全由服务器首条战报驱动首次显示
- **显示条件**：`dragon21EnergyVal > 0` 且 `DragonWordBossUI[instanceID]` 存在对应 index
- **dragon21/22/23 共用同一套数据**，通过副本 instanceID 区分使用哪个 UI 皮肤
