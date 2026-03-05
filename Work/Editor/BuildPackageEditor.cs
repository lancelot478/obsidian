using MiniJSON2;
using SAGA.Editor;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using PADPost;
using Unity.VisualScripting;
using UnityEditor;
#if UNITY_ANDROID
using UnityEditor.Android;
#endif
using UnityEngine;
#if SDK
using XD.SDK.Common.Editor;
#endif
using Debug = UnityEngine.Debug;

public enum GameConfigType
{
    innertest_remotebuild = 4,
    innertest = 3,
    test = 0,
    online = 1,
    tf = 2
}

public partial class BuildPackageEditor : Editor {
    private static DateTime _lastRequestTime = DateTime.Now;

    private static bool SimpleWebRequestSync(string url, string method,
        string credentials, ref string result) {
        bool status = false;
        try {
            // Create a request for the URL. 		
            var request = WebRequest.Create(url);
            request.Method = method;

            byte[] bytes = Encoding.ASCII.GetBytes(credentials);
            string base64 = Convert.ToBase64String(bytes);
            string authorization = string.Concat("Basic ", base64);
            request.Headers.Add("Authorization", authorization);

            // Get the response.
            var response = (HttpWebResponse)request.GetResponse();
            var dataStream = response.GetResponseStream();
            var reader = new StreamReader(dataStream);

            string responseFromServer = reader.ReadToEnd();
            result = responseFromServer;
            reader.Close();
            dataStream.Close();
            response.Close();
            status = true;
        }
        catch (Exception e) {
            result = $"WebRequest: {url} error, \n {e}";
        }
        return status;
    }

    [MenuItem("BuildPackage/RemoteBuild/Build Current Branch")]
    static public void RequestRemoteBuild() {
        // if ((DateTime.Now - _lastRequestTime).TotalSeconds < 10) {
        //     EditorUtility.DisplayDialog("Oops!", "Please Wait...", "ok");
        //     return;
        // }

        _lastRequestTime = DateTime.Now;

        string currentHead = Path.Combine(Application.dataPath, "../.git/HEAD");
        string branchName = File.ReadAllText(currentHead).Remove(0, 16).Trim();

        string jobUrl = $"http://172.26.166.251:8080/job/muffin-pipeline-{branchName}-develop";
        string statusUrl = jobUrl + "/api/json?pretty=true";
        string buildUrl = jobUrl + "/buildWithParameters";
        string credentialFileName = Path.Combine(Application.dataPath, "Editor/JenkinsTokenUnityPacker.txt");
        if (File.Exists(credentialFileName)) {
            string credentials = File.ReadAllText(credentialFileName);
            string result = "";
            if (SimpleWebRequestSync(statusUrl, "POST", credentials, ref result)) {
                var statusJson = new JSON();
                statusJson.serialized = result;

                // check remote is building now
                if (statusJson.ToString("color").EndsWith("_anime")) {
                    string info = "已有正在打包的任务，请等待...";
                    EditorUtility.DisplayDialog("Oops!", info, "ok");
                }
                else {
                    if (SimpleWebRequestSync(buildUrl, "POST", credentials, ref result)) {
                        if (result == string.Empty) {
                            EditorUtility.DisplayDialog("Success!", "打包请求已发送!", "ok");
                        }
                    }
                    else {
                        Debug.Log(result);
                    }
                }
            }else {
                Debug.Log(result);
            }
        }
        else {
            EditorUtility.DisplayDialog("!", $"{credentialFileName} 文件不存在", "ok");
        }
    }
    /*
     * Build CommandLine Args:
     *     buildTarget : ios, android
     *     outputPath : path
     *     buildType : GameConfigType
     *     versionName : 1.0
     *     buildCode : 1
     *     debugBuild : on, off
     *     appendBuildAB : true, false
     *     appendBuildABPath : path
     */
    static public void Build() {
        var args = CommandLine.Parse(Environment.GetCommandLineArgs());
        var target = args.ArgPairs["target"]; //ios, android
        string outputPath = args.ArgPairs["outputPath"];
        var
            gameConfigType =
                (GameConfigType)Enum.Parse(typeof(GameConfigType), args.ArgPairs["buildType"]); // splited with ';'
        string versionName = "1.0";
        if (args.ArgPairs.ContainsKey("versionName")) {
            versionName = args.ArgPairs["versionName"];
        }

        int buildCode = 1;
        if (args.ArgPairs.ContainsKey("buildCode")) {
            buildCode = int.Parse(args.ArgPairs["buildCode"]);
        }

        bool debug = false;
        if (args.ArgPairs.ContainsKey("debugBuild")) {
            debug = args.ArgPairs["debugBuild"] == "on";
        }

        bool appendBuildAb = true;
        if (args.ArgPairs.ContainsKey("appendBuildAB")) {
            appendBuildAb = args.ArgPairs["appendBuildAB"] == "true";
        }

        string appendBuildAbPath = Path.Combine(Application.dataPath, "/../_AssetsBundles/");
        if (args.ArgPairs.ContainsKey("appendBuildABPath")) {
            appendBuildAbPath = args.ArgPairs["appendBuildABPath"];
        }

        string productName = null;
        if (args.ArgPairs.ContainsKey("productName")) {
            productName = args.ArgPairs["productName"];
        }
        
        // bool buildAndroidBundle = false;
        // if (args.ArgPairs.TryGetValue("isGoogle", out var value)) {
        //     buildAndroidBundle = value == "true";
        // }
        SimpleBuild(target, outputPath, versionName, buildCode, gameConfigType, debug, appendBuildAb,
            appendBuildAbPath, fromConsole: true, productName: productName);
    }

