local _ENV = Boli2Env

SceneLoader = {}
local this = SceneLoader
local _scenePathPrefix = "Assets/Art/Environment/Levels/"
local _scenePathExt = ".unity"
local function GetSceneAssetPath(sceneName)
    return _scenePathPrefix .. sceneName--.._scenePathExt
end

local function unloadSceneLoader(sceneName)
    GlobalAsset.Unload(sceneName)
end

local currUnLoadSceneCount = 0
local isUnloading = false

-- Editor 模式下 ShaderFind的查询备忘，优化加载速度
local shaderFindBackUpTbl = {}

local function fixMaterialsInEditorMode()
    if not BattleTest.isEnabledCheckMat then
        return
    end
    local renders = UnityEngine.GameObject.FindObjectsOfType(UnityEngine.Renderer,true).Table
    for k, v in pairs(renders) do
        if shaderFindBackUpTbl[v] == nil then
            if GlobalFun.GetType(  v.gameObject,nil,"XDPlugin.OGR.GrassRender") == nil then
                shaderFindBackUpTbl[v] = v
                for k1, v1 in pairs(v.materials.Table) do
                    GlobalFun.CheckMatShader(v1)
                end
            end
        end
    end
    local skybox = UnityEngine.RenderSettings.skybox
    if skybox ~= nil then
        GlobalFun.CheckMatShader(skybox)
    end
end

local function GetAssetPath(id)
    local sceneName = MapConfig.GetBlockSceneName(id)
    local blockGroup=MapConfig.GetBlockGroupConfigById(id)
    local sceneAssetName = GetSceneAssetPath(sceneName)
    local assetPath = sceneAssetName
    local sceneNameArr
    local sceneNamePrefix = ""
    if blockGroup and blockGroup.sceneNamePrefix then
        sceneNamePrefix=blockGroup.sceneNamePrefix
        assetPath = _scenePathPrefix .. sceneNamePrefix
    end
    if blockGroup and blockGroup.sceneNameSuffix then
        sceneNameArr=blockGroup.sceneNameSuffix
    end
    --if id == 201 or id == 203 or id == 204 or id == 205 or id == 206 or id == 207 then
    --    sceneNamePrefix = "Level_001"
    --    assetPath = _scenePathPrefix .. sceneNamePrefix
    --    sceneNameArr = { sceneNamePrefix .. "_1", sceneNamePrefix .. "_2", sceneNamePrefix .. "_3", sceneNamePrefix .. "_4", sceneNamePrefix .. "_5", sceneNamePrefix .. "_Boss" }
    --end
    --if id == 221 or id == 222 or id == 223 or id == 224 or id == 225 or id == 226 then
    --    sceneNamePrefix = "Level_002"
    --    assetPath = _scenePathPrefix .. sceneNamePrefix
    --    sceneNameArr = { sceneNamePrefix .. "_1", sceneNamePrefix .. "_2", sceneNamePrefix .. "_3", sceneNamePrefix .. "_4", sceneNamePrefix .. "_5", sceneNamePrefix .. "_Boss" }
    --end
    --if id == 241 or id == 242 or id == 243 or id == 244 or id == 245 or id == 246 then
    --    sceneNamePrefix = "Level_003"
    --    assetPath = _scenePathPrefix .. sceneNamePrefix
    --    sceneNameArr = { sceneNamePrefix .. "_1", sceneNamePrefix .. "_2", sceneNamePrefix .. "_3", sceneNamePrefix .. "_4", sceneNamePrefix .. "_5", sceneNamePrefix .. "_Boss" }
    --end
    --if id == 261 or id == 262 or id == 263 or id == 264 or id == 265 or id == 266 then
    --    sceneNamePrefix = "Level_004"
    --    assetPath = _scenePathPrefix .. sceneNamePrefix
    --    sceneNameArr = { sceneNamePrefix .. "_1", sceneNamePrefix .. "_2", sceneNamePrefix .. "_3", sceneNamePrefix .. "_4", sceneNamePrefix .. "_5", sceneNamePrefix .. "_Boss" }
    --end
    --if id == 281 or id == 282 or id == 283 or id == 284 or id == 285 or id == 286 then
    --    sceneNamePrefix = "Level_002"
    --    assetPath = _scenePathPrefix .. sceneNamePrefix
    --    sceneNameArr = { sceneNamePrefix .. "_6", sceneNamePrefix .. "_7", sceneNamePrefix .. "_8", sceneNamePrefix .. "_9", sceneNamePrefix .. "_10", sceneNamePrefix .. "_Boss" }
    --end
    --副本
    if sceneNameArr == nil then
        sceneNameArr = { sceneName }
    end
    return assetPath, sceneNameArr, sceneNamePrefix
