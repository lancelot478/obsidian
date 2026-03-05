using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using PADPost;
using UnityEditor;
using UnityEngine;

#if UNITY_IPHONE
using UnityEditor;
using UnityEditor.iOS.Xcode;
using UnityEditor.iOS.Xcode.Extensions;
using UnityEditor.iOS.Xcode.PBX;
#endif

[Serializable]
public class ApplicationLocaleInfo {
    public string Locale;
    public string AppName;
    public string CameraUsageDescription;
    public string MicrophoneUsageDescription;
    public string LocationUsageDescription;
    public string UserTrackUsageDescription;
#if HaoWan
    public string NSPhotoLibraryAddUsageDescription;
#endif
}

[Serializable]
public class PlatformLocaleName {
    public string platform;
    public string[] locales;
    public string[] platformLocales;
}

[Serializable]
public class ApplicationInfo {
    public ApplicationLocaleInfo[] locales;
    public string defaultLocale;
    public string[] availableLocales;
    public PlatformLocaleName[] platformLocales;
    public string[] disabledLocales;
}

public static class PostBuildPackageEditor {

    #region iOS

    public static void iOSPostBuild(string outputPath) {
#if UNITY_IPHONE
        // string iosAppControllerClassPath = Path.Combine(outputPath,"Classes/UnityAppController.mm");
        // if (File.Exists(iosAppControllerClassPath)) {
        //     File.WriteAllText(iosAppControllerClassPath,File.ReadAllText(iosAppControllerClassPath)
        //         .Replace("return  [XDSDK application:application supportedInterfaceOrientationsForWindow:window];","")
        //         .Replace("[XDSDK application:application didReceiveRemoteNotification:userInfo","")
        //         .Replace("fetchCompletionHandler:completionHandler];",""));
        // }
        
        {
            // var headFile = Path.Combine(outputPath, "Classes/UI/Keyboard.h");
            // if (File.Exists(headFile)) {
            // 	File.WriteAllText(headFile, File.ReadAllText(headFile)
            // 		.Replace("@end", "- (UITextField*)getTextField; \n @end")
            // 	);
            // }
        }

        {
            var ocFile = Path.Combine(outputPath, "Classes/UI/Keyboard.mm");
            if (File.Exists(ocFile)) {
                File.WriteAllText(ocFile, File.ReadAllText(ocFile)
                        .Replace("UIReturnKeyDone", "UIReturnKeySend")		
                    // .Replace("@end", "- (UITextField*) getTextField { return textField; } \n @end") 
                    // + " \n extern \"C\" void UnityKeyboard_SetReturnKeyType(const int type) { [[KeyboardDelegate Instance] getTextField].returnKeyType = ((UIReturnKeyType) type); }"
                );
            }
        }

        {
            string pbxProjectPath = PBXProject.GetPBXProjectPath(outputPath);
            PBXProject pbxProject = new PBXProject();
            pbxProject.ReadFromFile(pbxProjectPath);

            string unityFrameworkTargetGuid = pbxProject.GetUnityFrameworkTargetGuid();
            string[] needAddLibraries = {"libresolv.9.tbd"};
            foreach (string needAddLibrary in needAddLibraries) {
                pbxProject.AddLibToProject(unityFrameworkTargetGuid, needAddLibrary);
            }

            // 增加apple登录，支付,推送等
            AddProjectCapability(pbxProjectPath);

            //Main
            string target = pbxProject.GetUnityMainTargetGuid();
            pbxProject.SetBuildProperty(target, "ENABLE_BITCODE", "NO");
            
            //Unity Tests
            target = pbxProject.TargetGuidByName(PBXProject.GetUnityTestTargetName());
            pbxProject.SetBuildProperty(target, "ENABLE_BITCODE", "NO");
            
            //Unity Framework
            target = pbxProject.GetUnityFrameworkTargetGuid();
            pbxProject.SetBuildProperty(target, "ENABLE_BITCODE", "NO");
            
            BuildPackageEditor.PostBuildPackage_HaoWaniOS(outputPath,pbxProject);
            
            pbxProject.WriteToFile(pbxProjectPath);
        }
        // add locales
        {
            string infoPath = "ApplicationInfo";
            var appInfoJson = Resources.Load<TextAsset>(infoPath)?.text;
            var appInfo = JsonUtility.FromJson<ApplicationInfo>(appInfoJson);
            AddLocalizationToXcodeProject(outputPath, appInfo);
        }
       
#endif
    }

#if UNITY_IPHONE

