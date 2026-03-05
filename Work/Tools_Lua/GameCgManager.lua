local _ENV = Boli2Env
---@class  GameCgManager
GameCgManager = {}
--region Private Parameters

local class = GameCgManager
local _isStopping = false
local needToCheckCameraEnable

--endregion

--region Public Parameters

class.CG_Data = {
    LOGIN_GAME = { name = "LOGIN_GAME", needMask = true, alphaTime = 0.4 },
    CHANGE_MAP_CG = {
        { name = "video/changemapcg",
          assetName = "ChangeMapCg.m4v",
          startWwiseEventNameCN = { "Map_Set_Bus_Volume", "SFX_Set_Bus_Volume", "Play_Cutscene_Muffin_CG_CN" },
          stopWwiseEventNameCN = { "Map_Reset_Bus_Volume", "SFX_Reset_Bus_Volume", "Stop_Cutscene_Muffin_CG" },
          stopWwiseEventNameTW = { "Map_Reset_Bus_Volume", "SFX_Reset_Bus_Volume", "Stop_Cutscene_Muffin_CG" },
          startWwiseEventNameTW = { "Map_Set_Bus_Volume", "SFX_Set_Bus_Volume", "Play_Cutscene_Muffin_CG_TW" },
          stopWwiseEventNameKR = { "Map_Reset_Bus_Volume", "SFX_Reset_Bus_Volume", "Stop_Cutscene_Muffin_CG" },
          startWwiseEventNameKR = { "Map_Set_Bus_Volume", "SFX_Set_Bus_Volume", "Play_Cutscene_Muffin_CG_KO" },
          stopWwiseEventNameJP = { "Map_Reset_Bus_Volume", "SFX_Reset_Bus_Volume", "Stop_Cutscene_Muffin_CG" },
          startWwiseEventNameJP = { "Map_Set_Bus_Volume", "SFX_Set_Bus_Volume", "Play_Cutscene_Muffin_CG_JP" },
          stopWwiseEventNameUS = { "Map_Reset_Bus_Volume", "SFX_Reset_Bus_Volume", "Stop_Cutscene_Muffin_CG" },
          startWwiseEventNameUS = { "Map_Set_Bus_Volume", "SFX_Set_Bus_Volume", "Play_Cutscene_Muffin_CG_US" },
          triggerLevel = 201, --当前地图到达这一关且最大通关地图小于这一关时触发
          MaskTimeLength = 2.5, --视频的遮罩多长，决定多久后开始播放视频
          CGKey = "NeedShowChangeMapCG_", --是否播放过CG的唯一KEy 不能重复
        },
        { name = "video/changelandcg",
          assetName = "ChangeLandCg.m4v",
          startWwiseEventNameCN = { "Map_Set_Bus_Volume", "SFX_Set_Bus_Volume", "Play_Cutscene_Muffin_CG_001_CN" },
          stopWwiseEventNameCN = { "Map_Reset_Bus_Volume", "SFX_Reset_Bus_Volume", "Stop_Cutscene_Muffin_CG" },
          stopWwiseEventNameTW = { "Map_Reset_Bus_Volume", "SFX_Reset_Bus_Volume", "Stop_Cutscene_Muffin_CG" },
          startWwiseEventNameTW = { "Map_Set_Bus_Volume", "SFX_Set_Bus_Volume", "Play_Cutscene_Muffin_CG_001_TW" },
          stopWwiseEventNameKR = { "Map_Reset_Bus_Volume", "SFX_Reset_Bus_Volume", "Stop_Cutscene_Muffin_CG" },
          startWwiseEventNameKR = { "Map_Set_Bus_Volume", "SFX_Set_Bus_Volume", "Play_Cutscene_Muffin_CG_001_KO" },
          stopWwiseEventNameJP = { "Map_Reset_Bus_Volume", "SFX_Reset_Bus_Volume", "Stop_Cutscene_Muffin_CG" },
          startWwiseEventNameJP = { "Map_Set_Bus_Volume", "SFX_Set_Bus_Volume", "Play_Cutscene_Muffin_CG_001_JP" },
          stopWwiseEventNameUS = { "Map_Reset_Bus_Volume", "SFX_Reset_Bus_Volume", "Stop_Cutscene_Muffin_CG" },
          startWwiseEventNameUS = { "Map_Set_Bus_Volume", "SFX_Set_Bus_Volume", "Play_Cutscene_Muffin_CG_001_US" },
          triggerLevel = 4701, --当前地图到达这一关且最大通关地图小于这一关时触发
          MaskTimeLength = 2.5, --视频的遮罩多长，决定多久后开始播放视频
          CGKey = "NeedShowChangeMapCG2_", --是否播放过CG的唯一KEy 不能重复
        },
        { name = "video/changemapcg6501",
          assetName = "ChangeMapCg6501.m4v",
          startWwiseEventNameCN = { "Map_Set_Bus_Volume", "SFX_Set_Bus_Volume", "Play_Cutscene_Muffin_CG_002_CN" },
          stopWwiseEventNameCN = { "Map_Reset_Bus_Volume", "SFX_Reset_Bus_Volume", "Stop_Cutscene_Muffin_CG" },
          stopWwiseEventNameTW = { "Map_Reset_Bus_Volume", "SFX_Reset_Bus_Volume", "Stop_Cutscene_Muffin_CG" },
          startWwiseEventNameTW = { "Map_Set_Bus_Volume", "SFX_Set_Bus_Volume", "Play_Cutscene_Muffin_CG_002_TW" },
          stopWwiseEventNameKR = { "Map_Reset_Bus_Volume", "SFX_Reset_Bus_Volume", "Stop_Cutscene_Muffin_CG" },
          startWwiseEventNameKR = { "Map_Set_Bus_Volume", "SFX_Set_Bus_Volume", "Play_Cutscene_Muffin_CG_002_KO" },
          stopWwiseEventNameJP = { "Map_Reset_Bus_Volume", "SFX_Reset_Bus_Volume", "Stop_Cutscene_Muffin_CG" },
          startWwiseEventNameJP = { "Map_Set_Bus_Volume", "SFX_Set_Bus_Volume", "Play_Cutscene_Muffin_CG_002_JP" },
          stopWwiseEventNameUS = { "Map_Reset_Bus_Volume", "SFX_Reset_Bus_Volume", "Stop_Cutscene_Muffin_CG" },
          startWwiseEventNameUS = { "Map_Set_Bus_Volume", "SFX_Set_Bus_Volume", "Play_Cutscene_Muffin_CG_002_US" },
          triggerLevel = 6501, --当前地图到达这一关且最大通关地图小于这一关时触发
          MaskTimeLength = 2.5, --视频的遮罩多长，决定多久后开始播放视频
          CGKey = "NeedShowChangeMapCG3_", --是否播放过CG的唯一KEy 不能重复
        },
        { name = "video/changemapcg8301",
          assetName = "ChangeMapCg8301.m4v",
          startWwiseEventNameCN = { "Map_Set_Bus_Volume", "SFX_Set_Bus_Volume", "Play_Cutscene_Muffin_CG_003_CN" },
          stopWwiseEventNameCN = { "Map_Reset_Bus_Volume", "SFX_Reset_Bus_Volume", "Stop_Cutscene_Muffin_CG" },
          stopWwiseEventNameTW = { "Map_Reset_Bus_Volume", "SFX_Reset_Bus_Volume", "Stop_Cutscene_Muffin_CG" },
          startWwiseEventNameTW = { "Map_Set_Bus_Volume", "SFX_Set_Bus_Volume", "Play_Cutscene_Muffin_CG_003_TW" },
          stopWwiseEventNameKR = { "Map_Reset_Bus_Volume", "SFX_Reset_Bus_Volume", "Stop_Cutscene_Muffin_CG" },
          startWwiseEventNameKR = { "Map_Set_Bus_Volume", "SFX_Set_Bus_Volume", "Play_Cutscene_Muffin_CG_003_KO" },
          stopWwiseEventNameJP = { "Map_Reset_Bus_Volume", "SFX_Reset_Bus_Volume", "Stop_Cutscene_Muffin_CG" },
          startWwiseEventNameJP = { "Map_Set_Bus_Volume", "SFX_Set_Bus_Volume", "Play_Cutscene_Muffin_CG_003_JP" },
          stopWwiseEventNameUS = { "Map_Reset_Bus_Volume", "SFX_Reset_Bus_Volume", "Stop_Cutscene_Muffin_CG" },
          startWwiseEventNameUS = { "Map_Set_Bus_Volume", "SFX_Set_Bus_Volume", "Play_Cutscene_Muffin_CG_003_US" },
          triggerLevel = 8301, --当前地图到达这一关且最大通关地图小于这一关时触发
          MaskTimeLength = 2.5, --视频的遮罩多长，决定多久后开始播放视频
          CGKey = "NeedShowChangeMapCG4_", --是否播放过CG的唯一KEy 不能重复
        },
        { name = "video/changemapcg10501",
          assetName = "ChangeMapCg10501.m4v",
          startWwiseEventNameCN = { "Map_Set_Bus_Volume", "SFX_Set_Bus_Volume", "Play_Cutscene_Muffin_CG_004_CN" },
          stopWwiseEventNameCN = { "Map_Reset_Bus_Volume", "SFX_Reset_Bus_Volume", "Stop_Cutscene_Muffin_CG" },
          stopWwiseEventNameTW = { "Map_Reset_Bus_Volume", "SFX_Reset_Bus_Volume", "Stop_Cutscene_Muffin_CG" },
          startWwiseEventNameTW = { "Map_Set_Bus_Volume", "SFX_Set_Bus_Volume", "Play_Cutscene_Muffin_CG_004_TW" },
          stopWwiseEventNameKR = { "Map_Reset_Bus_Volume", "SFX_Reset_Bus_Volume", "Stop_Cutscene_Muffin_CG" },
          startWwiseEventNameKR = { "Map_Set_Bus_Volume", "SFX_Set_Bus_Volume", "Play_Cutscene_Muffin_CG_004_KO" },
          stopWwiseEventNameJP = { "Map_Reset_Bus_Volume", "SFX_Reset_Bus_Volume", "Stop_Cutscene_Muffin_CG" },
          startWwiseEventNameJP = { "Map_Set_Bus_Volume", "SFX_Set_Bus_Volume", "Play_Cutscene_Muffin_CG_004_JP" },
          stopWwiseEventNameUS = { "Map_Reset_Bus_Volume", "SFX_Reset_Bus_Volume", "Stop_Cutscene_Muffin_CG" },
          startWwiseEventNameUS = { "Map_Set_Bus_Volume", "SFX_Set_Bus_Volume", "Play_Cutscene_Muffin_CG_004_US" },
          triggerLevel = 10501, --当前地图到达这一关且最大通关地图小于这一关时触发
          MaskTimeLength = 2.5, --视频的遮罩多长，决定多久后开始播放视频
          CGKey = "NeedShowChangeMapCG5_", --是否播放过CG的唯一KEy 不能重复
        },
    }
}
class.EnterGameMoviePath = "Movies/video_EnterGame"
--class.LoginGameMoviePath = "Movies/video_LoginGame"


