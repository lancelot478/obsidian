using System.IO;
using UnityEditor;
using UnityEngine;

namespace XDPlugin.Tools
{
    public static class AssetHelper
    {
        public static long GetFileSize(string path)
        {
            var obj = AssetDatabase.LoadMainAssetAtPath(path);
            if (obj == null)
            {
                return default;
            }

            var sprite = obj as Sprite;
            Texture texture = null;
            if (sprite != null)
            {
                texture = sprite.texture;
            }

            if (texture == null)
            {
                texture = obj as Texture;
            }

            if (texture != null)
            {
                var type = System.Reflection.Assembly.Load("UnityEditor.dll").GetType("UnityEditor.TextureUtil");
                var methodInfo = type.GetMethod("GetStorageMemorySize",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public);
                if (methodInfo != null)
                {
                    var realSize = (int) methodInfo.Invoke(null, new object[] {texture});
                    return realSize;
                }
            }

            var fileInfo = new FileInfo(path);
            if (fileInfo.Exists)
            {
                return fileInfo.Length;
            }

            return default;
        }
    }
}