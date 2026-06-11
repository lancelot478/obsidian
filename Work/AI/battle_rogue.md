# 目录

- [[#Q1: TTDBL-41514 肉鸽1.0开发提交做了哪些内容？]]
- [[#Q2: 详细介绍 Battle/OperBattle 战斗逻辑]]
- [[#Q3: 怎么实现事件暂停和手动暂停的？]]
- [[#Q4: 客户端战报驱动下的战斗逻辑是怎么进行的？]]

## Q1: TTDBL-41514 肉鸽1.0开发提交做了哪些内容？

### 结论



`TTDBL-41514 【系统】【服务器】肉鸽1.0开发单` 是一组肉鸽/英灵卡片 1.0 的完整后端开发提交，范围不是单点功能，而是一套活动玩法闭环：配置加载、玩家持久化、客户端协议、队伍创建与重连、战斗服帧逻辑、战报上传与反作弊、死斗/故事排行榜、奖励结算、符文系统、商店、GM 工具和 Web GM 后台。

### 提交规模

- 共定位到 69 个匹配 `TTDBL-41514` 的 git 提交。
- 主要改动目录集中在 `internal/`、`proto/`、`tools/`、`tests/`。
- 高频文件包括：
  - `internal/operbattle/oper_battle_rogue.go`
  - `internal/data/rogue.go`
  - `internal/db/model/rogue.go`
  - `internal/db/model/rogue_api.go`
  - `internal/service/game/rogue.go`
  - `internal/scene/rogue/rogue.go`
  - `proto/pb/game/rogue.proto.txt`
  - `proto/pb/rpc/rpc_rogue.proto`
  - `proto/pb/rpc/rpc_game_rogue.proto`
  - `proto/pb/rpc/rpc_center.proto`

### 客户端协议

- 新增 MID 78：`RogueAID`。
- 客户端 AID 覆盖 0 到 23：
  - 活动总览：获取活动信息、死斗信息、故事信息。
  - 英雄：解锁英雄、获取英雄、英雄卡池十连抽。
  - 队伍：创建队伍、登录队伍。
  - 战斗交互：选择英雄、刷新英雄、选择技能、刷新技能、上传战报。
  - 死斗：每日奖励、全服奖励、排行榜。
  - 故事：故事排行榜、关卡通关信息。
  - 符文：获取符文、清除新符文标记、设置符文空间、解锁符文格子。
  - 商店：获取商店、设置兑换提醒、购买商品。
- 公共协议补充了 `RogueTeamInfo`、`RogueSceneData`、`RoguePlayerSceneData`、技能统计、战斗上传日志、排行榜战斗信息、符文掉落与符文空间结构。
- 推送协议新增死斗和故事结算推送，用于客户端收到战斗奖励、波次、英雄、技能、伤害、符文、通关状态等结果。

### 数据与配置

- 新增肉鸽配置加载入口 `internal/data/rogue.go`。
- 配置类型覆盖：
  - 活动参数、地图、怪物、怪物组、怪物等级、波次。
  - 英雄、英雄抽取卡池、技能、技能组、技能刷新。
  - 符文、符文模型、符文阶段、符文空间。
  - 死斗活动、故事活动、任务活动、商店、事件表。
- 活动开放逻辑区分整个活动、死斗模式开放天数、故事模式开放天数、故事地图开放天数与前置地图通关要求。

### 玩家持久化

- `RogueInfo` 保存玩家肉鸽活动状态，落 Redis/MySQL：
  - 活动 ID、活动开启时玩家等级。
  - 已拥有英雄、英雄抽卡数据。
  - 当前队伍模式、队伍 ID、队伍 SID。
  - 死斗每日累计伤害、每日奖励领取状态、全服奖励领取状态。
  - 每日 rogue 币获取数量。
  - 故事地图通关、首通、完美通关、最短通关时间。
  - 符文背包、符文空间、符文格子、符文阶段。
  - 商店购买记录与提醒状态。
  - 作弊次数、榜单封禁状态。
- 新增 `RogueTeam` 保存肉鸽队伍数据，包括模式、成员初始可选英雄、成员装备符文。

### Game 服业务

- `internal/service/game/rogue.go` 实现客户端请求入口。
- 创建队伍时会：
  - 校验活动是否开启、模式是否开放、故事地图是否解锁。
  - 检查玩家是否已在其他副本队伍中。
  - 根据模式确定地图，死斗随机地图，故事使用客户端传入地图。
  - 走跨服 zone 规则，选择 Rogue/Team RPC host。
  - 创建队伍后写回玩家队伍信息并更新大厅。
- 战斗中选择英雄、刷新英雄、选择技能、刷新技能通过 common instance 的自定义操作转发到战斗队伍逻辑。
- 战报上传由 game 转发到 rogue/team，再转发到 operbattle。
- 结算时区分死斗与故事：
  - 死斗累计每日伤害，处理战斗奖励、每日奖励、任务进度、结算推送。
  - 故事写入通关、首通、完美通关、最短通关时间，按关卡推进符文阶段，处理通关奖励与结算推送。
  - 作弊战斗不发正常收益，并增加作弊计数。

### Rogue/Team 服

- 新增 `internal/scene/rogue` 队伍场景。
- 队伍支持创建、登录、重连、进入战斗、战斗结束广播、队伍大厅更新。
- 创建队伍时会从玩家已拥有英雄中随机初始可选英雄，并同步玩家已装备符文。
- 战斗中断线重登时，如果队伍仍在战斗中，会返回战斗场景数据。
- 活动结束或 battle 心跳异常时，队伍会触发解散/清理。

### Battle/OperBattle 战斗逻辑

- `internal/operbattle/oper_battle_rogue.go` 新增肉鸽战斗运行时。
- 运行时维护：
  - 当前波次、已通过波次、波次刷新/击杀/经验拾取统计。
  - 战斗帧数、暂停状态、最大战斗时长、最大暂停时长。
  - 总伤害、总怪物血量、刷新怪物数量。
  - 玩家英雄、技能、经验等级、掉落奖励、技能伤害统计。
  - 事件状态与事件队列。
- 战报支持客户端上传：
  - 战斗结束。
  - 技能伤害。
  - 当前波次。
  - 暂停/继续。
  - 击杀怪物、拾取怪物经验。
  - 波次通过。
  - 刷新怪物。
  - 事件结束。
- 事件系统支持掉落宝箱、掉落技能、多选一技能、转盘奖励、技能召唤人机等类型。
- 反作弊主要围绕非法选择、波次/怪物统计不一致、伤害或结算数据异常等场景，命中后标记 `isCheat` 并影响结算。

### 排行榜与 Center 服

- Center 增加 Rogue 赛季与排行 RPC：
  - 死斗排行增加/获取。
  - 故事排行增加/获取。
  - 守卫排行预留/实现获取与增加。
  - 获取赛季信息。
  - GM 删除排行。
- `internal/db/repo/rogue_rank.go` 维护跨区 zone 下的赛季、死斗排行、故事排行、守卫排行。
- 新活动期会重置赛季数据与相关排行榜。
- 死斗排行按总伤害，故事排行按地图通关时间，排行信息包含队伍成员、英雄、主动/被动技能、符文和战斗配置。

### 符文系统

- 玩家有符文背包与符文空间。
- 符文背包记录总拥有数量、当前可用数量、是否新获得。
- 符文空间记录符文 ID 与坐标。
- 设置符文空间前会用 `RogueRuneSpaceBox` 校验：
  - 坐标是否在已解锁格子内。
  - 符文摆放是否合法。
  - 背包数量是否足够。
- 解锁符文格子会校验当前符文阶段、坐标是否可解锁、是否重复解锁，并扣除配置指定道具。
- 故事关卡进度会推动符文阶段升级。

### 商店与掉落

- 肉鸽商店基于 `rogue_shop.json`。
- 支持获取商品、设置兑换提醒、购买商品。
- 商品购买会校验活动、商品存在、商店等级、购买次数限制、货币是否足够。
- 奖励发放走 `PlayerDropHandle`，消耗走 `PlayerPropsHandle`，并写 xdlog。
- rogue 币设置每日获取上限，死斗和故事结算都会受该上限约束。

### GM 与后台

- 增加 Rogue GM 工具：
  - 结束死斗战斗。
  - 结束故事战斗。
  - 删除 Rogue 排行或清空排行。
  - 设置玩家 Rogue 榜单封禁状态。
- Web GM 新增页面：
  - `Rogue排行榜`
  - `Rogue封禁玩家`
- 后台接口新增：
  - `/v1/api/roguerank`
  - `/v1/api/rogueban`

### 埋点与任务

- 新增 xdlog 事件：
  - 解锁英雄。
  - 英雄抽取。
  - 死斗战斗奖励。
  - 死斗每日奖励。
  - 死斗全服奖励。
  - 故事战斗奖励。
  - 解锁符文空间。
- 任务类型覆盖英雄抽取、技能选择、击杀怪物、故事首通/完美通关、死斗伤害等肉鸽行为。

### 验证与测试痕迹

- 提交中涉及 `tests/api/operbattle_test.go`、`tests/api/data_test.go`、`tests/game/game_test.go` 等测试样例调整。
- 本次整理只做 git 内容归纳与文档写入，未执行 `make test` 或 `go test ./...`。

### 风险与关注点

- 该功能链路跨 game、rogue/team、battle/operbattle、center、admin/web-gm，多服务契约变更多，回归时应优先做端到端联调。
- 协议、生成代码与服务注册强相关，后续改动 proto 后必须走 `make pbproto`。
- 战报由客户端上传，服务端已有多处校验和作弊标记，但仍需要重点压测非法上传、重复上传、断线重连、暂停超时、活动结束时队伍清理。
- 排行榜有赛季重置和 GM 删除能力，需要重点验证新一期活动切换、跨区 zone、故事地图维度排行与榜单封禁。

## Q2: 详细介绍 Battle/OperBattle 战斗逻辑

### 总体定位

肉鸽 1.0 的 Battle/OperBattle 逻辑核心在 `internal/operbattle/oper_battle_rogue.go`。它不是完整服务器权威模拟所有怪物移动、技能碰撞和伤害过程的战斗，而是一个“服务端战斗状态机 + 客户端战报上传 + 服务端配置校验/统计/结算”的混合模型。

Battle/OperBattle 负责维护队伍战斗生命周期、玩家选择英雄/技能的暂停事件、波次与怪物统计、经验升级、掉落事件、伤害统计、作弊标记和最终结算。客户端负责上传战斗过程事实，例如当前波次、刷新怪物、击杀怪物、拾取经验、技能伤害、战斗结束等；服务端根据配置表校验这些事实是否合理，并据此生成结算数据。

### 初始化流程

战斗队伍由 `NewOperBattleRogue` 创建：

- 使用 `RPCInOperBattleRogueCreateTeam` 中的公共队伍信息初始化 `BaseOperBattleTeam`。
- 帧率使用 `OperBattleRogueFrameNum = 10`。
- 保存玩法模式 `rogueMode`，例如死斗或故事。
- 从 `RogueParamData` 读取等级经验倍率、最大战斗时长、最大暂停时长。
- 初始化事件队列、波次信息、总刷怪统计。
- 将 team 服传入的 `RPCRoguePlayerInfo` 转成 `OperBattleRoguePlayer`，包括玩家基础信息、初始可选英雄、已装备符文。

玩家初始状态：

- 初始等级为 1。
- 尚未选择英雄。
- 有一组初始可选英雄，每个英雄有刷新次数。
- 技能槽上限默认主动 4 个、被动 4 个，后续会受英雄配置影响。
- 记录符文、技能统计、击杀统计、掉落奖励等战斗内数据。

### 状态机

肉鸽战斗沿用 OperBattle 队伍状态：

- `Default`：默认初始状态。
- `Ready`：战斗开始前准备阶段。
- `BattleIn`：战斗进行中。
- `BattlePause`：战斗暂停中。
- `End`：战斗结束，等待结算或关闭。

每帧由 `UpdateFrame` 推进：

- 如果所有真人玩家离线，设置关闭队伍。
- `Default` 会进入 `Ready`，并触发选择英雄事件。
- `Ready` 等待英雄/技能选择事件完成，事件完成后进入 `BattleIn`。
- `BattleIn` 每帧递增 `battleInFrameNum`，并广播当前战斗帧。
- 如果进入事件或玩家手动暂停，则切到 `BattlePause`。
- `BattlePause` 等待事件处理完或手动暂停解除，再回到 `BattleIn`。
- `End` 时如果是强制退出则解散队伍，否则向 team/rogue 服发送战斗结算。

这里有两个时间概念：

- `GetFrameNum()` 是 OperBattle 队伍自身运行帧。
- `battleInFrameNum` 是肉鸽实际战斗帧，只在 `BattleIn` 中增长；事件暂停和手动暂停期间不增长。

因此肉鸽可以支持“战斗内时间暂停”，结算时 `GetRealBattleTime` 使用 `battleInFrameNum` 计算实际战斗时长。

### 事件机制

肉鸽战斗使用事件队列控制暂停交互：

- `eventList`：待处理事件队列。
- `eventID`：当前正在处理的事件。
- `eventType/eventValue/eventData`：当前事件给客户端展示/执行的数据。

事件处理入口是 `tryHandleEventList`：

- 如果当前已有事件在处理，直接返回。
- 从 `eventList` 取第一个事件。
- 选择英雄事件只设置当前事件。
- 选择技能事件会先生成技能候选列表。
- 配置表事件会根据 `RogueEventData` 生成奖励、技能或转盘数据。
- 成功触发后设置 `eventID`，并推送全量场景数据。

主要事件包括：

- 选择英雄：战斗开始前触发，所有玩家未选英雄时阻塞战斗。
- 选择技能：初始技能选择或升级后触发。
- 掉落宝箱：事件结束后给玩家追加掉落道具。
- 掉落技能：事件结束后直接给玩家追加技能。
- 多个技能选一个：客户端回传选择的技能，服务端校验必须在候选列表内。
- 转盘奖励：服务端随机出转盘索引，事件结束后按索引发道具。
- 技能召唤人机：作为事件类型数据下发给客户端。

事件期间队伍会停在 `Ready` 或 `BattlePause`。如果准备/暂停超过最大暂停帧，会设置 `OperBattleRogueBattleQuit` 强制退出。

### 英雄选择

英雄相关操作不是客户端直接打到 Battle，而是：

客户端 AID -> Game -> Team/Rogue -> CommonInstance 自定义操作 -> OperBattle。

Battle 只在当前事件是选择英雄时接受：

- `OperBattleCustomOptTypeRogueSelectHero`
- `OperBattleCustomOptTypeRogueRefreshHero`

选择英雄时服务端校验：

- 玩家存在。
- 地图配置存在。
- 英雄配置存在。
- 玩家还没有选择过英雄。
- 选择的英雄必须在当前候选 `SelectHeros` 中。

选择成功后：

- 写入 `SetHero`。
- 根据英雄配置调整主动/被动技能槽上限。
- 添加英雄初始主动技能与被动技能。
- 记录这些技能为已刷新技能。
- 初始化技能选择次数。
- 清除选择英雄事件。
- 检查是否需要立即触发选择技能事件。
- 推送全量场景数据。

刷新英雄时：

- 只能在选择英雄事件中刷新。
- 用客户端传入的全部可用英雄集合，排除已经刷出来过的英雄。
- 根据地图配置随机 1 个新英雄替换指定候选位。
- 消耗该候选位刷新次数。

### 技能选择

技能选择同样通过自定义操作进入 OperBattle：

- `OperBattleCustomOptTypeRogueSelectSkill`
- `OperBattleCustomOptTypeRogueRefreshSkill`

技能选择触发场景：

- 英雄选择后，地图配置给初始技能选择次数。
- 玩家升级后，如果 `SelectSkillLevel < Level`，会补一次技能选择。
- 配置事件也可能给予技能。

`GenerateSelectSkill` 会根据当前等级找到技能刷新配置：

- 根据 `SelectSkillLevel` 找到刷新等级。
- 读取 `RogueSkillRefreshItem`。
- 汇总基础技能组和满足前置条件后追加的技能组。
- 展开技能组连接表中的技能与权重。
- 如果主动/被动技能槽已满，则过滤掉玩家尚未拥有的同类新技能，只允许已有技能升级。
- 随机出地图配置数量的候选技能。
- 设置 `SkillRefreshNum`，允许本次候选整体刷新一次。

选择技能时校验：

- 技能配置存在，否则标记作弊。
- 玩家有可选择次数，否则标记作弊。
- 技能必须在当前候选 `SelectSkills` 里，否则标记作弊。

选择成功后：

- 扣减选择次数。
- 记录技能选择次数。
- 根据技能组决定加入主动槽、被动槽或其它技能集合。
- 记录已选择技能、已刷新技能组。
- 清空当前候选技能。
- 清除选择技能事件。
- 如果仍有待选择次数或升级触发，继续排队选择技能事件。

### 战报上传

客户端战报通过 `RogueUploadBattleLog` 到达 Battle，最终进入 `handleBattleRpcPlayerUploadLog`。

战报按 `OpType` 处理：

- `RogueEnd`：上传战斗结果，只接受失败或胜利；非法结果标记作弊。
- `RogueSkillHurt`：上传技能真实伤害和表现伤害，累计总伤害与技能伤害统计。
- `RogueCurWave`：上传当前波次与波次 ID，服务端创建波次记录。
- `RogueRefreshMonster`：上传本波刷新出的怪物组。
- `RogueStopBattle`：上传手动暂停或结束暂停。
- `RogueGetMonsterExp`：上传拾取怪物经验，服务端按怪物、等级、倍率计算经验并升级。
- `RogueKillMonster`：上传击杀怪物数量，服务端累计波次击杀与玩家击杀。
- `RoguePassWave`：代码中当前主体逻辑被注释，实际通过击杀统计自动推进已通过波次。
- `RogueEventEnd`：上传事件结束，服务端校验当前事件 ID 后发奖励或技能。

战报帧有一个简单过滤：

- 如果上传帧小于已处理的 `uploadLogFrame`，直接丢弃。
- 注释说明“不过滤重复帧数，可以重复发送”，所以相同帧仍可能被处理。

### 波次与怪物校验

服务端维护 `waveInfo`：

- 每个波次记录波次 ID。
- 最大刷新次数。
- 已刷新次数。
- 下一次允许刷新的帧。
- 每种怪物的刷新数量、击杀数量、经验拾取数量。

`SetWave` 校验：

- 地图配置存在。
- 当前地图包含客户端上传的 waveID，否则标记作弊。
- waveID 配置存在，否则标记作弊。
- 波次只能递增；跳波会标记作弊。
- 波次配置若有任务/事件 ID，会加入事件队列。

`WaveRefreshMonster` 校验：

- 波次必须存在。
- 上传 waveID 必须与当前波次记录一致。
- 怪物组必须属于该波次配置。
- 怪物组配置必须存在。
- 刷新次数不能超过波次最大刷新次数。
- 非首次刷新时会校验刷新帧，允许 3 秒误差，过早刷新会标记作弊。

刷新怪物时服务端还会：

- 统计每种怪物刷新数量。
- 统计全队总刷新怪物数量。
- 根据怪物等级配置累加总怪物血量，用于最终伤害反作弊。

`AddKillMonster` 校验：

- 波次必须存在。
- waveID 必须一致。
- 击杀数量不能超过已刷新数量。
- 波次所有怪物击杀完后，自动推进 `passWave`。
- 如果波次配置了 fever/event，会加入事件队列。

`PickExpMonster` 校验：

- 波次必须存在。
- waveID 必须一致。
- 拾取经验的怪物数量不能超过已击杀数量。
- 当前波次所有怪物经验都拾取后删除该波次运行数据，减少内存占用。

### 经验与升级

经验不是直接信任客户端上传值，而是客户端上传怪物、数量、倍率等参数，服务端按配置计算：

- 通过怪物 ID 找怪物配置。
- 通过 waveID 找怪物等级。
- 从怪物等级配置计算基础经验。
- 校验当前等级配置允许的 rate。
- 按 `monsterExp * monsterNum * expRate * rate / 10000` 累加经验。

玩家升级后：

- 更新等级和当前经验。
- 如果升级，检查是否触发技能选择事件。
- 设置推送全量场景。
- 如果经验有变化，追加 `SBRogueRoleLevelAndExp` 广播玩家等级和经验。

### 暂停逻辑

暂停来源有两类：

- 事件暂停：选择英雄、选择技能、事件表奖励等。
- 手动暂停：客户端上传 `RogueStopBattle`。

在 `BattleIn` 中，只要当前有事件或手动暂停，就切到 `BattlePause`。暂停期间：

- 不增长 `battleInFrameNum`。
- 持续尝试处理事件队列。
- 事件未结束或手动暂停未解除时累加暂停帧。
- 超过最大暂停帧，设置强制退出。
- 暂停解除后回到 `BattleIn` 并广播队伍状态。

### 场景同步

当状态变化、事件变化、英雄/技能选择、等级变化等发生时，服务端通过 battleLog 标记推送：

- `SetPushAllSceneData`：推送全量战斗场景。
- `SBRogueBattleInFrameNum`：广播战斗内帧数。
- `SBRogueRoleLevelAndExp`：广播玩家等级经验。
- `SBGetTeamStatus`：广播队伍状态。

`GetRogueSceneData` 返回给客户端的核心字段：

- 当前战斗内帧。
- 当前波次。
- 经验倍率。
- 当前事件 ID、事件类型、事件值、事件额外数据。
- rogue 模式。
- 每个玩家的选择英雄、已选英雄、选择技能次数、技能刷新次数、候选技能、主动技能、被动技能、等级、经验、符文。

断线重连时，team/rogue 服如果发现队伍仍在战斗中，会返回 Battle 场景数据，客户端可用这些数据恢复展示与交互。

### 结算逻辑

战斗结束时有三种路径：

- 客户端上传 `RogueEnd`，服务端设置胜利或失败。
- 达到最大战斗时长，服务端强制胜利。
- 准备/暂停超时，服务端设置强制退出。

`End` 状态下：

- 如果是强制退出，调用 `SendTeamDissolveRpc` 解散队伍。
- 否则调用 `SendBattleResultRpc(GetOverInfo())` 发送结算到 team/rogue 服。
- 然后关闭 battle 队伍。

`GetOverInfo` 生成结算：

- 模式、结算波次、实际战斗时长、总伤害、结束状态。
- 执行最终反作弊检查。
- 写入是否作弊。
- 为每个玩家生成结算信息：
  - 角色 ID。
  - 已选英雄。
  - 掉落奖励。
  - 选过的技能。
  - 主动/被动技能与伤害。
  - 技能选择次数。
  - 击杀怪物数量。
  - 符文。

奖励由玩家战斗内 `DropProps` 加上按击杀数量计算的 rogue 币组成；实际发奖在 Game 服结算处理里完成。

### 反作弊点

当前 Battle/OperBattle 主要反作弊点包括：

- 上传非法战斗结束状态。
- 选择不存在的技能。
- 没有技能选择次数却选择技能。
- 选择不在候选列表中的技能。
- 上传地图不包含的 waveID。
- waveID 不存在。
- 波次不递增。
- 刷新不存在或不属于当前波次的怪物组。
- 刷新次数超过配置上限。
- 过早刷新怪物。
- 击杀数量超过刷新数量。
- 拾取经验数量超过击杀数量。
- 上传总伤害超过服务端按刷新怪物累计出的总怪物血量。
- 故事模式胜利时，结算波次小于地图最大波次。

经验总量校验曾有设计，但代码中注释掉了，因为经验存在 buff 加成后不容易准确统计。

### GM 结束战斗

GM 工具可以直接结束死斗或故事战斗：

- team/rogue 服校验队伍在战斗中、模式匹配、参数数量正确。
- 转发到 OperBattle 的 GM Tool。
- Battle 根据参数覆盖波次、战斗时长、总伤害。
- 设置为胜利、不作弊。
- 发送结算并关闭队伍。

GM 参数语义是 `[波次, 战斗时长(秒), 总伤害]`。

### 关键理解

这套 Battle/OperBattle 逻辑的重点不是“服务端模拟每一次攻击”，而是“服务端控制玩法状态与关键选择，客户端上传战斗事实，服务端用配置表重算可验证部分并结算”。因此后续排查肉鸽战斗问题时，应该优先看四类数据是否一致：

- 客户端上传战报顺序与内容。
- 地图、波次、怪物组、怪物等级、技能刷新配置。
- `eventID/eventList` 是否卡住导致暂停。
- `isCheat` 是在哪个校验点被置位。

## Q3: 怎么实现事件暂停和手动暂停的？

### 结论

肉鸽战斗的事件暂停和手动暂停最终都通过同一个队伍状态实现：`OperBattleTeamStatusBattlePause`。区别是暂停来源不同：

- 事件暂停：由 `eventID != 0` 触发，也就是 `IsDuringEvent()` 为 true。
- 手动暂停：由客户端战报上传 `RogueStopBattle`，服务端写 `isManualPause`。

暂停期间 `battleInFrameNum` 不增长，所以肉鸽的实际战斗时间会停住。恢复时重新切回 `OperBattleTeamStatusBattleIn`。

### 事件暂停怎么触发

事件暂停依赖三个字段：

- `eventList`：待处理事件队列。
- `eventID`：当前正在处理的事件。
- `eventType/eventValue/eventData`：当前事件下发给客户端的数据。

事件进入队列的典型来源：

- 初始进入 `Ready` 时，如果玩家没选英雄，`checkTriggerSelectHeroEvent` 添加 `RogueBattleEventSelectHero`。
- 玩家选择英雄后，`checkTriggerSelectSkillEvent` 可能添加 `RogueBattleEventSelectSkill`。
- 玩家升级后，选择技能等级落后于当前等级，也会添加选择技能事件。
- 波次开始或击杀完某波后，波次配置里的 `TaskID/FeverID` 可能添加配置表事件。

每帧会调用 `tryHandleEventList`：

- 如果当前已经有 `eventID`，说明事件还没结束，不再取新事件。
- 如果没有当前事件，就从 `eventList` 取一个事件。
- 选择技能事件会先生成候选技能。
- 配置表事件会生成宝箱、技能、多选一、转盘等奖励数据。
- 最后 `SetEventID(eventID)`，并推送全量场景给客户端。

一旦 `eventID` 被设置，`IsDuringEvent()` 就会返回 true。

### BattleIn 中如何进入暂停

战斗进行中由 `updateStatusBattleIn` 推进：

```go
o.battleInFrameNum++
o.tryHandleEventList()
if !o.IsEnd() && (o.IsDuringEvent() || o.isManualPause) {
    o.SetTeamStatus(data.OperBattleTeamStatusBattlePause)
    o.tmFrameCount = uint32(0)
    o.AddBattleLogToAllPlayer(SBGetTeamStatus(o.GetTeamStatus()))
}
```

关键点：

- 只有 `BattleIn` 状态才会递增 `battleInFrameNum`。
- 只要出现当前事件，或手动暂停标记为 true，就切到 `BattlePause`。
- 切状态时重置 `tmFrameCount`，它用于统计暂停持续帧数。
- 队伍状态变化会通过 battle log 广播给客户端。

### Ready 中的事件暂停

战斗开始前的 `Ready` 阶段也会被事件阻塞：

- 初始 `Default -> Ready` 时触发选择英雄事件。
- `updateStatusReady` 每帧调用 `tryHandleEventList`。
- 如果 `IsDuringEvent()` 为 true，就停留在 `Ready`。
- 如果没有事件了，才调用 `enterBattleIn()` 进入战斗。

所以选择英雄/初始技能选择并不是进入 `BattleIn` 后再暂停，而是在 `Ready` 阶段先处理完。超过最大暂停帧也会强制退出。

### 手动暂停怎么触发

手动暂停通过客户端上传战报：

- `OpType = OperBattleOpTypeRogueStopBattle`
- `Data[0] = 1` 表示开始暂停。
- `Data[0] = 0` 表示结束暂停。

服务端处理逻辑是：

```go
o.SetManualPause(l.Data[0] == 1)
```

`SetManualPause` 只是设置布尔值：

```go
func (o *OperBattleRogue) SetManualPause(isPause bool) {
    o.isManualPause = isPause
}
```

真正切换到暂停状态是在下一次 `updateStatusBattleIn` 中判断 `o.isManualPause`。

### 暂停中如何保持暂停

进入 `BattlePause` 后，由 `updateStatusBattlePause` 处理：

```go
o.tryHandleEventList()
if o.IsDuringEvent() || o.isManualPause {
    o.tmFrameCount++
    if o.tmFrameCount > o.maxPauseFrameNum {
        o.SetEnd(data.OperBattleRogueBattleQuit)
    }
    return
}
o.tmFrameCount = uint32(0)
o.SetTeamStatus(data.OperBattleTeamStatusBattleIn)
o.AddBattleLogToAllPlayer(SBGetTeamStatus(o.GetTeamStatus()))
```

也就是说暂停状态会持续到两个条件都解除：

- 当前事件结束，即 `eventID == 0`。
- 手动暂停解除，即 `isManualPause == false`。

只要其中一个还存在，战斗继续停在 `BattlePause`，并累加 `tmFrameCount`。如果超过 `maxPauseFrameNum`，战斗会设置为 `OperBattleRogueBattleQuit`，最终走强制退出/解散。

### 事件暂停怎么解除

不同事件解除方式不同，但共同点是最终会清掉 `eventID`。

选择英雄：

- 客户端走选择英雄操作。
- 服务端校验英雄在候选列表中。
- 成功后调用 `ClearEventID(RogueBattleEventSelectHero)`。
- 然后检查是否需要触发选择技能事件。

选择技能：

- 客户端走选择技能操作。
- 服务端校验技能存在、玩家有选择次数、技能在候选列表中。
- 成功后调用 `ClearEventID(RogueBattleEventSelectSkill)`。
- 如果还有待处理的升级/选择次数，会继续添加下一次选择技能事件。

配置表事件：

- 客户端上传 `RogueEventEnd`。
- 服务端校验上传的 eventID 等于当前 `eventID`。
- 根据事件类型发道具、加技能或校验多选一技能。
- 最后调用 `ClearEventID(eventData.ID)` 并清理 `eventType/eventValue/eventData`。

### 为什么暂停时战斗时间会停

肉鸽实际战斗时长不是直接用队伍总帧，而是用 `battleInFrameNum`：

```go
return int64((o.GetFrameTick() * time.Duration(o.battleInFrameNum)).Minutes())
```

而 `battleInFrameNum++` 只发生在 `updateStatusBattleIn`。进入 `BattlePause` 或停在 `Ready` 时不会增长，所以事件选择、手动暂停、暂停超时等待都不会计入实际战斗时间。

### 一句话流程

完整流程可以理解为：

`事件入队/手动暂停上传 -> BattleIn 每帧检查 -> 命中事件或手动暂停则切 BattlePause -> BattlePause 中等待 eventID 清零且 isManualPause 为 false -> 恢复 BattleIn -> battleInFrameNum 继续增长`。

## Q4: 客户端战报驱动下的战斗逻辑是怎么进行的？

### 结论

肉鸽这套战斗里，客户端上传的 `RogueUploadBattleLog` 不是普通日志，而是服务端推进战斗统计和结算状态的核心输入流。服务端自己维护队伍状态机、事件暂停、候选英雄/技能、波次记录、怪物刷新/击杀/经验拾取统计、总伤害、总怪物血量和最终结算；客户端把实际战斗过程按 `OpType` 分批上传，服务端逐条消费、校验、更新状态。

可以理解成：

`客户端实际打怪 -> 按帧上传战报 -> Battle 校验并累计可信状态 -> 触发升级/事件/暂停/结算 -> 结算回传 Rogue/Team -> Game 发奖和推送`。

### 战报入口链路

入口大致是：

- 客户端调用 `InRogueUploadBattleLog`。
- Game 服 `handleRogueUploadBattleLog` 校验玩家还在 Rogue 队伍中。
- Game 服通过 `RpcRogueUploadLog` 发给 Rogue/Team 服。
- Rogue/Team 服 `handleGrpcRogueUploadLog` 调用 `team.BattleSendUploadLog`。
- 最终 Battle/OperBattle 收到 `RPCInOperBattleRogueUploadLog`。
- `HandleBattleRpcData` 根据 `OperBattleKeyIDRogueUploadLog` 分发到 `handleBattleRpcPlayerUploadLog`。

Battle 侧先做基础保护：

- 如果队伍已结束，直接忽略。
- 如果 `UploadLog` 为空，忽略。
- 如果找不到上传玩家，忽略。
- 如果上传帧小于已处理帧 `uploadLogFrame`，忽略。
- 相同帧不会被过滤，注释里明确“可以重复发送”。

### 战报批处理方式

`RogueBattleUploadData` 里有一个 `FrameNum` 和多条 `Log`。服务端每次收到一批后：

- 更新全队 `uploadLogFrame`。
- 按顺序遍历这批 `Log`。
- 每条 `Log` 根据 `OpType` 改变不同状态。
- 这一批处理完后，如果玩家经验变化，则广播等级/经验。

这意味着一次上传可以同时包含多种事实，例如：

- 当前波次开始。
- 本波刷新了某几个怪物组。
- 玩家击杀了若干怪物。
- 玩家拾取经验升级。
- 升级触发技能选择事件。
- 上传技能伤害统计。

### 一局战斗的典型推进顺序

一局战斗大致可以按这个顺序理解：

1. Battle 创建队伍，进入 `Ready`。
2. 服务端触发选择英雄事件，客户端选择英雄。
3. 选择英雄后触发初始技能选择事件，客户端选择技能。
4. 事件结束后进入 `BattleIn`，`battleInFrameNum` 开始增长。
5. 客户端遇到新波次，上传 `RogueCurWave`。
6. 客户端刷新怪物组，上传 `RogueRefreshMonster`。
7. 玩家打怪期间，客户端上传 `RogueSkillHurt` 累计技能伤害。
8. 怪物死亡，客户端上传 `RogueKillMonster`。
9. 玩家拾取经验，客户端上传 `RogueGetMonsterExp`。
10. 服务端按配置计算经验，若升级则触发选择技能事件。
11. 事件出现后，服务端切 `BattlePause`，客户端选择技能后恢复。
12. 某波全部击杀后，服务端自动推进通过波次，并可能触发波次事件。
13. 客户端最终上传 `RogueEnd`。
14. 服务端进入 `End`，生成 `OverInfo` 并发给 Rogue/Team 服。
15. Rogue/Team 服广播结束，Game 服处理玩家奖励、任务、推送和排行榜。

### RogueCurWave：创建波次上下文

客户端上传格式：

`[当前第几波, 波次id]`

服务端调用 `SetWave`：

- 如果该波次已经存在，直接返回，避免重复创建。
- 校验地图配置存在。
- 校验当前地图包含这个 waveID，否则标记作弊。
- 校验 waveID 配置存在，否则标记作弊。
- 如果上传的波次大于当前最大波次，要求必须是递增的；跳波会标记作弊。
- 创建 `OperBattleRgoueWaveInfo`，记录 waveID、最大刷新次数、怪物计数 map。
- 如果波次配置了 `TaskID`，加入事件队列。

这一步相当于告诉服务端：“客户端已经进入第 N 波，这波对应配置表中的某个 waveID。” 后续刷怪、击杀、拾取经验都要挂在这个波次上下文下面。

### RogueRefreshMonster：登记刷怪事实

客户端上传格式：

`[当前波次, 波次id, 怪物组id1, 怪物组id2, ...]`

服务端调用 `WaveRefreshMonster`：

- 校验波次已存在。
- 校验上传 waveID 与服务端记录一致。
- 校验怪物组属于这个 waveID。
- 读取怪物组配置，展开为怪物 ID 和数量。
- 记录当前波次每种怪物的刷新数量。
- 记录全队总刷新怪物数量。
- 根据怪物等级配置累计 `totalMonsterHP`。
- 累加本波刷新次数。
- 如果刷新次数超过配置上限，标记作弊。
- 校验刷新间隔，非首次刷新如果比配置时间早太多，标记作弊。
- 记录下一次理论允许刷新的帧。

这一步很重要，因为服务端后续判断“击杀是否超量”和“总伤害是否超过怪物总血量”都依赖这里累计的刷怪数据。

### RogueKillMonster：登记击杀事实

客户端上传格式：

`[当前波次, 波次id, 怪物id, 怪物数量, ...]`

服务端处理：

- 校验波次存在。
- 校验 waveID 一致。
- 校验怪物配置、怪物等级配置存在。
- 调用 `AddKillMonster` 累计击杀。
- 给玩家累计总击杀怪物数量。

`AddKillMonster` 内部会检查：

- 击杀数量不能超过该怪物已刷新数量。
- 如果当前波所有怪物都已击杀，则 `CheckIsWaveEnterEnd` 返回 true。
- 波次全清后，服务端调用 `SetPassWave` 自动推进已通过波次。
- 如果波次配置了 `FeverID`，把它加入事件队列。

所以 `RoguePassWave` 的主体逻辑虽然注释掉了，但“通过波次”不是完全丢了，而是由击杀统计自动推出来。

### RogueGetMonsterExp：拾取经验并触发升级

客户端上传格式：

`[当前波次, 波次id, 怪物id, 怪物数量, exp倍率, rate, ...]`

服务端并不直接相信客户端上传一个经验值，而是自己按配置计算：

- 根据玩家当前等级拿等级配置。
- 校验 `rate` 是否在当前等级配置允许范围内。
- 根据 waveID 找波次配置。
- 根据怪物 ID 找怪物配置。
- 根据怪物类型和波次怪物等级找怪物等级配置。
- 计算经验：`怪物经验 * 数量 * expRate * rate / 10000`。
- 调用 `PickExpMonster` 登记该怪物经验已拾取。

`PickExpMonster` 会校验：

- 波次存在。
- waveID 一致。
- 拾取经验数量不能超过击杀数量。
- 如果本波所有怪物经验都拾取完，就删除该波 `waveInfo`，释放运行时数据。

经验累计后调用 `player.AddExp`：

- 如果经验有变化，批处理结束后广播等级/经验。
- 如果升级，调用 `checkTriggerSelectSkillEvent` 添加技能选择事件。
- 技能事件会导致战斗进入暂停，等客户端选完技能再继续。

### RogueSkillHurt：累计伤害统计

客户端上传格式：

`[技能id, 技能真实伤害, 技能造成伤害, ...]`

这里有两个伤害：

- `realHurt`：真实伤害，不超过怪物血量上限，用于全队总伤害 `totalHurt`。
- `hurt`：表现伤害，用于技能统计展示。

服务端处理：

- 过滤负数。
- `totalHurt` 累加 `realHurt`。
- 玩家维度按技能 ID 累计 `realHurt/hurt`。

最终结算时：

- `totalHurt` 会作为战斗总伤害上报。
- 每个主动/被动技能会带上对应伤害统计。
- `CheckCheat` 会用 `totalHurt` 与 `totalMonsterHP` 做最终校验：如果总真实伤害超过服务端按刷怪记录累计出的总怪物血量，则标记作弊。

### RogueStopBattle：控制手动暂停

客户端上传格式：

- `Data[0] = 1`：开始暂停。
- `Data[0] = 0`：结束暂停。

服务端只设置一个状态：

`isManualPause = Data[0] == 1`

真正的暂停切换发生在每帧 `updateStatusBattleIn/updateStatusBattlePause`：

- `BattleIn` 中发现 `isManualPause == true`，切到 `BattlePause`。
- `BattlePause` 中如果 `isManualPause` 仍为 true，就持续暂停。
- 客户端上传结束暂停后，`isManualPause` 变 false。
- 如果没有事件也在处理中，就恢复 `BattleIn`。

### RogueEventEnd：结束当前事件

客户端上传格式：

`[事件id, 额外数据...]`

服务端处理：

- eventID 必须非 0。
- 上传 eventID 必须等于当前正在处理的 `eventID`。
- 事件配置必须存在。
- 根据事件类型执行结果。

事件结果：

- 掉落宝箱：把 `eventValue` 里的道具追加到玩家 `DropProps`。
- 掉落技能：把事件给的技能添加到玩家技能集合。
- 多选一技能：校验客户端选择的技能必须在 `eventValue` 候选里，再添加技能。
- 转盘：使用服务端之前随机出的 `eventData` 索引发放对应道具。

事件结束后：

- 清掉当前 `eventID`。
- 清掉 `eventType/eventValue/eventData`。
- 推送全量场景数据。
- 如果没有其它事件和手动暂停，战斗可以恢复。

### RogueEnd：进入结算

客户端上传格式：

`[结束状态]`

服务端只接受：

- `OperBattleRogueBattleFail`
- `OperBattleRogueBattleWin`

如果上传其它状态，标记作弊。合法时调用 `SetEnd`：

- 写 `endStatus`。
- 队伍状态切到 `OperBattleTeamStatusEnd`。
- 广播队伍状态。

下一帧 `UpdateFrame` 看到队伍是 `End`：

- 如果是强制退出，走解散。
- 否则调用 `SendBattleResultRpc(GetOverInfo())`。
- `GetOverInfo` 汇总波次、战斗时长、总伤害、玩家奖励、技能、击杀、符文等。
- 再执行最终反作弊检查。
- 通过 `RpcRogueBattleOver` 发给 Rogue/Team 服。

### 服务端状态如何被战报驱动

这套逻辑里，服务端主要维护几类“可校验状态”：

- `wave/waveInfo/passWave`：当前波次、每波刷怪/击杀/经验拾取、已通过波次。
- `totalMonsterHP`：根据刷怪记录和配置累计出的怪物总血量。
- `totalHurt`：客户端上传的真实伤害累计。
- `player.TotalKillMonster`：玩家击杀数量，用于结算 rogue 币和任务。
- `player.TotalExp/Level/Exp`：按服务端配置计算后的经验和等级。
- `eventID/eventList`：当前事件与待处理事件，影响暂停和交互。
- `isManualPause`：手动暂停状态。
- `isCheat`：任何关键校验失败后的作弊标记。

客户端上传的是过程事实，服务端用这些事实逐步搭出一份可结算的战斗账本。

### 为什么重复帧有风险

当前只过滤 `FrameNum < uploadLogFrame`，不拒绝 `FrameNum == uploadLogFrame`。注释也写了相同帧可以重复发送。

这意味着如果客户端在同一帧重复上传相同内容，某些操作可能重复累计，例如：

- 技能伤害可能重复累计。
- 击杀数量可能再次校验并可能触发作弊。
- 拾取经验可能再次校验并可能触发作弊。
- 掉落事件如果重复结束，通常会因为 `eventID` 已清掉而被跳过。

所以这套设计更像“允许同帧多批战报”，但对“同帧同内容重放”并没有通用幂等键。排查异常时要特别看客户端是否重复上传了同一批 `Log`。

### 关键理解

肉鸽战报驱动的战斗逻辑不是“客户端上传一个最终结果，服务端照单全收”，而是每类战报都对应服务端的一段状态变更和校验：

- 波次战报建立战斗进度。
- 刷怪战报建立怪物总量和血量上限。
- 击杀战报推进波次通过。
- 经验战报触发升级和技能选择。
- 伤害战报生成统计并参与最终作弊校验。
- 暂停战报控制 `BattlePause`。
- 事件结束战报解除事件阻塞并发放事件奖励。
- 结束战报只负责把战斗切入结算，最终结算仍由服务端汇总当前账本生成。
