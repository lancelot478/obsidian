#Muffin


*根据viewPos获得UiPos(位移变换)*
```lua
function GlobalFun.GetUiPosWithView(pos)  
    -- local rect = BasePlane.GetUiCanvasRect()  
    -- local cam = BasePlane.GetUiCamera()    
    -- local _, uiPos = RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, pos, cam, pos)    
    -- return uiPos    pos.x = pos.x * SettingsData.uiWidth - SettingsData.uiWidthHalf--此修改主要以性能优化角度出发 非必要情况不使用RectTransformUtility.ScreenPointToLocalPointInRectangle  
    pos.y = pos.y * SettingsData.uiHeight - SettingsData.uiHeightHalf  
    return pos  
end
```

*worldPos转换为viewPos(world => camera => projection)*
```lua
function GlobalFun.WorldToViewportPointXYZ(x, y, z)  
    --世界坐标转视口坐标  
    local matrixVpInfo = BattleCamera.matrixVpInfo  
	if matrixVpInfo.canRefresh then  
	    matrixVpInfo.canRefresh = false  
	    local cam = BattleCamera.GetCamera()  
	    if cam == nil then  
	        return    
		end    
	    local matixVp = cam.projectionMatrix * cam.worldToCameraMatrix  
	    matrixVpInfo.m00 = matixVp.m00  
	    matrixVpInfo.m01 = matixVp.m01  
	    matrixVpInfo.m02 = matixVp.m02  
	    matrixVpInfo.m03 = matixVp.m03  
	    matrixVpInfo.m10 = matixVp.m10  
	    matrixVpInfo.m11 = matixVp.m11  
	    matrixVpInfo.m12 = matixVp.m12  
	    matrixVpInfo.m13 = matixVp.m13  
	    matrixVpInfo.m20 = matixVp.m20  
	    matrixVpInfo.m21 = matixVp.m21  
	    matrixVpInfo.m22 = matixVp.m22  
	    matrixVpInfo.m23 = matixVp.m23  
	    matrixVpInfo.m30 = matixVp.m30  
	    matrixVpInfo.m31 = matixVp.m31  
	    matrixVpInfo.m32 = matixVp.m32  
	    matrixVpInfo.m33 = matixVp.m33  
	end 
    if matrixVpInfo == nil then  
        return    
	end    
	local x1 = matrixVpInfo.m00 * x + matrixVpInfo.m01 * y + matrixVpInfo.m02 * z + matrixVpInfo.m03  
    local y1 = matrixVpInfo.m10 * x + matrixVpInfo.m11 * y + matrixVpInfo.m12 * z + matrixVpInfo.m13  
    local z1 = matrixVpInfo.m20 * x + matrixVpInfo.m21 * y + matrixVpInfo.m22 * z + matrixVpInfo.m23  
    local w1 = matrixVpInfo.m30 * x + matrixVpInfo.m31 * y + matrixVpInfo.m32 * z + matrixVpInfo.m33  
    x1 = 0.5 + 0.5 * x1 / w1;  
    y1 = 0.5 + 0.5 * y1 / w1;  
    z1 = w1  
    return x1, y1, z1  
end
```
