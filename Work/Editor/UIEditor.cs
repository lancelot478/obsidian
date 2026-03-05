using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.IO;
public class UIEditor : Editor
{

    [MenuItem("Tools/Test/Prefab按钮添加点击特效")]
    public static void ButtonAddPressAnim()
    {
        var obj = Selection.activeGameObject;
        if (obj != null)
        {
            var overrideControllerPath = "Assets/Textures/UI2.0/Effect/Animation/Common/common_btn.controller";
            var overrideController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(overrideControllerPath);
            TransformProcess(obj.transform, overrideController);
            EditorUtility.SetDirty(obj);
            AssetDatabase.SaveAssets();
            Debug.Log(obj.name + "执行完毕。");
        }
        else
        {
            Debug.Log("请先选中一个prefab。");
        }
    }

    public static void TransformProcess(Transform tra, RuntimeAnimatorController controller)
    {
        var button = tra.GetComponent<Button>();
        if (tra.name != "tex_Mask" && button != null)
        {
            AddAnimator(button, controller);
        }
        var count = tra.childCount;
        if (count > 0)
        {
            for (int i = 0; i < count; i++)
            {
                var child = tra.GetChild(i);
                TransformProcess(child, controller);
            }
        }
    }

    public static void AddAnimator(Button button, RuntimeAnimatorController controller)
    {
        if (button.GetComponent<Animator>() == null)
        {
            button.transition = Selectable.Transition.Animation;
            var animtor = ObjectFactory.AddComponent<Animator>(button.gameObject);
            animtor.runtimeAnimatorController = controller;
            Debug.Log("button 名字：" + button.name);
        }

    }

    public static string MOVE_PATH = "Assets/Prefabs/SkillEffectTemp/";
    [MenuItem("Tools/Test/移除不使用特效")]
    public static void MoveEffectPrefabToOldFile()
    {
        var obj = Selection.activeGameObject;
        if (obj != null)
        {
            string name = obj.name;
            string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(obj);
            AssetImporter objAsseet = AssetImporter.GetAtPath(path);
            objAsseet.assetBundleName = null;
            System.IO.FileInfo fileInfo = new System.IO.FileInfo(path);

            string moveObjPath = MOVE_PATH + fileInfo.Name;

            if (fileInfo.Exists && System.IO.Directory.Exists(MOVE_PATH))
            {
                string movePath = MOVE_PATH + fileInfo.Name;
                if (System.IO.File.Exists(movePath))
                {
                    System.IO.File.Delete(movePath);
                }
                fileInfo.MoveTo(movePath);
            }
            AssetDatabase.Refresh();
            Debug.Log(name + "执行完毕。");
        }
        else
        {
            Debug.Log("请先选中一个prefab。");
        }
    }

