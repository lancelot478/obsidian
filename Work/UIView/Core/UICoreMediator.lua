local callUIFunc = function(self, funcName, ...)
    local member = IndexTable(self, funcName)
    if type(member) ~= "function" then
        return member
    end
    --local args = { ... }
    local status, ret = GlobalFun.Try(member, ...)
    if not status then
        return
    end
    return ret
end

---------------- Life cycle ----------------
function InitPanel(self)
    local status = callUIFunc(self, "GetStatus")
    if status > UIStatus.UIInitialized or status < UIStatus.ModuleInitialized then
        return
    end
    local param = callUIFunc(self, "Param")
    callUIFunc(self, "SetStatus", UIStatus.UIInitialized)
    callUIFunc(self, "InitPanel", param)
end

function OnEnter(self)
    local status = callUIFunc(self, "GetStatus")
    if status == UIStatus.Displaying then
        return
    end
    local param = callUIFunc(self, "Param")
    callUIFunc(self, "SetStatus", UIStatus.Displaying)
    callUIFunc(self, "OnEnter", table.unpack(param))
end

function OnUpdate(self, deltaTime)
    local status = callUIFunc(self, "GetStatus")
    if status ~= UIStatus.Displaying then
        return
    end
    callUIFunc(self, "OnUpdate", deltaTime)
end

function OnLateUpdate(self, deltaTime)
    local status = callUIFunc(self, "GetStatus")
    if status ~= UIStatus.Displaying then
        return
    end
    callUIFunc(self, "OnLateUpdate", deltaTime)
end

function OnExit(self)
    local status = callUIFunc(self, "GetStatus")
    if status ~= UIStatus.Displaying then
        return
    end
    callUIFunc(self, "SetStatus", UIStatus.BeforeExit)
    callUIFunc(self, "OnExit")
    callUIFunc(self, "SetStatus", UIStatus.Exit)
end

function OnDestroy(self)
    callUIFunc(self, "OnDestroy")
    callUIFunc(self, "SetStatus", UIStatus.Destroy)
end

---------------- API ----------------
function ModuleInitialize(self, viewBase)
    callUIFunc(self, "Initialize", viewBase)
end

function SetParam(self, param)
    callUIFunc(self, "SetParam", param)
end

function GetViewType(self)
    return callUIFunc(self, "ViewType")
end