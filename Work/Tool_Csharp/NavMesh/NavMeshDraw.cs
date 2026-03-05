using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NavMeshDraw : MonoBehaviour
{
    static Dictionary<long, MapData> mapDataDic;
    static Dictionary<GroundKind, Color> groundColDic = new Dictionary<GroundKind, Color>()
    {
        {GroundKind.Grass, Color.green},
        {GroundKind.Water, Color.blue},
        {GroundKind.Stone, Color.gray},
        {GroundKind.Wood, Color.yellow},
        {GroundKind.Dirt, Color.white},
    };
    static NavMeshDraw self;

    void OnDrawGizmos()
    {
        if (mapDataDic == null)
        {
            return;
        }
        foreach (MapData mapData in mapDataDic.Values)
        {
            Vector3 pos = mapData.pos;
            Vector3 startPos = new Vector3(pos.x, 0, pos.z);
            Color col = Color.white;
            groundColDic.TryGetValue(mapData.groundKind, out col);
            Gizmos.color = col;
            Gizmos.DrawLine(startPos , pos);
        }
    }

    public static void SetMapDataDic(Dictionary<long, MapData> mapDataDic)
    {
        NavMeshDraw.mapDataDic = mapDataDic;
        if (NavMeshDraw.self == null)
        {
            GameObject obj = new GameObject("SetMapDataDic");
            NavMeshDraw.self = obj.AddComponent<NavMeshDraw>();
        }
    }
}
