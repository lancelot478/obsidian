using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MiniJSON2;
using Newtonsoft.Json;
using PADPost;
using UnityEditor;
using UnityEngine;

public partial class BuildPackageEditor
{
    [MenuItem("BuildPackage/PreBuild/Test/PostTest", false)]
    public static void PostAndroidAndIOS_Test()
    {
#if UNITY_EDITOR 
#if UNITY_IOS
        var build_android_path = Path.Combine(AssetsFolderPath, "..", "build_ios");
        PostBuildPackageEditor.iOSPostBuild(build_android_path);
#endif
#if UNITY_ANDROID
        // var build_android_path = Path.Combine(AssetsFolderPath, "..", "build_android");
        // PADAssetManager.Log(build_android_path);
        // PadAndroidBuildPostprocess.AfterAndroidBuild(build_android_path);
        
#endif
#endif
    }

    [MenuItem("BuildPackage/PreBuild/带SDK(海外只有TW用XDSDK)", false)]
    public static void PreBuild_MenuItem_SDK()
    {
        _parameter |= BinaryParameter.SDK;
        Config.LoadRegionInEditor();
        if (Config.GetRegionName() == Config.CN)
        {
            _parameter |= BinaryParameter.CN;
        }
        PreBuildManifestProcess(_parameter,Config.GetRegionName());
        PreBuildProcess(_parameter);
    }
    [MenuItem("BuildPackage/PreBuild/不带SDK", false)]
    public static void PreBuild_MenuItem_NoSDK()
    {
        _parameter |= BinaryParameter.None;
        Config.LoadRegionInEditor();
        if (Config.GetRegionName() == Config.CN)
        {
            _parameter |= BinaryParameter.CN;
        }
        PreBuildManifestProcess(_parameter,Config.GetRegionName());
        PreBuildProcess(_parameter);
    }
    [MenuItem("BuildPackage/PreBuild/谷歌(默认带SDK)", false)]
    public static void PreBuild_MenuItem_Google()
    {
        _parameter = BinaryParameter.SDK | BinaryParameter.SplitPack;
        Config.LoadRegionInEditor();
        if (Config.GetRegionName() == Config.CN)
        {
            _parameter |= BinaryParameter.CN;
        }
        PreBuildManifestProcess(_parameter,Config.GetRegionName());
        PreBuildProcess(_parameter);
    }

    private static async void SwitchRegion(string region) {
        var regionResourcePath = Path.Combine(Application.dataPath, $"Resources/_CONSTS/PACKAGE_CHANNEL_TYPE.txt");
        await File.WriteAllTextAsync(regionResourcePath, Config.RegionNameToId[region].ToString());
        Config.LoadRegionInEditor();
    }
    
    
    private static async void SwitchLang(string region) {
        var configPath = Path.Combine(Application.dataPath, "Resources/_CONSTS/REGION_CONFIG.json"); 
        var text = File.ReadAllText(configPath);
        
        JSON json = new JSON();
        json.serialized = text;
        
        var replaceLang = json.ToJSON(region).ToJSON("lang");
        JSON twLang = json.ToJSON("TW");
        twLang["lang"] = replaceLang;
        
        var replaceHaoWan = json.ToJSON(region).ToJSON("hao_wan");
        twLang["hao_wan"] = replaceHaoWan;
      
        // var replaceHaoWanLang = replaceHaoWan["langID"];
        // JSON twHaoWan = twLang.ToJSON("hao_wan");
        // twHaoWan["langID"] = replaceHaoWanLang;
        
        await File.WriteAllTextAsync(configPath,json.serialized);
        AssetDatabase.SaveAssets();
        Config.LoadRegionInEditor();
    }
    // private const string TW_MENU = "切换地区/切换配置表/TW";
    // private const string CN_MENU = "切换地区/切换配置表/CN";
    // private const string JP_MENU = "切换地区/切换配置表/JP";
    // private const string KR_MENU = "切换地区/切换配置表/KR";
    // private const string SEA_MENU = "切换地区/切换配置表/SEA";
    // private const string NA_MENU = "切换地区/切换配置表/NA";
    // private const string EU_MENU = "切换地区/切换配置表/EU";
    // private const string VI_MENU = "切换地区/切换配置表/VI(越南)";
    
    // private const string TW_LANG_MENU = "切换地区/切换多语言/繁体";
    // private const string CN_LANG_MENU = "切换地区/切换多语言/简体";
    // private const string JP_LANG_MENU = "切换地区/切换多语言/日语";
    // private const string KR_LANG_MENU = "切换地区/切换多语言/韩语";
    // private const string SEA_LANG_MENU = "切换地区/切换多语言/英语";
    // private const string VI_LANG_MENU = "切换地区/切换多语言/越南语";

    // [MenuItem(TW_MENU, false)]
    public static void SwitchConfigTW() {
        SwitchRegion(Config.TW);
        AssetDatabase.Refresh();
    }

    // [MenuItem(CN_MENU, false)]
    public static void SwitchConfigCN() {
        SwitchRegion(Config.CN);
        AssetDatabase.Refresh();
    }
    
    // [MenuItem(JP_MENU, false)]
    public static void SwitchConfigJP() {
        SwitchRegion(Config.JP);
        AssetDatabase.Refresh();
    }
    
