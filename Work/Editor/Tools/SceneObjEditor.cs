using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.AzureSky;

public class SceneObjEditor
{
    private static readonly TextEditor _textEditor = new TextEditor();

    private static int count = 0; 
    
    [MenuItem("GameObject/输出选中坐标", priority = 23)]
    public static void PrintObjPos()
    {
     
        GameObject[] selectedObjects = Selection.gameObjects;

        // 检查是否选择了任何对象
        if (selectedObjects.Length == 0)
        {
            Debug.Log("没有选中任何对象。");
            return;
        }
        // 按对象名称排序
        selectedObjects = selectedObjects.OrderBy(obj => obj.name).ToArray();

        string pos = "";
        for (int i = 0; i < selectedObjects.Length; i++)
        {
            Vector3 v3 = selectedObjects[i].transform.position;
            pos += $"{v3.x},{v3.z}";

            if (i < selectedObjects.Length - 1)
            {
                pos += ",";
            }
        }
        count++;
        if (selectedObjects.Length == count)
        {
            Debug.Log($"输出坐标组: {pos}");
            count = 0;
        }
    }

    
    
    [MenuItem("GameObject/复制并输出已选择的单个物体坐标信息(世界)", priority = 21)]
    public static void CopyObjectPosAndRot()
    {
        object obj = Selection.activeObject;
        if (obj != null)
        {
            GameObject gameObj = obj as GameObject;
            string str = string.Format("pos={{{0},{1},{2}}},rot={{{3},{4},{5}}}", gameObj.transform.position.x,
                gameObj.transform.position.y, gameObj.transform.position.z, gameObj.transform.eulerAngles.x,
                gameObj.transform.eulerAngles.y, gameObj.transform.eulerAngles.z);
            Debug.Log(str);
            _textEditor.text = str;
            _textEditor.SelectAll();
            _textEditor.Copy();
            Debug.Log("复制成功");
        }
    }
    
    [MenuItem("GameObject/复制并输出已选择的单个物体坐标信息(本地)", priority = 22)]
    public static void CopyObjectLocalPosAndRot()
    {
        object obj = Selection.activeObject;
        if (obj != null)
        {
            GameObject gameObj = obj as GameObject;
            string str = string.Format("pos={{{0},{1},{2}}},rot={{{3},{4},{5}}}", gameObj.transform.localPosition.x,
                gameObj.transform.localPosition.y, gameObj.transform.localPosition.z, gameObj.transform.localEulerAngles.x,
                gameObj.transform.localEulerAngles.y, gameObj.transform.localEulerAngles.z);
            Debug.Log(str);
            _textEditor.text = str;
            _textEditor.SelectAll();
            _textEditor.Copy();
            Debug.Log("复制成功");
        }
    }

    [MenuItem("GameObject/复制UI相对Canvas路径", priority = 20)]
    static void CopyPath()
    {
        Transform tra = Selection.activeTransform;
        if (tra != null)
        {
            string path = GetFullPath(tra);
            string result = path.Replace("Canvas/", "").Replace("CanvasTop/", "");

            for (int i = 0; i < 2; i++)
            {
                var index = result.IndexOf("/",0)+1;
                result = index != -1 ? result[index..]: result;
            }
         
            _textEditor.text = result;
            _textEditor.SelectAll();
            _textEditor.Copy();
            Debug.Log("复制成功");
        }
    }

    [MenuItem("GameObject/美术相关/清理场景所有grassCamTra", priority = 2)]
    public static void ClearGrassRender()
    {
        bool find = false;
        List<GameObject> removeLst = new List<GameObject>();
        do
        {
            GameObject tar = GameObject.Find("grassCamTra");
            if (tar == null) break;
            find = tar != null && removeLst.Contains(tar) == false;
            tar.hideFlags = HideFlags.None;
            removeLst.Add(tar);
            tar.name = tar.name + "_WaitRemove";
        } while (find);

        EditorUtility.DisplayProgressBar("清理grassCamTra", "processing...", 0f);
        for (int i = 0; i < removeLst.Count; i++)
        {
            EditorUtility.DisplayProgressBar("清理grassCamTra", "processing...", i + 1 / removeLst.Count);
            GameObject.DestroyImmediate(removeLst[i]);
        }

        EditorUtility.ClearProgressBar();
    }
    
    [MenuItem("GameObject/美术相关/复制天气组件时间曲线数据", priority = 1)]
    public static void ExportAzureTimeControllerCurve()
    {
        GameObject selectObj = Selection.activeGameObject;
        AzureTimeController timeCtrl = null;
        selectObj.TryGetComponent<AzureTimeController>(out timeCtrl);
        StringBuilder sb = new StringBuilder();
        if (timeCtrl)
        {
            AnimationCurve curve = timeCtrl.dayLengthCurve;
            Keyframe[] keyframeArr = curve.keys;
            for (int i = 0; i < keyframeArr.Length; i++)
            {
                Keyframe frame = keyframeArr[i];
                if (i == keyframeArr.Length - 1)
                    sb.Append($"{frame.time},{frame.value}");
                else
                    sb.Append($"{frame.time},{frame.value}@");
            }
            _textEditor.text = sb.ToString();
            _textEditor.SelectAll();
            _textEditor.Copy();
            Debug.Log("复制成功");
        }
    }

    public static string GetFullPath(Transform tra)
    {
        if (tra == null) return string.Empty;
        if (tra.parent == null) return tra.name;
        return GetFullPath(tra.parent) + "/" + tra.name;
    }
}