using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using MiniJSON2;
using UnityEditor;
using Newtonsoft.Json;
using PADPost;
using SAGA.Editor;
using UnityEngine;
using PADPost;

using Unity.VisualScripting;
using UnityEditor.SceneManagement;

public partial class BuildPackageEditor
{
    private static BinaryParameter _parameter = BinaryParameter.None;

    public const bool AndroidPackageLimit2G = true; 
    public static List<string> AndroidPackageLimit2G_FolderList = new()
    {
        "assets",
        "atlas",
        "video",
        "modelmonster",
        "modelrole",
        "modelweapon",
        "modelpet",
        "modelmount",
        "bigworldmap",
        "modelanimation"
    }; 
    public static List<string> AndroidPackageLimit2G_FileList = new()
    {
        "modelshare",
        "fxshare",
        "sceneshare"
    }; 
    public const bool AddResource = true;
#if UNITY_SPLIT_PACK
    public const bool UsingUnitySplitPack = true;
#else
    public const bool UsingUnitySplitPack = false;
#endif
 
    [Flags]
    private enum BinaryParameter
    {
        None = 0,
        SDK = 1, //是否带SDK
        CN = 2, // 是否是国内
        SplitPack = 4, // 是否要分包
    }
    private static readonly string AssetsFolderPath = Application.dataPath;
    private const string CscDefineString = "-define:";
   
