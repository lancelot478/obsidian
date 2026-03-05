local _ENV = Boli2Env

ScrollPlane = {
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

    maskHeight,
    objHeight,
    scrollNum, --拖动数量
    scrollData,
    compTabRes, --item 初始化结果存放
    isInit
}
ScrollPlane.__index = ScrollPlane

local function New(_scrollRect, _layoutRect, _initAction, _refreshAction, _spaceOffset, _topOffset, _irregular)
    local scrollPlane = {}
    setmetatable(scrollPlane, ScrollPlane)
    scrollPlane:Init(_scrollRect, _layoutRect, _initAction, _refreshAction, _spaceOffset, _topOffset, _irregular)
    return scrollPlane
end

function ScrollPlane:New(_scrollRect, _layoutRect, _initAction, _refreshAction, _spaceOffset, _topOffset)
    return New(_scrollRect, _layoutRect, _initAction, _refreshAction, _spaceOffset, _topOffset, false)
end

--不规则item
function ScrollPlane:NewIrrgular(_scrollRect, _layoutRect, _initAction, _refreshAction, _spaceOffset, _topOffset)
    return New(_scrollRect, _layoutRect, _initAction, _refreshAction, _spaceOffset, _topOffset, true)
end

local function InitRectAnchorAncPivot(rect)
    rect.anchorMin = Vector2(0.5, 1)
    rect.anchorMax = Vector2(0.5, 1)
    rect.pivot = Vector2(0.5, 1)
end

function ScrollPlane:Init(_scrollRect, _layoutRect, _initAction, _refreshAction, _spaceOffset, _topOffset, _irregular)
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

    self:InitMaskHeight()
    self:InitObjHeight()
    self.scrollNum = 0
    self.scrollData = {}
    self.compTabRes = {}
    self.isInit = false
end

function ScrollPlane:InitUpdateEvent()
    self.scrollRect.onValueChanged:RemoveAllListeners()
    self.scrollRect.onValueChanged:AddListener(function(val)
        self:Update(val)
    end)
end

function ScrollPlane:InitMaskHeight()
    local maskRect = GlobalFun.GetRect(self.scrollRect)
    self.maskHeight = maskRect.rect.size.y
end

function ScrollPlane:InitObjHeight()
    self.objHeight = self.objRect.sizeDelta.y
end

function ScrollPlane:GetItemTra(index)
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

function ScrollPlane:CycleItemTra(tra)
    if GlobalFun.NotNilOrNull(tra) then
        GlobalFun.SetObj(tra.gameObject, false)
        self.poolQuene:Enqueue(tra)
    end
end

function ScrollPlane:InitScrollPlane(_scrollInfo, _startIndex, _heights, _isRetainOriginalPos)
    self.isInit = _scrollInfo ~= nil and #_scrollInfo >= 0
    if self.isInit then
        self:InitData(_scrollInfo, _heights)
        self:InitLayoutRect(_startIndex, _isRetainOriginalPos)
        self:Update()
    end
end

function ScrollPlane:GetItemPosY(index)
    if index == 1 then
        return - self.topOffset
    else
        local upItem = self.scrollData[index - 1]
        return upItem.anchoredPosition.y - upItem.height - self.spaceOffset
    end
end

function ScrollPlane:InitData(_scrollInfo, _heights)
    if self.scrollData then
        for _, v in ipairs(self.scrollData) do
            self:CycleItemTra(v.tra)
        end
    end
    self.scrollData = {}
    if _scrollInfo and #_scrollInfo > 0 then
        local hasHeight = _heights ~= nil
        for i, v in ipairs(_scrollInfo) do
            local height = hasHeight and _heights[i] or self.objHeight
            self:AddScrollData(v, height, hasHeight)
        end
    else
        self.scrollNum = 0
    end
end

--新增scrollData数据
function ScrollPlane:AddScrollData(data, height, checkedHeight)
    local index = #self.scrollData + 1
    self.scrollData[index] = {
        index = index,
        tra = nil,
        data = data,
        height = height,
        checkedHeight = checkedHeight,
        anchoredPosition = Vector2(0.0, self:GetItemPosY(index)),
    }
    self.scrollNum = index
end

--扩充scrollData数据
function ScrollPlane:ExpandScrollData(data, height)
    self:AddScrollData(data, height, true)
    self:InitLayoutRect()
    self:Update()
end

function ScrollPlane:InitLayoutRect(_startIndex, _isRetainOriginalPos)
    local totalHeight = self:GetItemPosY(self.scrollNum + 1)
    UIKit.SetRectSize(self.layoutRect, nil, -totalHeight)
    if _isRetainOriginalPos then
        --刷新保留原来位置不动
    elseif _startIndex and self.scrollNum > 0 then
        _startIndex = _startIndex > self.scrollNum and self.scrollNum or _startIndex
        local startPos = -self.scrollData[_startIndex].anchoredPosition.y
        self:SetLayoutRectPos(startPos)
    end
end

function ScrollPlane:SetLayoutRectPos(posY)
    local totalHeight = self:GetItemPosY(self.scrollNum + 1)
    if - totalHeight > self.maskHeight then
        local offset = -totalHeight - self.maskHeight
        if posY > math.abs(offset) then
            posY = offset
        end
        UIKit.SetRectPos(self.layoutRect, nil, posY)
    end
end

function ScrollPlane:IsVisible(info)
    local pos1 = self.layoutRect.anchoredPosition
    local pos2 = info.anchoredPosition
    if (pos1.y > info.height - pos2.y) or (pos1.y + self.maskHeight < -pos2.y) then
        return false
    end
    return true
end

