
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ParticlesEditor : Editor
{

    [MenuItem("Assets/粒子/检测不规范render")]
    public static void CheckFXParticleMaterialMissing()
    {
        var selectedObjs = Selection.GetFiltered<GameObject>(SelectionMode.DeepAssets);
        CheckParticeRender(selectedObjs);
    }

    static string getGameObjectPath(Transform transform)
    {
        var path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }
        return path;
    }

    static void CheckParticeRender(GameObject[] fxObjs)
    {
        Debug.Log("检查Prefab数量:" + fxObjs.Length);
        foreach (var fxObj in fxObjs)
        {
            ParticleSystem[] particles = fxObj.GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem particle in particles)
            {
                ParticleSystemRenderer renderer = particle.GetComponent<ParticleSystemRenderer>();
                if (renderer != null)
                {
                    if (renderer.enabled)
                    {
                        if (renderer.sharedMaterial == null)
                        {
                            Debug.Log("材质丢失:" + fxObj.name + "----" + particle.name);
                        }
                    }
                    else
                    {
                        Debug.Log("未使用renderer组件:" + fxObj.name + "----" + particle.name);
                    }
                }
            }
            Transform[] traArr = fxObj.GetComponentsInChildren<Transform>(true);
            foreach (Transform childTraj in traArr)
            {
                MeshRenderer meshRenderer = childTraj.GetComponent<MeshRenderer>();
                SkinnedMeshRenderer skinnedRender = childTraj.GetComponent<SkinnedMeshRenderer>();
                if (meshRenderer != null && meshRenderer.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.On
                    || skinnedRender != null && skinnedRender.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.On)
                {
                    Debug.Log("是否需开启阴影:" + fxObj.name + "----" + childTraj.name);
                }
            }
            int childCountAll = fxObj.GetComponentsInChildren<Transform>().Length;
            if (childCountAll > 15)
            {
                Debug.Log("粒子物体过多:" + fxObj.name + "----" + childCountAll);
            }
        }
    }

    //[MenuItem("Assets/粒子/删除LV02")]
    //public static void DeleteLV02()
    //{
    //    var selectedObjs = Selection.GetFiltered<GameObject>(SelectionMode.DeepAssets);
    //    foreach (var fxObj in selectedObjs)
    //    {
    //        DeletePrefabLV02(fxObj);
    //    }
    //    AssetDatabase.Refresh();
    //}

    //static string[] infoArr = new string[] { "LV02", "Lv02", "lv02" };
    //static void DeletePrefabLV02(GameObject obj)
    //{
    //    foreach (string info in infoArr)
    //    {
    //        Transform lv02Obj = obj.transform.Find(info);
    //        string path = AssetDatabase.GetAssetPath(obj);
    //        if (lv02Obj != null)
    //        {
    //            GameObject newObj = MonoBehaviour.Instantiate(obj);
    //            Transform delObj = newObj.transform.Find(info);
    //            MonoBehaviour.DestroyImmediate(delObj.gameObject);
    //            Debug.Log(path);
    //            PrefabUtility.SaveAsPrefabAsset(newObj, path);
    //            MonoBehaviour.DestroyImmediate(newObj);
    //        }
    //    }
    //}

    static string info = "LV01";
    private static object animControl;

    [MenuItem("Assets/粒子/删除LV01")]
    public static void DeleteLV01()
    {
        var selectedObjs = Selection.GetFiltered<GameObject>(SelectionMode.DeepAssets);
        List<string> arr = new List<string>() { };
        foreach (var obj in selectedObjs)
        {
            Transform lv01Obj = obj.transform.Find(info);
            Debug.Log(lv01Obj != null);
            if (lv01Obj != null)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                GameObject newObj = MonoBehaviour.Instantiate(obj);
                Transform delTra = newObj.transform.Find(info);
                Transform parentTra = delTra.parent;
                if(parentTra != null)
                {
                    Component[] components = delTra.GetComponents<Component>();
                    Animator anim = delTra.GetComponent<Animator>();
                    if(components.Length > 2)
                    {

                          for (int i = 0; i < components.Length; i++)
                         {
                             Debug.Log("=1111==" + components[i].GetType());
                        }
                        arr.Add(parentTra.name + "==" + components.Length);
                    }
                    if (components.Length == 1  || (components.Length == 2 && anim !=  null)) {

                        Debug.Log("=1111==" + components.Length);
                        int count = delTra.childCount;
                        if (count > 0)
                        {
                            Transform[] tras = new Transform[count];
                            for (int i = 0; i < count; i++)
                            {
                                tras[i] = delTra.GetChild(i);
                            }
                            for (int i = 0; i < count; i++)
                            {
                                tras[i].SetParent(parentTra);
                            }
                            if (anim != null)
                            {
                                Animator parentAnim = parentTra.gameObject.AddComponent<Animator>();
                                parentAnim.runtimeAnimatorController = anim.runtimeAnimatorController;
                            }
                            MonoBehaviour.DestroyImmediate(delTra.gameObject);
                            Debug.Log(path);
                            PrefabUtility.SaveAsPrefabAsset(newObj, path);


                        }
                    
                  //  for (int i = 0; i < components.Length; i++)
                   // {
                   //     Debug.Log("=1111==" + components[i].GetType());
                   // }

                   
                        MonoBehaviour.DestroyImmediate(newObj);
                    }
                    else
                    {
                        MonoBehaviour.DestroyImmediate(newObj);
                    }
                   
                }
            }
        }
        for (int i = 0; i < arr.Count; i++)
        {
            Debug.Log("@@@@@@@@@@@@@==>" + arr[i]);
        }
        AssetDatabase.Refresh();
    }
}