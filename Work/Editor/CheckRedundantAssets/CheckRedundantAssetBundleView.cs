using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;

namespace SAGA.Editor
{
    public class AssetBundleConfigAssetCorrespondingBundleTreeView : TreeView
    {
        private readonly CheckRedundantAssetBundleViewItem _root;

        private readonly HashSet<string> _treeData;

        private int _itemIndex = -1;

        public int AcquireIndex
        {
            get
            {
                _itemIndex++;
                return _itemIndex;
            }
        }

        public AssetBundleConfigAssetCorrespondingBundleTreeView(TreeViewState state, HashSet<string> treeData) :
            base(state)
        {
            showBorder = true;
            showAlternatingRowBackgrounds = true;
            _treeData = treeData;
            _root = new CheckRedundantAssetBundleViewItem()
                {id = AcquireIndex, depth = -1, displayName = "Root"};
        }

        protected override TreeViewItem BuildRoot()
        {
            foreach (var item in _treeData)
            {
                var child = new CheckRedundantAssetBundleViewItem
                {
                    id = AcquireIndex, depth = 0, displayName = item
                };
                _root.AddChild(child);
            }

            return _root;
        }
    }
}