end
--场景是否加载
local function IsSceneLoaded(sceneName)
    local scene = SceneManager.GetSceneByName(sceneName)
    if scene then
        return scene.isLoaded
    end
    return false
end

local function LoadSceneAsyncByBundleName_old(_path, _sceneName, _onLoading, _loadCompleted, _loadMode)
    local co = coroutine.create(function(_sceneLoadPath)
        local asyncOperation = SceneManager.LoadSceneAsync(_sceneLoadPath, _loadMode)
        asyncOperation.allowSceneActivation = false
        while not asyncOperation.isDone do
            Yield(WaitForEndOfFrame())
            local progress = 0
            if asyncOperation.progress >= 0.89 then
                progress = 1
                asyncOperation.allowSceneActivation = true
            else
                progress = asyncOperation.progress
            end
            if _onLoading ~= nil then
                _onLoading(progress)
            end
        end
        if _loadCompleted ~= nil then
            _loadCompleted()
        end
        if isEditor then
            fixMaterialsInEditorMode()
        end
    end)
    GlobalAsset.Load(_path, function(abTab, ab)
        coroutine.resume(co, _sceneName)
    end, true)
end
local function LoadSceneAsyncByBundleName(_path, _sceneName, _onLoading, _loadCompleted, _loadMode)
        GlobalAsset.Load(_path, function(abTab, ab)
            SceneManager.LoadScene(_sceneName, _loadMode)
            GlobalFun.InvokeByFrame(function()
                if _loadCompleted ~= nil then
                    _loadCompleted()
                    if isEditor then
                        fixMaterialsInEditorMode()
                    end
                end
            end)
        end, true)
end

--region lifecycle
function this.StartAction()

end
--endregion

--region 加载

local function LoadBlockDependAssetsArr(nameArr, onCompleted)
    local loadedCount = 0
    for i, v in ipairs(nameArr) do
        if v[2] == BlockDependAssetLoadOption.NORMAL or v[2] == BlockDependAssetLoadOption.ONLY_LOAD then
            GlobalAsset.Load(v[1], function(abTab, ab)
                loadedCount = loadedCount + 1
                if loadedCount == #nameArr then
                    if onCompleted then
                        onCompleted()
                    end
                end
            end, true)
        else
            loadedCount = loadedCount + 1
            if loadedCount == #nameArr then
                if onCompleted then
                    onCompleted()
                end
            end
        end
    end
end
local function LoadAllBlockSceneByAsset_old(assetPath, sceneNameArr, onLoadSceneCompleted, loadMode)
    GlobalAsset.Load(assetPath, function(abTab, ab)
        -- if flag then
        BattleMapLog("_LoadSceneAsync LoadAsset", assetPath)
        local co = coroutine.create(function()
            BattleMapLog("_LoadSceneAsync LoadScene")
            for _, v in ipairs(sceneNameArr) do
                local asyncOperation = SceneManager.LoadSceneAsync(v, loadMode)
                asyncOperation.allowSceneActivation = false
                while not asyncOperation.isDone do
                    Yield(WaitForEndOfFrame())
                    local progress = 0
                    if asyncOperation.progress >= 0.89 then
                        progress = 1
                        asyncOperation.allowSceneActivation = true
                    else
                        progress = asyncOperation.progress
                    end
                    --if _onLoading ~= nil then
                    --    _onLoading(progress)
                    --end
                end
            end
            if isEditor then
                fixMaterialsInEditorMode()
            end
            if onLoadSceneCompleted ~= nil then
                onLoadSceneCompleted()
            end
        end)
        coroutine.resume(co)
        -- end
    end, true)
