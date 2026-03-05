using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using SAGA.Editor;
using UnityEditor;
using UnityEngine;

public partial class CheckUselessAssets
{
    private class EffectConfig
    {
        public int eType;
        public string eName;
        public int hPoint;
        public int id;
    }

    private const string _effectConfigPath = "Assets/Config/ttdbl2_config_client/EffectConfig.json";
    private const string _effectPath = "Assets/Prefabs/SkillEffect";
    private const string _tempEffectPath = "Assets/Prefabs/SkillEffectTemp";

    [MenuItem("Assets/检查无用资源/删除无引用特效资源")]
    public static void ExecuteEffectPrefab()
    {
        var effText = AssetDatabase.LoadAssetAtPath<TextAsset>(_effectConfigPath);
        if (effText == null)
        {
            Debug.LogError($"找不到特效配置: {_effectConfigPath}");
            return;
        }

        var loadData = JsonConvert.DeserializeObject<Dictionary<string, EffectConfig>>(effText.text);
        if (loadData == null)
        {
            Debug.LogError($"找不到特效配置解析失败: {_effectConfigPath}");
            return;
        }

        var configPrefabs = loadData.Values.Select(item => item.eName.ToLower()).ToList();

        var allPrefabs = Directory.GetFiles(_effectPath, "*.prefab", SearchOption.TopDirectoryOnly);
        var uselessPrefabs = allPrefabs.Where(item =>
        {
            var fileName = Path.GetFileNameWithoutExtension(item);
            return !configPrefabs.Contains(fileName.ToLower());
        });
        var results = CheckReferences(uselessPrefabs).Where(item => item.Value.Count <= 0).Select(item => item.Key);
        foreach (var prefab in results)
        {
            MarkAssetBundleNames.SetPathBundleName(prefab, string.Empty);
            var destFile = Path.Combine(_tempEffectPath, Path.GetFileName(prefab));
            File.Move(prefab, destFile);
        }

        AssetDatabase.Refresh();
    }
}