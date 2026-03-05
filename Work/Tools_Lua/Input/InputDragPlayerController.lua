local _ENV = Boli2Env
---@class InputDragPlayerController:InputDragController
---@field super InputDragController
InputDragPlayerController = class("InputDragController", InputDragController)

--鼠标拖拽旋转速率倍率
local mouseXAcc = 6
--触摸拖拽旋转速率倍率
local touchXAcc = 12

function InputDragPlayerController:ctor(tra, customIgnoreCondCheckFunc)
    self.currResetTime = 0
    self.resetStartTime = 0
    --几秒回归
    self.resetTotalTime = 3
    self.customIgnoreCondCheckFunc = customIgnoreCondCheckFunc or nil
    self.tra = tra
    self.super.ctor(self, function(deltaX, isMouse)
        self:HandleTouchInputX(deltaX, isMouse)
    end, nil, function(isMouse)
        self:CancelTouchInputX(isMouse)
    end, self.customIgnoreCondCheckFunc)
end

function InputDragPlayerController:Update()
    if self.tra then
        self.super.Update(self)
    end
end

function InputDragPlayerController:SetTra(tra)
    self.tra = tra
end

function InputDragPlayerController:HandleTouchInputX(deltaX, isMouse)
    local tra = self.tra
    if tra then
        if isMouse then
            TransformExtension.SetLocalEulerAngles(tra.transform,0, tra.transform.localEulerAngles.y - deltaX * mouseXAcc, 0)
        else
            TransformExtension.SetLocalEulerAngles(tra.transform,0, tra.transform.localEulerAngles.y - deltaX * touchXAcc, 0)
        end
    end
    self.resetStartTime = GameMain:GetTime() + self.resetTotalTime
end

function InputDragPlayerController:CancelTouchInputX(isMouse)
    if isMouse and self.resetStartTime == 0 then
        return
    end
    if GameMain:GetTime() > self.resetStartTime then
        self.currResetTime = self.currResetTime + GameMain:GetDeltaTime()
        local tra = self.tra
        local y = tra.transform.localEulerAngles.y
        if y > 180 then
            y = y - 360
        end
        if tra then
            local x,y,z=GlobalFun.GetLerpXYZXYZ(tra.transform.localEulerAngles.x, y, tra.transform.localEulerAngles.z,0,0,0, 0.1)
            TransformExtension.SetLocalEulerAngles(tra.transform,x,y,z)
        end
        if self.currResetTime >= self.resetTotalTime then
            TransformExtension.SetLocalEulerAngles(tra.transform,0, 0, 0)
            self.currResetTime = 0
            self.resetStartTime = 0
        end
    end
end 

--业务层处理旋转且需要组件恢复旋转时调用。
function InputDragPlayerController:HandleTouchInput2()
    self.resetStartTime = GameMain:GetTime() + self.resetTotalTime
end 