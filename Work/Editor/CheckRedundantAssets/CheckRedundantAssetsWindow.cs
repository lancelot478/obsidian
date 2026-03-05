using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace SAGA.Editor
{
    public partial class CheckRedundantAssetsWindow : EditorWindow
    {
        [MenuItem("Tools/CheckRedundantAssets")]
        public static CheckRedundantAssetsWindow OpenAssetConfigWindow()
        {
            var window = CreateWindow<CheckRedundantAssetsWindow>();
            window.titleContent = new GUIContent(nameof(CheckRedundantAssetsWindow));
            window.Focus();
            window.Repaint();
            return window;
        }

        private void DrawToolButton()
        {
            EditorLayout.ExpandHorizontalScope(() =>
            {
                EditorLayout.ToolBarButton("Analyse", () => AnalyseData = null, "执行分析Bundle的操作");
                EditorLayout.ToolBarButton("MarkRedundant", () =>
                {
                    var oriAssetBundleNames = AssetDatabase.GetAllAssetBundleNames();
                    var assetBundleNames = new HashSet<string>();
                    foreach (var bundleName in oriAssetBundleNames)
                    {
                        assetBundleNames.Add(bundleName);
                    }

                    var pathState = new HashSet<string>();
                    var pathList = new Dictionary<string, HashSet<string>>();
                    foreach (var bundleData in AnalyseData)
                    {
                        var path = bundleData.Key;
                        var parent = path.Substring(0, path.LastIndexOf("/", StringComparison.Ordinal));
                        if (!pathList.TryGetValue(parent, out var pathArray))
                        {
                            pathArray = new HashSet<string>();
                            pathList.Add(parent, pathArray);
                        }

                        pathArray.Add(path);

                        var getState = bundleData.Value.Count > 1;
                        if (getState)
                        {
                            pathState.Add(parent);
                        }
                    }

                    pathList = pathList
                        .OrderByDescending(item => item.Key.Split('/').Length)
                        .ToDictionary(item => item.Key, item => item.Value);
                    foreach (var pathInfo in pathList)
                    {
                        var parentPath = pathInfo.Key;
                        if (!pathState.Contains(parentPath))
                        {
                            continue;
                        }

                        var childPath = pathInfo.Value
                            .OrderByDescending(item => item.Split('/').Length)
                            .ToArray();
                        foreach (var path in childPath)
                        {
                            var haveName = AssetDatabase.GetImplicitAssetBundleName(path);
                            if (!string.IsNullOrEmpty(haveName))
                            {
                                continue;
                            }

                            // parentPath = "art/ta"
                            // childFolder = "art/ta/test"
                            // parentFolder = "art"
                            // 找到子文件夹
                            var childFolder = assetBundleNames.ToList().Find(item => item.StartsWith($"{parentPath}/") && item.Length > parentPath.Length);
                            // 找到父文件夹
                            var parentFolder = assetBundleNames.ToList().Find(item => parentPath.StartsWith($"{item}/"));
                            string newName;
                            if (string.IsNullOrEmpty(childFolder))
                            {
                                newName = string.IsNullOrEmpty(parentFolder) ? parentPath : parentFolder;
                            }
                            else
                            {
                                newName = MarkAssetBundleNames.GetAssetBundleName(path);
                            }

                            assetBundleNames.Add(newName);
                            MarkAssetBundleNames.SetPathBundleName(path, newName);
                        }
                    }
                }, "自动处理冗余资源");
                EditorLayout.ToolBarButton("BuildBundle", () =>
                {
                    var platform = AssetBundleEditor.GetPlatformTarget();
                    AssetBundleEditor.BuildAllAssetsBundlesWithPath(platform);
                }, "生成AssetBundle");
                EditorLayout.ToolBarButton("LogReport", () =>
                {
                    FileHelper.SelectFolder((folderName) =>
                    {
                        var filePath = Path.Combine(folderName, "redundant.csv");
                        ExportLog(filePath);
                    }, "选择保存文件夹");
                }, "导出报告");
                var showRTip = FilterRedundant ? "仅显示冗余" : "显示全部";
                EditorLayout.MiniToggleButton(FilterRedundant, newState => { FilterRedundant = newState; }, "R",
                    $"当前{showRTip}资源");
            });
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }

        private void OnGUI()
        {
            DrawToolButton();

            var topOffset = 30;
            var viewHeight = position.height - topOffset;

            var assetLeft = 0;
            var assetWidth = position.width * 0.6f;

            // 绘制资源列表
            var leftRect = new Rect(assetLeft, topOffset, assetWidth, viewHeight);
            using (new GUILayout.VerticalScope())
            {
                AssetsAssetTreeView?.OnGUI(leftRect);
            }

            // 资源对于的Bundle列表
            var bundleLeft = assetWidth;
            var bundleWidth = position.width - assetWidth;
            BundleTreeView?.OnGUI(new Rect(bundleLeft, topOffset, bundleWidth, viewHeight));
        }
    }
}