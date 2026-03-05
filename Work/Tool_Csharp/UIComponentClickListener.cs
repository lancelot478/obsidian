using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using SLua;

[CustomLuaClassAttribute]
public class UIComponentClickListener : MonoBehaviour,IPointerClickHandler
{
    public delegate void VoidDelegate(GameObject go);
    public VoidDelegate onClick;

    static public UIComponentClickListener Get(GameObject go)
    {
        UIComponentClickListener listener = go.GetComponent<UIComponentClickListener>();
        if (listener == null) listener = go.AddComponent<UIComponentClickListener>();
        return listener;
    }

    public  void OnPointerClick(PointerEventData eventData)
    {
        if (onClick != null)
        {
            onClick(gameObject);
        }
    }

}