    private static void AddLibToProject(this PBXProject proj, string targetGuid, string libName) {
        string file = proj.AddFile("usr/lib/" + libName, "Frameworks/" + libName, PBXSourceTree.Sdk);
        proj.AddFileToBuild(targetGuid, file);
    }
    
    private static void AddProjectCapability(string outputPath) {
        var manager = new ProjectCapabilityManager(outputPath, "Unity-iPhone.entitlements", "Unity-iPhone");
        manager.AddSignInWithApple();  //添加苹果登录能力，其他能力(如推送)，游戏可按需求类似这样添加
        manager.WriteToFile();
    }
    
    
    const string k_InfoFile = "InfoPlist.strings";

    // locales
    private static void AddLocalizationToXcodeProject(string projectDirectory, ApplicationInfo appInfo) {
        if (appInfo == null)
            throw new ArgumentNullException(nameof(appInfo));

        var pbxPath = PBXProject.GetPBXProjectPath(projectDirectory);
        var project = new PBXProject();
        project.ReadFromFile(pbxPath);
        project.ClearKnownRegions(); // Remove the deprecated regions that get added automatically.

        var plistDocument = new PlistDocument();
        var plistPath = Path.Combine(projectDirectory, "Info.plist");
        plistDocument.ReadFromFile(plistPath);
        // Default language
        // How iOS Determines the Language For Your App - https://developer.apple.com/library/archive/qa/qa1828/_index.html
        var notHaveDefault = string.IsNullOrEmpty(appInfo.defaultLocale);
        // ? LocalizationEditorSettings.GetLocales() ? [0]?.Identifier : LocalizationSettings.Instance.m_ProjectLocaleIdentifier;
        
        var platformLocale = appInfo.platformLocales.Single(platform => platform.platform == "iOS");
        if (!notHaveDefault) {
            var index = Array.IndexOf(platformLocale.locales, appInfo.defaultLocale);
            var code = platformLocale.platformLocales[index];
            project.SetDevelopmentRegion(code);// + platformLocale.locales.Length * 1]);
        }

      
        var bundleLanguages = plistDocument.root.CreateArray("CFBundleLocalizations");
        foreach (var locale in appInfo.availableLocales) {
            var index = Array.IndexOf(platformLocale.locales, locale);
            var code = platformLocale.platformLocales[index];
            project.AddKnownRegion(code);

            if (!appInfo.disabledLocales.Contains(locale)) {
                var langCode = platformLocale.platformLocales[index];// + platformLocale.locales.Length * 2];
                bundleLanguages.AddString(langCode);
            }

            var fileCode = platformLocale.platformLocales[index];// + platformLocale.locales.Length * 1];
            var localeDir = fileCode + ".lproj";
            var dir = Path.Combine(projectDirectory, localeDir);
            Directory.CreateDirectory(dir);

            var filePath = Path.Combine(dir, k_InfoFile);
            var relativePath = Path.Combine(localeDir, k_InfoFile);
                
                
          
            GenerateLocalizedInfoPlistFile(appInfo.locales.Single(info => info.Locale == locale), plistDocument, filePath);
            project.AddLocaleVariantFile(k_InfoFile, fileCode, relativePath);
        }

        foreach (var disabledLocale in appInfo.disabledLocales) {
            var index = Array.IndexOf(platformLocale.locales, disabledLocale);
            var fileCode = platformLocale.platformLocales[index];// + platformLocale.locales.Length * 1];
            project.RemoveLocaleVariantFile(k_InfoFile, fileCode, fileCode + ".lproj/" + k_InfoFile);
        }

        string firstCurrentAppName = default;
        foreach (var locale in appInfo.availableLocales)
        {
            bool canAdd = true;
            foreach (var disabledLocale in appInfo.disabledLocales)
            {

                if (disabledLocale == locale)
                {
                    canAdd = false;
                }
            }

            if (canAdd)
            {
                firstCurrentAppName = appInfo.locales.Single(info => info.Locale == locale).AppName;
            }
        }
        
        plistDocument.root.SetString("CFBundleDevelopmentRegion", "$(DEVELOPMENT_LANGUAGE)");
        Config.LoadRegionInEditor();
        if (Config.IsGlobal())
        {
            firstCurrentAppName = "GO!GO!Muffin";
        }
#if HaoWan
        plistDocument.root.SetString("CFBundleDisplayName", firstCurrentAppName);
        plistDocument.root.SetString("CFBundleName", firstCurrentAppName);
#else
        plistDocument.root.SetString("CFBundleDisplayName", "$(PRODUCT_NAME)");
        plistDocument.root.SetString("CFBundleName", "$(PRODUCT_NAME)");
#endif
        // Inclusion of this key improves performance associated with displaying localized application names.
        plistDocument.root.SetBoolean("LSHasLocalizedDisplayName", true);
        plistDocument.root.SetString("NSUserTrackingUsageDescription", "Allow 'Go!Go!Muffin' to track your activity across other companies' apps and websites? We would like your permission to provide better services, and minimize the interruption induced by irrelevant services");
        plistDocument.root.SetString("NSCameraUsageDescription", "麥芬需要開啟相機為你獲得更完整的遊戲體驗，確認的事情就拜託咯！");
        plistDocument.root.SetString("NSMicrophoneUsageDescription", "麥芬需要開啟麥克風為你獲得更完整的遊戲體驗，確認的事情就拜託咯！");
        plistDocument.root.SetString("NSLocationWhenInUseUsageDescription", "麥芬需要開啟定位為你獲得更完整的遊戲體驗，確認的事情就拜託咯！");
#if HaoWan
        plistDocument.root.SetString("NSPhotoLibraryAddUsageDescription", "Test");
#endif
        //plistDocument.root.SetString("UIFileSharingEnabled", "YES");

        BuildPackageEditor.PostBuildPackage_HaoWaniOSPlist(plistDocument);
        plistDocument.WriteToFile(plistPath);
        project.WriteToFile(pbxPath);
    }

