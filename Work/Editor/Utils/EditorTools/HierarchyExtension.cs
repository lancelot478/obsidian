using UnityEditor;
using UnityEngine;

public class HierarchyExtension : MonoBehaviour {
    private static readonly string functionSaveKey = "HierarchyExtensionOpenState";

    [MenuItem(("Tools/HierarchyExtension开关"))]
    public static void ToggleFunctionOn() {
        int currentValue = PlayerPrefs.GetInt(functionSaveKey, 1);
        int newValue = currentValue == 1 ? 0 : 1;
        PlayerPrefs.SetInt(functionSaveKey, newValue);
    }

    [InitializeOnLoadMethod]
    static void InitOnLoad()
    {
        EditorApplication.hierarchyWindowItemOnGUI += (instanceID, rect) =>
        {
            int currentFunctionValue = PlayerPrefs.GetInt(functionSaveKey, 1);
            if (currentFunctionValue != 1) {
                return;
            }
            var gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (gameObject == null)
            {
                return;
            }

            var state = gameObject.activeSelf;
            rect.width = 20;
            var newValue = EditorGUI.Toggle(rect, state);
            if (newValue == state)
            {
                return;
            }

            gameObject.SetActive(newValue);
            EditorUtility.SetDirty(gameObject);
        };
    }
}