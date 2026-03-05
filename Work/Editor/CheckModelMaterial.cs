using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Text;
using MiniJSON2;

namespace GMF
{
    public class CheckModelMaterial : Editor
    {
  /// <summary>
  /// key mesh 路径or 名字   value 材质球资源路径
  /// </summary>
        public static Dictionary<string, List<string>> m_saveMatInfo = new Dictionary<string, List<string>>() { };
        
  /// <summary>
  /// key mesh 路径or 名字   value 材质球资源
  /// </summary>
  public static Dictionary<string, List<Material>> m_matDic = new Dictionary<string, List<Material>>() { };

  
        /// <summary>
        /// 配置文件存储路径
        /// </summary>
        const string MODEL_CONFIG_PATH = "Assets/Config/ttdbl2_config/ModelMatInfo.json";

        [MenuItem("Assets/CheckRoleModel/2保存角色材质信息")]
        static void SaveRoleModelMaterial()
        {
            GameObject[] gs = Selection.gameObjects;
            foreach (GameObject g in gs)
            {
                string assetPath = AssetDatabase.GetAssetPath(g);
                if (assetPath.Contains(".prefab"))
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject)) as GameObject;
                    SaveMat(prefab, assetPath);
                }
            }
        }

        public static void CheckRoleModelMaterialByObj(GameObject prefab,string assetPath)
        {
            CheckMat(prefab, prefab.name, assetPath,true);
        }

        [MenuItem("Assets/CheckRoleModel/3应用角色材质信息")]
        static void CheckRoleModelMaterial()
        {
            GameObject[] gs = Selection.gameObjects;
            foreach (GameObject g in gs)
            {
                string assetPath = AssetDatabase.GetAssetPath(g);
                if (assetPath.Contains(".prefab"))
                {
                    GameObject prefab =  MonoBehaviour.Instantiate(g);
                    CheckMat(prefab,g.name, assetPath);
                }
            }
        }

        static JSON GetModelMatJsonConfig(string path)
        {
            TextAsset textAsset = AssetDatabase.LoadAssetAtPath(path, typeof(TextAsset)) as TextAsset;
            JSON js = new JSON();
            if (textAsset is { }) js.serialized = textAsset.text;
            return js;
        }
        static void CheckMat(GameObject prefab,string prefabName, string prefabPath,bool isCreate = false)
        {
            if (prefab == null)
            {
                return;
            }
            JSON modelPointJs = GetModelMatJsonConfig(MODEL_CONFIG_PATH);
            LoadAssets(modelPointJs,prefabName);
            if (m_matDic.Count == 0 )
            {
                if (!isCreate)
                {
                    MonoBehaviour.DestroyImmediate(prefab);
                    Debug.LogWarning("-------无保存配置，请先保存 -----！！！！！！");
                }
                return;
            }
            SkinnedMeshRenderer[] skinArr = prefab.transform.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (SkinnedMeshRenderer skin in skinArr)
            {
                string skinName = skin.name;
                if (m_matDic.TryGetValue(skinName, out List<Material> mats))
                {
                    skin.sharedMaterials = mats.ToArray();
                }
            }
            MeshRenderer[] meshArr = prefab.transform.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer mesh in meshArr)
            {
                string meshName = mesh.name;
                if (m_matDic.TryGetValue(meshName, out List<Material> mats))
                {
                    mesh.sharedMaterials = mats.ToArray();
                }
            }
            if (!isCreate) {
                PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
                MonoBehaviour.DestroyImmediate(prefab);
                AssetDatabase.Refresh();
                Debug.Log("-----应用完成 ----！！！！");
            }
        }
        static void LoadAssets(JSON js , string name)
        {
            m_matDic.Clear();
            JSON roleJson = js.ToJSON(name);
            Dictionary<string, List<string>> loadMatDic = new Dictionary<string, List<string>>() { };
            foreach (var item in  roleJson.fields)
            {
                loadMatDic.TryGetValue(item.Key, out  List<string> paths);
                if (paths == null)
                {
                    paths = GetMatPathList(item.Value.ToString());
                    loadMatDic.Add(item.Key,paths);
                }
            }

            foreach (var item in loadMatDic)
            {
                for (int i = 0; i < item.Value.Count; i++)
                {
                    m_matDic.TryGetValue(item.Key, out List<Material> mats);
                    if (mats == null)
                    {
                        mats = new List<Material>() { };
                        m_matDic.Add(item.Key,mats);
                    }
                    Material mat = AssetDatabase.LoadAssetAtPath(item.Value[i], typeof(Material)) as Material;
                    mats.Add(mat);
                }
            }
        }
        
        static void SaveMat(GameObject prefab,string assetsPath)
        {
            if (prefab == null)
            {
                return;
            }
            m_saveMatInfo.Clear();
            JSON modelPointJs = GetModelMatJsonConfig(MODEL_CONFIG_PATH);
            SkinnedMeshRenderer[] skinArr = prefab.transform.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (SkinnedMeshRenderer skin in skinArr)
            {
                string skinName = skin.name;
                m_saveMatInfo.TryGetValue(skinName, out List<string> matPaths);
                if (matPaths == null)
                {
                    matPaths = new List<string>() { };
                    m_saveMatInfo.Add(skinName,matPaths);   
                }
                else
                {
                    Debug.LogError($"--------预制体{assetsPath}内部存在相同名字的Mesh 请检查！！！！");
                    return;
                }
                foreach (var mat in skin.sharedMaterials)
                {
                    string matPath = AssetDatabase.GetAssetPath(mat);
                    matPaths.Add(matPath);
                }
            }

            MeshRenderer[] meshRendArr = prefab.transform.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer mesh in meshRendArr)
            {
                string meshName = mesh.name;
                m_saveMatInfo.TryGetValue(meshName, out List<string> matPaths);
                if (matPaths == null)
                {
                    matPaths = new List<string>() { };
                    m_saveMatInfo.Add(meshName, matPaths);
                }
                else
                {
                    Debug.LogError($"--------预制体{assetsPath}内部存在相同名字的Mesh 请检查！！！！");
                    return;
                }
                foreach (var mat in mesh.sharedMaterials)
                {
                    string matPath = AssetDatabase.GetAssetPath(mat);
                    matPaths.Add(matPath);
                }
            }

            ClearJson(modelPointJs, prefab.name);
            SaveModelMatConfig(modelPointJs, prefab.name);
            SaveJson(modelPointJs, MODEL_CONFIG_PATH);
            AssetDatabase.Refresh();
            Debug.Log("-----保存完成 ----！！！！" );
        }

        public static void ClearJson(JSON js, string name)
        {
            JSON newJson = new JSON();
            js[name] = newJson;
        }
        public static void SaveModelMatConfig(JSON js, string name)
        {
            JSON roleJson = js.ToJSON(name);
            js[name] = roleJson;
            foreach (KeyValuePair<string, List<string>> item in m_saveMatInfo)
            {
                string path = GetMatPathStr(item.Value);
                if (!string.IsNullOrEmpty(path))
                {
                    roleJson[item.Key] = path;      
                }
            }
            Debug.Log("------配置保存成功----------"+name);
        }
        public static void SaveJson(JSON js, string path)
        {
            path = GetFullPathWithAssetPath(path);
            byte[] bytes = Encoding.UTF8.GetBytes(js.serialized);
            ToolText.WriteFile(path, bytes);
        }
        public static string GetFullPathWithAssetPath(string assetPath)
        {
            return Application.dataPath.Replace("Assets", string.Empty) + assetPath;
        }
        
        static string GetMatPathStr(List<string> matPaths)
        {
            string path = "";
            for (int i = 0; i < matPaths.Count; i++)
            {
               string  tempPath = matPaths[i];
                if (i > 0 && i < matPaths.Count )
                {
                    tempPath = "$" + tempPath;
                }
                path += tempPath;
            }
            return path;
        }
        static List<string> GetMatPathList(string path)
        {
            List<string> list = new List<string>() { };
            string[] arr = path.Split('$');
            for (int i = 0; i < arr.Length; i++)
            {
                list.Add(arr[i]);
            }
            return list;
        }
        
        [MenuItem("Assets/CheckRoleModel/4设置特效Ab路径")]
        static void SetEffectAbPath()
        {
            GameObject[] gs = Selection.gameObjects;
            foreach (GameObject g in gs)
            {
                string assetPath = AssetDatabase.GetAssetPath(g);
                if (assetPath.Contains(".prefab"))
                {
                    AssetImporter ai = AssetImporter.GetAtPath(assetPath);
                    //string abName = assetPath.Replace("Assets/Prefabs/SkillEffect", "fx");
                    //abName = abName.Replace(".prefab", "");
                    //Debug.Log("@@@@@@@@@@@"+ assetPath +"@@@@@@"+ abName);
                    string abName = "fx/" + g.name;
                    ai.assetBundleName = abName;
                }
            }
            AssetDatabase.Refresh();
        }

        [MenuItem("Assets/CheckRoleModel/5设置宠物资源Ab路径")]
        static void SetPetAssetAbPath()
        {
            string replacePath = "Assets/Models/";
            Object[] folderObj = Selection.GetFiltered(typeof(Object), SelectionMode.Assets);
            foreach (Object folder in folderObj)
            {
                string assetPath = AssetDatabase.GetAssetPath(folder);
                string createPath = assetPath.Replace(replacePath, "");
                Debug.Log($"@@@@@@@{folder.name}@@==abPath=>{createPath.ToLower()}");
                string fullPath = Application.dataPath.Replace("Assets", string.Empty) + assetPath + "/";

                DirectoryInfo direction = new DirectoryInfo(fullPath);
                FileInfo[] files = direction.GetFiles("*", SearchOption.AllDirectories);
                for (int i = 0; i < files.Length; i++)
                {
                    FileInfo file = files[i];
                    if (file.Extension == ".meta" || file.Extension == ".DS_Store")
                    {
                        continue;
                    }
                    string path = file.FullName.Replace("\\", "/");
                    path = path.Replace(Application.dataPath, "Assets");
                    AssetImporter ai = AssetImporter.GetAtPath(path);
                    ai.assetBundleName = createPath.ToLower();
                }
            }
            AssetDatabase.Refresh();
        }

        [MenuItem("Assets/CheckRoleModel/6 更改无用特效路径")]
        static void SetPetAssetAbPath1()
        {
            string goPath = "Assets/Prefabs/SkillEffect/Temp/";
            GameObject[] gs = Selection.gameObjects;
            foreach (GameObject g in gs)
            {
                string assetPath = AssetDatabase.GetAssetPath(g);
                if (assetPath.Contains(".prefab"))
                {
                    string newPath = goPath + g.name + ".prefab";
                 
                    //GameObject effectObj = AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject)) as GameObject;
                    bool isPass = AssetDatabase.CopyAsset(assetPath, newPath);
                    if(isPass)
                    {
                        bool isDel = AssetDatabase.DeleteAsset(assetPath);
                        Debug.Log("=成功==>" + assetPath + "===" + newPath + "====" +isPass + "====" +isDel);
                    }
                    //PrefabUtility.SaveAsPrefabAsset(effectObj, goPath + g.name + ".prefab");
                }
            }
            AssetDatabase.Refresh();
        }
    }
}