--endregion 



--region Constructor and Init

--初始化背景CG类型和URL
local function InitBackGroundCgData()
    --缺省值
    class.backGroundCGType = 0
    local backGroundCgObj = GameObject.Find("GameCGPlane_CheckUpdate")
    if backGroundCgObj ~= nil then
        local bgVideoPlayerTra = GlobalFun.GetTra(backGroundCgObj.transform, "obj_VideoPlayer")
        local bgVideoPlayer= GlobalFun.GetType(bgVideoPlayerTra,nil,UnityEngine.Video.VideoPlayer)
        if bgVideoPlayer then
            class.backGroundCGURL = bgVideoPlayer.clip.name
            if class.backGroundCGURL then
                if not GlobalFun.IsNilOrNull(class.backGroundCGURL) then
                    local reverseStr = string.reverse(class.backGroundCGURL)
                    --local extensionStrRevIndex = string.find(reverseStr, ".", 0, true)
                    --local extensionStrIndex = string.len(self.backGroundCGURL) - extensionStrRevIndex + 1
                    --local numberStr = string.sub(self.backGroundCGURL, extensionStrIndex - 1, extensionStrIndex - 1)
                    local numberStr = string.sub(reverseStr, 1, 1)
                    if numberStr then
                        local number = tonumber(numberStr)
                        if number > 2 then
                            --夜晚 3 /4 .m4v
                            class.backGroundCGType = 1
                        end
                    end
                end
            end
        end
    end
