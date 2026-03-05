#Unity 

[[UGUI源码一]6千字带你入门UGUI源码 - 知乎](https://zhuanlan.zhihu.com/p/437704772)

[UGUI 事件分发揭秘 - 知乎](https://zhuanlan.zhihu.com/p/477232369)


|                             |
| --------------------------- |
| [[UGUI源码-EventSystem]]      |
| [[UGUI源码-ExecuteEvents]]    |
| [[UGUI源码-PointerEventData]] |
| [[UGUI源码-RayCast]]          |
|                             |

```Csharp
public class Button : Selectable, IPointerClickHandler, ISubmitHandler
 {
     [Serializable]
     //定义一个点击事件
     public class ButtonClickedEvent : UnityEvent {}
​
     // 实例化一个ButtonClickedEvent的事件
     [FormerlySerializedAs("onClick")]
     [SerializeField]
     private ButtonClickedEvent m_OnClick = new ButtonClickedEvent();
​
     protected Button()
     {}
    
     //常用的onClick.AddListener()就是监听这个事件
     public ButtonClickedEvent onClick
     {
         get { return m_OnClick; }
         set { m_OnClick = value; }
     }
     
     //如果按钮处于活跃状态并且可交互(Interactable设置为true)，则触发事件
     private void Press()
     {
         if (!IsActive() || !IsInteractable())
             return;
​
         UISystemProfilerApi.AddMarker("Button.onClick", this);
         m_OnClick.Invoke();
     }
    
     //鼠标点击时调用该函数，继承自 IPointerClickHandler 接口
     public virtual void OnPointerClick(PointerEventData eventData)
     {
         if (eventData.button != PointerEventData.InputButton.Left)
             return;
​
         Press();
     }
    
     //按下“提交”键后触发(需要先选中该游戏物体)，继承自 ISubmitHandler
     //"提交"键可以在 Edit->Project Settings->Input->Submit 中自定义
     public virtual void OnSubmit(BaseEventData eventData){...}
​
     private IEnumerator OnFinishSubmit(){...}
}
```
`IPointerClickHandler`接口仅包含一个`OnPointerClick()`方法，当鼠标点击时会调用该接口的方法。而`Button`能触发点击事件是因为继承自`IPointerClickHandler`接口，并且重写了`OnPointerClick`方法。

那`IPointerClickHandler`接口的方法又是被谁调用的呢？查找引用，发现是`ExecuteEvents`类的`Execute`方法(该类相当于事件执行器，提供了许多通用的事件处理方法)，并且`Execute`方法赋值给`s_PointerClickHandler`字段。

```csharp
private static readonly EventFunction<IPointerClickHandler> s_PointerClickHandler = Execute;
private static void Execute(IPointerClickHandler handler, BaseEventData eventData)
{
    handler.OnPointerClick(ValidateEventData<PointerEventData>(eventData));
}
```
**为了能看的更清楚，总结一下调用关系，即`Button`继承自`Selectable`、`IPointercliClickHandler`、`ISubmitHandler`，而`IPointercliClickHandler`、`ISubmitHandler`继承自`IEventSystemHandler`，`ExecuteEvent`会在鼠标松开时通过`Execute`函数调用`IPointercliClickHandler`、`ISubmitHandler`接口的方法，从而触发`Button`的`onClick`事件**E

继续往上找，`ExecuteEvents`类中还定义了一个`EventFunction<T1>`的泛型委托以及该委托类型的属性，这个返回`s_PointerClickHandler`，要查找谁触发的点击事件，只需要找到谁调用了`pointerClickHandler`即可

```csharp
public delegate void EventFunction<T1>(T1 handler, BaseEventData eventData);
public static EventFunction<IPointerClickHandler> pointerClickHandler
{
    get { return s_PointerClickHandler; }
}
```
容易发现，`StandaloneInputModule`和`TouchInputModule`类对其有调用，这两个类继承自`BaseInput`，主要用以处理鼠标、键盘、控制器等设备的输入，**`EventSystem`类会在`Update`中每帧检查可用的输入模块的状态是否发生变化，并调用`TickModules()`和当前输入模块(`m_CurrentInputModule`)的`Process()`函数**(后面会进行讲解)。下面是`StandaloneInputModule`的部分代码，它继承自`BaseInputModule`

```csharp
// 计算和处理任何鼠标按钮状态的变化
//Process函数间接对其进行调用（调用链过长，不一一展示)
protected void ProcessMousePress(MouseButtonEventData data)
{
    ...//省略部分代码
       //鼠标按键抬起时调用（按键包括鼠标左键、中间滑轮和右键）
    if (data.ReleasedThisFrame())
    {
        ReleaseMouse(pointerEvent, currentOverGo);
    }
    ...
}
​
//满足松开鼠标的条件时调用
//currentOverGo ：当前选中的游戏物体
private void ReleaseMouse(PointerEventData pointerEvent, GameObject currentOverGo)
{
    ...//省略部分代码
    if (pointerEvent.pointerPress == pointerUpHandler && pointerEvent.eligibleForClick)
    {
        //执行Execute函数，传入ExecuteEvents.pointerClickHandler委托
        ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerClickHandler);
    }  
    ...
}
```

查看`ExecuteEvents.Execute`的实现

> 上面已经查看过`Execute`方法，为什么现在又出来一个？  
> 因为`ExecuteEvents`中有N多个重载函数

```csharp
//target ： 需要执行事件的游戏对象
public static bool Execute<T>(GameObject target, BaseEventData eventData, EventFunction<T> functor) where T : IEventSystemHandler
{
    var internalHandlers = s_HandlerListPool.Get();
    //获取target对象的事件
    GetEventList<T>(target, internalHandlers);
    //  if (s_InternalHandlers.Count > 0)
    //      Debug.Log("Executinng " + typeof (T) + " on " + target);
​
    for (var i = 0; i < internalHandlers.Count; i++)
    {
        T arg;
        try
        {
            arg = (T)internalHandlers[i];
        }
        catch (Exception e)
        {
            var temp = internalHandlers[i];
            Debug.LogException(new Exception(string.Format("Type {0} expected {1} received.", typeof(T).Name, temp.GetType().Name), e));
            continue;
        }
​
        try
        {
            //执行EventFunction<T>委托,例如pointerClickHandler(arg,eventData)
            functor(arg, eventData);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
​
    var handlerCount = internalHandlers.Count;
    s_HandlerListPool.Release(internalHandlers);
    return handlerCount > 0;
}
```

**也就是说，`EventSystem`会在`Update()`中调用当前可用`BaseInputModule`的`Process()`方法，该方法会处理鼠标的按下、抬起等事件，当鼠标抬起时调用`ReleaseMouse()`方法，并最终调用`Execute()`方法并触发`IPointerClick`事件。** 如下图所示(为了简洁，类图并不完整)

![](https://pic2.zhimg.com/80/v2-271819772d80baa805565dc9182d3ab5_720w.webp)

> ReleaseMouse()是否只有鼠标左键抬起才会触发？  
> 鼠标左、中、右键都会触发该函数，只不过`Button`在实现`OnPointerClick()`函数时忽略了鼠标中键和右键，使得只有左键能触发`Button`的点击事件

但现在还存在一个问题，怎么知道上述代码中事件执行目标`target`的值呢？探究这个问题之前，我们需要先对`UGUI`源码有个总体的认识，因为它涉及的知识点比较多。

## **事件系统整体概述**

我们先看`EventSystem`源码在文件夹中的分类

![](https://pic4.zhimg.com/80/v2-15edd68d6268be3dd761ac92e6b6961b_720w.webp)

从图中就可以看出主要**包含三个子板块，分别是`EvnetData`、`InputModules`和`Raycasters`**。

再看一个整体的类图，类图中包括了许多重要的类，如`EventSystem`、`BaseRaycast`、`BaseInputModule`等，它们都是继承自`UIBehaviour`，而`UIBehaviour`又是继承`MonoBehaviour`。（类图并不完整，只涉及部分类）

![](https://pic4.zhimg.com/80/v2-d401d582568a761d54f89a64a8371a93_720w.webp)

接下来对这些内容进行详细讲解。

### **`EventSystem`类**

事件系统主要是**基于输入(键盘、鼠标、触摸或自定义输入)向应用程序中的对象发送事件**，当然这需要其他组件的配合。当你在`GameObject`中添加`EventSystem`时，你会发现它并没有太多的功能，这是因为**`EventSystem`本身被设计成事件系统不同模块之间通信的管理者和推动者**，它主要包含以下功能：

- **管理哪个游戏对象被认为是选中的**
- **管理正在使用的输入模块**
- **管理射线检测(如果需要)**
- **根据需要更新所有输入模块**

### 管理输入模块

下面看一下具体代码。首先是声明了`BaseInputModule`类型的`List`和变量，用来保存输入模块(`Module`)

```csharp
//系统输入模块
private List<BaseInputModule> m_SystemInputModules = new List<BaseInputModule>();
//当前输入模块
private BaseInputModule m_CurrentInputModule;
```

接下来，它会在`Update`中处理这些模块，调用`TickModules`方法，更新每一个模块，并且会在满足条件的情况下调用当前模块的`Process`方法

```csharp
 protected virtual void Update()
 {
    //遍历m_SystemInputModules，如果其中的Module不为null，则调用UpdateModule方法
     TickModules();
​
     //遍历m_SystemInputModules判断其中的输入模块是否支持当前平台
     //如果支持并且可以激活，则将其赋值给当前输入模块并Break
     bool changedModule = false;
     var systemInputModulesCount = m_SystemInputModules.Count;
     for (var i = 0; i < systemInputModulesCount; i++)
     {
         var module = m_SystemInputModules[i];
         if (module.IsModuleSupported() && module.ShouldActivateModule())
         {
             if (m_CurrentInputModule != module)
             {
                 ChangeEventModule(module);
                 changedModule = true;
             }
             break;
         }
     }
​
     //如果上面没找到符合条件的模块，则使用第一个支持当前平台的模块
     if (m_CurrentInputModule == null)
     {
         for (var i = 0; i < systemInputModulesCount; i++)
         {
             var module = m_SystemInputModules[i];
             if (module.IsModuleSupported())
             {
                 ChangeEventModule(module);
                 changedModule = true;
                 break;
             }
         }
     }
​
     //如果当前模块没有发生变化并且当前模块不为空
     if (!changedModule && m_CurrentInputModule != null)
         m_CurrentInputModule.Process();
 }
​
private void TickModules()
{
    var systemInputModulesCount = m_SystemInputModules.Count;
    for (var i = 0; i < systemInputModulesCount; i++)
    {
        if (m_SystemInputModules[i] != null)
            m_SystemInputModules[i].UpdateModule();
    }
}
```

`Process()`方法主要是将各种输入事件（如点击、拖拽等事件）传递给`EventSystem`当前选中的`GameObject`(即`m_CurrentSelected`)

### 管理选中的游戏对象

当场景中的游戏物体(`Button`、`Dropdown`、`InputField`等)被选中时，会通知之前选中的对象执行被取消(`OnDeselect`)事件，通知当前选中的对象执行选中(`OnSelect`)事件，部分代码如下

```csharp
public void SetSelectedGameObject(GameObject selected, BaseEventData pointer)
{
    ......//省略部分代码
    //通知之前被选中取消选中
    ExecuteEvents.Execute(m_CurrentSelected, pointer, ExecuteEvents.deselectHandler);
    m_CurrentSelected = selected;
    //通知当前物体被选中
    ExecuteEvents.Execute(m_CurrentSelected, pointer, ExecuteEvents.selectHandler);
    m_SelectionGuard = false;
}
```

### 管理射线检测

`EventSystem`中，还有一个非常重要的函数`RaycastAll()`，主要是获取目标。它被`PointerInputModule`类调用，大致来说是当鼠标设备可用或触摸板被使用时调用。

```csharp
public void RaycastAll(PointerEventData eventData, List<RaycastResult> raycastResults)
 {
     raycastResults.Clear();
     //获取BaseRaycast对象
     var modules = RaycasterManager.GetRaycasters();
     var modulesCount = modules.Count;
     for (int i = 0; i < modulesCount; ++i)
     {
         var module = modules[i];
         if (module == null || !module.IsActive())
             continue;
        //调用Raycast方法，
         module.Raycast(eventData, raycastResults);
     }
​
     raycastResults.Sort(s_RaycastComparer);
 }
```

它首先获取所有的`BaseRaycast`对象，然后调用它的`Raycast`方法，用以获取屏幕某个点下的所有目标（这个方法具体功能及实现的会在`Raycast`模块中进行讲解)，最后对得到的结果进行排序，大部分情况都是根据深度(`Depth`)进行排序，在一些情况下也会使用距离(`Distance`)、排序顺序(`SortingOrder`，如果是`UI`元素则是根据`Canvas`面板的`Sort order`值，`3D`物体默认是0)或者排序层级(`Sorting Layer`)等作为排序依据。

讲了这么一大堆，来张图总结一下。**`EventSystem`会在`Update`中调用输入模块的`Process`方法来处理输入消息，`PointerInputModule`会调用`EventSystem`中的`RaycastAll`方法进行射线检测，`RaycastAll`又会调用`BastRaycaster`的`Raycast`方法执行具体的射线检测操作，主要是获取被选中的目标信息。**

![](https://pic4.zhimg.com/80/v2-9685293899700c01f1d93245f0deda87_720w.webp)

简单概括一下`UML`图的含义，比如实线+三角形表示继承，实线+箭头表示关联，虚线+箭头表示依赖，关联和依赖的区别主要是引用其他类作为成员变量代表的是关联关系，将其他类作为局部变量、方法参数，或者引用它的静态方法，就属于依赖关系。

### **`InputModules`**

输入模块是配置和定制事件系统主逻辑的地方。 自带的输入模块有两个，一个是为独立输入(`StandaloneInputModule`)，另一个是为触摸输入(`TouchInputModule`)。 `StandaloneInputModule`是`PC`、`Mac&Linux`上的具体实现，而`TouchInputModule`是`IOS`、`Android`等移动平台上的具体实现，每个模块都按照给定配置接收和分派事件。 运行`EventSystem`后，它会查看附加了哪些输入模块，并将事件传递给特定的模块。 内置的输入模块旨在支持常见的游戏配置，如触摸输入、控制器输入、键盘输入和鼠标输入等。

它的主要任务有三个，分别是

- **处理输入**
- **管理事件状态**
- **发送事件到场景对象**

在讲`Button`的时候我们提到鼠标的点击事件是在`BaseInputModule`中触发的，除此之外，`EventInterface`接口中的其他事件也都是由输入模块产生的，具体触发条件如下：

- 当鼠标或触摸进入、退出当前对象时执行`pointerEnterHandler`、`pointerExitHandler`。
- 在鼠标或者触摸按下、松开时执行`pointerDownHandler`、`pointerUpHandler`。
- 在鼠标或触摸松开并且与按下时是同一个响应物体时执行`pointerClickHandler`。
- 在鼠标或触摸位置发生偏移（偏移值大于一个很小的常量）时执行`beginDragHandler`。
- 在鼠标或者触摸按下且当前对象可以响应拖拽事件时执行`initializePotentialDrag`。
- 对象正在被拖拽且鼠标或触摸移动时执行`dragHandler`。
- 对象正在被拖拽且鼠标或触摸松开时执行`endDragHandler`。
- 鼠标或触摸松开且对象未响应`pointerClickHandler`情况下，如果对象正在被拖拽，执行`dropHandler`。
- 当鼠标滚动差值大于零执行`scrollHandler`。
- 当输入模块切换到`StandaloneInputModule`时执行`updateSelectedHandler`。（不需要Input类）
- 当鼠标移动导致被选中的对象改变时，执行`selectHandler`和`deselectHandler`。
- 导航事件可用情况下，按下上下左右键，执行`moveHandler`，按下确认键执行`submitHandler`，按下取消键执行`cancelHandler`。

更加底层的调用还是`UnityEngine.Input`类，但可惜的是这部分`Unity`并没有开源。

> 每次事件系统中只能有一个输入模块处于活跃状态，并且必须与`EventSystem`组件处于相同的游戏对象上。

### 执行事件

既然`InputModule`主要就是处理设备输入，发送事件到场景对象，那这些事件是怎么执行的呢？在讲`Button`的时候，我们提到过`ExecuteEvent`类，其实事件的执行都是通过这个类进行的，不过也需要`EventInterface`接口配合。这个类中定义了许多接口，比如鼠标按下、点击、拖拽等，下图展示了部分接口的继承关系。

![](https://pic4.zhimg.com/80/v2-cfeebe5f05d3c8aa3dbe72fb99501913_720w.webp)

`ExecuteEvent`类中提供了一个方法让外部统一调用以执行事件

```csharp
public static bool Execute<T>(GameObject target, BaseEventData eventData, EventFunction<T> functor) where T : IEventSystemHandler
{
    //从对象池中取出一个IEventSystemHandler类型的元素
    var internalHandlers = s_HandlerListPool.Get();
    //获取指定对象(target)的事件,并保存在internalHandlers中
    GetEventList<T>(target, internalHandlers);
    //  if (s_InternalHandlers.Count > 0)
    //      Debug.Log("Executinng " + typeof (T) + " on " + target);
​
    var internalHandlersCount = internalHandlers.Count;
    for (var i = 0; i < internalHandlersCount; i++)
    {
        T arg;
        try
        {
            arg = (T)internalHandlers[i];
        }
        catch (Exception e)
        {
            var temp = internalHandlers[i];
            Debug.LogException(new Exception(string.Format("Type {0} expected {1} received.", typeof(T).Name, temp.GetType().Name), e));
            continue;
        }
​
        try
        {
            //执行事件
            functor(arg, eventData);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
​
    var handlerCount = internalHandlers.Count;
    s_HandlerListPool.Release(internalHandlers);
    return handlerCount > 0;
}
```

这个方法之前有讲过，主要就是查找`target`对象上的`T`类型的组件列表，并遍历执行。

除此之外，还有一个`GetEventHandler`方法，它主要是通过冒泡的方式查找到能够处理指定事件的对象。

```csharp
// 在游戏对象上冒泡指定的事件，找出哪个对象将实际接收事件。 
public static GameObject GetEventHandler<T>(GameObject root) where T : IEventSystemHandler
{
    if (root == null)
        return null;
​
    Transform t = root.transform;
    //冒泡查找,如果物体本身不能处理输入的事件,交予parent处理
    while (t != null)
    {
        if (CanHandleEvent<T>(t.gameObject))
            return t.gameObject;
        t = t.parent;
    }
    
    return null;
}
​
 // 指定的游戏对象是否能够处理指定的事件
 public static bool CanHandleEvent<T>(GameObject go) where T : IEventSystemHandler
 {
     var internalHandlers = s_HandlerListPool.Get();
     GetEventList<T>(go, internalHandlers);
     var handlerCount = internalHandlers.Count;
     s_HandlerListPool.Release(internalHandlers);
     return handlerCount != 0;
 }
```

比如我们在场景中创建一个`Button`，那这个`Button`还包含了Text组件，当鼠标点击的时候会调用`GetEventHandler`函数，该函数的`root`参数其实是`Text`，但是会通过冒泡的方式查找到它的父物体`Button`，然后调用`Button`的点击事件。

### **`Raycasters`**

事件系统需要一个方法来检测当前输入事件需要发送到哪里，这是由`Raycasters`提供的。 给定一个屏幕空间位置，它们将收集所有潜在目标，找出它们是否在给定位置下，然后返回离屏幕最近的对象。 系统提供了以下几种类型的`Raycaster`:

- **`Graphic Raycaster：` 检测`UI`元素**
- **`Physics 2D Raycaster：` 用于`2D`物理元素**
- **`Physics Raycaster：` 用于`3D`物理元素**

![](https://pic3.zhimg.com/80/v2-bd2f8a5e377ab90e25e1d70392f418ee_720w.webp)

`BaseRaycaster`是其他`Raycaster`的基类，这是是一个抽象类。在它`OnEnable`里将自己注册到`RaycasterManager`，并在`OnDisable`的时候从后者移除。

`RaycasterManager`是一个静态类，维护了一个`BaseRaycaster`类型的`List`，功能比较简单，包含获取(`Get`)、添加(`Add`)、移除(`Remove`)方法。

`BaseRaycaster`中最重要的就是`Raycast`方法了，它的子类都对该方法进行了重写。

### `Physics Raycaster`

它主要用于检测`3D`物理元素，并且保存被射线检测到物体的数据，下面是部分代码

```csharp
public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
{
    //判断是否超出摄像机的远近裁剪平面的距离
    if (!ComputeRayAndDistance(eventData, ref ray, ref displayIndex, ref distanceToClipPlane))
        return;
​
    //采用ReflectionMethodsCache.Singleton.raycast3DAll()来获取所有射线照射到的对象
    //用反射的方式把Physics.RaycastAll()方法缓存下来，让Unity的Physics模块与UI模块，保持低耦合，没有过分依赖。
    if (m_MaxRayIntersections == 0)
    {
        m_Hits = ReflectionMethodsCache.Singleton.raycast3DAll(ray, distanceToClipPlane, finalEventMask);
        hitCount = m_Hits.Length;
    }
    else
    {
        if (m_LastMaxRayIntersections != m_MaxRayIntersections)
        {
            m_Hits = new RaycastHit[m_MaxRayIntersections];
            m_LastMaxRayIntersections = m_MaxRayIntersections;
        }
​
        hitCount = ReflectionMethodsCache.Singleton.getRaycastNonAlloc(ray, m_Hits, distanceToClipPlane, finalEventMask);
    }
​
    //获取到被射线照射到的对象，根据距离进行排序，然后包装成RaycastResult,加入到resultAppendList中
    if (hitCount != 0)
    {
        if (hitCount > 1)
            System.Array.Sort(m_Hits, 0, hitCount, RaycastHitComparer.instance);
​
        for (int b = 0, bmax = hitCount; b < bmax; ++b)
        {
            var result = new RaycastResult
            {
                ...//为result赋值
            };
            resultAppendList.Add(result);
        }
    }
}
```

`Physics2DRaycaster`继承自`PhysicsRaycaster`，实现功能和方式基本一致，只不过是用于检测`2D`物体，这里不具体讲解

### `GraphicRaycast`

**`GraphicRaycast`用于检测`UI`元素，它依赖于`Canvas`，我们在场景中添加`Canvas`默认都会包含一个`GraphicRaycast`组件。它先获取鼠标坐标，将其转换为`Camera`的视角坐标，然后分情况计算射线的距离（`hitDistance`），调用`Graphic`的`Raycast`方法来获取鼠标点下方的元素，最后将满足条件的结果添加到`resultAppendList`中。**

![](https://pic4.zhimg.com/80/v2-8bd60abc5511daada3c336a9aee12193_720w.webp)

一大波代码来袭，不感兴趣可以跳过

```csharp
public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
{
    if (canvas == null)
        return;
​
    //返回Canvas上的所有包含Graphic脚本并且raycastTarget=true的游戏物体
    var canvasGraphics = GraphicRegistry.GetRaycastableGraphicsForCanvas(canvas);
​
    if (canvasGraphics == null || canvasGraphics.Count == 0)
        return;
​
    int displayIndex;
    //画布在ScreenSpaceOverlay模式下默认为null
    var currentEventCamera = eventCamera; // Property can call Camera.main, so cache the reference
​
    if (canvas.renderMode == RenderMode.ScreenSpaceOverlay || currentEventCamera == null)
        displayIndex = canvas.targetDisplay;
    else
        displayIndex = currentEventCamera.targetDisplay;
​
    //获取鼠标位置
    var eventPosition = Display.RelativeMouseAt(eventData.position);
    if (eventPosition != Vector3.zero)
    {
        int eventDisplayIndex = (int)eventPosition.z;
        
        if (eventDisplayIndex != displayIndex)
            return;
    }
    else
    {
        eventPosition = eventData.position;
    }
​
    // Convert to view space
    //将鼠标点在屏幕上的坐标转换成摄像机的视角坐标,如果超出范围则return
    Vector2 pos;
    if (currentEventCamera == null)
    {
        float w = Screen.width;
        float h = Screen.height;
        if (displayIndex > 0 && displayIndex < Display.displays.Length)
        {
            w = Display.displays[displayIndex].systemWidth;
            h = Display.displays[displayIndex].systemHeight;
        }
        pos = new Vector2(eventPosition.x / w, eventPosition.y / h);
    }
    else
        pos = currentEventCamera.ScreenToViewportPoint(eventPosition);
​
    // If it's outside the camera's viewport, do nothing
    if (pos.x < 0f || pos.x > 1f || pos.y < 0f || pos.y > 1f)
        return;
​
    float hitDistance = float.MaxValue;
​
    Ray ray = new Ray();
​
    //如果currentEventCamera不为空,摄像机发射射线
    if (currentEventCamera != null)
        ray = currentEventCamera.ScreenPointToRay(eventPosition);
​
    //如果当前画布不是ScreenSpaceOverlay模式并且blockingObjects != BlockingObjects.None
    //计算hitDistance的值
    if (canvas.renderMode != RenderMode.ScreenSpaceOverlay && blockingObjects != BlockingObjects.None)
    {
        float distanceToClipPlane = 100.0f;
​
        if (currentEventCamera != null)
        {
            float projectionDirection = ray.direction.z;
            distanceToClipPlane = Mathf.Approximately(0.0f, projectionDirection)
                ? Mathf.Infinity
                : Mathf.Abs((currentEventCamera.farClipPlane - currentEventCamera.nearClipPlane) / projectionDirection);
        }
        #if PACKAGE_PHYSICS
            if (blockingObjects == BlockingObjects.ThreeD || blockingObjects == BlockingObjects.All)
            {
                if (ReflectionMethodsCache.Singleton.raycast3D != null)
                {
                    var hits = ReflectionMethodsCache.Singleton.raycast3DAll(ray, distanceToClipPlane, (int)m_BlockingMask);
                    if (hits.Length > 0)
                        hitDistance = hits[0].distance;
                }
            }
        #endif
            #if PACKAGE_PHYSICS2D
            if (blockingObjects == BlockingObjects.TwoD || blockingObjects == BlockingObjects.All)
            {
                if (ReflectionMethodsCache.Singleton.raycast2D != null)
                {
                    var hits = ReflectionMethodsCache.Singleton.getRayIntersectionAll(ray, distanceToClipPlane, (int)m_BlockingMask);
                    if (hits.Length > 0)
                        hitDistance = hits[0].distance;
                }
            }
        #endif
    }
​
    m_RaycastResults.Clear();
​
    //调用Raycast函数重载
    Raycast(canvas, currentEventCamera, eventPosition, canvasGraphics, m_RaycastResults);
​
    //遍历m_RaycastResults，判断Graphic的方向向量和Camera的方向向量是否相交，然后判断Graphic是否在Camera的前面，并且距离小于等于hitDistance，满足了这些条件，才会把它打包成RaycastResult添加到resultAppendList里。
    int totalCount = m_RaycastResults.Count;
    for (var index = 0; index < totalCount; index++)
    {
        var go = m_RaycastResults[index].gameObject;
        bool appendGraphic = true;
​
        if (ignoreReversedGraphics)
        {
            if (currentEventCamera == null)
            {
                // If we dont have a camera we know that we should always be facing forward
                var dir = go.transform.rotation * Vector3.forward;
                appendGraphic = Vector3.Dot(Vector3.forward, dir) > 0;
            }
            else
            {
                // If we have a camera compare the direction against the cameras forward.
                var cameraForward = currentEventCamera.transform.rotation * Vector3.forward * currentEventCamera.nearClipPlane;
                appendGraphic = Vector3.Dot(go.transform.position - currentEventCamera.transform.position - cameraForward, go.transform.forward) >= 0;
            }
        }
​
        if (appendGraphic)
        {
            float distance = 0;
            Transform trans = go.transform;
            Vector3 transForward = trans.forward;
​
            if (currentEventCamera == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                distance = 0;
            else
            {
                // http://geomalgorithms.com/a06-_intersect-2.html
                distance = (Vector3.Dot(transForward, trans.position - ray.origin) / Vector3.Dot(transForward, ray.direction));
​
                // Check to see if the go is behind the camera.
                if (distance < 0)
                    continue;
            }
​
            if (distance >= hitDistance)
                continue;
​
            var castResult = new RaycastResult
            {
                ......
            };
            resultAppendList.Add(castResult);
        }
    }
```

上述代码中调用了`Raycast`函数重载，作用是向屏幕投射射线并收集屏幕下方所有挂载了`Graphic`脚本的游戏对象，该函数内容为：

```csharp
private static void Raycast(Canvas canvas, Camera eventCamera, Vector2 pointerPosition, IList<Graphic> foundGraphics, List<Graphic> results)
{
    // Necessary for the event system
    //遍历场景内Graphic对象(挂载Graphic脚本的对象)
    int totalCount = foundGraphics.Count;
    for (int i = 0; i < totalCount; ++i)
    {
        Graphic graphic = foundGraphics[i];
​
        // -1 means it hasn't been processed by the canvas, which means it isn't actually drawn
        if (!graphic.raycastTarget || graphic.canvasRenderer.cull || graphic.depth == -1)
            continue;
​
        //目标点是否在矩阵中
        if (!RectTransformUtility.RectangleContainsScreenPoint(graphic.rectTransform, pointerPosition, eventCamera, graphic.raycastPadding))
            continue;
​
        //超出摄像机范围
        if (eventCamera != null && eventCamera.WorldToScreenPoint(graphic.rectTransform.position).z > eventCamera.farClipPlane)
            continue;
​
        //调用符合条件的Graphic的Raycast方法
        if (graphic.Raycast(pointerPosition, eventCamera))
        {
            s_SortedGraphics.Add(graphic);
        }
    }
​
    s_SortedGraphics.Sort((g1, g2) => g2.depth.CompareTo(g1.depth));
    totalCount = s_SortedGraphics.Count;
    for (int i = 0; i < totalCount; ++i)
        results.Add(s_SortedGraphics[i]);
​
    s_SortedGraphics.Clear();
}
```

**函数中又调用了`Graphic`类的`Raycast`函数，它主要是做两件事，一件是使用`RectTransform`的值过滤元素，另一件是使用`Raycast`函数确定射线击中的元素。**`RawImage`、`Image`和`Text`都间接继承自`Graphic`。

![](https://pic2.zhimg.com/80/v2-b9be049e2a433d8a8437328d49e8dc4d_720w.webp)

```csharp
 public virtual bool Raycast(Vector2 sp, Camera eventCamera)
 {
     if (!isActiveAndEnabled)
         return false;
​
     //UI元素,比如Image,Button等
     var t = transform;
​
     var components = ListPool<Component>.Get();
​
     bool ignoreParentGroups = false;
     bool continueTraversal = true;
​
     while (t != null)
     {
         t.GetComponents(components);
         for (var i = 0; i < components.Count; i++)
         {
​
             Debug.Log(components[i].name);
             var canvas = components[i] as Canvas;
             if (canvas != null && canvas.overrideSorting)
                 continueTraversal = false;
​
             //获取ICanvasRaycastFilter组件(Image,Mask,RectMask2D)
             var filter = components[i] as ICanvasRaycastFilter;
​
             if (filter == null)
                 continue;
​
             var raycastValid = true;
​
             //判断sp点是否在有效的范围内
             var group = components[i] as CanvasGroup;
             if (group != null)
             {
                 if (ignoreParentGroups == false && group.ignoreParentGroups)
                 {
                     ignoreParentGroups = true;
                     raycastValid = filter.IsRaycastLocationValid(sp, eventCamera);
                 }
                 else if (!ignoreParentGroups)
                     raycastValid = filter.IsRaycastLocationValid(sp, eventCamera);
             }
             else
             {
                 raycastValid = filter.IsRaycastLocationValid(sp, eventCamera);
             }
​
             if (!raycastValid)
             {
                 ListPool<Component>.Release(components);
                 return false;
             }
         }
         //遍历它的父物体
         t = continueTraversal ? t.parent : null;
     }
     ListPool<Component>.Release(components);
     return true;
 }
```

这里也使用了`ICanvasRaycastFilter`接口中的`IsRaycastLocationValid`函数，主要还是判断点的位置是否有效，不过这里使用了Alpha测试。Image、Mask以及RectMask2D都继承了该接口。

![](https://pic4.zhimg.com/80/v2-d80ee9c4b4f5c61ef1c05a1fc793bfab_720w.webp)

```csharp
 public virtual bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
 {
     //小于阈值(alphaHitTestMinimumThreshold)的Alpha值将导致射线事件穿透图像。 
     //值为1将导致只有完全不透明的像素在图像上注册相应射线事件。
     if (alphaHitTestMinimumThreshold <= 0)
         return true;
​
     if (alphaHitTestMinimumThreshold > 1)
         return false;
​
     if (activeSprite == null)
         return true;
​
     Vector2 local;
     if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out local))
         return false;
​
     Rect rect = GetPixelAdjustedRect();
​
     // Convert to have lower left corner as reference point.
     local.x += rectTransform.pivot.x * rect.width;
     local.y += rectTransform.pivot.y * rect.height;
​
     local = MapCoordinate(local, rect);
​
     // Convert local coordinates to texture space.
     Rect spriteRect = activeSprite.textureRect;
     float x = (spriteRect.x + local.x) / activeSprite.texture.width;
     float y = (spriteRect.y + local.y) / activeSprite.texture.height;
​
     try
     {
         return activeSprite.texture.GetPixelBilinear(x, y).a >= alphaHitTestMinimumThreshold;
     }
     catch (UnityException e)
     {
         Debug.LogError("Using alphaHitTestMinimumThreshold greater than 0 on Image whose sprite texture cannot be read. " + e.Message + " Also make sure to disable sprite packing for this sprite.", this);
         return true;
     }
 }
```

### `EventData`

`EventData`用以存储事件信息，涉及到的东西不多，不展开讲解，层级关系如下图所示

![](https://pic1.zhimg.com/80/v2-a72d1e210d9964fe63a440b4172d84ac_720w.webp)

## **实战：为Button的点击事件添加参数**

在执行`Button`点击事件时，有些情况下我们需要获取触发事件的`Button`对象信息，这时可以自己实现一个`Button`点击事件

```csharp
/// <summary>
/// UI事件监听器(与Button等UI挂在同一个物体上）：管理所有UGUI事件，提供事件参数类
/// 若想看所有相关委托  自行查看EventTrigger类
/// </summary>
public class UIEventListener : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    //2.定义委托
    public delegate void PointerEventHandler(PointerEventData eventData);
​
    //3.声明事件
    public event PointerEventHandler PointerClick;
    public event PointerEventHandler PointerDown;
    public event PointerEventHandler PointerUp;
​
    /// <summary>
    /// 通过变换组件获取事件监听器
    /// </summary>
    /// <param name="transform"></param>
    /// <returns></returns>
    public static UIEventListener GetEventListener(Transform transform)
    {
        UIEventListener uIEventListener = transform.GetComponent<UIEventListener>();
​
        if (uIEventListener == null)
            uIEventListener = transform.gameObject.AddComponent<UIEventListener>();
​
        return uIEventListener;
    }
​
    //1.实现接口
    public void OnPointerClick(PointerEventData eventData)
    {
        //表示抽象的有  抽象类  接口（多类抽象行为）  委托（一类抽象行为）
        //4.引发事件
        if (PointerClick != null)
            PointerClick(eventData);
    }
​
    public void OnPointerDown(PointerEventData eventData)
    {
        PointerDown?.Invoke(eventData);
    }
​
    public void OnPointerUp(PointerEventData eventData)
    {
        PointerUp?.Invoke(eventData);
    }
}
```

使用的时候，我们只需要将它挂载到`Button`组件上，然后在`PointerClick`事件中添加自己的处理函数。

## **总结**

`Button`点击事件怎么触发的呢？首先是`EventSystem`在`Update`中调用当前输入模块的`Process`方法处理所有的鼠标事件，并且输入模块会调用`RaycastAll`来得到目标信息，通过冒泡的方式找到事件实际接收者并执行点击事件(这只是总体流程，中间省略很多具体步骤)。

下面是UGUI系列第二篇文章，UI重建。码字不易，欢迎点赞支持！ ❤️

[Ruyi Y：[UGUI源码二]Unity UI重建(Rebuild)源码分析47 赞同 · 2 评论文章![](https://pic1.zhimg.com/v2-244b1039604167076d24d5c091aecde8_180x120.jpg)](https://zhuanlan.zhihu.com/p/448293298)

![](https://pic2.zhimg.com/80/v2-46b1ca8927625fb9ffb2cf94092b4651_720w.webp)

最后来一张层级关系图

![](https://pic2.zhimg.com/80/v2-9a23d2a2f220e3e8100607ddc0999721_720w.webp)