end
local function LoadAllBlockSceneByAsset(assetPath, sceneNameArr, onLoadSceneCompleted, loadMode)
    GlobalAsset.Load(assetPath, function(abTab, ab)
        GlobalFun.Print(assetPath,sceneNameArr)
        for i, v in ipairs(sceneNameArr) do
            SceneManager.LoadScene(v,loadMode)
        end
        GlobalFun.InvokeByFrame(function()
            if onLoadSceneCompleted ~= nil then
                onLoadSceneCompleted()
            end
            if isEditor then
                fixMaterialsInEditorMode()
            end
        end)
    end, true)
end

function SceneLoader.LoadSceneAsyncByName(_path, _onLoading, _loadCompleted, _loadMode)
    local dependAssetsBundleNameArr = MapConfig.GetMapDependAssetNameArrByMapName(_path)
    if dependAssetsBundleNameArr then
        LoadBlockDependAssetsArr(dependAssetsBundleNameArr, function()
            local needLoadAbName = GetSceneAssetPath(_path)
            LoadSceneAsyncByBundleName(needLoadAbName, _path, _onLoading, _loadCompleted, _loadMode)
        end)
    else
        local needLoadAbName = GetSceneAssetPath(_path)
        LoadSceneAsyncByBundleName(needLoadAbName, _path, _onLoading, _loadCompleted, _loadMode)
    end
end

--加载所有区块 根据其中一个Id
function SceneLoader.LoadAllBlockAsyncByOneId(id, onLoadCompleted, loadMode)
    local assetPath, sceneNameArr = GetAssetPath(id)
    BattleMapLog("_LoadSceneAsync", assetPath)
    local dependAssetsBundleNameArr = MapConfig.GetBlockGroupDependAssetNameArrByBlockId(id)
    if not GlobalFun.IsArrayNilOrEmpty(dependAssetsBundleNameArr) then
        LoadBlockDependAssetsArr(dependAssetsBundleNameArr, function()
            LoadAllBlockSceneByAsset(assetPath, sceneNameArr, onLoadCompleted, loadMode)
        end)
    else
        LoadAllBlockSceneByAsset(assetPath, sceneNameArr, onLoadCompleted, loadMode)
    end
end

--endregion

--region 卸载

--卸载AB
local function UnloadAssetBundleBySceneName(sceneName)
    local needLoadAbName = GetSceneAssetPath(sceneName)
    unloadSceneLoader(needLoadAbName)
end

function SceneLoader.UnloadSceneByNameArrAsync(sceneNameArr, needUnloadCount, unloadCompleted, needUnloadBundle)
    if isUnloading == false then
        currUnLoadSceneCount = 0
        isUnloading = true
    end
    if #sceneNameArr == 0 then
        return
    end
    local tbl = sceneNameArr
    local name = sceneNameArr[1]
    table.remove(tbl, 1)
    local co = coroutine.create(function(sceneName, sceneNameArr)
        local asyncOperation = SceneManager.UnloadSceneAsync(sceneName)
        while asyncOperation ~= nil and not asyncOperation.isDone do
            Yield(WaitForEndOfFrame())
        end
        if needUnloadBundle then
            --bundle卸载
            UnloadAssetBundleBySceneName(sceneName)
            --卸载依赖资源包
            local dependAssetsBundleNameArr = MapConfig.GetMapDependAssetNameArrByMapName(sceneName)
            if dependAssetsBundleNameArr then
                for i, v in ipairs(dependAssetsBundleNameArr) do
                    if v[2] == BlockDependAssetLoadOption.NORMAL or v[2] == BlockDependAssetLoadOption.ONLY_UNLOAD then
                        GlobalAsset.Unload(v[1])
                    end
                end
            end
        end
        currUnLoadSceneCount = currUnLoadSceneCount + 1
        if currUnLoadSceneCount == needUnloadCount then
            isUnloading = false
            if unloadCompleted ~= nil then
                unloadCompleted()
                Resources.UnloadUnusedAssets()
            end
        else
            SceneLoader.UnloadSceneByNameArr(tbl, needUnloadCount, unloadCompleted, needUnloadBundle)
        end
    end)
    coroutine.resume(co, name, tbl)
