local _ENV = Boli2Env

UIEvent = {
    REFRESH_TASK = "REFRESH_TASK", --任务数据刷新
    REFRESH_EXP_RATE = "REFRESH_EXP_RATE", --经验效率刷新
    REFRESH_EXP_RATE_BOX = "REFRESH_EXP_RATE_BOX", --经验效率宝箱刷新
    --REFRESH_PRIVATE_CHAT = "REFRESH_PRIVATE_CHAT", --私人聊天红点刷新
    REFRESH_TEAM_INVITE = "REFRESH_TEAM_INVITE", -- 队伍邀请
    REFRESH_CURRENCY = "REFRESH_CURRENCY", --货币刷新
    REFRESH_POWER = "REFRESH_POWER", --刷新战力
    REFRESH_NAME = "REFRESH_NAME", --刷新角色名字
    REFRESH_AVATAR = "REFRESH_AVATAR", --刷新头像
    CHARGE_WEB = "CHARGE_WEB", --网页充值成功

    RefreshBlackSmithView = "RefreshBlackSmithView",
    Refresh_BlackSmith_Compose = "Refresh_BlackSmith_Compose",
    Refresh_BlackSmith_Drawings = "Refresh_BlackSmith_Drawings",

    UpdateSystemInfo = "UpdateSystemInfo", --
    VIEW_STATE_CHANGE = "VIEW_STATE_CHANGE",

    --事件
    EvolveJob = "EvolveJob", --转职
    UpdateServerEvents = "UpdateServerEvents",
    AddBaseLv = "AddBaseLv", --升级
    RefreshSkillReleaseMode = "RefreshSkillReleaseMode", --设置战斗模式
    LockEquip = "LockEquip", --装备上锁
    SetEquipAt = "SetEquipAt", --更换装备
    MeltEquip = "MeltEquip", --熔炼装备
    EnhanceEquip = "EnhanceEquip", --强化装备
    WashEquipEntry = "WashEquipEntry", -- 洗炼装备
    SaveEquipWash = "SaveEquipWash", -- 保存装备洗炼结果
    AddEquipGrade = "AddEquipGrade", --升阶装备
    MapArrive = "MapArrive", --地图到达
    MapUnlock = "MapUnlock", --地图解锁
    MapUnlockTip = "MapUnlockTip", --地图解锁提示
    ChooseEquipBack = "ChooseEquipBack", --进阶材料选择
    SetPetAtBattle = "SetPetAtBattle", --主战宠物更换
    MainPetVisible = "MainPetVisible", --主战宠物释放觉醒技
    UpdateEnergyAndSkills = "UpdateEnergyAndSkills", --主界面技能能量/冷却时间刷新
    ShowSkillReleaseSettings = "ShowSkillReleaseSettings", --显示技能设置
    ReleaseSkillEffect = "ReleaseSkillEffect", --主界面技能释放特效
    CleanSkillEffect = "CleanSkillEffect", --清除主界面技能释放特效
    UpdateInstanceSkillView = "UpdateInstanceSkillView", --刷新副本技能
    CollectInstanceSkillView = "CollectInstanceSkillView", --清除副本技能
    SetBuffState = "SetBuffState",
    SetMainBuffState = "SetMainBuffState",
    SetClientFakeSkill = "SetClientFakeSkill",

    SET_ITEM = "SET_ITEM", -- 设置道具
    ADD_EQUIP = "ADD_EQUIP", --增加装备
    DELETE_EQUIP = "DELETE_EQUIP", --熔炼减少装备
    BATTLE_DROP = "BATTLE_DROP", --战斗掉落

    REFRESH_GUILD_RED_POINT = "REFRESH_GUILD_RED_POINT", -- 刷新公会红点
    REFRESH_GUILD_SIGN_IN_RED_POINT = "REFRESH_GUILD_SIGN_IN_RED_POINT", -- 刷新公会红点
    SHOW_BUBBLE = "SHOW_BUBBLE", -- 气泡显示
    SHOW_BUBBLE_NAME = "SHOW_BUBBLE_NAME", -- 名称显示
    SET_GUILD_CAMERA_FOCUS = "SET_GUILD_CAMERA_FOCUS",
    SET_GUILD_CAMERA_FOCUS_END = "SET_GUILD_CAMERA_FOCUS_END",
    GUILD_OPEN_FUNC = "GUILD_OPEN_FUNC",
    GUILD_EMPTY_BUBBLES = "GUILD_EMPTY_BUBBLES",
    GUILD_TREE_STAGE_CHANGED = "GUILD_TREE_STAGE_CHANGED",

    REFRESH_FASHION_SHOP_TIME_LIMIT_PAGE_RED_DOT = "REFRESH_FASHION_SHOP_TIME_LIMIT_PAGE_RED_DOT", --限时商店红点
    REFRESH_FASHION_SHOP_CONSTANT_RED_DOT = "REFRESH_FASHION_SHOP_CONSTANT_RED_DOT", --常驻红点
    REFRESH_FASHION_LOTTERY_RED_DOT = "REFRESH_FASHION_LOTTERY_RED_DOT",--时装抽奖红点
    REFRESH_FASHION_LOTTERY_LOG_REWARD_RED_DOT = "REFRESH_FASHION_LOTTERY_LOG_REWARD_RED_DOT",--时装抽奖登录红点


    ITEMSVIEW_STATE_CHANGED = "ITEMSVIEW_STATE_CHANGED",
    PLAYER_INFO_HEAD_RED_POINT = "PLAYER_INFO_HEAD_RED_POINT",
    PLAYER_INFO_HEAD_FRAME_RED_POINT = "PLAYER_INFO_HEAD_FRAME_RED_POINT",
    PLAYER_INFO_BACKDROP_RED_POINT = "PLAYER_INFO_BACKDROP_RED_POINT",
    PLAYER_INFO_POSE_RED_POINT = "PLAYER_INFO_POSE_RED_POINT",

    REFRESH_GIRDWALK_CIRCLE_REWARD_RED_DOT  = "REFRESH_GIRDWALK_CIRCLE_REWARD_RED_DOT",

    --宠物红点
    REFRESH_PET_ACTIVITY_RED_DOT= "REFRESH_PET_ACTIVITY_RED_DOT",
    
    --种田
    SET_FARM_CAMERA_FOCUS = "SET_FARM_CAMERA_FOCUS",
    SET_FARM_CAMERA_FOCUS_END = "SET_FARM_CAMERA_FOCUS_END",
    FARM_SCENE_ROOT_END = "FARM_SCENE_ROOT_END",
    FARM_CHANGE_SCENE = "FARM_CHANGE_SCENE",
}

