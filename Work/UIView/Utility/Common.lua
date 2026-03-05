local _ENV = Boli2Env

local unpack = unpack or table.unpack
local setfenv = setfenv
if not setfenv then
    local function findenv(f)
        local level = 1
        repeat
            local name, value = debug.getupvalue(f, level)
            if name == '_ENV' then
                return level, value
            end
            level = level + 1
        until name == nil
        return nil
    end
    setfenv = function(f, t)
        local level = findenv(f)
        if level then
            debug.setupvalue(f, level, t)
        end
        return f
    end
end

function reimport(name)
    local package = package
    package.loaded[name] = nil
    package.preload[name] = nil
    return require(name)
end

function c_require(moduleName)
    local status = GlobalFun.Try(reimport, moduleName)
    return status
end

function CacheModuleMember(module, memberName, generate)
    if not module or memberName == nil then
        return
    end
    local member = module[memberName]
    if member == nil then
        if generate == nil then
            member = { }
        elseif type(generate) == "function" then
            member = generate()
        end
        module[memberName] = member
    end
    return member
end

function IndexTable(table, key, ...)
    if table == nil then
        return
    end
    if key == nil then
        return table
    end
    local item = table[key]
    return IndexTable(item, ...)
end

local _composeTableRoute
_composeTableRoute = function(acc, now, ...)
    if not acc then
        return
    end
    if now == nil then
        return unpack(acc)
    end
    if type(now) == "table" then
        for _, v in pairs(now) do
            table.insert(acc, v)
        end
    else
        table.insert(acc, now)
    end

    return _composeTableRoute(acc, ...)
end

local composeAcc = {}
function ComposeTableRoute(...)
    for key, _ in pairs(composeAcc) do
        composeAcc[key] = nil
    end
    return _composeTableRoute(composeAcc, ...)
end

function SetTableValue(table, value, key, nextKey, ...)
    if not table then
        return
    end
    if not key then
        return table
    end
    if type(table) ~= "table" then
        error("table path inspect invalid data")
    end
    local item = table[key]
    if item == nil or not nextKey then
        local preSet = nextKey and {} or value
        item = preSet
        table[key] = item
    end
    return SetTableValue(item, value, nextKey, ...)
end

function GetOrSet(table, defaultValue, key, nextKey, ...)
    if not table then
        return
    end
    if key == nil then
        return table
    end
    if type(table) ~= "table" then
        error("table path inspect invalid data")
    end
    local item = table[key]
    if item == nil then
        local preSet = nextKey and {} or defaultValue
        item = preSet
        table[key] = item
    end
    return GetOrSet(item, defaultValue, nextKey, ...)
end

function GetOrGenerate(table, defaultValue, key, nextKey, ...)
    if not table then
        return
    end
    if key == nil then
        return table
    end
    if type(table) ~= "table" then
        error("table path inspect invalid data")
    end
    local item = table[key]
    if item == nil then
        local preSet = nextKey and {} or (type(defaultValue) == "function" and defaultValue() or defaultValue)
        item = preSet
        table[key] = item
    end
    return GetOrGenerate(item, defaultValue, nextKey, ...)
end

function GetOrSetTable(table, ...)
    return GetOrSet(table, {}, ...)
end

function CallTable(tab, memberName, ...)
    local member = IndexTable(tab, memberName)
    if type(member) == "function" then
        return member(...)
    else
        return member
    end
end

function CheckParam(...)
    local args = { ... }
    for _, arg in pairs(args) do
        if not arg then
            return false
        end
    end
    return true
end

local packageLoad = {}
function LoadRestrictedModule(moduleName, meta, cache)
    if not moduleName then
        return
    end
    meta = meta or { __index = _ENV }
    local modulePath = string.gsub(moduleName, "%.", "/")
    local moduleFunc = packageLoad[moduleName] or loadfile(modulePath)
    if not moduleFunc then
        return
    end
    local moduleInst = {}
    setmetatable(moduleInst, meta)
    setfenv(moduleFunc, moduleInst)()
    if cache then
        packageLoad[moduleName] = moduleFunc
    end
    return moduleInst
end