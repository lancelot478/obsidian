local _ENV = Boli2Env

local routes = {
    componentData = { "Container", "ComponentData" },
    componentConfig = { "Container", "ComponentConfig" },
}

local registerLuaCallback = function(self, comp, componentName, func)
    if GlobalFun.IsNull(comp) then
        return
    end
    if not func then
        return
    end
    local callFunc
    if type(func) == "function" then
        callFunc = func
    elseif type(func) == "string" then
        callFunc = IndexTable(self, func)
    end
    if type(callFunc) ~= "function" then
        error("cannot bind target event, func is invalid.", func)
        return
    end
    if componentName == "Button" or componentName == "GameObject" then
        GlobalFun.SetBtnFun(comp, callFunc)
    elseif componentName == "Slider" or componentName == "InputField" or componentName == "Toggle" or componentName == "Scrollbar" then
        GlobalFun.BindChangeEvent(comp, callFunc)
    end
end

local getChildComponent = function(self, path, componentName, callback, lang, initFunc)
    local component = nil
    if not component and path then
        local transform = IndexTable(self, "transform")
        if not componentName or componentName == "GameObject" then
            component = GlobalFun.GetObj(transform, path)
        else
            if componentName == "Text" then
                component = GlobalFun.GetTMP(transform, path)
            elseif componentName == "InputField" then
                component = GlobalFun.GetType(transform, path, "TMPro.TMP_InputField")
            elseif componentName == "Slider" then
                component = GlobalFun.GetSli(transform, path)
            end
            if not component then
                component = GlobalFun.GetType(transform, path, componentName)
            end
        end
        local text
        if type(lang) == "string" then
            text = GlobalText.GetText(lang) or lang
        elseif type(lang) == "function" then
            text = lang()
        end
        if text then
            UIKit.SetText(component, text or "")
        end
    end
    registerLuaCallback(self, component, componentName, callback)
    if not component then
        local transform = IndexTable(self, "transform")
        error("warning: cannot get component: ", transform.name, path, componentName)
    else
        GlobalFun.Try(initFunc, component)
    end
    return component
end

local generateComponentData
generateComponentData = function(self, componentConfig)
    if not componentConfig then
        return
    end
    local configType = type(componentConfig)
    if configType == "table" then
        -- parse param
        local path = IndexTable(componentConfig, "Path")
        local component = IndexTable(componentConfig, "Component")
        local callback = IndexTable(componentConfig, "Callback")
        local lang = IndexTable(componentConfig, "Lang")
        local initFunc = IndexTable(componentConfig, "Init")

        local singleConfig = path and (component or callback or true)
        if singleConfig then
            return getChildComponent(self, path, component, callback, lang,initFunc)
        else
            local table = {}
            for configKey, value in pairs(componentConfig) do
                table[configKey] = generateComponentData(self, value)
            end
            return table
        end
    elseif configType == "function" then
        local state, comp = GlobalFun.Try(componentConfig)
        if state then
            return comp
        end
    else
        error("无法解析具体类型", configType, componentConfig)
    end
end

local initializeComponent = function(self)
    local componentConfig = IndexTable(self, table.unpack(routes.componentConfig))
    if not componentConfig then
        return
    end
    local componentData = generateComponentData(self, componentConfig)
    SetTableValue(self, componentData, table.unpack(routes.componentData))
end

local injectComponentData = function(core, componentConfig)
    local componentInterface = rawget(core, "Component")
    if componentInterface then
        error("cannot reset component definition")
    end

    SetTableValue(core, componentConfig, table.unpack(routes.componentConfig))

    -- create interface
    componentInterface = {}
    local compMeta = {
        __index = function(_, key)
            local currentStatus = CallTable(core, "GetStatus")
            local moduleInitialized = currentStatus >= UIStatus.UIInitialized
            if not moduleInitialized then
                error("cannot index component data before ui initialize complete")
            end
            local value = IndexTable(core, ComposeTableRoute(routes.componentData, key))
            return value
        end,
        __newindex = function()
            error("cannot set component data!")
        end
    }
    setmetatable(componentInterface, compMeta)
    rawset(core, "Component", componentInterface)
end

function UIExtension:Component_ModuleStatusChanged(status)
    if status == UIStatus.UIInitialized then
        local module = self.module
        initializeComponent(module)
    end
end

function UIExtension:Component_CatchNewIndex(key, value)
    if key == "Component" then
        return GlobalFun.Try(function()
            local core = self.core
            injectComponentData(core, value)
        end)
    end
end