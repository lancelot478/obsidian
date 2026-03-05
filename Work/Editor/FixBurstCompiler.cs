#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System;

public class FixBurstCompiler : IPreprocessBuildWithReport
{
    public int callbackOrder { get { return 0; } }

    public void OnPreprocessBuild(BuildReport report)
    {
#if UNITY_2018_4
        var ndkRoot = EditorPrefs.GetString("AndroidNdkRootR16b");

        Environment.SetEnvironmentVariable("ANDROID_NDK_ROOT", ndkRoot);
#endif
    }
}
#endif