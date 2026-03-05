local _ENV = Boli2Env
HeadFrameManager = {}
HeadFrameType = {
    Normal = { 1, 1, 1 },
    SetView = { 0.45, 0.45, 0.45 },
    MenuPlayersView = { 0.33, 0.33, 0.33 }, --主界面队友头像
    PropItem = { 0.36, 0.36, 0.36 }, --道具图标
    ChatView = { 0.3, 0.3, 0.3 },
    SelectRoleView={0.75,0.75,0.75}
}
local headFrameAbPath = "plane/avatar/dynamicheadframe/"
local resInsIdleTable = {
    --[id1]={}
    --[id2]={}
}
local resInsPool = {

    --[key1]={
    --[id1]={}
    --[id2]={}
    --}
}
local resInsIDPool={
    --[id]={
    --ins 
    --},
    --[id2]={
    --ins
    --}
}

local function SetDynamicHeadFrame(viewKey, id, assetName, dynamicTra, viewType)
    if not resInsPool[viewKey] then
        resInsPool[viewKey] = {}
    end
    if not resInsPool[viewKey][id] then
        resInsPool[viewKey][id] = {}
    end
    if not resInsIdleTable[id] or #resInsIdleTable[id] == 0 then
        GlobalAsset.Load(headFrameAbPath..assetName, function(abtab, ab)
            local asset = ab:LoadAsset(assetName)
            local headframeIns = GlobalFun.Instantiate(asset)
            GlobalFun.CheckMat(headframeIns)
            GlobalFun.CheckUIImageMat(headframeIns)
            if dynamicTra then
                GlobalFun.SetParent(headframeIns.transform, dynamicTra, true)
            end
            TransformExtension.SetLocalScale(headframeIns.transform, viewType[1], viewType[2], viewType[3])
            table.insert(resInsPool[viewKey][id], headframeIns)
        end)
    else
        if dynamicTra then
            local idleIns = resInsIdleTable[id][1]
            GlobalFun.SetObj(idleIns.gameObject, true)
            table.remove(resInsIdleTable[id], 1)
            table.insert(resInsPool[viewKey][id], idleIns)
            GlobalFun.SetParent(idleIns.transform, dynamicTra, true)
            TransformExtension.SetLocalScale(idleIns.transform, viewType[1], viewType[2], viewType[3])
        end
    end
end
local function SetDynamicHeadFrame2(id, assetName, dynamicTra, viewType)
    if not resInsIDPool[id] then
        resInsIDPool[id] = {}
    end
    if not resInsIdleTable[id] or #resInsIdleTable[id] == 0 then
        GlobalAsset.Load(headFrameAbPath..assetName, function(abtab, ab)
            local asset = ab:LoadAsset(assetName)
            local headframeIns = GlobalFun.Instantiate(asset)
            GlobalFun.CheckMat(headframeIns)
            GlobalFun.CheckUIImageMat(headframeIns)
            if dynamicTra then
                GlobalFun.SetParent(headframeIns.transform, dynamicTra, true)
            end
            TransformExtension.SetLocalScale(headframeIns.transform, viewType[1], viewType[2], viewType[3])
            table.insert(resInsIDPool[id], headframeIns)
        end)
    else
        if dynamicTra then
            local idleIns = resInsIdleTable[id][1]
            GlobalFun.SetObj(idleIns.gameObject, true)
            table.remove(resInsIdleTable[id], 1)
            table.insert(resInsIDPool[id], idleIns)
            GlobalFun.SetParent(idleIns.transform, dynamicTra, true)
            TransformExtension.SetLocalScale(idleIns.transform, viewType[1], viewType[2], viewType[3])
        end
    end
end

