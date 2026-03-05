local _ENV = Boli2Env

SoundManager = {}
SoundManager.RTPCName = {
    GME_1P = "GME_1P",
    GME_3P = "GME_3P"
}

local isLoadedDefaultBanks = false --是否加载默认banks
local startCallBack
local globalObj
local checkUpdateAudioSource
local bgmEmitterObj --背景音/镜头过场动画、裂隙
local uiEmitterObj --UI点击音效
-- local treeBirdEmitterObj, windmillEmitterObj, waterEmitterObj, waterfallEmitterObj
local clickPar
local stateTable = {}

--环境音
local ambientDic = {}
ambientDic[1] = {eventName = "Play_Weather_Bird", eventTag = "TreeBird", eventObj = nil} --树上鸟叫
ambientDic[2] = {eventName = "Play_Windmill", eventTag = "Windmill", eventObj = nil} --风车
ambientDic[3] = {eventName = "Play_Water", eventTag = "Water", eventObj = nil} --水源
ambientDic[4] = {eventName = "Play_Waterfall", eventTag = "Waterfall", eventObj = nil} --瀑布

--天气变化环境声设置：stateIndex:1~4
local curStateIndex
local weatherStates = {
    "Morning_04_30_AM",
    "Noon_12_PM",
    "Afternoon_16_40_PM",
    "Evening_20_PM"
}

local bankLoadedInfo = {} --bank是否加载信息
local defaultBankDic = {
    "Action", --角色通用bank:采集，脚步这些
    "General_Skill",
    "General_Monster",
    "Cutscene",
    "Map",
    "Music",
    "UI",
    "UI_Activity", --活动音效bank
    "UI_Update_Bank", --音效热更bank
    "Pet", --宠物音效
}
local loginMusStateType = {
    "Mus_Login_Day", --白天
    "Mus_Login_Night", --夜晚
    "Mus_Login_Role", --创建角色界面
    "Mus_Login_Create", --选角/其他
}
local uiNPCVOStateType = {
    "Close_UI",
    "Open_UI"
}
local voBankDic = {
    --不同职业语音 VO bank
    ["71001"] = {bank = "Swordmen_Woman_VO", greetEventName = "Play_Swordmen_Greetings_Vo_Woman", herolState = "Swordmen"},
    ["171001"] = {bank = "Swordmen_Man_VO", greetEventName = "Play_Swordmen_Greetings_Vo_Man", herolState = "Swordmen"},
    ["71002"] = {bank = "Ranger_Woman_VO", greetEventName = "Play_Ranger_Greetings_Vo_Woman", herolState = "Ranger"}, --游侠
    ["171002"] = {bank = "Ranger_Man_VO", greetEventName = "Play_Ranger_Greetings_Vo_Man", herolState = "Ranger"},
    ["71003"] = {bank = "Magician_Woman_VO", greetEventName = "Play_Mage_Greetings_Vo_Woman", herolState = "Mage"}, --魔法师
    ["171003"] = {bank = "Magician_Man_VO", greetEventName = "Play_Mage_Greetings_Vo_Man", herolState = "Mage"},
    ["71004"] = {bank = "Assassin_Woman_VO", greetEventName = "Play_Assassin_Greetings_Vo_Woman", herolState = "Assassin"}, --刺客
    ["171004"] = {bank = "Assassin_Man_VO", greetEventName = "Play_Assassin_Greetings_Vo_Man", herolState = "Assassin"},
    ["71005"] = {bank = "Reverend_Woman_VO", greetEventName = "Play_Reverend_Greetings_Vo_Woman", herolState = "Reverend"}, --牧师
    ["171005"] = {bank = "Reverend_Man_VO", greetEventName = "Play_Reverend_Greetings_Vo_Man", herolState = "Reverend"},
}

local BGMType = {
    Main = 0, -- 主线战斗
    Instance = 1, -- 副本战斗
    GuildInstance = 2, -- 公会战斗
    WorldBoss = 3, -- 世界战斗
    Challenge = 4, -- 挑战战斗
    Guard = 5, -- 守卫战斗
    BigRush = 6, -- 大暴走
    FinalMemory = 7, --追忆战
    Shackle = 8, --桎梏之形

    --全屏幕相关BGM
    HomeBuild = 100, --家园
    Adventure = 101, --秘宝寻宝
    ChallengeMap = 102, -- 挑战本地图
    Closet = 103, --时装商店
    Guild = 104, --公会大厅
    Cat = 105, --猫猫包
    Activity = 110, --各种活动（追忆战UI）
    Farm = 111, ---种田
}

local isPlayBattleBGM
local curRetAudioNameArr --当前地图块背景乐
local curBgmType
local battleType, isBoss, battleBGMEvent
local activityBGMEvent

local MIN_VOLUME = 0
local MAX_VOLUME = 100

local setBusVolumeDic = {}
setBusVolumeDic[BGMType.Main] = {"SFX_Set_Bus_Volume", "Boss_Set_Bus_Volume", "Map_Set_Bus_Volume"}
setBusVolumeDic[BGMType.Instance] = {"SFX_Set_Bus_Volume", "Boss_Set_Bus_Volume", "Map_Dungeon_Set_Bus_Volume"}
setBusVolumeDic[BGMType.GuildInstance] = {"SFX_Set_Bus_Volume", "Boss_Set_Bus_Volume"}
setBusVolumeDic[BGMType.WorldBoss] = {"SFX_Set_Bus_Volume", "Boss_Set_Bus_Volume"}
setBusVolumeDic[BGMType.Challenge] = {"SFX_Set_Bus_Volume", "Boss_Set_Bus_Volume", "Map_Dungeon_Set_Bus_Volume"}
setBusVolumeDic[BGMType.Guard] = {"SFX_Set_Bus_Volume", "Boss_Set_Bus_Volume"}
setBusVolumeDic[BGMType.BigRush] = {"SFX_Set_Bus_Volume", "Boss_Set_Bus_Volume"}
setBusVolumeDic[BGMType.FinalMemory] = {"SFX_Set_Bus_Volume", "Boss_Set_Bus_Volume"}
setBusVolumeDic[BGMType.Shackle] = {"SFX_Set_Bus_Volume", "Boss_Set_Bus_Volume"}
setBusVolumeDic[BGMType.HomeBuild] = {"Campsite_Set_Bus_Volume"}
setBusVolumeDic[BGMType.Adventure] = {"Map_World_Set_Volume"}
setBusVolumeDic[BGMType.ChallengeMap] = nil
setBusVolumeDic[BGMType.Closet] = {"Campsite_Set_Bus_Volume"}
setBusVolumeDic[BGMType.Guild] = nil
setBusVolumeDic[BGMType.Cat] = nil
setBusVolumeDic[BGMType.Activity] = nil

