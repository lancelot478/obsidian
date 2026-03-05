using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;


[DisallowMultipleComponent]
public class UISound : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{

    [Header("点击音效")]
    public string clickEventName;
    [Header("弹起音效")]
    public string upEventName;
    [Header("按压音效")]
    public string downEventName;

    [Header("打开音效")]
    public string showEventName;
    [Header("关闭音效")]
    public string destroyEventName;
    [Header("显示音效")]
    public string enableEventName;
    [Header("隐藏音效")]
    public string disableEventName;

    void Start()
    {
        if (!string.IsNullOrEmpty(showEventName))
        {
            AudioManager.PostEvent(showEventName);
        }  
    }

    public void OnEnable()
    {
        if (!string.IsNullOrEmpty(enableEventName))
        {
            AudioManager.PostEvent(enableEventName);
        }
    }

    public void OnDisable()
    {
        if (!string.IsNullOrEmpty(disableEventName))
        {
            AudioManager.PostEvent(disableEventName);
        }
    }

    public void OnDestroy()
    {
        if (!string.IsNullOrEmpty(destroyEventName))
        {
            AudioManager.PostEvent(destroyEventName);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!string.IsNullOrEmpty(downEventName))
        {
            AudioManager.PostEvent(downEventName);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!string.IsNullOrEmpty(upEventName))
        {
            AudioManager.PostEvent(upEventName);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!string.IsNullOrEmpty(clickEventName))
        {
            AudioManager.PostEvent(clickEventName);
        }
    }

}