using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.Sprites;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MiniJSON2;
using Newtonsoft.Json;

public class AssetBundleEditor : AssetPostprocessor
{
    public static BuildAssetBundleOptions buildOption = BuildAssetBundleOptions.DeterministicAssetBundle | BuildAssetBundleOptions.ChunkBasedCompression;

    [MenuItem("Assets/PackAb")]
    public static void BuildAssetBundle()
    {
        BuildAllAssetsBundlesWithPath(GetPlatformTarget());
    }

    [MenuItem("Edit/PackAbAndEnterGame &e")]
    public static void BuildAssetBundleAndRun()
    {
        BuildAllAssetsBundlesWithPath(GetPlatformTarget());
        EditorApplication.EnterPlaymode();
    }

    [MenuItem("Assets/打包/打包含脚本", false, 100)]
    public static void BuildAssetBundleWithLS()
    {
        BuildAllAssetsBundlesWithPath(GetPlatformTarget());
    }

    public static void BuildAllAssetsBundlesFromCmd() {
        CommandArgs args = CommandLine.Parse(Environment.GetCommandLineArgs());
        string target = args.ArgPairs["target"]; //ios, android
        string appendBuildABPath = args.ArgPairs["appendBuildABPath"];

        BuildTarget buildTarget = BuildTarget.NoTarget;

        switch (target) {
            case "ios":
                buildTarget = BuildTarget.iOS;
                break;
            case "android":
                buildTarget = BuildTarget.Android;
                break;
        }

        if (buildTarget != BuildTarget.NoTarget && appendBuildABPath != null) {
            AtlasProcessEditor.UpdateAtlas();
            LuaScriptsProcessEditor.GenerateLuaScriptTxt(false);
            bool result = BuildAllAssetsBundlesWithPath(buildTarget, appendBuildABPath);
            if (!result) {
                EditorApplication.Exit(998);
            }
            LuaScriptsProcessEditor.RemoveGeneratedLuaScriptTxt();
        }
    }

    public static bool BuildAllAssetsBundlesWithPath(BuildTarget target, string path = null) {
        bool result = true;
        
        AssetDatabase.Refresh();
        AssetDatabase.RemoveUnusedAssetBundleNames();
        AssetDatabase.Refresh();
        
        buildOption |= BuildAssetBundleOptions.DeterministicAssetBundle;
        BuildTarget buildTarget = target;
        if (path == null)
        {
            path = Path.Combine(Application.dataPath, "../_AssetsBundles/");
        }

        path = Path.Combine(path, buildTarget.ToString().ToUpper());
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        AssetDatabase.Refresh();

        //GenerateAssetMap();
        Fix(path);
        try
        {
            AssetBundleManifest resultManifest = BuildPipeline.BuildAssetBundles(path, buildOption, buildTarget);
            if (resultManifest == null) {
                result = false;
            }
        }
        catch(Exception e)
        {
            Debug.LogError("####" + e.ToString());
            return false;
        }

        AssetDatabase.Refresh();
        checkAssetBundleNamesValid();
        AssetDatabase.Refresh();
        AssetDatabase.RemoveUnusedAssetBundleNames();
        AssetDatabase.Refresh();
        deleteUnusedAssetBundles(path);

        return result;
    }

    static void Fix(string path) {
        HashSet<string> needFixPath = new HashSet<string>() {
            "/atlas/monster", "/plane/petdivine"
        };

        try
        {
            foreach (string needFix in needFixPath) {
                string needFixFullPath = path + needFix;
                if (File.Exists(needFixFullPath))
                {
                    File.Delete(needFixFullPath);
                }
            }
        }
        catch { 
        }
    }

    #region AssetBundle encrypted

    private static void preEncryptedAssetBundles(string path) {
        // delete old encrypted files
        foreach (var needEncryptedAssetBundleName in Config.NeedEncryptedAssetBundleNames) {
            var fi = new FileInfo(Path.Combine(path, needEncryptedAssetBundleName));
            if (fi.Exists) {
                fi.Delete();
            }
        
            var fiMeta = new FileInfo(Path.Combine(path, needEncryptedAssetBundleName + ".manifest"));
            if (fi.Exists) {
                fiMeta.Delete();
            }
        }
    }

