local _ENV = Boli2Env

---@class Pagination
Pagination = class("Pagination")

function Pagination:ctor(limit,quest,callback)
    self.limit = limit
    self.callback = callback
    self.quest = quest
    self.page = 1
    self.data = {}
end
local function request(self)
    if self.isRequest then
        return
    end
    self.page = self.page + 1
    self.isRequest = true
    local quest = self.quest
    quest(self.requestPageData[self.page],function(serverDataArr)
        --to do 查重
        for _,serverData in ipairs(serverDataArr) do
            table.insert(self.data,serverData)
        end
        self.isRequest = false
     
        self.callback(self.data)
    end)
end
function Pagination:Start(rawData)
    self:Clear()
    self.totalNum = #rawData
    self.requestPageData = GlobalFun.GetLineArray(rawData,self.limit)
    request(self)
end
function Pagination:Update()
    if #self.data >= self.totalNum then
        return
    end
    request(self)
end
function Pagination:Clear()
    self.data = {}
    self.page = 0
end