local resetBusVolumeDic = {}
resetBusVolumeDic[BGMType.Main] = {"SFX_Reset_Bus_Volume", "Boss_Reset_Bus_Volume", "Map_Reset_Bus_Volume"}
resetBusVolumeDic[BGMType.Instance] = {"SFX_Reset_Bus_Volume", "Boss_Reset_Bus_Volume", "Map_Dungeon_Reset_Bus_Volume"}
resetBusVolumeDic[BGMType.GuildInstance] = {"SFX_Reset_Bus_Volume", "Boss_Reset_Bus_Volume"}
resetBusVolumeDic[BGMType.WorldBoss] = {"SFX_Reset_Bus_Volume", "Boss_Reset_Bus_Volume"}
resetBusVolumeDic[BGMType.Challenge] = {"SFX_Reset_Bus_Volume", "Boss_Reset_Bus_Volume", "Map_Dungeon_Reset_Bus_Volume"}
resetBusVolumeDic[BGMType.Guard] = {"SFX_Reset_Bus_Volume", "Boss_Reset_Bus_Volume"}
resetBusVolumeDic[BGMType.BigRush] = {"SFX_Reset_Bus_Volume", "Boss_Reset_Bus_Volume"}
resetBusVolumeDic[BGMType.FinalMemory] = {"SFX_Reset_Bus_Volume", "Boss_Reset_Bus_Volume"}
resetBusVolumeDic[BGMType.Shackle] = {"SFX_Reset_Bus_Volume", "Boss_Reset_Bus_Volume"}
resetBusVolumeDic[BGMType.HomeBuild] = {"Campsite_Reset_Bus_Volume"}
resetBusVolumeDic[BGMType.Adventure] = {"Map_World_ReSet_Volume"}
resetBusVolumeDic[BGMType.ChallengeMap] = nil
resetBusVolumeDic[BGMType.Closet] = {"Campsite_Reset_Bus_Volume"}
resetBusVolumeDic[BGMType.Guild] = nil
resetBusVolumeDic[BGMType.Cat] = nil
resetBusVolumeDic[BGMType.Activity] = nil

local function IsOpen()
    return isLoadedDefaultBanks
end

--场景BGM
local function IsSceneBGM(type)
    return type <= 10
end

--设置当前bgm静音状态
local function SetBGMQuiet(isQuiet)
    local res = isQuiet and setBusVolumeDic or resetBusVolumeDic
    local arr = res[curBgmType]
    if arr then
        for _, v in ipairs(arr) do
            SoundManager.Play(v, bgmEmitterObj)
        end
    end
end

--游戏开始，登录主界面BGM
function SoundManager.PlayStartBGM(dontNeedPlayWagon)
    if not GlobalFun.IsNull(checkUpdateAudioSource) then
        GlobalFun.DestroyObj(checkUpdateAudioSource.gameObject, 0)
        checkUpdateAudioSource = nil
    end
    --现在不分昼夜，默认白天
    SoundManager.SetLoginMusState(1)
    SoundManager.Play("Stop_Music", bgmEmitterObj)
    SoundManager.Play("Play_Music_Scene_Start", bgmEmitterObj)
    if not dontNeedPlayWagon then
        SoundManager.Play("Play_Scene_Start_Wagon_Loop", bgmEmitterObj)
    end
    local main = BattleSceneData.GetMainBattlePlayer()
    if main then
        SoundManager.SetMainRoleState(not main:IsAlive())
    end
end

function SoundManager.SetLoginMusState(stateType)
    SoundManager.SetState("Mus_Login_Transition", loginMusStateType[stateType])
end
-- 0恢复 1静音
function SoundManager.SetUINPCVOState(stateType)
    SoundManager.SetState("UI_NPC_VO", uiNPCVOStateType[stateType])
end

function SoundManager.StopLoginInterfaceBgm()
    SoundManager.Play("Stop_Login_Interface", bgmEmitterObj)
end

function SoundManager.StopStartBgm()
    SoundManager.Play("Stop_Music", bgmEmitterObj)
end

function SoundManager.PlaySelectRoleCreateBgm()
    SoundManager.Play("Play_Create_Ambience", bgmEmitterObj)
end

function SoundManager.PlayStartGameCGBGM()
    SoundManager.Play("Play_Music_Scene_Start_Tall", bgmEmitterObj)
end

--创角界面，选择角色
function SoundManager.PlaySelectRoleBGM(jobId)
    local id = math.floor(jobId / 1000)
    local eventName
    if id == 1 then
        eventName = "Play_Create_Swordmen"
    elseif id == 2 then
        eventName = "Play_Create_Ranger"
    elseif id == 3 then
        eventName = "Play_Create_Magician"
    elseif id == 4 then
        eventName = "Play_Create_Assassin"
    elseif id == 5 then
        eventName = "Play_Create_Reverend"
    end
    if eventName ~= nil then
        SoundManager.Play("Login_Set_Voice_Volume", bgmEmitterObj)
        SoundManager.Play(eventName, bgmEmitterObj)
    end
end

local function PlayAmbientEvent()
    --场景移动时，刷新环境声位置（场景移动，声音位置停留在原位置）
    AudioManager.RefreshAmbienEventPositions()
    for _, v in ipairs(ambientDic) do
        AudioManager.PostAmbientEvent(v.eventName, v.eventTag, v.eventObj)
    end
end

local function CheckFinalMemoryEvent(eventName)
    if GlobalFun.IsNilOrEmpty(eventName) then
        return false
    end
    return string.find(eventName, "Play_Music_Battle_FinalMemory_") ~= nil
end

