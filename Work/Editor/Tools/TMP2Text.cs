using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SAGA.Editor
{
    public class TMP2Text
    {
        private static Font _textFont;

        [MenuItem("Assets/ToText/Prefab")]
        [MenuItem("GameObject/ToText/Prefab")]
        public static void ExecutePrefab()
        {
            var assetGUIDs = Selection.assetGUIDs;

            var assetPaths = new string[assetGUIDs.Length];

            for (var i = 0; i < assetGUIDs.Length; i++)
            {
                assetPaths[i] = AssetDatabase.GUIDToAssetPath(assetGUIDs[i]);
            }

            var allAssets = new HashSet<string>();
            assetPaths.ToList().ForEach(path =>
            {
                if (!path.Contains("Prefabs/Plane"))
                {
                    Debug.LogError($"不是界面相关预制体：{path}");
                    return;
                }

                if (Directory.Exists(path))
                {
                    var allFiles = Directory.GetFiles(path, "*.prefab", SearchOption.AllDirectories);
                    allFiles.ToList().ForEach(item => allAssets.Add(item));
                }
                else
                {
                    allAssets.Add(path.Replace("\\", "/"));
                }
            });

            foreach (var asset in allAssets)
            {
                TMPChange(asset);
            }
        }

        [MenuItem("Assets/ToText/Instance")]
        [MenuItem("GameObject/ToText/Instance")]
        public static void ExecuteInstance()
        {
            var selectObj = Selection.activeObject;
            TMPChange(selectObj as GameObject);
            if (selectObj != null)
            {
                EditorUtility.SetDirty(selectObj);
            }
        }

        private static void TMPChange(GameObject root)
        {
            if (!root)
            {
                return;
            }

            var list = root.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var text in list)
            {
                var target = text.transform;
                var size = text.rectTransform.sizeDelta;
                var strContent = text.text;
                var color = text.color;
                var fontSize = text.fontSize;
                var fontStyle = text.fontStyle;
                var textAnchor = text.alignment;
                var raycastTarget = text.raycastTarget;

                int childs = text.transform.childCount;
                for (int i = childs - 1; i >= 0; i--)
                {
                    GameObject.DestroyImmediate(text.transform.GetChild(i).gameObject);
                }
                Object.DestroyImmediate(text);

                var toText = target.gameObject.AddComponent<Text>();
                if (_textFont == null)
                {
                    var path = "Assets/TextMesh Pro/Font/HYZhengYuan/HYZhengYuan-85S.ttf";
                    _textFont = AssetDatabase.LoadAssetAtPath(path, typeof(Font)) as Font;
                }

                toText.font = _textFont;
                toText.rectTransform.sizeDelta = size;
                toText.text = strContent;
                toText.color = color;
                toText.fontSize = (int)fontSize;
                toText.alignment = textAnchor switch
                {
                    TextAlignmentOptions.TopLeft => TextAnchor.UpperLeft ,
                    TextAlignmentOptions.Top => TextAnchor.UpperCenter,
                    TextAlignmentOptions.TopRight => TextAnchor.UpperRight ,
                    TextAlignmentOptions.MidlineLeft => TextAnchor.MiddleLeft  ,
                    TextAlignmentOptions.Midline => TextAnchor.MiddleCenter  ,
                    TextAlignmentOptions.MidlineRight => TextAnchor.MiddleRight  ,
                    TextAlignmentOptions.BottomLeft => TextAnchor.LowerLeft  ,
                    TextAlignmentOptions.Bottom => TextAnchor.LowerCenter  ,
                    TextAlignmentOptions.BottomRight => TextAnchor.LowerRight  ,
                    _ => toText.alignment
                };
                toText.supportRichText = true;
                toText.raycastTarget = raycastTarget;
            }
        }

        private static void TMPChange(string path)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            TMPChange(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
    }
}