using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using SAGA.Editor;
using UnityEngine;

public partial class GuildSceneDataInspector
{
    private void ExportConfig()
    {
        var groupNodes = FindObjectsOfType<GuildSceneTypeNode>();
        if (groupNodes == null)
        {
            return;
        }

        var config = new List<GuildSceneTypeNodeData>();
        foreach (var groupNode in groupNodes)
        {
            var validPoint = true;
            var nodeCount = groupNode.transform.childCount;
            var pointList = new List<GuildScenePointData[]>();
            for (var idx = 0; idx < nodeCount; idx++)
            {
                var nodeTran = groupNode.transform.GetChild(idx);
                if (nodeTran == null)
                {
                    continue;
                }

                var pointCount = nodeTran.transform.childCount;
                var points = new List<GuildScenePointData>();
                for (var pIndex = 0; idx < pointCount; idx++)
                {
                    var pointTran = nodeTran.transform.GetChild(pIndex);
                    var pointName = pointTran.name;
                    if (pointTran == null)
                    {
                        Debug.LogError($"路径点 {pIndex} 不存在，跳过处理...");
                        validPoint = false;
                        break;
                    }

                    var scenePoint = pointTran.GetComponent<GuildScenePoint>();
                    if (scenePoint == null)
                    {
                        Debug.LogError($"路径点 {pointName} 不存在GuildScenePoint组件，跳过处理...");
                        validPoint = false;
                        break;
                    }

                    var pointIdStr = pointName.Replace("Point_", "");
                    if (!int.TryParse(pointIdStr, out var pointId))
                    {
                        Debug.LogError($"路径点 {pointName} 命名有误，跳过处理...");
                        validPoint = false;
                        break;
                    }

                    var transform = scenePoint.transform;
                    var pos = transform.position;
                    var rot = transform.rotation.eulerAngles;
                    var pointData = new GuildScenePointData
                    {
                        id = pointId,
                        name = $"{groupNode.name} - {pointName}",
                        position = new Vec3
                        {
                            x = pos.x,
                            y = pos.y,
                            z = pos.z,
                        },
                        durationMin = scenePoint.durationMin,
                        durationMax = scenePoint.durationMax,
                        moveNext = scenePoint.moveNext,
                        activities = scenePoint.activities,
                    };
                    if (scenePoint.syncRotation)
                    {
                        pointData.rotation = new Vec3
                        {
                            x = rot.x,
                            y = rot.y,
                            z = rot.z,
                        };
                    }

                    points.Add(pointData);
                }

                pointList.Add(points.ToArray());
            }

            if (!validPoint)
            {
                continue;
            }

            if (pointList.Count <= 0)
            {
                continue;
            }

            var typeData = new GuildSceneTypeNodeData
            {
                weight = groupNode.weight,
                pointType = groupNode.pointType,
                characterType = (int)groupNode.characterType,
                points = pointList.ToArray()
            };
            config.Add(typeData);
        }

        var existConfigStr = string.Empty;
        if (File.Exists(ConfigPath))
        {
            existConfigStr = File.ReadAllText(ConfigPath);
        }

        var wrapper = JsonConvert.DeserializeObject<Dictionary<string, GuildSceneTypeNodeData[]>>(existConfigStr);
        if (wrapper == null)
        {
            wrapper = new Dictionary<string, GuildSceneTypeNodeData[]>();
        }

        wrapper[GameObject.name] = config.ToArray();
        var configStr = JsonConvert.SerializeObject(wrapper, Formatting.Indented);

        File.WriteAllText(ConfigPath, configStr);
        EditorHelper.ShowDialog($"导出成功: {ConfigPath}", cancel: null);
    }
}