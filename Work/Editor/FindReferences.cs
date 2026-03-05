using System;
using UnityEngine;
using System.Collections;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using NUnit.Framework;
using Object = UnityEngine.Object;

public class FindReferences
{
    private enum Type
    {
        All,
        SkillCard
    }

    private static readonly Dictionary<Type, string> Dic = new Dictionary<Type, string>()
    {
        {Type.All, ""},
        {Type.SkillCard, "Prefabs/Plane/SkillCard"}
    };

    [MenuItem("Assets/Find References/CheckAllPrefabDirectory", false, 10)]
    private static void FindSkillCardDirectory()
    {
        if (EditorUtility.DisplayDialog("检查资源索引", "该操作耗时较长，是否确定继续检查？", "Yes!", "No~"))
        {
            Find(Type.All,true);
        }
    }    
    [MenuItem("Assets/Find References/CheckAllPrefabFile", false, 11)]
    private static void FindSkillCardFile()
    {
        if (EditorUtility.DisplayDialog("检查资源索引", "该操作耗时较长，是否确定继续检查？", "Yes!", "No~"))
        {
            Find(Type.All,false);
        }
    }


    private static void Find(Type type,bool isDirectory)
    {
        EditorSettings.serializationMode = SerializationMode.ForceText;
        
        // var paths = Selection.assetGUIDs.Select(AssetDatabase.GUIDToAssetPath).ToArray();
        var guids = Selection.assetGUIDs.ToArray();
        // var paths = Selection.GetFiltered(typeof(Object), SelectionMode.Assets | SelectionMode.ExcludePrefab);
        if (guids.Length == 0)
        {
            Debug.LogError("Please select an asset");
            return;
        }

        foreach (var selectGuid in guids)
        {
           
            string assetPath = AssetDatabase.GUIDToAssetPath(selectGuid);
           
            if (string.IsNullOrEmpty(assetPath))
                return;
            IEnumerable<string> allFiles;
            if(isDirectory)
            {
                allFiles = Directory.GetFiles(assetPath, "*.*", SearchOption.AllDirectories)
                    .Where(item => Path.GetExtension(item) != ".meta");
            }
            else
            {
                allFiles = new[] { assetPath };
            }
          
            foreach (var path in allFiles)
            {
                string guid = AssetDatabase.AssetPathToGUID(path);
                List<string> withoutExtensions = new List<string>() {".prefab", ".unity", ".mat", ".asset"};
                string findPath = Path.Combine(Application.dataPath,Dic[type]);
                string[] files = Directory.GetFiles(findPath, "*.*", SearchOption.AllDirectories)
                    .Where(s => withoutExtensions.Contains(Path.GetExtension(s).ToLower())).ToArray();


                int num = 0;
                for (var i = 0; i < files.Length; ++i)
                {
                    string file = files[i];
                    //显示进度条
                    
                        EditorUtility.DisplayProgressBar("匹配资源", "正在匹配资源中...", 1.0f * i / files.Length);
                    if (Regex.IsMatch(File.ReadAllText(file), guid))
                    {
                        Debug.Log(file, AssetDatabase.LoadAssetAtPath<Object>(GetRelativeAssetsPath(file)));
                        num++;
                    }
                }

                if (num == 0)
                {
                    var sourcePath = path.Replace("Assets", Application.dataPath);
                    File.Delete(sourcePath);
                    Debug.LogError(path + "     匹配到" + num + "个");
                }
                else if (num == 1)
                {
                    Debug.Log(path + "     匹配到" + num + "个");
                }
                else
                {
                    Debug.LogWarning(path + "     匹配到" + num + "个");
                }
            }
           
        }

        EditorUtility.ClearProgressBar();
    }
    private static void MoveAsset(string path)
    {
        var name = path.Substring(path.LastIndexOf("/") + 1);

        var sourcePath = path.Replace("Assets", Application.dataPath);
        var targetPath = Application.dataPath.Replace("Assets", "TempAtlas"+"/"+name);
      
        var targetDir = Directory.GetParent(targetPath);
        if (!targetDir.Exists)
        {
            targetDir.Create();
        }
        
        File.Copy(sourcePath, targetPath, true);
        File.Delete(sourcePath);
    }

    [MenuItem("Assets/Find References", true)]
    static private bool VFind()
    {
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        return (!string.IsNullOrEmpty(path));
    }

    static private string GetRelativeAssetsPath(string path)
    {
        return "Assets" + Path.GetFullPath(path).Replace(Path.GetFullPath(Application.dataPath), "").Replace('\\', '/');
    }
}