    /*
     * Build CommandLine Args:
     *     buildTarget : ios, android
     */
    static public void SwitchPlatform() {
        AssetDatabase.Refresh();
        EditorApplication.Exit(0);
    }
    [MenuItem("BuildPackage/LocalBuild(Skip PackAb)/iOS")]
    static public void SimpleBuildiOS_SkipPackAb() {
        string abPath = Path.Combine(Application.dataPath, "../_AssetsBundles");
        
        SimpleBuild("ios",
            Application.dataPath + "/../build_ios",
            "3.0", 1, GameConfigType.test, appendBuildAbPath: abPath);
    }
    [MenuItem("BuildPackage/LocalBuild(Skip PackAb)/Android")]
    static public void SimpleBuildAndroid_SkipPackAb() {
        string abPath = Path.Combine(Application.dataPath, "../_AssetsBundles");
        
        SimpleBuild("android",
            Application.dataPath + "/../build_android",
            "3.0", 1, GameConfigType.test, appendBuildAbPath: abPath);
    }
    [MenuItem("BuildPackage/LocalBuild(Skip PackAb)/iOS Debug")]
    static public void SimpleBuildiOSDebug_SkipPackAb() {
        string abPath = Path.Combine(Application.dataPath, "../_AssetsBundles");
       
        SimpleBuild("ios",
            Application.dataPath + "/../build_ios",
            "3.0", 1, GameConfigType.test, true, appendBuildAbPath: abPath);
    }
    [MenuItem("BuildPackage/LocalBuild(Skip PackAb)/Android Debug")]
    static public void SimpleBuildAndroidDebug_SkipPackAb() {
        string abPath = Path.Combine(Application.dataPath, "../_AssetsBundles");
       
        SimpleBuild("android",
            Application.dataPath + "/../build_android",
            "3.0", 1, GameConfigType.test, true, appendBuildAbPath: abPath);
    }
    [MenuItem("BuildPackage/LocalBuild/iOS")]
    static public void SimpleBuildiOS() {
        string abPath = Path.Combine(Application.dataPath, "../_AssetsBundles");
        if (AddResource)
        {
            LuaScriptsProcessEditor.GenerateLuaScriptTxt(false);
            AssetBundleEditor.BuildAllAssetsBundlesWithPath(BuildTarget.iOS, abPath);
        }
       

        SimpleBuild("ios",
            Application.dataPath + "/../build_ios",
            "3.0", 1, GameConfigType.test, appendBuildAbPath: abPath);
        
        LuaScriptsProcessEditor.RemoveGeneratedLuaScriptTxt();
    }
    
