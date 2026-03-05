using UnityEngine;
using UnityEditor;
using UnityEditor.Sprites;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using MiniJSON2;
using UnityEngine.Rendering;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using XDPlugin.OGR;
using System;


public class NavMeshExport : EditorWindow
{
    const string MAP_DATA_JS_KEY = "mapDataDic";
    const string MAP_BLOCK_JS_KEY = "mapBlockKey";
    const string MAP_PATH_JS_KEY = "mapPathArr";
    const string MAP_FEATURE_JS_KEY = "mapFeatureKey";
    const string MAP_MONSTER_JS_KEY = "mapMonsterKey";
    const string PATH_OBJ_TAG = "Tree";
    const int RAY_LAYER = 15;
    const int GRASS_SIZE = 5;
    static Dictionary<string, GroundKind> tagGroundDic = new Dictionary<string, GroundKind>()
    {
        { "Bridge",  GroundKind.Wood},
        { "Stone",  GroundKind.Stone},
        { "Water",  GroundKind.Water},
    };
    static JSON navDataJs;
    static Dictionary<long, MapData> mapDataDic = new Dictionary<long, MapData>();
    static Dictionary<long, GroundKind> groundDataDic = new Dictionary<long, GroundKind>();
    static string Width = "130";
    static string Height = "130";
    static GameObject[] pointObjArr;
    static bool isDraw;

    public class MapBlockPathData
    {
        public JSON js = new JSON();
        public List<long> blockPathKeyList = new List<long>();
        public JSON mapPathJs = new JSON();
        public JSON pointFeatureJs = new JSON();
    }

    static void SetMapData()//设置地图可寻路区域和区域属性
    {
        mapDataDic.Clear();
        int width = int.Parse(Width);
        int height = int.Parse(Height);
        JSON mapDataJs = new JSON();
        for (int i = -width * NavMeshPoint.accuracy; i <= width * NavMeshPoint.accuracy; i++)
        {
            for (int j = -height * NavMeshPoint.accuracy; j <= height * NavMeshPoint.accuracy; j++)
            {
                //if (i == -550 && j == -300)
                //{
                //    MapData mapData = NavMeshPoint.GetMapDataWithPos(i, j);
                //    Debug.Log(mapData.normalizeX + " " + mapData.normalizeZ + " " + mapData.key);
                //}
                MapData mapData = NavMeshPoint.GetMapDataWithPos(i, j);
                if (mapData != null && !mapDataDic.ContainsKey(mapData.key))
                {
                    GroundKind kind = GetGroundKind(mapData);
                    if (kind == GroundKind.NIL)
                    {
                        if (groundDataDic.ContainsKey(mapData.key))
                        {
                            mapData.groundKind = groundDataDic[mapData.key];
                        }
                    }
                    else
                    {
                        mapData.groundKind = kind;
                    }
                    mapDataDic.Add(mapData.key, mapData);
                    mapDataJs[mapData.key.ToString()] = new int[] { mapData.normalizeY, (int)mapData.groundKind };
                }
            }
        }
        if (navDataJs == null)
        {
            navDataJs = new JSON();
        }
        navDataJs[MAP_DATA_JS_KEY] = mapDataJs;
    }

