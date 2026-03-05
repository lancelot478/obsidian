using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SAGA.Editor
{
    public class CheckRedundantAssetsTreeView : TreeView
    {
        private CheckRedundantAssetsWindow _configWindow;
        
        private readonly CheckRedundantAssetsViewItem _root;

        private Dictionary<string, HashSet<string>> treeData;

        private int _itemIndex = -1;

        public int AcquireIndex
        {
            get
            {
                _itemIndex++;
                return _itemIndex;
            }
        }

        public CheckRedundantAssetsTreeView(TreeViewState state, Dictionary<string, HashSet<string>> treeData, CheckRedundantAssetsWindow configWindow) :
            base(state)
        {
            _configWindow = configWindow;
            showBorder = true;
            showAlternatingRowBackgrounds = true;
            this.treeData = treeData;
            _root = new CheckRedundantAssetsViewItem() {id = AcquireIndex, depth = -1, displayName = "Root"};
        }

        protected override TreeViewItem BuildRoot()
        {
            GenerateTree();
            SetupDepthsFromParentsAndChildren(_root);
            return _root;
        }

        private void GenerateTree()
        {
            if (treeData == null)
            {
                return;
            }

            foreach (var itemInfo in treeData)
            {
                GenerateSingleItem(_root, itemInfo.Key);
            }
        }

        private CheckRedundantAssetsViewItem GenerateSingleItem(CheckRedundantAssetsViewItem viewItem, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return viewItem;
            }

            var index = path.IndexOf("/", StringComparison.Ordinal);
            string name;
            if (index > -1)
            {
                name = path.Substring(0, index);
                path = path.Substring(index + 1, path.Length - index - 1);
            }
            else
            {
                name = path;
                path = string.Empty;
            }

            if (!viewItem.hasChildren ||
                !(viewItem.children.Find(item => item.displayName == name) is CheckRedundantAssetsViewItem newItem))
            {
                var fullPath = string.IsNullOrEmpty(viewItem.Path) ? name : Path.Combine(viewItem.Path, name);
                newItem = new CheckRedundantAssetsViewItem(fullPath) {id = AcquireIndex, displayName = name};
                viewItem.AddChild(newItem);
            }

            return GenerateSingleItem(newItem, path);
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            base.SelectionChanged(selectedIds);

            if (selectedIds.Count <= 0)
            {
                return;
            }

            var findItems = FindRows(selectedIds);
            var findItem = findItems.FirstOrDefault();
            if (!(findItem is CheckRedundantAssetsViewItem configItem))
            {
                return;
            }

            var path = configItem.Path;
            var pathObject = AssetDatabase.LoadMainAssetAtPath(path);
            if (pathObject == null)
            {
                return;
            }

            Selection.activeObject = pathObject;
            EditorGUIUtility.PingObject(pathObject);
            _configWindow.SelectAsset = path;
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            base.RowGUI(args);

            if (!(args.item is CheckRedundantAssetsViewItem configItem))
            {
                return;
            }

            var sizeString = configItem.GetSizeString();
            if (string.IsNullOrEmpty(sizeString))
            {
                return;
            }
            
            var height = args.rowRect.height;
            var width = 50;
            var right = args.rowRect.xMax;
            var labelRect = new Rect(right - width, args.rowRect.yMin, width, height);
            GUI.Label(labelRect, new GUIContent(sizeString, "当前资源大小"));
        }
    }
}