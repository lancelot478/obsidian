local _ENV = Boli2Env

local autoTestType = require("Tools/AutoTest/AutoTestType")

local funcType = autoTestType.autoTestFuncType
local eventType = autoTestType.autoTestEvent

local function invokeCallImpl(callType, ...)
    AutoTest.CallImpl(callType, ...)
end

local onEvent = {
    -- 开屏登录按钮界面
    [eventType.LoginView] = function()
        invokeCallImpl(funcType.LoginButton)
    end,
    -- 登陆完成，进入游戏界面
    [eventType.WelcomeView] = function()
        invokeCallImpl(funcType.StartGameButton)
    end,

    --- 角色列表选择界面
    [eventType.SelectRole] = function()
        invokeCallImpl(funcType.SelectRoleEnterGame)
    end,

    --- 创建角色
    -- 选择职业
    [eventType.CreateRole_Job] = function()
        invokeCallImpl(funcType.CreateRoleJob)
    end,
    -- 职业CG
    [eventType.CreateRole_JobCG] = function()
        invokeCallImpl(funcType.SkipCreateRoleJob)
    end,
    -- 捏脸
    [eventType.CreateRole_Config] = function()
        invokeCallImpl(funcType.CreateRoleSelectSex)
    end,
    -- 名字
    [eventType.CreateRole_Name] = function()
        invokeCallImpl(funcType.CreateRoleStartGame)
    end,

    --- 首次引导对话
    [eventType.FirstGuideDialog] = function(actionType)
        if actionType == GuideActionType.TALK then
            local loopClick
            loopClick = function()
                local showGuide = UIManager:IsPlaneActive(UIDefine.GameGuideView)
                if not showGuide then
                    return
                end
                invokeCallImpl(funcType.FirstGuideDialogNext)
                TimeMgr.AddTask(loopClick, 0.5)
            end
            loopClick()

        elseif actionType == GuideActionType.TIPS then
            invokeCallImpl(funcType.SkipTipsGuide)
        end
    end,
    -- 跳过二次确认框
    [eventType.GuideTipSkipConfirm] = function()
        invokeCallImpl(funcType.ConfirmSkipTipsGuide)
    end,

    --- 升级界面
    [eventType.UpgradePopView] = function()
        local loopCheck
        loopCheck = function()
            local showGuide = UIManager:IsPlaneActive(UIDefine.GameGuideView)
            if not showGuide then
                UIManager:Close(UIDefine.UpgradePopUpView)
                return
            end
            TimeMgr.AddTask(loopCheck, 2)
        end
        TimeMgr.AddTask(loopCheck, 2)
    end,
}

return {
    onEvent = onEvent,
}