end
function class.RegisterEventListeners()
 
end
function class.OnBtnSkipClick()
    if class.type==GameCgManager.CG_Data.LOGIN_GAME then
        class.StopCG(true)
        EventManager:EventResponse("LoadingPlaneView", "Replay")
    else
        for i, data in pairs(GameCgManager.CG_Data.CHANGE_MAP_CG) do
            if  class.type == data then
                --todo
                --StopWwise
                class.StopCG(true)
            end
        end
    end
end
function class.Init(tra,type)
    class.type=type
    if not BattleTest.ActionConfig.PlayCG then
        class.hasError = true
    end
    if type == GameCgManager.CG_Data.LOGIN_GAME then
        --初始化背景CG Data
        InitBackGroundCgData()
    end
 
    if tra then
        class.uiView=GameCGPlaneUIView.new(tra)
    end
end

function class.Dispose()
    class.isPlaying = false
    if class.videoPlayer then
        class.videoPlayer.clip = nil
    end
    if class.type == class.CG_Data.LOGIN_GAME then
        if LoginGamePlane.loadingPlane then
            GlobalFun.DestroyObj(LoginGamePlane.loadingPlane.gameObject, 0)
        end
    else
        for i, data in pairs(GameCgManager.CG_Data.CHANGE_MAP_CG) do
            if class.type == data then
                GlobalAsset.Unload(data.name)
            end
        end
    end
 
    class._data = nil
    class.uiView = nil
    class.startAction = nil
    class._callBack = nil
    class.videoPlayer = nil
    class.CS2LuaEntry = nil
    class.hasError = false
    _isStopping = nil
    Resources.UnloadUnusedAssets()
    class.ReleaseLoginGameSkillShowRenderTexture()
