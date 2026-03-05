local _ENV = Boli2Env

ReceiveLog = {}

local _LogType = {
    Error = 0,
    Assert = 1,
    Warning = 2,
    Log = 3,
    Exception = 4,
}

-- 错误消息每次登录只发一次，这里加个标志
ReceiveLog.SendFlag = nil

local function handleErrorMessage(logString, stackTrace)
    if isEditor then
        return
    end
    if not Config.Online then
        return
    end
    -- 检查flag
    local blockFlag = ReceiveLog.SendFlag
    if blockFlag then
        return
    end
    ReceiveLog.SendFlag = true
    -- 上报错误
    local clickObjNameArr = SoundClick.GetClickObjNameArr()
    local clickInfoStr = table.concat(clickObjNameArr, '--')
    local composeString = string.format("clickInfo:%s\nerrorMessage:\n%s", clickInfoStr, logString)
    AssistService.SendClientErrorMessage(composeString)
    UnityEngine.Debug.unityLogger.logEnabled = false
end

function ReceiveLog.OnReceived(logString, stackTrace, type)
    if type == _LogType.Error then
        handleErrorMessage(logString, stackTrace)
    elseif type == _LogType.Warning then
        if isEditor then
            print("Warning in lua   ",logString)
        end
    end
end


