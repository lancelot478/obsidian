using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using XDPlugin.Tools;

namespace SAGA.Editor
{
    public partial class CheckRedundantAssetsWindow
    {
        private Dictionary<string, HashSet<string>> AnalyseAssetBundle()
        {
            
            
            var assetBundleNames = AssetDatabase.GetAllAssetBundleNames();
            var dataStruct = new Dictionary<string, HashSet<string>>();
            foreach (var bundleName in assetBundleNames)
            {
                var assetPaths = AssetDatabase.GetAssetPathsFromAssetBundle(bundleName);
                if (assetPaths == null) continue;
                foreach (var assetPath in assetPaths)
                {
                    var depAssets = AssetDatabase.GetDependencies(assetPath, true);
                    if (depAssets == null) continue;
                    foreach (var asset in depAssets)
                    {
                        var assetBundleName = AssetDatabase.GetImplicitAssetBundleName(asset);
                        if (!string.IsNullOrEmpty(assetBundleName))
                        {
                            if (!dataStruct.TryGetValue(asset, out var _))
                            {
                                dataStruct.Add(asset, new HashSet<string>() {assetBundleName});
                            }
                        }
                        else
                        {
                            var valid = ValidateAsset(asset);
                            if (!valid) continue;
                            if (!dataStruct.TryGetValue(asset, out var bundles))
                            {
                                bundles = new HashSet<string>();
                                dataStruct.Add(asset, bundles);
                            }

                            bundles.Add(bundleName);
                        }
                    }
                }
            }

            dataStruct = dataStruct.OrderBy(item => item.Key).ToDictionary(item => item.Key, item => item.Value);
            return dataStruct;
        }

        public string GetAssetBundleName(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath);
            if (importer == null)
            {
                return string.Empty;
            }

            var bundleName = importer.assetBundleName;
            if (importer.assetBundleVariant.Length > 0)
            {
                bundleName = bundleName + "." + importer.assetBundleVariant;
            }

            return bundleName;
        }

        private static bool ValidateAsset(string assetName)
        {
            // if (!assetName.StartsWith("Assets/"))
            //     return false;
            var ext = Path.GetExtension(assetName);
            return ext != ".dll" && ext != ".cs" && ext != ".meta" && ext != ".js" && ext != ".boo";
        }

        private void ExportLog(string filePath)
        {
            var sb = new StringBuilder();
            foreach (var data in FilteredAnalyseData)
            {
                var size = AssetHelper.GetFileSize(data.Key);
                var count = data.Value.Count;
                var redundantSize = size * (data.Value.Count - 1);
                sb.AppendLine($"{count},{size},{redundantSize},{data.Key}");
            }
            File.WriteAllText(filePath, sb.ToString());
            FileHelper.OpenDirectory(filePath, true);
            EditorHelper.ShowDialog("报告导出成功，是否打开？", () =>
            {
                EditorUtility.OpenWithDefaultApp(filePath);
            });
        }
    }
}