    public static void PostEncryptedAssetBundles(string path) {
        foreach (var needEncryptedAssetBundleName in Config.NeedEncryptedAssetBundleNames) {
            var fi = new FileInfo(Path.Combine(path, needEncryptedAssetBundleName));
            if (fi.Exists) {
                var fileBytes = File.ReadAllBytes(fi.FullName);
                fi.Delete();
                using (var fs = File.OpenWrite(fi.FullName)) {
                    Debug.Log($"Encrypting AssetBundle: {fi.FullName} ...");
                    ToolText.EncryptAssetBundleBytes(fs, fileBytes);
                }
            }
        }
    }

    #endregion


    private static void deleteUnusedAssetBundles(string path) {
        var assetBundleNamesSet = AssetDatabase.GetAllAssetBundleNames();
        var directoryInfo = new DirectoryInfo(path);
        var directoryPathLength = (directoryInfo.FullName + "/").Length;
        var allFiles = Directory.GetFiles(directoryInfo.FullName, "*", SearchOption.AllDirectories);

        var needDeleteFiles = new List<FileInfo>();
        foreach (var filePath in allFiles) {
            var fileInfo = new FileInfo(filePath);
            
            if (string.IsNullOrEmpty(fileInfo.Extension)) {
                var asAssetBundleName = fileInfo.FullName.Substring(directoryPathLength).Replace('\\', '/');
            
                if (!asAssetBundleName.Equals(directoryInfo.Name) && !assetBundleNamesSet.Contains(asAssetBundleName)) {
                    needDeleteFiles.Add(fileInfo);
                }
            }
        }

        foreach (var needDeleteFile in needDeleteFiles) {
            var manifestFile = needDeleteFile.FullName + ".manifest";
            var manifestFileInfo = new FileInfo(manifestFile);
            if (manifestFileInfo.Exists) {
                manifestFileInfo.Delete();
            }

            if (needDeleteFile.Exists) {
                needDeleteFile.Delete();
            }
            
            Debug.Log($"Deleting Unused AssetBundle: {needDeleteFile.FullName} && 'manifest' file");
        }
    }
    
    // [MenuItem("Assets/CheckAssetsValid")]
    private static void checkAssetBundleNamesValid()
    {
        var checkTable = new Dictionary<string,string> { { " ","空格" } };
        string[] assetBundleNames = AssetDatabase.GetAllAssetBundleNames();
        var inValidAssetBundleNames = new Dictionary<string,List<string>>();
        foreach (string abName in assetBundleNames) {
            var chars = new List<string>();
            foreach (var kv in checkTable) {
                if (abName.Contains(kv.Key)) {
                    chars.Add(kv.Key);
                }
            }

            if (chars.Count > 0) {
                inValidAssetBundleNames.Add(abName,chars);
            }
        }

        if (inValidAssetBundleNames.Count > 0) {
            string errorString = "不合适AssetBundle名: \n";
            foreach (var inValidAssetBundleName in inValidAssetBundleNames) {
                string inValidCharsInName = "";
                foreach (string c in inValidAssetBundleName.Value) {
                    inValidCharsInName += checkTable[c] + ",";
                }

                string oneError = inValidAssetBundleName.Key + ", 包含: " + inValidCharsInName;
                errorString += oneError + "\n";
                Debug.LogError(oneError);
            }

            EditorUtility.DisplayDialog("资源包含不合适AssetBundle名",
                "请查看日志\n" + errorString,
                "Ok");

            Debug.LogError("Have Invalid AssetBundleNames!!!\n" + errorString);
        }
    }

    [MenuItem("Assets/资源/检查AssetBundle名")]
    public static void CheckAssetBundlesInFolderEditor() {
        List<string> invalidAssets = new List<string>();
        CheckAssetBundlesInFolder(invalidAssets);

        foreach (string assetPath in invalidAssets) {
            Debug.LogError($"资源: {assetPath} AssetBundle名为空，请检查！");
        }
    }
    