    [MenuItem("BuildPackage/LocalBuild/Android")]
    static public void SimpleBuildAndroid() {
        string abPath = Path.Combine(Application.dataPath, "../_AssetsBundles");
        if (AddResource)
        {
            LuaScriptsProcessEditor.GenerateLuaScriptTxt(false);
            AssetBundleEditor.BuildAllAssetsBundlesWithPath(BuildTarget.Android, abPath);
        }

        SimpleBuild("android",
            Application.dataPath + "/../build_android",
            "3.0", 1, GameConfigType.test, appendBuildAbPath: abPath);
        
        LuaScriptsProcessEditor.RemoveGeneratedLuaScriptTxt();
    }
    [MenuItem("BuildPackage/LocalBuild/iOS Debug")]
    static public void SimpleBuildiOSDebug() {
        string abPath = Path.Combine(Application.dataPath, "../_AssetsBundles");
        LuaScriptsProcessEditor.GenerateLuaScriptTxt(false);
        AssetBundleEditor.BuildAllAssetsBundlesWithPath(BuildTarget.iOS, abPath);
        
        SimpleBuild("ios",
            Application.dataPath + "/../build_ios",
            "3.0", 1, GameConfigType.test, true, appendBuildAbPath: abPath);
        
        LuaScriptsProcessEditor.RemoveGeneratedLuaScriptTxt();
    }
    [MenuItem("BuildPackage/LocalBuild/Android Debug")]
    static public void SimpleBuildAndroidDebug() {
        string abPath = Path.Combine(Application.dataPath, "../_AssetsBundles");
        LuaScriptsProcessEditor.GenerateLuaScriptTxt(false);
        AssetBundleEditor.BuildAllAssetsBundlesWithPath(BuildTarget.Android, abPath);
        
        SimpleBuild("android",
            Application.dataPath + "/../build_android",
            "3.0", 1, GameConfigType.test, true, appendBuildAbPath: abPath);
        
        LuaScriptsProcessEditor.RemoveGeneratedLuaScriptTxt();
    }
    

    [MenuItem("BuildPackage/LocalBuild/Windows")]
    static public void SimpleBuildWindows() {
        SimpleBuild("windows",
            Application.dataPath + "/../build_windows/" + PlayerSettings.productName + ".exe",
            "3.0", 1, GameConfigType.test);
    }
   
