using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace SAGA.Editor
{
    public class MarkAssetBundleNames
    {
        private static readonly string LevelAssetsPathPrefix = "/Art/Environment/";

        private static string[] GetSelectionPath()
        {
            var assets = Selection.assetGUIDs.Select(AssetDatabase.GUIDToAssetPath).ToArray();
            return assets;
        }

        public static string GetAssetBundleName(string path)
        {
            var haveExtension = Path.HasExtension(path);
            string bundleName;
            if (haveExtension)
            {
                var lastIndex = path.LastIndexOf(".", StringComparison.Ordinal);
                bundleName = path.Substring(0, lastIndex);
                //特效打包
                if (bundleName.Contains("Assets/Prefabs/SkillEffect"))
                    bundleName = bundleName.Replace("Assets/Prefabs/SkillEffect", "fx");
                //plane打包
                // if(bundleName.Contains("Assets/Prefabs/Plane")) {
                //     bundleName = bundleName.Replace("Assets/Prefabs/Plane","plane");
                // }
            }
            else
            {
                bundleName = path;
            }

            return bundleName;
        }

        public static void SetPathAssetBundleName(string path, string name = null)
        {
            var bundleName = GetAssetBundleName(path);
            if (name != null)
                bundleName = name;
            SetPathBundleName(path, bundleName);
        }

        public static void SetPathBundleName(string path, string bundleName)
        {
            if (string.IsNullOrEmpty(path)) return;

            var importer = AssetImporter.GetAtPath(path);
            if (importer == null) return;

            importer.assetBundleName = bundleName.Replace(" ", string.Empty);
        }

        public static string GetPathBundleName(string path)
        {
            if (string.IsNullOrEmpty(path)) return default;

            var importer = AssetImporter.GetAtPath(path);
            return importer == null ? default : importer.assetBundleName;
        }

        [MenuItem("Assets/AssetPathPrint")]
        private static void MarkAssetBundlePathPrint()
        {
            var paths = GetSelectionPath();
            foreach (var path in paths)
            {
                var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                    .Where(item => Path.GetExtension(item) != ".meta");
                foreach (var file in files) Debug.Log(file);
                var dirs = Directory.GetDirectories(path, "*.*", SearchOption.AllDirectories);
                foreach (var dir in dirs) Debug.Log(dir);
            }
        }

        [MenuItem("Assets/资源/ClearAll")]
        private static void MarkCleanAll()
        {
            var sel = GetSelectionPath();
            foreach (var path in sel)
            {
                var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
                foreach (var file in files) SetPathBundleName(file, string.Empty);

                var dirs = Directory.GetDirectories(path, "*.*", SearchOption.AllDirectories);
                foreach (var dir in dirs) SetPathBundleName(dir, string.Empty);
            }
        }

        [MenuItem("Assets/资源/Mark")]
        private static void MarkSelectionAsset()
        {
            var path = GetSelectionPath();
            foreach (var s in path) SetPathAssetBundleName(s);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Assets/资源/Recursive/File")]
        public static void MarkDirectoryRecursiveFile()
        {
            var path = GetSelectionPath();
            foreach (var s in path)
            {
                var allFiles = Directory.GetFiles(s, "*.*", SearchOption.AllDirectories)
                    .Where(item => Path.GetExtension(item) != ".meta");
                foreach (var filePath in allFiles) SetPathAssetBundleName(filePath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Assets/资源/设置ChapterBg(主线任务章节背景图片bundleName)")]
        public static void MarkDirectoryChapterFile()
        {
            var path = "Assets/Textures/Atlas/ChapterBg";
            var allFiles = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                .Where(item => Path.GetExtension(item) != ".meta");
            foreach (var filePath in allFiles)
            {
                var importer = AssetImporter.GetAtPath(filePath);
                if (importer == null) return;

                var lastIndex = filePath.LastIndexOf(".", StringComparison.Ordinal);
                var bundleName = filePath.Substring(0, lastIndex);
                bundleName = bundleName.Replace("Assets/Textures/", "");
                importer.assetBundleName = bundleName.Replace(" ", string.Empty);
            } 
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Assets/资源/设置所有子目录资源bundle为自身相同bundleName,完成后清除自身bundleName")]
        public static void MarkDirectoryRecursiveFileAsSelf()
        {
            var importer = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]));
            var bundleName = importer.assetBundleName;
            var path = GetSelectionPath();
            foreach (var s in path)
            {
                var allFiles = Directory.GetFiles(s, "*.*", SearchOption.AllDirectories)
                    .Where(item => Path.GetExtension(item) != ".meta");
                foreach (var filePath in allFiles) SetPathAssetBundleName(filePath, bundleName);
            }

            importer.assetBundleName = null;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Assets/资源/(须选中ModelAsset目录)设置场景ModelAssets目录下所有资源Bundle")]
        public static void MarkDirectoryRecursiveFileAsParentLevelFolder()
        {
            var importer = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]));
            var path2 = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);
            var lastIndex = path2.LastIndexOf("/");
            path2 = path2.Remove(lastIndex);
            lastIndex = path2.LastIndexOf("/");
            // path2 = path2.Substring(lastIndex + 1, 9);
            path2 = path2.Remove(0,lastIndex);
            var bundleName = $"assets/art/environment/levels/{path2}_sceneassets";
            var path = GetSelectionPath();
            foreach (var s in path)
            {
                var allFiles = Directory.GetFiles(s, "*.*", SearchOption.AllDirectories)
                    .Where(item => Path.GetExtension(item) != ".meta");
                foreach (var filePath in allFiles) SetPathAssetBundleName(filePath, bundleName);
            }

            importer.assetBundleName = null;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        [MenuItem("Assets/资源/(须选中场景文件)设置场景Bundle")]
        public static void MarkSceneBundle()
        {
            var importer = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]));
            var path2 = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);
            var lastIndex = path2.LastIndexOf("/");
            path2 = path2.Remove(0,lastIndex);
             lastIndex = path2.LastIndexOf(".");
             if (lastIndex!=-1)
                path2 = path2.Remove(lastIndex);
            var bundleName = $"assets/art/environment/levels/{path2}";

            importer.assetBundleName = bundleName;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        [MenuItem("Assets/资源/(须选中猫猫包资源文件)设置Bundle")]
        public static void MarkCatBundle()
        {
            foreach (var str in Selection.assetGUIDs)
            {
                var importer = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(str));
                var path2 = AssetDatabase.GUIDToAssetPath(str);
                var lastIndex = path2.LastIndexOf("/");
                path2 = path2.Remove(0,lastIndex);
                lastIndex = path2.LastIndexOf(".");
                if (lastIndex!=-1)
                    path2 = path2.Remove(lastIndex);
                var bundleName = $"modelcat/{path2}";

                importer.assetBundleName = bundleName;
            }
           

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
       [MenuItem("Assets/资源/(须选中动态头像框prefab)设置动态头像框prefabBundle")]
        public static void MarkDynamicHeadFrameBundle()
        {
            var importer = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]));
            var path2 = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);
            var lastIndex = path2.LastIndexOf("/");
            path2 = path2.Remove(0,lastIndex);
             lastIndex = path2.LastIndexOf(".");
             if (lastIndex!=-1)
                path2 = path2.Remove(lastIndex);
            var bundleName = $"plane/avatar/dynamicheadframe/{path2}";

            importer.assetBundleName = bundleName;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/资源相关/一键设置所有关卡ModelAssets和Common子文件BundleName")]
        public static void MarkAllLevelModelAndCommonAssets()
        {
            var commonPath = LevelAssetsPathPrefix + "Common/";
            //Common公共资源
            var files = Directory.GetFiles(Application.dataPath + commonPath, "*.*", SearchOption.AllDirectories);
            foreach (var VARIABLE in files)
                if (!VARIABLE.Contains(".meta"))
                    AssetImporter.GetAtPath("Assets/" + VARIABLE.Replace(Application.dataPath + "/", string.Empty))
                        .assetBundleName = "sceneshare";
            Debug.Log("<color=yellow>Common资源Bundle设置完毕</color>");
            //每个关卡文件夹
            var allDirs = Directory.GetDirectories(Application.dataPath + LevelAssetsPathPrefix);
            foreach (var dir in allDirs)
                if (dir.Contains("Level_"))
                {
                    var _dir = dir + "/ModelAssets/";
                    files = Directory.GetFiles(_dir, "*.*", SearchOption.AllDirectories);
                    foreach (var VARIABLE in files)
                        if (!VARIABLE.Contains(".meta"))
                        {
                            var modelAssetCharIndex = VARIABLE.LastIndexOf("/ModelAssets/");
                            var levelName = VARIABLE.Remove(modelAssetCharIndex)
                                .Replace(Application.dataPath, string.Empty)
                                .Replace(LevelAssetsPathPrefix, string.Empty);
                            var bundleName = $"assets/art/environment/levels/{levelName}_sceneassets";
                            var importer = AssetImporter.GetAtPath("Assets/" +
                                                                   VARIABLE.Replace(Application.dataPath + "/",
                                                                       string.Empty));
                            importer.assetBundleName = bundleName;
                        }
                }

            Debug.Log("<color=yellow>关卡资源Bundle设置完毕</color>");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        //递归处理每个资产的依赖项
        private static void InitAssetBundleDic(string assetPath, Dictionary<string, List<string>> dic)
        {
            var dependencies = AssetDatabase.GetDependencies(assetPath);
            Debug.Log(dependencies.Length + "每个资产依赖项len");
            // if (dependencies != null && dependencies.Length > 0)
            // {
            //     foreach (var depAssets in dependencies)
            //     {
            //         // if (depAssets!=assetPath)
            //         // InitAssetBundleDic(depAssets, dic);
            //     }
            // }

            // AssetImporter im= AssetImporter.GetAtPath(assetPath);
            // if (dic.ContainsKey(assetPath))
            // {
            //     dic[assetPath].Add(im.assetBundleName);
            // }
            // else
            // {
            //     dic.Add(assetPath,new List<string>(){im.assetBundleName});
            // }
        }

        private static void AnalysisSceneBundle(string name, Dictionary<string, List<string>> dic)
        {
            var assets = AssetDatabase.GetAssetPathsFromAssetBundle(name);
            foreach (var assetName in assets)
                if (assetName.Contains(".unity") || assetName.Contains("WorldMapPrefab.prefab") ||
                    assetName.EndsWith("HomeBuildMap.prefab"))
                {
                    var dependAssets = AssetDatabase.GetDependencies(assetName, false);
                    foreach (var dependAssetName in dependAssets)
                        if (dic.ContainsKey(dependAssetName))
                        {
                            if (!dic[dependAssetName].Contains(name))
                                dic[dependAssetName].Add(name);
                        }
                        else
                        {
                            dic[dependAssetName] = new List<string> { name };
                        }
                }
        }


        [MenuItem("Tools/资源相关/一键导出关卡副本场景分析结果")]
        public static void ExportAnalysisScenesAssetsFiles()
        {
            var bundleNameArr = AssetDatabase.GetAllAssetBundleNames();
            var assetPathBundleDic = new Dictionary<string, List<string>>();
            //分析所有bundle 建立<AssetPath,BundleNameList>Dic
            foreach (var bundleName in bundleNameArr)
            {
                var assets = AssetDatabase.GetAssetPathsFromAssetBundle(bundleName);
                foreach (var assetPath in assets)
                {
                    var deps = AssetDatabase.GetDependencies(assetPath, false);
                    foreach (var depAssetPath in deps)
                    {
                        var im = AssetImporter.GetAtPath(depAssetPath);
                        //明确哪个bundle中用到这个这个Asset
                        if (assetPathBundleDic.ContainsKey(depAssetPath))
                        {
                            if (!assetPathBundleDic[depAssetPath].Contains(bundleName))
                                assetPathBundleDic[depAssetPath].Add(bundleName);
                        }
                        else
                        {
                            assetPathBundleDic.Add(depAssetPath, new List<string> { bundleName });
                        }
                        // Debug.Log("bbb"+assetPath+"   "+depAssetPath +"  "+bundleName);
                    }
                }
            }

            // foreach (var VARIABLE in assetPathBundleDic)
            // {
            //     Debug.Log("KEY.....");
            //     Debug.Log(VARIABLE.Key);
            //     Debug.Log("VAL.....");
            //     foreach (var VARIABLE2 in VARIABLE.Value)
            //     {
            //         Debug.Log(VARIABLE2);
            //     
            //     }
            // }
            //分析所有场景Depend Assets
            Debug.Log("开始分析所有场景资源");
            var allSceneDepAssetDic = new Dictionary<string, List<string>>();
            foreach (var bundleName in bundleNameArr)
                if (bundleName.Contains("environment/levels"))
                    AnalysisSceneBundle(bundleName, allSceneDepAssetDic);

            var sb = new StringBuilder();
            var addResult = new HashSet<string>();
            foreach (var pair in allSceneDepAssetDic)
            {
                //场景依赖资产
                var assetName = pair.Key;
                if (assetName.Contains(".cs")) continue;
                var bundleName1 = pair.Value;
                if (assetPathBundleDic.ContainsKey(assetName))
                {
                    var bundleName2 = assetPathBundleDic[assetName];
                    foreach (var name in bundleName2)
                        if (!bundleName1.Contains(name))
                            bundleName1.Add(name);
                }

                var im = AssetImporter.GetAtPath(assetName);
                //被引用>1
                if (bundleName1.Count > 1)
                {
                    if (im.assetBundleName != string.Empty)
                    {
                        if (addResult.Add(assetName))
                        {
                            //存在丢失引用风险
                            sb.AppendLine($"资产存在丢失引用风险!Path:{assetName}   资产自身bundleName: {im.assetBundleName}");
                            foreach (var VARIABLE in bundleName1) sb.AppendLine($"    ————资产被Bundle:{VARIABLE} 所引用 ");
                            sb.AppendLine("");
                        }
                    }
                    else
                    {
                        //存在冗余风险
                        if (addResult.Add(assetName))
                        {
                            //存在丢失引用风险
                            sb.AppendLine($"资产存在冗余风险! Path:{assetName}  资产自身无bundleName");
                            foreach (var VARIABLE in bundleName1) sb.AppendLine($"    ————资产被Bundle:{VARIABLE} 所引用 ");
                            sb.AppendLine("");
                        }
                    }
                }
            }

            var savePath = "C:/ProjectSaga/场景资产分析日志.txt";
            if (Directory.Exists("C:/ProjectSaga/"))
            {
                File.WriteAllText(savePath, sb.ToString());
            }
            else
            {
                Directory.CreateDirectory("C:/ProjectSaga/");
                File.WriteAllText(savePath, sb.ToString());
            }

            Debug.Log("<color=yellow>场景资产分析日志已生成在C:/ProjectSaga/场景资产分析日志.txt</color>");
        }

        [MenuItem("Assets/资源/Recursive/Directory")]
        public static void MarkDirectoryRecursiveDirectory()
        {
            var path = GetSelectionPath();
            foreach (var s in path) MarkDirectoryRecursiveDirectory(s);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void MarkSelectionAsset(string stripPath, bool isHideSelectName)
        {
            var path = GetSelectionPath();
            foreach (var s in path)
            {
                var newS = s.Replace(stripPath, "");
                newS = newS.Replace(".prefab", "");
                newS = newS.Replace(".jpg", "");
                newS = newS.Replace(".png", "");
                newS = newS.Replace(" ", "");
                newS = newS.Replace("/NewPlaceHolder", "");
                if (isHideSelectName)
                {
                    int lastSubIndex = newS.LastIndexOf("/");
                    newS = newS.Substring(0, lastSubIndex);
                }
                Debug.Log(s + " " + newS);
                var importer = AssetImporter.GetAtPath(s);
                if (importer == null) return;
                importer.assetBundleName = newS;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Assets/资源/设置Texture路径")]
        private static void MarkStripSelectionAssetSimple()
        {
            MarkSelectionAsset("Assets/Textures/", false);
        }

        [MenuItem("Assets/资源/设置Prefab路径")]
        private static void MarkSelectionAssetSimple()
        {
            MarkSelectionAsset("Assets/Prefabs/", false);
        }

        [MenuItem("Assets/资源/设置模型路径")]
        private static void MarkModelAbPath()
        {
            MarkSelectionAsset("Assets/Models/", true);
        }

        private static void MarkDirectoryRecursiveDirectory(string path)
        {
            if (string.IsNullOrEmpty(path)) return;

            SetPathBundleName(path, string.Empty);

            var allDirectories = Directory.GetDirectories(path, "*.*");
            if (allDirectories.Length <= 0)
            {
                SetPathAssetBundleName(path);
                return;
            }

            var allFiles = Directory.GetFiles(path, "*.*")
                .Where(item => Path.GetExtension(item) != ".meta")
                .ToArray();
            foreach (var file in allFiles) SetPathAssetBundleName(file);

            foreach (var directory in allDirectories) MarkDirectoryRecursiveDirectory(directory);
        }
    }
}