local function CheckSceneBGM(retAudioNameArr)
    if retAudioNameArr and curRetAudioNameArr ~= retAudioNameArr then
        if curRetAudioNameArr then
            if CheckFinalMemoryEvent(curRetAudioNameArr[1]) then
                local eventName = string.gsub(curRetAudioNameArr[1], "Play_", "Stop_")
                print("@@@@@@@@@@@@@@@eventName  ",eventName)
                SoundManager.Play(eventName, bgmEmitterObj)
            else
                for _, v in ipairs(curRetAudioNameArr) do
                    SoundManager.Stop(v, bgmEmitterObj)
                end
            end
        end
        curRetAudioNameArr = retAudioNameArr
        if CheckFinalMemoryEvent(retAudioNameArr[1]) then
            SoundManager.SetFinalMemoryState("Amb")
        else
            for _, v in ipairs(retAudioNameArr) do
                SoundManager.Play(v, bgmEmitterObj)
            end
        end
        PlayAmbientEvent()
    end
end

--场景BGM SCENE_BATTLE_KIND :
-- ModeKindMain = 0, -- 主线战斗
-- ModeKindRaid = 1, -- 副本战斗
-- ModeKindRaidGuid = 2, -- 公会战斗
-- ModeKindWorld = 3, -- 世界战斗
-- ModeKindChallenge = 4, -- 挑战战斗
-- ModeKindGuard = 5,  -- 守卫战斗
-- ModeKindBigRush = 6,  -- 大暴走
-- ModeKindFinalMemory  = 7,  --终末追忆  狼本
-- ModeKindEditor = 98, -- 编辑器战斗
-- ModeKindQuick = 99, -- 快速战斗
function SoundManager.PlaySceneBGM()
    local retAudioNameArr, bgmType = MapConfig.GetBlockGroupAudioName()
    if curBgmType == bgmType then
        CheckSceneBGM(retAudioNameArr)
    else
        if curBgmType == BGMType.Instance or curBgmType == BGMType.Challenge then
            SoundManager.Stop("Play_Music_Battle_Boss_General", bgmEmitterObj)
        end
        if bgmType == BGMType.Instance or bgmType == BGMType.Challenge then
            SoundManager.SetInstanceOrChallengeState(bgmType, "Amb", true)
        elseif bgmType == BGMType.BigRush then
            SoundManager.SetBigRushState("Amb", true)
        end
        SoundManager.PlayBGM(bgmType, retAudioNameArr)
    end
end

--营地音效
function SoundManager.PlayBGMHomeBuild()
    SoundManager.PlayBGM(BGMType.HomeBuild)
end

-- 营地BGM状态切换(周年庆活动等)
-- Mus_Campsite
-- State：Campsite【默认营地音乐
-- State：SemiAnnual【半周年庆音乐
function SoundManager.SetHomeBuildState()
    local state = ActivitiesData.IsActivityOpen("client_operHub_1") and "SemiAnnual" or "Campsite"
    SoundManager.SetState("Mus_Campsite", state)
end

-- function SoundManager.BusyBGMHomeBuild()
--     SoundManager.Play("Campsite_Set_Bus_Volume", bgmEmitterObj)
-- end

function SoundManager.StopBGMCopy()
    SoundManager.Play("Stop_Music_Dungeon", bgmEmitterObj)
end

--挑战本地图BGM
function SoundManager.PlayBGMChallengeMap()
    SoundManager.PlayBGM(BGMType.ChallengeMap)
end

function SoundManager.StopBGMChallengeMap()
    SoundManager.Stop("Play_Music_Scene_Challenge", bgmEmitterObj)
    SoundManager.PlayBGM(BGMType.Main)
end

--挑战本场景BGM
function SoundManager.StopBGMChallenge()
    SoundManager.StopBGMCopy() --挑战本复用副本BGM
end

--旅团大厅音效
function SoundManager.PlayBGMGuild()
    SoundManager.PlayBGM(BGMType.Guild)
end

function SoundManager.StopBGMGuild()
    SoundManager.Stop("Play_Music_Scene_Guild", bgmEmitterObj)
    SoundManager.Stop("Play_Weather_Guild", bgmEmitterObj)
    SoundManager.PlayBGM(BGMType.Main)
end

---播放种田音效
function SoundManager.PlayBGMFarm()
    SoundManager.PlayBGM(BGMType.Farm)
end

function SoundManager.StopBGMFarm()
    --TODO 关闭这个需要开启别的吗
    SoundManager.Play("Stop_Music_Activity_Farming")
    if MainInterfaceData.IsInHomeBuildTab() then
        SoundManager.PlayBGMHomeBuild()
    else
        SoundManager.PlayBGM(BGMType.Main)
    end
end

function SoundManager.PlayBGMCloset()
    SoundManager.PlayBGM(BGMType.Closet)
    if InstanceManager.InBattle() then
        SoundManager.Play("BossWorld_Set_Bus_Volume", bgmEmitterObj)
        SoundManager.Play("Battle_Dungeon_Set_Bus_Volume", bgmEmitterObj)
    end
end

function SoundManager.StopBGMCloset()
    if InstanceManager.InBattle() then
        SoundManager.PlayBGM(InstanceManager.TeamType())
        SoundManager.Play("BossWorld_Reset_Bus_Volume", bgmEmitterObj)
        SoundManager.Play("Battle_Dungeon_Reset_Bus_Volume", bgmEmitterObj)
    else
        SoundManager.PlayBGM(BGMType.Main)
    end
    if isPlayBattleBGM then
        if curBgmType == BGMType.Main then
            SoundManager.Play("Map_Set_Bus_Volume", bgmEmitterObj)
        elseif curBgmType == BGMType.Instance then
            SoundManager.Play("Map_Dungeon_Set_Bus_Volume", bgmEmitterObj)
        end
    end
end

function SoundManager.PlayBGMCat()
    SoundManager.PlayBGM(BGMType.Cat)
    if InstanceManager.InBattle() then
        SoundManager.Play("BossWorld_Set_Bus_Volume", bgmEmitterObj)
        SoundManager.Play("Battle_Dungeon_Set_Bus_Volume", bgmEmitterObj)
    end
end

