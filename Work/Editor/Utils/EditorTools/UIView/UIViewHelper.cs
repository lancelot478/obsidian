using System;
using System.Linq;
using DremEditor.Sirenix.Utilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using EditorUtility = UnityEditor.EditorUtility;
using Object = UnityEngine.Object;

namespace SAGA.Editor
{
    public static class UIViewHelper
    {
        [MenuItem("GameObject/View/Copy", priority = 0)]
        public static void Copy()
        {
            var tra = Selection.activeTransform;
            if (tra == null)
            {
                return;
            }

            var path = GetFullPath(tra);
            GUIUtility.systemCopyBuffer = path;
        }

        [MenuItem("GameObject/View/CopyTransform", priority = 0)]
        public static void CopyTransform()
        {
            CopyPath(typeof(Transform));
        }

        [MenuItem("GameObject/View/CopyText", priority = 0)]
        public static void CopyText()
        {
            CopyPath(typeof(Text));
        }

        [MenuItem("GameObject/View/CopyImage", priority = 0)]
        public static void CopyImage()
        {
            CopyPath(typeof(Image));
        }

        [MenuItem("GameObject/View/CopyButton", priority = 0)]
        public static void CopyButton()
        {
            CopyPath(typeof(Button), true);
        }

        [MenuItem("GameObject/View/CopyToggle", priority = 0)]
        public static void CopyToggle()
        {
            CopyPath(typeof(Toggle), true);
        }

        [MenuItem("GameObject/View/CopyInputField", priority = 0)]
        public static void CopyInputField()
        {
            CopyPath(typeof(InputField), true);
        }

        [MenuItem("GameObject/View/CopySlider", priority = 0)]
        public static void CopySlider()
        {
            CopyPath(typeof(Slider), true);
        }

        [MenuItem("GameObject/View/CopyScrollBar", priority = 0)]
        public static void CopyScrollBar()
        {
            CopyPath(typeof(Scrollbar), true);
        }

        [MenuItem("GameObject/View/_CopyPosition", priority = 100)]
        public static void CopyPosition()
        {
            var objects = Selection.objects;
            if (objects == null)
            {
                return;
            }

            var enumerable = objects.ToArray().Select(item => ((RectTransform)((GameObject)item).transform).anchoredPosition)
                .Select(item => new Position(item.x, item.y, 0));
            var obj = new PositionData() { isCp = true, data = enumerable.ToArray() };
            var val = JsonUtility.ToJson(obj);
            GUIUtility.systemCopyBuffer = val;
        }


        [MenuItem("GameObject/View/_PastePosition", true, priority = 100)]
        public static bool CheckPastePosition()
        {
            var clipBoard = GUIUtility.systemCopyBuffer;
            if (string.IsNullOrEmpty(clipBoard))
            {
                return false;
            }

            PositionData data = null;
            try
            {
                data = JsonUtility.FromJson<PositionData>(clipBoard);
                return data is { isCp: true };
            }
            catch
            {
                return false;
            }
        }

        [MenuItem("GameObject/View/_PastePosition", priority = 100)]
        public static void PastePosition()
        {
            var clipBoard = GUIUtility.systemCopyBuffer;
            var data = JsonUtility.FromJson<PositionData>(clipBoard);
            var objects = Selection.objects;
            for (int i = 0; i < objects.Length; i++)
            {
                if (i >= data.data.Length)
                {
                    return;
                }

                var pos = data.data[i];
                var obj = objects[i] as GameObject;
                if (obj != null)
                {
                    ((RectTransform)obj.transform).anchoredPosition = new Vector3(pos.x, pos.y, pos.z);
                }
            }
        }

        private static void CopyPath(Type type, bool callback = false)
        {
            var tra = Selection.activeTransform;
            if (tra == null)
            {
                return;
            }

            GetOrAddComponent(tra, type);
            var path = GetFullPath(tra);
            var typeName = type != null ? $" Component = \"{type.Name}\", " : string.Empty;
            var callbackStr = callback ? "Callback = function()\n\n end" : "";
            var text = $"{tra.name} = {{ Path = \"{path}\", {typeName}{callbackStr} }},";
            GUIUtility.systemCopyBuffer = text;
        }

        private static void GetOrAddComponent(Component obj, Type type)
        {
            if (obj == null || type == null)
            {
                return;
            }

            var comp = obj.GetComponent(type);
            if (comp == null)
            {
                obj.gameObject.AddComponent(type);
                EditorUtility.SetDirty(obj);
            }
        }

        private static string GetFullPath(Transform tra)
        {
            if (tra == null)
            {
                return string.Empty;
            }

            if (tra.parent == null || tra.GetComponent<Canvas>() != null)
            {
                return string.Empty;
            }

            var parent = GetFullPath(tra.parent);
            var fix = string.IsNullOrEmpty(parent) ? "" : $"{parent}/";
            return fix + tra.name;
        }
    }
}

[Serializable]
class Position
{
    public Position(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public float x;
    public float y;
    public float z;
}

[Serializable]
class PositionData
{
    public bool isCp;
    public Position[] data;
}