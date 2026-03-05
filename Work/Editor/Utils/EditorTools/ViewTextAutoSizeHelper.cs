using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SAGA.Editor;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class ViewTextAutoSizeHelper : AssetPostprocessor
{
    private const float minSize = 24.0f;

    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        return;
        Process(importedAssets);
    }

    [MenuItem("Assets/界面Text自适应")]
    private static void ManualProcess()
    {
        var selectedObjects = Selection.GetFiltered(typeof(Object), SelectionMode.DeepAssets);
        var prefabPaths = selectedObjects.Select(AssetDatabase.GetAssetPath)
            .Where(assetPath => assetPath.EndsWith(".prefab")).ToArray();
        Process(prefabPaths);
    }

    private static void Process(string[] importedAssets)
    {
        var changed = false;
        foreach (string importedAsset in importedAssets)
        {
            if (!importedAsset.EndsWith(".prefab"))
            {
                continue;
            }

            if (!importedAsset.StartsWith("Assets/Prefabs/Plane"))
            {
                continue;
            }

            var gameObject = PrefabUtility.LoadPrefabContents(importedAsset);
            var changeList = Change(importedAsset, gameObject);
            changed = changed || changeList.Count > 0;
            if (changeList.Count > 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"TMPText组件自适应：{importedAsset}");
                foreach (var line in changeList)
                {
                    sb.AppendLine($"节点路径：{line}");
                }

                Debug.LogError(sb.ToString());
            }

            PrefabUtility.SaveAsPrefabAsset(gameObject, importedAsset);
        }

        if (changed)
        {
            EditorHelper.ShowDialog(
                "存在界面Prefab的文本未做自适应，已经调整好了，观察控制台日志查看列表。\n如果有你的界面，记得提交，反之请忽略此信息。\n（能通知到对应的人或者发群里就更好了）", ok: "知道了",
                cancel: "好的");
        }
    }

    private static List<string> Change(string path, GameObject gameObject)
    {
        if (!gameObject)
        {
            return default;
        }

        var result = new List<string>();

        var tmpList = gameObject.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var text in tmpList)
        {
            if (text.enableAutoSizing)
            {
                continue;
            }

            var fontSize = text.fontSize;
            if (fontSize <= minSize)
            {
                continue;
            }

            text.fontSizeMin = minSize;
            text.fontSizeMax = fontSize;
            text.enableAutoSizing = true;

            result.Add(GetFullPath(text.transform));
        }

        var textList = gameObject.GetComponentsInChildren<Text>(true);
        foreach (var text in textList)
        {
            if (text.resizeTextForBestFit)
            {
                continue;
            }

            var fontSize = text.fontSize;
            if (fontSize <= minSize)
            {
                continue;
            }

            text.resizeTextMinSize = (int)minSize;
            text.resizeTextMaxSize = fontSize;
            text.resizeTextForBestFit = true;
            result.Add(GetFullPath(text.transform));
        }

        return result;
    }

    private static string GetFullPath(Transform tra)
    {
        if (tra == null)
        {
            return string.Empty;
        }

        if (tra.parent == null || tra.GetComponent<Canvas>() != null)
        {
            return string.Empty;
        }

        var parent = GetFullPath(tra.parent);
        var fix = string.IsNullOrEmpty(parent) ? "" : $"{parent}/";
        return fix + tra.name;
    }
}