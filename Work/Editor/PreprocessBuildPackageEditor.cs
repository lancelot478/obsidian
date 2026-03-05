using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class PreprocessBuildPackageEditor : UnityEditor.Build.IPreprocessBuildWithReport, UnityEditor.Build.IPostprocessBuildWithReport{
    public int callbackOrder {
        get { return 1; }
    }

    public void OnPostprocessBuild(BuildReport report) {
        
    }


    public void OnPreprocessBuild(BuildReport report)
    {
       
        
        // report.summary.platform, report.summary.outputPath
    }
}
