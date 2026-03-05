local _ENV = Boli2Env
---@class InputDragController
InputDragController = class("InputDragController")

function InputDragController:ctor(dragXAction, dragYAction, cancelDragXAction, customIgnoreCondCheckFunc)
    self.isPaused = false

    self.dragDeltaXAction = dragXAction

    self.dragDeltaYAction = dragYAction
    self.cancelDragXAction = cancelDragXAction
    --默认的自定义忽略拖拽事件函数
    if customIgnoreCondCheckFunc == nil then
        customIgnoreCondCheckFunc = function()
            return EventSystem.current ~= nil and not BasePlane.PointerOnAnyUI()
        end
    end
    self.customIgnoreCondCheckFunc = customIgnoreCondCheckFunc
end

function InputDragController:Update()
    if self.isPaused then
        return
    end
    self:HandleInput()
end

function InputDragController:Pause()
    self.isPaused = true
end
function InputDragController:Resume()
    self.isPaused = false
end
function InputDragController:HandleInput()
    if self.isPaused then
        return
    end
    if isEditor then
        self:HandleMouseInput()
    else
        self:HandleTouchInput()
    end

end
function InputDragController:HandleMouseInput()
    if self.customIgnoreCondCheckFunc() then
        if Input.GetMouseButton(0) then
            local mouseX = Input.GetAxis("Mouse X")
            if self.dragDeltaXAction then
                self.dragDeltaXAction(mouseX, true)
            end
        end
    end
    if not Input.GetMouseButton(0) then
        if self.cancelDragXAction then
            self.cancelDragXAction(true)
        end
    end
end
function InputDragController:HandleTouchInput()
    if self.customIgnoreCondCheckFunc() then
        if Input.touchCount > 0 then
            local touch = Input.GetTouch(0)
            -- move
            if Input.touchCount == 1 and touch.phase == TouchPhase.Moved then
                local deltaPositionX = touch.deltaPosition.normalized.x
                if self.dragDeltaXAction then
                    self.dragDeltaXAction(deltaPositionX, false)
                end
            end
        end
        if Input.touchCount == 0 then
            if self.cancelDragXAction then
                self.cancelDragXAction(false)
            end
        end
    end
end


