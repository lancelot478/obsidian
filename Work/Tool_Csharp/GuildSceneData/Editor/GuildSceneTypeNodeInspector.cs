using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GuildSceneTypeNode), true)]
public class GuildSceneTypeNodeInspector : Editor
{
    private GuildSceneTypeNode Target => target as GuildSceneTypeNode;
    private GameObject GameObject => Target.gameObject;
    private Transform Transform => Target.transform;

    private SerializedProperty _pointTypeProperty;
    private SerializedProperty _weightTypeProperty;
    private SerializedProperty _characterTypeProperty;

    private void OnEnable()
    {
        _pointTypeProperty = serializedObject.FindProperty("pointType");
        _weightTypeProperty = serializedObject.FindProperty("weight");
        _characterTypeProperty = serializedObject.FindProperty("characterType");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var pointTypeContent = new GUIContent { text = "节点类型", tooltip = "节点的类型" };
        EditorGUILayout.PropertyField(_pointTypeProperty, pointTypeContent);

        EditorGUILayout.IntSlider(_weightTypeProperty, 0, 100, new GUIContent("节点权重"));

        var characterTypeTitle = new GUIContent { text = "角色类型", tooltip = "允许的角色类型" };
        var val = (CharacterType)_characterTypeProperty.enumValueFlag;
        val = (CharacterType)EditorGUILayout.EnumFlagsField(characterTypeTitle, val);
        _characterTypeProperty.enumValueFlag = (int)val;

        // if (GUILayout.Button("创建路径点"))
        // {
        //     CreatePathPoint();
        // }

        serializedObject.ApplyModifiedProperties();
    }

    private void CreatePathPoint()
    {
        var maxIdx = 0;
        for (int idx = 0; idx < Transform.childCount; idx++)
        {
            var tran = Transform.GetChild(idx);
            var realIdx = GuildSceneDataInspector.ParsePointId(tran.name);
            if (realIdx > 0)
            {
                maxIdx = Mathf.Max(maxIdx, realIdx);
            }
        }

        var obj = new GameObject($"Point_{maxIdx + 1}");
        obj.transform.SetParent(Transform);
        obj.AddComponent<GuildScenePoint>();

        EditorUtility.SetDirty(GameObject);
    }
}