    private static void PreBuildManifestProcess(BinaryParameter parameter,string region,string channel = "TapTap")
    {
        ProcessRegion(region);
        
        var isHaoWan = Config.FirstGetHaoWanByRegion(region);
        var stripSDK = (parameter & BinaryParameter.SDK) == 0;
        var cnSDK = (parameter & BinaryParameter.CN) > 0;
        
        ChangeAndroidManifest(stripSDK,region);
        ChangeManifestFile(stripSDK,region);
        if (stripSDK)
        {
            CscMacroReplaceRemove("SDK");
            CscMacroReplaceRemove("HaoWan"); 
            PADPostFileHelper.DeleteDirectory(Path.Combine(Application.dataPath,"PnSDK"));
            return;
        }
        if (isHaoWan)
        {
            CscMacroReplaceRemove("SDK");
            CscMacroReplaceAdd("HaoWan"); 
        }
        else
        {
            CscMacroReplaceRemove("HaoWan");
            PADPostFileHelper.DeleteDirectory(Path.Combine(Application.dataPath,"PnSDK"));
            ChangeXDConfigFile(region,channel);
            ChangeTDSInfo(cnSDK);
            CscMacroReplaceRemove("HaoWan"); 
            CscMacroReplaceAdd("SDK"); 
        }
       
       
        AssetDatabase.Refresh();
        AssetDatabase.SaveAssets();
    }
    private static void PreBuildProcess(BinaryParameter parameter)
    {
        Config.LoadRegionInEditor();
        var stripSDK = (parameter & BinaryParameter.SDK) == 0;
        if (stripSDK)
        {
            return;
        }
        var cnSDK = (parameter & BinaryParameter.CN) > 0;
        var splitPack = (parameter & BinaryParameter.SplitPack) > 0;
        Debug.Log("cnSDK："+cnSDK+"splitPack："+splitPack);
        if (cnSDK)
        {
            CscMacroReplaceAdd("CN");
        }
        else
        {
            CscMacroReplaceRemove("CN");
        }
        PreBuildPackage_HaoWan(splitPack);
        if (splitPack)
        {
            if (!UsingUnitySplitPack)
            {
                PADPostFileHelper.CopyDirectory(Path.Combine(Application.dataPath, "..", "_PADCache/javaAsset/"),Path.Combine(Application.dataPath,"Plugins","Android"));
            }
            CscMacroReplaceAdd("SPLIT_PACK");
            
        }
        else
        {
            CscMacroReplaceRemove("SPLIT_PACK");
        }
        
       
        PADPostFileHelper.AddMainTemplateDeps("PAD","implementation 'com.google.android.play:asset-delivery:2.1.0'",
            !Config.IsHaoWan && splitPack && !UsingUnitySplitPack);
        
        
        
      
       
        
        EditorUserBuildSettings.buildAppBundle = splitPack;
        PlayerSettings.Android.useAPKExpansionFiles = splitPack && UsingUnitySplitPack;
        if (splitPack)
        {
            SplitAssetSettings();
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static int LoadRegionId()
    {
        var regionFile = Resources.Load<TextAsset>("_CONSTS/PACKAGE_CHANNEL_TYPE");
        if (!regionFile)
        {
            Debug.LogError("_CONSTS/PACKAGE_CHANNEL_TYPE 未找到！");
        }

        var region = regionFile.text;
        if (string.IsNullOrEmpty(region))
        {
            Debug.LogError("_CONSTS/PACKAGE_CHANNEL_TYPE 内容为空！");
        }
        int.TryParse(region, out var regionCode);
        return regionCode;
    }
    public static void ReplaceXdConfigChannel(string outPutPath,string channel)
    {
        // var configPath = Path.Combine(outPutPath,"unityLibrary/src/main/assets","XDConfig.json"); 
        var text = File.ReadAllText(outPutPath);
        

        var configMd = JsonConvert.DeserializeObject<XDConfigModel>(text);
        if (configMd == null)
        {
            Debug.LogError("XDConfig.json 解析失败！");
            return;
        }
        configMd.tapsdk.db_config.channel = channel;
        var data = Resources.Load<XDConfigData>("XDConfigData");
        data.channel = configMd.tapsdk.db_config.channel;

        var serializedText = JsonConvert.SerializeObject(configMd, Formatting.Indented,new JsonSerializerSettings { 
            NullValueHandling = NullValueHandling.Ignore
        });
        File.WriteAllText(outPutPath,serializedText);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

    }
    private static void ProcessRegion(string region) {
        var sourceApplicationInfoPath = Path.Combine(Application.dataPath, $"Editor/Resources/ApplicationInfo_{region}.json");
        var toApplicationInfoPath = Path.Combine(Application.dataPath, $"Editor/Resources/ApplicationInfo.json");
        PADPostFileHelper.FileCopy(sourceApplicationInfoPath, toApplicationInfoPath);
        
        var regionResourcePath = Path.Combine(Application.dataPath, $"Resources/_CONSTS/PACKAGE_CHANNEL_TYPE.txt");
        File.WriteAllText(regionResourcePath, Config.RegionNameToId[region].ToString());
    }
  

    private static void ChangeTDSInfo(bool isCn)
    {
        string tdsPath = Path.Combine(Application.dataPath,"Plugins", "TDS-Info.plist");
        var sourceText = File.ReadAllText(tdsPath);
        string destText;
        if (isCn)
        {
            destText = sourceText.Replace("8ijounjwvxgsp4sfso","27xdq3Q5qMrmRetbt6");
        }
        else
        {
            destText = sourceText.Replace("27xdq3Q5qMrmRetbt6","8ijounjwvxgsp4sfso");
        }
        File.WriteAllText(tdsPath,destText);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
    private static void ChangeAndroidManifest(bool stripSDK,string region)
    {
        string srcPath = Path.Combine(Application.dataPath, "Plugins/Android/AndroidManifest_Region",$"AndroidManifest_{region}");
        string noSDKPath = Path.Combine(Application.dataPath, "Plugins/Android/AndroidManifest_Region","AndroidManifest_NoSDK");
       
       
        string sourcePath = (stripSDK ?  noSDKPath : srcPath).Replace("\\", "/");
        string destPath =  Path.Combine(Application.dataPath, "Plugins/Android","AndroidManifest.xml").Replace("\\", "/");
        var sourceText = File.ReadAllText(sourcePath);
        File.WriteAllText(destPath,sourceText);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
    private static void SplitAssetSettings()
    {
        var source = UsingUnitySplitPack ? "**SPLITS**" : "**PLAY_ASSET_PACKS****SPLITS**";
        var dest = UsingUnitySplitPack ? "**PLAY_ASSET_PACKS****SPLITS**" : "**SPLITS**";
        string launcherTemplatePath = Path.Combine(Application.dataPath,  "Plugins/Android/launcherTemplate.gradle");
        if (File.Exists(launcherTemplatePath)) {
            string fileString = File.ReadAllText(launcherTemplatePath);
            if (!fileString.Contains(dest))
            {
                fileString = fileString.Replace(source, dest);
            }
          
            File.WriteAllText(launcherTemplatePath, fileString);
        }
        AssetDatabase.Refresh();
        AssetDatabase.SaveAssets();
    }
    
    private static void SDKPreSettingsBeforeBuild(string target,bool isGoogle){
        
        var regionId = LoadRegionId();
        Config.LoadRegionInEditor();
        var regionName = Config.RegionIdToName[regionId];
        
        XdConfigSetting(target,isGoogle);
        
        ProjectSetting(target,isGoogle,regionName);

        GoogleServiceSetting(target,isGoogle,regionName);
    }
    
    
    private static void ChangeManifestFile(bool stripSDK,string region)
    {
        
        var destPath = Path.Combine(AssetsFolderPath, "../Packages/manifest.json");
        var destData = JsonConvert.DeserializeObject<Manifest>(File.ReadAllText(destPath));
        var destJsonDependency = destData.dependencies;

        string sourcePath;
        if (stripSDK)
        {
            sourcePath = Path.Combine(Application.dataPath, "XDSDK_Region", "Manifest_NoSDK.json");
            File.WriteAllText(destPath, File.ReadAllText(sourcePath));
            return;
        }

        sourcePath  = Path.Combine(Application.dataPath, "XDSDK_Region",$"Manifest_{region}.json");
      
       
        
       
        var sourceJson = new JSON
        {
            serialized = File.ReadAllText(sourcePath)
        };
        foreach (var kv in sourceJson.fields)
        {
            destJsonDependency[kv.Key] = kv.Value.ToString();
        }
        var serializedText = JsonConvert.SerializeObject(destData, Formatting.Indented, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        });
        File.WriteAllText(destPath, serializedText);
        AssetDatabase.Refresh();
        AssetDatabase.SaveAssets();
    }
    private static void CscMacroReplace(string older, string replace)
    {
        var cscRspPath = Path.Combine(AssetsFolderPath, "csc.rsp");
        var cscContent = PADPostFileHelper.LoadConfigFile(cscRspPath);
        if (cscContent.Contains(older))
        {
            cscContent = cscContent.Replace(older, replace);
        }

        File.WriteAllText(cscRspPath, cscContent);
    }

    private static void CscMacroReplaceAdd(string macro)
    {
        CscMacroReplace("\n" + "#" + CscDefineString + macro, "\n" + CscDefineString + macro);
    }

    private static void CscMacroReplaceRemove(string macro)
    {
        CscMacroReplace("\n" + CscDefineString + macro, "\n" + "#" + CscDefineString + macro);
    }




    static private void ProjectSetting(string target,bool isGoogle,string regionName)
    {
        var bundleDic = Config.BundleIdDic[regionName];
        
        string platform = target == "ios" ? Config.iOS : isGoogle ? Config.Google : Config.Android;
        bundleDic.TryGetValue(platform,out var packageId);

        
        if (target == "android")
        {
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, packageId);
        }
        else
        {
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, packageId);
        }
        AssetDatabase.Refresh();
        AssetDatabase.SaveAssets();
    }
    private static void GoogleServiceSetting(string target,bool isGoogle,string regionName)
    {
        if (target == "ios")
        {
            return;
        }

        if (regionName == Config.CN)
        {
            return;
        }
        var gpFatherPath  = Path.Combine(Application.dataPath, "XDSDK_Region",$"GP_{regionName}");
    
        string googlePath = Path.Combine(gpFatherPath, "google-service-google.json");
        string taptapPath = Path.Combine(gpFatherPath, "google-service-taptap.json");

        string sourcePath = (isGoogle ? googlePath : taptapPath).Replace("\\", "/");
        string destPath =  Path.Combine(Application.dataPath, "Plugins/Android/google-services.json").Replace("\\", "/");
        var sourceText = File.ReadAllText(sourcePath);
        
        File.WriteAllText(destPath,sourceText);
    }
    private static void ChangeXDConfigFile(string region,string channel)
    {
       
        string destPath = Path.Combine(Application.dataPath, "XDConfig.json");
        var sourcePath  = Path.Combine(Application.dataPath, "XDSDK_Region",$"XDConfig_{region}.json");
        
        var sourceText = File.ReadAllText(sourcePath);
        
        
        var configMd = JsonConvert.DeserializeObject<XDConfigModel>(sourceText);
        if (configMd == null)
        {
            Debug.LogError("XDConfig.json 解析失败！");
            return;
        }
        configMd.tapsdk.db_config.channel = channel;
        var data = Resources.Load<XDConfigData>("XDConfigData");
        data.channel = configMd.tapsdk.db_config.channel;

        var serializedText = JsonConvert.SerializeObject(configMd, Formatting.Indented,new JsonSerializerSettings { 
            NullValueHandling = NullValueHandling.Ignore
        });
       
        File.WriteAllText(destPath,serializedText);
    }
    static void XdConfigSetting(string target,bool isGoogle)
    {
#if SDK
        var configPath = Path.Combine(Application.dataPath, "XDConfig.json"); 
        var text = File.ReadAllText(configPath);
        

        var configMd = JsonConvert.DeserializeObject<XDConfigModel>(text);
        if (configMd == null)
        {
            Debug.LogError("XDConfig.json 解析失败！");
            return;
        }

        var oldChannel = configMd.tapsdk.db_config.channel;
        configMd.tapsdk.db_config.channel = target == "ios" ? "iOS" : isGoogle ? "Google" : oldChannel;
       
        
        var data = Resources.Load<XDConfigData>("XDConfigData");
        data.region_type = configMd.region_type;
        data.client_id = configMd.tapsdk.client_id;
        data.channel = configMd.tapsdk.db_config.channel;
        data.appId = configMd.app_id;
        
        var serializedText = JsonConvert.SerializeObject(configMd, Formatting.Indented,new JsonSerializerSettings { 
            NullValueHandling = NullValueHandling.Ignore
        });
        File.WriteAllText(configPath,serializedText);
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
#endif
        
    }
    
    [Serializable]
    private class Manifest {
        public Dictionary<string,string> dependencies;
        public List<object> scopedRegistries;
    }
    [Serializable]
   private class XDConfigModel{
        public Apple apple{ get; set; }
        public string region_type{ get; set; }
        public string bundle_id{ get; set; }
        public string client_id{ get; set; }
        public string app_id{ get; set; }
        public Aliyun aliyun{ get; set; }
        public bool idfa_enabled{ get; set; }
        public string game_name{ get; set; }
        public string report_url{ get; set; }
        public string logout_url{ get; set; }
        public string web_pay_url{ get; set; }
        public Tapsdk tapsdk{ get; set; }
        public List<string> logos{ get; set; }
        public Firebase firebase{ get; set; }
        public Facebook facebook{ get; set; }
        public Line line{ get; set; }
        public Twitter twitter{ get; set; }
        public Google google{ get; set; }
        public Adjust adjust{ get; set; }
        public Appsflyer appsflyer{ get; set; }
        public ADConfig ad_config { get; set; }
        public QQConfig qq { get; set; }
        public WeChatConfig wechat { get; set; }
        public WeiBoConfig weibo { get; set; }
        public XHSConfig xhs { get; set; }
    }
   [Serializable]
   private class Apple{
        public string service_id{ get; set; }
    }     
   [Serializable]
   private class Adjust{
        public string app_token{ get; set; }
        public List<Event> events{ get; set; }
    }    
    [Serializable]
    private class Aliyun{
        public string phone_auth_token_ios{ get; set; }
        public string phone_auth_token_android{ get; set; }
    }
    [Serializable]
    private class Appsflyer{
        public string app_id{ get; set; }
        public string dev_key_android{ get; set; }
        public string dev_key{ get; set; }
        public string dev_key_ios{ get; set; }
    }
    [Serializable]
    private class DbConfig{
        public bool enable{ get; set; }
        public string channel{ get; set; }
        public string game_version{ get; set; }
    }
    [Serializable]
    private class Event{
        public string event_name{ get; set; }
        public string token{ get; set; }
    }
    [Serializable]
    private class Firebase{
        public bool enableTrack{ get; set; }
    }
    [Serializable]
    private class Facebook{
        public string app_id{ get; set; }
        public string client_token{ get; set; }
        public List<string> permissions{ get; set; }
    }
    [Serializable]
    private class Google{
        public string CLIENT_ID{ get; set; }
        public string CLIENT_ID_FOR_ANDROID{ get; set; }
    }
    [Serializable]
    private class Line{
        public string channel_id{ get; set; }
    }
    [Serializable]
    private class Tapsdk{
        public string client_id{ get; set; }
        public string client_token{ get; set; }
        public string server_url{ get; set; }
        public DbConfig db_config{ get; set; }
        public List<string> permissions{ get; set; }
    }
    [Serializable]
    private class Twitter{
        public string consumer_key{ get; set; }
        public string consumer_secret{ get; set; }
    }
    [Serializable]
    private class TTConfig
    {
        public string app_id { get; set; }
        public string app_name { get; set; }
        public string ios_enable { get; set; }
    }
    [Serializable]
    private class GDTConfig
    {
        public string user_action_set_id { get; set; }
        public string app_secret_key { get; set; }
    }
    [Serializable]
    private class ADConfig
    {
        public TTConfig tt_config { get; set; }
        public GDTConfig gdt_config { get; set; }
    }
    [Serializable]
    private class QQConfig
    {
        public string app_id { get; set; }
        public string universal_link { get; set; }
    }
    [Serializable]
    private class WeChatConfig
    {
        public string app_id { get; set; }
        public string universal_link { get; set; }
    }
    [Serializable]
    private class WeiBoConfig
    {
        public string app_id { get; set; }
        public string universal_link { get; set; }
    }
    [Serializable]
    private class XHSConfig
    {
        public string app_id_ios { get; set; }
        public string app_id_android { get; set; }
        public string universal_link { get; set; }
    }
    
#region cmd
    public static void ReplaceXdConfigChannelCmd()
    {
#if !SDK || HaoWan
        return;
#endif
        string path = "";
        string value = "";
        var args = CommandLine.Parse(Environment.GetCommandLineArgs());

        if (args.ArgPairs.TryGetValue("path", out var tempValue)) {
            path = tempValue ;
        }
        if (args.ArgPairs.TryGetValue("channel", out tempValue)) {
            value = tempValue ;
        }

        // ReplaceXdConfigChannel(path, value);
    }
    private static void PreBuildManifestCmd()
    {
        bool isSDK = false, isCn = false, isSplitPack = false;
        string region = null;
        string channel = null;
        var args = CommandLine.Parse(Environment.GetCommandLineArgs());

        if (args.ArgPairs.TryGetValue("isSDK", out var tempValue)) {
            isSDK = tempValue == "true";
        }
        if (args.ArgPairs.TryGetValue("region", out tempValue)) {
            region = tempValue;
        }
        if (args.ArgPairs.TryGetValue("channel", out tempValue)) {
            channel = tempValue;
        }
        isCn = region == "CN";

        var sdkOptions = isSDK ? BinaryParameter.SDK : BinaryParameter.None;
        var cnOptions = isCn ? BinaryParameter.CN : BinaryParameter.None;
        var parameter = sdkOptions | cnOptions ;
        Debug.Log("PreBuildManifestCmd"+"  "+sdkOptions+"  "+cnOptions+"  "+sdkOptions+channel);
        PreBuildManifestProcess(parameter,region,channel);
       
    }
    private static void PreBuildProcessCmd()
    {
        bool isSDK = false, isSplitPack = false;
        var args = CommandLine.Parse(Environment.GetCommandLineArgs());

        if (args.ArgPairs.TryGetValue("isSDK", out var tempValue)) {
            isSDK = tempValue == "true";
        }
        // if (args.ArgPairs.TryGetValue("isCn", out tempValue)) {
        //     isCn = tempValue == "true";
        // }
        if (args.ArgPairs.TryGetValue("isSplitPack", out tempValue)) {
            isSplitPack = tempValue == "true";
        }

        var region = LoadRegionId();
        Config.LoadRegionInEditor();
        var isCn = Config.RegionIdToName[region] == Config.CN;
        var sdkOptions = isSDK ? BinaryParameter.SDK : BinaryParameter.None;
        var cnOptions = isCn ? BinaryParameter.CN : BinaryParameter.None;
        var splitPackOptions = isSplitPack ? BinaryParameter.SplitPack : BinaryParameter.None;
        var parameter = sdkOptions | cnOptions | splitPackOptions;
        Debug.Log("PreBuildProcessCmd"+"  "+sdkOptions+"  "+cnOptions+"  "+sdkOptions+"  "+splitPackOptions);
        PreBuildProcess(parameter);
    }
#endregion
}