end





--endregion

--region Delegates and Events

-- videoPlayerEvents 如果不支持cg播放  
function class.OnErrorReceived()
    if class.OnPrepareCompletedCallBack then
        class.OnPrepareCompletedCallBack()
    end
    class.hasError = true
end
function class.OnPrepareCompleted()
    if class.OnPrepareCompletedCallBack then
        class.OnPrepareCompletedCallBack()
    end
end

function class.UpdateAction()
    class.CheckCameraEnable()
    --print("self._planeUI.GetVideoPlayer().frame", self._planeUI.GetVideoPlayer().frame, self._planeUI.GetVideoPlayer().time,self._planeUI.GetVideoPlayer().isPlaying,self.isPlaying)
    if not class.videoPlayer then return end 
    if  class.videoPlayer.frame >= 1 and class.videoPlayer.isPlaying and not class.isPlaying then
        if class.startAction ~= nil then
            class.startAction()
        end
        class.startAction = nil
        class.isPlaying = true
    end
    if class.videoPlayer.isPlaying == false and class.isPlaying then
        class.isPlaying = false
    
        class.StopCG()
    end
end

--endregion

--region CG Functionality

--预加载
function class.PrepareCG(type)
    class._data = nil
    class.videoPlayer = class._planeUI.GetVideoPlayer()
    if class.CS2LuaEntry == nil then
        class.CS2LuaEntry = class.videoPlayer.gameObject:AddComponent(CS2LuaVideoPlayerEventsEntry)
        class.CS2LuaEntry:SetLuaTable(class)
    end
    if type == class.CG_Data.LOGIN_GAME then
        class._planeUI.ShowCgPlane(false)
        class.videoPlayer.clip = Resources.Load(class.EnterGameMoviePath)
        class.videoPlayer:Prepare()
        class._data = class.CG_Data.LOGIN_GAME
    end
