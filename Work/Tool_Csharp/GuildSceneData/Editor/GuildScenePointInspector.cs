using System;
using SAGA.Editor;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GuildScenePoint), true)]
public class GuildScenePointInspector : Editor
{
    private SerializedProperty _durationMinProperty;
    private SerializedProperty _durationMaxProperty;
    private SerializedProperty _activitiesProperty;
    private SerializedProperty _moveNextProperty;
    private SerializedProperty _syncRotationProperty;


    private void OnEnable()
    {
        _durationMinProperty = serializedObject.FindProperty("durationMin");
        _durationMaxProperty = serializedObject.FindProperty("durationMax");

        _moveNextProperty = serializedObject.FindProperty("moveNext");
        _activitiesProperty = serializedObject.FindProperty("activities");
        _syncRotationProperty = serializedObject.FindProperty("syncRotation");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var rotationContent = new GUIContent { text = "是否需要旋转", tooltip = "节点旋转状态" };
        _syncRotationProperty.boolValue = EditorGUILayout.Toggle(rotationContent, _syncRotationProperty.boolValue);

        var durationMinContent = new GUIContent { text = "最小持续时间", tooltip = "节点的持续时间" };
        EditorGUILayout.Slider(_durationMinProperty, 0, 60, durationMinContent);

        var durationMaxContent = new GUIContent { text = "最大持续时间", tooltip = "节点的持续时间" };
        EditorGUILayout.Slider(_durationMaxProperty, 0, 60, durationMaxContent);

        if (_durationMaxProperty.floatValue < _durationMinProperty.floatValue)
        {
            _durationMinProperty.floatValue = _durationMaxProperty.floatValue;
        }

        if (_durationMinProperty.floatValue > _durationMaxProperty.floatValue)
        {
            _durationMinProperty.floatValue = _durationMaxProperty.floatValue;
        }

        var moveNextContent = new GUIContent { text = "是否需要移动", tooltip = "当切换到下个节点是否移动" };
        _moveNextProperty.boolValue = EditorGUILayout.Toggle(moveNextContent, _moveNextProperty.boolValue);

        var activitiesContent = new GUIContent { text = "行为列表", tooltip = "当前节点的行为" };

        _activitiesProperty.isExpanded = EditorGUILayout.Foldout(_activitiesProperty.isExpanded, activitiesContent);
        if (_activitiesProperty.isExpanded)
        {
            EditorGUI.indentLevel++;
            using (new GUILayout.HorizontalScope())
            {
                _activitiesProperty.arraySize = EditorGUILayout.IntField("总数量", _activitiesProperty.arraySize);
                if (GUILayout.Button("添加"))
                {
                    CreateNewPoint();
                }

                if (GUILayout.Button("检测"))
                {
                }

                if (GUILayout.Button("清空"))
                {
                    EditorHelper.ShowDialog("是否清空所有行为？", () => { _activitiesProperty.ClearArray(); });
                }
            }

            for (var i = 0; i < _activitiesProperty.arraySize; ++i)
            {
                var elementProperty = _activitiesProperty.GetArrayElementAtIndex(i);
                using (new GUILayout.HorizontalScope())
                {
                    var foldoutContent = new GUIContent($"行为 - {i + 1}");
                    elementProperty.isExpanded = EditorGUILayout.Foldout(elementProperty.isExpanded, foldoutContent);
                    if (EditorGUILayout.LinkButton("删除行为"))
                    {
                        _activitiesProperty.DeleteArrayElementAtIndex(i);
                        continue;
                    }
                }

                EditorGUI.indentLevel++;

                if (elementProperty.isExpanded)
                {
                    var typeProperty = elementProperty.FindPropertyRelative("type");
                    var triggerTypeProperty = elementProperty.FindPropertyRelative("triggerType");
                    var durationMinProperty = elementProperty.FindPropertyRelative("durationMin");
                    var durationMaxProperty = elementProperty.FindPropertyRelative("durationMax");
                    var paramProperty = elementProperty.FindPropertyRelative("param");
                    var syncPointDurationProperty = elementProperty.FindPropertyRelative("syncPointDuration");

                    EditorGUILayout.PropertyField(typeProperty, new GUIContent("行为类型"));
                    var paramName = (GuildSceneActivityType)typeProperty.enumValueFlag switch
                    {
                        GuildSceneActivityType.Anim => "动画名称",
                        GuildSceneActivityType.Bubble => "语言ID",
                        _ => string.Empty
                    };

                    if (!string.IsNullOrEmpty(paramName))
                    {
                        EditorGUILayout.PropertyField(paramProperty, new GUIContent(paramName));
                    }

                    EditorGUILayout.PropertyField(triggerTypeProperty, new GUIContent("触发时机"));

                    // 行为时间
                    EditorGUILayout.PropertyField(syncPointDurationProperty, new GUIContent("同步节点时间"));
                    if (syncPointDurationProperty.boolValue)
                    {
                        durationMinProperty.floatValue = _durationMinProperty.floatValue;
                        durationMaxProperty.floatValue = _durationMaxProperty.floatValue;
                    }

                    GUI.enabled = !syncPointDurationProperty.boolValue;
                    var actMinDurPropContent = new GUIContent { text = "最小持续时间", tooltip = "默认读节点时间" };
                    EditorGUILayout.Slider(durationMinProperty, 0, 60, actMinDurPropContent);

                    var actMaxDurPropContent = new GUIContent { text = "最大持续时间", tooltip = "默认读节点时间" };
                    EditorGUILayout.Slider(durationMaxProperty, 0, 60, actMaxDurPropContent);

                    if (durationMaxProperty.floatValue < durationMinProperty.floatValue)
                    {
                        durationMinProperty.floatValue = durationMaxProperty.floatValue;
                    }

                    if (durationMinProperty.floatValue > durationMaxProperty.floatValue)
                    {
                        durationMinProperty.floatValue = durationMaxProperty.floatValue;
                    }

                    GUI.enabled = true;
                }

                EditorGUI.indentLevel--;
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void CreateNewPoint()
    {
        var maxIndex = _activitiesProperty.arraySize;
        _activitiesProperty.InsertArrayElementAtIndex(maxIndex);
        var newProperty = _activitiesProperty.GetArrayElementAtIndex(maxIndex);

        var typeProperty = newProperty.FindPropertyRelative("type");
        typeProperty.enumValueFlag = (int)GuildSceneActivityType.Anim;

        var triggerTypeProperty = newProperty.FindPropertyRelative("triggerType");
        triggerTypeProperty.enumValueFlag = (int)GuildSceneActivityTriggerType.Enter;

        var syncPointDurationProperty = newProperty.FindPropertyRelative("syncPointDuration");
        syncPointDurationProperty.boolValue = true;

        var durationProperty = newProperty.FindPropertyRelative("duration");


        var paramProperty = newProperty.FindPropertyRelative("param");
        paramProperty.stringValue = string.Empty;

        serializedObject.ApplyModifiedProperties();
    }
}