local _ENV = Boli2Env

ScrollPlaneHorizontal = {
    scrollRect,
    layoutRect,
    initAction, --初始化函数
    refreshAction, --刷新函数
    spaceOffset, --item之间间隔,默认间隔10.0
    topOffset, --顶上间隔
    isIrregular, --拖动物体是否不规则
    objTra,
    objRect,
    poolQuene,
    maskWidth,
    objWidth,
    scrollNum, --拖动数量
    scrollData,
    compTabRes, --item 初始化结果存放
    isInit
}
ScrollPlaneHorizontal.__index = ScrollPlaneHorizontal

local function New(_scrollRect, _layoutRect, _initAction, _refreshAction, _spaceOffset, _topOffset, _irregular)
    local scrollPlaneHorizontal = {}
    setmetatable(scrollPlaneHorizontal, ScrollPlaneHorizontal)
    scrollPlaneHorizontal:Init(_scrollRect, _layoutRect, _initAction, _refreshAction, _spaceOffset, _topOffset, _irregular)
    return scrollPlaneHorizontal
end

function ScrollPlaneHorizontal:New(_scrollRect, _layoutRect, _initAction, _refreshAction, _spaceOffset, _topOffset)
    return New(_scrollRect, _layoutRect, _initAction, _refreshAction, _spaceOffset, _topOffset, false)
end

--不规则item
function ScrollPlaneHorizontal:NewIrrgular(_scrollRect, _layoutRect, _initAction, _refreshAction, _spaceOffset, _topOffset)
    return New(_scrollRect, _layoutRect, _initAction, _refreshAction, _spaceOffset, _topOffset, true)
end

local function InitRectAnchorAncPivot(rect)
    rect.anchorMin = Vector2(0, 0.5)
    rect.anchorMax = Vector2(0, 0.5)
    rect.pivot = Vector2(0, 0.5)
end

function ScrollPlaneHorizontal:Init(_scrollRect, _layoutRect, _initAction, _refreshAction, _spaceOffset, _topOffset, _irregular)
    self.scrollRect = _scrollRect
    self.layoutRect = _layoutRect
    self.initAction = _initAction
    self.refreshAction = _refreshAction
    self.spaceOffset = _spaceOffset or 10.0
    self.topOffset = _topOffset or 0.0
    self.isIrregular = _irregular

    local objPool = GlobalFun.GetObj(self.layoutRect, "objPool")
    if objPool.activeSelf then
        GlobalFun.SetObj(objPool, false)
    end
    self.objTra = GlobalFun.GetTra(self.layoutRect, "objPool/obj")
    self.objRect = GlobalFun.GetRect(self.layoutRect, "objPool/obj")
    self.poolQuene = Queue.new()
    InitRectAnchorAncPivot(self.layoutRect)
    InitRectAnchorAncPivot(self.objRect)
    self:InitUpdateEvent()

    self:InitMaskWidth()
    self:InitObjWidth()
    self.scrollNum = 0
    self.scrollData = {}
    self.compTabRes = {}
    self.isInit = false
end

function ScrollPlaneHorizontal:InitUpdateEvent()
    self.scrollRect.onValueChanged:RemoveAllListeners()
    self.scrollRect.onValueChanged:AddListener(
        function(val)
            self:Update(val)
        end
    )
end

function ScrollPlaneHorizontal:InitMaskWidth()
    local maskRect = GlobalFun.GetRect(self.scrollRect)
    self.maskWidth = maskRect.rect.size.x
end

function ScrollPlaneHorizontal:InitObjWidth()
    self.objWidth = self.objRect.sizeDelta.x
end

function ScrollPlaneHorizontal:GetItemTra(index)
    local tra
    if self.poolQuene:IsEmpty() then
        tra = GlobalFun.InstantiateTra(self.objTra, self.layoutRect)
        Transform.SetLocalPosition(tra, 0, 0, 0)
    else
        tra = self.poolQuene:Dequeue()
    end
    GlobalFun.SetObj(tra.gameObject, true)
    return tra
end

function ScrollPlaneHorizontal:CycleItemTra(tra)
    if GlobalFun.NotNilOrNull(tra) then
        GlobalFun.SetObj(tra.gameObject, false)
        self.poolQuene:Enqueue(tra)
    end
end

function ScrollPlaneHorizontal:InitScrollPlane(_scrollInfo, _startIndex, _heights, _isRetainOriginalPos)
    self.isInit = _scrollInfo ~= nil and #_scrollInfo >= 0
    if self.isInit then
        self:InitData(_scrollInfo, _heights)
        self:InitLayoutRect(_startIndex, _isRetainOriginalPos)
        self:Update()
    end