    public static void CheckAssetBundlesInFolderForCI() {
        // List<string> invalidAssets = new List<string>();
        // bool anyInvalid = CheckAssetBundlesInFolder(invalidAssets);
        //
        // foreach (string assetPath in invalidAssets) {
        //     Debug.LogError($"资源: {assetPath} AssetBundle名为空，请检查！");
        // }
        //
        // if (anyInvalid) {
        //     EditorApplication.Exit(123);
        // }
        
        //                 
        // var buildLangs = new List<LocaleIdentifier>() {
        //     new("zh-hant"),
        //     new("zh-CN"),
        // };
        // var newLocales = new List<Locale>();
        // var currentLocales = LocalizationSettings.AvailableLocales.Locales;
        // foreach (var lang in buildLangs) {
        //     foreach (var locale in currentLocales) {
        //         if (lang.Code == locale.Identifier.Code) {
        //             newLocales.Add(LocalizationSettings.AvailableLocales.GetLocale(lang));
        //         }
        //     }
        // }
        //
        // foreach (var currentLocale in currentLocales) {
        //     LocalizationEditorSettings.RemoveLocale(currentLocale);
        // }
        //     
        // foreach (var locale in newLocales) {
        //     LocalizationEditorSettings.AddLocale(locale);
        // }
    }
    
    public static bool CheckAssetBundlesInFolder(List<string> invalidAssets) {
        string[] folders = {
            "Prefabs/SKillEffect"
        };

        bool anyInvalid = false;
        if (invalidAssets != null) {
            foreach (string folderName in folders) {
                string folderDirectoryPath = Path.Combine(Application.dataPath, folderName);
                DirectoryInfo folderDirectory = new DirectoryInfo(folderDirectoryPath);

                if (folderDirectory.Exists) {
                    FileInfo[] assetsInfo = folderDirectory.GetFiles("*", SearchOption.TopDirectoryOnly);
                    foreach (FileInfo info in assetsInfo) {
                        string relativePath = Path.Combine("Assets", Path.GetRelativePath(Application.dataPath, info.FullName));
                        AssetImporter importer = AssetImporter.GetAtPath(relativePath);

                        if (importer != null && string.IsNullOrEmpty(importer.assetBundleName)) {
                            invalidAssets.Add(importer.assetPath);
                            anyInvalid = true;
                        }
                    }
                }
            }
        }

        return anyInvalid;
    }

    public static BuildTarget GetPlatformTarget()
    {
        BuildTarget platformTarget = BuildTarget.StandaloneOSX;
#if UNITY_IPHONE
        platformTarget = BuildTarget.iOS;
#endif
#if UNITY_ANDROID
        platformTarget = BuildTarget.Android;
#endif
#if UNITY_STANDALONE_WIN
        platformTarget = BuildTarget.StandaloneWindows64;
#endif
        return platformTarget;
    }

    //public static void GenerateAssetMap()
    //{
    //    var assetBundleNames = AssetDatabase.GetAllAssetBundleNames().OrderBy(item => item).ToList();
    //    var dataDict = new Dictionary<string, int>();
    //    foreach (var bundleName in assetBundleNames)
    //    {
    //        var assetsList = AssetDatabase.GetAssetPathsFromAssetBundle(bundleName).Select(item => item.ToLower()).ToArray();
    //        foreach (var asset in assetsList)
    //        {
    //            var index = assetBundleNames.FindIndex(item => item.Equals(bundleName));
    //            dataDict.Add(asset, index);
    //        }
    //    }

    //    dataDict = dataDict.OrderBy(item => item.Key).ToDictionary(item => item.Key, item => item.Value);
    //    var assetDeps = dataDict.Keys.ToDictionary(item => item, AssetDatabase.GetDependencies);

    //    var data = new AssetsMap {MapData = dataDict, BundleData = assetBundleNames, AssetDeps = assetDeps};
    //    var assetMap = JsonConvert.SerializeObject(data, Formatting.Indented);
    //    var relativePath = Path.Combine("Config/LocalConfig", "AssetsMap.json");
    //    var savePath = Path.Combine(Application.dataPath, relativePath);
    //    File.WriteAllText(savePath, assetMap);
    //    AssetDatabase.Refresh();
    //    var importer = AssetImporter.GetAtPath(Path.Combine("Assets", relativePath));
    //    importer.assetBundleName = "assetsmap";
    //    importer.SaveAndReimport();
    //}

    //class AssetsMap
    //{
    //    public Dictionary<string, int> MapData;
    //    public List<string> BundleData;
    //    public Dictionary<string, string[]> AssetDeps;
    //}

