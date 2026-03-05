using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using SAGA.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

[CustomEditor(typeof(GuildSceneData), true)]
public partial class GuildSceneDataInspector : Editor
{
    private GuildSceneData Target => target as GuildSceneData;
    private GameObject GameObject => Target.gameObject;
    private Transform Transform => Target.transform;

    private string ConfigPath { get; set; }

    private void OnEnable()
    {
        ConfigPath = Path.Combine(Application.dataPath, "Config/ttdbl2_config/GuildScenePath.json");
    }

    public override void OnInspectorGUI()
    {
        GUILayout.Label("节点管理");

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("创建类型节点"))
        {
            CreateNewTypeNode();
        }

        if (GUILayout.Button("清理无效节点"))
        {
            ClearInvalidPoint();
        }

        GUILayout.EndHorizontal();


        GUILayout.Label("配置操作");
        if (GUILayout.Button("导出配置"))
        {
            ExportConfig();
        }
    }

    private void CreateNewTypeNode()
    {
        var maxIdx = 0;
        for (int idx = 0; idx < Transform.childCount; idx++)
        {
            var tran = Transform.GetChild(idx);
            var realIdx = ParsePointId(tran.name);
            if (realIdx > 0)
            {
                maxIdx = Mathf.Max(maxIdx, realIdx);
            }
        }

        var obj = new GameObject($"TypeNode");
        obj.transform.SetParent(Transform);
        obj.AddComponent<GuildSceneTypeNode>();

        EditorUtility.SetDirty(GameObject);
    }

    private void ClearInvalidPoint()
    {
        // return;
        // EditorHelper.ShowDialog("将会移除不符合规则的节点，请注意保存临时数据，是否继续？", () =>
        // {
        //     var invalidNode = new HashSet<GameObject>();
        //     var existName = new HashSet<string>();
        //     for (var idx = 0; idx < Transform.childCount; idx++)
        //     {
        //         var tran = Transform.GetChild(idx);
        //         var childName = tran.name;
        //         var childIdx = ParsePointId(tran.name);
        //         if (childIdx > 0)
        //         {
        //             if (!existName.Contains(childName))
        //             {
        //                 existName.Add(childName);
        //             }
        //             else
        //             {
        //                 invalidNode.Add(tran.gameObject);
        //             }
        //         }
        //         else
        //         {
        //             invalidNode.Add(tran.gameObject);
        //         }

        //         if (tran.GetComponent<GuildScenePoint>() == null)
        //         {
        //             invalidNode.Add(tran.gameObject);
        //         }
        //     }

        //     foreach (var node in invalidNode)
        //     {
        //         DestroyImmediate(node);
        //     }

        //     EditorUtility.SetDirty(GameObject);
        // });
    }

    public static int ParsePointId(string name)
    {
        var idStr = name.Replace("Point_", "");
        return int.TryParse(idStr, out var id) ? id : default;
    }
}