local _ENV = Boli2Env

---@class ScrollRectCenterOnChild
ScrollRectCenterOnChild = class("ScrollRectCenterOnChild")

function ScrollRectCenterOnChild:ctor(_scrollRectObj, _contentObj, _onPageIndexChanged, _scrollSpeed, _scrollThreshold)
    self.scrollRectGameObject = _scrollRectObj.gameObject
    self.contentTransform = _contentObj.gameObject.transform
    self.onPageIndexChanged = _onPageIndexChanged
    self.scrollSpeed = _scrollSpeed and _scrollSpeed or 8.0
    self.scrollThreshold = _scrollThreshold and _scrollThreshold or 100.0
    
    self.scrollRect = GlobalFun.GetType(self.scrollRectGameObject, nil, "ScrollRect")
    
    self.pageArray = {}
    self.pageCount = 0
    self.items = {}
    
    self.targetPagePosition = 0.0
    self.currentPage = 0
    self.isDragging = false
    self.startPressPosition = Vector2.zero
    self.canUpdate = true
    
    self.cs2luaUGUIEventsEntry = GlobalFun.GetOrAddComponent(self.scrollRectGameObject, "CS2LuaUGUIEventsEntry")
    self.cs2luaUGUIEventsEntry:SetLuaTable(self)
end

function ScrollRectCenterOnChild:InitPageArray()
    self.items = {}
    self.pageArray = {}
    
    local itemsCount = self.contentTransform.childCount

    for i = 1, itemsCount do
        local oneItemObj = self.contentTransform:GetChild(i - 1).gameObject
        if oneItemObj.activeSelf then
            table.insert(self.items, oneItemObj)
        end
    end
    
    self.pageCount = #self.items
    local pageCountMinusOne = self.pageCount - 1
    for i = 1, self.pageCount do
        table.insert(self.pageArray, (i - 1) * 1.0 / pageCountMinusOne)
    end
end

function ScrollRectCenterOnChild:SetCanUpdate(_enabled)
    self.canUpdate = _enabled == true
end

function ScrollRectCenterOnChild:_Update(_deltaTime)
    if self.canUpdate and not self.isDragging then
        if self.scrollRect.horizontal then
            self.scrollRect.horizontalNormalizedPosition = GlobalFun.Lerp(self.scrollRect.horizontalNormalizedPosition, self.targetPagePosition, self.scrollSpeed * _deltaTime);
        elseif self.scrollRect.vertical then
            self.scrollRect.verticalNormalizedPosition = GlobalFun.Lerp(self.scrollRect.verticalNormalizedPosition, self.targetPagePosition, self.scrollSpeed * _deltaTime);
        end
    end
end

function ScrollRectCenterOnChild:onChangeCurrentPageIndex() 
    if self.onPageIndexChanged ~= nil then
        self.onPageIndexChanged(self.currentPage) 
    end
end

function ScrollRectCenterOnChild:CS_OnBeginDrag(_eventData)
    self.isDragging = true;
    self.startPressPosition = _eventData.position;
end

function ScrollRectCenterOnChild:CS_OnEndDrag(_eventData)
    self.isDragging = false;
    local pos = self.scrollRect.horizontal and self.scrollRect.horizontalNormalizedPosition or self.scrollRect.verticalNormalizedPosition;
    local direction = self.scrollRect.horizontal and _eventData.position.x - self.startPressPosition.x or _eventData.position.y - self.startPressPosition.y;
    local index = 1
    local offset = math.abs(self.pageArray[index] - pos);
    for i = 1, #self.pageArray do
        local _offset = math.abs(self.pageArray[i] - pos);
        if _offset < offset then
            index = i
            offset = _offset
        end 
    end
    

    if math.abs(direction) >= self.scrollThreshold then
        index = index - math.floor(math.sign(direction))
    end
    index = math.clamp(index, 1, #self.pageArray);

    self.targetPagePosition = self.pageArray[index];
    self.currentPage = index;

    self:onChangeCurrentPageIndex()
end

function ScrollRectCenterOnChild:SetCurrentPageIndex(_pageIndex) 
    self.currentPage = _pageIndex;
    self.targetPagePosition = self.pageArray[self.currentPage];
    
    self:onChangeCurrentPageIndex()
end