    static public void SimpleBuild(
        string target,
        string outputPath,
        string versionName,
        int buildCode,
        GameConfigType gameConfigType,
        bool debug = false,
        bool appendBuildAb = true,
        string appendBuildAbPath = null, bool fromConsole = false,
        string productName = null
        ) {
        try {
            if (string.IsNullOrEmpty(outputPath)) {
                return;
            }
            var pre = DateTime.UtcNow;
            AssetDatabase.Refresh();
            //       https://forum.unity.com/threads/problem-zipping-il2cpp-symbols-from-ci-build.561697/
            //       outputPath ends with '/' will cause problem
            //        Command Line Error:
            //        Unknown switch:
            outputPath = new DirectoryInfo(outputPath).FullName.TrimEnd('/', '\\');

            if (!string.IsNullOrEmpty(versionName)) {
                PlayerSettings.bundleVersion = versionName;
            }

            bool buildAndroidBundle = false;
#if SPLIT_PACK
            buildAndroidBundle = true;
#endif
            SDKPreSettingsBeforeBuild(target,buildAndroidBundle);

            InBuildPackage_HaoWan(target);
            var buildOptions = BuildOptions.None;
            var buildTarget = BuildTarget.iOS;
            var buildTargetGroup = BuildTargetGroup.iOS;

            if (target == "ios") {
                buildTarget = BuildTarget.iOS;
                buildTargetGroup = BuildTargetGroup.iOS;
                PlayerSettings.iOS.buildNumber = buildCode.ToString();
            }
           
            if (target == "android") {
                EditorUserBuildSettings.exportAsGoogleAndroidProject = true;
                EditorUserBuildSettings.buildAppBundle = PlatformSDKConfig.IsGooglePlay;
                EditorUserBuildSettings.androidCreateSymbols = AndroidCreateSymbols.Debugging;
#if UNITY_ANDROID
                AndroidExternalToolsSettings.maxJvmHeapSize = 8192;
#endif

                buildTarget = BuildTarget.Android;
                buildTargetGroup = BuildTargetGroup.Android;
                buildOptions |= BuildOptions.CompressWithLz4;
                PlayerSettings.Android.bundleVersionCode = buildCode;
                
            }

            if (target == "windows") {
                buildTarget = BuildTarget.StandaloneWindows64;
                buildTargetGroup = BuildTargetGroup.Standalone;

                EditorUserBuildSettings.selectedBuildTargetGroup = buildTargetGroup;
                EditorUserBuildSettings.selectedStandaloneTarget = buildTarget;
            }
            

            EditorHelper.SetSymbol(buildTargetGroup, "RELEASE_MODE", !debug);
            EditorHelper.SetSymbol(buildTargetGroup, "DEBUG_MODE", debug);

            if (debug) {
                buildOptions |= BuildOptions.Development;
                //buildOptions |= BuildOptions.EnableDeepProfilingSupport;
                //buildOptions |= BuildOptions.ConnectWithProfiler;
                //buildOptions |= BuildOptions.AllowDebugging;
            }

            if (target == "windows") {
                var buildFileInfo = new FileInfo(outputPath);
                if (buildFileInfo.Directory != null && buildFileInfo.Directory.Exists) {
                    buildFileInfo.Directory.Delete(true);
                }
            }
            else {
                if (Directory.Exists(outputPath)) {
                    Directory.Delete(outputPath, true);
                }

                Directory.CreateDirectory(outputPath);
            }

            // EditorUserBuildSettings.SwitchActiveBuildTarget(targetGroup,target);

            PlayerSettings.SplashScreen.show = false;
            if (!string.IsNullOrEmpty(productName)) {
                PlayerSettings.productName = productName;
            }

            AssetDatabase.Refresh();

            string destinationSoundBankFolder = string.Empty;
            string streamingAssetsAbPath = PrepareBuildAssets(pre, buildCode, appendBuildAb, appendBuildAbPath,
                buildTarget, out bool buildAbResult, ref destinationSoundBankFolder);

            if (!buildAbResult) {
                Debug.LogError("------------------->>>>BUILD PACKAGE: PACKAB ERROR!!!!!");

                if (fromConsole) {
                    EditorApplication.Exit(668);
                }
            }
            else {
                var levels = new List<string>();
                for (int i = 0; i < EditorBuildSettings.scenes.Length; i++) {
                    var scene = EditorBuildSettings.scenes[i];
                    if (!string.IsNullOrEmpty(scene.path) && scene.enabled) {
                        levels.Add(scene.path);
                    }
                }

                var playerOptions = new BuildPlayerOptions();
                playerOptions.options = buildOptions;
                playerOptions.scenes = levels.ToArray();
                playerOptions.target = buildTarget;
                playerOptions.targetGroup = buildTargetGroup;
                playerOptions.locationPathName = outputPath;
                BuildPipeline.BuildPlayer(playerOptions);
                
                // PlatformPostPackerEditor.OnAfterBuild(target, outputPath, gameConfigType);
                if (Directory.Exists(streamingAssetsAbPath)) {
                    Directory.Delete(streamingAssetsAbPath, true);
                }

                if (AddResource)
                {
                    DeleteSoundBanks(destinationSoundBankFolder);
                }
                AssetDatabase.Refresh();
#if UNITY_IPHONE
                PostBuildPackageEditor.iOSPostBuild(outputPath);
#endif
#if UNITY_ANDROID
                PostBuildPackageEditor.AndroidPostBuild(outputPath);
#endif
                EditorUtility.DisplayDialog("Build Complete.",
                    $"[Simple Build Completed! Cost {(DateTime.UtcNow - pre).TotalSeconds} Seconds!]",
                    "Done.");
            }

        }
        catch (Exception e) {
            Debug.LogError(e);
            if (fromConsole) {
                EditorApplication.Exit(666);
            }
        }
    }

