local _ENV = Boli2Env

--------------------------------------
--- Config: Partial
--- Hook: PostPartial
--------------------------------------

local routes = {
    partialData = { "Container", "PartialData" },
    moduleInst = { "LuaInst", "ModuleInst" },
    stagedConfig = { "Container", "StagedConfig" },
    loader = { "Container", "loader" },
    cacheRoot = { "LocalHost", "Container", "CacheRoot" },
    cacheLocalPartial = { "LocalHost", "Container", "CacheLocalPartialData" },
}

local partialType = {
    localPrefab = 1,
    component = 2,
}

---------------- Partial

local callHostTarget = function(self, key, path, ...)
    local module = self.module
    local host = IndexTable(module, key)
    if type(path) == "string" then
        return CallTable(host, path, ...)
    elseif type(path) == "table" then
        return CallTable(host, table.unpack(path), ...)
    end
end

local function callHost(self, path, ...)
    return GlobalFun.Try(callHostTarget, self, "LocalHost", path, ...)
end

local function callParent(self, path, ...)
    return GlobalFun.Try(callHostTarget, self, "Host", path, ...)
end

local function combineCacheKey(self, configKey)
    local module = self.module
    local uiWidget = IndexTable(module, ComposeTableRoute(routes.stagedConfig, "Partial", configKey, "Widget"))
    if uiWidget then
        return uiWidget
    end
    local widgetPath = IndexTable(module, ComposeTableRoute(routes.stagedConfig, "Partial", configKey, "WidgetPath"))
    local viewName = self:CallViewBase("viewName")
    return string.format("%s-%s", viewName, widgetPath)
end

local function saveLocalPartial(self, partialItem)
    local module = self.module
    local state, hostTransform = callHost(self, "transform")
    if not state or GlobalFun.IsNull(hostTransform) then
        return
    end
    local rootTran = GetOrGenerate(module, function()
        local cacheObject = GameObject("_")
        local cacheTran = cacheObject.transform
        GlobalFun.SetParent(cacheTran, hostTransform, false)
        UIKit.SetObjectState(cacheObject, false)
        return cacheTran
    end, ComposeTableRoute(routes.cacheRoot))
    if GlobalFun.IsNull(rootTran) then
        return
    end
    if not partialItem then
        return
    end
    local partialT = IndexTable(partialItem, "_partialType")
    local luaInst = IndexTable(partialItem, "LuaInst")
    local objectTran
    if partialT == partialType.localPrefab then
        objectTran = IndexTable(luaInst, "transform")
    elseif partialT == partialType.component then
        objectTran = IndexTable(luaInst, "tra")
    end
    if GlobalFun.IsNull(objectTran) then
        return
    end
    local configKey = IndexTable(partialItem, "_configKey")
    GlobalFun.SetParent(objectTran, rootTran, false)

    local uiWidget = combineCacheKey(self, configKey)
    local cacheTable = GetOrGenerate(module, {}, ComposeTableRoute(routes.cacheLocalPartial, uiWidget))
    table.insert(cacheTable, luaInst)
end

local function releaseAllSavedPartial(self)
    local module = self.module
    local savedPartial = IndexTable(module, ComposeTableRoute(routes.cacheLocalPartial))
    SetTableValue(module, nil, ComposeTableRoute(routes.cacheLocalPartial))
    if not savedPartial then
        return
    end
    for _, partials in pairs(savedPartial) do
        for _, partial in pairs(partials) do
            local partialT = IndexTable(partial, "ModuleInst", "PartialType")
            if partialT == partialType.component then
                UIManager:UnloadView(partial)
            end
        end
    end
end

