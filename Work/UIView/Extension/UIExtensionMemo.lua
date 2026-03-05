local _ENV = Boli2Env

local routes = {
    memoData = { "Container", "MemoData" },
    initializeMemo = { "Container", "InitializeMemo" },
    hookList = { "Container", "HookList" },
}

local memoCompare = function(self, memo, value, old)
    local status, state = self:CallHook("PostMemoCompare", memo, value, old)
    if status then
        return state
    else
        if type(value) == "table" then
            return
        end
        return old == value
    end
end

local onMemoEvent = function(self, module, memo, value, old)
    local instanceId = module.InstanceId
    self:CallHook("PostMemo", memo, value, old, instanceId)
end

local injectMemoData = function(self, initializeConfig)
    local core = self.core
    local memoConfig = rawget(core, "Memo")
    if memoConfig then
        error("cannot reset memo definition")
    end
    SetTableValue(core, initializeConfig, table.unpack(routes.initializeMemo))
    memoConfig = {}
    local memoMeta = { __index = function(_, key)
        local currentStatus = CallTable(core, "GetStatus")
        local moduleInitialized = currentStatus >= UIStatus.ModuleInitialized
        if not moduleInitialized then
            return key
        else
            local value
            local uiInitialized = currentStatus >= UIStatus.UIInitialized
            if uiInitialized then
                value = IndexTable(core, ComposeTableRoute(routes.memoData, key))
                --if not value then
                --    LocalAreaForeach(function(inst)
                --        if value ~= nil then
                --            return true
                --        end
                --        value = IndexTable(inst, ComposeTableRoute(routes.memoData, key))
                --    end)
                --end
            else
                value = IndexTable(core, ComposeTableRoute(routes.initializeMemo, key))
            end
            return value
        end
    end, __newindex = function(_, key, value)
        local currentStatus = CallTable(core, "GetStatus")
        local uiDisplaying = currentStatus >= UIStatus.Displaying
        if not uiDisplaying then
            SetTableValue(core, value, ComposeTableRoute(routes.initializeMemo, key))
            return
        end
        local old = IndexTable(core, ComposeTableRoute(routes.memoData, key))
        local isSame = memoCompare(self, key, value, old)
        if isSame then
            return
        end
        SetTableValue(core, value, ComposeTableRoute(routes.memoData, key))
        local module = self.module
        onMemoEvent(self, module, key, value, old)
    end }
    setmetatable(memoConfig, memoMeta)
    rawset(core, "Memo", memoConfig)
    return memoConfig
end

local initializeMemo = function(self)
    -- 默认值赋值
    local initializeConfig = IndexTable(self, table.unpack(routes.initializeMemo))
    if initializeConfig then
        for key, value in pairs(initializeConfig) do
            SetTableValue(self, value, ComposeTableRoute(routes.memoData, key))
        end
    end
end

local firstCallMemo = function(self, module)
    -- 所有钩子key
    local memoHooks = IndexTable(module, ComposeTableRoute(routes.hookList, "PostMemo"))
    if not memoHooks then
        return
    end
    for key, _ in pairs(memoHooks) do
        local value = IndexTable(module, "Memo", key)
        onMemoEvent(self, module, key, value)
    end
end

---------------- Module API ----------------

function UIExtension:Memo_ModuleStatusChanged(status)
    if status == UIStatus.UIInitialized then
        local module = self.module
        initializeMemo(module)
    elseif status == UIStatus.Displaying then
        local module = self.module
        firstCallMemo(self, module)
    end
end

function UIExtension:Memo_CatchIndex(key)
    if key == "Memo" then
        local value = injectMemoData(self)
        return value
    end
end

function UIExtension:Memo_CatchNewIndex(key, value)
    if key == "Memo" then
        return GlobalFun.Try(injectMemoData, self, value)
    end
end