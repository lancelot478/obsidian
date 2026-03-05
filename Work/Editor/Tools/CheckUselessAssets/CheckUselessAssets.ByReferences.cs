using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SAGA.Editor;
using UnityEditor;
using UnityEngine;

public partial class CheckUselessAssets
{
    private static readonly List<string> WithoutExtensions = new List<string> {".prefab", ".unity", ".mat", ".asset", ".anim", ".playable"};

    [MenuItem("Assets/检查无用资源/删除无引用资源", true)]
    public static bool CheckExecuteReference()
    {
        return Selection.assetGUIDs.Length > 0;
    }

    [MenuItem("Assets/检查无用资源/删除无引用资源")]
    public static void ExecuteReference()
    {
        var assetGUIDs = Selection.assetGUIDs;

        var assetPaths = new string[assetGUIDs.Length];

        for (int i = 0; i < assetGUIDs.Length; i++)
        {
            assetPaths[i] = AssetDatabase.GUIDToAssetPath(assetGUIDs[i]);
        }

        var allAssets = new HashSet<string>();
        assetPaths.ToList().ForEach(path =>
        {
            if (Directory.Exists(path))
            {
                var allFiles = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
                allFiles.ToList().ForEach(item => allAssets.Add(item));
            }
            else
            {
                allAssets.Add(path);
            }
        });
        var result = CheckReferences(allAssets);

        if (result.Count < 100)
        {
            foreach (var kv in result)
            {
                var deps = kv.Value;
                var sb = new StringBuilder();
                sb.AppendLine($"{kv.Key}:\n");
                deps.ToList().ForEach(item => sb.AppendLine(item));
                Debug.Log(sb.ToString());
            }
        }

        foreach (var kv in result)
        {
            if (kv.Value.Count > 0)
            {
                continue;
            }

            File.Delete(kv.Key);
            Debug.LogError($"已删除无引用资源：{kv.Key}");
        }

        AssetDatabase.Refresh();
    }

    private static ConcurrentDictionary<string, HashSet<string>> CheckReferences(IEnumerable<string> paths)
    {
        var checkPaths = paths.Where(item => Path.GetExtension(item) != ".meta").ToDictionary(item => item, AssetDatabase.AssetPathToGUID).ToList();
        var allPaths = AssetDatabase.GetAllAssetPaths();
        var result = new ConcurrentDictionary<string, HashSet<string>>();
        Parallel.ForEach(allPaths, refPath =>
        {
            var extension = Path.GetExtension(refPath);
            if (!WithoutExtensions.Contains(extension))
            {
                return;
            }

            if (!File.Exists(refPath))
            {
                return;
            }

            var content = File.ReadAllText(refPath);
            if (string.IsNullOrEmpty(content))
            {
                return;
            }

            foreach (var checkInfo in checkPaths)
            {
                var checkPath = checkInfo.Key;
                var checkGuid = checkInfo.Value;
                var list = result.GetOrAdd(checkPath, param => new HashSet<string>());
                if (content.IndexOf(checkGuid, StringComparison.Ordinal) > 0)
                {
                    list.Add(refPath);
                }
            }
        });
        return result;
    }
}