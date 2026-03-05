local _ENV = Boli2Env

if not UIKit then
    UIKit = {}
end
local emptyStr = ""

-- 设置Object状态
function UIKit.SetObjectState(object, state)
    if GlobalFun.IsNull(object) then
        return
    end
    state = state == true
    GlobalFun.SetObj(object, state)
end

function UIKit.SetChildObjectState(tran, path, state)
    local childObj = UIKit.GetChildObject(tran, path)
    UIKit.SetObjectState(childObj, state)
end

-- 设置组件状态
function UIKit.SetComponentState(comp, state)
    if GlobalFun.IsNull(comp) then
        return
    end
    state = not not state
    comp.enabled = state
end

function UIKit.GetChild(tran, path)
    if not path then
        return
    end
    if GlobalFun.IsNull(tran) then
        return
    end
    local child = GlobalFun.GetTra(tran, path)
    return child
end

function UIKit.GetChildObject(tran, path)
    local child = UIKit.GetChild(tran, path)
    if GlobalFun.IsNull(child) then
        return
    end
    local object = child.gameObject
    return object
end

function UIKit.GetComponent(tran, componentName, path)
    if path then
        tran = UIKit.GetChild(tran, path)
    end
    if GlobalFun.IsNull(tran) then
        return
    end
    local component = GlobalFun.GetType(tran, nil, componentName)
    return component
end

function UIKit.SetScaleX(tran, x)
    if not x then
        x = 1
    end
    if GlobalFun.IsNull(tran) then
        return
    end
    tran.localScale = Vector3(x, 1, 1)
end

function UIKit.SetText(comp, value)
    if GlobalFun.IsNull(comp) then
        return
    end
    if not value then
        value = ""
    end
    comp.text = value
end

function UIKit.SetSprite(comp, sprite, nativeSize)
    if GlobalFun.IsNull(comp) or GlobalFun.IsNull(sprite) then
        return
    end
    comp.sprite = sprite
    if nativeSize then
        comp:SetNativeSize()
    end
end

function UIKit.SetImage(comp, value, type, ...)
    if GlobalFun.IsNull(comp) then
        return
    end
    if not value then
        return
    end
    GlobalAtlas.SetIcon(comp, value, type, ...)
end
function UIKit.SetButton(comp, func)
    if GlobalFun.IsNull(comp) then
        return
    end
    if not func then
        return
    end
    GlobalFun.SetBtnFun(comp, func)
end
function UIKit.GetInputText(comp)
    if GlobalFun.IsNull(comp) then
        return
    end
    return comp.text or ""
end

function UIKit.SetToggleValue(comp, state)
    if GlobalFun.IsNull(comp) then
        return
    end
    state = state == true
    comp.isOn = state
end

function UIKit.CalcTextWidth(textComp)
    if GlobalFun.IsNull(textComp) then
        return
    end
    local textGenerator = textComp.cachedTextGeneratorForLayout
    local settings = textComp:GetGenerationSettings(Vector2.zero)
    local textPixelsPerUnit = textComp.pixelsPerUnit
    local width = textGenerator:GetPreferredWidth(textComp.text, settings) / textPixelsPerUnit
    return width
end

function UIKit.SetRectSize(rect, x, y)
    if GlobalFun.IsNull(rect) then
        return
    end
    local sizeDelta = rect.sizeDelta
    sizeDelta.x = x or sizeDelta.x
    sizeDelta.y = y or sizeDelta.y
    rect.sizeDelta = sizeDelta
end

function UIKit.SetRectPos(rect, x, y)
    if GlobalFun.IsNull(rect) then
        return
    end
    local pos = rect.anchoredPosition
    pos.x = x or pos.x
    pos.y = y or pos.y
    rect.anchoredPosition = pos
end

function UIKit.SetRectOffsetZero(rect)
    if GlobalFun.IsNull(rect) then
        return
    end
    rect.offsetMin = Vector2.zero
    rect.offsetMax = Vector2.zero
end

-- (+x)  / (-x)
function UIKit.NumToStr1(val)
    local retVal = ""
    if val then
        if val > 0 then
            retVal = "(+" .. val .. ")"
        elseif val < 0 then
            retVal = "(" .. val .. ")"
        end
    end
    return retVal
end

function UIKit.NumToStr2(val,str)
    local retVal = ""
    if val then
        if val > 0 then
            retVal = "(+" .. str .. ")"
        elseif val < 0 then
            retVal = "(" .. str .. ")"
        end
    end
    return retVal
end

function UIKit.SetInteractive(comp, state)
    if GlobalFun.IsNull(comp) then
        return
    end
    comp.interactable = state
end

local _tabActiveColor = Color(124 / 255, 99 / 255, 71 / 255)
local _tabInactiveColor = Color(1.0, 1.0, 1.0)
function UIKit.SetTabTextColor(text, active)
    if GlobalFun.IsNull(text) then
        error("set tab text color but text is nil")
        return
    end
    text.color = active and _tabActiveColor or _tabInactiveColor
end

function UIKit.SetColor(comp, x, y, z, w)
    if GlobalFun.IsNull(comp) then
        error("set color but comp is nil")
        return
    end
    x = tonumber(x) or 255
    y = tonumber(y) or 255
    z = tonumber(z) or 255
    w = tonumber(w) or 255
    comp.color = Color(x / 255.0, y / 255.0, z / 255.0, w / 255.0)
end

function UIKit.RemoveButtonListener(comp)
    if GlobalFun.IsNull(comp) then
        return
    end
    local onClick = comp.onClick
    if GlobalFun.IsNull(onClick) then
        return
    end
    onClick:RemoveAllListeners()
end