    static void GenerateLocalizedInfoPlistFile(ApplicationLocaleInfo appInfo, PlistDocument plistDocument, string filePath) {
        using (var stream = new StreamWriter(filePath, false, Encoding.UTF8)) {
            stream.Write(
                "/*\n" +
                $"\t{k_InfoFile}\n" +
                $"\tThis file was auto-generated by {nameof(ApplicationInfo)}\n" +
                $"\tChanges to this file may cause incorrect behavior and will be lost if the project is rebuilt.\n" +
                $"*/\n\n");

            WriteLocalizedValue("CFBundleName", stream, appInfo.AppName, plistDocument);
            WriteLocalizedValue("CFBundleDisplayName", stream, appInfo.AppName, plistDocument);
            WriteLocalizedValue("NSCameraUsageDescription", stream, appInfo.CameraUsageDescription, plistDocument);
            WriteLocalizedValue("NSMicrophoneUsageDescription", stream, appInfo.MicrophoneUsageDescription, plistDocument);
            WriteLocalizedValue("NSLocationWhenInUseUsageDescription", stream, appInfo.LocationUsageDescription, plistDocument);
            WriteLocalizedValue("NSUserTrackingUsageDescription", stream, appInfo.UserTrackUsageDescription, plistDocument);
#if HaoWan
            WriteLocalizedValue("NSPhotoLibraryAddUsageDescription", stream, appInfo.NSPhotoLibraryAddUsageDescription, plistDocument);
#endif
        }
    }

    static void WriteLocalizedValue(string valueName, StreamWriter stream, string localizedString, PlistDocument plistDocument) {
        if (string.IsNullOrEmpty(localizedString)) {
            return;
        }
        
        stream.WriteLine($"\"{valueName}\" = \"{localizedString}\";");
        plistDocument.root.SetString(valueName, string.Empty);
    }

#endif

    #endregion


