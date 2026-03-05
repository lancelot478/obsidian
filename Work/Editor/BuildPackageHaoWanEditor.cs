using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using PADPost;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking.Types;

#if UNITY_IPHONE
using UnityEditor.iOS.Xcode;
#endif
public partial class BuildPackageEditor : Editor {
    public static void PostBuildPackage_HaoWanAndroid(string outputPath,bool isGoogle)
    {
        Config.LoadRegionInEditor();
        if (!Config.IsHaoWan)
        {
            return;
        }
        
        {
            var gradlePropertiesPath = Path.Combine(outputPath, "gradle.properties");
            File.WriteAllText(gradlePropertiesPath,
                File.ReadAllText(gradlePropertiesPath)
                    .Replace("org.gradle.jvmargs=-Xmx8192M", "org.gradle.jvmargs=-Xmx8192m -XX:+UseParallelGC")
                    .Replace("unityTemplateVersion=3", "unityTemplateVersion=3\nandroid.useAndroidX=true\nandroid.enableJetifier=true"));
        }
    }
#if UNITY_IPHONE
    public static void PostBuildPackage_HaoWaniOSPlist(PlistDocument plistDocument)
    {
        Config.LoadRegionInEditor();
        if (!Config.IsHaoWan)
        {
            return;
        }
        var rootDic = plistDocument.root;
        var schemesItem = new List<string>
        {
            "fbapi",
            "fbapi20130214",
            "fbapi20130410",
            "fbapi20130702",
            "fbapi20131010",
            "fbapi20131219",
            "fbapi20140410",
            "fbapi20140116",
            "fbapi20150313",
            "fbapi20150629",
            "fbapi20160328",
            "fbauth",
            "fb-messenger-share-api",
            "fbauth2",
            "fbshareextension",
            "instagram"
        };

        if (!(rootDic["LSApplicationQueriesSchemes"] is PlistElementArray plistElementList))
        {
            plistElementList = rootDic.CreateArray("LSApplicationQueriesSchemes");
        }

        foreach (var t in schemesItem)
        {
            plistElementList.AddString(t);
        }

        //添加url
        var dict = plistDocument.root.AsDict();
        if (!(dict["CFBundleURLTypes"] is PlistElementArray array))
        {
            array = dict.CreateArray("CFBundleURLTypes");
        }

        var urlTypesItem = new List<List<string>>
        {
            new() { "facebook", 
                "fb1130372094840569",
                "fb333579739765862",
                "fb973962204049033",
                "fb2131396987255164"
            },
            new() { "firebase", 
                "com.googleusercontent.apps.604761677855-qpb022aoqec0o4m4b58fc9ehsmn100rm",
                "com.googleusercontent.apps.598400804825-kdpieu8olt1mnvl8buovkfom4ouumatf",
                "com.googleusercontent.apps.288808456782-u4on34rp9m7chp0nq59qug6c6a5n1mbi",
                "com.googleusercontent.apps.281203722802-k410ar8hihr6q8uc7ou83glbk5b1t4kq"
            },
            new() { "pnsdk", 
                "pnsdk2102001",
                "pnsdk2101001",
                "pnsdk2100001",
                "pnsdk2116001"
            },
        
        };
        var index = 1;
        if(Config.IsKR()){
            index = 1;
        }
        else if(Config.IsJP())
        {
            index = 2;
        }
        else if (Config.IsGlobal())
        {
            index = 3;
        }
        else if (Config.IsVI())
        {
            index = 4;
        }
        for (var i = 0; i < 3; i++)
        {
            var dictDict = array.AddDict();
            dictDict.SetString("CFBundleTypeRole", "Editor");
            dictDict.SetString("CFBundleURLName", urlTypesItem[i][0]);
            var array2 = dictDict.CreateArray("CFBundleURLSchemes");
            
            array2.AddString(urlTypesItem[i][index]);
        }

         plistDocument.root.SetString("FacebookDisplayName", "$(PRODUCT_NAME)");
         plistDocument.root.SetBoolean("FacebookAdvertiserIDCollectionEnabled", true);
         plistDocument.root.SetBoolean("FacebookAutoLogAppEventsEnabled", true);
    }
    public static void PostBuildPackage_HaoWaniOS(string outputPath ,PBXProject pbxProject)
    {
        Config.LoadRegionInEditor();
        if (!Config.IsHaoWan)
        {
            return;
        }
        string mainTarget = pbxProject.GetUnityMainTargetGuid();
        string frameworkTarget = pbxProject.GetUnityFrameworkTargetGuid();
            
        var frameWorks = new List<string>()
            {
                "PnSDK.framework",
                "Promise.framework",
                "Promises.framework",
                "FBLPromises.framework",
                "PnSDKUnityAdapter.framework"
            };
        if (Config.IsVI())
        {
            frameWorks.Add("PnSDKForVN.framework");
        }
        for (int i = 0; i < frameWorks.Count; i++)
        {
            var frameWorkName = frameWorks[i];
            // pbxProject.AddFrameworkToProject(target,frameWorkName,true);
            var FRAMEWORK_TARGET_PATH = "Frameworks/PnSDK/Plugins/iOS"; // relative to build folder
            var destPath = Path.Combine(FRAMEWORK_TARGET_PATH, frameWorkName);
            var fileGuid = pbxProject.AddFile(destPath, destPath, PBXSourceTree.Source);
            UnityEditor.iOS.Xcode.Extensions.PBXProjectExtensions.AddFileToEmbedFrameworks(pbxProject, mainTarget, fileGuid);
        }
        pbxProject.SetBuildProperty(mainTarget, "FRAMEWORK_SEARCH_PATHS", "$(inherited)");
        pbxProject.AddBuildProperty(mainTarget,"FRAMEWORK_SEARCH_PATHS","$(PROJECT_DIR)/Frameworks/PnSDK/Plugins/iOS");

        pbxProject.SetBuildProperty(frameworkTarget, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "YES");
        pbxProject.SetBuildProperty(mainTarget, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "YES");
        
        // pbxProject.AddBuildProperty(frameworkTarget, "OTHER_LDFLAGS", "-ObjC");
        // pbxProject.AddBuildProperty(frameworkTarget, "OTHER_LDFLAGS", "-ld64");
        
        
        string iosAppControllerClassPath = Path.Combine(outputPath,"Classes/UnityAppController.mm");
        if (File.Exists(iosAppControllerClassPath))
        {
            File.WriteAllText(iosAppControllerClassPath, File.ReadAllText(iosAppControllerClassPath)
                .Replace("AppController_SendNotificationWithArg(kUnityOnOpenURL, notifData);\n    return YES;",
                    "#if __IPHONE_OS_VERSION_MAX_ALLOWED <= _IPHONE80_\n    " +
                    "return [PnSDK application:application openURL:url sourceApplication:sourceApplication annotation:annotation];\n" +
                    "#else\n    return [PnSDK application:app openURL:url options:options];" +
                    "\n#endif\n" +
                    "//    return YES;").
                Replace("#include <mach/mach_time.h>",
                    "#include <mach/mach_time.h>\n" +
                    "#import <PnSDK/PnSDK.h>"));

        }
    }
#endif
    public static void PreBuildPackage_HaoWan(bool splitPack)
    {
        if (!Config.IsHaoWan)
        {
            return;
        }

        {
            string fileName = splitPack ? "mainTemplate.gradle.haowan" : "mainTemplate.gradle.taptap.haowan";
            var sourceDir = Path.Combine(Application.dataPath,  $"Plugins/Android/{fileName}");
            var destDir = Path.Combine(Application.dataPath,  "Plugins/Android/mainTemplate.gradle");
            if (File.Exists(sourceDir) && File.Exists(destDir))
            {
                var fileString = File.ReadAllText(sourceDir);
                File.WriteAllText(destDir, fileString);
            }
        }
        {
            var data = Resources.Load<XDConfigData>("XDConfigData");
#if UNITY_IPHONE
            data.channel = "iOS";
#endif
#if UNITY_ANDROID
            data.channel =  splitPack ? "Google" : "TapTap";
#endif
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
        }
        // {
        //     var sourceDir = Path.Combine(Application.dataPath,  "Plugins/Android/HaoWanSDK.java.haowan");
        //     var destDir = Path.Combine(Application.dataPath,  "Plugins/Android/HaoWanSDK.java");
        //     PADPostFileHelper.FileCopy(sourceDir,destDir);
        //     AssetDatabase.Refresh();
        //     AssetDatabase.SaveAssets();
        // }
    }