    [MenuItem("Assets/打包/刷新模型AB路径")]
    public static void RefreshModelAbPath()
    {
        UnityEngine.Object[] objArr = Selection.GetFiltered<UnityEngine.Object>(SelectionMode.DeepAssets);
        for (int i = 0; i < objArr.Length; i++)
        {
            UnityEngine.Object obj = objArr[i];
            string abName = string.Empty;
            int abState = 0;
            switch (obj.GetType().ToString())
            {
                case "UnityEngine.Texture2D":
                case "UnityEngine.GameObject":
                case "UnityEngine.AnimatorOverrideController":
                case "UnityEngine.Material":
                    abState = 1;
                    break;
                case "UnityEngine.AnimationClip":
                    abState = GetAnimationClipState(obj);
                    break;
            }
            if (abState != 0)
            {
                bool isLogin = false;
                if (abState == 2)
                {
                    isLogin = true;
                }
                string assetPath = AssetDatabase.GetAssetPath(obj);
                AssetImporter ai = AssetImporter.GetAtPath(assetPath);
                string assetBundleName = GetABPath(assetPath, isLogin);
                string[] abPathArr = AssetDatabase.GetAssetPathsFromAssetBundle(assetBundleName);
                bool hasAbPath = abPathArr.Length > 0;
                if (hasAbPath)
                {
                    ai.assetBundleName = assetBundleName;
                }
            }
        }
        AssetDatabase.Refresh();
    }

    static string GetABPath(string assetPath, bool isLoginPath)
    {
        if (isLoginPath)
        {
            return "plane/logingameplane";
        }
        string abName = assetPath.Replace("Assets/Models/", "");
        int subIndex = abName.LastIndexOf("/");
        abName = abName.Substring(0, subIndex);
        abName = abName.Replace(".prefab", "");
        return abName.ToLower();
    }

    static int GetAnimationClipState(UnityEngine.Object obj)
    {
        int abState = 0;
        string objName = obj.name.ToLower();
        if (objName.Contains("attack")
            || objName.Contains("use_skill")
            || objName.Contains("wait")
            || objName.Contains("walk")
            || objName.Contains("die")
            || objName.Contains("win")
             || objName.Contains("reborn")
             || objName.Contains("repel")
             || objName.Contains("stun")
             || objName.Contains("collection")
             || objName.Contains("hit")
             || objName.Contains("use_assault")
             || objName.Contains("show_anim")
             || objName.Contains("break_defense")
             )
        {
            abState = 1;
        }
        if (objName.Contains("battleshow")
            || objName.Contains("weaponswitch")
            || objName.Contains("xuanjue")
            // || objName.Contains("show_")
            || objName.Contains("timeline")
            )
        {
            abState = 2;
        }
        return abState;
    }



    //[MenuItem("Assets/清除Canvas", priority = 21)]
    //public static void ClearCanvas()
    //{
    //    GameObject[] objArr = Selection.GetFiltered<GameObject>(SelectionMode.Assets);
    //    for (int i = 0; i < objArr.Length; i++)
    //    {
    //        GameObject obj = objArr[i];
    //        MonoBehaviour.DestroyImmediate(obj.GetComponent<UnityEngine.Rendering.SortingGroup>(), true);
    //        //MonoBehaviour.DestroyImmediate(obj.GetComponent<UnityEngine.CanvasGroup>(), true);
    //        //MonoBehaviour.DestroyImmediate(obj.GetComponent<UnityEngine.UI.GraphicRaycaster>(), true);
    //        //MonoBehaviour.DestroyImmediate(obj.GetComponent<UnityEngine.Canvas>(), true);
    //        //MonoBehaviour.DestroyImmediate(obj.GetComponent<UnityEngine.CanvasRenderer>(), true);
    //    }
    //}

    [MenuItem("Assets/ProcessAssets/CheckPackAbError")]
    public static void CheckPackAbError() {
        string workDirName = Path.Combine(Application.dataPath, "../_testPackAb");
        DirectoryInfo workDir = new DirectoryInfo(workDirName);
        if (!workDir.Exists) {
            workDir.Create();
        }
        
        string[] abNames = AssetDatabase.GetAllAssetBundleNames();
        foreach (string abName in abNames) {
            AssetBundleBuild abBuild = new AssetBundleBuild();
            abBuild.assetBundleName = abName;
            abBuild.assetNames = AssetDatabase.GetAssetPathsFromAssetBundle(abName);
            
            AssetBundleManifest buildResult = BuildPipeline.BuildAssetBundles(workDir.FullName, new[] { abBuild }, buildOption, GetPlatformTarget());
            if (buildResult == null) {
                Debug.LogError($"PackAb: --------- {abName} ---------Error");
            }
            
            // break;
        }
        
        // if (workDir.Exists) {
        //     workDir.Delete(true);
        // }
    }