function SoundManager.StopBGMCat()
    if InstanceManager.InBattle() then
        SoundManager.PlayBGM(InstanceManager.TeamType())
        SoundManager.Play("BossWorld_Reset_Bus_Volume", bgmEmitterObj)
        SoundManager.Play("Battle_Dungeon_Reset_Bus_Volume", bgmEmitterObj)
    else
        SoundManager.PlayBGM(BGMType.Main)
    end
    if isPlayBattleBGM then
        if curBgmType == BGMType.Main then
            SoundManager.Play("Map_Set_Bus_Volume", bgmEmitterObj)
        elseif curBgmType == BGMType.Instance then
            SoundManager.Play("Map_Dungeon_Set_Bus_Volume", bgmEmitterObj)
        end
    end
    SoundManager.Stop("Play_Music_Scene_Catbread", bgmEmitterObj)
end

--各种活动音效跳转（春节?）
--苍龙引时装/大玩家时装/龙踞岛//bingo活动/紧急委托
function SoundManager.PlayActivityBGM(bgmEventName)
    SoundManager.PlayBGM(BGMType.Activity)
    if bgmEventName then
        activityBGMEvent = bgmEventName
        SoundManager.Play(activityBGMEvent, bgmEmitterObj)
    end
end

function SoundManager.StopActivityBGM(stopEventName)
    if stopEventName then
        SoundManager.Play(stopEventName, bgmEmitterObj)
    elseif activityBGMEvent then
        SoundManager.Stop(activityBGMEvent, bgmEmitterObj)
        activityBGMEvent = nil
    end
    --切换状态
    if MainInterfaceData.IsInHomeBuildTab() then
        SoundManager.PlayBGM(BGMType.HomeBuild)
    else
        SoundManager.PlayBGM(BGMType.Main)
        if isPlayBattleBGM then
            if curBgmType == BGMType.Main then
                SoundManager.Play("Map_Set_Bus_Volume", bgmEmitterObj)
            end
        end
    end
end

--冒险bgm
function SoundManager.PlayBGMMap()
    SoundManager.PlayBGM(BGMType.Main)
end

function SoundManager.PlayBGMBattleEnd(isWin)
    if battleType == 1 or battleType == 4 then
        SoundManager.SetInstanceOrChallengeState(battleType, "Amb")
    elseif battleType == 6 then
        SoundManager.SetBigRushState("Amb")
    elseif battleType == 7 then
        SoundManager.SetFinalMemoryState("Amb")
    else
        SoundManager.Stop(battleBGMEvent, bgmEmitterObj)
        if curBgmType == BGMType.Main then
            SoundManager.Play("Map_Reset_Bus_Volume", bgmEmitterObj)
        elseif curBgmType == BGMType.Instance then
            SoundManager.Play("Map_Dungeon_Reset_Bus_Volume", bgmEmitterObj)
        end
    end
    isPlayBattleBGM = false
    battleType, isBoss = nil, nil
    battleBGMEvent = nil
end

--战斗类型0:主线 1:秘境 2:调查团 4:挑战本
function SoundManager.PlayBGMBattleStart(_battleType, _isBoss)
    isPlayBattleBGM = true
    battleType, isBoss = _battleType, _isBoss
    if battleType == 0 then
        battleBGMEvent = "Play_Music_Boss_Common_01"
        SoundManager.Play("Map_Set_Bus_Volume", bgmEmitterObj)
        SoundManager.Play(battleBGMEvent, bgmEmitterObj)
    elseif battleType == 1 or battleType == 4 then
        if battleType == 1 then
            SoundManager.Play("Map_Dungeon_Set_Bus_Volume", bgmEmitterObj)
        end
        SoundManager.SetInstanceOrChallengeState(battleType, isBoss and "Stage1" or "Elite")
        -- local instanceId = BattleMapComponent.DataManager.GetInstanceId()
        -- if isBoss then
        --     battleBGMEvent = "Play_Music_Boss_Common_01"
        --     if battleType == 1 then
        --         if instanceId == 900016 then
        --             --九王斗兽场
        --             battleBGMEvent = "Play_Music_Battle_Dungeon_HC"
        --         elseif instanceId == 900019 then
        --             --躁动绿洲之丘
        --             battleBGMEvent = "Play_Music_Boss_FragonLizard"
        --         elseif instanceId == 900020 then
        --             --白沙渊下的鼓动（绿洲）
        --             battleBGMEvent = "Play_Music_Battle_RuhrGreenbelt"
        --         elseif instanceId == 900024 then
        --             --无始无终燃烧塔
        --             battleBGMEvent = "Play_Music_Battle_DungeonFF1"
        --         elseif instanceId >= 900025 and instanceId <= 900029 then
        --             --新大陆秘境
        --             battleBGMEvent = "Play_Music_Boss_Common_02_1"
        --         end
        --     elseif battleType == 4 then
        --         if instanceId == 920010 then
        --             --920010沙海灾殃追击战
        --             battleBGMEvent = "Play_Music_Battle_RuhrGreenbelt"
        --         elseif instanceId >= 920013 and instanceId <= 920015 then
        --             --新大陆绝境
        --             battleBGMEvent = "Play_Music_Boss_Common_02_1"
        --         end
        --     end
        -- else
        --     if instanceId == 900024 then
        --         --无始无终燃烧塔
        --         battleBGMEvent = "Play_Music_Battle_DungeonFFDungeon"
        --     else
        --         battleBGMEvent = "Play_Music_Battle_Dungeon"
        --     end
        -- end
    elseif battleType == 2 then
        battleBGMEvent = "Play_Music_Battle_Guild_Boss"
        SoundManager.Play(battleBGMEvent, bgmEmitterObj)
    elseif battleType == 6 then
        SoundManager.SetBigRushState(isBoss and "Stage1" or "Elite")
    elseif battleType == 7 then
        SoundManager.SetFinalMemoryState(isBoss and "Stage1" or "Elite")
    end
    -- SoundManager.Play(battleBGMEvent, bgmEmitterObj)
    -- if curBgmType == BGMType.Main then
    --     SoundManager.Play("Map_Set_Bus_Volume", bgmEmitterObj)
    -- elseif curBgmType == BGMType.Instance then
    --     SoundManager.Play("Map_Dungeon_Set_Bus_Volume", bgmEmitterObj)
    -- end
end