    static void SetMapPoint()//设置地图路径点信息
    {
        pointObjArr = GameObject.FindGameObjectsWithTag(PATH_OBJ_TAG);
        Dictionary<Transform, MapBlockPathData> mapBlockDataDic = new Dictionary<Transform, MapBlockPathData>();
        foreach (GameObject obj in pointObjArr)
        {
            Transform pathRootTra = obj.transform.root;
            MapBlockPathData blocPathData;
            if (mapBlockDataDic.ContainsKey(pathRootTra))
            {
                blocPathData = mapBlockDataDic[pathRootTra];
            }
            else
            {
                blocPathData = new MapBlockPathData();
                mapBlockDataDic.Add(pathRootTra, blocPathData);
            }
            NavMeshPoint point = obj.GetComponent<NavMeshPoint>();
            switch (point.pointType)
            {
                case NavMeshPoint.PointType.Start:
                    List<NavMeshPoint> exportInfoList = new List<NavMeshPoint>();
                    NavMeshPoint nextPoint = point;
                    for (; ; )
                    {
                        if (nextPoint == null)
                        {
                            break;
                        }
                        MapData mapData = nextPoint.GetMapData(); 
                        if (mapData == null)
                        { 
                            Debug.Log("mapData.key nil:" + nextPoint.name + " " + nextPoint.transform.position);
                        }
                        blocPathData.pointFeatureJs[mapData.key.ToString()] = (int)nextPoint.pointType;
                        blocPathData.blockPathKeyList.Add(mapData.key);
                        switch (nextPoint.pointType)
                        {
                            case NavMeshPoint.PointType.Monster:
                            case NavMeshPoint.PointType.Wait:
                                exportInfoList.Add(nextPoint);
                                break;
                        }
                        if (nextPoint.point.Length <= 0)
                        {
                            break;
                        }
                        nextPoint = nextPoint.point[0];
                    }
                    blocPathData.js[MAP_PATH_JS_KEY] = blocPathData.blockPathKeyList.ToArray();
                    blocPathData.js[MAP_FEATURE_JS_KEY] = blocPathData.pointFeatureJs;
                    blocPathData.js[MAP_MONSTER_JS_KEY] = GetMapPathInfo(exportInfoList);
                    break;
            }
        }
        SaveMapPath(mapBlockDataDic);
    }

    static void SaveMapPath(Dictionary<Transform, MapBlockPathData> mapBlockDataDic)//保存地图路径信息
    {
        JSON[] mapListJsArr = new JSON[mapBlockDataDic.Count];
        foreach (KeyValuePair<Transform, MapBlockPathData> k in mapBlockDataDic)
        {
            int index = -1;
            string name = k.Key.name;
            string lastKey = name.Substring(name.Length - 2, 2);
            switch (lastKey)
            {
                case "_A":
                    index = 0;
                    break;
                case "_B":
                    index = 1;
                    break;
                case "_C":
                    index = 2;
                    break;
                case "_D":
                    index = 3;
                    break;
                case "_E":
                    index = 4;
                    break;
                case "_F":
                    index = 5;
                    break;
                case "_G":
                    index = 6;
                    break;
                case "_H":
                    index = 7;
                    break;
                case "_I":
                    index = 8;
                    break;
                case "_J":
                    index = 9;
                    break;
                case "_K":
                    index = 10;
                    break;
                    break;
                case "_L":
                    index = 11;
                    break;
                    break;
                case "_M":
                    index = 12;
                    break;
                    break;
                case "_N":
                    index = 13;
                    break;
                    break;
                case "_O":
                    index = 14;
                    break;
                    break;
                case "_P":
                    index = 15;
                    break;
                    break;
                case "_Q":
                    index = 16;
                    break;
                default:
                    Debug.Log("SaveMapPath err:" + name + " " + lastKey);
                    break;
            }
            if (index == -1)
            {
                Debug.Log("SetMapPath err:" + k.Key.name);
                return;
            }
            mapListJsArr[index] = k.Value.js;
        }
        navDataJs[MAP_BLOCK_JS_KEY] = mapListJsArr;
    }

    static List<JSON> GetMapPathInfo(List<NavMeshPoint> monsterPointList)//保存地图信息
    {
        List<JSON> monsterJsList = new List<JSON>();
        foreach (NavMeshPoint point in monsterPointList)
        {
            JSON js = new JSON();
            MonsterPointData monsterData = point.monsterPointData;
            js["posKey"] = point.GetMapData().key;
            js["groupIDArr"] = monsterData.groupIdArr;
            js["weightArr"] = monsterData.weightArr;
            js["waitTime"] = monsterData.waitTime;
            monsterJsList.Add(js);
        }
        return monsterJsList;
    }

