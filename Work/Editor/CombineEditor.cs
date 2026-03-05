using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

public class CombineEditor : Editor
{
    static List<string> scenePathList = new List<string>() {
        "Bldg",
        "Veg/Tree",
        "Veg/Bush",
        "Veg/Grass",
        "Others/Prop",
        "Others/Stone",
        "Other Mesh",
    };
    static string parentName = "SceneCombine";
    static List<Transform> tranList;
    static Transform newTrans;
    static Transform sceneTran;
    static Transform[] selectTrans;
    static Dictionary<string, Material> matDic;
    static Transform combineTrans;
    public static Dictionary<string, Dictionary<string, Material[]>> areaMaterials;
    static Dictionary<string, List<CombineInstance>> combines;

    static bool IsScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != "Level_001_3")
            return false;
        return true;
    }

    public static void Init()
    {
        Clear();
        matDic = new Dictionary<string, Material>();
        tranList = new List<Transform>();
        areaMaterials = new Dictionary<string, Dictionary<string, Material[]>>();
        combines = new Dictionary<string, List<CombineInstance>>();

        GameObject combineObj = GameObject.Find(parentName);
        if (combineObj == null)
            combineObj = new GameObject(parentName);
        combineTrans = combineObj.transform;
    }

    [MenuItem("GameObject/CombineEditor/SelectCombine")]
    public static void InitializeBySelect()
    {
        if (!IsScene())
            return;
        Init();
        selectTrans = Selection.GetTransforms(SelectionMode.TopLevel);
        for (int i = 0; i < selectTrans.Length; i++)
        {
            tranList.Add(selectTrans[i]);
        }
        SetCombinData();
    }

    [MenuItem("Assets/CombineEditor/Combine")]
    static void InitializeByPath()
    {
        if (!IsScene())
            return;
        Init();
        newTrans = GameObject.Find("New").transform;
        if (newTrans != null)
        {
            for (int i = 0; i < newTrans.childCount; i++)
            {
                Transform tran = newTrans.GetChild(i);
                if (tran.gameObject.activeInHierarchy)
                    tranList.Add(tran);
            }
        }

        sceneTran = GameObject.Find("Scene").transform;
        if (sceneTran != null)
        {
            foreach (var path in scenePathList)
            {
                Transform tran = sceneTran.Find(path);
                if (tran != null)
                    if(tran.gameObject.activeInHierarchy)
                        tranList.Add(tran);
            }
        }
        SetCombinData();
    }

    static CombineInstance GetCombine(Transform oriTrans, MeshFilter meshFilter)
    {
        CombineInstance combine = new CombineInstance();
        combine.mesh = meshFilter.sharedMesh;
        combine.transform = oriTrans.localToWorldMatrix;
        return combine;
    }

    static void SetDic(Material[] mats, CombineInstance combine, string name)
    {
        string matName = GetDicName(mats);
        if (!areaMaterials[name].ContainsKey(matName))
        {
            areaMaterials[name].Add(matName, mats);
        }
        string newName = matName + "_" + name;
        if (combines.ContainsKey(newName))
        {
            combines[newName].Add(combine);
        }
        else
        {
            List<CombineInstance> coms = new List<CombineInstance>();
            coms.Add(combine);
            combines[newName] = coms;
        }
    }

    static void SetCombinData()
    {
        foreach (var tran in tranList)
        {
            tran.gameObject.SetActive(false);
            MeshRenderer[] renderer = tran.GetComponentsInChildren<MeshRenderer>();
            MeshFilter[] meshFilter = tran.GetComponentsInChildren<MeshFilter>();
            string pathName = tran.name;
            for (int i = 0; i < renderer.Length; i++)
            {
                Material[] mat = renderer[i].sharedMaterials;
                //for (int j = 0; j < mat.Length; j++)
                //{
                //    Material material = mat[j];
                //    if (!areaMaterials.ContainsKey(pathName))
                //    {
                //        areaMaterials.Add(pathName, new Dictionary<string, Material[]>());
                //    }
                //    CombineInstance combine = GetCombine(renderer[i].transform, material, meshFilter[i]);
                //    SetDic(mat, combine, pathName);
                //}
                if (!areaMaterials.ContainsKey(pathName))
                {
                    areaMaterials.Add(pathName, new Dictionary<string, Material[]>());
                }
                CombineInstance combine = GetCombine(renderer[i].transform, meshFilter[i]);
                SetDic(mat, combine, pathName);
            }
            CombineMesh(pathName);
        }
        Scene scene = SceneManager.GetActiveScene();
        EditorSceneManager.SaveScene(scene);
    }

    static void CombineMesh(string pathName)
    {
        if (areaMaterials.ContainsKey(pathName))
        {
            foreach (var item in areaMaterials[pathName])
            {
                Material[] mats = item.Value;
                string newName = GetDicName(mats)+"_"+ pathName;
                GameObject obj = new GameObject(newName);
                obj.transform.SetParent(combineTrans);
                MeshFilter combineMehFilter = obj.AddComponent<MeshFilter>();
                combineMehFilter.sharedMesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                combineMehFilter.sharedMesh.CombineMeshes(combines[newName].ToArray(), false);
                //combineMehFilter.sharedMesh.uv2 = null;
                //combineMehFilter.sharedMesh.uv3 = null;
                //combineMehFilter.sharedMesh.uv4 = null;
                //combineMehFilter.sharedMesh.uv5 = null;
                //combineMehFilter.sharedMesh.uv6 = null;
                //combineMehFilter.sharedMesh.uv7 = null;
                //combineMehFilter.sharedMesh.uv8 = null;

                MeshRenderer meshRenderer = obj.AddComponent<MeshRenderer>();
                meshRenderer.sharedMaterials = mats;
                //meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                //meshRenderer.receiveShadows = true;
                //combineMehFilter.sharedMesh.OptimizeReorderVertexBuffer();
                //combineMehFilter.sharedMesh.OptimizeIndexBuffers();
                //combineMehFilter.sharedMesh.Optimize();
            }
        }
    }

    static string GetDicName(Material[] mats)
    {

        string name = mats[0].name;
        mats[0].enableInstancing = true;
        for (int i = 1; i < mats.Length; i++)
        {
            mats[i].enableInstancing = true;
            name = name + "_" + mats[i].name;
        }
        return name;
    }

    static void Clear()
    {
        GameObject combineObj = GameObject.Find(parentName);
        if (combineObj != null)
        {
            combineTrans = null;
            DestroyImmediate(combineObj);
            if (sceneTran == null)
                sceneTran = GameObject.Find("Scene").transform;
            foreach (var path in scenePathList)
            {
                Transform tran = sceneTran.Find(path);
                if (tran != null)
                {
                    tran.gameObject.SetActive(true);
                }
            }

            if (newTrans != null)
                newTrans.gameObject.SetActive(true);

        }
    }

    [MenuItem("Assets/CombineEditor/Clear")]
    static void CrearScene()
    {
        Clear();
        Scene scene = SceneManager.GetActiveScene();
        EditorSceneManager.SaveScene(scene);
    }
}