    [MenuItem("Assets/ProcessAssets/CheckAssetBundlePrefabsError")]
    public static void CheckAssetBundlePrefabsMissingReferenceEditor() {
        // Set the AssetBundle directory to be relative to the Unity project directory
        var baseAbPaths = new List<string>() {
            // Path.Combine(Application.dataPath, "../_AssetsBundles/IOS/fxmat"),
            Path.Combine(Application.dataPath, "../_AssetsBundles/IOS/fxshare"),
        };
        string assetBundleDirectory = Path.Combine(Application.dataPath, "../_AssetsBundles/IOS/fx");
        // var testAssetName = "Assets/Prefabs/SkillEffect/Accessory/FX_Assassin_YTM_Swimsuit_Weapon_001.prefab";
        // var testAssetObj = AssetDatabase.LoadAssetAtPath<GameObject>(testAssetName);
        // checkFXRefMatValidAction(testAssetName, testAssetObj, new List<string>());
        // return;
        
        CheckAssetBundlePrefabsReferenceDirectory(baseAbPaths, assetBundleDirectory, "checkFXRefMatValidAction");
    }

    public static void CheckAssetBundlePrefabsReferenceCmd() {
        var args = CommandLine.Parse(Environment.GetCommandLineArgs());
        var checklistbase = args.ArgPairs["checklistbase"];
        var checklist = args.ArgPairs["checklist"];
        var checkactions = args.ArgPairs["checkactions"];

        var baselist = checklistbase.Split(',');
        if (baselist == null) return;
        
        var lists = checklist.Split(',');
        if (lists == null) return;
        
        var actions = checkactions.Split(',');
        if (actions == null) return;

        if (lists.Length != actions.Length) return;

        var allSuccess = true;
        for (var index = 0; index < lists.Length; index++) {
            var directory = lists[index];
            var actionName = actions[index];
            var baseAbPaths = baselist[index].Split('#').ToList();

            allSuccess = allSuccess && CheckAssetBundlePrefabsReferenceDirectory(baseAbPaths, directory, actionName);
        }

        if (!allSuccess) {
            EditorApplication.Exit(669);
        }
    }

    private static CheckAssetBundleRefConfig checkAbConfig = null;

    [Serializable]
    private class CheckAssetBundleRefConfig {
        public List<string> checkFXRefMatValidActionIgnore;
    }

    private static void LoadCheckAssetBundleRefConfig() {
        // Load the JSON file from Resources folder
        TextAsset jsonFile = Resources.Load<TextAsset>("CheckAssetBundleRef");
        if (jsonFile != null) {
            // Parse the JSON content
            checkAbConfig = JsonUtility.FromJson<CheckAssetBundleRefConfig>(jsonFile.text);

            // Example usage of the loaded configuration
            if (checkAbConfig != null) {
                
            } else {
                Debug.LogError("Failed to parse configuration.");
            }
        } else {
            Debug.LogError("Could not find the config file in Resources.");
        }
    }

