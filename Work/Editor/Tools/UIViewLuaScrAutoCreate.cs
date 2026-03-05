using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class UIViewLuaScrAutoCreate : MonoBehaviour
{
    static string baseLuaSrcName = Application.dataPath + "/Editor/Tools/BasePlaneSrc.txt";
    [MenuItem("Assets/Lua/Auto_Create_LuaViewSrc")]
    public static void CreatLuaViewSrc( MenuCommand menucommond)
    {
        if (Selection.objects.Length == 1)
        {
            Debug.Log("创建脚本" + Selection.objects[0].name);
            GameObject traget = Selection.objects[0] as GameObject;
            Dictionary<string, UIComponent> cmts = new Dictionary<string, UIComponent>();  
            string srcPath = Application.dataPath + "/Scripts/LuaScript/Plane/" + traget.name + ".lua";
            GetChildComponents(traget.transform, null,ref cmts);
            List<UIComponent> listCmts = new List<UIComponent>();
            foreach(var cmt in cmts.Values)
            {
                listCmts.Add(cmt);
            }

            Debug.Log("创建目标 ： " + traget.name + "  绑定components 数量： " + listCmts.Count);
            string readpath = "";
            if (File.Exists(srcPath))
            {
                readpath = srcPath;
            }
            else
            {
                readpath = baseLuaSrcName;
            }
            string[] lines = File.ReadAllLines(readpath);
            int insertIndex_start = 0;
            int insertIndex_end = 0;
            for (int index = 0; index < lines.Length; index++)
            {
                if (lines[index].Contains("Auto Create Start"))
                {
                    insertIndex_start = index;
                }
                if (lines[index].Contains("Auto Create End"))
                {
                    insertIndex_end = index;
                    break;
                }
            }
            if (insertIndex_end <= insertIndex_start)
            {
                Debug.LogError("Lua源文件错误，请添加-Auto Create Start-和-Auto Create End-标志区间");
                return;
            }
            int oldcmtNum = insertIndex_end - insertIndex_start - 1;
            int newcmtNum = listCmts.Count;
            int newLength = lines.Length + (newcmtNum - oldcmtNum);
            int insertIndex_new_end = insertIndex_end + newcmtNum - oldcmtNum;
            string[] newLines = new string[newLength];
            int cmtIndex = 0;
            for (int index = 0; index < newLines.Length; index++)
            {
                string linestr = "";
                if (index >= insertIndex_new_end)
                {
                    linestr = lines[index - (newcmtNum - oldcmtNum)];
                }
                else if (index <= insertIndex_start)
                {
                    linestr = lines[index];
                }
                else
                {
                    UIComponent UIcmt = listCmts[cmtIndex];
                    linestr = "self." + listCmts[cmtIndex].Name + " = GlobalFun.GetType(self.tra,\"" + listCmts[cmtIndex].Path + "\",\"" + listCmts[cmtIndex].Cmt + "\")";
                    cmtIndex++;
                }
                if (linestr.Contains("BasePlaneSrc"))
                {
                    linestr = linestr.Replace("BasePlaneSrc", traget.name);
                }
                newLines[index] = linestr;
                Debug.Log(linestr);
            }
            File.WriteAllLines(srcPath, newLines);
            AssetDatabase.Refresh();
        }
        else
        {
            Debug.Log("选中界面prefab资源");
        }
    }
    static void GetChildComponents(Transform transform,string parentPath,ref Dictionary<string,UIComponent> cmts)
    {
        if (transform.childCount <= 0)
            return;
        string tempPath = parentPath;
        for (int i = 0; i < transform.childCount; i++)
        {
            string path = "";
            if (parentPath == null)
            {
                path = "";
            }
            else
            {
                path = tempPath + "/";
            }
            GameObject traget = transform.GetChild(i).gameObject;
            string objName = traget.name;
            var tmpTextAttached = objName.Contains("TMP_Text");
            int index = objName.LastIndexOf("_");
            if (index > 0)
            {
                string componentName = objName.Substring(index + 1);
                if (traget.GetComponent(componentName) != null ||
                    (tmpTextAttached && traget.GetComponent<TMPro.TMP_Text>()))
                {
                    if (tmpTextAttached) componentName = "TMPro.TMP_Text";
                    UIComponent cmt = new UIComponent(objName, componentName, path + objName);
                    if (cmts.ContainsKey(objName))
                    {
                        int bitIndex = 1;
                        while (cmts.ContainsKey(objName + "_" + bitIndex.ToString()))
                        {
                            bitIndex++;
                        }

                        cmt.Name = objName + "_" + bitIndex.ToString();
                    }

                    cmts.Add(cmt.Name, cmt);
                }
            }
            GetChildComponents(transform.GetChild(i), path + objName, ref cmts);
        }
    }
}

//[System.Serializable]
public struct UIComponent
{
    public string Name;
    public string Cmt;
    public string Path;
    public UIComponent(string name, string cmt,string path)
    {
        Name = name;
        Cmt = cmt;
        Path = path;
        Debug.Log(name+ " " + cmt + " " + path);
    }
}