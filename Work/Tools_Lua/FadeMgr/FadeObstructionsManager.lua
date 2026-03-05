local _ENV = Boli2Env
FadeObstructionManager = {}
FadeObstructionManager.FadeOpen = false--功能开关
FadeObstructionManager.RayRadius = 0.25--检测球半径
FadeObstructionManager.MaxDistance = 12.34--扫描的最大距离
FadeObstructionManager.MaxDistanceHomeBuild = 1.0--扫描的最大距离
FadeObstructionManager.IsHomeBuild = false
FadeObstructionManager.isAvoidClosed = true --避让开关
FadeObstructionManager.isAvoid = false

local layerBuilding = LayerMask.NameToLayer("Buliding")
local layerTree = LayerMask.NameToLayer("Tree")
local fadeLayerMask = (1 << layerBuilding) | (1 << layerTree)--屏蔽检测层
local alphaMultiply = 8.0--alpha倍率
local hideInfoDic = {}
local lookTar
local logRayCastTime = 0
local nextRayCastTime = 0.05
local shareMatInfoDic = {}
local MatState = {NIL = 0, SHOW = 1, HIDE = 2}
local camDir = Vector3.zero
local ray

local function RefreshCamDir()
    local x1, y1, z1 = TransformExtension.GetPosition(lookTar)
    local x2, y2, z2 = TransformExtension.GetPosition(BattleCamera.GetCameraTra())
    camDir.x = x1 - x2
    camDir.y = y1 - y2
    camDir.z = z1 - z2
end

local function GetShareMatKey(render)
    return render.name .. render.sharedMaterial.name
end

local function RefreshMat(deltaTime)
    for _, v in pairs(shareMatInfoDic) do
        local sign
        local isShow = v.state == MatState.SHOW
        local isHide = v.state == MatState.HIDE
        if isShow then
            sign = 1.0
        end
        if isHide then
            sign = -1.0
        end
        if sign ~= nil then
            local alpha = v.alpha + deltaTime * sign * alphaMultiply
            if isHide and alpha <= 0 then
                v.state = MatState.SHOW
            end
            if isShow and alpha >= 1 then
                v.state = MatState.NIL
                for render in pairs(v.renderDic) do
                    render.sharedMaterials = v.sharedMaterials
                    v.renderDic[render] = nil
                end
            end
            if alpha < 0.0 then
                alpha = 0.0
            else
                if alpha > 1.0 then
                    alpha = 1.0
                end
            end
            v.alpha = alpha
            for _, k in ipairs(v.matArr) do
                k:SetFloat("_Alpha", alpha)
            end
        end
    end
end

local function RefreshFadeObstructions(hits)
    local index = 1
    while index <= #hits do
        local hit = hits[index]
        local obj = hit.collider.gameObject
        local hideInfo = hideInfoDic[obj]
        if hideInfo == nil then
            hideInfo = {}
            hideInfo.shareMatInfoArr = {}
            local renderArr = GlobalFun.GetComponentsInChildren(obj, UnityEngine.Renderer)
            local index = 1
            while index <= #renderArr do
                local render = renderArr[index]
                if render.sharedMaterial == nil then--移除不含材质的render
                    table.remove(renderArr, index)
                else
                    index = index + 1
                end
            end
            hideInfo.renderArr = renderArr
            for _, v in ipairs(hideInfo.renderArr) do
                local sharedMaterial = v.sharedMaterial
                local key = GetShareMatKey(v)
                local shareMatInfo = shareMatInfoDic[key]
                if shareMatInfo == nil then
                    shareMatInfo = {}
                    shareMatInfo.alpha = 1.0
                    shareMatInfo.renderDic = {}
                    shareMatInfo.matArr = {}
                    local sharedMaterials = v.sharedMaterials
                    shareMatInfo.sharedMaterials = sharedMaterials
                    for i = 1, #sharedMaterials do
                        local newMat = Material(sharedMaterial)
                        newMat:SetFloat("_SrcBlend", 5.0)
                        newMat:SetFloat("_DstBlend", 10.0)
                        newMat:SetInt("_Zwrite", 0)
                        newMat.renderQueue = 3000
                        table.insert(shareMatInfo.matArr, newMat)
                    end
                    shareMatInfoDic[key] = shareMatInfo
                end
                table.insert(hideInfo.shareMatInfoArr, shareMatInfo)
            end
            hideInfoDic[obj] = hideInfo
        end
        for _, v in ipairs(hideInfo.shareMatInfoArr) do
            v.state = MatState.HIDE
        end
        for _, v in ipairs(hideInfo.renderArr) do
            local shareMatInfo = shareMatInfoDic[GetShareMatKey(v)]
            if shareMatInfo.renderDic[v] == nil then
                v.materials = shareMatInfo.matArr
                shareMatInfo.renderDic[v] = 1
            end
        end
        index = index + 1
    end
end

function FadeObstructionManager.StartAction(looktar)
    lookTar = looktar
end

function FadeObstructionManager.UpdateAction()
    if not FadeObstructionManager.FadeOpen then
        return
    end
    local camTra = BattleCamera.GetCameraTra()
    if camTra == nil then
        return
    end
    if MainInterfaceData.IsInHomeBuildTab() then--营地无需此功能
        return
    end
    local deltaTime = GameMain:GetDeltaTime()
    if logRayCastTime > 0 then
        logRayCastTime = logRayCastTime - deltaTime
    end
    if logRayCastTime <= 0 then
        logRayCastTime = nextRayCastTime
        RefreshCamDir()
        if ray == nil then
            ray = Ray(camTra.position, camDir)
        else
            ray.origin = camTra.position
            ray.direction = camDir
        end
        local dis
        if FadeObstructionManager.IsHomeBuild then
            dis = FadeObstructionManager.MaxDistanceHomeBuild
        else
            dis = FadeObstructionManager.MaxDistance
        end
        local hits = Physics.SphereCastAll(ray, FadeObstructionManager.RayRadius, dis, fadeLayerMask).Table
        RefreshFadeObstructions(hits)
        RefreshMat(deltaTime)
    end
end

function FadeObstructionManager.Clear()
    hideInfoDic = {}
    for _, v in pairs(shareMatInfoDic) do
        for render in pairs(v.renderDic) do
            render.sharedMaterials = v.sharedMaterials
            v.renderDic[render] = nil
        end
    end
    shareMatInfoDic = {}
end