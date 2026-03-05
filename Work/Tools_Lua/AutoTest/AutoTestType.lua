local _ENV = Boli2Env

local autoTestEvent = {
    LoginView = 1, -- 第一屏登录界面
    WelcomeView = 2, -- 登陆完成，点击进入选角界面的界面
    SelectRole = 3, -- 选择角色界面
    MainView = 4, -- 主界面

    -- 创建角色
    CreateRole_Job = 5,-- 选择职业
    CreateRole_JobCG = 6,-- 职业CG
    CreateRole_Config = 7, -- 捏脸
    CreateRole_Name = 8, -- 名称

    --- Loading
    LoadingView = 30,

    --- 引导相关
    -- 首次引导对话
    FirstGuideDialog = 40,
    -- Tips引导
    GuideTipSkipConfirm = 41,

    --- 升级弹窗
    UpgradePopView = 50,


}

local autoTestFuncType = {
    ---
    LoginButton = 1, -- 第一屏登录按钮
    ---
    StartGameButton = 2, -- 开始游戏按钮
    CreateRoleButton = 3, -- 创建角色按钮
    CreateRoleJob = 4, -- 选择角色，参数 job
    SkipCreateRoleJob = 5, -- 跳过创角的动画
    CreateRoleJobAnimFinish = 6, -- 创建角色结束之后的“前往创建”按钮
    CreateRoleSelectSex = 7, -- 创建角色选择性别
    CreateRoleStartGame = 8, -- 创建角色开始游戏
    SkipFirstEnterGameDialog = 10, -- 跳过首次进入游戏的对话
    CreateRoleSelectSex = 11,


    --- 选择角色界面
    SelectRoleEnterGame = 20,

    --- Loading
    SkipLoadingDisplay = 30,

    --- 首次引导对话
    FirstGuideDialogNext = 40,
    SkipTipsGuide = 41,
    ConfirmSkipTipsGuide=42

}

return {
    autoTestFuncType = autoTestFuncType,
    autoTestEvent = autoTestEvent
}