using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using SLua;
using XDPlugin.OGR;

[CustomLuaClassAttribute]
public class QualityManager : MonoBehaviour
{
    static QualityManager self;
    public UniversalRendererData forwardData;

    void Start()
    {
        self = this;
    }
    void Update()
    {
        
    }

    public static void SetQuality(int index)
    {
        switch (index)
        {
            case 0:
            case 1:

                break;
            case 2:
            case 3:
                break;
            case 4:
            case 5:
                break;
        }
    }

    public static void SetRendererFeature(string name, bool isActive)
    {
        foreach (ScriptableRendererFeature k in self.forwardData.rendererFeatures)
        {
            if (k.name == name)
            {
                k.SetActive(isActive);
                break;
            }
        }
    }

    public static void SetGrassRenderList(string _renderKeyStr)
    {
        GrassRender.SetGrassRenderList(_renderKeyStr);
    }

    public static void SetFadeObj(string name, float alpha)
    {
        
    }
}