    private static string PrepareBuildAssets(DateTime pre,
        int buildCode,
        bool appendBuildAb,
        string appendBuildAbPath,
        BuildTarget target, out bool buildResult, ref string destinationSoundBankFolder,
        bool isGoogle = false) 
    {
        if (!AddResource)
        {

            Directory.CreateDirectory(Path.Combine(Application.streamingAssetsPath, target.ToString().ToUpper()));
            buildResult = true;
            return string.Empty;
        }
        var buildInfo = new JSON();
        buildInfo["build"] = buildCode;

        var configInfo = new FileInfo(Path.Combine(Application.dataPath, "Resources/Configs/build_config.txt"));
        if (configInfo.Directory != null && !configInfo.Directory.Exists) {
            configInfo.Directory.Create();
        }

        File.WriteAllText(configInfo.FullName, buildInfo.serialized);

        string tempAssetBundlePath = Path.Combine(Application.dataPath, "../_AssetsBundles");
        if (appendBuildAb && appendBuildAbPath != null) {
            tempAssetBundlePath = appendBuildAbPath;
        }

        if (!appendBuildAb) {
            tempAssetBundlePath = tempAssetBundlePath + ".temp." + ToolText.GetTimeStampByDateTime(pre);
            if (Directory.Exists(tempAssetBundlePath)) {
                Directory.CreateDirectory(tempAssetBundlePath);
            }
        }
        string streamingAssetsAbPath =
            new DirectoryInfo(Path.Combine(Application.streamingAssetsPath, target.ToString().ToUpper())).FullName +
            Path.DirectorySeparatorChar;
      
      
        // LuaScriptsProcessEditor.PrePackLS();
        // LuaScriptsProcessEditor.GenerateLuaScriptTxt(false);
        buildResult = true;//AssetBundleEditor.BuildAllAssetsBundlesWithPath(target, path: tempAssetBundlePath);
        // LuaScriptsProcessEditor.PostPackLS();
        // LuaScriptsProcessEditor.GenerateLuaScriptTxt(true);
       
        
        //谷歌直接拷贝音频退出
        if (!Directory.Exists(streamingAssetsAbPath))
        {
            Directory.CreateDirectory(streamingAssetsAbPath);
        }
        
        #if SPLIT_PACK
            if(!UsingUnitySplitPack){
                FilesTool.DirectoryCopy(
                    Path.Combine(tempAssetBundlePath, target.ToString().ToUpper()),
                    streamingAssetsAbPath, 
                    true, true);
            }
        #else
             FilesTool.DirectoryCopy(
                        Path.Combine(tempAssetBundlePath, target.ToString().ToUpper()),
                        streamingAssetsAbPath, 
                        true, true);
             
             //控制包体在1.9G以内
             TextAsset hotUpdateEnableSetting = Resources.Load<TextAsset>("_CONSTS/HOT_UPDATE_ENABLED");
             var hpDisabled = hotUpdateEnableSetting != null && hotUpdateEnableSetting.text.Trim().Equals("false"); //关闭热更新的情况下
            

             if (AndroidPackageLimit2G && !hpDisabled && target == BuildTarget.Android)
             {
                 foreach (var folder in AndroidPackageLimit2G_FolderList)
                 {
                     var assetScenePath = Path.Combine(streamingAssetsAbPath,folder);
                     PADPostFileHelper.DeleteDirectory(assetScenePath);
                 }
                 foreach (var file in AndroidPackageLimit2G_FileList)
                 {
                     var assetScenePath = Path.Combine(streamingAssetsAbPath,file);
                     PADPostFileHelper.DeleteFile(assetScenePath);
                 }
             }
        #endif
      
        AssetBundleEditor.PostEncryptedAssetBundles(streamingAssetsAbPath);
        
        if (!appendBuildAb) {
            Directory.Delete(tempAssetBundlePath, true);
        }
       
        string[] allFiles = Directory.GetFiles(streamingAssetsAbPath, "*", SearchOption.AllDirectories);
        var packedMD5Info = new JSON();
        foreach (string filePath in allFiles) {
            var info = new FileInfo(filePath);
            if (info.Exists) {
                if (filePath.EndsWith(".manifest")) {
                    info.Delete();
                }
                else {
                    byte[] bytes = File.ReadAllBytes(filePath);
                    // ToolText.SetEncryptOrDecrypt(bytes);
                    var fileInfo = new JSON();
                    fileInfo["info"] = ToolText.GetMD5Hash(bytes);
#if UNITY_ANDROID && SPLIT_PACK && !UNITY_SPLIT_PACK  
                    fileInfo["type"] = (int)GetDownLoadTypeFromPath(streamingAssetsAbPath,filePath,null);
#endif                    
                    File.WriteAllBytes(filePath, bytes);

                    string fileName = PathExtension.NormalizePathSeparatorChar(Path.GetRelativePath(streamingAssetsAbPath, info.FullName));
                    packedMD5Info[fileName] = fileInfo;
                }
            }
        }
        File.WriteAllText(Path.Combine(streamingAssetsAbPath, "fileInfo.json"), packedMD5Info.serialized);
      
        
        CopySoundBanks(target, ref destinationSoundBankFolder);
        GenerateAudioFilesInfo(target);
        
        AssetDatabase.Refresh();
        return streamingAssetsAbPath;
    }

