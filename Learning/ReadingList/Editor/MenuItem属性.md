[Unity Editor 基础篇（十一）：MenuItem属性\_unityeditor.menuitem-CSDN博客](https://blog.csdn.net/qq_33337811/article/details/72852342)
1.为Unity添加菜单项

使用方法：

MenuItem(string itemName, bool isValidateFunction, int priority) 
itemName：菜单名称路径 
isValidateFunction：不写为false，true则点击菜单前就会调用 
priority：菜单项显示排序
第一个参数为菜单路径名称，可添加菜单快捷键：(空格后加如下符号设置) 

_w 单一的快捷键 W 
%w ctrl+w 
&w Alt+w
例如：
[MenuItem("GameObject/caymanwindow # w")]  
//这样就可以自定义快捷键调用方法，相当于点击了菜单项

第三个参数为优先级，表示上下排列的顺序，小的在上，不写则默认为1000。

例如：

[MenuItem("EamTools/T1",false,1)]
    static void T1()
    {
 
    }
    [MenuItem("EamTools/T2",false,2)]
    static void T2()
    {
 
    }
    [MenuItem("EamTools/T3",false,0)]
    static void T3()
    {
 
    }
效果：顺序受参数影响了


另外，如果相邻的两个的priority参数值相差>=11，则认为是不同组的，中间会有线分割显示：
 [MenuItem("EamTools/T1",false,1)]
    static void T1()
    {
 
    }
    [MenuItem("EamTools/T2",false,12)]
    static void T2()
    {
 
    }
    [MenuItem("EamTools/T3",false,0)]
    static void T3()
    {
 
    }


第二个参数，默认为false，设为ture则击菜单前就会调用 如：检测按钮是否要显示(要谢两个方法，一个参数true控制按钮显示，一个参数false控制点击要做的事)
 [MenuItem("GameObject/caymanwindow", true)]   //用于判断按钮什么时候显示
    static bool ValidateSelection()
    {
        return Selection.activeGameObject != null;
    }
    [MenuItem("GameObject/caymanwindow", false)]   //点击按钮要做的事
    static void Show()
    {
        Debug.Log("Show:"+Selection.activeGameObject.name);
    }
2.为组件添加菜单项

1/ 使用方法：

[MenuItem("CONTEXT/组件名/显示方法名")]
这是个固定写法。
把“组件名“对应的脚本挂在物体上，则这个脚本组件右击后显示的上下文里便有了这样的菜单显示。

如：

 [MenuItem("CONTEXT/Peoplr/InitMe")]
    static void InitMe()
    {
        Debug.Log("Init");
    }


然后引入一个MenuCommand类：http://www.ceeger.com/Script/MenuCommand/MenuCommand.html

有两个变量：

context：上下文是一个菜单命令的目标对象。获取到的就是这个脚本组件对象。

userData：传递自定义信息到一个菜单项的整数。


在点击菜单调用的静态方法中加入MenuCommand参数，点击时系统便会自动的传入点击的脚本对象菜单进去：

 [MenuItem("CONTEXT/Peoplr/InitMe")]
    static void InitMe(MenuCommand cmd)
    {
        Peoplr po = cmd.context as Peoplr;
        po.a = 5;
        Debug.Log("Init");
    }
然后我们获取到脚本组件对象，修改公开变量的值，形成初始化。

2/ 还有一种方法：ContextMenu内置属性

注意事项：

1.必须是放在 Assets / Editor 文件夹下的类，且使用了 using UnityEditor 
2.调用的函数必须是静态函数（static）