    [MenuItem("Tools/Test/PackTexturesToMultipleSprite")]
    public static void CreateSpriteSheetAtlasSliced()
    {
        int maxSize = 2048;
        List<Texture2D> textures = new List<Texture2D>();
        Object[] selectedObjects = Selection.GetFiltered<Texture2D>(SelectionMode.DeepAssets);//Selection.objects;
        foreach (Object obj in selectedObjects)
        {
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(obj));
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.mipmapEnabled = false;
                importer.isReadable = true;

                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(obj));
                AssetDatabase.Refresh();
            }
            textures.Add(obj as Texture2D);
        }
        textures.Sort((x, y) => string.Compare(x.name, y.name));
        string path = AssetDatabase.GetAssetPath(textures[0]);
        path = path.Remove(path.LastIndexOf('/'));

        Texture2D atlas = new Texture2D(maxSize, maxSize, TextureFormat.ARGB32, false);
        Color[] colors = new Color[maxSize * maxSize];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.clear;
        }
        atlas.SetPixels(colors);
        atlas.Apply();
        Rect[] rects = atlas.PackTextures(textures.ToArray(), 4, maxSize, false);

        byte[] bytes = atlas.EncodeToPNG();
        System.IO.File.WriteAllBytes(path + "/spritesheet.png", bytes);

        AssetDatabase.Refresh();
        TextureImporter tempImporter = (TextureImporter)AssetImporter.GetAtPath(path + "/spritesheet.png");
        if (tempImporter != null)
        {
            tempImporter.textureType = TextureImporterType.Sprite;
            tempImporter.spriteImportMode = SpriteImportMode.Multiple;
            tempImporter.maxTextureSize = maxSize;

            int count = textures.Count;
            SpriteMetaData[] meta = new SpriteMetaData[count];

            for (int i = 0; i < count; i++)
            {
                meta[i].name = textures[i].name; //i.ToString();
                meta[i].alignment = (int)SpriteAlignment.Center;
                meta[i].pivot = Vector2.zero;
                meta[i].rect = new Rect(rects[i].x * atlas.width, rects[i].y * atlas.height, rects[i].width * atlas.width, rects[i].height * atlas.height); //rects[i];
            }

            tempImporter.spritesheet = meta;
            AssetDatabase.ImportAsset(path + "/spritesheet.png");
            AssetDatabase.Refresh();
        }
    }

    static void AddSpriteMetaData(List<SpriteMetaData> data, int width, int height, string name){
        SpriteMetaData smd = new SpriteMetaData ();
        smd.pivot = new Vector2 (0.5f, 0.5f);
        smd.alignment = 9;
        smd.name = name;
        smd.rect = new Rect (0, 0, width, height);
        Debug.Log ("Add sprite name :" + smd.name);
        data.Add (smd);
    }

    [MenuItem("Assets/坐骑图标处理 SliceMountsTexture")]
    static void SlicePetsTexture ()
    {
        DirectoryInfo rootDirInfo = new DirectoryInfo (Application.dataPath + "/Textures/Atlas/Mount");
        foreach (FileInfo pngFile in rootDirInfo.GetFiles("*.png",SearchOption.TopDirectoryOnly)) {
            string allPath = pngFile.FullName;
            string assetPath = allPath.Substring (allPath.IndexOf ("Assets"));
            Texture2D myTexture = (Texture2D)AssetDatabase.LoadAssetAtPath<Texture2D> (assetPath);
            string path = AssetDatabase.GetAssetPath (myTexture);
            TextureImporter ti = AssetImporter.GetAtPath (path) as TextureImporter;
            ti.isReadable = true;
            List<SpriteMetaData> newData = new List<SpriteMetaData> ();
            
            int SliceWidth = 0;
            int SliceHeight = 0;
            int SliceMinWidth = 188;
            int SliceMinHeight = 188;
            ti.GetSourceTextureWidthAndHeight(out SliceWidth, out SliceHeight);
            AddSpriteMetaData(newData, SliceWidth, SliceHeight, myTexture.name);
            AddSpriteMetaData(newData, SliceMinHeight, SliceMinHeight, myTexture.name + "_small");

            ti.spritesheet = newData.ToArray ();
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Multiple;
            Debug.Log ("Path :" + path);
            AssetDatabase.ImportAsset (path, ImportAssetOptions.ForceUpdate);
        }
        AssetDatabase.Refresh ();
    }
    

    // 自定义Image按钮
    
    [MenuItem("GameObject/UI/Common Image")]
    public static void CreateImage()
    {
        var image = Create<Image>();
        image.raycastTarget = false;
        image.maskable = image.GetComponentInParent<RectMask2D>() != null || image.GetComponentInParent<Mask>() != null;
    }

    public static T Create<T>(string name = null) where T : Component
    {
        if (string.IsNullOrEmpty(name))
            name = typeof(T).Name;
        GameObject go = new GameObject(name);
        go.transform.SetParent(Selection.activeTransform);
        Selection.activeGameObject = go;
        return go.AddComponent<T>();
    }
}

