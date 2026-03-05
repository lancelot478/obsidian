local _ENV = Boli2Env

RedPoint = {
    redID,
    redType,
    refreshFun,
    father,

    children,
    redTra,
    redValue,
}
RedPoint.__index = RedPoint

function RedPoint:New(redID, redType, refreshFun, father)
    local point = setmetatable({}, RedPoint)
    point:Init(redID, redType, refreshFun, father)
    return point
end

function RedPoint:Init(redID, redType, refreshFun, father)
    self.redID = redID
    self.redType = redType
    self.refreshFun = refreshFun
    self.father = father
    if self.father ~= nil then
        self.father:AddChild(self)
    end
    self.children = {}
    self.redTra = nil
    self:RefreshValue()
end

--数值转化
local function ValueToInt(value)
    if type(value) == "boolean" then
        return value and 1 or 0
    elseif type(value) == "nil" then
        return 0
    end
    return value
end

function RedPoint:RefreshValue()
    if self.refreshFun then
        self.redValue = ValueToInt(self.refreshFun())
    else
        self.redValue = 0
    end
end

function RedPoint:IsActive()
    if self.redValue > 0 then
        return true
    else
        for i, v in ipairs(self.children) do
            if v:IsActive() then
                return true
            end
        end
        return false
    end
end

function RedPoint:SetObjState(isDirectDisplay)
    local isShow = isDirectDisplay or self:IsActive()
    if self.redTra ~= nil then
        if self.redID == RedID.HomeBuild then
            GlobalFun.SetObj(self.redTra.gameObject, MainInterfaceData.IsTabUnlock(1) and isShow)
        else
            GlobalFun.SetObj(self.redTra.gameObject, isShow)
        end
    end
    return isShow
end

function RedPoint:RefreshFather(isDirectDisplay)
    if self.father then
        self.father:Refresh(isDirectDisplay)
    end
end

--isDirectDisplay:是否直接显示
function RedPoint:Refresh(isDirectDisplay)
    if not isDirectDisplay then
        self:RefreshValue()
    end
    local isShow = self:SetObjState(isDirectDisplay)
    self:RefreshFather(isShow)
    self:RefreshBagObj(isShow)
end

function RedPoint:RefreshBagObj(isShow)
    if isShow and self.redID == RedID.Bag and GlobalFun.NotNilOrNull(self.redTra) then
        self.redType = RedType.Common
        for i, v in ipairs(self.children) do
            if v.redType == RedType.Full and v.redValue > 0 then
                self.redType = RedType.Full
                break
            end
        end  
        GlobalFun.SetObj(GlobalFun.GetObj(self.redTra, "Common"), self.redType == RedType.Common)
        GlobalFun.SetObj(GlobalFun.GetObj(self.redTra, "Full"), self.redType == RedType.Full)
    end
end

function RedPoint:IsExistChild(child)
    for _, v in pairs(self.children) do
        if v.redID == child.redID then
            return true
        end
    end
    return false
end

function RedPoint:AddChild(child)
    if not self:IsExistChild(child) then
        self.children[#self.children + 1] = child
    end
end

function RedPoint:SetRedTra(redTra)
    self.redTra = redTra
    if GlobalFun.NotNilOrNull(redTra) then
        self:Refresh()
    end
end

function RedPoint:SetRedTraNil()
    self.redTra = nil
end