    static void NavDataServerExport()//地图数据导出
    {
        SetSceneLayer();
        SetGrassData();
        SetMapData();
        SetMapPoint();
        string path = Application.dataPath + GetConfPath();
        ToolText.WriteFile(path, Encoding.UTF8.GetBytes(navDataJs.serialized));
        AssetDatabase.Refresh();
        if (isDraw)
        {
            NavMeshDraw.SetMapDataDic(mapDataDic);
        }
    }

    [MenuItem("Window/EditorWindow/NavDataEditor #G")]
    public static void OpenEditorWindow()
    {
        EditorWindow wnd = EditorWindow.GetWindow(typeof(NavMeshExport), false, title: "NavDataEditor");
        wnd.position = new Rect(Screen.width / 2, Screen.height / 2, 500, 300);
        wnd.Show();
    }

    void OnGUI()
    {
        if (GUILayout.Button("Export"))
        {
            NavDataServerExport();
        }
        GUILayout.Label("/Config/ttdbl2_config_client/" + SceneManager.GetActiveScene().name + "_NavData.json");
        GUILayout.Space(30);
        GUILayout.Label("地图长度");
        float.Parse(Height);
        Height = GUILayout.TextField(Height);
        GUILayout.Label("地图宽度");
        Width = GUILayout.TextField(Width);
        GUILayout.Label("精度");
        NavMeshPoint.Accuracy = GUILayout.TextField(NavMeshPoint.Accuracy);
        isDraw = GUILayout.Toggle(isDraw, "IsDrawMesh");
    }

    static string GetConfPath()
    {
        return "/" + SceneManager.GetActiveScene().name + "_NavData.json";
    }

    static void SetGrassData()
    {
        GrassRender[] grassDataArr = GameObject.FindObjectsOfType<GrassRender>();
        for (int i = 0; i < grassDataArr.Length; i++)
        {
            GrassDataEntry[] grassDataEntryArr = grassDataArr[i].grassData.grassData;
            for (int j = 0; j < grassDataEntryArr.Length; j++)
            {
                for (int k = 0; k < grassDataEntryArr[j].position.Length; k++)
                {
                    Vector3 grassPos = grassDataEntryArr[j].position[k];
                    int x = (int)Math.Round(grassPos.x, 1) * 10;
                    int z = (int)Math.Round(grassPos.z, 1) * 10;
                    SetGrassRangData(x, z);
                }
            }
        }
    }

    static void SetGrassRangData(int x, int z)
    {
        for (int i = x - GRASS_SIZE; i <= x + GRASS_SIZE; i++)
        {
            for (int j = z - GRASS_SIZE; j <= z + GRASS_SIZE; j++)
            {
                long key = ToolText.calcKeyByXZ(i, j);
                groundDataDic[key] = GroundKind.Grass;
            }
        }
    }


    static GroundKind GetGroundKind(MapData m)
    {
        RaycastHit hit;
        Vector3 pos = new Vector3(m.pos.x, -10, m.pos.z);
        if (Physics.Raycast(pos, Vector3.up, out hit, 20.0f, 1 << NavMeshExport.RAY_LAYER))
        {
            string tagName = hit.transform.gameObject.tag;
            GroundKind kind = GroundKind.NIL;
            if (tagGroundDic.ContainsKey(tagName))
            {
                kind = tagGroundDic[tagName];
                switch (tagName)
                {
                    case "Water":
                        if (hit.point.y > m.pos.y)
                        {
                            kind = GroundKind.Water;
                        }
                        break;
                }
                return kind;
            }
        }
        return GroundKind.NIL;
    }

    static void SetSceneLayer()
    {
        foreach (KeyValuePair<string, GroundKind> k in tagGroundDic)
        {
            GameObject[] objArr = GameObject.FindGameObjectsWithTag(k.Key);
            foreach (GameObject obj in objArr)
            {
                obj.layer = RAY_LAYER;
            }
        }
    }
}