    public static void AndroidPostBuild(string outputPath, bool isGoogle = false) {
#if UNITY_ANDROID
        //gradle wrapper
        var sourceDir = new DirectoryInfo(Application.dataPath + "/../gradle").FullName;
        var distDir = new DirectoryInfo(Path.Combine(outputPath, "gradle")).FullName;
        Directory.CreateDirectory(distDir);
        if (Directory.Exists(sourceDir)) {
            FilesTool.DirectoryCopy(
                sourceDir,
                distDir, true, true);
        }

        {
            var androidLaunchGradlePath = Path.Combine(outputPath, "launcher/build.gradle");
            var buildGradeString = File.ReadAllText(androidLaunchGradlePath);

            buildGradeString = buildGradeString.Replace("storePassword ''", "storePassword 'xindong'").Replace("keyPassword ''", "keyPassword 'xindong'");
            buildGradeString = buildGradeString.Replace("noCompress", "noCompress '' //");
            File.WriteAllText(androidLaunchGradlePath, buildGradeString);
        }
        BuildPackageEditor.PostBuildPackage_HaoWanAndroid(outputPath,isGoogle);
        {
            var unityLibraryGradlePath = Path.Combine(outputPath, "unityLibrary/build.gradle");
            var unityLibraryGradleString = File.ReadAllText(unityLibraryGradlePath);
            unityLibraryGradleString = unityLibraryGradleString.Replace("noCompress", "noCompress '' //");
            File.WriteAllText(unityLibraryGradlePath, unityLibraryGradleString);
        }

        {
            var gradlePropertiesPath = Path.Combine(outputPath, "gradle.properties");
            File.WriteAllText(gradlePropertiesPath, File.ReadAllText(gradlePropertiesPath).Replace("org.gradle.jvmargs=-Xmx4096M", "org.gradle.jvmargs=-Xmx1536m -XX:+UseParallelGC").Replace("android.enableJetifier=true", "android.enableJetifier=true\nandroid.injected.testOnly=false").Replace("android.enableR8=false", ""));
        }

        // browscap.ini
        {
            var browscapFilePath = Path.Combine(outputPath, "unityLibrary/src/main/assets/bin/Data/Managed/etc/mono/browscap.ini");
            var fileInfo = new FileInfo(browscapFilePath);
            if (fileInfo.Exists) {
                File.WriteAllText(fileInfo.FullName, File.ReadAllText(fileInfo.FullName)
                    .Replace("babelserver.org", "")
                    .Replace("otc.dyndns.org", "")
                );
            }
        }

        // add locales
        {
            string infoPath = "ApplicationInfo";
            var appInfoJson = Resources.Load<TextAsset>(infoPath)?.text; 
            var appInfo = JsonUtility.FromJson<ApplicationInfo>(appInfoJson);
            AddLocalizationToAndroidGradleProject(Path.Combine(outputPath, "launcher"), appInfo);
        }
        {
            // https://xindong.slack.com/archives/C062FTKRKCK/p1711348569697389?thread_ts=1707131944.702639&cid=C062FTKRKCK
            //delete GDT.aar temporarily
            var gdtAArPath = Path.Combine(outputPath, "unityLibrary","libs","GDTActionSDK.min.1.8.2.aar");
            var fileInfo = new FileInfo(gdtAArPath);
            if (fileInfo.Exists)
            {
                fileInfo.Delete();
            }
            var libraryBuildPath = Path.Combine(outputPath, "unityLibrary", "build.gradle");
            var libaryBuildContent = PADPostFileHelper.LoadConfigFile(libraryBuildPath);
       
            libaryBuildContent = libaryBuildContent.Replace("implementation(name: 'GDTActionSDK.min.1.8.2', ext:'aar')", "");
            File.WriteAllText(libraryBuildPath, libaryBuildContent);
        }
        // .Replace("android.enableJetifier=true","android.enableJetifier=true \n android.injected.testOnly=false"))
        // var gradlePropertiesString = File.ReadAllText(gradlePropertiesPath);
        PadAndroidBuildPostprocess.AfterAndroidBuild(outputPath);
        Debug.Log("AndroidPostBuild  Completed");
#endif
    }

#if UNITY_ANDROID

    const string k_InfoFile = "strings.xml";