function ScrollPlane:CheckItemHeight(info, compRes)
    if self.isIrregular and not info.checkedHeight then
        local co = coroutine.create(function()
            Yield(WaitForEndOfFrame())
            info.checkedHeight = true
            if info.height ~= compRes.rect.sizeDelta.y or compRes.rect.anchoredPosition.y ~= self:GetItemPosY(info.index) then
                info.height = compRes.rect.sizeDelta.y
                info.anchoredPosition = Vector2(0.0, self:GetItemPosY(info.index))
                compRes.rect.anchoredPosition = info.anchoredPosition
                self:InitLayoutRect()
            end
        end)
        coroutine.resume(co)
    end
end

function ScrollPlane:RefreshItem(info)
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

function ScrollPlane:Update(val)
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

function ScrollPlane:TriggerProgressCallback(val)
    local progressCallback = self.progressCallback
    if not val then
        return
    end
    local progress = math.clamp(1 - val.y, 0, 1)
    GlobalFun.Try(progressCallback, progress)
end

function ScrollPlane:RegisterProgressCallback(progressCallback)
    self.progressCallback = progressCallback
end

--是否拖到底部了
function ScrollPlane:IsBottom()
    return self.scrollRect.normalizedPosition.y <= 0.01
end

function ScrollPlane:SetBottom()
    if self.isIrregular then
        GlobalFun.InvokeByFrame(function()
            if GlobalFun.IsNilOrNull(self.scrollRect) then
                return
            end
            self.scrollRect.normalizedPosition = Vector2.zero
        end)
    else
        self.scrollRect.normalizedPosition = Vector2.zero
    end
end

function ScrollPlane:SetTop()
    self.scrollRect.normalizedPosition = Vector2.one
end

--刷新Items刷新动效
function ScrollPlane:PlayRefreshAnim(animPath, interval, duration, showAnimationArr)
    local res = {}
    for i = 1, self.scrollNum do
        local info = self.scrollData[i]
        local hasObj = GlobalFun.NotNilOrNull(info.tra)
        if hasObj and GlobalFun.IsActive(info.tra.gameObject) then
            local obj = info.tra.gameObject
            res[#res + 1] = {
                obj = obj,
                anim = GlobalFun.GetAnim(info.tra, animPath),
            }
            GlobalFun.SetObj(obj, false)
        end
    end
    --remove
    for i, v in ipairs(res) do
        TimeMgr.RemoveTimer("ScrollPlane.PlayRefreshAnim_1" .. i)
    end
    TimeMgr.RemoveTimer("ScrollPlane.PlayRefreshAnim_2")
    for i, v in ipairs(res) do
        local delay = (i - 1) * interval
        local func = function()
            GlobalFun.SetObj(v.obj, true)
            if showAnimationArr then
                GlobalFun.PlayAnim(v.anim, showAnimationArr[1], 0, 0.0)
            else
                UIKit.SetComponentState(v.anim, true)
            end
        end
        TimeMgr.AddTimer("ScrollPlane.PlayRefreshAnim_1" .. i, delay, func)
    end
    --恢复原状
    local allDelay = (#res - 1) * interval + duration
    local resetFun = function()
        for i, v in ipairs(res) do
            if showAnimationArr then
                GlobalFun.PlayAnim(v.anim, showAnimationArr[2], 0, 0.0)
            else
                UIKit.SetComponentState(v.anim, false)
            end
        end
    end
    TimeMgr.AddTimer("ScrollPlane.PlayRefreshAnim_2", allDelay, resetFun)
end

function ScrollPlane:PlayRefreshAnimByLine(animPath, interval, begin, num, showAnimationArr)
    if num < 2 then
        return
    end
    local res = {}
    for i = 1, self.scrollNum do
        local info = self.scrollData[i]
        local hasObj = GlobalFun.NotNilOrNull(info.tra)
        if hasObj and GlobalFun.IsActive(info.tra.gameObject) then
            for itemNum = 1, num do
                local itemTra = GlobalFun.GetTra(info.tra, tostring(itemNum))
                if itemNum <= #info.data then
                    res[#res + 1] = {
                        obj = itemTra.gameObject,
                        anim = GlobalFun.GetAnim(itemTra, animPath),
                    }
                end
                GlobalFun.SetObj(itemTra.gameObject, false)
            end
        end
    end
    for i, v in ipairs(res) do
        local delay = (i - 1) * interval
        local func = function()
            GlobalFun.SetObj(v.obj, true)
            if showAnimationArr then
                GlobalFun.PlayAnim(v.anim, showAnimationArr[1], 0, 0.0)
            else
                UIKit.SetComponentState(v.anim, true)
            end
        end
        TimeMgr.AddTask(func, delay)
    end
    --恢复原状
    local allDelay = (#res - 1) * interval + begin
    local resetFun = function()
        for i, v in ipairs(res) do
            if showAnimationArr then
                GlobalFun.PlayAnim(v.anim, showAnimationArr[2], 0, 0.0)
            else
                UIKit.SetComponentState(v.anim, false)
            end
        end
    end
    TimeMgr.AddTask(resetFun, allDelay)
end

--将Item重新初始化
function ScrollPlane:ReInit()
    for k, v in pairs(self.compTabRes) do
        local itemTra = v.tra
        self.compTabRes[k] = self.initAction(itemTra)
    end
end
--销毁物体--由于 UI 框架不兼容循环列表（在界面缓存的情况下），故特殊处理兼容
--function ScrollPlane:Destroy()
--    if self.scrollData then
--        for _, v in ipairs(self.scrollData) do
--            self:CycleItemTra(v.tra)
--        end
--    end
--    self.poolQuene:Foreach(function(itemTra)
--        GlobalFun.DestroyObj(itemTra.gameObject)
--    end)
--    self.scrollData = {}
--    self.poolQuene:Clear()
--end