function SoundManager.PlayMusicBattle_Stage2()
    if battleType == 1 or battleType == 4 then
        SoundManager.SetInstanceOrChallengeState(battleType, "Stage2")
    elseif battleType == 7 then
        SoundManager.SetFinalMemoryState("Stage2")
    else
        SoundManager.Stop(battleBGMEvent, bgmEmitterObj)
    end
    -- SoundManager.Stop(battleBGMEvent, bgmEmitterObj)
    -- local instanceId = BattleMapComponent.DataManager.GetInstanceId()
    -- --无始无终燃烧塔:第二阶段:特殊处理
    -- if instanceId == 900024 then
    --     return
    -- end
    -- if battleType == 1 then
    --     battleBGMEvent = "Play_Music_Boss_Common_02"
    --     if instanceId >= 900025 and instanceId <= 900029 then
    --         --新大陆秘境
    --         battleBGMEvent = "Play_Music_Boss_Common_02_2"
    --     end
    --     SoundManager.Play(battleBGMEvent, bgmEmitterObj)
    -- elseif battleType == 4 and instanceId >= 920013 and instanceId <= 920015 then
    --     --新大陆绝境
    --     battleBGMEvent = "Play_Music_Boss_Common_02_2"
    --     SoundManager.Play(battleBGMEvent, bgmEmitterObj)
    -- end
end

--无始无终燃烧塔:第二阶段
function SoundManager.PlayMusicBattle_DungeonFF2()
    if battleType == 1 or battleType == 4 then
        SoundManager.SetInstanceOrChallengeState(battleType, "Stage2")
    elseif battleType == 7 then
        SoundManager.SetFinalMemoryState("Stage2")
    else
        SoundManager.Stop(battleBGMEvent, bgmEmitterObj)
    end
    -- SoundManager.Stop(battleBGMEvent, bgmEmitterObj)
    -- if battleType == 1 then
    --     battleBGMEvent = "Play_Music_Battle_DungeonFF2"
    --     SoundManager.Play(battleBGMEvent, bgmEmitterObj)
    -- end
end

--秘宝寻宝
function SoundManager.PlayBGMAdventure()
    SoundManager.PlayBGM(BGMType.Adventure)
end

local function CheckAndPlay(bgmEventName)
    if not AudioManager.IsEventPlayingOnGameObject(bgmEventName, bgmEmitterObj) then
        SoundManager.Play(bgmEventName, bgmEmitterObj)
    end
end

function SoundManager.CheckAndPlayBGM(bgmEventName)
    if bgmEventName == nil then
        return
    end
    CheckAndPlay(bgmEventName)
end

function SoundManager.PlayBGM(bgmType, ...)
    if curBgmType == bgmType then
        return
    end
    SetBGMQuiet(true)
    curBgmType = bgmType
    SetBGMQuiet(false)
    if IsSceneBGM(curBgmType) then
        CheckSceneBGM(...)
    elseif curBgmType == BGMType.HomeBuild then
        SoundManager.SetHomeBuildState()
        CheckAndPlay("Play_Music_Scene_Campsite")
    elseif curBgmType == BGMType.Adventure then
        CheckAndPlay("Play_Music_Map_World")
    elseif curBgmType == BGMType.ChallengeMap then
        SoundManager.Play("Play_Music_Scene_Challenge", bgmEmitterObj)
    elseif curBgmType == BGMType.Closet then
        SoundManager.SetHomeBuildState()
        CheckAndPlay("Play_Music_Scene_Campsite")
    elseif curBgmType == BGMType.Guild then
        SoundManager.Play("Play_Music_Scene_Guild", bgmEmitterObj)
        SoundManager.Play("Play_Weather_Guild", bgmEmitterObj)
    elseif curBgmType == BGMType.Cat then
        SoundManager.Play("Play_Music_Scene_Catbread", bgmEmitterObj)
    elseif curBgmType == BGMType.Farm then
        SoundManager.Play("Play_Music_Activity_Farming", bgmEmitterObj)
    end
end

-- Mus_NightmareCraze
-- Common_Loop_01
-- Common_Loop_02
-- Common_Loop_03
-- Common_Loop_04
-- Common_END
--1.进入这个场景,这时候玩家处于待机状态 Common_Loop_01
--2.等待倒计时结束或者手动点击开始战斗切换为：Common_Loop_02
--3.战斗到第15波时，切换state为：Common_Loop_03
--4.战斗到第20波时, 切换state为：Common_Loop_04
--5.战斗结束，切换state为：Common_END
GuardBGM = {
    Common = {
        Name = "Mus_NightmareCraze",
        Enter = "Common_Loop_01",
        State_1 = "Common_Loop_02",
        State_2 = "Common_Loop_03",
        State_3 = "Common_Loop_04",
        End = "Common_END",
        Stop = "Stop_Music_Battle_NightmareCraze_Common",
    },
    Loop = {
        Name = "Mus_NightmareCraze", --Play_Music_Battle_NightmareCraze_Common
        Enter = "Common_Loop_01",
        State_1 = "Common_Loop_02",
        State_2 = "Common_Loop_03",
        State_3 = "Common_Loop_04",
        State_4 = "Common_Loop_05",
        State_5 = "Common_Loop_06",
        End = "Common_END",
        Stop = "Stop_Music_Battle_NightmareCraze_Common",
    },
}
function SoundManager.SetGuardState(typeTable, state)
    -- print("SetGuardState", typeTable['Name'] , typeTable[state])
    SoundManager.SetState(typeTable['Name'], typeTable[state]) -- Play_Music_Battle_NightmareCraze_Common
end

--大暴走
-- 进入场景post：Play_Music_Battle_Slime，State切换为：Amb
-- 进入战斗切换为：Stage1
-- Mus_Boss_General
-- Amb
-- Elite
-- Stage1
-- Stage2
function SoundManager.SetBigRushState(state, isEnter)
    SoundManager.SetState("Mus_Boss_General", state)
    if isEnter then
        return
    end
    if curRetAudioNameArr == nil then
        --登陆时候，如果在副本，可能BGM为空
        curRetAudioNameArr = MapConfig.GetBlockGroupAudioName()
    end
    local eventName = curRetAudioNameArr[1]
    if eventName then
        SoundManager.Play(eventName, bgmEmitterObj)
    end
end