end

--预加载
function class.PrepareCG2(player, type , onPrepareCompletedCallBack)
    class.OnPrepareCompletedCallBack=onPrepareCompletedCallBack
    if player then
        class.videoPlayer = player
        if class.CS2LuaEntry == nil then
            class.CS2LuaEntry = class.videoPlayer.gameObject:AddComponent(CS2LuaVideoPlayerEventsEntry)
            class.CS2LuaEntry:SetLuaTable(class, player)
        end
        if type == class.CG_Data.LOGIN_GAME then
            class.videoPlayer.clip = Resources.Load(class.EnterGameMoviePath)
            class.videoPlayer:Prepare()
            class._data = class.CG_Data.LOGIN_GAME
        else  
            class._data = type
            local bundleName=type.name
            GlobalAsset.Load(bundleName, function(abTab, ab)
                local video = ab:LoadAsset(type.assetName)
                if video then
                    --GlobalFun.SetObj(self.videoTra.gameObject, true)
                    class.videoPlayer.clip = video
                    class.videoPlayer:Prepare()
                end
            end)
        end
    else
        class.hasError = true
        if class.OnPrepareCompletedCallBack then
            class.OnPrepareCompletedCallBack()
        end
  
    end

end

function class.PlayCG(type, callBack, startAction)
    --设备不支持播放cg
    if class.hasError  then
        if startAction ~= nil then
            startAction()
        end
        if callBack ~= nil then
            callBack()
        end
        class.Dispose()
        return
    end
    class.startAction = startAction
    class._callBack = callBack
    if type == class.CG_Data.LOGIN_GAME then
        SoundManager.PlayStartGameCGBGM()
        GlobalFun.InvokeByFrame(function()
          --   class._planeUI.ShowCgPlane(true)
        end)
        if class.videoPlayer.isPrepared and class.videoPlayer.isPlaying == false then
            class.videoPlayer.targetCamera = BasePlane.GetUiCamera()
            BasePlane.SetUICameraRenderer(2)
            class.videoPlayer:Play()
            if class.uiView then
                class.uiView:Show(true)
            end
        end
    else
        if class.videoPlayer.isPrepared and class.videoPlayer.isPlaying == false then
            --class.videoPlayer.targetCamera = BasePlane.GetUiCamera()
            --BasePlane.SetUICameraRenderer(2)
            class.videoPlayer.targetTexture =class.GetCgRenderTexture()
            class.videoPlayer:Play()
            if Config.PackageChannelType == 0 and class._data.startWwiseEventNameCN then
                class._data.startWwiseEventName = class._data.startWwiseEventNameCN
                class._data.stopWwiseEventName = class._data.stopWwiseEventNameCN
            elseif Config.PackageChannelType == 3 and class._data.startWwiseEventNameTW then
                class._data.startWwiseEventName = class._data.startWwiseEventNameTW
                class._data.stopWwiseEventName = class._data.stopWwiseEventNameTW
            elseif Config.PackageChannelType == 4 and class._data.startWwiseEventNameKR then
                class._data.startWwiseEventName = class._data.startWwiseEventNameKR
                class._data.stopWwiseEventName = class._data.stopWwiseEventNameKR
            elseif Config.PackageChannelType == 5 and class._data.startWwiseEventNameJP then
                class._data.startWwiseEventName = class._data.startWwiseEventNameJP
                class._data.stopWwiseEventName = class._data.stopWwiseEventNameJP
            elseif class._data.startWwiseEventNameUS then
                class._data.startWwiseEventName = class._data.startWwiseEventNameUS
                class._data.stopWwiseEventName = class._data.stopWwiseEventNameUS
            end
            if class._data .startWwiseEventName then
                for i, v in ipairs(class._data .startWwiseEventName ) do
                    SoundManager.PlayUISound( v)
                end
           
            end
            if class.uiView then
                class.uiView:Show(true)
            end
        end
    end
