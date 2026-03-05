
我们都知道很多效果的制作离不开RT的使用，比如说Glow（无论是不是全屏的），比如说DOF（景深），比如说Distort（扭曲）等等等等。包括我之前介绍过的我自己写的残影效果。其实这些东西说白了都是基于获得的RT,经过N次Blur以后再加点零碎还原回去。记得我第一次看见Distort效果还是好多年前玩《英雄连》的时候。当时想破脑袋也不知道人家是怎么做出来的。现在这种东西基本上是个TA就能随手写出来了。

既然一切的基础是RT，那么就难免要和镜头打交道。在被Unity说不清道不明的文档折磨了千百遍之后，总算爬过了无数的坑，把自己想要的效果做了出来。

所以这里重点不想说某某效果是怎么做出来的，更不会把整个源代码都贴出来。主要还是想把实验过程当中遇到的问题总结一下。以下的内容会有些杂乱，每一个涉及到的问题之间的关联性都不大。很多都是前人已经总结过的东西我再做一遍笔记。

1、<mark class="hltr-cyan">关于cullingmask</mark>

2、<mark class="hltr-cyan">关于渲染回调函数的执行顺序</mark>

3、<mark class="hltr-cyan">关于RT的位数问题</mark>

4、<mark class="hltr-cyan">CommandBuffer的使用方法和适用范围</mark>

5、<mark class="hltr-cyan">关于RenderTexture.GetTemporary()</mark>和RenderTexture.ReleaseTemporary()的使用

### 1、关于cullingmask

这个玩意儿其实大家都知道是干什么用的，不做赘述。主要说一下在C#脚本里怎么控制它。当然关于这方面的资料百度能搜出一大把，关于位运算的更详细的介绍大家可以自己去看，我这里就做个简单的总结：

(1)什么都不渲染，值为0。什么都渲染，值为-1。

(2)这个东西最恶心的就是要转换两次。首先是<mark class="hltr-cyan">LayerName转换为LayerID</mark>。然后是<mark class="hltr-cyan">LayerID转换为LayerMask</mark>。

名字转ID用函数LayerMask.NameToLayer(反之为LayerMask.LayerToName)。ID转Mask用1<<layerID。

(3)开启用加法，关闭用减法。

举个例子，比如Nothing的时候(当前值为0)开启一个名为”abc”的层，代码就是：0 + (1 << LayerMask.NameToLayer(“abc”))。

反之Everthing的时候(当前值为-1)关闭名称”abc”为的层，代码就是：-1 - (1 << LayerMask.NameToLayer(“abc”))。

强烈提醒各位童鞋，这里的括号非常重要，如果不写的话会出问题！

### 2、关于渲染回调函数的执行顺序

关于这个问题，我直接放出几张图来说明：

