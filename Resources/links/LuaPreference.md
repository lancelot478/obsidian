#Muffin

LuaPrefsInstance

```lua
local _ENV = Boli2Env  
  
require "Tools/PlayerPrefs/PlayerPrefsInternal"  
  
PlayerPrefsInstance = class("PlayerPrefsInstance")  
  
function PlayerPrefsInstance:ctor(moduleName, user)  
    moduleName = moduleName or ""  
    if moduleName == "" then  
        error("PlayerPrefs模块名为空")  
        return  
    end    self.user = user == true  
    self.moduleName = moduleName  
end  
  
function PlayerPrefsInstance:GetModuleKey()  
    local moduleKey = self.moduleKey  
    if moduleKey then  
        return moduleKey  
    end  
    local moduleName = self.moduleName  
    local user = self.user  
    if user then  
        local roleId = GameData.GetRoleId()  
        if not roleId then  
            error(string.format("PlayerPrefs error, access role id before login: %s", moduleName))  
            return  
        end        moduleKey = string.format("PlayerPrefs_INSTANCE_%s_%s", moduleName, tostring(roleId))  
    else  
        moduleKey = string.format("PlayerPrefs_INSTANCE_%s", moduleName)  
    end  
    self.moduleKey = moduleKey  
    return moduleKey  
end  
  
function PlayerPrefsInstance:Set(key, value)  
    local moduleKey = self:GetModuleKey()  
    PlayerPrefsInternal.SetModuleValue(moduleKey, key, value)  
end  
  
function PlayerPrefsInstance:Get(key, defaultValue)  
    local moduleKey = self:GetModuleKey()  
    return PlayerPrefsInternal.GetModuleValue(moduleKey, key, defaultValue)  
end  
  
function PlayerPrefsInstance:GetString(key, defaultValue)  
    local moduleKey = self:GetModuleKey()  
    defaultValue = defaultValue or ""  
    return PlayerPrefsInternal.GetModuleValue(moduleKey, key, defaultValue)  
end  
  
function PlayerPrefsInstance:GetInt(key, defaultValue)  
    local moduleKey = self:GetModuleKey()  
    defaultValue = defaultValue or 0  
    return PlayerPrefsInternal.GetModuleValue(moduleKey, key, defaultValue)  
end  
  
function PlayerPrefsInstance:GetBool(key, defaultValue)  
    local moduleKey = self:GetModuleKey()  
    defaultValue = defaultValue or false  
    return PlayerPrefsInternal.GetModuleValue(moduleKey, key, defaultValue)  
end  
  
function PlayerPrefsInstance:DeleteKey(key)  
    local moduleKey = self:GetModuleKey()  
    PlayerPrefsInternal.SetModuleValue(moduleKey, key, nil)  
end  
  
function PlayerPrefsInstance:DeleteAll()  
    local moduleKey = self:GetModuleKey()  
    PlayerPrefsInternal.DeleteModule(moduleKey)  
end  
  
function PlayerPrefsInstance:HasKey(key)  
    local moduleKey = self:GetModuleKey()  
    return PlayerPrefsInternal.ModuleHasKey(moduleKey, key)  
end  
  
--保存数据  
function PlayerPrefsInstance:Save()  
    local moduleKey = self:GetModuleKey()  
    PlayerPrefsInternal.SaveModule(moduleKey)  
end  
  
function PlayerPrefsInstance.SaveAll()  
    PlayerPrefsInternal.SaveAll()  
end
```

PlayPrefsInternal

