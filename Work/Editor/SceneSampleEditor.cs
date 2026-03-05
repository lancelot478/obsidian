using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.IO;
using System.Text;
using MiniJSON2;
using UnityEditor.SceneManagement;
using XDPlugin.OGR;

public class SceneSampleEntry
{
    public Mesh mesh;
    public Material mat;
    public List<Transform> traList = new List<Transform>();
    public List<Vector3> posList = new List<Vector3>();
    public List<float> heightScaleList = new List<float>();
    public List<float> weightScaleList = new List<float>();
    public List<float> lengthScaleList = new List<float>();
    public List<float> rotationYList = new List<float>();
    public List<float> rotationXList = new List<float>();
    public List<float> rotationZList = new List<float>();
    public List<int> pathIndexList = new List<int>();
    public List<int> thingIndexList = new List<int>();
    public List<string> keyList = new List<string>();
 const string sceneGrassRenderAssetPath = "Assets/Config/ttdbl2_config_client/SceneGrassPath.json";
    public bool isEquals(Mesh mesh, Material mat)
    {
        return mesh.GetInstanceID() == this.mesh.GetInstanceID() && mat.GetInstanceID() == this.mat.GetInstanceID();
    }

    public bool canAdd()
    {
        return posList.Count < 1024;
    }

    public void SetEntry(Mesh mesh, Material mat)
    {
        this.mesh = mesh;
        this.mat = mat;
    }

    public void AddEntry(Transform tra, int keySum)
    {
        string key = "Render" + keySum;
        keyList.Add(key);
        tra.name = key;
        traList.Add(tra);
        posList.Add(tra.position);
        Vector3 scale = tra.lossyScale;
        heightScaleList.Add(scale.y);
        weightScaleList.Add(scale.x);
        lengthScaleList.Add(scale.z);
        Vector3 rotation = tra.eulerAngles;
        rotationYList.Add(rotation.y / Mathf.Rad2Deg);
        rotationXList.Add(rotation.x);
        rotationZList.Add(rotation.z);
        int pathIndex = 0;
        int changeIndex = 0;
        Transform parentTra = tra.parent;
        while (parentTra != null)
        {
            switch (parentTra.name)
            {
                case "Luxian01":
                    pathIndex = 1;
                    changeIndex = 99;
                    break;
                case "Luxian02":
                    pathIndex = 2;
                    changeIndex = 99;
                    break;
                case "Luxian03":
                    pathIndex = 3;
                    changeIndex = 99;
                    break;
                case "Change_a":
                    changeIndex = 1;
                    pathIndex = 99;
                    break;
                case "Change_b":
                    changeIndex = 2;
                    pathIndex = 99;
                    break;
            }
            parentTra = parentTra.parent;
        }
        pathIndexList.Add(pathIndex);
        thingIndexList.Add(changeIndex);
    }

    public void DestroyTraList()
    {
        for (int i = 0; i < traList.Count; i++)
        {
            Transform tra = traList[i];
            LODGroup lodGroup = tra.parent.GetComponent<LODGroup>();
            if (lodGroup != null)
            {
                MonoBehaviour.DestroyImmediate(lodGroup);
            }
            if (tra != null)
            {
                MonoBehaviour.DestroyImmediate(tra.GetComponent<MeshRenderer>());
                MonoBehaviour.DestroyImmediate(tra.GetComponent<MeshFilter>());
                //Transform parent = tra.parent;
                //if (parent.GetComponent<LODGroup>() == null)
                //{
                //    MonoBehaviour.DestroyImmediate(tra.gameObject);
                //}
                //else
                //{
                //    MonoBehaviour.DestroyImmediate(parent.gameObject); 
                //}
            }
        }
    }

    [MenuItem("Assets/场景/显示场景隐藏物")]
    static void Standard()
    {
        GameObject[] objArr = GameObject.FindObjectsOfType<GameObject>();
        foreach (GameObject i in objArr)
        {
            i.hideFlags = HideFlags.None;
        }
    }
    
    [MenuItem("Assets/场景/导出选中场景GrassRender组件Json文件")]
    static void MakeSceneGrassRendererFile()
    {
        Stack<string> pathStack = new Stack<string>();
       var scene = SceneManager.GetActiveScene();
       Debug.Log($"导出场景{scene.name}");
       GrassRender[] arr=GameObject.FindObjectsOfType<GrassRender>();
       List<string> pathList = new List<string>();
       foreach (var render in arr)
       {
           Transform parent = null;
           pathStack.Push(render.gameObject.name);
           if (render.transform.parent != null)
               parent = render.transform.parent;
           pathStack.Push(parent.gameObject.name);
           while (parent!=null)
           {
               parent = parent.transform.parent;
               if (parent == null) break;
               pathStack.Push(parent.gameObject.name);
           }

           int count = 0;
           StringBuilder sbd = new StringBuilder();
           while (pathStack.Count!=0)
           {
               if (count == 0)
               {
                   pathStack.Pop();
                   count++;
                   continue;
               }
               string path = pathStack.Pop();
               if (pathStack.Count != 0)
               {
                   sbd.Append(path + "/");
               }
               else
               {
                   sbd.Append(path );
               }

           } 
           pathList.Add(sbd.ToString());
       }

       CreateOrUpDateGrassRenderJson(scene.name, pathList);
    }
    
    static void CreateOrUpDateGrassRenderJson(string levelName, List<string> pathLst)
    {
       string fullPath= tModelEditor.GetFullPathWithAssetPath(sceneGrassRenderAssetPath);
       JSON js = null;
       if (File.Exists(fullPath))
       {
           js =tModelEditor.GetAssetJson(sceneGrassRenderAssetPath);
           js[levelName] = pathLst;
      
       }
       else
       {
            js = new JSON();
           js[levelName] = pathLst;
       }
       byte[] bytes = Encoding.UTF8.GetBytes(js.serialized);
       ToolText.WriteFile(fullPath, bytes);
       Debug.Log("OUT..."+fullPath);
    }


}