local _ENV = Boli2Env

TimeMgr = {}

local timerArr = {}
local timerRemoveCache ={}
local timerAddCache={}
local _taskLst = {}
local TIME_TYPE = {
    SEC = 1, --秒
    FRAME = 2, --帧
}

local Timer = {
    id=nil,
    value=nil,
    timeType=nil,
    action=nil,
    updateAction=nil
}
Timer.__index = Timer

local WARN_RANGE = 20

function Timer:New(id, value, timeType, action, updateAction)
    local timer = {}
    setmetatable(timer, Timer)
    timer:Init(id, value, timeType, action, updateAction)
    return timer
end

function Timer:Init(id, value, timeType, action, updateAction)
    self.id = id
    self.value = value
    self.timeType = timeType
    self.action = action
    self.updateAction = updateAction
end

function Timer:GetInterval()
    if self.timeType == TIME_TYPE.FRAME then
        return 1
    else
        return GameMain:GetDeltaTime()
    end
end

function Timer:Update()
    if self.value == nil then
        self.value = 0
    end
    self.value = self.value - self:GetInterval()
    if self.value <= 0 then
        if self.action then
            self.action()
        end
        self:Destroy()
    else
        if self.updateAction then
            self.updateAction(self.value)
        end
    end
end

local function RemoveTimer(id,needExecute)
    if timerArr[id] ~= nil or timerAddCache[id]~=nil then
        if needExecute then
            if timerArr[id].action then 
                timerArr[id].action()
                --置空 防止多次执行
                timerArr[id].action=nil
            end
        end
        table.insert(timerRemoveCache,id)
      --  timerArr[id] = nil
    end
end


function Timer:Destroy()
    RemoveTimer(self.id)
    self.id = nil
    self.time = nil
    self.frame = nil
    self.action = nil
    self.updateAction = nil
end

local function UpdateTimer()
    for i, v in pairs(timerRemoveCache) do
        timerArr[v]=nil
    end
    timerRemoveCache={}

    for i, v in pairs(timerAddCache) do
        timerArr[i]=v
    end
    timerAddCache={}
    for _, v in pairs(timerArr) do
        if v ~= nil then
            v:Update()
        end
    end
end

local function CtorTimeTask(func, delay, timeType,cantRemove)
    local t = {}
    t.func = func
    t.delay = GameMain:GetTime() + delay
    t.cantRemove=cantRemove
    return t
end

local function UpdateTask()
    if #_taskLst > 0 then
        local time = GameMain:GetTime()
        for i = #_taskLst, 1, -1 do
            if time >= _taskLst[i].delay then
                _taskLst[i].func()
                table.remove(_taskLst, i)
            end
        end
    end
end

function TimeMgr.Update()
    UpdateTask()
    UpdateTimer()
end

function TimeMgr.AddTask(func, delay, delayTimeType,cantRemove)
    table.insert(_taskLst, CtorTimeTask(func, delay,nil,cantRemove))
end

function TimeMgr.AddTimer(id, time, action, updateAction)
    RemoveTimer(id)
    local timer = Timer:New(id, time, TIME_TYPE.SEC, action, updateAction)
    
    timerAddCache[id]=timer
    --timerArr[id] = timer
end

function TimeMgr.AddFramer(id, frame, action)
    RemoveTimer(id)
    local timer = Timer:New(id, frame, TIME_TYPE.FRAME, action)
    timerAddCache[id]=timer
   -- timerArr[id] = timer
end

function TimeMgr.RemoveTimer(id)
    RemoveTimer(id)
end
function TimeMgr.RemoveTimerAndDoAction(id)
    RemoveTimer(id,true)
end

function TimeMgr.HasTimer(id)
    return  timerArr[id] ~= nil or timerAddCache[id]~=nil
end

function TimeMgr.RemoveAllTimer()
    for _,v in ipairs(timerArr) do
        if v ~= nil then
            RemoveTimer(v.id)
        end
    end
end

function TimeMgr.CollectAll()
    local cacheTbl=nil
    for i, v in ipairs(_taskLst) do
        if v.cantRemove then
            if not cacheTbl then cacheTbl={} end 
            table.insert(cacheTbl,v)
        end
    end
    _taskLst = {}
    if cacheTbl then
        for i, v in ipairs(cacheTbl) do
            table.insert(_taskLst,v)
        end
    end
end
