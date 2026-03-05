local _ENV = Boli2Env

local playerPrefs_KEYMAP = "AutoTest"
local playerPrefsInstance = PlayerPrefsInstance.new(playerPrefs_KEYMAP)

local autoTestStateSwitchKey = "autoTestStateSwitchKey"

local autoTestType = require("Tools/AutoTest/AutoTestType")
local impl = require("Tools/AutoTest/AutoTestImpl")

AutoTest = {
    funcType = autoTestType.autoTestFuncType,
    eventType = autoTestType.autoTestEvent,
    eventImpl = impl.onEvent,
}


local autoTestInst
local function getAutoTestInst()
    if not autoTestInst then
        autoTestInst = AutoTestInstance.new()
    end
    return autoTestInst
end

function AutoTest.PostEvent(eventType, ...)
    local testInst = getAutoTestInst()
    testInst:PostEvent(eventType, ...)
end

function AutoTest.RegisterFunc(funcType,func, ...)
    local testInst = getAutoTestInst()
    testInst:RegisterFunc(funcType,func, ...)
end

function AutoTest.CallImpl(funcType, ...)
    local testInst = getAutoTestInst()
    testInst:CallImpl(funcType, ...)
end

function AutoTest.SetEnable(state)
    state = state == true
    playerPrefsInstance:SetBool(autoTestStateSwitchKey, state)
    playerPrefsInstance:Save()
end

function AutoTest.CheckEnable()
    local state = playerPrefsInstance:GetBool(autoTestStateSwitchKey, false)
    return state
end


