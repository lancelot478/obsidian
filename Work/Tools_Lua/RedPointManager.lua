local _ENV = Boli2Env

RedPointManager = {}

RedType = {
    Common = 1, --通用红点
    Full = 2 --满红点
}
RedID = {
    --家园
    HomeBuild = "HomeBuild",
    OpenServerActivities = "OpenServerActivities",
    OpenServerActivities_CommonPacks = "OpenServerActivities_CommonPacks",
    OpenServerActivities_ChargeRebate = "OpenServerActivities_ChargeRebate",
    HomeBuildActivities = "HomeBuildActivities",
    HomeBuildActivitiesFashionDraw = "HomeBuildActivitiesFashionDraw",
    HomeBuildActivities_TabBanners = "HomeBuildActivities_TabBanners",
    HomeBuildActivities_FollowSocialMedia = "HomeBuildActivities_FollowSocialMedia",
    HomeBuildActivities_baseCharges_4 = "HomeBuildActivities_baseCharges_4",
    HomeBuildActivities_logins_1 = "HomeBuildActivities_logins_1",
    HomeBuildActivities_growthReward = "HomeBuildActivities_growthReward",
    HomeBuildActivities_cumuCharge = "HomeBuildActivities_cumuCharge",
    HomeBuildActivities_monthSign = "HomeBuildActivities_monthSign",
    HomeBuildActivities_weekCycle = "HomeBuildActivities_weekCycle",
    HomeBuildActivities_weekCycle_Point = "HomeBuildActivities_weekCycle_Point",
    HomeBuildActivities_weekCycle_Exchange = "HomeBuildActivities_weekCycle_Exchange",
    HomeBuildActivities_weekCycle_Sell = "HomeBuildActivities_weekCycle_Sell",
    HomeBuildActivities_TabGiftPacks = "HomeBuildActivities_TabGiftPacks", -- gift
    HomeBuildActivities_GiftPack = "HomeBuildActivities_GiftPack",
    HomeBuildActivities_GiftPack9 = "HomeBuildActivities_GiftPack9",
    HomeBuildActivities_GiftPack10 = "HomeBuildActivities_GiftPack10",
    HomeBuildActivities_GiftPack11 = "HomeBuildActivities_GiftPack11",
    HomeBuildActivities_TabMonthCard = "HomeBuildActivities_TabMonthCard",
    HomeBuildActivities_MonthCard = "HomeBuildActivities_MonthCard",
    HomeBuildActivities_TabBP = "HomeBuildActivities_TabBP",
    HomeBuildActivities_TabCharge = "HomeBuildActivities_TabCharge",
    HomeBuildServerEvents = "HomeBuildServerEvents",
    HomeBuildServerEvents_Road = "HomeBuildServerEvents_Road",
    HomeBuildServerEvents_Epic = "HomeBuildServerEvents_Epic",
    HomeBuildServerEvents_Evolve = "HomeBuildServerEvents_Evolve",
    HomeBuildActivities_GridWalk = "HomeBuildActivities_GridWalk_",
    HomeBuildActivities_InviteGiftPack = "HomeBuildActivities_InviteGiftPack",
    HomeBuildActivities_InviteReward = "HomeBuildActivities_InviteReward",
    HomeBuildActivities_TimeLimitedPacks = "HomeBuildActivities_TimeLimitedPacks",
    HomeBuildActivities_WheelCycle = "HomeBuildActivities_WheelCycle",
    HomeBuildActivities_Bingo = "HomeBuildActivities_Bingo",
    HomeBuildActivities_BingoComboReward = "HomeBuildActivities_BingoComboReward",
    HomeBuildActivities_BingoBag = "HomeBuildActivities_BingoBag",
    HomeBuildActivities_BingoRecord = "HomeBuildActivities_BingoRecord",
    HomeBuildActivities_BingoActiveGrid = "HomeBuildActivities_BingoActiveGrid",
    HomeBuildActivities_BingoCanDraw = "HomeBuildActivities_BingoCanDraw",
    HomeBuildActivities_FlipCard = "HomeBuildActivities_FlipCard",
    HomeBuildActivities_FlipCardCanFlip = "HomeBuildActivities_FlipCardCanFlip",
    HomeBuildActivities_FlipCardFreeGift = "HomeBuildActivities_FlipCardFreeGift",
    HomeBuildActivities_FlipCardExchange = "HomeBuildActivities_FlipCardExchange",
    HomeBuildActivities_GiftLimitPack = "HomeBuildActivities_GiftLimitPack",
    HomeBuildActivities_LinedogMatch = "HomeBuildActivities_LinedogMatch",
    HomeBuildActivities_Magnet = "HomeBuildActivities_Magnet",
    HomeBuildActivities_MagnetNormal = "HomeBuildActivities_MagnetNormal",
    HomeBuildActivities_MagnetSpecial = "HomeBuildActivities_MagnetSpecial",
    HomeBuildActivities_MagnetNormalDrawLess = "HomeBuildActivities_MagnetNormalDrawLess",
    HomeBuildActivities_MagnetNormalDrawMore = "HomeBuildActivities_MagnetNormalDrawMore",
    HomeBuildActivities_MagnetSpecialDrawSingle = "HomeBuildActivities_MagnetSpecialDrawSingle",
    HomeBuildActivities_MagnetNormalExchange = "HomeBuildActivities_MagnetNormalExchange",
    HomeBuildActivities_MagnetSpecialExchange = "HomeBuildActivities_MagnetSpecialExchange",
    HomeBuildActivities_MiniGameFish = "HomeBuildActivities_MiniGameFish",
    HomeBuildActivities_InvitePlayerNew = "HomeBuildActivities_InvitePlayerNew",
    HomeBuildActivities_InvitePlayerOld = "HomeBuildActivities_InvitePlayerOld",
    HomeBuildActivities_InvitePlayerNewApply = "HomeBuildActivities_InvitePlayerNewApply",
    HomeBuildActivities_InvitePlayerOldApply = "HomeBuildActivities_InvitePlayerOldApply",
    HomeBuildActivities_FarmHarvest = "HomeBuildActivities_FarmHarvest",
    HomeBuildActivities_FarmOrder = "HomeBuildActivities_FarmOrder",
    HomeBuildActivities_FarmOtherRed = "HomeBuildActivities_FarmOtherRed",

    Activity_TransferJobRed = "Activity_TransferJobRed",
    
    Activities_SemiAnnual = "Activities_SemiAnnual",
    Activities_XLLPEvent = "Activities_XLLPEvent",
    Activities_ColorMatch = "Activities_ColorMatch",
    HomeBuildActivities_FarmAllRed = "HomeBuildActivities_FarmAllRed",

    --背包
    Bag = "Bag",
    BagEquip = "BagEquip",
    BagFull = "BagFull",
    BagItem = "BagItem",
    BagFrag = "BagFrag",
    --人物
    Role = "Role",
    RoleInnate = "RoleInnate", --天赋
    RoleSkill = "RoleSkill", --技能
    RoleRelic = "RoleRelic", --秘宝
    RoleMount = "RoleMount", --坐骑
    RoleCat = "RoleCat", --猫猫包
    --宠物
    Pet = "Pet",

    --队伍
    Team = "Team",

    --秘境
    Trial = "Trial",
    Instance = "Instance",
    WorldBoss = "WorldBoss",
    Challenge = "Challenge",
    Guard = "Guard",
    BigRush = "BigRush",
    FinalMemory = "FinalMemory",
    Shackle = "Shackle",

    -- 旅团
    Guild = "Guild",
    GuildSignIn = "GuildSignIn",
    GuildRaid = "GuildRaid",
    GuildRaidKey = "GuildRaidKey",
    GuildInfo = "GuildInfo",
    GuildActivity = "GuildActivity",
    GuildInvite = "GuildInvite",
    GuildApply = "GuildApply",
    GuildActivity_Task = "GuildActivity_Task",
    GuildJoin = "GuildJoin",
    GuildActivity_Achievement = "GuildActivity_Achievement",
    GuildWishWallReceiveEntry = "GuildWishWallReceiveEntry",
    GuildWishWallEntry = "GuildWishWallEntry",
    GuildWishWallReceive = "GuildWishWallReceive",
    GuildWishWallCanPublish = "GuildWishWallCanPublish",
    GuildWishWallCanReward = "GuildWishWallCanReward",
    GuildWishWallDailyPublish = "GuildWishWallDailyPublish",
    GuildPuzzleEntry = "GuildPuzzleEntry",

    --冒险手册
    Task = "Task",
    TaskMainLine = "TaskMainLine",
    TaskDaily = "TaskDaily",
    TaskWeek = "TaskWeek",
    TaskAchievement = "TaskAchievement",
    --新手试炼
    NewTrain = "NewTrain",
    --跨服活动
    CrossService = "CrossService",
    --回归老服活动
    Activity_Return = "Activity_Return",

    --私聊
    Chat = "Chat",

    --个人信息
    RoleInfo = "RoleInfo",
    RoleBadge = "RoleBadge", --角色勋章
    HeadOrHeadFrame = "HeadOrHeadFrame", --头像头像框
    PIDHeadFrame = "PIDHeadFrame", --个人设置头像框
    PIDHead = "PIDHead", --个人设置头像
    RoleBackdrop = "RoleBackdrop", --个人背景板
    RolePose = "RolePose", --个人动作

    --时装
    FashionShop = "FashionShop",
    FashionShopLimitTimePage = "FashionShopLimitTimePage", --时装商店限时页签
    FashionLottery = "FashionLottery", --时装抽奖
    FashionLottery_LogReward = "FashionLottery_LogReward", --时装抽奖登录奖励

    --限时宠物抽
    PetTimeLimitedLottery = "PetTimeLimitedLottery", --限时宠物抽取
    PetTimeLimitedLotteryShop = "PetTimeLimitedLotteryShop", --限时宠物抽取商店

    --猫猫虫
    GridWalkStart = "GridWalkStart_",
    GridWalkCircleReward = "GridWalkCircleReward_",
    GridWalkExchangeShop = "GridWalkExchangeShop_",
    GridWalkTask = "GridWalkTask_",
    GridWalkGivePet = "GridWalkGivePet_",
    GridWalkFreeGift = "GridWalkFreeGift_",

    ---种田
    FarmHandBook = "FarmHandBook",
    FarmOrder = "FarmOrder",
    FarmCanCommit = "FarmCanCommit",
    --ChatPrivateTab = "ChatPrivateTab",

}
local redPoints = {}

