using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class TextureCompressEditor : AssetPostprocessor
{
    //[MenuItem("Assets/ProcessAssets/CompressAllTextures")]
    //public static void TestCompressAllTextures() {
    //string[] allTextureGUIDs = AssetDatabase.FindAssets("t:texture", null);
    //string[] allTextureGUIDs = Selection.assetGUIDs;

    //List<TextureImporter> laterSave = new List<TextureImporter>();

    //foreach (string guid1 in allTextureGUIDs) {
    //    string        assetPath = AssetDatabase.GUIDToAssetPath(guid1);
    //    AssetImporter ai        = AssetImporter.GetAtPath(assetPath);
    //    if (ai == null) continue;

    //    TextureImporter ti = ai as TextureImporter;
    //    if (ti == null) continue;
    //    SetTexFormat(ti, ti.GetPlatformTextureSettings("iPhone"));
    //    SetTexFormat(ti, ti.GetPlatformTextureSettings("Android"));
    //    laterSave.Add(ti);
    //}

    //foreach (TextureImporter ti in laterSave) {
    //    ti.SaveAndReimport();
    //}

    //AssetDatabase.Refresh();
    //}

    //void OnPreprocessTexture()
    //{
    //TextureImporter ti = CheckTextureImporter(assetImporter);
    //if (ti == null)
    //{
    //    return;
    //}
    //var assetPath = ti.assetPath;
    //if (assetPath.StartsWith("Assets/Textures/") && assetPath.EndsWith(".png"))
    //{
    //    ti.textureType = TextureImporterType.Sprite;
    //    Debug.Log(ti.assetPath);
    //}
    //ti.SaveAndReimport();
    //AssetDatabase.Refresh();
    //}
    static List<string> passTexList = new List<string>() {
        "img_upgrade_bg_01",
    };
       
    [MenuItem("Assets/图片/检查图片压缩格式")]
    public static void CheckTextureFormat()
    {
        Texture[] folderTexArr = Selection.GetFiltered<Texture>(SelectionMode.DeepAssets);
        for (int i = 0; i < folderTexArr.Length; i++)
        {
            Texture tex = folderTexArr[i];
            if (passTexList.Contains(tex.name))
            {
                continue;
            }
            string assetPath = AssetDatabase.GetAssetPath(tex);
            AssetImporter ai = AssetImporter.GetAtPath(assetPath);
            if (ai == null)
            {
                continue;
            }
            TextureImporter ti = ai as TextureImporter;
            if (ti != null)
            {
                if (
                    !IsTexFormat(ti.GetPlatformTextureSettings("iPhone"), true)
                    || !IsTexFormat(ti.GetPlatformTextureSettings("Android"), true)
                    )
                {
                    Debug.Log("Tex nil format:"+ tex.name);
                }
            }
        }
        Debug.Log("Check Tex count:" + folderTexArr.Length);
    }

    [MenuItem("Assets/图片/压缩图片格式")]
    public static void SetFolderTexFormat()
    {
        Texture[] folderTexArr = Selection.GetFiltered<Texture>(SelectionMode.DeepAssets);
        List<TextureImporter> laterSave = new List<TextureImporter>();
        for (int i = 0; i < folderTexArr.Length; i++)
        {
            Texture tex = folderTexArr[i];
            if (passTexList.Contains(tex.name))
            {
                continue;
            }
            string assetPath = AssetDatabase.GetAssetPath(tex);
            AssetImporter ai = AssetImporter.GetAtPath(assetPath);
            TextureImporter ti = CheckTextureImporter(ai);
            if (ti != null)
            {
                laterSave.Add(ti);
            }
        }
        foreach (TextureImporter ti in laterSave)
        {
            ti.SaveAndReimport();
        }
        AssetDatabase.Refresh();
    }
    [MenuItem("Assets/图片/压缩图片格式256")]
    public static void SetFolderTexFormat256()
    {
        Texture[] folderTexArr = Selection.GetFiltered<Texture>(SelectionMode.DeepAssets);
        List<TextureImporter> laterSave = new List<TextureImporter>();
        for (int i = 0; i < folderTexArr.Length; i++)
        {
            Texture tex = folderTexArr[i];
            if (passTexList.Contains(tex.name))
            {
                continue;
            }
            string assetPath = AssetDatabase.GetAssetPath(tex);
            AssetImporter ai = AssetImporter.GetAtPath(assetPath);
            TextureImporter ti = CheckTextureImporter(ai,256,true);
            if (ti != null)
            {
                laterSave.Add(ti);
            }
        }
        foreach (TextureImporter ti in laterSave)
        {
            ti.SaveAndReimport();
        }
        AssetDatabase.Refresh();
    }

    static TextureImporter CheckTextureImporter(AssetImporter ai, int size = 2048, bool changeMaxSize = false)
    {
        if (ai == null)
        {
            return null;
        }
        TextureImporter ti = ai as TextureImporter;
        if (ti == null)
        {
            return null;
        }
        ti.isReadable = false;
        //bool isEnableMipmap = ti.assetPath.Contains("Environment");
        ti.mipmapEnabled = false;
        //if (isEnableMipmap)
        //{
        //    ti.anisoLevel = 0;
        //    ti.streamingMipmaps = true;
        //}
        SetTexFormat(ti, ti.GetPlatformTextureSettings("iPhone"));
        SetTexFormat(ti, ti.GetPlatformTextureSettings("Android"));
        DisTexFormat(ti, ti.GetPlatformTextureSettings("Standalone"));
        if (ti.maxTextureSize > size)
        {
            int width = 0;
            int height = 0;
            ti.GetSourceTextureWidthAndHeight(out width, out height);
            if (width > size || height > size)
            {
                Debug.Log("Texture size > 2048:" + ti.assetPath);
            }

            if (changeMaxSize)
            {
                ti.maxTextureSize = size;
            }
            
        }
        return ti;
    }

    static bool IsTexFormat(TextureImporterPlatformSettings settings, bool isCheckASTC6)//检查图片是否压缩过
    {
        if (settings == null)
        {
            return false;
        }
        if (isCheckASTC6)
        {
            if (settings.format != TextureImporterFormat.ASTC_4x4
                && settings.format != TextureImporterFormat.ASTC_5x5
                && settings.format != TextureImporterFormat.ASTC_6x6
                && settings.format != TextureImporterFormat.ASTC_8x8
                && settings.format != TextureImporterFormat.ASTC_10x10
                && settings.format != TextureImporterFormat.ASTC_12x12
                )
            {
                return false;
            }
            return true;
        }
        if (settings.format != TextureImporterFormat.ASTC_4x4
            && settings.format != TextureImporterFormat.ASTC_5x5
            && settings.format != TextureImporterFormat.ASTC_8x8
            && settings.format != TextureImporterFormat.ASTC_10x10
            && settings.format != TextureImporterFormat.ASTC_12x12
            )
        {
            return false;
        }
        return true;
    }

    static void SetTexFormat(TextureImporter ti, TextureImporterPlatformSettings settings)
    {
        if (settings == null)
        {
            settings = new TextureImporterPlatformSettings();
        }
        settings.overridden = true;
        if (!IsTexFormat(settings, false))
        {
            settings.format = TextureImporterFormat.ASTC_6x6;
        }
        //iosSettings.name               = "iPhone";
        //iosSettings.maxTextureSize     = 2048;
        //iosSettings.resizeAlgorithm    = TextureResizeAlgorithm.Mitchell;
        //iosSettings.compressionQuality = 50;
        if (ti.textureType != TextureImporterType.SingleChannel)
        {
            ti.SetPlatformTextureSettings(settings);
        }
    }

    static void DisTexFormat(TextureImporter ti, TextureImporterPlatformSettings settings)
    {
        if (settings == null || !settings.overridden)
        {
            return;
        }
        settings.overridden = false;
        ti.SetPlatformTextureSettings(settings);
    }

    // [MenuItem("Assets/Texture/启动材质GPU加速")]
    // public static void SetMaterialInstancing()
    // {
    //     Material[] matArr = Selection.GetFiltered<Material>(SelectionMode.DeepAssets);
    //     foreach (Material mat in matArr)
    //     {
    //         mat.enableInstancing = true;
    //     }
    //     AssetDatabase.Refresh();
    // }

    [MenuItem("Assets/图片/筛选非正常图片尺寸")]
    public static void SetMaterialInstancing()
    {
        Texture[] folderTexArr = Selection.GetFiltered<Texture>(SelectionMode.DeepAssets);
        Debug.Log(folderTexArr.Length);
        string info = "";
        for (int i = 0; i < folderTexArr.Length; i++)
        {
            Texture tex = folderTexArr[i];
            string assetPath = AssetDatabase.GetAssetPath(tex);
            string filePath = Application.dataPath.Replace("Assets", "") + assetPath;
            FileInfo file = new FileInfo(filePath);
            int normalLength = tex.width * tex.height * 4 + 100000;
            if (file.Length > normalLength)
            {
                info += assetPath + " " + file.Length / 1000 + "k\n";
            }
        }
        Debug.Log(info);
    }

    //[MenuItem("Assets/Material/检测材质属性")]
    //public static void RefreshMaterial()
    //{
    //    Material[] matArr = Selection.GetFiltered<Material>(SelectionMode.DeepAssets);
    //    for (int i = 0; i < matArr.Length; i++)
    //    {
    //        Material mat = matArr[i];
    //        if (mat.shader.name == "Hidden/InternalErrorShader")
    //        {
    //            string assetPath = AssetDatabase.GetAssetPath(mat);
    //            AssetDatabase.DeleteAsset(assetPath);
    //        }
    //        else
    //        {
    //            //List<string> keyList = new List<string>(mat.shaderKeywords);
    //            //keyList.Add("_UseNoEffect");
    //            //mat.shaderKeywords = keyList.ToArray();
    //            //mat.SetVector("_DynamicFxColor", Vector4.one);
    //            //mat.SetColor("_DynamicFxColor", Vector4.one);
    //        }
    //    }
    //    AssetDatabase.Refresh();
    //}

    [MenuItem("Assets/图片/关闭UI mipMap")]
    public static void CloseUITextureMimMap()
    {
        Texture[] folderTexArr = Selection.GetFiltered<Texture>(SelectionMode.DeepAssets);

        foreach (var tex in folderTexArr)
        {
            string assetPath = AssetDatabase.GetAssetPath(tex);
            if (assetPath.Contains("Assets/Textures"))
            {

                AssetImporter ai = AssetImporter.GetAtPath(assetPath);
                TextureImporter textureImporter = ai as TextureImporter;

                textureImporter.mipmapEnabled = false;
                textureImporter.SaveAndReimport();
            }
        }
        AssetDatabase.Refresh();
    }
}