end
function SceneLoader.UnloadSceneByNameArr(sceneNameArr, needUnloadCount, unloadCompleted, needUnloadBundle)
    if isUnloading == false then
        currUnLoadSceneCount = 0
        isUnloading = true
    end
    if #sceneNameArr == 0 then
        return
    end
    local tbl = sceneNameArr
    local name = sceneNameArr[1]
    table.remove(tbl, 1)
    SceneExtension.UnloadScene(name)
    if needUnloadBundle then
        --bundle卸载
        UnloadAssetBundleBySceneName(name)
        --卸载依赖资源包
        local dependAssetsBundleNameArr = MapConfig.GetMapDependAssetNameArrByMapName(sceneName)
        if dependAssetsBundleNameArr then
            for i, v in ipairs(dependAssetsBundleNameArr) do
                if v[2] == BlockDependAssetLoadOption.NORMAL or v[2] == BlockDependAssetLoadOption.ONLY_UNLOAD then
                    GlobalAsset.Unload(v[1])
                end
            end
        end
    end
    currUnLoadSceneCount = currUnLoadSceneCount + 1
    if currUnLoadSceneCount == needUnloadCount then
        isUnloading = false
        if unloadCompleted ~= nil then
            unloadCompleted()
            Resources.UnloadUnusedAssets()
        end
    else
        SceneLoader.UnloadSceneByNameArr(tbl, needUnloadCount, unloadCompleted, needUnloadBundle)
    end
end
function SceneLoader.UnloadSceneBySceneObj(obj, id, onUnLoadCompleted, oneNeedLoadBLockId)
    --local sceneName = MapConfig.GetBlockSceneName(id)
    local co = coroutine.create(function(_obj)
        local asyncOperation = SceneManager.UnloadSceneAsync(_obj.scene)
        while not asyncOperation.isDone do
            Yield(WaitForEndOfFrame())
        end
        --每个区块卸载完后判断是否区块包中的区块都已卸载，则卸载ab包
        local assetPath, sceneNameArr, sceneNamePrefix = GetAssetPath(id)
        local isLoaded = false
        for i, v in ipairs(sceneNameArr) do
            if sceneNamePrefix then
                isLoaded = IsSceneLoaded(v)
            else
                isLoaded = IsSceneLoaded(assetPath .. "/" .. v)
            end
            if isLoaded then
                break
            end
        end
        if not isLoaded  then
            --都已卸载 
            GlobalAsset.Unload(assetPath)
            --卸载区块依赖包
            local dependAssetsBundleNameArr = MapConfig.GetBlockGroupDependAssetNameArrByBlockId(id)
            local needLoadDependAssetsBundleNameArr = nil
            if oneNeedLoadBLockId then
                needLoadDependAssetsBundleNameArr = MapConfig.GetBlockGroupDependAssetNameArrByBlockId(oneNeedLoadBLockId)
            end
            if dependAssetsBundleNameArr then
                for i, v in ipairs(dependAssetsBundleNameArr) do
                    local isExistedInNewScene = false
                    if needLoadDependAssetsBundleNameArr then
                        for i, _v in ipairs(needLoadDependAssetsBundleNameArr) do
                            if v[1] == _v[1] then
                                isExistedInNewScene = true
                                break
                            end
                        end
                    end
                    --不在新区块中存在则正常卸载
                    if not isExistedInNewScene then
                        if v[2] == BlockDependAssetLoadOption.NORMAL or v[2] == BlockDependAssetLoadOption.ONLY_UNLOAD then
                            GlobalAsset.Unload(v[1])
                        end
                        if v[2] == BlockDependAssetLoadOption.UNLOAD_TEXTURE_ATLAS then
                            GlobalAtlas.UnloadDynamicSpriteWithPrefixPath(GlobalAtlas.TextureType.LevelBg,v[1])
                        end
                    end
                end
            end
        end
        if onUnLoadCompleted ~= nil then
            onUnLoadCompleted()
        end
    end)
    coroutine.resume(co, obj)
end

--endregion

