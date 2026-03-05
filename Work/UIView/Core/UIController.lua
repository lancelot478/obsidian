local _ENV = Boli2Env

require("Tools/UIView/Core/UIDriver")

require("Tools/UIView/Extension/UIExtension")
require("Tools/UIView/Extension/UIExtensionEvent")
require("Tools/UIView/Extension/UIExtensionViewInterface")
require("Tools/UIView/Extension/UIExtensionComponent")
require("Tools/UIView/Extension/UIExtensionMemo")
require("Tools/UIView/Extension/UIExtensionPartial")

if not UIController then
    UIController = {
        DebugState = false
    }
end

function UIController.Debug(...)
    local state = UIController.DebugState
    if not state then
        return
    end
    print(...)
end

local executeLoadUI = function(uiModuleName, modulePath)

    local newIndexFunc, indexFunc
    local uiCore, uiModule, uiMediator

    uiCore = LoadRestrictedModule("Tools/UIView/Core/UICore", { __index = function(tab, key)
        if key == "core" then
            return uiCore
        elseif key == "self" then
            return uiModule
        end
        local val = rawget(tab, key)
        if not val then
            val = (_ENV or _G)[key]
        end
        if not val and indexFunc then
            val = indexFunc(key)
        end
        return val
    end, __newindex = function(...)
        if newIndexFunc then
            newIndexFunc(...)
        else
            rawset(...)
        end
    end, __tostring = function()
        return string.format("%s.core", uiModuleName)
    end })
    if not uiCore then
        return
    end
    indexFunc = uiCore.OnIndex
    newIndexFunc = uiCore.OnNewIndex
    --rawset(uiCore, "core", uiCore)
    local extensionInst = UIExtension.new(uiCore)
    rawset(uiCore, "extension", extensionInst)
    CallTable(uiCore, "SetStatus", UIStatus.ExtensionInitialized)

    uiMediator = LoadRestrictedModule("Tools/UIView/Core/UICoreMediator", {
        __index = function(_, key)
            if key == "self" then
                return uiModule
            end
            return (_ENV or _G)[key]
        end
    }, true)
    if not uiMediator or not uiCore then
        return
    end
    uiModule = LoadRestrictedModule(modulePath, { __index = uiCore, __newindex = uiCore, __tostring = function()
        return string.format("%s.module", uiModuleName)
    end })
    if not uiModule then
        return
    end
    extensionInst:SetModule(uiModule)
    --rawset(uiCore, "self", uiModule)
    rawset(uiMediator, "self", uiModule)
    local uiInst = setmetatable({}, { __index = uiMediator })
    CallTable(uiModule, "SetStatus", UIStatus.ModuleInitialized)
    UIController.Debug(uiModule, uiCore)
    return uiInst
end

function UIController.LoadUI(uiModuleName)
    local _, uiInst = GlobalFun.Try(function()
        local modulePath = string.format("Views/%s", uiModuleName)
        return executeLoadUI(uiModuleName, modulePath)
    end)
    return uiInst
end