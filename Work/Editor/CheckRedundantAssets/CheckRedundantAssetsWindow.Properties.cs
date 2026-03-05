using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Serialization;

namespace SAGA.Editor
{
    public partial class CheckRedundantAssetsWindow
    {
        private TreeView _assetTreeView;

        private TreeView AssetsAssetTreeView
        {
            set
            {
                if (_assetTreeView == value)
                {
                    return;
                }

                _assetTreeView = value;
                SelectAsset = null;
            }
            get
            {
                if (_assetTreeView == null)
                {
                    _assetTreeView = new CheckRedundantAssetsTreeView(TreeViewState, FilteredAnalyseData, this);
                    _assetTreeView.Reload();
                }

                return _assetTreeView;
            }
        }

        private TreeViewState TreeViewState { get; } = new TreeViewState();
        private bool _filterRedundant;

        private bool FilterRedundant
        {
            get => _filterRedundant;
            set
            {
                if (_filterRedundant == value)
                {
                    return;
                }

                _filterRedundant = value;
                FilteredAnalyseData = null;
            }
        }

        private Dictionary<string, HashSet<string>> _analyseData;

        public Dictionary<string, HashSet<string>> AnalyseData
        {
            set
            {
                _analyseData = value;
                AssetsAssetTreeView = null;
            }
            get => _analyseData ?? (_analyseData = AnalyseAssetBundle());
        }

        private Dictionary<string, HashSet<string>> _filteredAnalyseData;

        public Dictionary<string, HashSet<string>> FilteredAnalyseData
        {
            set
            {
                _filteredAnalyseData = value;
                AssetsAssetTreeView = null;
            }
            get
            {
                if (_filteredAnalyseData == null)
                {
                    if (AnalyseData == null)
                    {
                        return default;
                    }

                    if (FilterRedundant)
                    {
                        _filteredAnalyseData = AnalyseData
                            .Where(item => item.Value.Count > 1)
                            .ToDictionary(item => item.Key, item => item.Value);
                    }
                    else
                    {
                        _filteredAnalyseData = AnalyseData;
                    }
                }

                return _filteredAnalyseData;
            }
        }

        private TreeViewState BundleTreeViewState { get; } = new TreeViewState();

        private TreeView _bundleTreeView;

        private TreeView BundleTreeView
        {
            set => _bundleTreeView = value;
            get
            {
                if (_bundleTreeView == null)
                {
                    if (AnalyseData == null || string.IsNullOrEmpty(SelectAsset))
                    {
                        return default;
                    }

                    if (AnalyseData.TryGetValue(SelectAsset, out var list))
                    {
                        _bundleTreeView =
                            new AssetBundleConfigAssetCorrespondingBundleTreeView(BundleTreeViewState, list);
                        _bundleTreeView.Reload();
                    }
                }

                return _bundleTreeView;
            }
        }

        private string _selectAsset;

        public string SelectAsset
        {
            set
            {
                if (_selectAsset == value)
                {
                    return;
                }

                var selectAsset = !string.IsNullOrEmpty(value) ? value.Replace("\\", "/") : value;
                _selectAsset = selectAsset;
                BundleTreeView = null;
            }
            get => _selectAsset;
        }
    }
}