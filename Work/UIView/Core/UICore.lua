---------------- Core Define

local routes = {
    extensionList = { "Container", "ExtensionList" },
    hookList = { "Container", "HookList" },
    stagedConfig = { "Container", "StagedConfig" },
}

local _core

function CallExtension(memberName, ...)
    local args = { ... }
    local core = _core or core
    _core = core
    return GlobalFun.Try(function()
        local extension = core.extension
        if not extension then
            return
        end
        local member = extension[memberName]
        if type(member) == "function" then
            return member(extension, table.unpack(args))
        else
            return member
        end
        error(string.format("ui core extension member not found: %s", tostring(memberName)))
    end)
end

function WrapHookConfig(hookName, config, index, newIndex)
    local core = _core or core
    _core = core
    local configInterface = rawget(core, hookName)
    if configInterface then
        print("config already initialized, cannot reassign data: ", hookName)
        return
    end
    if not index then
        index = function(_, key)
            return key
        end
    end
    local tab = {}
    setmetatable(tab, { __index = index, __newindex = newIndex })
    rawset(core, hookName, tab)
    SetTableValue(core, config, ComposeTableRoute(routes.stagedConfig, hookName))
    return true
end

function GetHookConfig(...)
    local core = _core or core
    _core = core
    return IndexTable(core, ComposeTableRoute(routes.stagedConfig, ...))
end

---------------- Before Module Initialize Complete
local onAddHookData = function(core, hookName, key, value)
    if not key then
        print(string.format("add hook data but index is nil", hookName))
        return
    end
    local hookTable = IndexTable(core, ComposeTableRoute(routes.hookList, hookName))
    if not hookTable then
        return
    end
    local oldData = rawget(hookTable, key)
    if oldData then
        print(string.format("list [%s] already has element named [%s]", hookName, key))
    end
    rawset(hookTable, key, value)
end

local onRegisterExtension = function(core, moduleName, extendModule)
    if not moduleName or not extendModule then
        error("empty ui core extension module")
    end
    SetTableValue(core, extendModule, ComposeTableRoute(routes.extensionList, moduleName))
end

local genAutoCreateTable
genAutoCreateTable = function()
    local tab = {}
    setmetatable(tab, { __index = function(t, k)
        local data = rawget(t, k)
        if not data then
            local status = GetStatus()
            if status < UIStatus.UIInitialized then
                data = genAutoCreateTable()
                rawset(t, k, data)
            end
        end
        return data
    end })
    return tab
end

local onIndexHook = function(core, hookName)
    local hookTable = IndexTable(core, ComposeTableRoute(routes.hookList, hookName))
    if hookTable then
        return hookTable
    end
    hookTable = GetOrGenerate(core, function()
        local tab = {}
        setmetatable(tab, { __newindex = function(_, newKey, newValue)
            onAddHookData(core, hookName, newKey, newValue)
        end, __index = function(_, key)
            local hookList = IndexTable(core, ComposeTableRoute(routes.hookList, hookName))
            if not hookList then
                return
            end
            local value = rawget(hookList, key)
            if not value then
                value = genAutoCreateTable()
                rawset(hookList, key, value)
            end
            return value
        end })
        rawset(core, hookName, tab)
        return tab
    end, ComposeTableRoute(routes.hookList, hookName))
    return hookTable
end

---------------- Framework API ----------------
function Initialize(viewBase)
    -- viewBase
    SetTableValue(self, viewBase, "ViewBase")

    -- 注册接口
    local transform = CallViewBase("tra")
    SetTableValue(self, transform, "transform")
    SetTableValue(self, transform.gameObject, "gameObject")
end

function CallViewBase(memberName, ...)
    local viewBase = IndexTable(self, "ViewBase")
    local member = IndexTable(viewBase, memberName)
    if type(member) == "function" then
        local args = { ... }
        return member(viewBase, table.unpack(args))
    else
        return member
    end
end

function CallHook(hookName, hookKey, ...)
    local core = _core or core
    _core = core
    local hookHandler = IndexTable(core, ComposeTableRoute(routes.hookList, hookName, hookKey))
    if type(hookHandler) ~= "function" then
        return
    end
    local args = { ... }
    return GlobalFun.Try(function()
        local arg1, arg2, arg3, arg4, arg5 = args[1], args[2], args[3], args[4], args[5]
        return hookHandler(arg1, arg2, arg3, arg4, arg5)
    end)
end

function LocalAreaBroadcast(funcName, ...)
    local status, allInst = CallExtension("GetHostAllInst")
    if not status or not allInst then
        return
    end
    local args = { ... }
    for luaInst, _ in pairs(allInst) do
        GlobalFun.Try(function()
            local member = IndexTable(luaInst, funcName)
            if type(member) ~= "function" then
                print("ui local area broad cast func name is invalid: ", funcName)
                return
            end
            local arg1, arg2, arg3, arg4, arg5 = args[1], args[2], args[3], args[4], args[5]
            member(arg1, arg2, arg3, arg4, arg5)
        end)
    end
end

function LocalAreaForeach(callback)
    local status, allInst = CallExtension("GetHostAllInst")
    if not status or not allInst then
        return
    end
    for luaInst, _ in pairs(allInst) do
        local _, breakFlag = GlobalFun.Try(function()
            callback(luaInst)
        end)
        if breakFlag then
            break
        end
    end
end

function SetStatus(status)
    local core = _core or core
    _core = core
    rawset(core, "Status", status)
    if status >= UIStatus.ExtensionInitialized then
        core.extension:ModuleStatusChanged(status)
    end
end

function GetStatus()
    local core = _core or core
    _core = core
    local status = rawget(core, "Status")
    return status or UIStatus.Uninitialized
end

function SetParam(param)
    self.Param = param
end

---------------- Internal API ----------------
function RegisterExtension(modulePath, extendModule)
    local core = _core or core
    _core = core
    local status = GlobalFun.Try(function()
        onRegisterExtension(core, modulePath, extendModule)
    end)
    if not status then
        print("register ui core extension module failed: ", modulePath)
        return
    end
end

local cachePostKey = UIDriver.__cachePostKey
if not cachePostKey then
    cachePostKey = {}
    UIDriver.__cachePostKey = cachePostKey
end

local postKey = "^Post.*"
function OnIndex(key)
    local core = _core or core
    _core = core
    local value
    -- Hooks
    if not value then
        local result = cachePostKey[key]
        if result == nil then
            result = not not string.match(key, postKey)
            cachePostKey[key] = result
        end
        if result then
            value = onIndexHook(core, key)
        end
    end

    local status = GetStatus()
    if status >= UIStatus.ExtensionInitialized then
        if not value then
            value = core.extension:CatchIndex(key, value)
        end
    end
    return value
end

function OnNewIndex(base, key, value)
    local result
    local status = GetStatus()
    if status >= UIStatus.ExtensionInitialized then
        if not result then
            result = base.extension:CatchNewIndex(key, value)
        end
    end
    if not result then
        rawset(base, key, value)
    end
end