local _ENV = Boli2Env
---@class InputDragFashionWeaponController:InputDragController
---@field super InputDragController
InputDragFashionWeaponController = class("InputDragFashionWeaponController", InputDragController)

--鼠标拖拽旋转速率倍率
local mouseXAcc = 6
--触摸拖拽旋转速率倍率
local touchXAcc = 12

function InputDragFashionWeaponController:ctor(tra, customIgnoreCondCheckFunc,dirType)
    self.dirType = dirType
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

function InputDragFashionWeaponController:Update()
    if self.tra then
        self.super.Update(self)
    end
end

function InputDragFashionWeaponController:SetTra(tra)
    self.tra = tra
end

function InputDragFashionWeaponController:HandleTouchInputX(deltaX, isMouse)
    local tra = self.tra
    if tra then
        if isMouse then
            tra.transform:Rotate(Vector3.up,deltaX * mouseXAcc*-1 * self.dirType)
        else
            tra.transform:Rotate(Vector3.up,deltaX * touchXAcc*-1 * self.dirType)
        end
    end
    --self.resetStartTime = GameMain:GetTime() + self.resetTotalTime
end

function InputDragFashionWeaponController:CancelTouchInputX(isMouse)
    if isMouse and self.resetStartTime == 0 then
        return
    end
    if GameMain:GetTime() > self.resetStartTime then
        self.currResetTime = self.currResetTime + GameMain:GetDeltaTime()
        local tra = self.tra
        if tra then
            --local x,y,z=GlobalFun.GetLerpXYZXYZ(tra.transform.localEulerAngles.x, y, tra.transform.localEulerAngles.z,0,0,0, 0.1)
            --TransformExtension.SetLocalEulerAngles(tra.transform,x,y,z)
        end
        if self.currResetTime >= self.resetTotalTime then
            --TransformExtension.SetLocalEulerAngles(tra.transform,0, 0, 0)
            self.currResetTime = 0
            self.resetStartTime = 0
        end
    end
end 

--业务层处理旋转且需要组件恢复旋转时调用。
function InputDragFashionWeaponController:HandleTouchInput2()
    self.resetStartTime = GameMain:GetTime() + self.resetTotalTime
end 