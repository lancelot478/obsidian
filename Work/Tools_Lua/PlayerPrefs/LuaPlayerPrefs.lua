local _ENV = Boli2Env

LuaPlayerPrefs = {}

local playerPrefs_KEYMAP = "COMMON_PREFS"

require "Tools/PlayerPrefs/PlayerPrefsInstance"
local playerPrefsInstance = PlayerPrefsInstance.new(playerPrefs_KEYMAP, true)

function LuaPlayerPrefs.SetString(key, value)
    playerPrefsInstance:Set(key, value)
end

function LuaPlayerPrefs.SetInt(key, value)
    playerPrefsInstance:Set(key, value)
end

function LuaPlayerPrefs.SetBool(key, value)
    playerPrefsInstance:Set(key, value)
end

function LuaPlayerPrefs.GetString(key, defaultValue)
    return playerPrefsInstance:GetString(key, defaultValue)
end

function LuaPlayerPrefs.GetInt(key, defaultValue)
    return playerPrefsInstance:GetInt(key, defaultValue)
end

function LuaPlayerPrefs.GetBool(key, defaultValue)
    return playerPrefsInstance:GetBool(key, defaultValue)
end

function LuaPlayerPrefs.DeleteKey(key)
    playerPrefsInstance:DeleteKey(key)
end

function LuaPlayerPrefs.DeleteAll()
    playerPrefsInstance:DeleteAll()
end

function LuaPlayerPrefs.HasKey(key)
    return playerPrefsInstance:HasKey(key)
end

--保存数据
function LuaPlayerPrefs.Save()
    playerPrefsInstance:Save()
end

-------------------------------------

function LuaPlayerPrefs.HasSuspendKey()
    return PlayerPrefs.HasKey(GlobalVariable.SuspendKey)
end

function LuaPlayerPrefs.ClearSuspendData()
    PlayerPrefs.DeleteKey(GlobalVariable.SuspendKey)
end

local suspendVal="1"
function LuaPlayerPrefs.SaveSuspendData()
    PlayerPrefs.SetInt(GlobalVariable.SuspendRoleSlotKey,GameData.GetRoleSlot())
    PlayerPrefs.SetString(GlobalVariable.SuspendKey,suspendVal)
    PlayerPrefs.SetString(GlobalVariable.SuspendUserIDKey,BattleData.GetUserId())
    PlayerPrefs.SetInt(GlobalVariable.SuspendServerID,GlobalNet.GetServerID())
    PlayerPrefs.SetString(GlobalVariable.SuspendZoneID,LoginGameNetData.GetCurrSelectServerZoneID())
end