function UIKit.IsPointUI()
    local eventSystem = EventSystem.current
    if not eventSystem then
        return
    end
    local pointUI
    if isEditor then
        pointUI = eventSystem:IsPointerOverGameObject()
    else
        if Input.touchCount < 1 then
            return
        end
        local touch = Input.GetTouch(0)
        if not touch then
            return
        end
        local fingerId = touch.fingerId
        if not fingerId then
            return
        end
        pointUI = eventSystem:IsPointerOverGameObject(fingerId)
    end
    return pointUI
end

function UIKit.SetFullChild(tran)
    if GlobalFun.IsNull(tran) then
        return
    end
    tran.anchorMin = Vector2.zero
    tran.anchorMax = Vector2.one
    tran.offsetMax = Vector2.zero
    tran.offsetMin = Vector2.zero
end

local function setScrollPosition(scroll, pos)
    if GlobalFun.IsNull(scroll) then
        return
    end
    scroll.normalizedPosition = pos
end

function UIKit.SetScrollBottom(scroll)
    setScrollPosition(scroll, Vector2.zero)
end

function UIKit.SetScrollTop(scroll)
    setScrollPosition(scroll, Vector2.one)
end

function UIKit.ClearText(comp)
    if GlobalFun.IsNull(comp) then
        return
    end
    comp.text = emptyStr
end

local function changeDisplaySwitch(tran, state, init)
    local handleTran = GlobalFun.GetTra(tran, "Bg/Handle")
    local maskObject = GlobalFun.GetObj(tran, "Bg/Mask")
    local textComp = GlobalFun.GetText(tran, "Text")
    local isNull = GlobalFun.IsNull(handleTran) or GlobalFun.IsNull(maskObject) or GlobalFun.IsNull(textComp)
    if isNull then
        return
    end
    local target = state and 41.0 or -41.0
    local time = init and 0.0 or 0.1
    LeanTween.moveX(handleTran, target, time):setOnComplete(function()
        GlobalFun.SetObj(maskObject, not not state)
    end)
    local text = state and GlobalText.GetText("SettingsView19") or GlobalText.GetText("SettingsView20")
    GlobalFun.SetText(textComp, text)
end

function UIKit.ChangeSwitch(tran, value)
    local isNull = GlobalFun.IsNull(tran)
    if isNull then
        return
    end
    changeDisplaySwitch(tran, value, true)
end

function UIKit.DisplaySwitch(tran, callback, initValue)
    local isNull = GlobalFun.IsNull(tran)
    if isNull then
        return
    end
    local state
    local function switchState (passState, init)
        if passState ~= nil then
            state = not not passState
        else
            state = not state
        end
        changeDisplaySwitch(tran, state, init)
        if not init then
            callback(state)
        end
    end
    GlobalFun.GetBtn(tran, "", switchState)
    switchState(initValue, true)
end

local function getRectSize(rectTran)
    local rect = rectTran.rect
    local sizeX = rect.width
    local sizeY = rect.height
    return sizeX, sizeY
end

local function setScreenConstraint(rect, rectX, rectY, coverTran)
    local screenX, screenY
    if GlobalFun.IsNull(coverTran) then
        local canvasTran = UIManager:GetTran()
        local canvasSize = canvasTran.sizeDelta
        screenX, screenY = canvasSize.x, canvasSize.y
    else
        screenX, screenY = getRectSize(coverTran)
    end

    local localScale = rect.localScale
    rectX = rectX * localScale.x
    rectY = rectY * localScale.y
    if rectX >= screenX and rectY >= screenY then
        return
    end
    local ratioX = screenX / rectX
    local ratioY = screenY / rectY
    local max = math.max(ratioX, ratioY)
    rectX = rectX * max / localScale.x
    rectY = rectY * max / localScale.y
    --print(rectX, rectY, screenX, screenY)
    UIKit.SetRectSize(rect, rectX, rectY)
end

local function setScreenTextureConstraint(rect, texture, preserveAspect)
    if GlobalFun.IsNull(rect) then
        return
    end
    if GlobalFun.IsNull(texture) then
        return
    end
    local sizeDelta = rect.rect
    local texWidth = texture.width
    local texHeight = texture.height
    local rectX, rectY = sizeDelta.width, sizeDelta.height

    local newWidth, newHeight
    if preserveAspect then
        local aspectRatio = rectX / rectY
        local texAspectRatio = texWidth / texHeight

        if aspectRatio > texAspectRatio then
            newWidth = rectY * texWidth / texHeight
            newHeight = rectY
        else
            newWidth = rectX
            newHeight = rectX * texHeight / texWidth
        end
    else
        newWidth, newHeight = rectX, rectY
    end
    GlobalFun.Try(setScreenConstraint, rect, newWidth, newHeight)
end

function UIKit.ScreenConstraint(rect, coverTran)
    if GlobalFun.IsNull(rect) then
        return
    end
    local sizeDelta = rect.sizeDelta
    local rectX, rectY = sizeDelta.x, sizeDelta.y
    GlobalFun.Try(setScreenConstraint, rect, rectX, rectY, coverTran)
end

function UIKit.ScreenImageConstraint(image)
    if GlobalFun.IsNull(image) then
        return
    end
    local sprite = image.sprite
    if GlobalFun.IsNull(sprite) then
        return
    end
    local preserveAspect = image.preserveAspect
    local texture = sprite.texture
    GlobalFun.Try(setScreenTextureConstraint, image.transform, texture, preserveAspect)
end

function UIKit.ScreenRawImageConstraint(rawImage)
    if GlobalFun.IsNull(rawImage) then
        return
    end
    local texture = rawImage.texture
    GlobalFun.Try(setScreenTextureConstraint, rawImage.transform, texture)
end