local function HasHomeBuildRedPoint()
    if MainInterfaceData.IsTabUnlock(1) then
        return
        AugurManager.CardMaterialBool()
                or MailManager.CheckRedPoint(true)
                --or  GlobalData.roleData:HaveServerEventsRedPoint()-- TasksData.HasRedPointByClassify(TaskConfig.TabType.EvolveJob)
                or EquipConfig.HasRedPointBlackSmith()
                or AdventureData.CheckEntryRedPoint()
                or ShopManager.HasRedPointInHomeBuild()
                or PetDivineManager.IsRedPoint()
                or BoardManager.CheckRpFunc()
    end
    return false
end

--info:{redID, redType, refreshFun, fatherRedID}
local function GetConfig()
    local config = {
        --营地
        { RedID.HomeBuild, RedType.Common, HasHomeBuildRedPoint, nil },
        -- { RedID.FashionLottery, RedType.Common, AvatarFashionLotteryData.HasRedDot, nil },
        -- { RedID.FashionLottery_LogReward, RedType.Common, AvatarFashionLotteryData.HasLogRewardCanReceive, nil },
        --营地宠物限时抽取活动
        { RedID.PetTimeLimitedLotteryShop, RedType.Common, PetActivityLotteryData.CanBuyFreeGift, nil },
        { RedID.PetTimeLimitedLottery, RedType.Common, PetActivityLotteryData.HasRedDot, nil },
        --{ RedID.FashionShop, RedType.Common, function()
        --    return AvatarFashionShopData.CheckConstShopRedDot()
        --end, RedID.HomeBuild },
        --
        --{ RedID.FashionShopLimitTimePage, RedType.Common, function()
        --    return AvatarFashionShopData.CheckLimitShopRedDot()
        --end,  RedID.FashionShop },
        { RedID.HomeBuildActivities, RedType.Common, ActivitiesData.HaveRedPoint, RedID.HomeBuild },
        { RedID.HomeBuildActivities_TabBanners, RedType.Common, ActivitiesData.HasRedPointOfBanners, RedID.HomeBuildActivities },
        { RedID.HomeBuildActivities_FollowSocialMedia, RedType.Common, function()
            return ActivitiesData.HaveSocialMediaRedpoint() or ActivitiesData.HaveSocialMediaRedpoint2()
        end, RedID.HomeBuildActivities_TabBanners },
        { RedID.HomeBuildActivities_baseCharges_4, RedType.Common, ActivitiesData.HaveFirstChargeRedpoint, nil },
        { RedID.HomeBuildActivities_logins_1, RedType.Common, ActivitiesData.HasNewLoginRedPoint, RedID.HomeBuildActivities_TabBanners },
        { RedID.HomeBuildActivities_growthReward, RedType.Common, ActivitiesData.HasGrowthBenefitsRedPoint, RedID.HomeBuildActivities_TabBanners },
        { RedID.HomeBuildActivities_cumuCharge, RedType.Common, nil, RedID.HomeBuildActivities_TabBanners },
        { RedID.HomeBuildActivities_monthSign, RedType.Common, ActivitiesData.HasMonthlyCheckInRedPoint, RedID.HomeBuildActivities_TabBanners },
        { RedID.HomeBuildActivities_weekCycle, RedType.Common, ActivitiesData.HaveThreeWeekRedPoint, RedID.HomeBuildActivities_TabBanners },
        { RedID.HomeBuildActivities_weekCycle_Point, RedType.Common, ActivitiesData.HaveThreeWeekRedPoint_Point, RedID.HomeBuildActivities_weekCycle },
        { RedID.HomeBuildActivities_weekCycle_Exchange, RedType.Common, ActivitiesData.HaveThreeWeekRedPoint_Exchange, RedID.HomeBuildActivities_weekCycle },
        { RedID.HomeBuildActivities_weekCycle_Sell, RedType.Common, ActivitiesData.HaveThreeWeekRedPoint_Sell, RedID.HomeBuildActivities_weekCycle },
        { RedID.HomeBuildActivities_InviteGiftPack, RedType.Common, ActivitiesData.HaveInviteGiftPackRedpoint, nil },
        -- { RedID.HomeBuildActivities_WheelCycle, RedType.Common, ActivitiesData.HaveWheelCircleRedPoint, RedID.HomeBuildActivities_TabBanners },
        { RedID.HomeBuildActivities_InviteReward, RedType.Common, ActivitiesData.HaveInviteRewardRedpoint, RedID.HomeBuildActivities_TabBanners },

        { RedID.HomeBuildActivities_Bingo, RedType.Common, nil, nil },
        { RedID.HomeBuildActivities_BingoComboReward, RedType.Common, BingoData.CheckComboRewardRedPoint, RedID.HomeBuildActivities_Bingo },
        { RedID.HomeBuildActivities_BingoBag, RedType.Common, nil, RedID.HomeBuildActivities_Bingo },
        { RedID.HomeBuildActivities_BingoRecord, RedType.Common, BingoData.CheckNewRecord, RedID.HomeBuildActivities_BingoBag },
        { RedID.HomeBuildActivities_BingoActiveGrid, RedType.Common, BingoData.CheckActiveGrid, RedID.HomeBuildActivities_Bingo },
        { RedID.HomeBuildActivities_BingoCanDraw, RedType.Common, BingoData.CheckCanDraw, RedID.HomeBuildActivities_Bingo },

        { RedID.HomeBuildActivities_FlipCard, RedType.Common, nil, nil },
        { RedID.HomeBuildActivities_FlipCardCanFlip, RedType.Common, FlipCardData.CheckRedCanFlip, RedID.HomeBuildActivities_FlipCard },
        { RedID.HomeBuildActivities_FlipCardFreeGift, RedType.Common, FlipCardData.CheckRedFreeGift, RedID.HomeBuildActivities_FlipCard },
        { RedID.HomeBuildActivities_FlipCardExchange, RedType.Common, FlipCardData.CheckRedExchange, RedID.HomeBuildActivities_FlipCard },

        { RedID.HomeBuildActivities_Magnet, RedType.Common, MagnetData.CheckRedFirstEnter, nil },
        { RedID.HomeBuildActivities_MagnetNormal, RedType.Common, nil, RedID.HomeBuildActivities_Magnet },
        { RedID.HomeBuildActivities_MagnetSpecial, RedType.Common, nil, RedID.HomeBuildActivities_Magnet },
        { RedID.HomeBuildActivities_MagnetNormalDrawLess, RedType.Common, MagnetData.CheckRedNormalDrawLess, RedID.HomeBuildActivities_MagnetNormal },
        { RedID.HomeBuildActivities_MagnetNormalDrawMore, RedType.Common, MagnetData.CheckRedNormalDrawMore, RedID.HomeBuildActivities_MagnetNormal },
        { RedID.HomeBuildActivities_MagnetSpecialDrawSingle, RedType.Common, MagnetData.CheckRedSpecialDraw, RedID.HomeBuildActivities_MagnetSpecial },
        { RedID.HomeBuildActivities_MagnetNormalExchange, RedType.Common, MagnetData.CheckRedNormalExchange, RedID.HomeBuildActivities_MagnetNormal },
        { RedID.HomeBuildActivities_MagnetSpecialExchange, RedType.Common, MagnetData.CheckRedSpecialExchange, RedID.HomeBuildActivities_MagnetSpecial },

        { RedID.HomeBuildActivities_LinedogMatch, RedType.Common, ActivitiesData.HasLinedogRedpoint, RedID.HomeBuildActivities_TabBanners },
        { RedID.HomeBuildActivities_MiniGameFish, RedType.Common, ActivitiesData.MiniGameFish_HasRedPoint, nil },
        { RedID.Activities_ColorMatch, RedType.Common, ActivitiesData.MiniGameColorMatch_HasRedPoint, nil },

        { RedID.HomeBuildActivities_InvitePlayerNew, RedType.Common, ActivitiesData.HaveNewInviteRedPoint, RedID.HomeBuildActivities_TabBanners },
        { RedID.HomeBuildActivities_InvitePlayerOld, RedType.Common, ActivitiesData.HaveOldInviteRedPoint, RedID.HomeBuildActivities_TabBanners },
        { RedID.HomeBuildActivities_InvitePlayerNewApply, RedType.Common, ActivitiesData.HaveNewInviteNum, RedID.HomeBuildActivities_TabBanners },
        { RedID.HomeBuildActivities_InvitePlayerOldApply, RedType.Common, ActivitiesData.HaveOldInviteNum, RedID.HomeBuildActivities_TabBanners },

        { RedID.HomeBuildActivities_TabGiftPacks, RedType.Common, ActivitiesData.HasGiftBoxRedpoint, RedID.HomeBuildActivities },
        { RedID.HomeBuildActivities_GiftPack, RedType.Common, ActivitiesData.HasGiftBoxRedpoint, RedID.HomeBuildActivities_TabGiftPacks },
        { RedID.HomeBuildActivities_GiftPack9, RedType.Common, function()
            return ActivitiesData.HasGiftBoxRedpointOfType(9)
        end, RedID.HomeBuildActivities_GiftPack },
        { RedID.HomeBuildActivities_GiftPack10, RedType.Common, function()
            return ActivitiesData.HasGiftBoxRedpointOfType(10)
        end, RedID.HomeBuildActivities_GiftPack },
        { RedID.HomeBuildActivities_GiftPack11, RedType.Common, function()
            return ActivitiesData.HasGiftBoxRedpointOfType(11)
        end, RedID.HomeBuildActivities_GiftPack },
        { RedID.HomeBuildActivities_TabMonthCard, RedType.Common, ActivitiesData.HasMonthCardRedpoint, RedID.HomeBuildActivities },
        { RedID.HomeBuildActivities_MonthCard, RedType.Common, ActivitiesData.HasMonthCardRedpoint, RedID.HomeBuildActivities_TabMonthCard },
        { RedID.HomeBuildActivities_TabBP, RedType.Common, ActivitiesData.HasBPRedPoint, RedID.HomeBuildActivities },
        { RedID.HomeBuildActivities_TabCharge, RedType.Common, nil, RedID.HomeBuildActivities },

        { RedID.HomeBuildServerEvents, RedType.Common, nil, RedID.HomeBuild },
        { RedID.HomeBuildServerEvents_Road, RedType.Common, function()
            return GlobalData.roleData:HaveWorldLevelRedpoint()
        end, RedID.HomeBuildServerEvents },
        { RedID.HomeBuildServerEvents_Epic, RedType.Common, function()
            return GlobalData.roleData:HaveServerEventsEpicRedPoint()
        end, RedID.HomeBuildServerEvents },
        { RedID.HomeBuildServerEvents_Evolve, RedType.Common, function()
            GlobalData.roleData:HaveServerEventsEvolveRedPoint()
        end, RedID.HomeBuildServerEvents },

        { RedID.OpenServerActivities, RedType.Common, ActivitiesData.HaveOpenServerRedpoint, RedID.HomeBuild },
        { RedID.OpenServerActivities_CommonPacks, RedType.Common, function()
            return ActivitiesData.HaveCommonPackRedpoint("commonGiftPack")
        end, RedID.OpenServerActivities },
        { RedID.OpenServerActivities_ChargeRebate, RedType.Common, ActivitiesData.HaveOpenServerRebateRedpoint, RedID.OpenServerActivities },

        { RedID.HomeBuildActivities_TimeLimitedPacks, RedType.Common, function()
            return ActivitiesData.HaveCommonPackRedpoint("limitGiftPack")
        end, RedID.HomeBuild },
        { RedID.HomeBuildActivities_GiftLimitPack, RedType.Common, function()
            return ActivitiesData.HaveCommonPackRedpoint("discountGiftPack")
        end, RedID.HomeBuild },

        { RedID.HomeBuildActivities_FarmHarvest, RedType.Common, function()
            return ActivitiesData.GetIsRedStateInFarm(1)
        end, RedID.HomeBuild },
        { RedID.HomeBuildActivities_FarmOrder, RedType.Common, function()
            return ActivitiesData.GetIsRedStateInFarm(3)
        end, RedID.HomeBuild },
        { RedID.HomeBuildActivities_FarmOtherRed, RedType.Common, ActivitiesData.GetIsNormalRedStateInFarm, RedID.HomeBuild },
        {RedID.Activity_TransferJobRed,RedType.Common,function() return ActivitiesData.HasTransferJobCheckInRedPoint()  end, RedID.HomeBuildActivities_TabBanners},
        { RedID.HomeBuildActivities_FarmAllRed, RedType.Common, ActivitiesData.HasFarmRedPoint, RedID.HomeBuild },
        { RedID.Activities_SemiAnnual, RedType.Common, function()
            return not not ActivitiesData.OperHub_HasRedpointOfType(1) 
                    or ActivitiesData.OperHub_HasRedpointOfType(5)
        end },
        { RedID.Activities_XLLPEvent, RedType.Common, function()
            local types = ActivitiesData.OperHubs.XLLPEvent
            for k, v in pairs(types) do
                if ActivitiesData.OperHub_HasRedpointOfType(v) then 
                    return true 
                end
            end
            
            return false
        end },

        --背包
        { RedID.Bag, RedType.Common, nil, nil },
        { RedID.BagEquip, RedType.Common, BagData.HasEquipRedPoint, RedID.Bag },
        { RedID.BagFull, RedType.Full, BagData.HasFullRedPoint, RedID.Bag },
        { RedID.BagItem, RedType.Common, BagData.HasItemRedPoint, RedID.Bag },
        { RedID.BagFrag, RedType.Common, BagData.HasFragRedPoint, RedID.Bag },

        --人物
        { RedID.Role, RedType.Common, nil, nil },
        { RedID.RoleInnate, RedType.Common, InnateData.HasRedPoint, RedID.Role },
        { RedID.RoleSkill, RedType.Common,
          function()
              return GlobalData.roleData.roleSkillCardsData:HaveAnyRedpoint()
          end,
          RedID.Role
        },
        { RedID.RoleRelic, RedType.Common, RelicsData.HasRedPoint, RedID.Role },
        { RedID.RoleMount, RedType.Common, MountsData.HasRedPoint, RedID.Role },
        { RedID.RoleCat, RedType.Common, CatsData.HasRedPoint, RedID.Role },

        --宠物
        { RedID.Pet, RedType.Common, PetsData.HasRedPoint, nil },

        --队伍
        { RedID.Team, RedType.Common, TeamManager.CheckNewUnlockRedPoint, nil },
        --秘境
        { RedID.Trial, RedType.Common, nil, nil },
        { RedID.Instance, RedType.Common, CopyManager.IsRedPoint, RedID.Trial },
        { RedID.WorldBoss, RedType.Common, WorldBossManager.IsRedPoint, RedID.Trial },
        { RedID.Challenge, RedType.Common, ChallengeManager.IsRedPoint, RedID.Trial },
        { RedID.Guard, RedType.Common, GuardManager.IsRedPoint, RedID.Trial },
        { RedID.FinalMemory, RedType.Common, FinalMemoryManager.IsRedPoint, RedID.Trial },
        { RedID.BigRush, RedType.Common, BigRushManager.IsRedPoint },
        { RedID.Shackle, RedType.Common, ShackleManager.IsRedPoint , RedID.Trial },
        --旅团
        { RedID.Guild, RedType.Common, nil, nil },
        { RedID.GuildJoin, RedType.Common, Guild.CheckJoinGuildRedPoint, RedID.Guild },
        { RedID.GuildSignIn, RedType.Common, Guild.CheckSignInRedPoint, RedID.Guild },
        { RedID.GuildRaid, RedType.Common, Guild.CheckInvestigateRedPoint, RedID.Guild },
        { RedID.GuildRaidKey, RedType.Common, GuildInstanceManager.IsRedPoint, nil },
        { RedID.GuildActivity, RedType.Common, nil, RedID.Guild },
        { RedID.GuildActivity_Task, RedType.Common, Guild.CheckTaskRedPoint, RedID.GuildActivity },
        { RedID.GuildActivity_Achievement, RedType.Common, Guild.CheckAchievementRedPoint, RedID.GuildActivity },
        { RedID.GuildInvite, RedType.Common, Guild.CheckInviteRedPoint, RedID.Guild },
        { RedID.GuildInfo, RedType.Common, nil, RedID.Guild },
        { RedID.GuildApply, RedType.Common, Guild.CheckApplyRedPoint, RedID.GuildInfo },
        -- 旅团许愿
        { RedID.GuildWishWallReceiveEntry, RedType.Common, nil, nil },
        { RedID.GuildWishWallReceive, RedType.Common, GuildWishWallData.RedPointCheckReceive, RedID.GuildWishWallReceiveEntry },
        { RedID.GuildWishWallEntry, RedType.Common, nil, RedID.Guild },
        { RedID.GuildWishWallCanPublish, RedType.Common, GuildWishWallData.RedPointCheckCanPublish, RedID.GuildWishWallEntry },
        { RedID.GuildWishWallCanReward, RedType.Common, GuildWishWallData.RedPointCheckCanReward, RedID.GuildWishWallEntry },
        { RedID.GuildPuzzleEntry, RedType.Common, GuildPuzzleData.HasRedPoint },
        { RedID.GuildWishWallDailyPublish, RedType.Common, GuildWishWallData.RedPointCheckDailyPublish, RedID.GuildWishWallEntry },

        --冒险手册
        { RedID.Task, RedType.Common, TasksData.HasRedPoint, nil },
        { RedID.TaskMainLine, RedType.Common, function()
            return TasksData.HasRedPointByClassify(TaskConfig.TabType.MainLine)
        end, RedID.Task },
        { RedID.TaskDaily, RedType.Common, function()
            return TasksData.HasRedPointByClassify(TaskConfig.TabType.Daily)
        end, RedID.Task },
        { RedID.TaskWeek, RedType.Common, function()
            return TasksData.HasRedPointByClassify(TaskConfig.TabType.Week)
        end, RedID.Task },
        { RedID.TaskAchievement, RedType.Common, function()
            return TasksData.HasRedPointByClassify(TaskConfig.TabType.Achievement)
        end, RedID.Task },

        --新手试炼
        { RedID.NewTrain, RedType.Common, ActivitiesData.HasNewTrainRedPoint, nil },
        --跨服活动
        { RedID.CrossService, RedType.Common, CrossServiceConfig.HasRedPoint, nil },
        --回归老服活动
        { RedID.Activity_Return, RedType.Common, ReturnConfig.HasRedPoint, nil },

        --私聊红点
        { RedID.Chat, RedType.Common, nil, nil },

        --个人信息
        { RedID.RoleInfo, RedType.Common, nil, nil },
        { RedID.RoleBadge, RedType.Common, function()
            return PlayerInfoManager:GetSelfBadgePointStats()
        end, RedID.RoleInfo },
        { RedID.PIDHead, RedType.Common, function()
            return PlayerInfoManager:HasAnyHeadOrHeadFrameRedPoint(1)
        end, RedID.RoleBadge }, --PIDSetting
        { RedID.PIDHeadFrame, RedType.Common, function()
            return PlayerInfoManager:HasAnyHeadOrHeadFrameRedPoint(2)
        end, RedID.RoleBadge }, --PIDSetting
        { RedID.RoleBackdrop, RedType.Common, function()
            return PlayerInfoManager:HasAnyBackdropPoint()
        end, RedID.RoleInfo },
        { RedID.RolePose, RedType.Common, function()
            return PlayerInfoManager:HasAnyPoseRedPoint()
        end, RedID.RoleBackdrop },

        ---种田
        { RedID.FarmHandBook, RedType.Common, function()
            return ActivitiesData.GetIsRedStateInFarm(4)
        end },
        { RedID.FarmOrder, RedType.Common, function()
            return ActivitiesData.GetIsRedStateInFarm(3)
        end },
        { RedID.FarmCanCommit, RedType.Common, function()
            return (FarmData.UpdateOrderCanCommit() or ActivitiesData.GetIsRedStateInFarm(6) or ActivitiesData.GetIsRedStateInFarm(7)) and not ActivitiesData.GetIsRedStateInFarm(3)
        end }
        --{RedID.ChatPrivateTab, RedType.Common, PrivateChatsData.HasPrivateRedPoint, RedID.Chat},
    }

    -- 猫猫虫
    local gridWalkCount = ActivityGridWalkConfig.GetConfigCount()
    for i = 1, gridWalkCount do
        local gridWalkKey = string.format("%s%d", RedID.HomeBuildActivities_GridWalk, i)

        -- 顶层
        local topConfig = { gridWalkKey, RedType.Common, nil, RedID.HomeBuildServerEvents }
        table.insert(config, topConfig)

        -- 绕圈奖励
        local circleRewardKey = string.format("%s%d", RedID.GridWalkCircleReward, i)
        local circleRewardConfig = { circleRewardKey, RedType.Common, function()
            return GridWalkData.CheckCircleRewardRedPoint(i)
        end, gridWalkKey }
        table.insert(config, circleRewardConfig)

        -- 开始按钮
        local startKey = string.format("%s%d", RedID.GridWalkStart, i)
        local startConfig = { startKey, RedType.Common, function()
            return GridWalkData.CheckStartRedPoint(i)
        end, gridWalkKey }
        table.insert(config, startConfig)

        -- 兑换商店按钮
        local exchangeKey = string.format("%s%d", RedID.GridWalkExchangeShop, i)
        local exchangeConfig = { exchangeKey, RedType.Common, function()
            return GridWalkData.CheckExchangeRedPoint(i)
        end, gridWalkKey }
        table.insert(config, exchangeConfig)

        -- 任务
        local taskKey = string.format("%s%d", RedID.GridWalkTask, i)
        local taskConfig = { taskKey, RedType.Common, function()
            return GridWalkData.CheckTaskRedPoint(i)
        end, gridWalkKey }
        table.insert(config, taskConfig)

        -- 领取宠物
        local givePetKey = string.format("%s%d", RedID.GridWalkGivePet, i)
        local givePetConfig = { givePetKey, RedType.Common, function()
            return GridWalkData.CheckGivePetRedPoint(i)
        end, gridWalkKey }
        table.insert(config, givePetConfig)

        -- 免费礼包
        local freeGiftKey = string.format("%s%d", RedID.GridWalkFreeGift, i)
        local freeGiftConfig = { freeGiftKey, RedType.Common, function()
            return GridWalkData.CheckFreeGiftRedPoint(i)
        end, gridWalkKey }
        table.insert(config, freeGiftConfig)
    end

    return config
