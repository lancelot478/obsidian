
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MiniJSON2;
using UnityEditor;
using UnityEngine;

public class HotUpdateEditor : Editor {
    private static BuildTarget GetPlatformTarget() {
        BuildTarget platformTarget = BuildTarget.iOS;
#if UNITY_ANDROID
        platformTarget = BuildTarget.Android;
#endif
        return platformTarget;
    }

    public static void BuildHotUpdateAssets() {
        try {
            CommandArgs args = CommandLine.Parse(System.Environment.GetCommandLineArgs());

            int buildResourceVersion = 0;
            if (args.ArgPairs.ContainsKey("resourceVersion")) {
                int  buildCode = 0;
                bool result    = int.TryParse(args.ArgPairs["resourceVersion"], out buildCode);
                if (result) {
                    buildResourceVersion = Mathf.Abs(buildCode);
                }
            }

            string cdnRepoPath = null;
            if (args.ArgPairs.ContainsKey("cdnRepoPath")) {
                cdnRepoPath = args.ArgPairs["cdnRepoPath"];
            }

            string appendBuildABPath = null;
            if (args.ArgPairs.ContainsKey("appendBuildABPath")) {
                appendBuildABPath = args.ArgPairs["appendBuildABPath"];
            }

            bool alreadyHaveNewestAB = false;
            if (args.ArgPairs.ContainsKey("alreadyHaveNewestAB")) {
                alreadyHaveNewestAB = (args.ArgPairs["alreadyHaveNewestAB"] == "true");
            }

            BuildTarget packPlatform = BuildTarget.iOS;
            if (args.ArgPairs.ContainsKey("packPlatform")) {
                packPlatform = (BuildTarget)Enum.Parse(typeof(BuildTarget), args.ArgPairs["packPlatform"]);
            }

            bool buildResult = GenerateHotUpdateAssets(buildResourceVersion: buildResourceVersion,
                cdnRepoPath: cdnRepoPath, appendBuildABPath: appendBuildABPath,
                alreadyHaveNewestAB: alreadyHaveNewestAB, currentPlatform: packPlatform);

            if (!buildResult) {
                Debug.LogError("------------------->>>>HOTUPDATE: PACKAB ERROR!!!!!");
                EditorApplication.Exit(666);
            }
        }
        catch (Exception e) {
            Debug.LogError(e);
            EditorApplication.Exit(668);
            throw;
        }
    }

    // [MenuItem("Assets/HotUpdate")]
    public static void GenerateHotUpdateAssetsEditor() {
        GenerateHotUpdateAssets(1, currentPlatform: BuildTarget.Android);
    }

    static bool GenerateHotUpdateAssets(int    buildResourceVersion = 0,
                                        string cdnRepoPath          = null,
                                        string appendBuildABPath    = null, 
                                        bool alreadyHaveNewestAB = false, 
                                        BuildTarget currentPlatform = BuildTarget.iOS) {
        DateTime pre     = DateTime.UtcNow;
        int      version = 0;
        version = buildResourceVersion;

        BuildTarget[] buildArray = new BuildTarget[1];

        // BuildTarget currentPlatform = GetPlatformTarget();
        
        Debug.Log("[Hot Update]: start!");

        if (currentPlatform == BuildTarget.iOS) {
            buildArray[0] = BuildTarget.iOS;
            // buildArray[1] = BuildTarget.Android;
        }
        else if (currentPlatform == BuildTarget.Android) {
            buildArray[0] = BuildTarget.Android;
            // buildArray[1] = BuildTarget.iOS;
        }

        bool allSucceed = true;

        // LuaScriptsProcessEditor.PrePackLS();
        // LuaScriptsProcessEditor.GenerateLuaScriptTxt(false);
        foreach (BuildTarget target in buildArray) {
            Debug.Log($"[Hot Update]: start! Platform: {target.ToString()}");
            
            AssetDatabase.Refresh();
            if (!alreadyHaveNewestAB) {
                Debug.Log($"[Hot Update]: Switch Platform: {target.ToString()}");
            
                // EditorUserBuildSettings.SwitchActiveBuildTarget(
                //     (BuildTargetGroup) Enum.Parse(typeof(BuildTargetGroup), target.ToString()), target);
                // AssetDatabase.Refresh();
                
                Debug.Log($"[Hot Update]: BuildAllAssetsBundlesWithPath : {target.ToString()}, {appendBuildABPath}");
                bool result = true;//AssetBundleEditor.BuildAllAssetsBundlesWithPath(target, path: appendBuildABPath);
                allSucceed = allSucceed && result;
            }
            AssetDatabase.Refresh();
            
            Debug.Log($"[Hot Update]: GenerateModifiedAssets : {target.ToString()}, {version}, {cdnRepoPath}, {appendBuildABPath}");
            GenerateModifiedAssets(target, version, cdnRepoPath, appendBuildABPath);
        }

        // LuaScriptsProcessEditor.PostPackLS();
        // LuaScriptsProcessEditor.GenerateLuaScriptTxtFlush();
        
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("完成", "资源准备完成, 用时:" + (DateTime.UtcNow - pre).TotalSeconds + "秒", "ok");
        
        return allSucceed;
    }