--追忆战
-- StateGroup：Mus_FinalMemory
-- State：Stage1（boss战一阶段【Play_Music_Battle_DungeonFF1
-- State：Stage2（boss战二阶段【Play_Music_Battle_DungeonFF2
-- State：Elite（精英战斗【Play_Music_Battle_DungeonFFDungeon
-- State：Amb（待机音乐【Play_Music_Battle_DungeonFFAmb
function SoundManager.SetFinalMemoryState(state)
    SoundManager.SetState("Mus_FinalMemory", state)
    if curRetAudioNameArr == nil then
        --登陆时候，如果在副本，可能BGM为空
        curRetAudioNameArr = MapConfig.GetBlockGroupAudioName()
    end
    local eventName = curRetAudioNameArr[1]
    if eventName then
        SoundManager.Play(eventName, bgmEmitterObj)
    end
end

SoundManager.PlayingID = nil

-- #####秘境和挑战副本#####
-- 通用boss战音乐事件：Play_Music_Battle_Boss_General
-- 【大陆判断】
-- State Group：Mus_Boss_Mainland
-- State：
-- General【通用
-- Chorny【第二大陆：乔尼尔
-- Gungnir【第一大陆：冈尼尔
-- Mistilteinn【第三大陆：米斯特汀
-- 战斗判定
-- State Group：Mus_Boss_General
-- State：
-- Amb【副本】
-- Elite【副本】
-- Stage1【主线&副本一阶段】
-- Stage2【副本二阶段】
local MainlandState = {"Gungnir", "Chorny", "Mistilteinn", "General"}
function SoundManager.SetInstanceOrChallengeState(battleType, bossState, isEnter)
    local id = BattleMapComponent.DataManager.GetInstanceId()
    local config
    if battleType == 1 then
        config = InstanceConfig.GetInstanceCfg(id)
    elseif battleType == 4 then
        config = ChallengeConfig.GetMapConfig(id)
    end
    if config then
        SoundManager.SetState("Mus_Boss_Mainland", MainlandState[(config and config.Area_Land) or 4])
    end
    SoundManager.SetState("Mus_Boss_General", bossState)
    if isEnter then
        if id == 900024 then
            SoundManager.Play("Play_Music_Battle_FinalMemory_02", bgmEmitterObj) --芬里尔秘境
        elseif id == 900031 then
            SoundManager.Play("Play_Music_Battle_FinalMemory_03", bgmEmitterObj)
        elseif id == 900037 then
            SoundManager.Play("Play_Music_Battle_FinalMemory_07", bgmEmitterObj)
        elseif id == 900039 then
            SoundManager.Play("Play_Music_Battle_FinalMemory_08", bgmEmitterObj)
        elseif id == 990044 then
            --默认
            SoundManager.PlayingID = SoundManager.Play("Play_Music_Evasive_Note", bgmEmitterObj)
            SoundManager.SetBgmSwitch("Mus_Evasive_Note_Track","Amb")
            SoundManager.SetBgmSwitch("Mus_Evasive_Note","PitchNormal")
            SoundManager.SetBgmSwitch("Mus_Evasive_Note_Track_Layer","Evasive_Note_Layer_01")
        else
            SoundManager.Play("Play_Music_Battle_Boss_General", bgmEmitterObj)
        end
    end
end

--播放采集音效 --type
function SoundManager.PlayMusicCollect(type, obj)
    --稀有道具
    if type == 5 then
        SoundManager.Play("Play_Collect_Rare", bgmEmitterObj)
    elseif type == 6 then
        SoundManager.Play("Stop_Collect_Plant", obj)
    end
end

--默认状态为Gamer_1P
--在1p玩家死亡时切换成Teammate_3P
--在1p玩家复活后切换回Gamer_1P
function SoundManager.SetMainRoleState(isDie)
    SoundManager.SetState("RoleType", isDie and "Teammate_3P" or "Gamer_1P")
end

function SoundManager.SetPlayerState(eventName, player, targetObj)
    if targetObj == nil then
        targetObj = player:GetAnimObj()
    end
    local switchState = "Teammate"
    if player:IsMainRole() or (player:IsPet() and player == BattleSceneData.GetMainRolePet()) or (player:IsMount() and BattleSceneData.IsBattleMainRoleMount(player)) then
        switchState = "Gamer"
    end
    SoundManager.SetSwitch("RoleType_Switch", switchState, targetObj)
    if player:IsRole() then
        local index = string.find(eventName, "_Vo")
        if index then
            local voID = player:GetSoundId()
            if voID ~= nil then
                local state = voBankDic[tostring(voID)].herolState
                SoundManager.SetSwitch("Herol_Switch", state, targetObj)
            end
        end
        SoundManager.SetSwitch("GenderType_Switch", player:GetSex() == 0 and "Woman" or "Man", targetObj)
    end
end

--怪物体型判定（同模型，不同怪物）
local function MonsterSizeSwitch(sourcePlayer)
    if sourcePlayer and sourcePlayer:IsMonster() then
        local switchState = sourcePlayer:IsTypeEliteOrBoss() and "Boss" or "Monster"
        SoundManager.SetSwitch("Monster_Size_Switch", switchState, sourcePlayer:GetAnimObj())
    end
end

function SoundManager.PlayBattleSound(eventName, targetPlayer, sourcePlayer)
    if targetPlayer == nil or targetPlayer:IsAnimNil() then
        return
    end
    local targetObj = targetPlayer:GetAnimObj()
    SoundManager.SetPlayerState(eventName, sourcePlayer or targetPlayer, targetObj)
    MonsterSizeSwitch(sourcePlayer)
    SoundManager.Play(eventName, targetObj)
end

--播放镜头过场动画音效
function SoundManager.PlayShotSound(eventName)
    SoundManager.Play(eventName, bgmEmitterObj)
end

--播放裂隙音效
--[[
裂隙生成 Play_Cracks_Generated
裂隙循环 Play_Cracks_Loop
裂隙怪物弹射轨迹 Play_Ejection_Trajectory
裂隙怪物落地 Play_Ejection_Monster_Landing
]]
function SoundManager.PlayCrackSound(eventName)
    SoundManager.Play(eventName, bgmEmitterObj)
end

function SoundManager.StopCrackSound(eventName)
    SoundManager.Stop(eventName, bgmEmitterObj)
