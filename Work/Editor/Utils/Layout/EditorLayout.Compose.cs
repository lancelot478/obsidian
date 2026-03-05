using System;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace SAGA.Editor
{
    public static partial class EditorLayout
    {
        public static void ToolBarButton(string content, Action action, string tooltip = null)
        {
            var call = GUILayout.Button(new GUIContent(content, tooltip), EditorStyles.toolbarButton,
                NormalButtonWidth);
            if (!call) return;
            action?.Invoke();
        }

        public static void MiniToggleButton(bool state, Action<bool> onChanged, string content, string tooltip = null)
        {
            var newState = GUILayout.Toggle(state, new GUIContent(content, tooltip), EditorStyles.toolbarButton,
                MiniButtonWidth);
            if (newState == state)
            {
                return;
            }

            onChanged?.Invoke(newState);
        }

        public static void ExpandHorizontalScope([CanBeNull] Action action)
        {
            using (new GUILayout.HorizontalScope("box", GUILayout.ExpandWidth(true)))
            {
                action?.Invoke();
            }
        }

        public static Vector2 ScrollView(Vector2 position, Action action)
        {
            position = EditorGUILayout.BeginScrollView(position);
            action?.Invoke();
            EditorGUILayout.EndScrollView();
            return position;
        }
    }
}