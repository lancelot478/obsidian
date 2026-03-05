using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using FashionType = ScriptableAvatarFashionData.FashionType;
using AvatarFashionColorSetSexConfig = ScriptableAvatarFashionData.AvatarFashionColorSetSexConfig;
using AvatarFashionColorSetTypeConfig = ScriptableAvatarFashionData.AvatarFashionColorSetTypeConfig;
using AvatarFashionColorSetConfig = ScriptableAvatarFashionData.AvatarFashionColorSetConfig;

[CustomEditor(typeof(ScriptableAvatarFashionData))]
public class AvatarFasihonDataInspectorEditor : Editor
{
    private const string LuaFileName = "Create_AvatarFashionConfig.lua";
    private string LuaFilePath;
    private ushort updateFrameInterval = 10;

    private ushort nextUpdateFrame = 0;

    //当前选中的时装类型
    private FashionType currSelectFashionType;
    private bool isDataSearched = false;
    private bool isOldDataViewMode = false;
    private int opFashionID = 0;
    [SerializeField] private SerializedProperty serializedProperty;

    private int currPage = 1;
    private int maxPage = 1;
    private int onePageCnt = 10;
    private ScriptableAvatarFashionData _data;

    private void OnEnable()
    {
        _data = base.serializedObject.targetObject as ScriptableAvatarFashionData;
        _data.editorFashionColorSet = null;
        _data.editorOneTypeFashionColorPageDic = null;
        serializedObject.Update();
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.BeginVertical();
        if (GUILayout.Button("[推荐]一键应用"))
        {
            Debug.Log(Application.dataPath);
            OnApplyAvatarFashionColorSetData();
        }

        if (GUILayout.Button("只导出Lua脚本"))
        {
            var data = serializedObject.targetObject as ScriptableAvatarFashionData;
            CreateLuaFile(data);
        }

        if (GUILayout.Button("更新所有AssetBundleName+导出Lua脚本"))
        {
            var data = serializedObject.targetObject as ScriptableAvatarFashionData;
            SetScriptableDataBundleName();
            SetAssetBundleName(data);
            SetFXAssetBundleName(data);
            CreateLuaFile(data);
        }

        if (GUILayout.Button("输出使用同一材质多Mask染色的染色ID"))
        {
            // Dictionary<string,string> oneSexMatConfLst = new Dictionary<string,string>();
            List<string> oneSexMatConfLst = new List<string>();
            var data = serializedObject.targetObject as ScriptableAvatarFashionData;
            foreach (var eachFashionTypeConf in data.FashionColorSetLst)
            {
                foreach (var oneColorSet in eachFashionTypeConf.FashionColorSetLst)
                {
                    foreach (var oneSexColorSet in oneColorSet.SexConfigLst)
                    {
                        if (oneSexColorSet.IsUseMaskColor)
                        {
                            oneSexMatConfLst.Clear();
                            foreach (var matConf in oneSexColorSet.MaskMaterialConfigLst)
                            {
                                if (matConf.MaskMaterial)
                                {
                                    // var key = matConf.MaskMaterial.name + matConf.Color1.ToString() +
                                    //           matConf.Color2.ToString() + matConf.Color3 + matConf.Color4;
                                    // if (!oneSexMatConfLst.ContainsKey(matConf.MaskMaterial.name))
                                    //     oneSexMatConfLst.Add(matConf.MaskMaterial.name, key);
                                    // else
                                    // {
                                    //     if(oneSexMatConfLst[matConf.MaskMaterial.name]!=key)
                                    //         Debug.Log($"染色ID:{oneColorSet.AssetID} 包含同一材质多Mask染色 请检查 {key}");
                                    // }
                                    if (oneSexMatConfLst.Contains(matConf.MaskMaterial.name))
                                    {
                                        Debug.Log($"染色ID:{oneColorSet.AssetID} 包含同一材质多Mask染色 请检查是否需要手动指定Index ");
                                    }
                                    else
                                    {
                                        oneSexMatConfLst.Add(matConf.MaskMaterial.name);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        EditorGUILayout.EndVertical();
        GUILayout.Space(50);
        isOldDataViewMode = EditorGUILayout.Toggle("使用旧版数据查看模式", isOldDataViewMode);
        if (isOldDataViewMode)
        {
            base.OnInspectorGUI();
        }
        else
        {
            onePageCnt = EditorGUILayout.IntField("每一页最大的数据量", onePageCnt);
            currSelectFashionType = (FashionType)EditorGUILayout.EnumPopup("要查询的时装类型", currSelectFashionType);
            if (GUILayout.Button("查看当前类型染色条目", GUILayout.Width(150), GUILayout.Height(50)))
            {
                InitPageData();
            }

            GUILayout.Space(50);
            GUILayout.Label("↓↓↓数据区↓↓↓");
            if (isDataSearched)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("当前分页", GUILayout.Width(80));
                GUILayout.Label(currPage.ToString() + "/" + maxPage);
                if (GUILayout.Button("上一页", GUILayout.Width(150), GUILayout.Height(50)))
                {
                    if (currPage != 1)
                    {
                        currPage--;
                        SelectOneDataPage(currPage);
                    }
                }

                if (GUILayout.Button("下一页", GUILayout.Width(150), GUILayout.Height(50)))
                {
                    if (currPage != maxPage)
                    {
                        currPage++;
                        SelectOneDataPage(currPage);
                    }
                }

                EditorGUILayout.EndHorizontal();
                opFashionID = EditorGUILayout.IntField("要操作的染色ID", opFashionID);
                GUILayout.Space(20);
                if (GUILayout.Button("新增数据", GUILayout.Width(150), GUILayout.Height(50)))
                {
                    AddDataToCurrTypeFashionColorSet();
                    InitPageData();
                    SelectOneDataPage(currPage);
                }

                if (GUILayout.Button("删除指定ID", GUILayout.Width(150), GUILayout.Height(50)))
                {
                    DelDataToCurrTypeFashionColorSet(opFashionID);
                    InitPageData();
                    SelectOneDataPage(1);
                }

                serializedProperty = base.serializedObject.FindProperty("editorOneTypeFashionColorPageList");
                EditorGUILayout.PropertyField(serializedProperty);
            }
        }

        // 应用对序列化对象所做的所有更改
        serializedObject.ApplyModifiedProperties();
    }

    private void SelectOneDataPage(int page)
    {
        _data.editorOneTypeFashionColorPageList = _data.editorOneTypeFashionColorPageDic[page];
        serializedObject.Update();
    }

    private void InitPageData()
    {
        ScriptableAvatarFashionData _data = base.serializedObject.targetObject as ScriptableAvatarFashionData;
        currPage = 1;
        if (_data.FashionColorSetLst != null)
        {
            foreach (var oneColorSet in _data.FashionColorSetLst)
            {
                if (oneColorSet.FashionType == currSelectFashionType)
                {
                    _data.editorFashionColorSet = oneColorSet;
                    _data.editorOneTypeFashionColorPageDic =
                        new Dictionary<int, List<ScriptableAvatarFashionData.AvatarFashionColorSetConfig>>();
                    var page = 1;
                    foreach (var oneFashionColorSet in _data.editorFashionColorSet.FashionColorSetLst)
                    {
                      
                            if (_data.editorOneTypeFashionColorPageDic.ContainsKey(page) &&
                                _data.editorOneTypeFashionColorPageDic[page].Count == onePageCnt)
                            {
                                page++;
                            }

                        if (_data.editorOneTypeFashionColorPageDic.ContainsKey(page) == false)
                        {
                            _data.editorOneTypeFashionColorPageDic[page] =
                                new List<ScriptableAvatarFashionData.AvatarFashionColorSetConfig>();
                        }

                        _data.editorOneTypeFashionColorPageDic[page].Add(oneFashionColorSet);
                    }

                    maxPage = page;
                    break;
                }
            }
        }

        if (_data.editorOneTypeFashionColorPageDic.ContainsKey(1))
        {
            _data.editorOneTypeFashionColorPageList = _data.editorOneTypeFashionColorPageDic[1];
        }

        serializedObject.Update();
        isDataSearched = true;
    }

    private void AddDataToCurrTypeFashionColorSet()
    {
        foreach (var oneColorSet in _data.FashionColorSetLst)
        {
            if (oneColorSet.FashionType == currSelectFashionType)
            {
                if (oneColorSet.FashionColorSetLst.Count == 0)
                    oneColorSet.FashionColorSetLst.Add(new AvatarFashionColorSetConfig());
                else
                    oneColorSet.FashionColorSetLst.Add(
                        DeepCopy(oneColorSet.FashionColorSetLst[oneColorSet.FashionColorSetLst.Count - 1]));
                serializedObject.Update();
                Debug.Log("新增一条染色数据"+ oneColorSet.FashionColorSetLst.Count);
                break;
            }
        }
    }
    public static T DeepCopy<T>(T original)
    {
        // 将原始对象序列化为JSON字符串
        string serialized = JsonUtility.ToJson(original);
        // 从JSON字符串反序列化出新的对象实例
        T copy = JsonUtility.FromJson<T>(serialized);
        return copy;
    }
    private void DelDataToCurrTypeFashionColorSet(int id)
    {
        foreach (var oneColorSet in _data.FashionColorSetLst)
        {
            if (oneColorSet.FashionType == currSelectFashionType)
            {
                if (oneColorSet.FashionColorSetLst != null)
                { 
                    for (int i = oneColorSet.FashionColorSetLst.Count - 1; i >= 0; i--)
                    {
                        if (oneColorSet.FashionColorSetLst[i].AssetID == id.ToString())
                        {
                            oneColorSet.FashionColorSetLst.RemoveAt(i);
                            serializedObject.Update();
                            Debug.Log("删除指定id染色数据成功:" + id);
                            break;
                        }
                    }
                }

                break;
            }
        }
    }


    private void OnApplyAvatarFashionColorSetData()
    {
        var data = serializedObject.targetObject as ScriptableAvatarFashionData;
        ModifyFileName();
        SetScriptableDataBundleName();
        SetAssetBundleName(data);
        SetFXAssetBundleName(data);
        CreateLuaFile(data);
    }

    private void SetScriptableDataBundleName()
    {
        var data = serializedObject.targetObject as ScriptableAvatarFashionData;
        //each fashion type
        foreach (var eachFashionTypeConfigEntity in data.FashionColorSetLst)
        {
            //each fashion 
            foreach (var oneFashionConfEntity in eachFashionTypeConfigEntity.FashionColorSetLst)
            {
                var colorIndex = oneFashionConfEntity.ColorSetIndex;
                Debug.Log("colorIndex" + " " + colorIndex);
                //each sex
                var enumerable = from VAR in oneFashionConfEntity.SexConfigLst
                    where VAR.Sex == ScriptableAvatarFashionData.SexType.Female
                    select VAR;

                ProcessSetScriptableDataBundleName(enumerable, colorIndex);

                enumerable = from VAR in oneFashionConfEntity.SexConfigLst
                    where VAR.Sex == ScriptableAvatarFashionData.SexType.Male
                    select VAR;

                ProcessSetScriptableDataBundleName(enumerable, colorIndex);

                enumerable = from VAR in oneFashionConfEntity.SexConfigLst
                    where VAR.Sex == ScriptableAvatarFashionData.SexType.UnLimit
                    select VAR;

                ProcessSetScriptableDataBundleName(enumerable, colorIndex);
            }
        }
    }

    private void ProcessSetScriptableDataBundleName(IEnumerable<AvatarFashionColorSetSexConfig> enumerable,
        int colorIndex)
    {
        foreach (var colorSetSexEntity in enumerable)
        {
            colorSetSexEntity.BundleName = $"modelmaterial/{colorSetSexEntity.Obj.gameObject.name}_c{colorIndex}";
        }
    }

    private void SetAssetBundleName(ScriptableAvatarFashionData data)
    {
        var unUsedMatLst = new List<string>();
        //each fashion type
        foreach (var eachFashionTypeConfigEntity in data.FashionColorSetLst)
        {
            //each fashion 
            foreach (var oneFashionConfEntity in eachFashionTypeConfigEntity.FashionColorSetLst)
            {
                var femalePath = "";
                var malePath = "";
                var unLimitSexPath = "";
                var enumerable = from VAR in oneFashionConfEntity.SexConfigLst
                    where VAR.Sex == ScriptableAvatarFashionData.SexType.Female
                    select VAR;
                foreach (var et in enumerable)
                {
                    femalePath = AssetDatabase.GetAssetPath(et.Obj);

                    if (!string.IsNullOrEmpty(femalePath))
                    {
                        femalePath = femalePath.Substring(0, femalePath.LastIndexOf('/'));
                        RoleEditor.SetOneAvatarFolderColorBundles(null, femalePath, unUsedMatLst);
                    }
                }

                enumerable = from VAR in oneFashionConfEntity.SexConfigLst
                    where VAR.Sex == ScriptableAvatarFashionData.SexType.Male
                    select VAR;
                foreach (var et in enumerable)
                {
                    malePath = AssetDatabase.GetAssetPath(et.Obj);
                    if (!string.IsNullOrEmpty(malePath))
                    {
                        malePath = malePath.Substring(0, malePath.LastIndexOf('/'));
                        RoleEditor.SetOneAvatarFolderColorBundles(null, malePath, unUsedMatLst);
                    }
                }

                enumerable = from VAR in oneFashionConfEntity.SexConfigLst
                    where VAR.Sex == ScriptableAvatarFashionData.SexType.UnLimit
                    select VAR;
                foreach (var et in enumerable)
                {
                    unLimitSexPath = AssetDatabase.GetAssetPath(et.Obj);
                    if (!string.IsNullOrEmpty(unLimitSexPath))
                    {
                        unLimitSexPath = unLimitSexPath.Substring(0, unLimitSexPath.LastIndexOf('/'));
                        RoleEditor.SetOneAvatarFolderColorBundles(null, unLimitSexPath, unUsedMatLst);
                    }
                }
            }
        }
    }

    private void SetFXAssetBundleName(ScriptableAvatarFashionData data)
    {
        //each fashion type
        foreach (var eachFashionTypeConfigEntity in data.FashionColorSetLst)
        {
            //each fashion 
            foreach (var oneFashionConfEntity in eachFashionTypeConfigEntity.FashionColorSetLst)
            {
                foreach (var oneSexConfEt in oneFashionConfEntity.SexConfigLst)
                {
                    if (oneSexConfEt.HasFX && oneSexConfEt.FXObj != null)
                    {
                        var assetPath = AssetDatabase.GetAssetPath(oneSexConfEt.FXObj);
                        AssetImporter ai = AssetImporter.GetAtPath(assetPath);
                        ai.assetBundleName = $"fx/{oneSexConfEt.FXObj.name}";
                        oneSexConfEt.FXBundleName = ai.assetBundleName;
                    }
                }
            }
        }
    }

    //需要修改文件名的资源文件
    private Dictionary<string, string> modifyFileNameDict;

    //每个资源文件夹下Mask材质当前的Index序号
    private Dictionary<string, int> maskMatCurrIndexEachFolderDict;

    private void ModifyFileName()
    {
        modifyFileNameDict = new Dictionary<string, string>();
        var data = serializedObject.targetObject as ScriptableAvatarFashionData;
        maskMatCurrIndexEachFolderDict = new Dictionary<string, int>();

        //each fashion type
        foreach (var eachFashionTypeConfigEntity in data.FashionColorSetLst)
        {
            Dictionary<Material, string> globalMaterials = new Dictionary<Material, string>();
            Dictionary<Material, int> matRefDict = new Dictionary<Material, int>();
            //each female fashion 
            //each fashion 
            foreach (var oneFashionConfEntity in eachFashionTypeConfigEntity.FashionColorSetLst)
            {
                var colorSetIndex = oneFashionConfEntity.ColorSetIndex;
                //each sex
                var enumerable = from VAR in oneFashionConfEntity.SexConfigLst
                    where VAR.Sex == ScriptableAvatarFashionData.SexType.Female
                    select VAR;

                ProcessModifyName(enumerable, globalMaterials, matRefDict, colorSetIndex);
            }

            globalMaterials = new Dictionary<Material, string>();
            matRefDict = new Dictionary<Material, int>();
            //each male fashion 
            foreach (var oneFashionConfEntity in eachFashionTypeConfigEntity.FashionColorSetLst)
            {
                var colorSetIndex = oneFashionConfEntity.ColorSetIndex;
                //each sex
                var enumerable = from VAR in oneFashionConfEntity.SexConfigLst
                    where VAR.Sex == ScriptableAvatarFashionData.SexType.Male
                    select VAR;

                ProcessModifyName(enumerable, globalMaterials, matRefDict, colorSetIndex);
            }

            globalMaterials = new Dictionary<Material, string>();
            matRefDict = new Dictionary<Material, int>();
            //each male fashion 
            foreach (var oneFashionConfEntity in eachFashionTypeConfigEntity.FashionColorSetLst)
            {
                var colorSetIndex = oneFashionConfEntity.ColorSetIndex;
                //each sex
                var enumerable = from VAR in oneFashionConfEntity.SexConfigLst
                    where VAR.Sex == ScriptableAvatarFashionData.SexType.UnLimit
                    select VAR;

                ProcessModifyName(enumerable, globalMaterials, matRefDict, colorSetIndex);
            }
        }

        //冲突检测
        var renameConflictIndex = 0;
        var hasConflict = false;
        foreach (var pair in modifyFileNameDict)
        {
            Debug.Log("modifyFileNameDict" + pair.Key + " " + pair.Value);
            var renameRes = AssetDatabase.RenameAsset(pair.Key, pair.Value);
            if (renameRes != string.Empty)
            {
                if (hasConflict == false) hasConflict = true;
                var objName = pair.Key.Remove(pair.Key.LastIndexOf('/'));
                objName = objName.Substring(objName.LastIndexOf('/') + 1);

                var srcMatName = pair.Key.Substring(pair.Key.LastIndexOf('/') + 1);
                srcMatName = srcMatName.Remove(srcMatName.LastIndexOf('.'));
                var targetMatPath = pair.Key.Replace(srcMatName, pair.Value);


                Debug.Log(
                    $"资源重命名失败,存在冲突资源源文件{pair.Key}  目标文件 {pair.Value}  ObjName:{objName}   targetMatPath:{targetMatPath}");
                AssetDatabase.RenameAsset(targetMatPath, $"Conflict_{objName}_{renameConflictIndex++}");
            }
        }

        serializedObject.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        if (hasConflict)
        {
            EditorUtility.DisplayDialog("Tool Tip", "染色资源应用过程中存在冲突资源,冲突资源前缀已重命名为Conflict_  请重新应用一次", "确定");
        }
    }

    private void ProcessModifyName(IEnumerable<ScriptableAvatarFashionData.AvatarFashionColorSetSexConfig> enumerable,
        Dictionary<Material, string> globalMaterials, Dictionary<Material, int> matRefDict, int colorSetIndex)
    {
        //deal global material
        foreach (var colorSetSexEntity in enumerable)
        {
            foreach (var mat in colorSetSexEntity.MatNameIndexArr)
            {
                if (mat == null)
                {
                    Debug.LogError($"Material 为空  objName{colorSetSexEntity.Obj.name}   ");
                }

                if (mat.name.Contains("Bodybase")) continue;
                // var isMask = false;
                // foreach (var VARIABLE in colorSetSexEntity.MaskMaterialConfigLst)
                // {
                //     if (VARIABLE.MaskMaterial == mat)
                //     {
                //         isMask = true;
                //         break;
                //     }
                // }
                // if(isMask) continue;
                Debug.Log(matRefDict.ContainsKey(mat) + " " + mat);
                if (matRefDict.ContainsKey(mat))
                {
                    Debug.Log("####" + " " + matRefDict[mat] + "  " + colorSetIndex);
                }

                if (!matRefDict.ContainsKey(mat))
                {
                    matRefDict[mat] = colorSetIndex;
                }
                else
                {
                    if (matRefDict[mat] != colorSetIndex && !globalMaterials.ContainsKey(mat))
                    {
                        //global
                        globalMaterials.Add(mat, colorSetSexEntity.Obj.gameObject.name);
                    }
                }
            }
        }

        var idx = 0;
        foreach (var pair in globalMaterials)
        {
            Debug.Log("Global Asset" + pair.Key);
            idx++;
            var path = AssetDatabase.GetAssetPath(pair.Key);
            Debug.Log("Global Asset2" + " " + path + " " + idx);
            modifyFileNameDict[path] = $"{pair.Value}_global_{idx}";
        }


        //  var index = 0;
        List<Material> recordLst;
        //normal material
        foreach (var colorSetSexEntity in enumerable)
        {
            var matIndex = 1;
            recordLst = new List<Material>();
            // index++;
            foreach (var mat in colorSetSexEntity.MatNameIndexArr)
            {
                Debug.Log($"{mat}");
                if (recordLst.Contains(mat) || mat.name.Contains("Bodybase") ||
                    modifyFileNameDict.ContainsKey(AssetDatabase.GetAssetPath(mat))) continue;
                recordLst.Add(mat);
                var oldPath = AssetDatabase.GetAssetPath(mat);
                modifyFileNameDict[oldPath] = $"{colorSetSexEntity.Obj.gameObject.name}_c{colorSetIndex}_{matIndex}";
                matIndex++;
            }
        }

        //mask material
        // foreach (var colorSetSexEntity in enumerable)
        // {
        //     recordLst = new List<Material>();
        //     foreach (var mat in colorSetSexEntity.MaskMaterialConfigLst)
        //     {
        //         Debug.Log(mat+$"{recordLst.Contains(mat.MaskMaterial)} ");
        //         if (recordLst.Contains(mat.MaskMaterial) || mat.MaskMaterial.name.Contains("_global_")||mat.MaskMaterial.name.Contains("Bodybase")) continue;
        //         var success= maskMatCurrIndexEachFolderDict.TryAdd(colorSetSexEntity.Obj.gameObject.name, 1);
        //         if (!success)
        //         {
        //             maskMatCurrIndexEachFolderDict[colorSetSexEntity.Obj.gameObject.name]++;
        //         }
        //
        //         var matIndex = maskMatCurrIndexEachFolderDict[colorSetSexEntity.Obj.gameObject.name];
        //         recordLst.Add(mat.MaskMaterial);
        //         var oldPath = AssetDatabase.GetAssetPath(mat.MaskMaterial);
        //         modifyFileNameDict[oldPath] = $"{colorSetSexEntity.Obj.gameObject.name}_mask_{matIndex}";
        //     }
        // }
    }

    //Create LuaScript Src
    private void CreateLuaFile(ScriptableAvatarFashionData dataEntity)
    {
        LuaFilePath = Application.dataPath + "/Scripts/LuaScript/Configs/AvatarFashion/Create/" + LuaFileName;
        if (File.Exists(LuaFilePath))
        {
            File.Delete(LuaFilePath);
        }

        if (!File.Exists(LuaFilePath))
        {
            var fs = File.Open(LuaFilePath, FileMode.OpenOrCreate);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("local _ENV = Boli2Env");
            sb.AppendLine("--内容自动生成 请勿手动修改");
            sb.AppendLine("Create_ColorSuitModelDic={ }");
            sb.AppendLine("Create_ColorHeadModelDic={ }");
            sb.AppendLine("Create_ColorFaceModelDic={ }");
            sb.AppendLine("Create_ColorWingModelDic={ }");
            sb.AppendLine("Create_ColorTailModelDic={ }");
            sb.AppendLine("Create_ColorWeaponModelDic={ }");
            sb.AppendLine("Create_ColorBigHeadModelDic={ }");
            sb.AppendLine("Create_ColorAssistantModelDic={ }");

            //analysis
            foreach (var oneConfigEntity in dataEntity.FashionColorSetLst)
            {
                string targetDicStr = null;
                switch (oneConfigEntity.FashionType)
                {
                    case ScriptableAvatarFashionData.FashionType.Suit:
                        targetDicStr = "Create_ColorSuitModelDic";
                        break;
                    case ScriptableAvatarFashionData.FashionType.Head:
                        targetDicStr = "Create_ColorHeadModelDic";
                        break;
                    case ScriptableAvatarFashionData.FashionType.Face:
                        targetDicStr = "Create_ColorFaceModelDic";
                        break;
                    case ScriptableAvatarFashionData.FashionType.Wing:
                        targetDicStr = "Create_ColorWingModelDic";
                        break;
                    case ScriptableAvatarFashionData.FashionType.Tail:
                        targetDicStr = "Create_ColorTailModelDic";
                        break;
                    case ScriptableAvatarFashionData.FashionType.Weapon:
                        targetDicStr = "Create_ColorWeaponModelDic";
                        break;
                    case ScriptableAvatarFashionData.FashionType.BigHead:
                        targetDicStr = "Create_ColorBigHeadModelDic";
                        break;
                    case ScriptableAvatarFashionData.FashionType.Assistant:
                        targetDicStr = "Create_ColorAssistantModelDic";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                foreach (var oneColorSetConfigEntity in oneConfigEntity.FashionColorSetLst)
                {
                    //每个资源
                    sb.AppendLine($"{targetDicStr}[\"{oneColorSetConfigEntity.AssetID}\"] =  " + "{");
                    var idx = 0;
                    //性别start
                    foreach (var oneSexConfigEntity in oneColorSetConfigEntity.SexConfigLst)
                    {
                        sb.AppendLine($"    [\"{(int)oneSexConfigEntity.Sex}@{(int)oneSexConfigEntity.Job}\"] = " +
                                      "{");
                        sb.AppendLine($"    BundleName = \"{oneSexConfigEntity.BundleName}\",");
                        sb.AppendLine($"    WeaponColorType = {(int)oneSexConfigEntity.weaponColorType},");
                        sb.AppendLine($"        objName = \"{oneSexConfigEntity.Obj.gameObject.name}\",");
                        //材质start
                        sb.AppendLine("         MatNameIndexArr = {");
                        int count = oneSexConfigEntity.MatNameIndexArr.Count;
                        if (count == 0)
                            sb.AppendLine("        },");
                        for (int i = 0; i < count; i++)
                        {
                            if (oneSexConfigEntity.MatNameIndexArr[i] == null)
                            {
                                Debug.Log("mat is null"+oneSexConfigEntity.mark);
                            }
                            sb.AppendLine($"        [{i + 1}] = \"{oneSexConfigEntity.MatNameIndexArr[i].name}\"");
                            if (i != oneSexConfigEntity.MatNameIndexArr.Count - 1)
                            {
                                sb.Append(",");
                            }
                            else
                            {
                                sb.AppendLine("         },");
                            }
                        }

                        //材质end
                        sb.AppendLine($"isUseMaskColor={oneSexConfigEntity.IsUseMaskColor.ToString().ToLower()},");
                        //mask start
                        if (oneSexConfigEntity.IsUseMaskColor)
                        {
                            sb.AppendLine("MaskTexColorIndexArr = {");
                            var maskIdx = 0;
                            foreach (var maskConfEntity in oneSexConfigEntity.MaskMaterialConfigLst)
                            {
                                var matIndex = maskConfEntity.CurrMatIndex;
                                if (matIndex == -1)
                                {
                                    for (int i = 0; i < oneSexConfigEntity.MatNameIndexArr.Count; i++)
                                    {
                                        if (oneSexConfigEntity.MatNameIndexArr[i] == maskConfEntity.MaskMaterial)
                                        {
                                            matIndex = i + 1;
                                            break;
                                        }
                                    }
                                }

                                sb.AppendLine($"[{matIndex}] = " + "{");
                                sb.AppendLine($"R1 = {maskConfEntity.Color1.r},");
                                sb.AppendLine($"G1 = {maskConfEntity.Color1.g},");
                                sb.AppendLine($"B1 = {maskConfEntity.Color1.b},");
                                sb.AppendLine($"A1 = {maskConfEntity.Color1.a},");

                                sb.AppendLine($"R2 = {maskConfEntity.Color2.r},");
                                sb.AppendLine($"G2 = {maskConfEntity.Color2.g},");
                                sb.AppendLine($"B2 = {maskConfEntity.Color2.b},");
                                sb.AppendLine($"A2 = {maskConfEntity.Color2.a},");

                                sb.AppendLine($"R3 = {maskConfEntity.Color3.r},");
                                sb.AppendLine($"G3 = {maskConfEntity.Color3.g},");
                                sb.AppendLine($"B3 = {maskConfEntity.Color3.b},");
                                sb.AppendLine($"A3 = {maskConfEntity.Color3.a},");

                                sb.AppendLine($"R4 = {maskConfEntity.Color4.r},");
                                sb.AppendLine($"G4 = {maskConfEntity.Color4.g},");
                                sb.AppendLine($"B4 = {maskConfEntity.Color4.b},");
                                sb.AppendLine($"A4 = {maskConfEntity.Color4.a}");

                                maskIdx++;
                                if (maskIdx == oneSexConfigEntity.MaskMaterialConfigLst.Count)
                                {
                                    sb.AppendLine("}");
                                }
                                else
                                {
                                    sb.AppendLine("},");
                                }
                            }

                            sb.AppendLine("},");
                        }
                        //mask end

                        #region 特效

                        sb.AppendLine($"hasFX={oneSexConfigEntity.HasFX.ToString().ToLower()},");
                        if (oneSexConfigEntity.HasFX)
                        {
                            sb.AppendLine($"fxBundleName=\"{oneSexConfigEntity.FXBundleName.ToLower()}\",");
                            sb.AppendLine($"fxAttachedType={(int)oneSexConfigEntity.FXAttachedType},");
                            sb.AppendLine($"fxID=\"{oneSexConfigEntity.FXID}\",");
                        }

                        #endregion

                        idx++;
                        if (idx != oneColorSetConfigEntity.SexConfigLst.Count)
                        {
                            sb.AppendLine("},");
                        }
                        else
                        {
                            sb.AppendLine("}");
                        }


                        //性别end
                    }

                    sb.AppendLine("}");
                }
            }

            //export
            using (StreamWriter sw = new StreamWriter(fs, new UTF8Encoding(false)))
            {
                sw.Write(sb.ToString());
            }

            fs.Close();
            AssetDatabase.Refresh();
        }
    }
}