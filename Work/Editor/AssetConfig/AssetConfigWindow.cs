using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace SAGA.Editor
{
    public partial class AssetConfigWindow : EditorWindow
    {
        private UnityEditor.IMGUI.Controls.TreeView _treeView;
    
    
    
        // [MenuItem("Tools/AssetConfig")]
        public static AssetConfigWindow OpenAssetConfigWindow()
        {
            var window = CreateWindow<AssetConfigWindow>();
            window.titleContent = new GUIContent(nameof(AssetConfigWindow));
            window.Focus();
            window.Repaint();
            return window;
        }

        private void OnEnable()
        {
            if (_treeViewState == null)
            {
                _treeViewState = new TreeViewState();
            }

            if (_treeView == null)
            {
                // _treeView = new UnityEditor.IMGUI.Controls.TreeView();
            }
        }
    }    
}