    internal static string GenerateAndroidLanguageCode(ApplicationInfo appInfo, string lang) {
        // // When we use System Language as Locale Source Chinese (Simplified) code is represented as (zh-hans) and Chinese (Traditional) code is represented as (zh-hant).
        // // But Android Localization is case-sensitive and ony supports Chinese (Simplified) code as (zh-Hans) and Chinese (Traditional) code as (zh-Hant).
        // // https://developer.android.com/reference/java/util/Locale.LanguageRange
        // localeIdentifier = localeIdentifier.Contains("hans") ? localeIdentifier.Replace("hans", "Hans") : localeIdentifier.Contains("hant") ? localeIdentifier.Replace("hant", "Hant") : localeIdentifier;
        // var code = localeIdentifier;
        //
        // var IsSpecialLocaleIdentifier = code.Contains("Hans") || code.Contains("Hant") || code.Contains("Latn") || code.Contains("Cyrl") || code.Contains("Arab") || code.Contains("valencia");
        //
        // // The language is defined by a two-letter ISO 639-1 language code, optionally followed by a two letter ISO 3166-1-alpha-2 region code (preceded by lowercase r).
        // // The codes are not case-sensitive; the r prefix is used to distinguish the region portion. You cannot specify a region alone.
        // // https://developer.android.com/guide/topics/resources/providing-resources
        // localeIdentifier = code.Contains("-") ? IsSpecialLocaleIdentifier ? code.Replace("-", "+") : code.Replace("-", "-r") : localeIdentifier;
        var platformLocaleName = appInfo.platformLocales.Single(info => info.platform == "Android");
        return platformLocaleName.platformLocales[Array.IndexOf(platformLocaleName.locales, lang)];
    }

    static void GenerateLocalizedXmlFile(string filePath, ApplicationLocaleInfo appinfo) {
        var localizedString = appinfo.AppName;
        if (string.IsNullOrEmpty(localizedString))
            return;

        // We are adding a back slash when the entry value contains an single quote, to prevent android build failures and show the display name with apostrophe ex: " J'adore ";
        // (?<!\\) - Negative Lookbehind to ignore any that already start with \\
        // (?<replace>') - match colon and place it into the replace variable
        var rawAppName = appinfo.AppName;
        Config.LoadRegionInEditor();
        if (Config.IsGlobal())
        {
            rawAppName = "GO!GO!Muffin";
        }
        var localizedValue = Regex.Replace(rawAppName, @"(?<!\\)(?<replace>')", @"\'");

        using (var stream = new StreamWriter(filePath, false, Encoding.UTF8)) {
            stream.WriteLine(
                $@"<?xml version=""1.0"" encoding=""utf-8""?>" +
                "<!--" +
                "\n" +
                $"\t{k_InfoFile}\n" +
                $"\tThis file was auto-generated by {nameof(ApplicationLocaleInfo)}\n" +
                $"\tChanges to this file may cause incorrect behavior and will be lost if the project is rebuilt.\n" +
                $"-->" +
                "\n" +
                $@"<resources>
                       <string name=""app_name""> {localizedValue} </string>
                       </resources>");
        }
    }

    private static void AddLocalizationToAndroidGradleProject(string projectDirectory, ApplicationInfo appInfo) {
        if (appInfo == null)
            throw new ArgumentNullException(nameof(appInfo));

        var project = new GradleProjectSettings();
        foreach (var lang in appInfo.availableLocales) {
            if (appInfo.disabledLocales.Contains(lang)) {
                continue;
            }
            
            var localeIdentifier = GenerateAndroidLanguageCode(appInfo, lang);
            var xmlFile = Path.Combine(Directory.CreateDirectory(Path.Combine(project.GetResFolderPath(projectDirectory), "values-" + localeIdentifier)).FullName, k_InfoFile);
            GenerateLocalizedXmlFile(xmlFile, appInfo.locales.Single(info => info.Locale == lang));
        }
        
        var defaultXmlFile = Path.Combine(Path.Combine(project.GetResFolderPath(projectDirectory), "values"), k_InfoFile);
        File.WriteAllText(defaultXmlFile, File.ReadAllText(defaultXmlFile).Replace(PlayerSettings.productName, appInfo.locales.Single(locale => locale.Locale == appInfo.defaultLocale).AppName));

        var androidManifest = new AndroidManifest(project.GetManifestPath(projectDirectory));
        androidManifest.SetAtrribute("label", project.LabelName);

        androidManifest.SaveIfModified();
    }
    
#endif
}