using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
namespace XDPlugin
{
    namespace OGR
    {
        public class GrassEditor : Editor
        {

            public class AreaData
            {
                public Vector3 maxPos;
                public Vector3 minPos;
                public Vector3 centerPos;
            }

            const string GRASS_DATA_PATH = "Assets/TerrainToMesh";
            public static List<GrassDataEntry> grassDatas ;
            public static GameObject[] grassObjs;
            public static GameObject grassGroupObj;
            public static Transform grassGroupTran;
            public static Transform terrainTran;
            public static Transform sceneTrans;
            public static MeshFilter[] meshFilters;
            public static Dictionary<string, Dictionary<string, Material>> areaMaterials;
            public static Dictionary<string, Material> materials;
            public static Dictionary<string, List<CombineInstance>> combines;
            public static MeshCollider[] terrainCollider;
            public static List<int> flagIndex;
            public static int row = 6;  //x
            public static int col = 6;  //z
            public static float width;
            public static float length;
            public static AreaData[][] areaDatas;
            //[MenuItem("Assets/GrassEditor/CreateGrass")]
            static void InitializeGrass()
            {
                Scene scene = SceneManager.GetActiveScene();
                if (scene.name != "Level_001_2")
                    return;
                ClearGrass();
                InitObj();
                GetGrassData();
                CombineGrassMesh();
                EditorSceneManager.SaveScene(scene);
            }

            static void InitObj()
            {
                sceneTrans = GameObject.Find("Scene").transform;
                grassGroupObj = new GameObject("Grass");
                grassGroupObj.transform.SetParent(sceneTrans);
                grassGroupTran = grassGroupObj.transform;

                terrainTran = GameObject.Find("Terrain").transform;
                int count = terrainTran.childCount;
                terrainCollider = new MeshCollider[count];
                flagIndex = new List<int>();
                
                for (int i = 0; i < count; i++)
                {
                    Transform meshTrans = terrainTran.GetChild(i);
                    Transform transform = meshTrans.GetChild(0);
                    meshTrans.GetChild(1).gameObject.SetActive(false);
                    MeshCollider meshCollider = transform.GetComponent<MeshCollider>();
                    if (meshCollider != null)
                    {
                        terrainCollider[i] = meshCollider;

                    }
                    else
                    {
                        terrainCollider[i] = transform.gameObject.AddComponent<MeshCollider>();
                        flagIndex.Add(i);
                    }
                }
            }

            static void GetGrassData()
            {
                SetAreaRange();
                areaMaterials = new Dictionary<string, Dictionary<string, Material>>();
                //materials = new Dictionary<string, Material>();
                combines = new Dictionary<string, List<CombineInstance>>();
                grassDatas = new List<GrassDataEntry>();
                foreach (string name in Directory.GetDirectories(GRASS_DATA_PATH))
                {
                    DirectoryInfo direction = new DirectoryInfo(name);
                    FileInfo[] files = direction.GetFiles("*", SearchOption.AllDirectories);
                    
                    for (int i = 0; i < files.Length; i++)
                    {
                        FileInfo file = files[i];
                        if (file.Extension != ".asset")
                            continue;
                        string dataPath = tModelEditor.GetFileAssetPath(file);
                        GrassData grassAssets = AssetDatabase.LoadAssetAtPath<GrassData>(dataPath);
                        GrassDataEntry[] dataEntry = grassAssets.grassData;
                        for (int j = 0; j < dataEntry.Length; j++)
                        {
                            
                            grassDatas.Add(dataEntry[j]);
                            GreateObjByMesh(dataEntry[j], j);
                        }
                    }
                }


            } 

            static void GreateObjByMesh(GrassDataEntry grassDataEntry, int index)
            {
                for (int i = 0; i < grassDataEntry.grassNum; i++)
                {
                    GameObject grass = new GameObject();
                    //MeshFilter meshFilter = grass.AddComponent<MeshFilter>();
                    //MeshRenderer renderer = grass.AddComponent<MeshRenderer>();
                    //renderer.sharedMaterial = grassDataEntry.grassMaterial;
                    //meshFilter.sharedMesh = grassDataEntry.grassMesh;

                    float rotation = grassDataEntry.rotation[i];
                    Quaternion rot = Quaternion.Euler(0.0f, Mathf.Rad2Deg * grassDataEntry.rotation[i], 0.0f);
                    Vector3 scale = new Vector3(grassDataEntry.widthScale[i], grassDataEntry.heightScale[i], grassDataEntry.widthScale[i]);
                    Transform trans = grass.transform;
                    trans.localRotation = rot;
                    trans.localScale = scale;
                    trans.localPosition = grassDataEntry.position[i];

                   
                    CombineInstance combine = new CombineInstance();
                    combine.mesh = grassDataEntry.grassMesh;
                    combine.transform = trans.localToWorldMatrix;

                    Material mat = grassDataEntry.grassMaterial;
                    SetAreaData(trans.position,mat,combine);
                    DestroyImmediate(grass);
                }
            }


