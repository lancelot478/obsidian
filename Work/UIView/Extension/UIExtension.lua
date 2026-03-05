local _ENV = Boli2Env

local routes = {
    stagedConfig = "_stagedConfig",
    exposeApi = "_exposeApi",
}

UIExtension = class("UIExtension")
UIExtension.static = {
    routes = routes
}

function UIExtension:ctor(core)
    self.core = core

    
    self:Event_Initialize()
    self:ViewInterface_Initialize()
    self:Partial_Initialize()
end

function UIExtension:SetModule(module)
    self.module = module
end

function UIExtension:WrapHookConfig(hookName, config, index, newIndex)
    local core = self.core
    return core.WrapHookConfig(hookName, config, index, newIndex)
    --local hookInterface = self.hookInterface
    --if not hookInterface then
    --    hookInterface = {}
    --    self.hookInterface = hookInterface
    --end
    --
    --local configInterface = hookInterface[hookName]
    --if configInterface then
    --    error("config already initialized, cannot reassign data: ", hookName)
    --    return
    --end
    --if not index then
    --    index = function(_, key)
    --        return key
    --    end
    --end
    --local tab = {}
    --setmetatable(tab, { __index = index, __newindex = newIndex })
    --hookInterface[hookName] = tab
    --
    --SetTableValue(self, config, routes.stagedConfig, hookName)
end

function UIExtension:GetHookConfig(...)
    --return IndexTable(self, routes.stagedConfig, ...)

    local core = self.core
    return core.GetHookConfig(...)
end

function UIExtension:ExposeAPI(name, func)
    SetTableValue(self, func, routes.exposeApi, name)
end

function UIExtension:ModuleStatusChanged(status)
    self:Event_ModuleStatusChanged(status)
    self:Component_ModuleStatusChanged(status)
    self:Memo_ModuleStatusChanged(status)
    self:Partial_ModuleStatusChanged(status)
end

function UIExtension:CallHook(hookName, hookKey, ...)
    local core = self.core
    return core.CallHook(hookName, hookKey, ...)
end

function UIExtension:CatchNewIndex(key, value)
    local result
    result = result or self:Event_CatchNewIndex(key, value)
    result = result or self:Component_CatchNewIndex(key, value)
    result = result or self:Memo_CatchNewIndex(key, value)
    result = result or self:Partial_CatchNewIndex(key, value)
    return result
end

function UIExtension:CatchIndex(key)
    local result
    result = result or IndexTable(self, routes.exposeApi, key)

    result = result or self:Memo_CatchIndex(key)
    result = result or self:Partial_CatchIndex(key)
    return result
end

function UIExtension:CallViewBase(memberName, ...)
    local core = self.core
    return core.CallViewBase(memberName, ...)
end