end

function class.StopCG(isSkip)
    --endregion 
    if _isStopping then
        return
    end
    _isStopping = true
    if class.type == class.CG_Data.LOGIN_GAME then
        SoundManager.StopLoginInterfaceBgm()
    else
        if class._data and class._data.stopWwiseEventName then
            for i, v in ipairs( class._data.stopWwiseEventName) do
                SoundManager.PlayUISound(v)
            end
        end
    end
    if class._data and class._data.needMask and not isSkip then
        class.videoPlayer:Pause()
        LeanTween.value(0.0, 1.0, class._data.alphaTime):setOnUpdate(function(_value)
            class.uiView._texMask.alpha = _value
        end)     :setOnComplete(function()
            if class._callBack ~= nil then
                class._callBack()
                class._callBack = nil
            end
            class.videoPlayer:Stop()
            BasePlane.SetUICameraRenderer(1)
            LeanTween.value(1.0, 0.0, class._data.alphaTime):setOnUpdate(function(_value)
                class.uiView._texMask.alpha = _value
            end)     :setOnComplete(function()
                class.Dispose()
            end)
        end)
    else
        BasePlane.SetUICameraRenderer(1)
        if class._callBack ~= nil then
            class._callBack()
            class._callBack = nil
        end
        if not GlobalFun.IsNilOrNull(class.videoPlayer) then
            class.videoPlayer:Stop()
        end
       -- GlobalFun.SetObj(class._planeUI.GetVideoPlayer().gameObject, false)
        class.Dispose()
    end
end

--endregion 

--region Utility Functions

--返回背景CG动画的时间,0清晨 1晚上
function class.GetBackGroundCGTime()
    return class.backGroundCGType
end

function class.CheckiOSLoginCG()
    if not isPlatformIPhone then--只有苹果才会有的问题
        return
    end
    local videoPlayer = LoginGamePlane.bgVideoPlayer
    if LoginGamePlane and not GlobalFun.IsNilOrNull(videoPlayer) then
        if not SelectRoleLogic.GetCurrState() and videoPlayer.isPrepared then
            BattleCamera.GetCamera().enabled=false
            videoPlayer:Stop()
            GlobalFun.InvokeByFrame(function()
                --恢复背景CG渲染
                videoPlayer:Play()
                needToCheckCameraEnable=true
            end)
        end
    end
    if  not GlobalFun.IsNilOrNull(class.videoPlayer) and class.videoPlayer.isPrepared and class.videoPlayer.isPlaying then
        class.videoPlayer:Stop()
    end
end

function class.CheckCameraEnable()
    if not needToCheckCameraEnable then return end
    local videoPlayer = LoginGamePlane.bgVideoPlayer
    if LoginGamePlane and not GlobalFun.IsNilOrNull(videoPlayer) then
        if not SelectRoleLogic.GetCurrState() and videoPlayer.isPrepared then
            if videoPlayer.frame >= 1 then
                BattleCamera.GetCamera().enabled=true
                needToCheckCameraEnable=false
            end
        end
    end
end
--endregion

--region Temporary RenderTexture
local loginSkillShowRT
function class.GetLoginGameSkillShowRenderTexture()
    if not loginSkillShowRT then
        loginSkillShowRT=RenderTexture.GetTemporary(1080, 640, 0,8);
    end
    return loginSkillShowRT
end
function class.ReleaseLoginGameSkillShowRenderTexture()
    if loginSkillShowRT then
        RenderTexture.ReleaseTemporary(loginSkillShowRT)
    end
end

local gameCgRT
function class.GetCgRenderTexture()
    if not gameCgRT then
        gameCgRT=RenderTexture.GetTemporary(720, 1280, 0,8);
    end
    return gameCgRT
end
function class.ReleaseCgRenderTexture()
    if gameCgRT then
        RenderTexture.ReleaseTemporary(gameCgRT)
    end
end
--endregion



