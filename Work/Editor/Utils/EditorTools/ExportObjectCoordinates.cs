using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text;
using System.Linq;

public class ExportOrderedCoordinates : EditorWindow
{
    private bool openAfterExport = true;
    private string inputCoordinates = "";
    private GameObject slimePrefab; // 用于存储油油史莱姆 prefab

    [MenuItem("关卡的妙妙小工具/导出已排序对象的XZ坐标")]
    public static void ShowWindow()
    {
        GetWindow<ExportOrderedCoordinates>("导出已排序的XZ坐标");
    }

    private void OnEnable()
    {
        // 加载油油史莱姆 prefab
        slimePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ModelMonster/Slime3.prefab");

        // 检查 prefab 是否成功加载
        if (slimePrefab == null)
        {
            Debug.LogError("未能加载油油史莱姆 prefab，请检查路径是否正确！");
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("导出已选择对象的 XZ 坐标", EditorStyles.boldLabel);
        GUILayout.Label("提示：对象将按名称排序后导出坐标为 .txt 文件", EditorStyles.wordWrappedLabel);
        openAfterExport = EditorGUILayout.Toggle("导出后自动打开文件", openAfterExport);

        if (GUILayout.Button("导出选中对象的名称和坐标"))
        {
            ExportCoordinates();
        }

        GUILayout.Space(10);
        GUILayout.Label("对现有的坐标有疑惑？输入你的XZ坐标，让我们在场景里对应位置生成油油史莱姆！", EditorStyles.wordWrappedLabel);
        inputCoordinates = EditorGUILayout.TextField("输入 X,Z 坐标", inputCoordinates);

        if (GUILayout.Button("生成油油史莱姆"))
        {
            GenerateSlimeAtCoordinates();
        }
    }

    private void ExportCoordinates()
    {
        var selectedTransforms = Selection.transforms;
        if (selectedTransforms.Length == 0)
        {
            Debug.LogWarning("未选择任何对象！");
            return;
        }

        var sortedTransforms = selectedTransforms.OrderBy(t => t.name).ToArray();

        StringBuilder txt = new StringBuilder();
        StringBuilder names = new StringBuilder();
        StringBuilder coordinates = new StringBuilder();

        foreach (var trans in sortedTransforms)
        {
            var position = trans.position;
            names.Append($"{trans.name},");
            coordinates.Append($"{position.x:F2},{position.z:F2},");
        }

        if (names.Length > 0) names.Length--;
        if (coordinates.Length > 0) coordinates.Length--;

        txt.AppendLine(names.ToString());
        txt.AppendLine(coordinates.ToString());

        string filePath = EditorUtility.SaveFilePanel("保存坐标", "", "ordered_xz_coordinates.txt", "txt");
        if (!string.IsNullOrEmpty(filePath))
        {
            try
            {
                File.WriteAllText(filePath, txt.ToString());
                Debug.Log("坐标已导出到 " + filePath);
                if (openAfterExport)
                {
                    System.Diagnostics.Process.Start(filePath);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError("导出文件时发生错误: " + ex.Message);
            }
        }
    }

    private void GenerateSlimeAtCoordinates()
    {
        if (string.IsNullOrEmpty(inputCoordinates))
        {
            Debug.LogWarning("请提供有效的坐标！");
            return;
        }

        // 移除引号和多余的空格
        inputCoordinates = inputCoordinates.Replace("\"", "").Trim();
        Debug.Log($"处理后的输入坐标: {inputCoordinates}"); // 输出调试信息

        if (slimePrefab == null)
        {
            Debug.LogError("无法生成油油史莱姆，因为油油史莱姆 prefab 未加载。");
            return;
        }

        // 按照 '@' 分隔坐标组
        var coordinateGroups = inputCoordinates.Split('@');

        for (int groupIndex = 0; groupIndex < coordinateGroups.Length; groupIndex++)
        {
            var group = coordinateGroups[groupIndex];
            var coordinates = group.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (coordinates.Length % 2 != 0) // 确保坐标对的数量是偶数
            {
                Debug.LogWarning($"组{groupIndex + 1}的输入坐标格式无效！每个坐标组必须有成对的坐标 (X,Z)。");
                continue; // 继续处理其他组
            }

            for (int i = 0; i < coordinates.Length; i += 2)
            {
                if (float.TryParse(coordinates[i], out float x) && float.TryParse(coordinates[i + 1], out float z))
                {
                    // 实例化油油史莱姆 prefab
                    var slime = PrefabUtility.InstantiatePrefab(slimePrefab) as GameObject;
                    slime.transform.position = new Vector3(x, 1, z); // 默认在 y=1 高度创建
                    
                    // 根据组别和点位编号命名
                    int pointIndex = (i / 2) + 1; // 计算点位索引
                    slime.name = $"组{groupIndex + 1}点位{pointIndex}"; // 使用组号和点位编号命名
                    Debug.Log($"在坐标 ({x}, 1, {z}) 生成了 {slime.name}。");
                }
                else
                {
                    Debug.LogWarning($"组{groupIndex + 1}的坐标 ({coordinates[i]}, {coordinates[i + 1]}) 无效，无法生成油油史莱姆。");
                }
            }
        }
    }
}