function HeadFrameManager.ReleaseDynamicHeadFrame(dynamicTra)
    if dynamicTra.transform.childCount > 0 then
        local ins = dynamicTra:GetChild(0)
        BattleMapAction.AddSceneChild(ins)
        GlobalFun.SetObj(ins.gameObject, false)
        for key, idInsPair in pairs(resInsPool) do
            for id, tbl in pairs(idInsPair) do
                for i, v in ipairs(tbl) do
                    if v == ins.gameObject then
                        table.remove(tbl, i)
                        if not resInsIdleTable[id] then
                            resInsIdleTable[id] = {}
                        end
                        table.insert(resInsIdleTable[id], ins.gameObject)
                        --GlobalFun.Print("111", resInsPool)
                        --GlobalFun.Print("111", resInsIdleTable)
                        return
                    end
                end
            end
        end
        for id, tbl in pairs(resInsIDPool) do
                for i, v in ipairs(tbl) do
                    if v == ins.gameObject then
                        table.remove(tbl, i)
                        if not resInsIdleTable[id] then
                            resInsIdleTable[id] = {}
                        end
                        table.insert(resInsIdleTable[id], ins.gameObject)
                        return
                    end
                end
        end
    end
end

function HeadFrameManager.ReleaseDynamicHeadFrameWithKey(key)
    local tbl = resInsPool[key]
    if tbl then
        for id, insTbl in pairs(tbl) do
            for i, ins in ipairs(insTbl) do
                BattleMapAction.AddSceneChild(ins)
                GlobalFun.SetObj(ins.gameObject, false)
                if not resInsIdleTable[id] then
                    resInsIdleTable[id] = {}
                end
                table.insert(resInsIdleTable[id], ins.gameObject)
            end
            tbl[id] = {}
        end
    end
end

function HeadFrameManager.SetHeadImageWithID(img, id, sex)
    if not id or id == 0 then
        return
    end
    local conf = RoleStyleHeadConf.GetConf(id)
    if conf then
        img.enabled = true
        local icoStr = conf.icon[1]
        if #conf.icon > 1 then
            icoStr = sex and conf.icon[sex + 1] or conf.icon[1]
        end
        GlobalAtlas.SetHeadIcon(img, icoStr)
    end
end

function HeadFrameManager.SetHeadFrameImageWithID(viewKey, img, id, dynamicTra, viewType)
    if not id or id == 0 then
        GlobalFun.SetObj(img.gameObject, false)
        return
    end
    GlobalFun.SetObj(img.gameObject, true)
    local conf = RoleStyleHeadConf.GetConf(id)
    if conf then
        if conf.isDynamic == 1 then
            img.enabled = false
            if dynamicTra  then
                HeadFrameManager.ReleaseDynamicHeadFrame(dynamicTra)
            end
            if viewKey then
                SetDynamicHeadFrame(viewKey, id, conf.icon[1], dynamicTra, viewType)
                else
                SetDynamicHeadFrame2( id, conf.icon[1], dynamicTra, viewType)
            end
        else
            img.enabled = true
            GlobalAtlas.SetHeadIcon(img, conf.icon[1])
            if dynamicTra then
                HeadFrameManager.ReleaseDynamicHeadFrame(dynamicTra)
            end
        end
    end
end

function HeadFrameManager.Dispose()
    local usedIdTbl = {}
    for key, idPair in pairs(resInsPool) do
        for id, v in pairs(idPair) do
            if #v > 0 then
                usedIdTbl[#usedIdTbl + 1] = id
            end
        end
    end
    for id, insTbl in pairs(resInsIDPool) do
        if #insTbl > 0 then
            usedIdTbl[#usedIdTbl + 1] = id
        end
    end
    for i, v in pairs(resInsIdleTable) do
        local isUsed = false
        for _i, usedID in ipairs(usedIdTbl) do
            if usedID == i then
                isUsed = true
                break
            end
        end
        if not isUsed then
            for i, obj in ipairs(v) do
                MonoBehaviour.DestroyImmediate(obj)
            end
            resInsIdleTable[i] = {}
            local conf = RoleStyleHeadConf.GetConf(i)
            if conf then
                GlobalAsset.Unload(headFrameAbPath .. conf.icon[1])
            end
        end
    end
end 