    #region SoundBanks

    private static void CopySoundBanks(BuildTarget target, ref string destinationSoundBankFolder) {
        // from: @AkBuildPreprocessor.CopySoundbanks
        if (!AkWwiseEditorSettings.Instance.CopySoundBanksAsPreBuildStep) {
            var platformName = AkBuildPreprocessor.GetPlatformName(target);
            if (!AkBuildPreprocessor.CopySoundbanks(
                    AkWwiseEditorSettings.Instance.GenerateSoundBanksAsPreBuildStep,
                    platformName,
                    ref destinationSoundBankFolder)) {
                UnityEngine.Debug.LogErrorFormat(
                    "WwiseUnity: SoundBank folder has not been copied for <{0}> target at <{1}>. This will likely result in a build without sound!!!",
                    target, AkWwiseInitializationSettings.ActivePlatformSettings.SoundbankPath);
            }
        }

        AssetDatabase.Refresh();
    }
    
    private static void DeleteSoundBanks(string destinationSoundBankFolder) {
        if (!AkWwiseEditorSettings.Instance.CopySoundBanksAsPreBuildStep) {
            destinationSoundBankFolder = PathExtension.NormalizePathSeparatorChar(destinationSoundBankFolder);
            AkBuildPreprocessor.DeleteSoundbanks(destinationSoundBankFolder);
        }

        AssetDatabase.Refresh();
    }

    private static void GenerateAudioFilesInfo(BuildTarget target) {
        // audio files
        string streamingAssetsAbPath =
            new DirectoryInfo(Path.Combine(Application.streamingAssetsPath, target.ToString().ToUpper())).FullName +
            Path.DirectorySeparatorChar;
        string infoJsonPath = Path.Combine(streamingAssetsAbPath, "fileInfo.json");

        JSON infoJson = new JSON();
        infoJson.serialized = File.ReadAllText(infoJsonPath);

        string audioStreamingAssetsAbPath = new DirectoryInfo(Path.Combine(
            Application.streamingAssetsPath,
            AkWwiseInitializationSettings.ActivePlatformSettings.SoundbankPath,
            target.ToString())).FullName + Path.DirectorySeparatorChar;

        audioStreamingAssetsAbPath = PathExtension.NormalizePathSeparatorChar(audioStreamingAssetsAbPath);

        List<string> allAudioFiles = new List<string>();
        foreach (string format in AudioManager.AUDIO_FILE_EXTENSIONS) {
            allAudioFiles.AddRange(Directory
                .GetFiles(audioStreamingAssetsAbPath, "*" + format, SearchOption.AllDirectories).ToList());
        }

        foreach (string audioFilePath in allAudioFiles) {
            var info = new FileInfo(audioFilePath);
            if (info.Exists) {
                var fileInfo = new JSON();
                fileInfo["info"] = ToolText.GetMD5HashFromFile(info.FullName);
#if UNITY_ANDROID && SPLIT_PACK && !UNITY_SPLIT_PACK  
                fileInfo["type"] = (int)GetDownLoadTypeFromPath(streamingAssetsAbPath,audioFilePath,DownLoadType.FastFollow1);
#endif
                string fileName = "audio/" + PathExtension.NormalizePathSeparatorChar(Path.GetRelativePath(audioStreamingAssetsAbPath, info.FullName));
                infoJson[fileName] = fileInfo;
            }
        }
        File.WriteAllText(infoJsonPath, infoJson.serialized);
        
        AssetDatabase.Refresh();
    }

    #endregion
    public  enum  DownLoadType
    {
        InstallTime,
        FastFollow1,
        FastFollow2,
    }

    private static DownLoadType GetDownLoadTypeFromPath(string streamingPath,string filePath,DownLoadType? preType)
    {
        if (preType != null)
        {
            return (DownLoadType)preType;
        }
        var artPath = Path.Combine(streamingPath, "assets", "art");
       
        return filePath.Contains(artPath) ? DownLoadType.FastFollow2 : DownLoadType.InstallTime;
    }   
}