local function getLocalPartialCache(self, partialT, configKey, root)
    local module = self.module
    local uiWidget = combineCacheKey(self, configKey)
    local cacheTable = GetOrGenerate(module, {}, ComposeTableRoute(routes.cacheLocalPartial, uiWidget))
    if not cacheTable then
        return
    end
    local luaInst = table.remove(cacheTable, 1)
    if not luaInst then
        return
    end
    if GlobalFun.IsNull(root) then
        return
    end
    local objectTran
    if partialT == partialType.localPrefab then
        objectTran = IndexTable(luaInst, "transform")
    elseif partialT == partialType.component then
        objectTran = IndexTable(luaInst, "tra")
    end
    if GlobalFun.IsNull(objectTran) then
        return
    end
    GlobalFun.SetParent(objectTran, root, false)
    objectTran:SetAsLastSibling()
    return luaInst
end

local generateChildPartialItem = function(widget, assetPath, widgetObject, single, rootTran, registerInst, onComp, partialT, ...)
    if not widget then
        return
    end
    if GlobalFun.IsNull(rootTran) then
        return
    end
    local planeTable = UIManager:loadUIDriver(widget)
    planeTable.args = { ... }
    local loadComp = function()
        GlobalFun.Try(registerInst, partialT, planeTable)
        UIManager:OpenView(planeTable)
        GlobalFun.Try(onComp, partialT, planeTable)
    end
    if GlobalFun.IsNull(widgetObject) then
        UIManager:LoadViewPrefab(assetPath, planeTable, rootTran, loadComp)
    else
        local objectInstance
        if not single then
            objectInstance = GlobalFun.Instantiate(widgetObject, rootTran)
        else
            GlobalFun.SetParent(widgetObject.transform, rootTran)
            objectInstance = widgetObject
        end
        UIManager:ExecuteLoadViewPrefab(objectInstance, planeTable, loadComp)
    end
end

local generateChildPartialObject = function(self, configKey, widgetPath, rootTran, ...)
    local module = self.module
    local transform = IndexTable(module, "transform")
    local widgetPrefab
    if type(widgetPath) == "function" then
        widgetPrefab = widgetPath()
    else
        widgetPrefab = GlobalFun.GetObj(transform, widgetPath)
    end
    if GlobalFun.IsNull(widgetPrefab) then
        error("cannot find widget path")
        return
    end
    local widgetObject = GlobalFun.Instantiate(widgetPrefab, rootTran)
    local widgetTransform = IndexTable(widgetObject, "transform")
    local status, compTab = self:CallHook("PostPartial", configKey, widgetTransform)
    if not status then
        return
    end
    local partialTab = {
        compTab = compTab,
        object = widgetObject,
        transform = widgetTransform,
    }
    return partialTab
end

local getLocalHost
getLocalHost = function(self, host, newHost)
    local module = self.module
    if not host then
        host = module
        newHost = IndexTable(module, "Host")
    end
    if not newHost then
        return host
    end
    host = newHost
    newHost = newHost.Host
    return getLocalHost(self, host, newHost)
end

local generatePartialItem = function(self, configKey, parent, registerInst, onComp, ...)
    local module = self.module
    local partialConfig = IndexTable(module, ComposeTableRoute(routes.stagedConfig, "Partial", configKey))
    if not configKey or not partialConfig then
        return
    end
    local root = parent or partialConfig.Root
    if not root then
        error("未指定根节点")
        return
    end
    local rootTran
    local rootType = type(root)
    if rootType == "function" then
        rootTran = root(...)
    elseif rootType == "string" then
        local transform = module.transform
        rootTran = GlobalFun.GetTra(transform, root)
    else
        rootTran = root
    end
    if GlobalFun.IsNull(rootTran) then
        return
    end
    local widgetPath = partialConfig.WidgetPath
    local assetPath = partialConfig.AssetPath
    local widget = partialConfig.Widget
    assetPath = assetPath or widget
    local partialT = widget and partialType.component or partialType.localPrefab
    local widgetTable = getLocalPartialCache(self, partialT, configKey, rootTran)
    if widget then
        if not widgetTable then
            local widgetObject
            if widgetPath then
                local transform = module.transform
                if type(widgetPath) == "function" then
                    widgetObject = widgetPath(transform)
                else
                    widgetObject = GlobalFun.GetObj(transform, widgetPath)
                end
            end
            local single = partialConfig.Single
            GlobalFun.Try(generateChildPartialItem, widget, assetPath, widgetObject, single, rootTran, registerInst, onComp, partialT, ...)
        else
            widgetTable.args = { ... }
            GlobalFun.Try(registerInst, partialT, widgetTable)
            UIManager:OpenView(widgetTable)
            GlobalFun.Try(onComp, partialT, widgetTable)
        end
    elseif widgetPath then
        if not widgetTable then
            widgetTable = generateChildPartialObject(self, configKey, widgetPath, rootTran)
        end
        GlobalFun.Try(registerInst, partialT, widgetTable)
        GlobalFun.Try(onComp, partialT, widgetTable)
    end
