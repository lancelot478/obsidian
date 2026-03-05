using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace SAGA.Editor
{
    public static class FileHelper
    {
        [Conditional("UNITY_EDITOR_WIN")]
        public static void OpenDirectory(string path, bool isFile = false)
        {
            if (string.IsNullOrEmpty(path)) return;
            path = path.Replace("/", "\\");
            if (isFile)
            {
                if (!File.Exists(path))
                {
                    Debug.LogError("No File: " + path);
                    return;
                }

                path = $"/Select, {path}";
            }
            else
            {
                if (!Directory.Exists(path))
                {
                    Debug.LogError("No Directory: " + path);
                    return;
                }
            }

            Process.Start("explorer.exe", path);
        }
        
        public static void SelectFolder(Action<string> onSelect, string defaultFolder = "", string title = "Select Folder")
        {
            if (string.IsNullOrEmpty(defaultFolder))
            {
                defaultFolder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            }
            var folderName = EditorUtility.OpenFolderPanel(title, defaultFolder, string.Empty);
            if (string.IsNullOrEmpty(folderName))
            {
                return;
            }
            onSelect?.Invoke(folderName);
        }
    }
}