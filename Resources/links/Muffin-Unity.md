#Muffin


```Csharp
//鼠标点击是否在某个 rect范围内
 RectTransformUtility.RectangleContainsScreenPoint(rect,mousePosition,  uiCamera)
 //鼠标点击到某个Canvas的local position
 RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, mousePosition, uiCamera)
```


FileTool.CropTextureFile
```Csharp
    public static bool CropTextureFile(string sourceFile, string destinationFile, string encode, int x, int y, int width, int height) {  
        if (!File.Exists(sourceFile)) {  
            return false;  
        }  

        var result = false;  
        byte[] fileData = System.IO.File.ReadAllBytes(sourceFile);  
        Texture2D texture = new Texture2D(2, 2);  
        texture.LoadImage(fileData);  
        int cropX = x;  
        int cropY = y;  
        int cropWidth = width;  
        int cropHeight = height;  
        Texture2D croppedTexture = new Texture2D(cropWidth, cropHeight);  
        Color[] pixels = texture.GetPixels(cropX, cropY, cropWidth, cropHeight);  
        croppedTexture.SetPixels(pixels);  
        croppedTexture.Apply();  
  
        // Save the cropped texture  
        byte[] croppedBytes = null;  
        if (encode.ToLower() == "jpg") {  
            croppedBytes = croppedTexture.EncodeToJPG();  
        }else if (encode.ToLower() == "png") {  
            croppedBytes = croppedTexture.EncodeToPNG();  
        }  
  
        if (croppedBytes != null) {  
            System.IO.File.WriteAllBytes(destinationFile, croppedBytes);  
            result = true;  
        }  
  
        return result;  
    }  
}
```

```lua
local function saveScreen()  
    local screenShootName = "share_out_image_facebook.png"  
    local textureName = "share_out_image_facebook_cropped.jpg"  
    local savePath = nil  
    if not isEditor then  
        savePath = FileTool.Path_Combine(UnityEngine.Application.persistentDataPath, screenShootName)  
    else  
        savePath = FileTool.Path_Combine(UnityEngine.Application.dataPath, "..", screenShootName)  
    end  
    local croppedTexturePath = FileTool.Path_Combine(UnityEngine.Application.persistentDataPath, textureName)  
      
    local sourceWidth = Screen.width-- math.min(Component.Content.sizeDelta.x, Screen.width)  
    local sourceHeight = math.floor(Screen.width / 1.7776) --math.min(Component.Content.sizeDelta.y, Screen.height)  
    local cropStartX = 0--math.floor((Screen.width / 2) - (sourceWidth / 2))  
    local cropStartY = math.floor((Screen.height / 2) - (sourceHeight / 2))  
  
    local co = coroutine.create(function()  
        UnityEngine.ScreenCapture.CaptureScreenshot(screenShootName)  
        Yield(WaitForSeconds(0.3))  
        local result = FileTool.CropTextureFile(savePath, croppedTexturePath, "jpg", cropStartX, cropStartY, sourceWidth, sourceHeight)  
        CloseUI()  
        Yield(WaitForSeconds(0.3))  
        if result then  
            local currentShareConfig = sharePlatformConfig[currentShareTo]  
            if currentShareConfig ~= nil then  
                if GameMain:GetPlatformSDKTrans():_have_ShareImage() and currentShareTo == "facebook" then  
                    --TimeMgr.AddTimer("SDKManager.ShareImage_success", 0.3, function()  
                    --    local currentShareDaily = GlobalData.roleData:GetShareDaily()                    --    if currentShareDaily > 0 then                    --        ActivitiesService.GetShareEventReward()                    --    end                    --end)                    
                    SDKManager.ShareImage(0, croppedTexturePath)  
                else  
                    local param = nil  
                    if getmetatable(currentShareConfig.csClass) ~= nil then  
                        param = currentShareConfig.csClass()  
                    else  
                        param = Slua.CreateClass(currentShareConfig.className)  
                    end  
                    if param ~= nil then  
                        if currentShareConfig.classProperties ~= nil then  
                            for k, v in pairs(currentShareConfig.classProperties) do  
                                param[k] = v  
                            end  
                        end                        param.ImageUrl = croppedTexturePath  
                        --print("------------------------->>1", sprint_table(param))  
                        SDKManager.Share(param, function()  
                            local currentShareDaily = GlobalData.roleData:GetShareDaily()  
                            if currentShareDaily > 0 then  
                                ActivitiesService.GetShareEventReward()  
                            end  
                        end, function()  
                            TipPlane.OpenTipPlane("LineDogNoApp")  
                        end)  
                    end  
                end            end        end    end)  
    coroutine.resume(co)  
end
```

```lua
local function cancelTween()  
    for _, tween in pairs(tweenHandler) do  
        if tween then  
            local id = tween.id  
            LeanTween.cancel(id)  
        end  
    end    tweenHandler = {}  
end

local function lookBlock(idx, immediate, callback, isLast, isRush)  
    GlobalFun.Try(cancelTween)  
  
    local animName, animTime = getPlayerAnimInfo(idx, isLast)  
  
    local blockItem = blockItems[idx]  
    local blockPos = blockItem.pos  
    local target = getBlockPos(idx)  
    local time = immediate and 0.0 or animTime  
    if isRush then  
        time = time * 0.35  
    end  
  
    tweenHandler.move1 = LeanTween.move(mapTran, target, time):setOnComplete(function()  
        GlobalFun.Try(cancelTween)  
        GlobalFun.Try(callback)  
    end)  
    tweenHandler.move3 = LeanTween.value(0.0, 1.0, time / 2):setOnComplete(function()  
        GlobalFun.Try(resetDirection, idx)  
    end)  
    local currentPos = playerTran.anchoredPosition  
    local targetPos = Vector2(blockPos.x, blockPos.y + 40)  
  
    SoundManager.PlayUISound("Play_UI_Activity_Island_Move")  
    tweenHandler.move2 = LeanTween.value(playerTran.gameObject, function(val)  
        UIKit.SetRectPos(playerTran, val.x, val.y)  
    end, currentPos, targetPos, time):setEase(LeanTweenType.easeOutCubic):setOnComplete(function()  
        SoundManager.PlayUISound("Play_UI_Activity_Island_Fall")  
    end)  
end
```