end

function SoundManager.PlayGreetingsVO(soundId)
    local info = voBankDic[tostring(soundId)]
    if info and info.greetEventName then
        SoundManager.Play(info.greetEventName, uiEmitterObj)
    end
end

--播放UI音效
function SoundManager.PlayUISound(eventName)
    SoundManager.Play(eventName, uiEmitterObj)
end

--设备外放/耳机状态设置
-- State_Group：Mix_Platform
-- State：ON_Android【手机外放（高频损失）】
-- State：ON_iOS【手机外放（高频损失）】
-- State：ON_Headset【耳机&PC&音箱外放（高质量）】
function SoundManager.SetDeviceState(isDeviceItself)
    local state
    if isDeviceItself then
        state = isPlatformAndroid and "ON_Android" or "ON_iOS"
    else
        state = "ON_Headset"
    end
    SoundManager.SetState("Mix_Platform", state)
end

local function SetWeatherState(stateIndex)
    local state = weatherStates[stateIndex]
    SoundManager.SetState("Weather", state)
    SoundManager.SetRTPC("Map_Weather", BattleMapTime.GetTime())
    --树上鸟叫声音修改
    local treeAmbient = ambientDic[1]
    SoundManager.SetSwitch("Weather_Switch", state, treeAmbient.eventObj)
    if curStateIndex == nil or stateIndex == 1 or stateIndex == 3 then
        AudioManager.PostAmbientEvent(treeAmbient.eventName, treeAmbient.eventTag, treeAmbient.eventObj)
    end
    curStateIndex = stateIndex
end

--昼夜天气状态
local function GetWeatherState(time)
    if time > 4.5 and time <= 12 then
        return 1
    elseif time > 12 and time <= 16.8 then
        return 2
    elseif time > 16.8 and time <= 20 then
        return 3
    else
        return 4
    end
end

local function CheckWeatherState()
    local azureCtrl = BattleMapTime.GetAzureCtrl()
    if azureCtrl then
        local time = azureCtrl.timeOfDay
        local state = GetWeatherState(time)
        if curStateIndex ~= state then
            SetWeatherState(state)
        end
    end
end

local function GetEmitterObj(name)
    local obj = GlobalFun.GetObj(globalObj.transform, name)
    local akObj = GlobalFun.GetType(obj, nil, "AkGameObj")
    if akObj == nil then
        obj:AddComponent("AkGameObj")
    end
    return obj
end

local function InitAmbientObjs()
    for _, v in ipairs(ambientDic) do
        v.eventObj = GetEmitterObj(v.eventTag)
    end
end

local function InitEmitter()
    globalObj = GameObject.FindWithTag("AudioManager")
    local audioObj = GameObject.Find("Audio_CheckUpdate")
    checkUpdateAudioSource = audioObj and audioObj:GetComponent("AudioSource")
    bgmEmitterObj = GetEmitterObj("BGM")
    uiEmitterObj = GetEmitterObj("OTHER")
    InitAmbientObjs()
    clickPar = GlobalFun.GetPar(globalObj.transform, "FX_dianji")
    if clickPar then
        GlobalFun.CheckMat(clickPar.transform)
    end
end

--todo:加载默认soundbank,是否要添加bank和event对应关系表？
local function LoadDefaultBanks()
    for _, v in pairs(defaultBankDic) do
        SoundManager.LoadBank(v)
    end
end

local function Init()
    InitEmitter()
    LoadDefaultBanks()
end

function SoundManager.StartAction(callBack)
    startCallBack = callBack
    Init()
end

local function PlayClickEffect()
    if clickPar ~= nil then
        local pos = GlobalFun.GetWorldPos(Input.mousePosition)
        TransformExtension.SetPosition(clickPar.transform, pos.x, pos.y, -0.5)
        clickPar:Play()
    end
end

local function ClickEvent()
    SoundClick.Play()
    PlayClickEffect()
end

local function CheckDefaultBanks()
    for _, v in pairs(defaultBankDic) do
        if not SoundManager.IsBankLoaded(v) then
            return false
        end
    end
    return true
end

--解决：在冒险地图里打boss中突然不想打了跳关卡。切地图后boss战音乐还在播
local function CheckAdventureBGM()
    if battleType == 0 and isPlayBattleBGM then
        SoundManager.PlayBGMBattleEnd()
    end
end

local function Start()
    isLoadedDefaultBanks = true
    if startCallBack then
        startCallBack()
    end
    if not GameMain:GetSuspend() then
        SoundManager.PlayStartBGM()
    end
    MsgMg.RegisterCallBack(UIEvent.MapArrive, CheckAdventureBGM)
end

function SoundManager.UpdateAction()
    if isLoadedDefaultBanks then
        if Input.GetMouseButtonDown(0) then
            ClickEvent()
        end
        CheckWeatherState()
    else
        if CheckDefaultBanks() then
            Start()
        end
    end
end

--############## AudioManager API:Start##############
function SoundManager.LoadBank(bankName, callBack)
    local loadBankCallBack = function(in_bankID, in_pInMemoryBankPtr, in_eLoadResult, in_Cookie)
        bankLoadedInfo[bankName] = true
        if callBack ~= nil then
            callBack()
        end
    end
    print("<color=#008888>Load:" .. bankName .. "</color>")
    bankLoadedInfo[bankName] = false
    AudioManager.LoadBank(bankName, loadBankCallBack)
end

function SoundManager.UnloadBank(bankName)
    print("<color=#006688>Unload:" .. bankName .. "</color>")
    bankLoadedInfo[bankName] = nil
    AudioManager.UnloadBank(bankName)
end

function SoundManager.Play(eventName, gameObj, callBack)
    if IsOpen() then
        local playingID = AudioManager.PostEvent(eventName, gameObj, callBack)
        return playingID
    end
end

function SoundManager.Stop(eventName, gameObj, transitionDuration)
    if IsOpen() and GlobalFun.NotNilOrEmpty(eventName) then
        if transitionDuration == nil then
            transitionDuration = 0
        end
        AudioManager.StopEvent(eventName, gameObj, transitionDuration)
    end
end

function SoundManager.StopByObj(gameObj)
    if IsOpen() then
        AudioManager.StopAll(gameObj)
    end