    private static bool checkFXRefMatValidAction(AssetBundle ab, string assetName, List<string> errorPaths) {
        if (checkAbConfig.checkFXRefMatValidActionIgnore.Contains(assetName)) {
            return true;
        }
        
        if (ab == null) {
            return true;
        }

        if (!ab.Contains(assetName)) {
            Debug.LogError($"Failed to find asset {assetName} in AssetBundle {ab.name}");
            return true;
        }

        var loadedAsset = ab.LoadAsset<GameObject>(assetName);
        if (loadedAsset == null) {
            Debug.LogError($"Failed to load asset {assetName} in AssetBundle {ab.name}");
            return true;
        }
        
        var go = MonoBehaviour.Instantiate(loadedAsset);
        if (go == null) {
            Debug.LogError($"Failed to Instantiate asset {assetName} in AssetBundle {ab.name}");
            return true;
        }

        var result = true;
        var checkType = "PPtr<Material>"; // Material
        var goTransform = go.transform;
        var particleSystems = go.GetComponentsInChildren<ParticleSystem>();
        foreach (var particleSystem in particleSystems) {
            if (!particleSystem.gameObject.activeSelf) {
                continue;
            }

            var renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
            if (renderer == null || !renderer.enabled || renderer.renderMode == ParticleSystemRenderMode.None 
                || !particleSystem.emission.enabled 
                || !particleSystem.shape.enabled) {
                continue;
            }

            var particleRelativePath = particleSystem.name;
            var t = particleSystem.transform;
            while (t.parent != null && t.parent != goTransform) {
                particleRelativePath = t.parent.name + "/" + particleRelativePath;
                t = t.parent;
            }

            SerializedObject so = new SerializedObject(renderer);
            var sp = so.GetIterator();

            var noMissing = true;
            while (sp.NextVisible(true)) {
                if (sp.propertyType == SerializedPropertyType.ObjectReference) {
                    if (sp.objectReferenceValue == null && sp.objectReferenceInstanceIDValue != 0 && sp.type == checkType) {
                        noMissing = false;
                        var errorPath = $"Prefab: {assetName}, GameObject: {particleRelativePath}, Component: {particleSystem.GetType()}/{renderer.GetType()}, Missing: {checkType}.";
                        errorPaths.Add(errorPath);
                    }
                }
            }

            result = result && noMissing;
        }
        
        // if(result) {
            MonoBehaviour.DestroyImmediate(go);
        // }
        return result;
    }

    private static bool CheckAssetBundlePrefabsReferenceDirectory(List<string> baseAbNames, string assetBundleDirectory, string checkFuncName) {
        LoadCheckAssetBundleRefConfig();
        
        List<AssetBundle> baseAbs = new List<AssetBundle>();
        foreach (var abName in baseAbNames) {
            var assetBundle = AssetBundle.LoadFromFile(abName);
            baseAbs.Add(assetBundle);
        }
        
        Type thisType = typeof(AssetBundleEditor);
        MethodInfo checkMethod = thisType.GetMethod(checkFuncName, BindingFlags.Static | BindingFlags.NonPublic);
        if (checkMethod == null) {
            Debug.LogError($"CheckFunction: {checkFuncName} not found!");
            return true;
        }
        
        List<string> missingAssets = new List<string>();

        // Get all AssetBundle files in the directory, excluding .manifest and .meta files
        string[] assetBundleFiles = Directory.GetFiles(assetBundleDirectory, "*", SearchOption.AllDirectories);
        List<string> filteredAssetBundleFiles = new List<string>();

        // Filter out .manifest and .meta files
        foreach (var file in assetBundleFiles) {
            if (!file.EndsWith(".manifest") && !file.EndsWith(".meta") && !file.EndsWith(".DS_Store")) {
                filteredAssetBundleFiles.Add(file);
            }
        }

        var logInfo = string.Empty;
        foreach (var assetBundlePath in filteredAssetBundleFiles) {
            // Load AssetBundle
            AssetBundle assetBundle = AssetBundle.LoadFromFile(assetBundlePath);
            if (assetBundle == null) {
                Debug.LogWarning($"Failed to load AssetBundle: {assetBundlePath}");
                continue;
            }

            // Get all asset names in this AssetBundle
            string[] assetNames = assetBundle.GetAllAssetNames();
            foreach (string assetName in assetNames) {
                var result = new List<string>();
                var objName = Path.GetFileNameWithoutExtension(assetName).ToLower();
                var valid = (bool) checkMethod.Invoke(null, new object[] { assetBundle, objName, result });
                
                if (!valid) {
                    missingAssets.Add(assetName);
                    foreach (var errorInfo in result) {
                        logInfo += errorInfo + Environment.NewLine + Environment.NewLine;
                    }
                }
            }

            assetBundle.Unload(true);
        }

        for (int i = baseAbs.Count - 1; i >= 0; i--) {
            baseAbs[i].Unload(true);
        }

        // Output missing assets if any
        if (missingAssets.Count > 0) {
            Debug.LogError(logInfo);
            return false;
        } else {
            Debug.Log("No missing assets found. All references are valid.");
        }
        
        return true;
    }
}