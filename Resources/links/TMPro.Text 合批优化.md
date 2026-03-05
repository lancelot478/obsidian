#Muffin

导航栏的五个Text，使用的是TMPro.Text组件，但是并没有合批，主要是以下几个问题

# 一、因为字体资产不包含常用汉字导致的合批失败

## 背景

1. TMP无法直接使用TTF字体，使用的是TMP_FontAssets
    
2. 它提供了对应的工具，可以通过TTF生成字体资产文件
    
3. 资产文件也存在一些配置，可以指定动态字体还是静态。
    
    1. 如果是静态，显示的文本在资产里面找不到就不显示了
        
    2. 如果是动态，显示的文本在资产里面找不到会创建动态图集
        

## 现状

项目内的字体只包含了几十个汉字，且是动态字体。

因为无法涵盖大部分常用汉字，导致很多使用TMPro.Text组件的地方都自动创建了动态图集，无法合批。

## 解决方案

（可选，目前已执行）

扩充字体资产的字符，导入常用的3500个汉字。

以一次性固定内存的代价，换取运行时的性能损耗。

# 二、因为ViewBase内的CheckMat函数导致的合批失败

在ViewBase内，每个界面初始化，都会调用`GlobalFun.CheckMat`

此函数会搜索界面下的所有`TMPro.Text`组件，并且访问组件上的`fontMaterial`属性

此属性被访问，将会创建一个新的Material实例，导致每个`TMPro.Text`组件使用的都是不同的Material，导致无法合批

# 三、因为代码动态设置描边选项导致的合批失败



- `tmpText.outlineWidth = 0.1`
    
- `tmpText.outlineColor = Color32(98, 85, 76, 153)`
    

分别会触发TMPro.Text的以下代码
 
其中比较关键的内容就是：**This will results in an instance of the material.**

所以`MainInterfaceView:SetTextState`函数每次执行的时候，都会创建两个新的Material实例，自然也是无法合批的。

## 解决方案

`TMPro.Text`的字体资产可以创建**Material Preset**，建议代码动态切换Preset的Material，而非直接修改Material参数。

# 