```lua
local _ENV = Boli2Env  
  
--local serpent = require "Lib/pb/serpent"  
local saveData, dumpModule  
  
local rawData = setmetatable({}, {  
    __index = function(tab, key)  
        if not key then  
            return        end        local data = json.decode(PlayerPrefs.GetString(key, "{}"), 1, nil)  
        rawset(tab, key, data)  
        return data  
    end  
})  
  
local cacheKeyMap = {}  
local function getModuleKeyMap(moduleKey)  
    local key = cacheKeyMap[moduleKey]  
    if not key then  
        key = string.format("%s_keymap", moduleKey)  
        cacheKeyMap[moduleKey] = key  
    end  
    return key  
end  
  
local keyMapContainer = setmetatable({}, {  
    __index = function(container, moduleKey)  
        local dataTab = setmetatable({}, {  
            __index = function(self, key)  
                if not key then  
                    return                end                local keyMapKey = getModuleKeyMap(moduleKey)  
                local rawKeyMapData = rawData[keyMapKey]  
                local value = rawKeyMapData[key]  
                if not value then  
                    value = (rawKeyMapData.l or 0) + 1  
                    rawKeyMapData.l = value  
                    rawKeyMapData[key] = value  
                    rawset(self, "dirty", true)  
                end  
                return tostring(value)  
            end,  
            __call = function(self)  
                saveData(self, moduleKey, true)  
            end  
        })  
        rawset(container, moduleKey, dataTab)  
        return dataTab  
    end  
})  
  
local function getRawData(moduleKey, key)  
    local data = rawData[moduleKey]  
    local keyMap = keyMapContainer[moduleKey]  
    local indexKey = keyMap[key]  
    return data[indexKey]  
end  
  
local dataContainer = setmetatable({}, {  
    __index = function(container, moduleKey)  
        local dataTab = setmetatable({}, {  
            __index = function(_, key)  
                return getRawData(moduleKey, key)  
            end,  
            __newindex = function(self, key, value)  
                local keyMap = keyMapContainer[moduleKey]  
                local indexKey = keyMap[key]  
                local dataTab = rawData[moduleKey]  
                local oldValue = dataTab[indexKey]  
                if oldValue == value then  
                    return                end                dataTab[indexKey] = value  
                rawset(self, "dirty", true)  
            end,  
            __call = function(self)  
                saveData(self, moduleKey)  
            end  
        })  
        rawset(container, moduleKey, dataTab)  
        return dataTab  
    end  
})  
  
local function getModuleValue(moduleName, key, defaultValue)  
    local dataTab = dataContainer[moduleName]  
    local value = dataTab[key]  
    if value == nil then  
        return defaultValue  
    else  
        return value  
    end  
end  
  
local function setModuleValue(moduleName, key, value)  
    local dataTab = dataContainer[moduleName]  
    dataTab[key] = value  
end  
  
local function deleteAll()  
    for key, _ in pairs(dataContainer) do  
        dataContainer[key] = nil  
    end  
end  
  
local function deleteModule(moduleKey)  
    dataContainer[moduleKey] = nil  
end  
  
local function hasKey(moduleKey, key)  
    return getModuleValue(moduleKey, key) ~= nil  
end  
  
local function saveModule(moduleKey)  
    local keyMap = keyMapContainer[moduleKey]  
    keyMap()  
    local data = dataContainer[moduleKey]  
    data()  
end  
  
dumpModule = function(moduleKey)  
    local keyMapKey = getModuleKeyMap(moduleKey)  
    local keyMap = rawData[keyMapKey]  
    local moduleRaw = dataContainer[moduleKey]  
    local showTab = {}  
    for strKey, _ in pairs(keyMap) do  
        showTab[strKey] = moduleRaw[strKey]  
    end  
    print("===========> dump", moduleKey, json.encode(showTab))  
end  
  
saveData = function(wrapper, moduleKey, isKeyMap)  
    local dirty = rawget(wrapper, "dirty")  
    if not dirty then  
        return    end  
    local writeKey = isKeyMap and getModuleKeyMap(moduleKey) or moduleKey  
    local raw = rawData[writeKey]  
    local tabStr = json.encode(raw)  
    --print("===========> saved ", tabStr)  
    PlayerPrefs.SetString(writeKey, tabStr)  
    rawset(wrapper, "dirty", nil)  
  
    --if not isKeyMap then  
    --    dumpModule(moduleKey)    --endend  
  
local function saveAll()  
  
end  
  
PlayerPrefsInternal = {  
    SetModuleValue = setModuleValue,  
    GetModuleValue = getModuleValue,  
    DeleteModule = deleteModule,  
    ModuleHasKey = hasKey,  
    SaveModule = saveModule,  
    DeleteAll = deleteAll,  
    SaveAll = saveAll,  
}
```
