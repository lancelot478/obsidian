using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System;

[Serializable]
public class MonsterPointData
{
    public int[] groupIdArr;//随机怪物组
    public int[] weightArr;//随机怪物组权重
    public bool isHideVal;//是否默认隐藏怪物组
    public int waitTime;//多久后进入战斗状态
}

public class NavMeshPoint : MonoBehaviour
{
    public static string Accuracy = "0.1";
    public static int accuracy = Mathf.CeilToInt((1 / float.Parse(Accuracy)));
    public enum PointType
    {
        Normal = 0,
        Start = 1,
        End = 2,
        Stop = 3,//不可刷怪区域
        Collect = 4,//采集点
        Monster = 5,//怪物点
        Save = 6,//存档点
        Wait = 7,//等待点
    }

    public NavMeshPoint[] point;//下个路径点
    public PointType pointType;//路径点类型
    public MonsterPointData monsterPointData;//路径点怪物信息
    MapData mapData;

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public MapData GetMapData()
    {
        if (mapData == null)
        {
            Vector3 pos = transform.transform.position;
            int x = GetAccuracyVal(pos.x);
            int z = GetAccuracyVal(pos.z);
            mapData = GetMapDataWithPos(x, z);
        }
        return mapData;
    }
    public void OnDrawGizmosSelected()
    {
        for (int i = 0; i < point.Length; i++)
        {
            if (isRedZonePoint(pointType) && isRedZonePoint(point[i].pointType))
            {
                Gizmos.color = Color.red;
            }
            else
            {
                Gizmos.color = new Color(0, 0, 1, 0.5f);
            }
            Vector3 nearPoint = point[i].transform.position;
            Vector3 pathPoint = transform.position;
            Vector3 link = point[i].transform.position - transform.position;
            Vector3 linknormal = link.normalized;
            //根据两点连线确定经过两点连线的垂直方向向量
            Vector3 verticallink = GetVerticalVector3(linknormal);
            for (int _i = 0; _i < 20; _i++)
            {
                Vector3 squareLength1 = verticallink * _i / 4f + pathPoint;
                Vector3 squareLength2 = -verticallink * _i / 4f + pathPoint;
                Vector3 squareWidth1 = verticallink * _i / 4f + nearPoint;
                Vector3 squareWidth2 = -verticallink * _i / 4f + nearPoint;
                Gizmos.DrawLine(squareLength1, squareWidth1);
                Gizmos.DrawLine(squareLength2, squareWidth2);
            }
        }
    }

    //求垂直向量
    public static bool isRedZonePoint(PointType pt)
    {
        return pt == PointType.Collect || pt == PointType.Stop;
    }
    public static Vector3 GetVerticalVector3(Vector3 dir)
    {
        if (dir.x == 0)
        {
            return new Vector3(0, dir.y, -1);
        }
        return new Vector3(-dir.z / dir.x, dir.z, 1).normalized;
    }

    public static int GetAccuracyVal(float val)
    {
        return Mathf.FloorToInt(val * 10);
    }

    public static MapData GetMapDataWithPos(int x, int z)
    {
        for (int k = -100; k <= 600; k++)//y:-10 ~ 60
        {
            NavMeshHit hit;
            Vector3 sourcePos = new Vector3(x / (float)accuracy, k / 10f, z / (float)accuracy);
            if (NavMesh.SamplePosition(sourcePos, out hit, 0.1f, NavMesh.AllAreas))
            {
                MapData mapData = new MapData(hit.position, x, z);
                return mapData;
            }
        }
        return null;
    }
}
public class MapData
{
    public long key;
    public int normalizeX;
    public int normalizeY;
    public int normalizeZ;
    public Vector3 pos;
    public Vector2 normalizePos;
    public GroundKind groundKind;
    public MapData(Vector3 hitPos, int x, int z)
    {
        normalizeX = x;
        normalizeY = Mathf.FloorToInt(hitPos.y * 10);
        normalizeZ = z;
        normalizePos = new Vector2(normalizeX, normalizeZ);
        if (normalizeZ < 0)
        {
            key = ((long)normalizeX << 32) | ((long)-z | (long)0x80000000);
        }
        else
        {
            key = ((long)normalizeX << 32) | (long)normalizeZ;
        }
        pos = hitPos;
    }
}

public enum GroundKind
{
    Dirt = 0,//泥地
    Grass = 1,//草
    Stone = 2,//石头
    Wood = 3,//木头
    Water = 4,//水
    NIL = 99,
}