-- 房间事件
RoomEvent = {
    ROOM_TEAM_CHANGED = "ROOM_TEAM_CHANGED", -- 加入或者离开队伍 收到消息后 调用 BattleSceneData.GetAllRoomData()
    ROOM_ROLE_INFO_CHANGED = "ROOM_ROLE_INFO_CHANGED", -- 队伍里个人信息变更  升级等 改变职业等

    SCENE_ROLE_CHANGED = "SCENE_ROLE_CHANGED", -- 相机使用 场景队伍角色改变会触发事件 然后刷新 所有角色就行
    SCENE_MONSTER_CHANGED = "SCENE_MONSTER_CHANGED", --相机使用 参数那个怪物

    ROOM_ROLE_LIFE_CHANGED = "ROOM_ROLE_LIFE_CHANGED", -- 队伍里角色死亡复活

    ROOM_ROLE_ONLINE_CHANGED = "ROOM_ROLE_ONLINE_CHANGED", --队伍角色上下线
    ROOM_ROLE_LEVEL_CHANGED = "ROOM_ROLE_LEVEL_CHANGED", --队伍角色等级变化
    ROOM_ROLE_AVATAR_CHANGED = "ROOM_ROLE_AVATAR_CHANGED", --队伍角色头像/头像框变化
}

-- 伤害统计面板相关
BattleStaticsEvent = {
    CHANGE_SUB_STATE = "BATTLE_STATICS_CHANGE_SUB_STATE", -- 改变子对象状态
}

InstanceEvent = {
    GetInstance = "Get_Instance",--获取对应类型的副本关卡数据
    GetSingleInstance = "Get_SingleInstance",
    GetTeamList = "Get_TeamList",--
    GetRankList = "Get_RankList",--
    RewardProcess = "RankListReward_Process",--
    ReceiveTask = "ReceiveTask",--成就奖励
    BattleRewardProcess = "BattleRewardProcess",
}

