
### Bubble Get Nearest EventSystemHandler
```Csharp

public delegate void EventFunction<T1>(T1 handler, BaseEventData eventData);
public static EventFunction<IPointerClickHandler> pointerClickHandler
{
    get { return s_PointerClickHandler; }
}

//检查是否有EventSystem
private static bool ShouldSendToComponent<T>(Component component) where T : IEventSystemHandler  
{  
    var valid = component is T;  
    if (!valid)  
        return false;  
  
    var behaviour = component as Behaviour;  
    if (behaviour != null)  
        return behaviour.isActiveAndEnabled;  
    return true;  
}
private static void GetEventList<T>(GameObject go, IList<IEventSystemHandler> results) where T : IEventSystemHandler  
{  
    if (results == null)  
        throw new ArgumentException("Results array is null", "results");  
  
    if (go == null || !go.activeInHierarchy)  
        return;  
  
    var components = ListPool<Component>.Get();  
    go.GetComponents(components);  
  
    var componentsCount = components.Count;  
    for (var i = 0; i < componentsCount; i++)  
    {  
        if (!ShouldSendToComponent<T>(components[i]))  
            continue;  
          
        results.Add(components[i] as IEventSystemHandler);  
    }  
    ListPool<Component>.Release(components);  
}
public static bool CanHandleEvent<T>(GameObject go) where T : IEventSystemHandler  
{  
    var internalHandlers = ListPool<IEventSystemHandler>.Get();  
    GetEventList<T>(go, internalHandlers);  
    var handlerCount = internalHandlers.Count;  
    ListPool<IEventSystemHandler>.Release(internalHandlers);  
    return handlerCount != 0;  
}
public static GameObject GetEventHandler<T>(GameObject root) where T : IEventSystemHandler  
{  
    if (root == null)  
        return null;  
  
    Transform t = root.transform;  
    while (t != null)  
    {  
        if (CanHandleEvent<T>(t.gameObject))  
            return t.gameObject;  
        t = t.parent;  
    }  
    return null;  
}
```


```Csharp
public override void Process()  
{
	if (!ProcessTouchEvents() && input.mousePresent)  
	    ProcessMouseEvent();
}
```