    public static void InBuildPackage_HaoWan(string platform)
    {
        var list = Config.HaoWanGlobalRegionList;
        var currentRegionName = Config.GetRegionName();
        if (list.Contains(currentRegionName))
        {
            currentRegionName = Config.GL;
        }

        if (platform == "ios")
        {
            string srcPath = default;
            if (Config.IsHaoWan)
            {
                srcPath = Path.Combine(Application.dataPath, "..", "_HaoWanFrameWork", $"{currentRegionName}");
            }
            else
            {
                srcPath = Path.Combine(Application.dataPath, "..", "_HaoWanFrameWork", $"{currentRegionName}","mobileprovision");
            }
            PADPostFileHelper.CopyDirectory(srcPath, Path.Combine(Application.dataPath,"PnSDK","Plugins","iOS"));
        }
        if (!Config.IsHaoWan)
        {
            return;
        }
        if (platform != "android")
        {
            return;
        }

        {
            var destDir = Path.Combine(Application.dataPath,  "Plugins/Android/mainTemplate.gradle");
            if (File.Exists(destDir) && File.Exists(destDir))
            {
                var fileString = File.ReadAllText(destDir);
                var replaceAppIDText = fileString.Replace("{0}",Config.GetHaoWanAppID(platform));
                File.WriteAllText(destDir, replaceAppIDText);
            }
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
        }
        {
            var sourceDir = Path.Combine(Application.dataPath,  "Plugins/Android/baseProjectTemplate.gradle.haowan");
            var destDir = Path.Combine(Application.dataPath,  "Plugins/Android/baseProjectTemplate.gradle");
            if (File.Exists(sourceDir) && File.Exists(destDir))
            {
                var fileString = File.ReadAllText(sourceDir);
                File.WriteAllText(destDir, fileString);
            }
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
        }
        // {
        //     var sourceDir = Path.Combine(Application.dataPath, "Plugins/Android", "MuffinUnityPlayerActivity.java.haowan");
        //     var destDir = Path.Combine(Application.dataPath,  "Plugins/Android", "MuffinUnityPlayerActivity.java");
        //     if (File.Exists(sourceDir) && File.Exists(destDir))
        //     {
        //         var fileString = File.ReadAllText(sourceDir);
        //         File.WriteAllText(destDir, fileString);
        //     }
        //     AssetDatabase.Refresh();
        //     AssetDatabase.SaveAssets();
        // }
    }
}


