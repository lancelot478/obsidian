using System;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;

public class GuildSceneData : MonoBehaviour
{
    private void Awake()
    {
        transform.position = Vector3.zero;

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);  
#endif
    }
    
}