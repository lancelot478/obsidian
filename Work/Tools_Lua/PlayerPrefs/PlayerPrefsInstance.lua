local _ENV = Boli2Env

require "Tools/PlayerPrefs/PlayerPrefsInternal"

PlayerPrefsInstance = class("PlayerPrefsInstance")

function PlayerPrefsInstance:ctor(moduleName, user)
    moduleName = moduleName or ""
    if moduleName == "" then
        error("PlayerPrefs模块名为空")
        return
    end
    self.user = user == true
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
        end
        moduleKey = string.format("PlayerPrefs_INSTANCE_%s_%s", moduleName, tostring(roleId))
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
