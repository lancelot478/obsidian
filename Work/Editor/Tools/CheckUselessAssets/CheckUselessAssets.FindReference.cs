using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public partial class CheckUselessAssets
{
    static Dictionary<string, List<string>> referReverseDic;

    [MenuItem("Assets/引用/查找引用旧", true)]
    public static bool CheckExecuteFindReference()
    {
        return Selection.assetGUIDs.Length > 0;
    }

    [MenuItem("Assets/引用/查找引用旧")]
    public static void ExecuteFindReference()
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
        var result = CheckReferences(allAssets).Where(item => item.Value.Count > 0);

        foreach (var kv in result)
        {
            var deps = kv.Value;
            var sb = new StringBuilder();
            sb.AppendLine($"{kv.Key}:\n");
            deps.ToList().ForEach(item => sb.AppendLine(item));
            Debug.Log(sb.ToString());
        }

        Debug.LogError("查找结束");
    }

    [MenuItem("Assets/引用/刷新")]
    public static void RefreshFindReference()
    {
        string[] assetPaths = AssetDatabase.GetAllAssetPaths();
        //string[] assetGuids = Selection.assetGUIDs;
        //string[] assetPaths = new string[assetGuids.Length];
        //for (int i = 0; i < assetGuids.Length; i++)
        //{
        //    assetPaths[i] = AssetDatabase.GUIDToAssetPath(assetGuids[i]);
        //}
        referReverseDic = new Dictionary<string, List<string>>();
        Debug.Log(assetPaths.Length);
        for (int i = 0; i < assetPaths.Length; i++)
        {
            string pathName = assetPaths[i];
            string[] depPathArr = AssetDatabase.GetDependencies(pathName, true);
            for (int j = 0; j < depPathArr.Length; j++)
            {
                string depPath = depPathArr[j];
                if (!referReverseDic.ContainsKey(depPath))
                {
                    referReverseDic[depPath] = new List<string>();
                }
                referReverseDic[depPath].Add(pathName);
            }
        }
    }

    [MenuItem("Assets/引用/查找")]
    public static void ExecuteFindReferenceNew()
    {
        FindAndDelReference(false);
    }

    [MenuItem("Assets/引用/查找并删除")]
    public static void ExecuteFindAndDelReference()
    {
        FindAndDelReference(true);
    }

    public static void FindAndDelReference(bool isDel)
    {
        bool isFind = !isDel;
        string[] assetGUIDs = Selection.assetGUIDs;
        List<string> nilRefAssetList = new List<string>();
        for (int i = 0; i < assetGUIDs.Length; i++)
        {
            string guid = assetGUIDs[i];
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            bool hasRef = true;
            if (referReverseDic.ContainsKey(assetPath))
            {
                List<string> referParentList = referReverseDic[assetPath];
                if (referParentList.Count == 1)
                {
                    AssetImporter ai = AssetImporter.GetAtPath(assetPath);
                    if (ai.assetBundleName == string.Empty)
                    {
                        hasRef = false;
                        nilRefAssetList.Add(assetPath);
                    }
                }
                else
                {
                    hasRef = true;
                    if (isFind)
                    {
                        for (int j = 0; j < referParentList.Count; j++)
                        {
                            Debug.Log(j + "------" + referParentList[j]);
                        }
                    }
                }
            }
            else
            {
                nilRefAssetList.Add(assetPath);
            }
            string colStr = "";
            if (hasRef)
            {
                colStr = " <color=cyan>";
            }
            else
            {
                colStr = " <color=red>";
            }
            Debug.Log(guid + colStr + assetPath + "</color>");
        }
        if (isDel)
        {
            Debug.Log("Del count:" + nilRefAssetList.Count);
            List<string> failDelLis = new List<string>();
            AssetDatabase.DeleteAssets(nilRefAssetList.ToArray(), failDelLis);
            for (int i = 0; i < nilRefAssetList.Count; i++)
            {
                Debug.Log(i + "failDelLis:" + nilRefAssetList[i]);
            }
            AssetDatabase.Refresh();
        }
    }

    [MenuItem("Assets/引用/输出无引用")]
    public static void LogNilReference()
    {
        if (referReverseDic == null)
        {
            return;
        }
        foreach (KeyValuePair<string, List<string>> k in referReverseDic)
        {
            if (k.Value.Count <= 1)
            {
                bool isShow = true;
                string assetPath = k.Key;
                //if(assetPath.Contains(".prefab") 
                //    || assetPath.Contains(".png") 
                //    || assetPath.Contains(".jpg")
                //    || assetPath.Contains(".tga")
                //    || assetPath.Contains(".mat") 
                //    || assetPath.Contains(".FBX")
                //    || assetPath.Contains(".anim")
                //    )
                //if (k.Key.Contains("Packages/") || k.Key.Contains("/Design/") || k.Key.Contains("/Plugins/"))
                //{
                //    isShow = false;
                //}
                if (isShow)
                {
                    Debug.Log("nil ref-----" + assetPath);
                }
            }
        }
    }
}