            static void CombineGrassMesh()
            {
                GameObject combineObj = new GameObject("GrassCombine");
                Transform combineTrans = combineObj.transform;
                combineTrans.SetParent(grassGroupTran);
                foreach (KeyValuePair<string, Dictionary<string, Material>> area in areaMaterials)
                {
                    foreach (KeyValuePair<string, Material> mats in area.Value)
                    {
                        string newName = area.Key + "_" + mats.Key;
                        GameObject obj = new GameObject(newName);
                        obj.transform.SetParent(combineTrans);
                        MeshFilter combineMehFilter = obj.AddComponent<MeshFilter>();
                        //combineMehFilter.sharedMesh = new Mesh();
                        combineMehFilter.sharedMesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32};
                        combineMehFilter.sharedMesh.CombineMeshes(combines[newName].ToArray(),true,true);
                        combineMehFilter.sharedMesh.uv2 = null;
                        combineMehFilter.sharedMesh.uv3 = null;
                        combineMehFilter.sharedMesh.uv4 = null;
                        combineMehFilter.sharedMesh.uv5 = null;
                        combineMehFilter.sharedMesh.uv6 = null;
                        combineMehFilter.sharedMesh.uv7 = null;
                        combineMehFilter.sharedMesh.uv8 = null;

                        MeshRenderer meshRenderer = obj.AddComponent<MeshRenderer>();
                        meshRenderer.sharedMaterial = mats.Value;
                        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        meshRenderer.receiveShadows = true;
                        combineMehFilter.sharedMesh.OptimizeReorderVertexBuffer();
                        combineMehFilter.sharedMesh.OptimizeIndexBuffers();
                        combineMehFilter.sharedMesh.Optimize();
                    }
                }

                for (int i = 0; i < flagIndex.Count; i++)
                {
                    DestroyImmediate(terrainCollider[flagIndex[i]].gameObject.GetComponent<MeshCollider>());
                }
                flagIndex.Clear();
            }

            static void SetDic(Material mat, CombineInstance combine, string name)
            {
                string matName = mat.name;
                if (!areaMaterials[name].ContainsKey(matName))
                {
                    areaMaterials[name].Add(matName, mat);
                }
                string newName = name + "_" + matName;
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

            //static void SetAreaRange(Vector3 pos, Material mat, CombineInstance combine)
            //{
            //    for (int i = 0; i < terrainCollider.Length; i++)
            //    {
            //        MeshCollider collider = terrainCollider[i];
            //        if (collider.bounds.Contains(pos))
            //        {
            //            string name = collider.name;
            //            if (areaMaterials.ContainsKey(name))
            //            {                            
            //                SetDic(mat, combine, name);
            //            }
            //            else
            //            {
            //                areaMaterials.Add(name,new Dictionary<string, Material>());
            //                SetDic(mat, combine, name);
            //            }
            //        }
                    
            //    }
            //}

            static void SetAreaRange()
            {
                Vector3[] vector3s = GetBorderPosArr();
                Vector3 max = vector3s[0];
                Vector3 min = vector3s[1];

                float xLength = max.x - min.x;
                float zLength = max.z - min.z;
                width = xLength / row;
                length = zLength / col;
                Vector3 maxPos = new Vector3(min.x + width, 0, min.z + length);
                float tempWid = 0;
                areaDatas = new AreaData[row][];
                for (int i = 0; i < row; i++)
                {
                    tempWid = width * i;
                    float tempLen = 0;
                    areaDatas[i] = new AreaData[col];
                    for (int j = 0; j < col; j++)
                    {
                        AreaData area = new AreaData();
                        tempLen = length * j;
                        area.maxPos = new Vector3(maxPos.x + tempWid, 0, maxPos.z + tempLen);
                        area.minPos = new Vector3(min.x + tempWid, 0, min.z + tempLen);
                        //area.centerPos = new Vector3(area.maxPos.x - tempWid * 0.5f , 0, area.maxPos.z - tempLen * 0.5f);
                        areaDatas[i][j] = area;
                    }
                }


            }

            static void SetAreaData(Vector3 pos, Material mat, CombineInstance combine)
            {
                Vector3 max = areaDatas[0][0].maxPos;
                Vector3 min = areaDatas[0][0].minPos;
                int xNum = Mathf.FloorToInt((pos.x - min.x)/width);
                int zNum = Mathf.FloorToInt((pos.z - min.z)/length);
                AreaData areaData = areaDatas[xNum][zNum];
                string name = "grass_" + xNum + "_" + zNum;
                if (areaMaterials.ContainsKey(name))
                {
                    SetDic(mat, combine, name);
                }
                else
                {
                    areaMaterials.Add(name, new Dictionary<string, Material>());
                    SetDic(mat, combine, name);
                }
            }

            static Vector3[] GetBorderPosArr()
            {
                Vector3 max = terrainCollider[0].bounds.max;
                Vector3 min = terrainCollider[0].bounds.min;
                for (int i = 0; i < terrainCollider.Length; i++)
                {
                    max = Vector3.Max(terrainCollider[i].bounds.max, max);
                    min = Vector3.Min(terrainCollider[i].bounds.min, min);
                }
                Vector3[] vector3s = {max, min};
                return vector3s;
            }

            static void ClearGrass()
            {
                grassGroupObj = GameObject.Find("Scene/Grass");
                if (grassGroupObj != null)
                {
                    DestroyImmediate(grassGroupObj);
                }

                terrainTran = GameObject.Find("Terrain").transform;
                if (terrainTran != null)
                {
                    for (int i = 0; i < terrainTran.childCount; i++)
                    {
                        Transform meshTrans = terrainTran.GetChild(i);
                        meshTrans.GetChild(1).gameObject.SetActive(true);
                    }
                }
            }

            //[MenuItem("Assets/GrassEditor/ClearGrass")]
            static void DelGrass()
            {
                ClearGrass();
                Scene scene = SceneManager.GetActiveScene();
                EditorSceneManager.SaveScene(scene);
            }
        }
    }

}