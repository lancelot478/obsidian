

```Csharp
protected static GameObject FindCommonRoot(GameObject g1, GameObject g2)  
{  
    if (g1 == null || g2 == null)  
        return null;  
  
    var t1 = g1.transform;  
    while (t1 != null)  
    {  
        var t2 = g2.transform;  
        while (t2 != null)  
        {  
            if (t1 == t2)  
                return t1.gameObject;  
            t2 = t2.parent;  
        }  
        t1 = t1.parent;  
    }  
    return null;  
}

public void RaycastAll(PointerEventData eventData, List<RaycastResult> raycastResults)  
{  
    raycastResults.Clear();  
    var modules = RaycasterManager.GetRaycasters();  
    var modulesCount = modules.Count;  
    for (int i = 0; i < modulesCount; ++i)  
    {  
        var module = modules[i];  
        if (module == null || !module.IsActive())  
            continue;  
  
        module.Raycast(eventData, raycastResults);  
    }  
  
    raycastResults.Sort(s_RaycastComparer);  
}
```


```Csharp
public override void Process()  
{
	if (!ProcessTouchEvents() && input.mousePresent)  
	    ProcessMouseEvent();
}
```