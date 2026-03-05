using System.IO;
using UnityEditor;
using UnityEngine;

namespace SAGA.Editor
{
    public class EditorConfig
    {
#if UNITY_ANDROID
        public const BuildTarget BuildTarget = UnityEditor.BuildTarget.Android;
#elif UNITY_IPHONE
        public const BuildTarget BuildTarget = UnityEditor.BuildTarget.iOS;
#endif
        // public static string BuildTargetString = BuildTarget.ToString().ToUpper();
        public static string EditorAssetBundlePath = Path.Combine(Application.dataPath, "../_AssetsBundles");
        // public static string PlatformAssetBundlePath = Path.Combine(EditorAssetBundlePath, BuildTargetString);
    }
}