    // [MenuItem(KR_MENU, false)]
    public static void SwitchConfigKR() {
        SwitchRegion(Config.KR);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
    
    // [MenuItem(SEA_MENU, false)]
    public static void SwitchConfigGL() {
        SwitchRegion("GL");
        AssetDatabase.Refresh();
    }
    
    // [MenuItem(NA_MENU, false)]
    public static void SwitchConfigNA() {
        SwitchRegion("NA");
        AssetDatabase.Refresh();
    }
    
    // [MenuItem(EU_MENU, false)]
    public static void SwitchConfigEU() {
        SwitchRegion("EU");
        AssetDatabase.Refresh();
    }  
    // [MenuItem(VI_MENU, false)]
    public static void SwitchConfigVI() {
        SwitchRegion("VI");
        AssetDatabase.Refresh();
    }
    // [MenuItem(TW_LANG_MENU, false)]
    public static void SwitchLangTW() {
        SwitchLang("TW");
        AssetDatabase.Refresh();
    }
    // [MenuItem(CN_LANG_MENU, false)]
    public static void SwitchLangCN() {
        SwitchLang("CN");
        AssetDatabase.Refresh();
    }
    // [MenuItem(KR_LANG_MENU, false)]
    public static void SwitchLangKR() {
        SwitchLang("KR");
        AssetDatabase.Refresh();
    }
    // [MenuItem(JP_LANG_MENU, false)]
    public static void SwitchLangJP() {
        SwitchLang("JP");
        AssetDatabase.Refresh();
    }
    // [MenuItem(SEA_LANG_MENU, false)]
    public static void SwitchLangSEA() {
        SwitchLang("SEA");
        AssetDatabase.Refresh();
    }    
    // [MenuItem(VI_LANG_MENU, false)]
    public static void SwitchLangVI() {
        SwitchLang("VI");
        AssetDatabase.Refresh();
    }
    [MenuItem("BuildPackage/LocalTemp", false)]
    public static void PreBuild_STRIP_Local()
    {
        InputDialog window = (InputDialog)EditorWindow.GetWindow(typeof(InputDialog));
        window.titleContent = new GUIContent("本地打包配置修改");
        window.Show();
    }

    public static void PreBuild_Local_Temp_Confirm(string packageId,bool upDate,string serverString,bool isOnline)
    {
        var resourcesConstPath = Path.Combine(AssetsFolderPath, "Resources/_CONSTS");
   
        var serverPath = Path.Combine(resourcesConstPath,"SERVER_CONFIG.txt");
        var versionPackagePath = Path.Combine(resourcesConstPath,"VERSION_PACKAGE.txt");
        var hotUpdateEnabledPath = Path.Combine(resourcesConstPath,"HOT_UPDATE_ENABLED.txt");
        if(isOnline)
        {
            CscMacroReplaceAdd("Online");
        }
        else
        {
            CscMacroReplaceRemove("Online");
        }
       
        PADPostFileHelper.Write(serverPath, serverString);
        PADPostFileHelper.Write(versionPackagePath, packageId);
        PADPostFileHelper.Write(hotUpdateEnabledPath, upDate.ToString().ToLower());
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}

public static class BuildEditorConfig
{
    public  static Dictionary<string, string> ServerDic = new()
    {
        {"inner_test","172.25.135.20:8190"},
        {"cn_online","muffin-sh-prod-agent.xdgtw.cn:9190"},
        {"tw_online","muffin-tw-prod-agent.xdgtw.com:9190"},
    };

    public static List<string> OnlineList = new()
    {
        "cn_online","tw_online"
    };
}
public class InputDialog : EditorWindow
{
    private static int versionId;
    private static bool isUpdate;
    private static bool isOnline;
    private static string serverString;
    
    private static List<string> serverList = new()
    {
        "inner_test","cn_online","tw_online"
    };
   
    int toolbarInt;
    int toolbarIndex = -1;
    string[] toolbarStrings = { "内网", "国内正式服", "台湾正式服" };
    void OnGUI()
    {
        GUILayout.BeginVertical();
        GUILayout.Space(20);
        
        EditorGUILayout.LabelField("版本号:");
        versionId = EditorGUILayout.IntField("", versionId, EditorStyles.boldLabel);
        
        GUILayout.Space(20);

        // userInput = EditorGUILayout.Field("Input:", userInput, EditorStyles.boldLabel);
        EditorGUILayout.LabelField("热更开启:");
        isUpdate = EditorGUILayout.ToggleLeft("是否开启热更:", isUpdate, EditorStyles.boldLabel);
        
        GUILayout.Space(20);
        EditorGUILayout.LabelField("选择服务器:");
        toolbarInt = GUILayout.Toolbar(toolbarInt, toolbarStrings);
        if (toolbarIndex!=toolbarInt)
        {
            toolbarIndex = toolbarInt;
            BuildEditorConfig.ServerDic.TryGetValue(serverList[toolbarIndex],out serverString);
            isOnline = BuildEditorConfig.OnlineList.Contains(serverList[toolbarIndex]);
        }
        GUILayout.Space(100);
        GUILayout.ExpandHeight(true);
        if (GUILayout.Button("确认修改"))
        {
            if (versionId > 0)
            {
                BuildPackageEditor.PreBuild_Local_Temp_Confirm(versionId.ToString(), isUpdate,serverString,isOnline);
            }
        }
        GUILayout.EndVertical();
    }
}