ViewEvent = {
    ON_SCREEN_RESOLUTION_CHANGED = "OnScreenResolutionChanged",
    ResetRTRawImage = "ResetRTRawImage",
    MessageEvent = "MessageEvent",
    RewardsViewClosed = "RewardsViewClosed",

    ChatChannelView_RefreshChannel = "ChatChannelView_RefreshChannel",
    PrivateChat_Create = "PrivateChat_Create", --私聊创建
    ChatAddMsg = "ChatAddMsg", --私聊刷新(队伍/旅团)
    PrivateChat_RefreshRoleInfo = "PrivateChat_RefreshRoleInfo", --私聊头像/头像框
    PrivateChatBtn_AddMsg = "PrivateChatBtn_AddMsg", --私聊按钮刷新
    BlackChatList_Remove = "PrivateChatBtn_AddMsg", --取消拉黑
    ChatInstanceAdd = "ChatInstanceAdd",

    --通用
    TreasureBoxDropView = "TreasureBoxDropView",--通用道具宝箱
    TreasureBoxDropView_NumChange = "TreasureBoxDropView_NumChange",--通用道具宝箱
    LoginStatusChanged = "LoginStatusChanged",--通用道具宝箱
    --技能
    BattleInfoCommonView_RefreshPrivateList = "BattleInfoCommonView_RefreshPrivateList",
    SkillCardViewEquipSuccess = "SkillCardViewEquipSuccess", --技能卡装备成功
    SkillCardMultiSuccess = "SkillCardMultiSuccess", --技能卡连点
    SkillCardEquipSuccess = "SkillCardEquipSuccess", --技能卡装备成功
    SkillCardContentEquipSuccess = "SkillCardContentEquipSuccess", --技能卡装备成功
    EquipDetailSuccess = "EquipDetailSuccess", --技能卡装备
    SkillCardEnhanceSuccess = "SkillCardEnhanceSuccess", --技能卡养成成功
    SKILL_CARD_UNLOCK_SLOT = "SKILL_CARD_UNLOCK_SLOT", --技能栏位解锁
    SkillCardRandomMatRefresh = "SkillCardRandomMatRefresh", --技能栏位解锁
    SkillCardApplySkillPref = "SkillCardApplySkillPref", --技能栏位解锁
    --商店
    ShopRefreshSuccess = "ShopRefreshSuccess", --刷新商店
    BuyGoodsSuccess = "BuyGoodsSuccess", --购买商品
    RequestBuyGoods = "RequestBuyGoods", --强制请求服务器
    CloseShopView = "CloseShopView",

    RefreshEnterChatRoomState = "RefreshEnterChatRoomState",
    RefreshExitChatRoomState = "RefreshExitChatRoomState",
    --SDK
    RTCSpeakerOn = "RTCSpeakerOn", --实时语音听筒
    RTCMicOn = "RTCMicOn", --实时语音麦克风
    --大区
    RegionDataRefresh = "RegionDataRefresh",

    BroadMachState = "BroadMachState", --匹配房状态广播
    BroadMatchClose = "BroadMatchClose", --非匹配
    BroadMatchOpen = "BroadMatchOpen", --匹配中
    BroadMatchSuccess = "BroadMatchSuccess", --匹配成功

    RefreshPlayerChatState = "RefreshPlayerChatState", --语音房中队友状态
    RefreshTeamPlayersReadyState = "RefreshTeamPlayersReadyState",
    ReadyingEventSuccess = "ReadyingEventSuccess",

    --邮箱
    GetMailReward = "GetMailReward",
    DelMail = "DelMail", --删除邮件

    FriendFanRefresh = "FriendFanRefresh", --有新的粉丝
    FollowFriendSuccess = "FollowFriendSuccess", --关注好友
    --充值成功
    BuyProductSuccess = "BuyProductSuccess", --充值
    --任务相关
    ShowAchievementLevel = "ShowAchievementLevel", --显示成绩等级弹窗
    InitTaskView = "InitTaskView", --刷新任务界面
    --副本
    ShowInstanceIDTeams = "ShowInstanceIDTeams",
    InstanceRefreshAppTeamPlayers = "InstanceRefreshAppTeamPlayers",
    InstanceRefreshApplyRedValue = "InstanceRefreshApplyRedValue",
    RefreshInstanceBattleView = "RefreshInstanceBattleView", --队伍状态
    RefreshInstanceBattleTips = "RefreshInstanceBattleTips", --队伍提示 
    RefreshInstanceBattleProcess = "RefreshInstanceBattleProcess", --副本进度
    InstanceRefreshTeamPlayers = "InstanceRefreshTeamPlayers",
    InstanceRefreshTeamPlayersSate = "InstanceRefreshTeamPlayersState",
    InstanceRefreshTeamPlayersVote = "InstanceRefreshTeamPlayersVote",
    RefreshInstanceTickTime = "RefreshInstanceTickTime",
    RefreshInstanceBattleTickTime = "RefreshInstanceBattleTickTime",
    RefreshGuardWaveTickTime = "RefreshGuardWaveTickTime",
    RefreshInstanceBattleWave = "RefreshInstanceBattleWave",
    RefreshBattleFinishLikeValue = "RefreshBattleFinishLikeValue",
    RefreshJobTypeIcon = "RefreshJobTypeIcon",

    PhysicalPowerGetNextTM = "PhysicalPowerGetNextTM",
    RefreshPhysicalPower = "RefreshPhysicalPower",
    ToFullPowerTime = "ToFullPowerTime",
    ExchangePowerSuccess = "ExchangePowerSuccess",
    UsePowerInfoChanged = "UsePowerInfoChanged",
    --邀请
    RefreshEnableInvitePlayerList = "RefreshEnableInvitePlayerList",

    --主线队伍
    RefreshInviteTeams = "RefreshInviteTeams",
    MainTeamListRefresh = "MainTeamListRefresh",
    RefreshMainTeamLogs = "RefreshMainTeamLogs",
    RefreshPlayerModelShow = "RefreshPlayerModelShow",
    RefreshMainTeamState = "RefreshMainTeamState",

    --占卜
    RefreshDivineResult = "RefreshDivineResult",
    RefreshDivineBtns = "RefreshDivineBtns",

    --宠物
    PetSetOnState = "PetSetOnState",
    PetResetOnState = "PetResetOnState",
    PetViewRefresh = "PetViewRefresh",
    PetPopViewRefresh = "PetPopViewRefresh",
    PetModelRefresh = "PetModelRefresh",
    PetModelSetActive = "PetModelSetActive",
    PetDvivineResult = "PetDvivineResult",
    PetDvivinePoolID = "PetDvivinePoolID",
    RefreshPetWishPool = "RefreshPetWishPool",
    StoneDivineResult = "StoneDivineResult",
    StoneDivinePool = "StoneDivinePool",
    StoneDivineBubble = "StoneDivineBubble",
    RefreshPetDivineView = "RefreshPetDivineView",
    AddPetLevel = "AddPetLevel",
    SetPetTSkillAt = "SetPetTSkillAt",
    AddPetTSkillLevel = "AddPetTSkillLevel",
    SetPetStoneAt = "SetPetStoneAt",
    AddPetStoneLevel = "AddPetStoneLevel",
    RefreshPetFeedbackView = "RefreshPetFeedbackView",
    SetPetTSkillPage = "SetPetTSkillPage",

    --秘宝
    RelicViewRefresh = "RelicViewRefresh",
    RelicPopViewRefresh = "RelicPopViewRefresh",
    RelicPopViewRefreshPassiveInfo = "RelicPopViewRefreshPassiveInfo",

    --坐骑
    MountViewRefresh = "MountViewRefresh",
    MountPopViewRefresh = "MountPopViewRefresh",
    MountSealRefresh = "MountSealRefresh", --贴纸洗练
    MountDivineRefresh = "MountDivineRefresh",
    MountBubbleGet = "MountBubbleGet",
    MountUpLv = "MountUpLv", --坐骑槽位升级
    MountChangeOutfitViewRefresh = "MountChangeOutfitViewRefresh",
    MountView_Main_SetModelState = "MountView_Main_SetModelState",
    MountSetSkillAt = "MountSetSkillAt",
    MountChipSelect = "MountChipSelect",
    MountUpStar = "MountUpStar",

    --猫猫包
    CatViewRefresh = "CatViewRefresh",
    CatChooseViewRefresh = "CatChooseViewRefresh",
    CatSelectViewRefresh = "CatSelectViewRefresh",
    CatDraw = "CatDraw",
    CatEggGet = "CatEggGet",
    CatUpLv = "CatUpLv",
    CatUpStar = "CatUpStar",
    GeneUpLv = "GeneUpLv",
    GeneUpViewRefresh = "GeneUpViewRefresh",
    ChooseCatToOperation = "ChooseCatToOperation",
    CatLock = "CatLock",
    CatFree = "CatFree",
    CatAdmissionBuy = "CatAdmissionBuy",
    CatBreadActive = "CatBreadActive",
    CatSetTypeDFireEffect = "CatSetTypeDFireEffect",
    CatSetTemp = "CatSetTemp",
    HaveCatsViewRefresh = "HaveCatsViewRefresh",

    --道具
    ItemInfoViewRefresh = "ItemInfoViewRefresh",

    --运营活动
    Refresh_GeneralLoginView = "Refresh_GeneralLoginView",
    Refresh_TrainView = "Refresh_TrainView",
    Refresh_MonthCheckIn = "Refresh_MonthCheckIn",
    Refresh_MonthCard = "Refresh_MonthCard",
    Refresh_GiftBoxes = "Refresh_GiftBoxes",
    Refresh_FirstCharge = "Refresh_FirstCharge",
    Refresh_GrowthRewards = "Refresh_GrowthRewards",
    Refresh_AccumulatedCharge = "Refresh_AccumulatedCharge",
    Refresh_ChargeVoucher = "Refresh_ChargeVoucher",
    Refresh_CommonGiftPacks = "Refresh_CommonGiftPacks",
    CommonGiftPacksPropBuySucceed = "CommonGiftPacksPropBuySucceed",
    Refresh_FashionSellPacks = "Refresh_FashionSellPacks",
    Refresh_GridWalk = "Refresh_GridWalk",
    Refresh_SocialMedia = "Refresh_SocialMedia",
    Refresh_SocialMediaFollowSuccess = "Refresh_SocialMediaFollowSuccess",
    Refresh_ActivityCenter = "Refresh_ActivityCenter",
    Refresh_OpenServerActivityCenter = "Refresh_OpenServerActivityCenter",
    Refresh_InviteGiftPack = "Refresh_InviteGiftPack",
    Refresh_InviteBonus = "Refresh_InviteBonus",
    Refresh_InviteBonusTasks = "Refresh_InviteBonusTasks",
    Refresh_InviteBonusShareDaily = "Refresh_InviteBonusShareDaily",
    GetInvitedBonusPlayers = "GetInvitedBonusPlayers",
    GetInvitedBonusTasksDetail = "GetInvitedBonusTasksDetail",
    Refresh_ActivitiesPreview = "Refresh_ActivitiesPreview",
    Refresh_WheelEvent = "Refresh_WheelEvent",
    Get_WheelEventReward = "Get_WheelEventReward",
    Get_WheelEventStageReward = "Get_WheelEventStageReward",
    Refresh_Bingo = "Refresh_Bingo",
    Refresh_BattleDrop = "Refresh_BattleDrop",
    Refresh_BingoRedPoint = "Refresh_BingoRedPoint",
    Refresh_FlipCard = "Refresh_FlipCard",
    Refresh_MalteseCheckIn = "Refresh_MalteseCheckIn",
    Refresh_Magnet = "Refresh_Magnet",
    Refresh_SemiCheckIn = "Refresh_SemiCheckIn",
    Refresh_CrossService_CastRecords = "Refresh_CrossService_CastRecords",    --跨服活动浇筑记录
    Refresh_CrossService_Task = "Refresh_CrossService_Task",    --领取任务刷新
    Refresh_CrossService_Shop = "Refresh_CrossService_Shop",    --兑换商品后刷新
    Refresh_CrossService_Forge = "Refresh_CrossService_Forge",    --浇铸后刷新
    Refresh_CrossService_RedObj = "Refresh_CrossService_RedObj",    --按钮红点
    Refresh_CrossService_Currency = "Refresh_CrossService_Currency",    --货币栏目
    Refresh_ReturnView = "Refresh_ReturnView", --回归活动
    
    Refresh_EndlessEvents = "Refresh_EndlessEvents",
    Get_EndlessEventsTasksRewards = "Get_EndlessEventsTasksRewards",
    Get_EndlessEventsSwitchList = "Get_EndlessEventsSwitchList",
    Switch_EndlessEvents = "Switch_EndlessEvents",
    
    Refresh_NewInviteAccept = "Refresh_NewInviteAccept",
    Refresh_NewInviteGiftPack = "Refresh_NewInviteGiftPack",
    Refresh_NewInviteBonusTasks = "Refresh_NewInviteBonusTasks",

    Refresh_TransferJob = "Refresh_TransferJob",

    -- 旅团
    GuildRefreshViewBtns = "GuildRefreshViewBtns",
    GuildInfoChanged = "GuildInfoChanged",
    GuildMemberListChanged = "GuildMemberListChanged",
    RecommendGuildListChanged = "RecommendGuildListChanged",
    GuildPropChanged = "GuildPropChanged",
    LastSearchGuildChanged = "LastSearchGuildChanged",
    GuildCreateResponse = "GuildCreateResponse",
    GuildJoinMessage = "GuildJoinMessage",
    GuildGetAppliesResponse = "GuildGetAppliesResponse",
    GuildRejectAppliesResponse = "GuildRejectAppliesResponse",
    GuildAcceptApplyResponse = "GuildAcceptApplyResponse",
    GuildGetMemberListResponse = "GuildGetMemberListResponse",
    GuildUpdateResponse = "GuildUpdateResponse",
    QuitGuildEvent = "QuitGuildEvent",
    GuildUpdateLog = "GuildUpdateLog",
    GuildSceneLoadComplete = "GuildSceneLoadComplete",
    GuildWishWallUpdate = "GuildWishWallUpdate",

    --旅团副本
    RefreshAchieveReward = "RefreshAchieveReward",

    --Fashion
    OnEquippedFashionItemAtClosetView = "OnEquippedFashionItemAtClosetView", --角色时装改变
    RefreshFashionClosetViewToTargetPage = "RefreshFashionClosetViewToTargetPage", --刷新衣柜界面
    OnAvatarSceneVMCameraChange = "OnAvatarSceneVMCameraChange", --角色衣柜/时装背景相机改变
    OnAvatarClickPageItem = "OnAvatarClickPageItem", --
    OnFashionShopItemClick = "OnFashionShopItemClick", --时装商品点击
    OnFashionShopReset = "OnFashionShopReset", --时装商店重置
    RefreshFashionShopView = "RefreshFashionShopView", --刷新时装商店视图
    ChangeGoodsItemViewSelectState = "ChangeGoodsItemViewSelectState", --时装商品修改选中态
    OnPayForShoppingCartSuccess = "OnPayForShoppingCartSuccess",
    RefreshShopShoppingCartIcon = "RefreshShopShoppingCartIcon", --刷新购物车缩略图
    OnSetFashionItemColorAtClosetView = "OnSetFashionItemColorAtClosetView", --时装设置染色数据
    ON_FASHION_UNLOCK_COLOR = "ON_FASHION_UNLOCK_COLOR", --时装解锁染色
    ON_FASHION_SHOP_REFRESH_COLOR_SET_VIEW = "ON_FASHION_SHOP_REFRESH_COLOR_SET_VIEW", --时装商店刷新染色视图
    OnRefreshAvatarClosetFashionItemColorIcon = "OnRefreshAvatarClosetFashionItemColorIcon", --刷新当前衣柜视图中的染色环
    OnCloseSetColorView = "OnCloseSetColorView", --关闭染色环,
    RefreshSaveCollocationView = "RefreshSaveCollocationView", --刷新保存搭配

    --背包
    RefreshBagView = "RefreshBagView",
    RefreshFragMeltView = "RefreshFragMeltView",

    RefreshMatchTime = "RefreshMatchTime",
    --团本排行榜
    RefreshWorldBossGetRankList = "RefreshWorldBossGetRankList",
    RefreshWorldBossAchieveItems = "RefreshWorldBossAchieveItems",
    RefreshWorldBossProcess = "RefreshWorldBossProcess",
    RefreshWorldbossBoxReward = "RefreshWorldbossBoxReward",
    -- RefreshWorldbossItemRed = "RefreshWorldbossItemRed",

    -- 寻宝
    AdventureInfoChanged = "AdventureInfoChanged",
    AdventureRewardInfo = "AdventureRewardInfo",
    AdventureLotteryStart = "AdventureLotteryStart",
    AdventureLotteryStop = "AdventureLotteryStop",
    AdventureLotteryShow = "AdventureLotteryShow",
    AdventureLotteryFinish = "AdventureLotteryFinish",
    AdventureVerifyFinish = "AdventureVerifyFinish",
    AdventureAreaRedPointUpdate = "AdventureAreaRedPointUpdate",
    AdventureRefreshDroppedPower = "AdventureRefreshDroppedPower",
    -- 便捷购买
    EasyBuySuccess = "EasyBuySuccess",
    EasyBuyChangeCount = "EasyBuyChangeCount",
    -- 
    ActivityCenter_SwitchToTab = "ActivityCenter_SwitchToTab",
    ActivityCenter_BPViewRefresh = "ActivityCenter_BPViewRefresh",

    --Efficiency
    Efficiency_RefreshAtTheEndOfBattleOrJoinRoom = "Efficiency_RefreshAtTheEndOfBattleOrJoinRoom",
    Efficiency_Refresh = "Efficiency_Refresh",
    Efficiency_Main_Refresh = "Efficiency_Refresh",

    Refreshed_LevelLimitInfo = "Refreshed_LevelLimitInfo",
    Refreshed_WorldLevelInfo = "Refreshed_WorldLevelInfo",

    -- Farm
    FarmUpdateOrders = "FarmUpdateOrders",
    FarmUpdateEntryRedPoint = "FarmUpdateEntryRedPoint",
    FarmUpdateHandbookRedPoint = "FarmUpdateHandbookRedPoint",
    FarmUpdateSoils = "FarmUpdateSouls",
    FarmUpdateBase = "FarmUpdateBase",
    FarmUpdateShop = "FarmUpdateShop",
    FarmClickSoil = "FarmSceneClickSoil",
    FarmOverSoil = "FarmSceneOverSoil",
    FarmMouseDownSoil = "FarmMouseDownSoil",
    FarmUpdateRoles = "FarmUpdateRoles",
    FarmRemovePlant = "FarmRemovePlant",
    FarmGrowInfoHideEvent = "FarmGrowInfoHideEvent",
    FarmDropCoinPickUp = "FarmDropCoinPickUp",
    FarmUpdateBookRewards = "FarmUpdateBookRewards",
    FarmUpdateBookFirstRewards = "FarmUpdateBookFirstRewards",
    FarmRefreshSelectNum = "FarmRefreshSelectNum",
    FarmRefreshFriendList = "FarmRefreshFriendList",
    FarmRefreshLogs = "FarmRefreshLogs",
    FarmRefreshCooperateOrders = "FarmRefreshCooperateOrders",
    FarmRefreshInviteOrders = "FarmRefreshInviteOrders",
    FarmRefreshName = "FarmRefreshName",
    FarmSceneRemovePlantFx = "FarmSceneRemovePlantFx",
    FarmInviteNumUpdate = "FarmInviteNumUpdate",

    --PIDSettings
    RefreshHeadOrHeadFrameView = "RefreshHeadOrHeadFrameView",
    RefreshPersonData = "RefreshPersonData",
    --challenge
    RefreshChallengeMapInfos = "RefreshChallengeMapInfos",
    RefreshChallengeGetRankList = "RefreshChallengeGetRankList",
    RefreshChallengeRewardProcess = "RefreshChallengeRewardProcess",
    --guard
    RefreshGuardMapInfos = "RefreshGuardMapInfos",
    RefreshGuardTaskState = "RefreshGuardTaskState",
    GuardSeasonLastTime = "GuardSeasonLastTime",
    --bigrush
    RefreshBigRushTask =  "RefreshBigRushTask",
    RefreshBigRushCheckInfoData = "RefreshBigRushCheckInfoData",
    BigRushShowGetStageRewardEffect = "BigRushShowGetStageRewardEffect",
    RefreshBigrushProcess = "RefreshBigrushProcess",
    RefreshFinalMemoryGetRankList = "RefreshFinalMemoryGetRankList",
    RefreshFinalMemoryRewardProcess = "RefreshFinalMemoryRewardProcess",
    --shackle
    RefreshShackleProcess = "RefreshShackleProcess",
    --AvatarLottery
    AvatarLottery_RefreshPoolShow = "AvatarLottery_RefreshPoolShow",

    PayWithVoucherSuccess = "PayWithVoucherSuccess",
    PayWithVoucherFailed = "PayWithVoucherFailed",

    -- 
    RefreshTriggerGiftPacks = "RefreshTriggerGiftPacks",

    ---个人背景板
    RefreshRoleBackdrop = "RefreshRoleBackdrop",
    RefreshBackdropItem = "RefreshBackdropItem",  ---刷新背景item
    OnBackdropItemClick = "OnBackdropItemClick",
    OnRoleBackdropExit = "OnRoleBackdropExit",
    --
    
    OnAvatarFashionShowViewClose = "OnAvatarFashionShowViewClose",
     
    ---- dog
    RefreshLineDogMatch = "RefreshLineDogMatch",
    LineDogAddLove = "LineDogAddLove",

    ---- minigame fish
    MiniGameFishRefresh = "MiniGameFishRefresh",
    LoginMiniGameFishSuccess = "LoginMiniGameFishSuccess",
    RefreshMiniGameFishInfo = "RefreshMiniGameFishInfo",
    MiniGameFishSelectSuccess = "MiniGameFishSelectSuccess",
    GetMiniGameFishRankList = "GetMiniGameFishRankList",
    PushMiniGameFish = "PushMiniGameFish",
    PushMiniGameFishEnd = "PushMiniGameFishEnd",

    --- minigame color match
    MiniGameColorMatchRefresh = "MiniGameColorMatchRefresh",
    MiniGameColorMatchParam = "MiniGameColorMatchParam",
    LoginMiniGameColorMatchSuccess = "LoginMiniGameColorMatchSuccess",
    MiniGameColorMatchSelectSuccess = "MiniGameColorMatchSelectSuccess",
    PushMiniGameColorMatch = "PushMiniGameColorMatch",
    PushMiniGameColorMatchEnd = "PushMiniGameColorMatchEnd",
    
    -----------------------------------------
    OnGetOperationTeamInfo = "OnGetOperationTeamInfo",
    OnCreateOperationTeamSuccess = "OnCreateOperationTeamSuccess",
    OnGetOperationTeamApplies = "OnGetOperationTeamApplies",
    OnGetOperationTeamsInfoByRoleIds = "OnGetOperationTeamsInfoByRoleIds",
    OnGetOperationTeamsInfoByShortId = "OnGetOperationTeamsInfoByShortId",
    OnGetOperationTeamHallList = "OnGetOperationTeamHallList",
    
    OnOperationTeamInviteSuccess = "OnOperationTeamInviteSuccess",
    
    OnLeaveOperationTeam = "OnLeaveOperationTeam",
    
    OnApplyOperationTeamResult = "OnApplyOperationTeamResult",
    OnRejectOperationTeamPlayerApply = "OnRejectOperationTeamPlayerApply",
    OnAcceptOperationTeamPlayerApply = "OnAcceptOperationTeamPlayerApply",
    
    OnRejectOperationTeamInvite = "OnRejectOperationTeamInvite",
    OnAcceptOperationTeamInvite = "OnAcceptOperationTeamInvite",
    
    OnKickOutOperationTeamMember = "OnKickOutOperationTeamMember",
    OnLeaderStartReadyOperationTeam = "OnLeaderStartReadyOperationTeam",
    OnMemberReadyOperationTeam = "OnMemberReadyOperationTeam",
    UpdateSettingsOperationTeam = "UpdateSettingsOperationTeam",

    ActivityTeamStartMatchResult = "ActivityTeamStartMatchResult",
    ActivityTeamMatchResult = "ActivityTeamMatchResult",
    
    -- operation team push
    OnPushOperationTeamUpdate = "OnPushOperationTeamUpdate",
    OnPushOperationNewPlayerApplyToTeam = "OnOperationNewPlayerApplyToTeam",
    OnPushOperationTeamInfoAfterAddMember = "OnOperationTeamInfoAfterAddMember",
    OnPushOperationTeamInfoAfterRemoveMember = "OnOperationTeamInfoAfterRemoveMember",
    OnPushOperationTeamKickOutMember = "OnPushOperationTeamKickOutMember",

    OnPushOperationTeamInfo = "OnPushOperationTeamInfo",
    OnPushOperationTeamInvite = "OnPushOperationTeamInvite",
    OnPushOperationTeamSetting = "OnPushOperationTeamSetting",
    
    
    ------
    CheckFashionGiveLimitResult = "CheckFashionGiveLimitResult",
    GetFashionGiveListResult = "GetFashionGiveListResult",
    DelFashionGiveListResult = "DelFashionGiveListResult",
    GetFashionGiveRewardResult = "GetFashionGiveRewardResult",
    VoucherFashionGiveResult = "VoucherFashionGiveResult",
    
    ---旅团拼图
    GuildPuzzleBagUpdate = "GuildPuzzleBagUpdate",
    GuildPuzzleOrderUpdate = "GuildPuzzleBagUpdate",
    GuildPuzzleOrderListUpdate = "GuildPuzzleOrderListUpdate",
    UpdateGuildPuzzleSelectData = "UpdateGuildPuzzleSelectData",
    UpdateOrderListShowState = "UpdateOrderListShowState",
    UpdateGuildRemovePuzzle = "UpdateGuildRemovePuzzle",
    GetGuildPuzzleDataUpdate = "GetGuildPuzzleDataUpdate",
}
