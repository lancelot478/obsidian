using System;
using System.Diagnostics;
using SLua;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CustomLuaClass]
public static class EditorAssetTool
{
    public static Object LoadAssetAtPath(string path, Type type = null)
    {
#if UNITY_EDITOR
        type ??= typeof(Object);
        var asset = AssetDatabase.LoadAssetAtPath(path, type);
        return asset;
#else
        return default;
#endif
    }
}