end

local function getHostAllInst(self, luaInst, collTab)
    local module = self.self
    -- get top host
    if not collTab then
        collTab = {}
        collTab[module] = true
    end
    if not luaInst then
        luaInst = IndexTable(module, "LocalHost")
        collTab[luaInst] = true
    end
    if not luaInst then
        return
    end
    local partialData = IndexTable(luaInst, table.unpack(routes.partialData))
    if partialData then
        for _, partialItem in pairs(partialData) do
            local partialT = IndexTable(partialItem, "_partialType")
            if partialT == partialType.component then
                local itemInst = IndexTable(partialItem, ComposeTableRoute(routes.moduleInst, "self"))
                if itemInst then
                    collTab[itemInst] = true
                    getHostAllInst(self, itemInst, collTab)
                end
            end
        end
    end
    return collTab
end

local function callPartial(self, instanceId, path, ...)
    local module = self.module
    local moduleInst = IndexTable(module, ComposeTableRoute(routes.partialData, instanceId, routes.moduleInst))
    local args = { ... }
    return GlobalFun.Try(function()
        if type(path) == "string" then
            return CallTable(moduleInst, path, table.unpack(args))
        elseif type(path) == "table" then
            return CallTable(moduleInst, table.unpack(path), table.unpack(args))
        end
    end)
end

local function callPartials(self, configKey, path, ...)
    if not configKey then
        return
    end
    local module = self.module
    local partialData = IndexTable(module, table.unpack(routes.partialData))
    if not partialData then
        return
    end
    for instanceId, partialItem in pairs(partialData) do
        local _configKey = IndexTable(partialItem, "_configKey")
        if _configKey == configKey then
            callPartial(self, instanceId, path, ...)
        end
    end
end

local function acquirePartialWithParent(self, configKey, parent, onComp, ...)
    if not configKey then
        return
    end

    local function registerInst(partialT, luaInst)
        if not luaInst then
            return
        end
        local module = self.module
        local instanceId = tostring(luaInst)
        SetTableValue(luaInst, module, "ModuleInst", "Host")
        SetTableValue(luaInst, instanceId, "ModuleInst", "InstanceId")
        SetTableValue(luaInst, partialT, "ModuleInst", "PartialType")
        local partialItem = {
            _type = configKey,
            _instanceId = instanceId,
            _partialType = partialT,
            _configKey = configKey
        }
        SetTableValue(partialItem, luaInst, "LuaInst")
        SetTableValue(module, partialItem, ComposeTableRoute(routes.partialData, instanceId))
    end

    local function onLoadComplete(partialT, luaInst)
        if not luaInst then
            GlobalFun.Try(onComp)
            return
        end
        local instanceId = tostring(luaInst)
        if partialT == partialType.component then
            GlobalFun.Try(onComp, instanceId)
        elseif partialT == partialType.localPrefab then
            local compTab = IndexTable(luaInst, "compTab")
            GlobalFun.Try(onComp, instanceId, compTab)
        end
    end

    GlobalFun.Try(generatePartialItem, self, configKey, parent, registerInst, onLoadComplete, ...)
end

local function acquirePartial(self, configKey, onComp, ...)
    acquirePartialWithParent(self, configKey, nil, onComp, ...)
