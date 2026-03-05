local _ENV = Boli2Env

SoundClick = {}

local objBtnDic = {} --button
local objScriptDic = {} --UISound.cs 脚本缓存
local clickObjNameArr = {}

-- local function CheckNullKey(dic)
--     for k, _ in pairs(dic) do
--         if tostring(k) == "null" then
--             dic[k] = nil
--         end
--     end
-- end

-- local function CheckDicsKey()
--     CheckNullKey(objBtnDic)
--     CheckNullKey(objScriptDic)
-- end

local function HasUISoundScript(clickObj)
    if objScriptDic[clickObj] == nil then
        objScriptDic[clickObj] = GlobalFun.GetType(clickObj, nil, "UISound") ~= nil
    end
    return objScriptDic[clickObj]
end

local function HasBtnOrTog(clickObj)
    if objBtnDic[clickObj] == nil then
        objBtnDic[clickObj] = GlobalFun.GetType(clickObj, nil, "Selectable") ~= nil
    end
    return objBtnDic[clickObj]
end

local function AddClickObj(clickObj)
    table.insert(clickObjNameArr, clickObj.name)
    if #clickObjNameArr > 7 then
        table.remove(clickObjNameArr, 1)
    end
end

function SoundClick.Play()
    local clickObj = BasePlane.GetClickObj()
    if clickObj == nil then
        return
    end
    AddClickObj(clickObj)
    -- CheckDicsKey()
    if HasUISoundScript(clickObj) then
        return
    end
    if HasBtnOrTog(clickObj) then
        SoundManager.PlayUISound("Play_Click")
    else
        SoundManager.PlayUISound("Play_Click_Touch_Screen")
    end
end

function SoundClick.GetClickObjNameArr()
    return clickObjNameArr
end

function SoundClick.Clear()
    objBtnDic = {}
    objScriptDic = {}
end
