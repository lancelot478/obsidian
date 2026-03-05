using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GuildSceneActivity), true)]
public class GuildSceneActivityInspector : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        GUILayout.Label("Test");
    }
}