![](https://pic4.zhimg.com/80/ee41fcaac2e19e9776d43cef9f7b2ecb_1440w.webp)

![](https://pic4.zhimg.com/80/fdf061e43292a778d1d32cf7839bd123_1440w.webp)

![](https://pic2.zhimg.com/80/5e69812557ed88efdfd674ad8fd6b4e5_1440w.webp)

  

这个流程图有几个地方需要注意：

首先，OnEnable是“夹在”Awake和Start之间的，这个很少有人注意到。

其次，游戏逻辑是在渲染发生之前的，换句话说Update/LateUpdate都结束之后，这一帧才会真正开始渲染。

第三，剩下的一堆关于渲染的回调函数真是有点壮观。其中在<mark class="hltr-cyan">OnPreCull</mark>(包括之前的步骤)，你还能再改变物体的Transform，错过这个村就没有这个店。而直到<mark class="hltr-cyan">OnPreRender</mark>(包括之前的步骤)，你还是可以改变物体的材质。

最后就是OnRenderImage放在渲染的最后——当然我想大家都已经很清楚了。

### 3、关于RT的变量depth

关于其RenderTexture的声明之中，depth(值为固定的0、16、24幸福三选一)这个值一直让我很疑惑。

以下引用官方文档的原话(大家都是吃这口饭的成年人，字幕什么的我就不发了)：

When 0 is used, then no Z buffer is created by a render texture.

16 means at least 16 bit Z buffer and no stencil buffer. 24 or 32 means at least 24 bit Z buffer, and a stencil buffer.

When requesting 24 bit Z Unity will prefer 32 bit floating point Z buffer if available on the platform.

我一直都不明白(也可能是因为我的理论水平太低，也可能是因为我的智商不够)，RT就是要一张图，你怎么往里塞
<mark class="hltr-cyan">Z Buffer</mark>和<mark class="hltr-cyan">Stencil Buffer</mark>?意义何在呢？

在跟Unity的Camera做不懈的斗争之中，我渐渐地理解了这东西的用意。我想这些buffer里的值并不是要塞到RT那张图里，而是渲染RT时暂时改变镜头的设置。如果选择0的话，物体之间是没有排序的，只适合于全部物体都指定了渲染顺序的情况。我想大概只有<mark class="hltr-cyan">2D游戏或者只渲染UI这两种特殊情况会选0</mark>。如果一个本来没有问题的场景，渲染出来的RT会出现明显得排序错误，那么在创建RT的时候看看是不是指定了Depth为0。一般游戏的RT选16。而当你在场景中使用了Stencil Buffer的时候，需要指定RT的depth为24，否则渲染出来的图不会有Stencil Buffer参与的痕迹。

另外需要特别注意的是：选择24的时候Z buffer的精度是"32 bit floating"。如果感到Z Buffer精度不够的话推荐使用值为24的depth。

### 4、CommandBuffer的使用方法和适用范围

CommandBuffer是Unity5新加的功能。我不太想写那些拗口的专业术语，简单来说就是允许你控制摄像机干点正常工作之外的“兼职”。在上一个版本的Unity里，摄相机什么时候工作，怎么渲染都是固定好的。留给你能改动的无非就是选选背景色，加个cullingmask这种无关痛痒的选项。

比如说之前要做外发光效果的时候，必须要单独设立一个摄像机去做RT，这个相机的属性其实和主相机没什么太大区别。但是你要为了维护这个东西写很多代码。有了<mark class="hltr-cyan">CommandBuffer就可以不必再建立额外的摄像机</mark>。CommandBuffer的好处就是让你给一个摄像机添加命令，好完成需要N个摄像机才能做到的工作。

关于CommandBuffer的使用方法，Unity官方有一个毛边玻璃的例子。代码不长，但是效果却很好。比起之前再架一个摄像机的方法好太多。

其中有几个地方值得说明一下：

第一、Unity里面RT的两个“身份”，一个是RenderToTextrue类型的变量，另一个只是一个int类型的ID(identifier)。很多相关的函数里都有这个identifier的变量。

第二、在镜头上添加CommandBuffer，需要注意“镜头事件”(CameraEvent)。这个东西是控制CommandBuffer执行的时机。比如在官方的例子里，CameraEvent用的是CameraEvent.AfterSkybox。指的就是在渲染完天空盒之后执行CommandBuffer中的命令。根据自己的需求安排CommandBuffer的执行时机。比如在非透明物体渲染之后、透明物体渲染之后、RenderImage之后等等。但是其中有很多坑。比如你的项目本身就没有使用Unity的SkyBox，那么指定了CameraEvent.AfterSkybox的CommandBuffer将不起作用。

第三、我认为使用CommandBuffer做RT最重要的语句：就是声明一个RT，并且在当前“镜头事件”触发的时候把屏幕中的物体渲染到这个RT中。大致的代码如下：

```csharp
CommandBuffer bufAfterForwardOpaque;
int RTAllGeoID;
RTAllGeoID = Shader.PropertyToID("_AllOpaqueTexture");
bufAfterForwardOpaque.GetTemporaryRT(RTAllGeoID, -1, -1, 0,FilterMode.Bilinear);
bufAfterForwardOpaque.Blit(BuiltinRenderTextureType.CurrentActive, RTAllGeoID);
```

第四、作为Unity的新特性，这东西还非常不完善。让我最蛋疼的就是没有RenderWithShader这种功能。就比如之前提到的外发光效果。如果只是简单的几个多边形，并且大家都是外发光，似乎效果不错。但是一旦出现了遮挡情况的发生，CommandBuffer就无能力了。这个时候还要重新回到老办法：架设一个新的镜头，配合RenderWithShader/SetReplacementShader这两个东西，在RT的时候把遮挡物体和目标物体都渲染一遍。

  

如果你对CommandBuffer比较了解，可能会想到替代RenderWithShader/SetReplacementShader的新玩具——DrawRenderer/DrawMesh。

我一开始也对这两个东西抱着极大的期望，但是试验下来大失所望。传入到这两个函数里的Renderer/Mesh并不会经过镜头剔除(Culling)。简单的来说你传进去多少就渲染多少。如果你遍历整个场景把所有的Renderer都传了进去。恭喜你成功地把游戏的drawcall翻了一倍。之前程序员使出吃奶力气一点一点优化的成果，都被你这一行代码彻底地变成无用功。

所以我翻遍google开始研究使用脚本做剔除。找到头都大了也没有什么好办法。别跟我提什么Renderer.isVisible，反正我试了半天法线没什么卵用。找到最后连AABB测试都翻出来了，心累。还有一个同样是Unity5新加的CullingGroup。说实话我也没怎么仔细研究。因为我发现我已经误入歧途。本来使用CommandBuffer的目的是想少架一个摄像机，是想让问题简化。结果越搞越复杂。最后的使用起来的效率反而还不如从前，何必呢。

所以我对CommandBuffer的看法是：对于一些简单的摄像机工作，可以用这个新东西代替老办法，比如你只想要一个摄像机视野内所有非blend物体的截图，或者只是针对几个固定的Renderer进行渲染RT。那么CommandBuffer无疑是非常好用的。如果处理一个极其复杂的场景，物体之间有着明显的遮挡关系，又或者你有这样或那样的需求，目前版本的CommandBuffer功能很难胜任。

### 5、关于RenderTexture.GetTemporary()和RenderTexture.ReleaseTemporary()的使用

这两对儿活宝是为了方便RT而设计的。真的很方便吗？用起来就知道真的是很麻烦。为了方便说明，我们先声明两个变量：

```text
RenderTexture rt0;
RenderTexture rt1;
```

第一个必须要知道的是：当rt0 = RenderTexture.GetTemporary()之后，rt0实际上会被分配任意一张在内存当中的图。如果你本来想要的结果莫名其妙的变成了一张人脸的贴图或者其他什么贴图，那么说明你并没有真正地把RT的结果赋值给rt0。所以偶尔在Scene视图里会出现贴图错误，原因在于此。

第二个关键问题是RenderTexture.GetTemporary()和RenderTexture.ReleaseTemporary()必须成对使用。如果没有Get,Release就会报错。最妙的是如果Get却没有Release。那么你的Unity很快就会因为内存用尽而崩溃。打开profiler，如果发现Total Objects In Scene这个数值一直再不断地增长，恭喜你中招了，赶紧回去查是哪个该死的RT没有Release掉。

![](https://pic3.zhimg.com/80/48a0e35d0ac186508608b5b1f0f19022_1440w.webp)

  

当然，这些都不算什么事儿。毕竟Get一下再Release一下，并不会浪费什么功夫。然而事实并非那么简单。比如下面这句：

```Csharp
//bloomRenderTexture是一张需要被模糊的物体的渲染图
// RenderImageMat是一个用来做blur的material。它的shader有两个pass，第一个pass是横向模糊一遍，第二个pass是纵向模糊一遍。Blur怎么做大家都应该知道，不再赘述。
public int blurLevel = 3;
rt0  = bloomRenderTexture;
rt1 = RenderTexture.GetTemporary(rtBloomWidth, rtBloomHeight);
for (int i = 0; i < blurLevel; i ++ )
{
    Graphics.Blit(rt0, rt1, RenderImageMat, 0);
    Graphics.Blit(rt1, rt0, RenderImageMat, 1);
}        			
RenderImageMat.SetTexture("_Bloom", rt0);
Graphics.Blit(src, dst, RenderImageMat, 2);
RenderTexture.ReleaseTemporary(rt1);
```

这个代码片段目的是多次Blur图片，逻辑上是不是很清楚。然而一运行起来Total Objects In Scene这个数值就开始暴涨。打开进程管理器，你就会惊喜地发现Unity的内存也同样在疯涨。明明我在使用之后调用了ReleaseTemporary()，依然没有任何作用。

为了解决这个让人崩溃的问题，我几乎试验了所有能想到的办法，最终无果。正当我怀疑人生的时候，我看到了Unity的更新文档，竟然发现了一条关于CommandBuffer的bug修复。虽然我上面的代码并没有用到CommandBuffer，但是我极度怀疑是Unity自身的问题造成了RT不能正确被Release的错误。

接下来就是漫长地下载、卸载、安装、版本升级。浪费了无数的时间，在5.4.0sp4版本下那个该死Total Objects In Scene终于稳定在一个正常的数值范围内。对此我只想用一句四个字的成语来表达我激动的心情：MDZZ。

所以如果你想使用RT来做一些酷炫的效果，我强烈推荐你升级到Unity的最新版本。

  

附录1：官方关于CommandBuffer的例子:

[https://blogs.unity3d.com/cn/2015/02/06/extending-unity-5-rendering-pipeline-command-buffers/](https://link.zhihu.com/?target=https%3A//blogs.unity3d.com/cn/2015/02/06/extending-unity-5-rendering-pipeline-command-buffers/)

  

```csharp
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

// See _ReadMe.txt for an overview
[ExecuteInEditMode]
public class CommandBufferBlurRefraction : MonoBehaviour
{
	public Shader m_BlurShader;
	private Material m_Material;

	private Camera m_Cam;

	// We'll want to add a command buffer on any camera that renders us,
	// so have a dictionary of them.
	private Dictionary<Camera,CommandBuffer> m_Cameras = new Dictionary<Camera,CommandBuffer>();

	// Remove command buffers from all cameras we added into
	private void Cleanup()
	{
		foreach (var cam in m_Cameras)
		{
			if (cam.Key)
			{
				cam.Key.RemoveCommandBuffer (CameraEvent.AfterSkybox, cam.Value);
			}
		}
		m_Cameras.Clear();
		Object.DestroyImmediate (m_Material);
	}

	public void OnEnable()
	{
		Cleanup();
	}

	public void OnDisable()
	{
		Cleanup();
	}

	// Whenever any camera will render us, add a command buffer to do the work on it
	public void OnWillRenderObject()
	{
		var act = gameObject.activeInHierarchy && enabled;
		if (!act)
		{
			Cleanup();
			return;
		}
		
		var cam = Camera.current;
		if (!cam)
			return;

		CommandBuffer buf = null;
		// Did we already add the command buffer on this camera? Nothing to do then.
		if (m_Cameras.ContainsKey(cam))
			return;

		if (!m_Material)
		{
			m_Material = new Material(m_BlurShader);
			m_Material.hideFlags = HideFlags.HideAndDontSave;
		}

		buf = new CommandBuffer();
		buf.name = "Grab screen and blur";
		m_Cameras[cam] = buf;

		// copy screen into temporary RT
		int screenCopyID = Shader.PropertyToID("_ScreenCopyTexture");
		buf.GetTemporaryRT (screenCopyID, -1, -1, 0, FilterMode.Bilinear);
		buf.Blit (BuiltinRenderTextureType.CurrentActive, screenCopyID);
		
        
		// get two smaller RTs
		int blurredID = Shader.PropertyToID("_Temp1");
		int blurredID2 = Shader.PropertyToID("_Temp2");
		buf.GetTemporaryRT (blurredID, -2, -2, 0, FilterMode.Bilinear);
		buf.GetTemporaryRT (blurredID2, -2, -2, 0, FilterMode.Bilinear);
		
		// downsample screen copy into smaller RT, release screen RT
		buf.Blit (screenCopyID, blurredID);
		buf.ReleaseTemporaryRT (screenCopyID); 
		
		// horizontal blur
		buf.SetGlobalVector("offsets", new Vector4(2.0f/Screen.width,0,0,0));
		buf.Blit (blurredID, blurredID2, m_Material);
		// vertical blur
		buf.SetGlobalVector("offsets", new Vector4(0,2.0f/Screen.height,0,0));
		buf.Blit (blurredID2, blurredID, m_Material);
		// horizontal blur
		buf.SetGlobalVector("offsets", new Vector4(4.0f/Screen.width,0,0,0));
		buf.Blit (blurredID, blurredID2, m_Material);
		// vertical blur
		buf.SetGlobalVector("offsets", new Vector4(0,4.0f/Screen.height,0,0));
		buf.Blit (blurredID2, blurredID, m_Material);

		buf.SetGlobalTexture("_GrabBlurTexture", blurredID);
        
        
        
        //buf.SetGlobalTexture("_GrabBlurTexture", screenCopyID);

		cam.AddCommandBuffer (CameraEvent.AfterSkybox, buf);
	}	

}