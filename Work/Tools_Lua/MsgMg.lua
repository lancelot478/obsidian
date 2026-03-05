local _ENV = Boli2Env

MsgMg = {eventTable = {}}

local function ContainsFuncName(tableTmp, funcName)
    for _, v in pairs(tableTmp) do
        if type(v) == "table" and v[2] == funcName then
            return true, v
        end
    end
    return false, nil
end

function MsgMg.IsFunctionContained(msgName,funcCallBack)
    if MsgMg.eventTable[msgName] == nil then
        MsgMg.eventTable[msgName] = {}
    end
    local tableTmp = MsgMg.eventTable[msgName]
    return table.contains(tableTmp, funcCallBack) 
end

function MsgMg.RegisterCallBack(msgName, funcCallBack, funcName)
    if MsgMg.eventTable[msgName] == nil then
        MsgMg.eventTable[msgName] = {}
    end
    local tableTmp = MsgMg.eventTable[msgName]
    if funcName ~= nil then
        if ContainsFuncName(tableTmp, funcName) then
            return
        end
        table.insert(tableTmp, {funcCallBack, funcName})
    else
        if table.contains(tableTmp, funcCallBack) then
            return
        end
        table.insert(tableTmp, funcCallBack)
    end
end

function MsgMg.SendMsg(msgName, ...)
    local events = MsgMg.eventTable[msgName]
    local tableTmp = table.clone(events)
    if tableTmp ~= nil and #tableTmp > 0 then
        for _, v in pairs(tableTmp) do
            if type(v) == "table" then
                v[1](...)
            elseif type(v) == "function" then
                v(...)
            end
        end
    end
end

function MsgMg.RemoveCallBack(msgName, funcCallBack, funcName)
    if MsgMg.eventTable[msgName] == nil then
        return
    end
    local tableTmp = MsgMg.eventTable[msgName]
    if funcName ~= nil then
        local contais, funcTable = ContainsFuncName(tableTmp, funcName)
        if contais then
            table.delete(tableTmp, funcTable)
        end
    else
        table.delete(tableTmp, funcCallBack)
    end
end

function MsgMg.RemoveCallBackByFuncName(msgName, funcName)
    MsgMg.RemoveCallBack(msgName, nil, funcName)
end

function MsgMg.RemoveCallBackByName(msgName)
    if MsgMg.eventTable[msgName] == nil then
        return
    end
    MsgMg.eventTable[msgName] = {}
end

function MsgMg.RemoveAllCallBack()
    MsgMg.eventTable = {}
end

function MsgMg.PrintCallBack(msgName)
    local tableTmp = MsgMg.eventTable[msgName]
    if tableTmp ~= nil then
        print("########", msgName .. " " .. #tableTmp)
        for i,v in ipairs(tableTmp) do
            print(i,v)
        end
    end
end