end

local function releasePartial(self, instanceId)
    local module = self.module
    if not instanceId then
        return
    end
    local partialItem = IndexTable(module, ComposeTableRoute(routes.partialData, instanceId))
    SetTableValue(module, nil, ComposeTableRoute(routes.partialData, instanceId))
    local partialT = IndexTable(partialItem, "_partialType")
    local luaInst = IndexTable(partialItem, "LuaInst")
    if partialT == partialType.component then
        SetTableValue(luaInst, true, "BlockDestroy")
        GlobalFun.Try(UIManager.CloseView, UIManager, luaInst, true)
    end
    saveLocalPartial(self, partialItem)
end

local function releasePartials(self, configKey)
    if not configKey then
        return
    end
    local module = self.module
    local partialData = IndexTable(module, table.unpack(routes.partialData))
    if not partialData then
        return
    end
    for instanceId, partialItem in pairs(partialData) do
        local configType = IndexTable(partialItem, "_type")
        if configType == configKey then
            releasePartial(self, instanceId)
        end
    end
end

local function getPartials(self, configKey)
    if not configKey then
        return
    end
    local module = self.module
    local partialData = IndexTable(module, table.unpack(routes.partialData))
    if not partialData then
        return
    end
    local ret
    for instanceId, partialItem in pairs(partialData) do
        local configType = IndexTable(partialItem, "_type")
        if configType == configKey then
            ret = ret or {}
            local luaInst = IndexTable(partialItem, "LuaInst")
            ret[instanceId] = luaInst
        end
    end
    return ret
end

local function releaseAllPartials(self)
    local module = self.module
    local partialData = IndexTable(module, table.unpack(routes.partialData))
    if not partialData then
        return
    end
    local waitRelease = {}
    for instanceId, _ in pairs(partialData) do
        table.insert(waitRelease, instanceId)
    end
    for _, instId in pairs(waitRelease) do
        releasePartial(self, instId)
    end
end

function UIExtension:Partial_CatchIndex(key)
    if key == "LocalHost" then
        local core = self.core
        local value = getLocalHost(self)
        rawset(core, key, value)
        return value
    end
end

function UIExtension:Partial_CatchNewIndex(key, value)
    if key == "Partial" then
        return self:WrapHookConfig(key, value)
    end
end

function UIExtension:Partial_ModuleStatusChanged(status)
    if status == UIStatus.BeforeExit then
        releaseAllPartials(self)
    elseif status == UIStatus.Destroy then
        releaseAllSavedPartial(self)
    end
end

function UIExtension:Partial_Initialize()
    self:ExposeAPI("CallHost", function(path, ...)
        return callHost(self, path, ...)
    end)

    self:ExposeAPI("CallParent", function(path, ...)
        return callParent(self, path, ...)
    end)

    self:ExposeAPI("CallPartials", function(configKey, path, ...)
        callPartials(self, configKey, path, ...)
    end)

    self:ExposeAPI("CallPartial", function(instanceId, path, ...)
        return callPartial(self, instanceId, path, ...)
    end)

    self:ExposeAPI("AcquirePartialWithParent", function(configKey, parent, onComp, ...)
        return acquirePartialWithParent(self, configKey, parent, onComp, ...)
    end)

    self:ExposeAPI("AcquirePartial", function(configKey, onComp, ...)
        return acquirePartial(self, configKey, onComp, ...)
    end)

    self:ExposeAPI("ReleasePartial", function(instanceId)
        releasePartial(self, instanceId)
    end)

    self:ExposeAPI("ReleasePartials", function(configKey)
        releasePartials(self, configKey)
    end)

    self:ExposeAPI("ReleaseAllPartials", function()
        releaseAllPartials(self)
    end)

    self:ExposeAPI("GetPartials", function(configKey)
        return getPartials(self, configKey)
    end)
end

function UIExtension:GetHostAllInst(luaInst, collTab)
    return getHostAllInst(self, luaInst, collTab)
end