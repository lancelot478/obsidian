using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AzureSky;
using SLua;

[CustomLuaClassAttribute]
public class AzureTest : MonoBehaviour
{
    private AzureTimeController controller = null;
    static public float m_pauseNum = 12; 
    public bool m_pause = false;

    void Start()
    {
        controller = GetComponent<AzureTimeController>();
    }

    void Update()
    {
        if (m_pause && controller != null)
        {
            controller.timeline = m_pauseNum;
        }
    }
}