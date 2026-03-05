local _ENV = Boli2Env

UIDriver = class("UIDriver", _ViewBase)

function UIDriver:ctor(planeName)
    self.viewName = planeName
    local moduleInst = UIController.LoadUI(planeName)
    if not moduleInst then
        error("无法加载到对应的UI模块", planeName)
        return
    end
    -- 加载子模块
    local moduleInstInterface = setmetatable({}, { __index = function(_, key)
        local value = IndexTable(moduleInst, "self", key)
        return value
    end, __newindex = function(_, key, value)
        local rawInst = IndexTable(moduleInst, "self")
        SetTableValue(rawInst, value, key)
    end, })
    self._moduleInst = moduleInst
    self.ModuleInst = moduleInstInterface

    -- 默认数据填充
    local planeType = self:CallMediator("GetViewType")
    self.Type = planeType
end

function UIDriver:GetSelfInstance()
    local _moduleInst = self._moduleInst
    return _moduleInst.self
end

function UIDriver:CallMediator(funcName, ...)
    local moduleSelf = self:GetSelfInstance()
    return CallTable(IndexTable(self, "_moduleInst"), funcName, moduleSelf, ...)
end

local function hostAndParam(self)
    local param = IndexTable(self, "args")
    self.param = param
end

local function initialize(self)
    self:CallMediator("ModuleInitialize", self)
    self:CallMediator("InitPanel")
end

function UIDriver:OnInit()
    hostAndParam(self)
    initialize(self)
end

function UIDriver:OnOpen()
    local param = IndexTable(self, "args")
    self:CallMediator("SetParam", param)
    self:CallMediator("OnEnter")
end

function UIDriver:OnClose()
    self:CallMediator("OnExit")
end

function UIDriver:OnDestroy()
    self:CallMediator("OnDestroy")
end

function UIDriver:Update(deltaTime)
    self:CallMediator("OnUpdate", deltaTime)
end

function UIDriver:LateUpdate(deltaTime)
    self:CallMediator("OnLateUpdate", deltaTime)
end