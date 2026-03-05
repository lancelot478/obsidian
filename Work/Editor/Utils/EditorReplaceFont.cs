using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SAGA.Editor;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class EditorReplaceFont
{
    private static readonly (string, string, string) HyZhengYuan = ("HYZhengYuan", "HYZhengYuan-85S.ttf",
        "汉仪正圆85简 SDF.asset");

    private static readonly (string, string, string) MplusRounded1CBold =
        ("MPLUSRounded1c", "MPLUSRounded1c-Bold.ttf", "MPLUSRounded1c-Bold SDF.asset");

    private static readonly (string, string, string) EliceDigitalBaeumBold =
        ("EliceDigitalBaeum", "EliceDigitalBaeum_Bold.otf", "EliceDigitalBaeum_Bold SDF.asset");


    private static readonly Dictionary<SystemLanguage, (string, string, string)> FontConfig = new()
    {
        { SystemLanguage.English, HyZhengYuan },
        { SystemLanguage.ChineseSimplified, HyZhengYuan },
        { SystemLanguage.ChineseTraditional, HyZhengYuan },
        { SystemLanguage.Japanese, MplusRounded1CBold },
        { SystemLanguage.Korean, EliceDigitalBaeumBold },
    };

    [MenuItem("Assets/多语言字体/切换英文")]
    public static void ReplaceFont_EN()
    {
        ReplaceFont(SystemLanguage.English);
    }

    [MenuItem("Assets/多语言字体/切换简体中文")]
    public static void ReplaceFont_CN()
    {
        ReplaceFont(SystemLanguage.ChineseSimplified);
    }

    [MenuItem("Assets/多语言字体/切换繁体中文")]
    public static void ReplaceFont_TW()
    {
        ReplaceFont(SystemLanguage.ChineseTraditional);
    }

    [MenuItem("Assets/多语言字体/切换日文")]
    public static void ReplaceFont_JP()
    {
        ReplaceFont(SystemLanguage.Japanese);
    }

    [MenuItem("Assets/多语言字体/切换韩文")]
    public static void ReplaceFont_KR()
    {
        ReplaceFont(SystemLanguage.Korean);
    }

    public static void ReplaceFont(SystemLanguage language)
    {
        if (!FontConfig.TryGetValue(language, out var config))
        {
            throw new Exception($"cannot find language config {language}");
        }

        var fontPath = Path.Combine("Assets/TextMesh Pro/Font", config.Item1, config.Item2);
        var font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
        var tmpFontPath = Path.Combine("Assets/TextMesh Pro/Font", config.Item1, config.Item3);
        var tmpFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(tmpFontPath);
        if (font == null || tmpFont == null)
        {
            throw new Exception($"cannot load language font {language}");
        }

        var planePath = Path.Combine(Application.dataPath, "Prefabs/Plane");
        if (!Directory.Exists(planePath))
        {
            throw new Exception("cannot find plane dir");
        }

        var idx = 0;
        var allPrefabs = Directory.GetFiles(planePath, "*.prefab", SearchOption.AllDirectories);
        foreach (var path in allPrefabs)
        {
            try
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                var changed = ExecuteChangeFont(root, font, tmpFont);
                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }

                idx++;
                Debug.Log($"progress: {idx} / {allPrefabs.Length}");
            }
            catch (Exception e)
            {
                Debug.Log($"load prefab failed {path}\n{e}");
            }
        }

        AssetDatabase.Refresh();
    }

    private static bool ExecuteChangeFont(GameObject obj, Font font, TMP_FontAsset tmpFontAsset)
    {
        var changed = false;
        if (obj == null)
        {
            return false;
        }

        var textList = obj.GetComponentsInChildren<Text>(true);
        foreach (var text in textList)
        {
            if (font != text.font)
            {
                changed = true;
                text.font = font;
            }
        }

        var tmpList = obj.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var proUGUI in tmpList)
        {
            if (proUGUI.font != tmpFontAsset)
            {
                changed = true;
                proUGUI.font = tmpFontAsset;
            }
        }

        return changed;
    }
}