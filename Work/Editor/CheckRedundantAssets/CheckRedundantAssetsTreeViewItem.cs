using System.IO;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using XDPlugin.Tools;

namespace SAGA.Editor
{
    public class CheckRedundantAssetsViewItem : TreeViewItem
    {
        public readonly string Path;

        private readonly long _fileSize;

        public CheckRedundantAssetsViewItem(string path = "")
        {
            Path = path;
            icon = AssetDatabase.GetCachedIcon(path) as Texture2D;


            // 获取大小
            _fileSize = AssetHelper.GetFileSize(path);
        }

        public string GetSizeString()
        {
            return _fileSize == 0 ? string.Empty : EditorUtility.FormatBytes(_fileSize);
        }
    }
}