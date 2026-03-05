using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System;

[AddComponentMenu("PageLayout")]
public class PageLayout : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
 
    private ScrollRect scroll;                        //滑动组件  
    private float targethorizontal = 0;             //滑动的起始坐标  
    private bool isDrag = false;                    //是否拖拽结束  
    private List<float> posList = new List<float>();            //求出每页的临界角，页索引从0开始  
    private int currentPageIndex = -1;
    public Action<int> OnPageChanged;
    public RectTransform content;
    private bool stopMove = true;
    public float smooting = 4;      //滑动速度  
    public float sensitivity = 0;
    private float startTime;
 
    private float startDragHorizontal;
    public Transform toggleList;

    void Start()
    {
        scroll = transform.GetComponent<ScrollRect>();
        
        // //按钮事件
        // for (int i = 0; i < num; i++)
        // {
        //     var toggle = toggleList.GetChild(i).GetComponent<Toggle>();
        //     if (toggle != null)
        //     {
        //         var index = i;
        //         toggle.onValueChanged.AddListener(delegate
        //         {
        //             scroll.horizontalNormalizedPosition = posList[index];
        //         });
        //     }
        // }
    }
    void Update()
    {
        if (!isDrag && !stopMove)
        {
            startTime += Time.deltaTime;
            float t = startTime * smooting;
            scroll.horizontalNormalizedPosition = Mathf.Lerp(scroll.horizontalNormalizedPosition, targethorizontal, t);
            if (t >= 1) 
                stopMove = true;
        }
    }
 
    private void PageTo(int index)
    {
        if (index >= 0 && index < posList.Count)
        {
            scroll.horizontalNormalizedPosition = posList[index];
           
        }
    }
    private void SetPageIndex(int index)
    {
        if (currentPageIndex != index)
        {
            currentPageIndex = index;
            if (OnPageChanged != null)
                OnPageChanged(index);
        }
    }
 
    public void OnBeginDrag(PointerEventData eventData)
    {
        isDrag = true;
        //未显示的长度
        posList.Clear();
        var num = scroll.content.transform.childCount;
        for (int i = 0; i < num; i++)
        {
            if(num <=1)
            {
                posList.Add(0);
                break;
            }
            var pos = (float) (1.0 * i / (num - 1));
            // Debug.Log("pos:" + pos);
            posList.Add(pos);
        }
        //开始拖动
        startDragHorizontal = scroll.horizontalNormalizedPosition;
    }
 
    public void OnEndDrag(PointerEventData eventData)
    {
        
        float posX = scroll.horizontalNormalizedPosition;
        posX += ((posX - startDragHorizontal) * sensitivity);
        posX = posX < 1 ? posX : 1;
        posX = posX > 0 ? posX : 0;
        int index = 0;
        float offset = Mathf.Abs(posList[index] - posX);
        
 
 
        for (int i = 1; i < posList.Count; i++)
        {
            float temp = Mathf.Abs(posList[i] - posX);
            // Debug.Log("temp " + temp);
            // Debug.Log("i" + i);
            if (temp < offset)
            {
                index = i;
                offset = temp;
            }
        }
        // PageTo(index);
        targethorizontal = posList[index];
        SetPageIndex(index);
        GetIndex(index);
        isDrag = false;
        startTime = 0;
        stopMove = false;
    }
 
    public void GetIndex(int index)
    {
        try
        {
            var toggle = toggleList.GetChild(index).GetComponent<Toggle>();
            toggle.isOn = true;
        }catch {}
    }
}