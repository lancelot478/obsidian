using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class CheckUIPrefabsEditor : Editor {
    [MenuItem("Assets/UI预设/更改Text组件字体")]
    public static void CheckFXParticlePrefabs() {
        string newFontPath = "Assets/Materials/Font/汉仪正圆85简.ttf";
        string numberFontPath = "Assets/Materials/Font/AstoriaExtraBold.ttf";
        
        Font fontAsset = AssetDatabase.LoadAssetAtPath<Font>(newFontPath);
        Font numberFontAsset = AssetDatabase.LoadAssetAtPath<Font>(numberFontPath);
        
        Object[] selectedObjs = Selection.GetFiltered<Object>(SelectionMode.DeepAssets);

        AssetDatabase.Refresh();
        CheckFontOfTextInUIPrefabs(selectedObjs, fontAsset, numberFontAsset.name);
        AssetDatabase.Refresh();
    }

    private static string getGameObjectPath(Transform transform) {
        var path = transform.name;
        while (transform.parent != null) {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }

        return path;
    }

    private static void CheckFontOfTextInUIPrefabs(Object[] objs, Font newFont, string numberFontName) {
        string newFontName = newFont.name;
        
        foreach (Object obj in objs) {
            string prefabPath = AssetDatabase.GetAssetPath(obj);
            if (!File.Exists(prefabPath)) {
                continue;
            }

            try {
                GameObject prefab = PrefabUtility.LoadPrefabContents(prefabPath);
                if (prefab != null) {
                    Text[] texts = prefab.GetComponentsInChildren<Text>(true);

                    bool needSavePrefab = false;
                    foreach (Text text in texts) {
                        string currentTextFontName = text.font.name;
                        if (currentTextFontName != newFontName && currentTextFontName != numberFontName) {
                            string oldFontName = currentTextFontName;
                            text.font = newFont;
                            needSavePrefab = true;

                            Debug.Log($"{getGameObjectPath(text.transform)}的字体已从:\n{oldFontName}\n更改为\n{newFont.name}");
                        }
                    }

                    if (needSavePrefab) {
                        PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
                    }
                }
            }
            catch (Exception e) {
                Debug.LogError(e);
                throw;
            }
        }
    }
}