end

local function GetRedPoint(redID)
    return redPoints[redID]
end

local function SetRedObj(redID, redTra)
    local redPoint = GetRedPoint(redID)
    if redPoint then
        redPoint:SetRedTra(redTra)
    end
end

function RedPointManager.CheckActive(redID)
    local redPoint = GetRedPoint(redID)
    if not redPoint then
        return
    end
    return redPoint:IsActive()
end

function RedPointManager.SetRedObj(redID, redTra)
    SetRedObj(redID, redTra)
end

function RedPointManager.SetRedObjNil(redID)
    SetRedObj(redID, nil)
end

function RedPointManager.Refresh(redID)
    local redPoint = GetRedPoint(redID)
    if redPoint then
        redPoint:Refresh()
    end
end

--注册红点
function RedPointManager.Register()
    local config = GetConfig()
    for _, v in ipairs(config) do
        local redID, redType, refreshFun, fatherRedID = v[1], v[2], v[3], v[4]
        local father = GetRedPoint(fatherRedID)
        local redPoint = RedPoint:New(redID, redType, refreshFun, father)
        redPoints[redID] = redPoint
    end
    --红点数据刷新触发数据
    MsgMg.RegisterCallBack(
            UIEvent.SET_ITEM,
            function(item)
                local itemType = item.config.itemType
                if itemType == ItemType.DIAMOND_PAID or itemType == ItemType.DIAMOND_FREE then
                    RedPointManager.Refresh(RedID.HomeBuild) --麦芬小金库
                elseif itemType == ItemType.GIL or itemType == ItemType.EQUIP_MAT then
                    --装备强化材料刷新
                    GlobalData.equipsData:SetEquipedRedPointState()
                    RedPointManager.Refresh(RedID.BagEquip)
                    RedPointManager.Refresh(RedID.BagItem)
                elseif itemType == ItemType.BUILD_DRAW or item.config.id == 3 then
                    RedPointManager.Refresh(RedID.HomeBuild) --打造
                elseif itemType == ItemType.INNATE then
                    RedPointManager.Refresh(RedID.RoleInnate)
                elseif itemType == ItemType.TREASURE_BOX then
                    RedPointManager.Refresh(RedID.BagItem)
                elseif itemType == ItemType.SKILLCARDPIECE then
                    RedPointManager.Refresh(RedID.BagFrag)
                elseif itemType == ItemType.GIL or itemType == ItemType.PET_USE then
                    RedPointManager.Refresh(RedID.Pet)
                elseif itemType == ItemType.RELIC_MATERIAL then
                    RedPointManager.Refresh(RedID.RoleRelic)
                elseif itemType == ItemType.BINGO_NUMBER then
                    RedPointManager.Refresh(RedID.HomeBuildActivities_BingoActiveGrid)
                elseif itemType == ItemType.ACTIVITY_PROP then
                    RedPointManager.Refresh(RedID.HomeBuildActivities_FlipCardCanFlip)
                    RedPointManager.Refresh(RedID.HomeBuildActivities_FlipCardExchange)
                elseif itemType == ItemType.CURRENCY then
                    RedPointManager.Refresh(RedID.GuildWishWallDailyPublish)
                end
                -- Magnet
                RedPointManager.Refresh(RedID.HomeBuildActivities_MagnetNormalDrawLess)
                RedPointManager.Refresh(RedID.HomeBuildActivities_MagnetNormalDrawMore)
                RedPointManager.Refresh(RedID.HomeBuildActivities_MagnetNormalExchange)
                RedPointManager.Refresh(RedID.HomeBuildActivities_MagnetSpecialDrawSingle)
                RedPointManager.Refresh(RedID.HomeBuildActivities_MagnetSpecialExchange)
                -- Bingo
                RedPointManager.Refresh(RedID.HomeBuildActivities_BingoCanDraw)
                -- 猫猫虫
                local gridWalkCount = ActivityGridWalkConfig.GetConfigCount()
                for idx = 1, gridWalkCount do
                    local startKey = string.format("%s%d", RedID.GridWalkStart, idx)
                    RedPointManager.Refresh(startKey)
                    local exchangeKey = string.format("%s%d", RedID.GridWalkExchangeShop, idx)
                    RedPointManager.Refresh(exchangeKey)
                end
            end
    )
    MsgMg.RegisterCallBack(
            UIEvent.ADD_EQUIP,
            function()
                RedPointManager.Refresh(RedID.BagEquip)
                RedPointManager.Refresh(RedID.BagFull)
            end
    )
    MsgMg.RegisterCallBack(
            UIEvent.DELETE_EQUIP,
            function()
                RedPointManager.Refresh(RedID.BagEquip)
                RedPointManager.Refresh(RedID.BagFull)
            end
    )
    MsgMg.RegisterCallBack(
            UIEvent.AddBaseLv,
            function()
                RedPointManager.Refresh(RedID.BagEquip)
                RedPointManager.Refresh(RedID.BagItem)
                RedPointManager.Refresh(RedID.RoleInnate)
                RedPointManager.Refresh(RedID.RoleSkill)
                RedPointManager.Refresh(RedID.GuildRaid)

                -- 猫猫虫
                local gridWalkCount = ActivityGridWalkConfig.GetConfigCount()
                for idx = 1, gridWalkCount do
                    local givePetKey = string.format("%s%d", RedID.GridWalkGivePet, idx)
                    RedPointManager.Refresh(givePetKey)
                end
            end
    )

    MsgMg.RegisterCallBack(ViewEvent.Refresh_GridWalk, function()
        -- 猫猫虫
        local gridWalkCount = ActivityGridWalkConfig.GetConfigCount()
        for idx = 1, gridWalkCount do
            local givePetKey = string.format("%s%d", RedID.GridWalkGivePet, idx)
            RedPointManager.Refresh(givePetKey)

            local freeGiftKey = string.format("%s%d", RedID.GridWalkFreeGift, idx)
            RedPointManager.Refresh(freeGiftKey)

            local exchangeKey = string.format("%s%d", RedID.GridWalkExchangeShop, idx)
            RedPointManager.Refresh(exchangeKey)
        end
    end)

    MsgMg.RegisterCallBack(
            UIEvent.REFRESH_TASK,
            function()
                RedPointManager.Refresh(RedID.Task)
                RedPointManager.Refresh(RedID.RoleBadge)
                -- 猫猫虫
                local gridWalkCount = ActivityGridWalkConfig.GetConfigCount()
                for idx = 1, gridWalkCount do
                    local taskKey = string.format("%s%d", RedID.GridWalkTask, idx)
                    RedPointManager.Refresh(taskKey)
                end
            end
    )
    MsgMg.RegisterCallBack(
            UIEvent.REFRESH_TEAM_INVITE,
            function()
                RedPointManager.Refresh(RedID.Team)
            end
    )

    MsgMg.RegisterCallBack(
            UIEvent.REFRESH_GUILD_RED_POINT,
            function()
                RedPointManager.Refresh(RedID.GuildJoin)
                RedPointManager.Refresh(RedID.GuildInvite)
                RedPointManager.Refresh(RedID.GuildApply)
                RedPointManager.Refresh(RedID.GuildActivity_Task)
                RedPointManager.Refresh(RedID.GuildActivity_Achievement)
                RedPointManager.Refresh(RedID.GuildRaid)
                RedPointManager.Refresh(RedID.GuildRaidKey)
            end
    )

    MsgMg.RegisterCallBack(
            ViewEvent.Refresh_BingoRedPoint,
            function()
                RedPointManager.Refresh(RedID.HomeBuildActivities_BingoComboReward)
                RedPointManager.Refresh(RedID.HomeBuildActivities_BingoRecord)
                RedPointManager.Refresh(RedID.HomeBuildActivities_BingoActiveGrid)
                RedPointManager.Refresh(RedID.HomeBuildActivities_BingoCanDraw)
            end
    )

    MsgMg.RegisterCallBack(
            UIEvent.REFRESH_GUILD_SIGN_IN_RED_POINT,
            function()
                RedPointManager.Refresh(RedID.GuildSignIn)

                -- local redPoint = GetRedPoint(RedID.GuildRaid)
                -- if redPoint.redTra ~= nil then
                --     if not redPoint.redTra.gameObject.activeInHierarchy then
                --         UIKit.SetObjectState(redPoint.redTra.gameObject,GuildInstanceManager.IsRedPoint()) --特殊处理
                --     end
                -- end
            end
    )
    MsgMg.RegisterCallBack(
            UIEvent.REFRESH_FASHION_SHOP_TIME_LIMIT_PAGE_RED_DOT,
            function()
                RedPointManager.Refresh(RedID.FashionShopLimitTimePage)
            end
    )
    MsgMg.RegisterCallBack(
            UIEvent.REFRESH_FASHION_SHOP_CONSTANT_RED_DOT,
            function()
                RedPointManager.Refresh(RedID.FashionShop)
            end
    )
    MsgMg.RegisterCallBack(
            UIEvent.REFRESH_FASHION_LOTTERY_RED_DOT,
            function()
                RedPointManager.Refresh(RedID.FashionLottery)
            end
    )
    MsgMg.RegisterCallBack(
            UIEvent.REFRESH_FASHION_LOTTERY_LOG_REWARD_RED_DOT,
            function()
                RedPointManager.Refresh(RedID.FashionLottery_LogReward)
            end
    )
    MsgMg.RegisterCallBack(
            UIEvent.PLAYER_INFO_HEAD_RED_POINT,
            function()
                RedPointManager.Refresh(RedID.PIDHead)
            end
    )
    MsgMg.RegisterCallBack(
            UIEvent.PLAYER_INFO_HEAD_FRAME_RED_POINT,
            function()
                RedPointManager.Refresh(RedID.PIDHeadFrame)
            end
    )
    MsgMg.RegisterCallBack(
            UIEvent.PLAYER_INFO_BACKDROP_RED_POINT,
            function()
                RedPointManager.Refresh(RedID.RoleBackdrop)
            end
    )
    MsgMg.RegisterCallBack(
            UIEvent.PLAYER_INFO_POSE_RED_POINT,
            function()
                RedPointManager.Refresh(RedID.RolePose)
            end
    )
    MsgMg.RegisterCallBack(
            ViewEvent.Refresh_GridWalk,
            function()
                local gridWalkCount = ActivityGridWalkConfig.GetConfigCount()
                for idx = 1, gridWalkCount do
                    local circleRewardKey = string.format("%s%d", RedID.GridWalkCircleReward, idx)
                    RedPointManager.Refresh(circleRewardKey)
                end
            end
    )

    MsgMg.RegisterCallBack(
            UIEvent.REFRESH_PET_ACTIVITY_RED_DOT,
            function()
                RedPointManager.Refresh(RedID.PetTimeLimitedLottery)
                RedPointManager.Refresh(RedID.PetTimeLimitedLotteryShop)
            end
    )

    MsgMg.RegisterCallBack(
            ViewEvent.Refresh_FlipCard,
            function()
                RedPointManager.Refresh(RedID.HomeBuildActivities_FlipCardCanFlip)
                RedPointManager.Refresh(RedID.HomeBuildActivities_FlipCardExchange)
                RedPointManager.Refresh(RedID.HomeBuildActivities_FlipCardFreeGift)
            end
    )

    MsgMg.RegisterCallBack(
            ViewEvent.Refresh_Magnet,
            function()
                RedPointManager.Refresh(RedID.HomeBuildActivities_MagnetNormalDrawLess)
                RedPointManager.Refresh(RedID.HomeBuildActivities_MagnetNormalDrawMore)
                RedPointManager.Refresh(RedID.HomeBuildActivities_MagnetNormalExchange)
                RedPointManager.Refresh(RedID.HomeBuildActivities_MagnetSpecialDrawSingle)
                RedPointManager.Refresh(RedID.HomeBuildActivities_MagnetSpecialExchange)
            end
    )

    MsgMg.RegisterCallBack(
            ViewEvent.GuildWishWallUpdate, function()
                RedPointManager.Refresh(RedID.GuildWishWallReceiveEntry)
                RedPointManager.Refresh(RedID.GuildWishWallReceive)
                RedPointManager.Refresh(RedID.GuildWishWallCanReward)
                RedPointManager.Refresh(RedID.GuildWishWallCanPublish)
                RedPointManager.Refresh(RedID.GuildWishWallDailyPublish)
            end
    )

end
