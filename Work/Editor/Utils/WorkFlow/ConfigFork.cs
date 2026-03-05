using System;
using System.IO;
using SAGA.Editor;
using UnityEditor;
using UnityEngine;

public class ConfigFork
{
    [MenuItem(("Tools/添加Fork菜单"))]
    public static void AddCustomCommands()
    {
        var forkPath = string.Empty;
#if UNITY_EDITOR_WIN
        var appLocal = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        forkPath = Path.Combine(appLocal, "Fork");
#elif UNITY_EDITOR_OSX
        var userHomeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        forkPath = Path.Combine(userHomeDir, "Library", "Application Support", "com.DanPristupov.Fork");
#endif
        if (string.IsNullOrEmpty(forkPath) || !Directory.Exists(forkPath))
        {
            EditorUtility.DisplayDialog("提示", "找不到Fork程序目录", "ok");
            return;
        }

        var sourcePath = Path.Combine(Application.dataPath, "..", "_EditorFiles/fork-command/custom-commands.json");
        if (!File.Exists(sourcePath))
        {
            EditorUtility.DisplayDialog("提示", "找不到脚本数据", "ok");
            return;
        }

        var targetPath = Path.Combine(forkPath, "custom-commands.json");
        if (File.Exists(targetPath))
        {
            File.Delete(targetPath);
        }

        File.Copy(sourcePath, targetPath, true);

        EditorUtility.DisplayDialog("提示", "操作成功，请重启Fork程序", "ok");
    }
}