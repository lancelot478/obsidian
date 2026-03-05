using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text;
using System;
using System.Collections.Generic;
public class ClientToolsEditor : EditorWindow
{
    
    enum ServerArea  {
        CN,
        EU,
        JP,
        KR,
        NA,
        SEA,
        TW,
        VI,
    }

    private string editorUserIdFilePath;
    private string inputAccountId = "";
    private string inputServerAddress = ""; 

    [MenuItem("客户端工具/打开面板")]
    public static void ShowWindow() {
        var window = GetWindow<ClientToolsEditor>("客户端工具");
    }

    private void OnEnable()
    {
        inputAccountId = SystemInfo.deviceUniqueIdentifier;
        editorUserIdFilePath = Path.Combine(UnityEngine.Application.dataPath, "EditorUserId.txt");

        var localSavedUserIdFile = new FileInfo(editorUserIdFilePath);
        if (localSavedUserIdFile.Exists)
        {
            var lines = File.ReadAllLines(localSavedUserIdFile.FullName);
            if (lines.Length > 0 && !string.IsNullOrEmpty(lines[0])) inputAccountId = lines[0];
        }
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        OnSwitchConfigGUI();
        GUILayout.Space(10);
        OnAccountGUI();
        GUILayout.Space(10);
        OnServerGUI();
    }

