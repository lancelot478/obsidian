local _ENV = Boli2Env

DragItem = class("DragItem")

function DragItem:ctor(tra, clickFun, dragEndFun, xMin, xMax, yMin, yMax)
    self.tra = tra
    self.clickFun = clickFun
    self.dragEndFun = dragEndFun
    self.xMin = xMin
    self.xMax = xMax
    self.yMin = yMin
    self.yMax = yMax
    self.offsetX, self.offsetY = 0, 0
    _, _, self.posZ = Transform.GetPosition(self.tra)

    local csUGUIEventsEntry = self.tra.gameObject:AddComponent(CS2LuaUGUIEventsEntry)
    csUGUIEventsEntry:SetLuaTable(self)
    self:SwitchDrag(true)
end

function DragItem:CS_OnDrag(eventData)
    if not self.canDrag then
        return
    end
    local worldPos = GlobalFun.GetWorldPos(Input.mousePosition)
    if self.xMin == nil or self.xMax == nil or self.yMin == nil or self.yMax == nil then
        Transform.SetPosition(self.tra, worldPos.x + self.offsetX, worldPos.y + self.offsetY, self.posZ)
    else
        local x = GlobalFun.Clamp(worldPos.x + self.offsetX, self.xMin, self.xMax)
        local y = GlobalFun.Clamp(worldPos.y + self.offsetY, self.yMin, self.yMax)
        Transform.SetPosition(self.tra, x, y, self.posZ)
    end
end

function DragItem:CS_OnBeginDrag(eventData)
    if not self.canDrag then
        return
    end
    self.isDraging = true
    BattleCameraInput.SetDragState(true)

    local worldPos = GlobalFun.GetWorldPos(Input.mousePosition)
    local x, y = Transform.GetPosition(self.tra)
    self.offsetX, self.offsetY = worldPos.x - x, worldPos.y - y
end

function DragItem:CS_OnEndDrag(eventData)
    if not self.canDrag then
        return
    end
    self.isDraging = false
    if self.dragEndFun then
        self.dragEndFun()
    end
    BattleCameraInput.SetDragState(false)
end

function DragItem:CS_OnPointerClick(eventData)
    if self.isDraging then
        return
    end
    if self.clickFun then
        self.clickFun()
    end
end

function DragItem:CS_OnPointerEnter(eventData)
    self.isEnter = true
end

function DragItem:CS_OnPointerExit(eventData)
    self.isEnter = false
end

function DragItem:IsDraging()
    return self.isDraging
end

--拖动开关
function DragItem:SwitchDrag(canDrag)
    if self.canDrag ~= canDrag then
        self.canDrag = canDrag
    end
end

