local _ENV = Boli2Env

ReConnectPlane = _ViewBase:New("ReConnectPlane", PlaneType.Loading)

local PlaneType = {
    Disable = 0,
    Tips = 1,
    Check = 2,
}


local rotateSpeed = 200
local planeType = PlaneType.Disable
local tipsObj
local checkObj
local clickCount = 0
local loadingBarTransform
local openTime = 0

local function OnClickReconnect()
    GameNet.ClearConnectNum()
    ReConnectPlane.ShowPlane(false)
    if SceneManager.GetActiveScene().name == GlobalVariable.LevelLightSceneName then
        clickCount = clickCount + 1
    end
end

function ReConnectPlane.ShowPlane(isNeedCheckNet)
    local nextPlaneType = PlaneType.Tips
    if isNeedCheckNet then
        nextPlaneType = PlaneType.Check
        if clickCount >= 3 then
            GameMain:OnLogout()
            return
        end
    end
    if nextPlaneType == planeType then
        return
    end
    planeType = nextPlaneType
    UIManager:Open("ReConnectPlane")
end

function ReConnectPlane:InitComponents()
    local tra = self.tra
    tipsObj = GlobalFun.GetObj(tra, "tex_TipsBg")
    GlobalFun.GetText(tipsObj.transform, "Text").text = GlobalText.GetText("NetReconnect")
    checkObj = GlobalFun.GetObj(tra, "tex_CheckBg")
    GlobalFun.GetText(checkObj.transform, "Title").text = GlobalText.GetText("NetTipsTitle")
    GlobalFun.GetText(checkObj.transform, "Tips").text = GlobalText.GetText("NetCheck")
    local reconnectBtn = GlobalFun.GetBtn(checkObj.transform, "btn_Reconnect", OnClickReconnect)
    GlobalFun.GetText(reconnectBtn.transform, "Text").text = GlobalText.GetText("NetReconnectBtn")
end

function ReConnectPlane:OnOpen(_value,_totalTime,_onComplete,_onShow)
    local isShowTips = planeType == PlaneType.Tips
    if isShowTips then
        openTime = GameMain:GetTime()
        GlobalFun.SetObj(tipsObj, false)
    end
    GlobalFun.SetObj(checkObj, not isShowTips)
end

function ReConnectPlane:Update(_deltaTime)
    local nowTime = GameMain:GetTime()
    if nowTime - openTime >= 5 then -- 网络断开时不立刻显示断开界面，超时一定时间再弹出
        openTime = nowTime
        GlobalFun.SetObj(tipsObj, true)
    end
end

function ReConnectPlane:OnClose()
    planeType = PlaneType.Disable
    clickCount = 0
end

function ReConnectPlane:OnDestroy()

end
