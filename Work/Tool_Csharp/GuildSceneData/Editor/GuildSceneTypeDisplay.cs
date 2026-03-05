using UnityEditor;
using UnityEngine;

public class GuildSceneTypeDisplay
{
    [InitializeOnLoadMethod]
    private static void DisplayPoint()
    {
        EditorApplication.hierarchyWindowItemOnGUI += (instanceID, rect) =>
        {
            var gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (gameObject == null)
            {
                return;
            }

            var exist = gameObject.GetComponentInParent<GuildSceneData>();
            if (!exist) return;
            var scenePoint = gameObject.GetComponent<GuildScenePoint>();
            if (scenePoint == null) return;
            var id = GuildSceneDataInspector.ParsePointId(scenePoint.name);
            if (id <= 0)
            {
                return;
            }

            rect.x += rect.width - 50;
            rect.width = 50;
            // EditorGUI.LabelField(rect, $"{scenePoint.pointType}-{id}");
        };
    }
    
    [InitializeOnLoadMethod]
    private static void DisplayTypeNode()
    {
        EditorApplication.hierarchyWindowItemOnGUI += (instanceID, rect) =>
        {
            var gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (gameObject == null)
            {
                return;
            }

            var exist = gameObject.GetComponentInParent<GuildSceneData>();
            if (!exist) return;
            var typeNode = gameObject.GetComponent<GuildSceneTypeNode>();
            if (typeNode == null) return;

            rect.x += rect.width - 80;
            rect.width = 80;
            EditorGUI.LabelField(rect, $"T:{typeNode.pointType} W:{typeNode.weight}");
        };
    }
}