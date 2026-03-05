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
    public class Text2TextMeshPro
    {
        private static TMP_FontAsset _tmpFont;

        [MenuItem("Assets/ToTMP/Prefab")]
        [MenuItem("GameObject/ToTMP/Prefab")]
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
                Change(asset);
            }
        }

        [MenuItem("Assets/ToTMP/Instance")]
        [MenuItem("GameObject/ToTMP/Instance")]
        public static void ExecuteInstance()
        {
            var selectObj = Selection.activeObject;
            Change(selectObj as GameObject);
            if (selectObj != null)
            {
                EditorUtility.SetDirty(selectObj);
            }
        }

        private static void Change(GameObject root)
        {
            if (!root)
            {
                return;
            }

            var list = root.GetComponentsInChildren<Text>(true);
            foreach (var text in list)
            {
                var target = text.transform;
                var size = text.rectTransform.sizeDelta;
                var strContent = text.text;
                var color = text.color;
                var fontSize = text.fontSize;
                var fontStyle = text.fontStyle;
                var textAnchor = text.alignment;
                var richText = text.supportRichText;
                var horizontalWrapMode = text.horizontalOverflow;
                var verticalWrapMode = text.verticalOverflow;
                var raycastTarget = text.raycastTarget;
                Object.DestroyImmediate(text);

                var textMeshPro = target.gameObject.AddComponent<TextMeshProUGUI>();
                if (_tmpFont == null)
                {
                    //_tmpFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/汉仪正圆85简 SDF");
                    var path = "Assets/TextMesh Pro/Font/HYZhengYuan/汉仪正圆85简 SDF.asset";
                    _tmpFont = AssetDatabase.LoadAssetAtPath(path, typeof(TMP_FontAsset)) as TMP_FontAsset;
                }
                
                textMeshPro.font = _tmpFont;
                textMeshPro.rectTransform.sizeDelta = size;
                textMeshPro.text = strContent;
                textMeshPro.color = color;
                textMeshPro.fontSize = fontSize;
                textMeshPro.fontStyle =
                    fontStyle == FontStyle.BoldAndItalic ? FontStyles.Bold : (FontStyles)fontStyle;
                textMeshPro.alignment = textAnchor switch
                {
                    TextAnchor.UpperLeft => TextAlignmentOptions.TopLeft,
                    TextAnchor.UpperCenter => TextAlignmentOptions.Top,
                    TextAnchor.UpperRight => TextAlignmentOptions.TopRight,
                    TextAnchor.MiddleLeft => TextAlignmentOptions.MidlineLeft,
                    TextAnchor.MiddleCenter => TextAlignmentOptions.Midline,
                    TextAnchor.MiddleRight => TextAlignmentOptions.MidlineRight,
                    TextAnchor.LowerLeft => TextAlignmentOptions.BottomLeft,
                    TextAnchor.LowerCenter => TextAlignmentOptions.Bottom,
                    TextAnchor.LowerRight => TextAlignmentOptions.BottomRight,
                    _ => textMeshPro.alignment
                };

                textMeshPro.richText = richText;
                if (verticalWrapMode == VerticalWrapMode.Overflow)
                {
                    textMeshPro.enableWordWrapping = true;
                    textMeshPro.overflowMode = TextOverflowModes.Overflow;
                }
                else
                {
                    textMeshPro.enableWordWrapping =
                        horizontalWrapMode != HorizontalWrapMode.Overflow;
                }

                textMeshPro.raycastTarget = raycastTarget;
            }
        }

        private static void Change(string path)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            Change(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
    }
}