    static void GenerateModifiedAssets(BuildTarget platform, int version,
                                       string cdnRepoPath       = null,
                                       string appendBuildABPath = null) {
        void CopyFile(FileInfo fileInfo, string s, JSON json, string tarPath1) {
            byte[] fileBytes = File.ReadAllBytes(fileInfo.FullName);
            var resultBytes = fileBytes;
            if (ToolText.AssetBundleNeedEncrypted(s)) {
                var memoryStream = new MemoryStream();
                Debug.Log($"Encrypting HotUpdate AssetBundle bytes: {s}");
                ToolText.EncryptAssetBundleBytes(memoryStream, fileBytes);
                if (memoryStream.TryGetBuffer(out var buffer)) {
                    resultBytes = buffer.Array;
                }
            }

            string md5Str = ToolText.GetMD5Hash(resultBytes);
            string key = s;
            JSON infoJson = new JSON();
            infoJson["info"] = md5Str;
            infoJson["size"] = resultBytes.Length;
            json[key] = infoJson;
            string targetFile = tarPath1 + key + "_" + md5Str;
            FileInfo targetFileInfo = new FileInfo(targetFile);
            if (targetFileInfo.Directory != null && targetFileInfo.Directory.Exists == false) {
                targetFileInfo.Directory.Create();
            }

            File.WriteAllBytes(targetFileInfo.FullName, resultBytes);
        }

        var ignoreHotUpdateFiles = new HashSet<string>() {
            "ANDROID", "IOS", ".DS_Store"
        };
        
        string buildABPath = Application.dataPath + "/../_AssetsBundles/";
        if (appendBuildABPath != null) {
            buildABPath = appendBuildABPath;
        }

        string repoPath = Application.dataPath  + "/../../boli2_cdn/" +
                          Config.UNITY_VARIANTS + "/"                 +
                          Config.UNITY_ENGINE_VERSION;
        if (cdnRepoPath != null) {
            repoPath = Path.Combine(cdnRepoPath, Config.UNITY_VARIANTS);
            repoPath = Path.Combine(repoPath, Config.UNITY_ENGINE_VERSION);
        }

        string platformName = platform.ToString().ToUpper();
        string        srcPath = Path.Combine(buildABPath, platformName) + "/";
        string        tarPath = Path.Combine(repoPath, platformName)    + "/";
        DirectoryInfo srcInfo = new DirectoryInfo(srcPath);
        // FilesTool.DirectoryCopy(srcPath, tarPath, true, true);
        JSON configJson = new JSON();
        configJson["notice"] = "检测到新版本，本次更新<color=#EA7829>@1MB</color>\n建议在Wi-Fi环境下进行更新";
        JSON     filesInfo = new JSON();
        List<string> files     = Directory.GetFiles(srcPath, "*", SearchOption.AllDirectories).ToList();
        int count = 0;
        
        foreach (string filePath in files) {
            FileInfo fileInfo = new FileInfo(filePath);
            if (ignoreHotUpdateFiles.Contains(fileInfo.Name)) {
                continue;
            }
            if (!string.IsNullOrEmpty(fileInfo.Extension)) {
                continue;
            }

            string name = PathExtension.NormalizePathSeparatorChar(Path.GetRelativePath(srcInfo.FullName, fileInfo.FullName));
            
            CopyFile(fileInfo, name, filesInfo, tarPath);

            EditorUtility.DisplayProgressBar("复制文件", name, (float) count++ / files.Count);
        }

        // audio files
        List<string> audioFiles = new List<string>();
        FileInfo akProjFileInfo = new FileInfo(AkWwiseEditorSettings.WwiseProjectAbsolutePath);
        if (akProjFileInfo.Exists) {
            if (akProjFileInfo.Directory != null && akProjFileInfo.Directory.Exists) {
                DirectoryInfo audioFilesDirectory = new DirectoryInfo(Path.Combine(akProjFileInfo.Directory.FullName, "GeneratedSoundBanks", platform.ToString()));
                foreach (string format in AudioManager.AUDIO_FILE_EXTENSIONS) {
                    audioFiles.AddRange(Directory.GetFiles(audioFilesDirectory.FullName, "*" + format, SearchOption.AllDirectories));
                }
                
                foreach (string audioFilePath in audioFiles) {
                    FileInfo audioFileInfo = new FileInfo(audioFilePath);
                    if (!AudioManager.AUDIO_FILE_EXTENSIONS.Contains(audioFileInfo.Extension)) {
                        continue;
                    }
                    string name = PathExtension.NormalizePathSeparatorChar(Path.GetRelativePath(audioFilesDirectory.FullName, audioFileInfo.FullName));
                    
                    name = "audio/" + name;
            
                    CopyFile(audioFileInfo, name, filesInfo, tarPath);
                }
            }
        }

        configJson["filesInfo"] = filesInfo;
        EditorUtility.ClearProgressBar();
        string md5Path = Path.Combine(Path.Combine(repoPath, Config.HOT_UPDATE_CONFIG),
            Config.HOT_UPDATE_MD5_FILE_NAME + platformName + "_" + version + ".txt");
        FileInfo md5FileInfo = new FileInfo(md5Path);
        if (md5FileInfo.Directory != null && md5FileInfo.Directory.Exists == false) {
            md5FileInfo.Directory.Create();
        } 
        File.WriteAllText(md5FileInfo.FullName, configJson.serialized);
    }
}
