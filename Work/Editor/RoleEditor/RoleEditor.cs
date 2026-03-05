using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;


public class RoleEditor : EditorWindow
{
    private static readonly string[] baseJobAssetPath =
    {
        "Assets/Models/ModelRole/Magician_F",
        "Assets/Models/ModelRole/Magician_M",
        "Assets/Models/ModelRole/Reverend_M",
        "Assets/Models/ModelRole/Reverend_F",
        "Assets/Models/ModelRole/Swordman_M",
        "Assets/Models/ModelRole/Swordman_F",
        "Assets/Models/ModelRole/Ranger_M",
        "Assets/Models/ModelRole/Ranger_F",
        "Assets/Models/ModelRole/Assassin_M",
        "Assets/Models/ModelRole/Assassin_F",
        "Assets/Models/ModelHair/Hair_Swordman_F",
        "Assets/Models/ModelHair/Hair_Swordman_M",
        "Assets/Models/ModelHair/Hair_Ranger_M",
        "Assets/Models/ModelHair/Hair_Ranger_F",
        "Assets/Models/ModelHair/Hair_Assassin_F",
        "Assets/Models/ModelHair/Hair_Assassin_M",
        "Assets/Models/ModelHair/Hair_Magician_M",
        "Assets/Models/ModelHair/Hair_Magician_F",
        "Assets/Models/ModelHair/Hair_Reverend_F",
        "Assets/Models/ModelHair/Hair_Reverend_M",
     
    };

    private ProgressBar progress_Process = null;
    private Toggle toggleAutoPlayAfterBuild = null;

    [MenuItem("Tools/角色模型编辑器")]
    public static void ShowExample()
    {
        RoleEditor wnd = GetWindow<RoleEditor>();
        wnd.titleContent = new GUIContent("RoleEditor");
    }
    [MenuItem("Assets/时装/初始化选中目录时装染色资源")]
    public static void SetAvatarFashionColorBundles()
    {
        var objArr = Selection.GetFiltered(typeof(UnityEngine.Object),SelectionMode.Assets);
        foreach (var obj in objArr)
        {
            SetOneAvatarFolderColorBundles(obj,null,null);
        }
    }



    //格式化资源名后,根据material资源名和引用关系进行分bundle包体
    public static void SetOneAvatarFolderColorBundles(UnityEngine.Object obj,string _modelPath,List<string> unUsedMatLst)
    {
        Dictionary<string, uint> tgaRefCountDic = new Dictionary<string, uint>();
        Dictionary<string, string> tgaDepMatDict = new Dictionary<string, string>();
        
        var modelFolderPath =_modelPath;
        var rootFolderName = "";
        if (obj != null)
        {
            modelFolderPath = AssetDatabase.GetAssetPath(obj);

        }
        Debug.Log(modelFolderPath+" "+rootFolderName);
        rootFolderName = modelFolderPath.Substring(modelFolderPath.LastIndexOf("/") + 1);
        //当前模型路径下所有资源,一般只处理材质和纹理贴图
        var assetsGUID = AssetDatabase.FindAssets("", new[] { modelFolderPath });
        foreach (var guid in assetsGUID)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (assetPath.Contains("ExcludeFolder")) continue;
            if (assetPath.Contains(".mat"))
            {
                var importer = AssetImporter.GetAtPath(assetPath);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
                if (assetPath.IndexOf("global") != -1)
                {
                    Debug.Log($"{assetPath}为时装公共资源,故打到时装资源本体包内");
                    var bundlePrefix = string.Empty;
                    if (assetPath.Contains("ModelRole"))
                    {
                        bundlePrefix = "modelrole";
                    } else if (assetPath.Contains("ModelHead"))
                    {
                        bundlePrefix = "modelhead";
                    }
                    else if (assetPath.Contains("ModelTails"))
                    {
                        bundlePrefix = "modeltails";
                    }
                    else if (assetPath.Contains("ModelWing"))
                    {
                        bundlePrefix = "modelwing";
                    }
                    else if (assetPath.Contains("ModelWeapon"))
                    {
                        bundlePrefix = "modelweapon";
                    }
                    importer.assetBundleName = $"{bundlePrefix}/{rootFolderName}";
                    var depPaths = AssetDatabase.GetDependencies(assetPath, false);
                    //记录当前材质依赖
                    foreach (var depPath in depPaths)
                    {
                        if (depPath.Contains(".tga"))
                        {
                            tgaRefCountDic.TryAdd(depPath, 0);
                            //todo 可以将公共材质引用的贴图直接打入本体bundle包,不打的话也会隐式打入本体bundle
                            tgaRefCountDic[depPath]++;
                        }
                    }
                }
                else
                {
                    //清空mask的bundleName 工具优化后不存在maskbundle 
                    if (assetPath.Contains("mask") || assetPath.Contains("Conflict"))
                    {
                        importer.assetBundleName=string.Empty;
                        continue;
                    }
                    var lastIndex = mat.name.LastIndexOf("_");
                    if (lastIndex==-1)
                    {
                        Debug.LogError($"材质球分包失败,没有对应染色类型,跳过该材质  :{mat.name}");
                        continue;
                    }
                    var colorMatType = mat.name.Remove(lastIndex);
                 
                    importer.assetBundleName = $"modelmaterial/{colorMatType}";
                    var depPaths = AssetDatabase.GetDependencies(assetPath, false);
                    foreach (var depPath in depPaths)
                    {
                        //记录当前材质依赖
                        if (depPath.Contains(".tga"))
                        {
                            tgaRefCountDic.TryAdd(depPath, 0);
                            tgaDepMatDict.TryAdd(depPath, colorMatType);
                            tgaRefCountDic[depPath]++;
                        }
                    }
                }
               
            }
        }

