local _ENV = Boli2Env

local routes = {
    logicEvent = "_event_LogicEvent",
}

local getBindLogicFunc = function(self, key)
    local func = IndexTable(self, routes.logicEvent, key)
    if not func then
        func = function(...)
            self:CallHook("PostEvent", key, ...)
        end
        SetTableValue(self, func, routes.logicEvent, key)
    end
    return func
end

local bindLogicListener = function(_self, eventName, func, state)
    local regFuncName = state and "RegisterCallBack" or "RemoveCallBack"
    local regFunc = MsgMg[regFuncName]
    if not regFunc then
        return
    end
    regFunc(eventName, func)
end

local handleLogicListener = function(self, state)
    local logicEventConfig = self:GetHookConfig("Event")
    if type(logicEventConfig) ~= "table" then
        return
    end
    for key, config in pairs(logicEventConfig) do
        local bindFunc = getBindLogicFunc(self, key)
        bindLogicListener(self, config, bindFunc, state)
    end
end

function UIExtension:Event_ModuleStatusChanged(status)
    if status == UIStatus.Displaying then
        handleLogicListener(self, true)
    elseif status == UIStatus.Exit then
        handleLogicListener(self, false)
    end
end

function UIExtension:Event_CatchNewIndex(key, value)
    if key == "Event" then
        return self:WrapHookConfig(key, value)
    end
end

function UIExtension:Event_Initialize()
    self:ExposeAPI("SendEvent", function(eventName, ...)
        MsgMg.SendMsg(eventName, ...)
    end)
end