end

function ScrollPlaneHorizontal:GetItemPosX(index)
    if index == 1 then
        return self.topOffset
    else
        local upItem = self.scrollData[index - 1]
        return upItem.anchoredPosition.x + upItem.width + self.spaceOffset
    end
end

function ScrollPlaneHorizontal:InitData(_scrollInfo, _heights)
    if self.scrollData then
        for _, v in ipairs(self.scrollData) do
            self:CycleItemTra(v.tra)
        end
    end
    self.scrollData = {}
    for i, v in ipairs(_scrollInfo) do
        local hasHeight = false
        self.scrollData[#self.scrollData + 1] = {
            index = i,
            tra = nil,
            data = v,
            width = self.objWidth,
            checkedWidth = false,
            anchoredPosition = Vector2(self:GetItemPosX(i), 0.0)
        }
    end
    self.scrollNum = #self.scrollData
end

function ScrollPlaneHorizontal:InitLayoutRect(_startIndex, _isRetainOriginalPos)
    local totalWidth = self:GetItemPosX(self.scrollNum + 1)
    UIKit.SetRectSize(self.layoutRect, totalWidth, nil)
    if _isRetainOriginalPos then
        --刷新保留原来位置不动
    elseif _startIndex and self.scrollNum > 0 then
        _startIndex = _startIndex > self.scrollNum and self.scrollNum or _startIndex
        local startPos = -self.scrollData[_startIndex].anchoredPosition.x
        self:SetLayoutRectPos(startPos)
    end
end

function ScrollPlaneHorizontal:SetLayoutRectPos(posX)
    local totalWidth = self:GetItemPosX(self.scrollNum + 1)
    if totalWidth > self.maskWidth then
        local offset = -totalWidth + self.maskWidth
        if posX < offset then
            posX = offset
        end
        UIKit.SetRectPos(self.layoutRect, posX, nil)
    end
end

function ScrollPlaneHorizontal:IsVisible(info)
    local pos1 = self.layoutRect.anchoredPosition
    local pos2 = info.anchoredPosition
    if (pos2.x < - pos1.x - info.width ) or (pos2.x > - pos1.x + self.maskWidth) then
        return false
    end
    return true
end

function ScrollPlaneHorizontal:CheckItemHeight(info, compRes)
    if self.isIrregular and not info.checkedHeight then
        local co =
            coroutine.create(
            function()
                Yield(WaitForEndOfFrame())
                info.checkedHeight = true
                if info.width ~= compRes.rect.sizeDelta.x or compRes.rect.anchoredPosition.x ~= self:GetItemPosX(info.index) then
                    info.width = compRes.rect.sizeDelta.x
                    info.anchoredPosition = Vector2(self:GetItemPosX(info.index), 0.0)
                    compRes.rect.anchoredPosition = info.anchoredPosition
                    self:InitLayoutRect()
                end
            end
        )
        coroutine.resume(co)
    end
end

function ScrollPlaneHorizontal:RefreshItem(info)
    local tra = info.tra
    if self.compTabRes[tra] == nil then
        self.compTabRes[tra] = self.initAction(tra)
    end
    local compRes = self.compTabRes[tra]
    self.refreshAction(info.data, compRes, info.index)
    if compRes.rect == nil then
        compRes.rect = GlobalFun.GetRect(tra)
    end
    compRes.rect.anchoredPosition = info.anchoredPosition
    self:CheckItemHeight(info, compRes)
end

function ScrollPlaneHorizontal:Update(val)
    if self.isInit then
        for i = 1, self.scrollNum do
            local info = self.scrollData[i]
            local isVisiable = self:IsVisible(info)
            local hasObj = GlobalFun.NotNilOrNull(info.tra)
            if isVisiable and not hasObj then
                info.tra = self:GetItemTra(i)
                self:RefreshItem(info)
            end
            if not isVisiable and hasObj then
                self:CycleItemTra(info.tra)
                info.tra = nil
            end
        end
        self:TriggerProgressCallback(val)
    end
end

function ScrollPlaneHorizontal:TriggerProgressCallback(val)
    local progressCallback = self.progressCallback
    if not val then
        return
    end
    local progress = math.clamp(1 - val.x, 0, 1)
    GlobalFun.Try(progressCallback, progress)
end

function ScrollPlaneHorizontal:RegisterProgressCallback(progressCallback)
    self.progressCallback = progressCallback
end

--将Item重新初始化
function ScrollPlaneHorizontal:ReInit()
    for k, v in pairs(self.compTabRes) do
        local itemTra = v.tra
        self.compTabRes[k] = self.initAction(itemTra)
    end
end
