
[博客园](https://www.cnblogs.com/wwhhgg/p/12931173.html)

# [Unity3D性能优化之资源导入标准和属性设置篇](https://www.cnblogs.com/wwhhgg/p/12931173.html)

* # 一、Unity使用的资源

## 1、外部资源：

 **不是Unity创建** ，而是外部工具做的模型以及贴图或通用的格式资源。例如图片资源、模型资源、动画资源、视频和声音资源。

## 2、内部资源：

 **Unity创建的** ，并且只有放在Unity才能识别。例如脚本、Shader、场景、预设、材质、精灵、动画控制器、时间线、物理材质等。

* # 二、Unity资源导入标准

## 1、纹理

* **关闭Read/Write 选项的** ：Read/Write Enabel 选项启用后， 选择这个选项将会允许从脚本来访问纹理数据， 开启此
  选项后， 将会产生纹理数据的副本， 副本会占用内存， 等同于一个纹理数据会有双倍的内
  存消耗。
* **纹理按照平台压缩格式** ：RGB32和RGB24等非压缩格式纹理占用内存较大。带有OpenGLES3或更高版本的Android的纹理格式应该是astc。另外Android按ETC2，IOS按PVRTC，PC按DXT5
* **过滤模式Filter Mode设置为 Bilinear的纹理：** Trilinear 三线性过滤（三线性插值） ， 纹理会在不同的 mip 水平之间进行模糊 ， 从而增
  加 GPU 开销。
* **Sprite纹理关闭Mipmap：** Mipmap 开启后， 内存会是未开启 Mipmap 的 1.33 倍， 因为 Mipmap 会生成一组长宽依
  次减少一倍的纹理序列， 一直生成到 1*1。Mipmap 提升 GPU 效率； 一般用于 3D 场景或角色， UI 不建议开启。UI用的纹理关闭，3D模型和场景用到的纹理开启。
* **Wrap 模式设置为 Clamp：** Wrapmode 使用了 Repeat 模式， 容易导致贴图边缘出现杂色

## 2、模型

* **关闭Read/Write 选项：** Read/Write Enabel 选项启用后， 选择这个选项将会允许从脚本来访问网格数据，开启此选项后，将会产生网格数据的副本，副本会占用内存，等同于一个网格数据会有更多的内存消耗。一个用于呈现，一个用于系统内存中的脚本访问（如果要进行代码操作动态合批，就是开启这个选项）
* **Mesh Compression选项：** 设置为High
* **开启 OptimizeMesh 选项：** 对于模型相关， 开启后， Unity 会对其进行网格优化， 提高。
* **Tangent 切线属性：** 无必要不导入
* **Normal 法线属性：** 无必要不导入
* **Color 属性：** （顶点色）无必要不导入
* **UV2 属性：** 常规情况只会用到 UV1， UV2 一般不会用到，无必要不导入

## 3、音频

* **长音频的Load Type用Streaming加载：** Streaming 选项开启后， 音频加载方式变为边播放边读取，能明显降低内存，背景音乐使用这种方式。

## **4、组件**

* **Animator 组件**开启 OptimizeGameObjects：**** OptimizeGameObject 可以有效降低动画开销
* **SkinnedMeshRenderer：** 不开启MotionVector，启用Skinned Motion Vectors将以消耗双倍内存为代价提高蒙皮网格的精度
* **不可见的 Image 组件：** alpha=0 的 Image 组件依然会参与渲染，需要去掉
* **不可见的 RawImage 组件：** alpha=0 的 RawImage 组件依然会参与渲染，需要去掉
* **使用了 Outline 组件：** 使用 Outline 效果会增加 4 倍的顶点数， 造成较高的重建开销， 可用 shadow 替代。

## **5、脚本资源

* **OnGUI 方法：** OnGUI 方法会造成较多的堆内存分配， 耗时也较高， 不建议使用。
* **空的 Update 方法：** 空 Update 方法在运行依然会被调用， 导致额外的开销， 建议去除。
* **存在.tag 的调用：** .tag 的使用会导致堆内存分配， 建议使用 CompareTag 进行替换。

### 最新随笔

* [1.Real - time Rendering 实时计算机图形学](https://www.cnblogs.com/wwhhgg/p/13268682.html)
* [2.计算机图形学-渲染管线](https://www.cnblogs.com/wwhhgg/p/13025584.html)
* [3.计算机图形学-概念](https://www.cnblogs.com/wwhhgg/p/13025576.html)
* [4.Uinty3D性能优化之声音资源科普篇](https://www.cnblogs.com/wwhhgg/p/13025544.html)
* [5.Unity3D性能优化之资源处理工具篇](https://www.cnblogs.com/wwhhgg/p/12937820.html)
* [6.Unity3D性能优化之资源分析工具篇](https://www.cnblogs.com/wwhhgg/p/12966916.html)
* [7.Unity3D项目性能优化实践经验总结](https://www.cnblogs.com/wwhhgg/p/12965891.html)
* [8.Unity3D性能优化之资源原理科普篇](https://www.cnblogs.com/wwhhgg/p/12937944.html)
* [9.Unity3D性能优化之资源导入标准和属性设置篇](https://www.cnblogs.com/wwhhgg/p/12931173.html)
* [10.C#图解教程思维导图](https://www.cnblogs.com/wwhhgg/p/12850597.html)

### 随笔分类

* [Lua(1)](https://www.cnblogs.com/wwhhgg/category/1572882.html)
* [Shader(1)](https://www.cnblogs.com/wwhhgg/category/1572880.html)
* [Unity3D(2)](https://www.cnblogs.com/wwhhgg/category/1572881.html)
* [Unity3D性能优化(9)](https://www.cnblogs.com/wwhhgg/category/1768399.html)
* [读书笔记(4)](https://www.cnblogs.com/wwhhgg/category/1759547.html)
* [数据结构(1)](https://www.cnblogs.com/wwhhgg/category/1680055.html)
* [思维导图(4)](https://www.cnblogs.com/wwhhgg/category/1681711.html)
* [图形学(6)](https://www.cnblogs.com/wwhhgg/category/1687735.html)
* [语言原理(2)](https://www.cnblogs.com/wwhhgg/category/1687688.html)

### 随笔档案

* [2020年7月(1)](https://www.cnblogs.com/wwhhgg/archive/2020/07.html)
* [2020年6月(3)](https://www.cnblogs.com/wwhhgg/archive/2020/06.html)
* [2020年5月(7)](https://www.cnblogs.com/wwhhgg/archive/2020/05.html)
* [2020年4月(11)](https://www.cnblogs.com/wwhhgg/archive/2020/04.html)
* [2020年3月(7)](https://www.cnblogs.com/wwhhgg/archive/2020/03.html)

### 阅读排行榜

* [1. C#图解教程(11035)](https://www.cnblogs.com/wwhhgg/p/12845398.html)
* [2. Unity3D地图编辑器(5431)](https://www.cnblogs.com/wwhhgg/p/12579450.html)
* [3. Lua实现面向对象两种方法(4095)](https://www.cnblogs.com/wwhhgg/p/12606677.html)
* [4. C#图解教程思维导图(2317)](https://www.cnblogs.com/wwhhgg/p/12850597.html)
* [5. Unity3D性能优化之美术资源制件规范(2154)](https://www.cnblogs.com/wwhhgg/p/12704216.html)

### 评论排行榜

* [1. Unity3D地图编辑器(1)](https://www.cnblogs.com/wwhhgg/p/12579450.html)

### 推荐排行榜

* [1. Unity3D性能优化之资源处理工具篇(1)](https://www.cnblogs.com/wwhhgg/p/12937820.html)

### 最新评论

* [1. Re:Unity3D地图编辑器](https://www.cnblogs.com/wwhhgg/p/12579450.html)
* 开源吗大佬
* --clqss

Copyright © 2022 学习使我进步** **
Powered by .NET 6 on Kubernetes