    private void OnSwitchConfigGUI()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("配置切换", EditorStyles.boldLabel);
        GUILayout.Label("当前配置："+Config.GetRegionName(), EditorStyles.boldLabel);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("TW"))
        {
            BuildPackageEditor.SwitchConfigTW();
        }
        if (GUILayout.Button("CN"))
        {
            BuildPackageEditor.SwitchConfigCN();
        }
        if (GUILayout.Button("JP"))
        {
            BuildPackageEditor.SwitchConfigJP();
        }
        if (GUILayout.Button("KR"))
        {
            BuildPackageEditor.SwitchConfigKR();
        }
        if (GUILayout.Button("GL"))
        {
            BuildPackageEditor.SwitchConfigGL();
        }
        if (GUILayout.Button("VI"))
        {
            BuildPackageEditor.SwitchConfigVI();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        GUILayout.BeginHorizontal();
        GUILayout.Label("切换多语言", EditorStyles.boldLabel);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("TW"))
        {
            BuildPackageEditor.SwitchLangTW();
        }
        if (GUILayout.Button("CN"))
        {
            BuildPackageEditor.SwitchLangCN();
        }
        if (GUILayout.Button("JP"))
        {
            BuildPackageEditor.SwitchLangJP();
        }
        if (GUILayout.Button("KR"))
        {
            BuildPackageEditor.SwitchLangKR();
        }
        if (GUILayout.Button("SEA"))
        {
            BuildPackageEditor.SwitchLangSEA();
        }
        if (GUILayout.Button("VI"))
        {
            BuildPackageEditor.SwitchLangVI();
        }
        GUILayout.EndHorizontal();
    }

    private void OnAccountGUI()
    {
        inputAccountId = EditorGUILayout.TextField("账号ID", inputAccountId);
        if (GUILayout.Button("导入账号"))
        {
            ImportAccountId();
        }
    }

    private void OnServerGUI()
    {
        GUILayout.Label("服务器切换", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();
        GUILayout.Label("港澳台");
        if (GUILayout.Button("内网服"))
        {
            ImportServerConfig(ServerArea.TW, "172.25.135.20:8190");
        }
        if (GUILayout.Button("灰度服"))
        {
            ImportServerConfig(ServerArea.TW, "muffin-tw-pre-agent.xdgtw.com:9190");
        }
        if (GUILayout.Button("正式服"))
        {
            ImportServerConfig(ServerArea.TW, "muffin-tw-prod-agent.xdgtw.com:9190");
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("国区");
        if (GUILayout.Button("灰度服"))
        {
            ImportServerConfig(ServerArea.CN, "muffin-sh-pre-agent.xdgtw.cn:9190");
        }
        if (GUILayout.Button("正式服"))
        {
            ImportServerConfig(ServerArea.CN, "muffin-sh-prod-agent.xdgtw.cn:9190");
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("韩国");
        if (GUILayout.Button("灰度服"))
        {
            ImportServerConfig(ServerArea.KR, "muffin-kr-pre-agent.xdgtw.com:9190");
        }
        if (GUILayout.Button("正式服"))
        {
            ImportServerConfig(ServerArea.KR, "muffin-kr-prod-agent.xdgtw.com:9190");
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("日本");
        if (GUILayout.Button("灰度服"))
        {
            ImportServerConfig(ServerArea.JP, "muffin-jp-pre-agent.xdgtw.com:9190");
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("北美");
        if (GUILayout.Button("灰度服"))
        {
            ImportServerConfig(ServerArea.NA, "muffin-na-test-agent.xdgtw.com:9190");
        }
        if (GUILayout.Button("正式服"))
        {
            ImportServerConfig(ServerArea.NA, "muffin-na-prod-agent.xdgtw.com:9190");
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("欧洲");
        if (GUILayout.Button("灰度服"))
        {
            ImportServerConfig(ServerArea.EU, "muffin-eu-test-agent.xdgtw.com:9190");
        }
        if (GUILayout.Button("正式服"))
        {
            ImportServerConfig(ServerArea.EU, "muffin-eu-prod-agent.xdgtw.com:9190");
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("东南亚");
        if (GUILayout.Button("灰度服"))
        {
            ImportServerConfig(ServerArea.SEA, "muffin-sgp-test-agent.xdgtw.com:9190");
        }
        if (GUILayout.Button("正式服"))
        {
            ImportServerConfig(ServerArea.SEA, "muffin-sgp-prod-agent.xdgtw.com:9190");
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("越南");
        if (GUILayout.Button("版署服"))
        {
            ImportServerConfig(ServerArea.VI, "muffin-vn-banshu-agent.xdgtw.com:9190");
        }
        GUILayout.EndHorizontal();

        inputServerAddress = EditorGUILayout.TextField("自定义地址", inputServerAddress);
        if (GUILayout.Button("切换"))
        {
            ImportServerConfig(ServerArea.TW, inputServerAddress);
        }
    }

    private void ImportAccountId()
    {
        if (inputAccountId == "")
        {
            Debug.LogWarning("账号id不能为空！");
            return;
        }
 
        File.WriteAllText(editorUserIdFilePath, inputAccountId);
        EditorUtility.DisplayDialog("客户端工具", "账号id写入完成", "确认");
    }

    private void ImportServerConfig(ServerArea area, string address)
    {
        var configPath = GetServerConfigPath(area);

        if (configPath == "")
        {
            return;
        }

        File.WriteAllText(configPath, address);
        EditorUtility.DisplayDialog("客户端工具", "服务器地址切换完成", "确认");
    }

    private string GetServerConfigPath(ServerArea area)
    {
        var path = UnityEngine.Application.dataPath + "/Resources/_CONSTS/SERVER_CONFIG_EDITOR";
        switch(area)
        {
            case ServerArea.CN:
                return path + "/SERVER_CONFIG_EDITOR_CN.txt";
            case ServerArea.EU:
                return path + "/SERVER_CONFIG_EDITOR_EU.txt";
            case ServerArea.JP:
                return path + "/SERVER_CONFIG_EDITOR_JP.txt";
            case ServerArea.KR:
                return path + "/SERVER_CONFIG_EDITOR_KR.txt";
            case ServerArea.NA:
                return path + "/SERVER_CONFIG_EDITOR_NA.txt";
            case ServerArea.SEA:
                return path + "/SERVER_CONFIG_EDITOR_SEA.txt";
            case ServerArea.TW:
                return path + "/SERVER_CONFIG_EDITOR_TW.txt";
            case ServerArea.VI:
                return path + "/SERVER_CONFIG_EDITOR_VI.txt";
            default:
                return "";
        }
    }
}