        foreach (var guid in assetsGUID)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (assetPath.Contains("ExcludeFolder")) continue;
            if (assetPath.Contains(".tga"))
            {
                var importer = AssetImporter.GetAtPath(assetPath);
                var bundlePrefix = string.Empty;
                if (assetPath.Contains("ModelRole"))
                {
                    bundlePrefix = "modelrole";
                } else if (assetPath.Contains("ModelHead"))
                {
                    bundlePrefix = "modelhead";
                }
                else if (assetPath.Contains("ModelTails"))
                {
                    bundlePrefix = "modeltails";
                }
                else if (assetPath.Contains("ModelWing"))
                {
                    bundlePrefix = "modelwing";
                }
                else if (assetPath.Contains("ModelWeapon"))
                {
                    bundlePrefix = "modelweapon";
                }
                if (tgaRefCountDic.ContainsKey(assetPath) && tgaRefCountDic[assetPath] > 1)
                {
                    Debug.Log($"{assetPath}被1个以上资源引用,故打到时装资源本体包内");
                    importer.assetBundleName = $"{bundlePrefix}/{rootFolderName}";
                }
                else
                {
                    if (tgaDepMatDict.TryGetValue(assetPath, out var value))
                    {
                        importer.assetBundleName = $"modelmaterial/{value}";
                    }
                    else
                    {
                        importer.assetBundleName = $"{bundlePrefix}/{rootFolderName}";
                    }
                }
            }
        }
    
        //检查无用资源
            foreach (var guid in assetsGUID)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (assetPath.Contains(".mat"))
            {
                var importer = AssetImporter.GetAtPath(assetPath);
                if (importer.assetBundleName == string.Empty)
                {
                    if (unUsedMatLst!=null && !unUsedMatLst.Contains(importer.assetPath))
                    {
                        unUsedMatLst.Add(importer.assetPath);
                        Debug.LogError($"材质资源无assetBundle包,请检查是否还需使用或删除   : {importer.assetPath}");
                    }
                }
            }
        }
        AssetDatabase.SaveAssets();
    }
    public void CreateGUI()
    {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        // Import UXML
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/RoleEditor/RoleEditor.uxml");
        VisualElement labelFromUXML = visualTree.Instantiate();
        root.Add(labelFromUXML);
        var btn_CreateBaseJob = rootVisualElement.Q<Button>("btn_CreateBaseJob");
        progress_Process = rootVisualElement.Q<ProgressBar>("progress_Process");
        toggleAutoPlayAfterBuild = rootVisualElement.Q<Toggle>("toggle_AutoPlayAfterBuildBundle");
        var field = rootVisualElement.Q<TextField>("filed_fashionSetting1");
        btn_CreateBaseJob.RegisterCallback<ClickEvent>(OnBtnCreateBaseJobClick);
        progress_Process.lowValue = 0;
    }


    private void OnProcessOneTask(float progress)
    {
        progress_Process.lowValue = progress;
    }

    private void OnBtnCreateBaseJobClick(ClickEvent evt)
    {
        List<Object> arr = new List<Object>();
        for (int i = 0; i < 20; i++)
        {
            Object folderObj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(baseJobAssetPath[i]);
            if (folderObj != null)
                arr.Add(folderObj);
        }
        OnProcessOneTask(50f);
        tModelEditor.CreateRolePrefabLogic(arr.ToArray());
        OnProcessOneTask(100f);
        AssetBundleEditor.BuildAssetBundle();
        if (toggleAutoPlayAfterBuild is { value: true })
        {
            EditorApplication.isPlaying = true;
        }
    }
}