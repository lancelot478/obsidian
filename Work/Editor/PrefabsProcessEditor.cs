using System;
using UnityEditor;
using UnityEngine;
public class PrefabsProcessEditor : Editor {

    static void SetGameObjectLayerRecursively(GameObject root, LayerMask mask) {
        if (root != null) {
            root.layer = mask;
            // Debug.Log($"Set {root.name} layer to {LayerMask.LayerToName(mask)}");
            for (int i = 0; i < root.transform.childCount; i++) {
                SetGameObjectLayerRecursively(root.transform.GetChild(i).gameObject, mask);
            }
        }
    }

    static void SetFolderPrefabsLayer(LayerMask mask) {
        UnityEngine.Object[] assets = Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.DeepAssets);
        foreach (UnityEngine.Object asset in assets) {
            if (asset is GameObject) {
                SetGameObjectLayerRecursively(asset as GameObject, mask);
            }
        }

        AssetDatabase.Refresh();
    }

    // [MenuItem("Assets/ProcessAssets/SetFolderLayerTo3D")]
    static void SetFolderLayerTo3D() {
        if (EditorUtility.DisplayDialog("Are You Sure?", "Are You Sure?", "Yes!", "No~")) {
            SetFolderPrefabsLayer(LayerMask.NameToLayer("3D"));
        }
    }

    static void SetFolderAssetBundlesName<T>() {
        if (EditorUtility.DisplayDialog("Are You Sure?", "Are You Sure?", "Yes!", "No~")) {
            UnityEngine.Object[] assets = Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.DeepAssets);
            string               folderName = String.Empty;
            foreach (UnityEngine.Object asset in assets) {
                if (asset is DefaultAsset) {
                    folderName = asset.name;
                    break;
                }
            }

            foreach (UnityEngine.Object asset in assets) {
                if (asset is T) {
                    string        filePath        = AssetDatabase.GetAssetPath(asset);
                    string        assetBundleName = folderName + "/" + asset.name;
                    AssetImporter ai              = AssetImporter.GetAtPath(filePath);
                    ai.assetBundleName = assetBundleName;
                }
            }

            AssetDatabase.Refresh();
        }
    }

    [MenuItem("Assets/ProcessAssets/SetFolderPrefabsName")]
    public static void SetFolderPrefabsAssetBundlesName() {
        SetFolderAssetBundlesName<GameObject>();
    }
}
