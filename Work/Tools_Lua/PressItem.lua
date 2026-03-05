local _ENV = Boli2Env

PressItem = class("PressItem")

local PRESS_TIME = 0.3

function PressItem:ctor(tra, clickFun, pressFun)
    self.tra = tra
    self.clickFun = clickFun
    self.pressFun = pressFun
    self.csUGUIEventsEntry = self.tra.gameObject:AddComponent(CS2LuaUGUIEventsEntry)
    self.csUGUIEventsEntry:SetLuaTable(self)
end

function PressItem:CS_OnPointerDown(eventData)
    self.btnDown = true
    self.btnDownTime = Time.realtimeSinceStartup
end

function PressItem:CS_OnPointerUp(eventData)
    self.btnDown = false
    if (Time.realtimeSinceStartup - self.btnDownTime) <= PRESS_TIME then
        if self.clickFun then
            self.clickFun()
        end
    end
end

function PressItem:Update()
    if self.btnDown and (Time.realtimeSinceStartup - self.btnDownTime) > PRESS_TIME then
        self.btnDown = false
        if self.pressFun then
            self.pressFun()
        end
    end
end

