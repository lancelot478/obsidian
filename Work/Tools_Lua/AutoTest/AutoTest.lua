local _ENV = Boli2Env

local unpack = unpack or table.unpack
require "Tools/AutoTest/AutoTestFacade"

AutoTestInstance = class("AutoTestInstance")

local impl = AutoTest.eventImpl

function AutoTestInstance:PostEvent(eventType, ...)
    local switch = AutoTest.CheckEnable()
    if not switch then
        return
    end
    local eventImpl = IndexTable(impl, eventType)
    local args = { ... }
    TimeMgr.AddTask(function()
        GlobalFun.Try(eventImpl, unpack(args))
    end, 1)
end

function AutoTestInstance:RegisterFunc(funcType, func, ...)
    local args = { ... }
    SetTableValue(self, function()
        GlobalFun.Try(func, unpack(args))
    end, "ImplList", funcType)
end

function AutoTestInstance:CallImpl(funcType, ...)
    local func = IndexTable(self, "ImplList", funcType)
    if not func then
        return
    end
    GlobalFun.Try(func, ...)
end