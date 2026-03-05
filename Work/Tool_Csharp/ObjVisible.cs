using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SLua;

[CustomLuaClassAttribute]
public class ObjVisible : MonoBehaviour
{
    static float offsetDis;
    static bool hasCheck;
    static Vector3 camPos;
    static Camera cam;
    Renderer render;

    void Start()
    {
        render = GetComponent<MeshRenderer>();
    }

    void Update()
    {
        if(cam == null)
        {
        	return;
        }
        if(!hasCheck)
        {
        	camPos = cam.transform.position;
        	hasCheck = true;
        }
        Vector3 pos = transform.position;
        bool isVisable = Mathf.Abs(camPos.x - pos.x) + Mathf.Abs(camPos.z - pos.z) < offsetDis;
        render.enabled = isVisable;
    }

    void LateUpdate()
    {
    	hasCheck = false;
    }

    public static void SetObjVisable(Camera _cam, float _offsetDis)
    {
    	cam = _cam;
    	offsetDis = _offsetDis;
    }
}