end

function SoundManager.SetSwitch(switchGroup, switchState, gameObj)
    if IsOpen() and switchGroup ~= nil and switchState ~= nil then
        AudioManager.SetSwitch(switchGroup, switchState, gameObj)
    end
end

function SoundManager.SetRTPC(name, value)
    if IsOpen() then
        AudioManager.SetRTPC(name, value)
    end
end

function SoundManager.SetState(stateGroup, state)
    if IsOpen() then
        if state then
            if stateTable[stateGroup] == nil or stateTable[stateGroup] ~= state then
                stateTable[stateGroup] = state
                AudioManager.SetState(stateGroup, state)
            end
        else
            print("state is null!!! please check group:", stateGroup)
        end
    end
end

--UI界面/战斗界面切换(半屏幕)
function SoundManager.SetUIState(isOpenUI)
    if IsOpen() then
        local stateGroup = "InterfaceType"
        local state = isOpenUI and "Open_UI" or "Close_UI"
        SoundManager.SetState(stateGroup, state)
    end
end

--全屏UI界面/战斗界面切换(新手试炼、冒险手册、秘宝界面)(全屏幕)
function SoundManager.SetFullUIState(isOpenUI)
    if IsOpen() then
        local stateGroup = "InterfaceType_Level2"
        local state = isOpenUI and "Open_UI" or "Close_UI"
        SoundManager.SetState(stateGroup, state)
    end
end

-- 进入宠物 展示
function SoundManager.SetSkillUIState(isOpenUI)
    if IsOpen() then
        local stateGroup = "UI_Pet_Skill"
        local state = isOpenUI and "Open_UI" or "Close_UI"
        SoundManager.SetState(stateGroup, state)
    end
end

--############## AudioManager API:End##############
local function SetBusVolume(name, value)
    if IsOpen() then
        value = GlobalFun.Clamp(value, MIN_VOLUME, MAX_VOLUME)
        SoundManager.SetRTPC(name, value)
    end
end

--设置背景音乐音量
function SoundManager.SetBgmVolume(value)
    SetBusVolume("Music", value)
end

--设置环境音效音量
function SoundManager.SetAmbienceVolume(value)
    SetBusVolume("Ambience", value)
end

--设置其他音效音量
function SoundManager.SetSFXVolume(value)
    SetBusVolume("SFX", value)
end

--设置UI音效音量
function SoundManager.SetUIVolume(value)
    SetBusVolume("UI", value)
end

--设置音效音量
function SoundManager.SetAudioVolume(value)
    SoundManager.SetAmbienceVolume(value)
    SoundManager.SetSFXVolume(value)
    SoundManager.SetUIVolume(value)
end

function SoundManager.UnloadAll()
    for k, v in pairs(bankLoadedInfo) do
        SoundManager.UnloadBank(k)
    end
    AudioManager.Term()
    GameObject.DestroyImmediate(globalObj)
end

function SoundManager.IsBankLoaded(bankName)
    if GlobalFun.IsNilOrEmpty(bankName) then
        return false
    end
    return bankLoadedInfo[bankName]
end

--bank是否使用中，针对角色bank
local function IsBankUsingByTeam(assetId, bankName)
    if BattleConfig.HasRole(assetId) then
        local players = BattleCameraTargetGroup.GetTeams()
        for _, v in ipairs(players) do
            if BattleConfig.GetModelBankName(v.assetId) == bankName then
                return true
            end
        end
    end
    return false
end

--角色语音bank卸载
local function UnloadVOBankByAssetId(assetId, bankName)
    if BattleConfig.HasRole(assetId) then
        local voBankName = string.format("%s%s", bankName, assetId % 10 == 0 and "_Woman_VO" or "_Man_VO")
        SoundManager.UnloadBank(voBankName)
    end
end

function SoundManager.UnloadBankByAssetId(assetId)
    local bankName = BattleConfig.GetModelBankName(assetId)
    if SoundManager.IsBankLoaded(bankName) and not IsBankUsingByTeam(assetId, bankName) then
        SoundManager.UnloadBank(bankName)
        UnloadVOBankByAssetId(assetId, bankName)
    end
end

--角色语音加载
function SoundManager.LoadVOBank(voBankID)
    if voBankID ~= nil then
        local info = voBankDic[tostring(voBankID)]
        if info ~= nil then
            local bankName = info.bank
            if not SoundManager.IsBankLoaded(bankName) then
                SoundManager.LoadBank(bankName)
            end
        end
    end
end

function SoundManager.PlayAdventureStandBy()
    SoundManager.Play("Play_Music_Relic_Standby", bgmEmitterObj)
end

function SoundManager.StopAdventureStandBy()
    SoundManager.Play("Stop_Music_Relic_Standby", bgmEmitterObj)
end

function SoundManager.PlayAdventureSlotRun()
    SoundManager.Play("Play_Music_Relic_SlotMachine", bgmEmitterObj)
end

function SoundManager.StopAdventureSlotRun()
    SoundManager.Play("Stop_Music_Relic_SlotMachine", bgmEmitterObj)
end

function SoundManager.StopAdventureMap()
    SoundManager.Play("Stop_Music_Map_World", bgmEmitterObj)
end

--音游使用 设置BGM的Switch
function SoundManager.SetBgmSwitch(switchGroup,switchState)
    if GlobalFun.IsNilOrEmpty(switchGroup) or GlobalFun.IsNilOrEmpty(switchState)  then
        return 
    end
    print("@@@@@@@@@SetBgmSwitch@@@@@@@@",switchGroup,switchState)
    SoundManager.SetSwitch(switchGroup,switchState ,bgmEmitterObj)
end

function SoundManager.PlayBgmSpeed(eventName)
    if GlobalFun.IsNilOrEmpty(eventName)  then
        return 
    end
    print("@@@@@@@@@PlayBgmSpeed@@@@@@@@",eventName)
    SoundManager.Play(eventName, bgmEmitterObj)
end

function SoundManager.SeekOnEvent(eventId, playingID,eventName,startTime)
    if GlobalFun.IsNilOrEmpty(eventName)  then
        return 
    end
    AudioManager.SeekOnEvent(eventId, playingID ,eventName,startTime ,bgmEmitterObj)
end