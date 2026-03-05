[Recast Navigation源码分析：导航网格Navmesh的生成原理 - 知乎](https://zhuanlan.zhihu.com/p/592339133)

本文的主要工作是结合Recast Navigation源码，详细分析导航网格的生成原理，为后续导航寻路打下理论基础。再理解了Recast的Navmesh生成原理后，对我们学习UE引擎的Navmesh模块有很大的帮助，方便我们更好的使用和改进UE引擎。

由于本文篇幅较长，一共三万多字，建议通过大纲跳转到你感兴趣的模块。

## Recast Navigation简介

在3D游戏中，导航寻路常用的是第三方开源库 Recast Navigation

![](https://pic3.zhimg.com/80/v2-c5d57e7991a73998d960a1d9a9ef045e_1440w.webp)

最初版的作者 Mikko Mononen 多年前曾经是 [Crytek工作室](https://link.zhihu.com/?target=https%3A//link.jianshu.com/%3Ft%3Dhttps%253A%252F%252Fzh.wikipedia.org%252Fwiki%252FCrytek) 的 AI 工程师。 [CryEngine](https://link.zhihu.com/?target=https%3A//link.jianshu.com/%3Ft%3Dhttps%253A%252F%252Fzh.wikipedia.org%252Fwiki%252FCryENGINE)、[孤岛惊魂](https://link.zhihu.com/?target=https%3A//link.jianshu.com/%3Ft%3Dhttps%253A%252F%252Fzh.wikipedia.org%252Fwiki%252F%2525E6%2525A5%2525B5%2525E5%25259C%2525B0%2525E6%252588%2525B0%2525E5%25259A%25258E) 就都是 Crytek 工作室开发的。源码地址：[https://github.com/recastnavigation/recastnavigation](https://link.zhihu.com/?target=https%3A//github.com/recastnavigation/recastnavigation)

RecastNavigation 是一个的导航寻路工具集，它包括了几个子集：

1. **Recast**：负责根据提供的模型生成导航网格。
2. **Detour**：利用导航网格做寻路操作。这里的导航网格可以是 Recast 生成的，也可以是其他工具生成的。
3. **DetourCrowd**：提供了群体寻路行为的功能。
4. **Recast Demo**：一个很完善的 Demo，基本上将 Recast 、 Detour 提供的功能通过可视化，展现了出来。弄懂了这个 Demo 的功能，基本也就了解了 RecastNavigation 究竟可以干什么事。

### 源网格数据分析

Navmesh是根据场景中的源网格数据，经过一定步骤和算法生成的，所以在生成navmesh之前，我们先了解一下源网格数据的格式。

从RecastDemo/Bin/Meshes目录下，随便打开一个obj格式的文件，里面保存了源网格数据的顶点，顶点索引，顶点法线等信息

![](https://pic3.zhimg.com/80/v2-8915346537e167f8dd017e160dee2c02_1440w.webp)

经过读取后，我们关心其中的顶点数据，顶点数，三角形数据，三角形数等数据，如下图所示：

![](https://pic3.zhimg.com/80/v2-68a5bf811c3dab67a00451239e3d4f4a_1440w.webp)

举例说明，假设有5个顶点，3个三角形：

![](https://pic4.zhimg.com/80/v2-e98a838378a443f2921721e29614226b_1440w.webp)

- 5个顶点的坐标用float* verts[3 * nverts]数组存储，分别表示nverts个顶点的x、y、z坐标，nverts的值为5
- 3个三角形用int* tris[3 * ntris]数组存储，分别表示ntris个三角形的（3*ntris）个顶点在verts数组中的下标，ntris的值为3。
- 在这个例子中，verts[3 * nverts]数组的内容是[x0,y0,z0,x1,y1,z1,x2,y2,z2,x3,y3,z3,x4,y4,z4]
- 而tris[3 * ntris]数组的内容是[0,1,2,0,2,3,2,3,4]

Recast打开InputMesh后，或者build namesh后可视化绘制IDraw Input Mesh，可以显示原始mesh数据，如下图所示：

![](https://pic1.zhimg.com/80/v2-7315d9b418871987313a53ded98fa508_1440w.webp)

### 什么是Navmesh

Navmesh又叫导航网格，导航网格是由多个凸多边形（Convex Polygon, Poly Mesh）组成的，它是标记哪些地方可行走的多边形网格数据结构，在这些网格的基础上通过一系列计算来实现自动寻路。 NavMesh 是由 Mesh 经过体素化、地区生成、轮廓生成、多边形网格生成、高度细节生成等步骤 生成的。

### 寻路的基本概念

在导航网格中的寻路是以 Poly 为单位的，在同个 Poly 中的两点，在忽略地形高度的情况下， 是可以直线到达的；如果两个点位于不同的 Poly，那么就会利用导航网格 + 寻路算法（比如A*算法）算出需要经过的 Poly，再算出具体路径。

### Recast可以生成哪些Navmesh

细分Recast生成的Navmesh种类，有三种：

1. Solo Mesh，纯粹的邻接凸多边形集合，以整张地图为管理单位，理解为一个大tile
2. Tile Mesh，基于tile划分的N个邻接凸多边形集合，整张地图划分为多个tile，以tile作为管理的最小单位
3. Temp Obstacles，管理方式同Tile-Mesh，但是其保留了体素数据，支持动态阻挡  
    以上3种各有各的适合场景，按业务需求选择，或结合之。

对应的源码：

![](https://pic4.zhimg.com/80/v2-1433e724e3514e11fc62358f940cdebb_1440w.webp)

### RecastDemo工具基础操作

### 如何生成Navmesh

这里以生成SoloMesh为例， 在右侧`Properties`选中`Sample`为`Solo Mesh`，`Input Mesh`为`nav_test.obj`，

![](https://pic3.zhimg.com/80/v2-2df43fb88b8baf4ea892c04ab829a602_1440w.webp)

下拉点击Build按钮，就能看到生成navmesh的结果如下：

![](https://pic2.zhimg.com/80/v2-e3533e71fddba253aa9b79c22fc56809_1440w.webp)

### 如何测试寻路路径

在左侧的`Tools`栏下，点击`Test NavMesh`，然后在地表上用右键（shift+左键）以及左键分别标定起始位置和结束位置，就能够直接看到寻路路径生成的结果

![](https://pic2.zhimg.com/80/v2-e6f32e198e165e818d60f9e5d97c1a65_1440w.webp)

左侧的`Tools`栏里面，点选`Pathfind Straight`，可以看到寻路路径的点位连接

![](https://pic4.zhimg.com/80/v2-6e74a9dec889f27d085864ec17fa8e53_1440w.webp)

点选`Pathfind Sliced`，可以看到寻路查找的整个过程。

![动图封面](https://pic4.zhimg.com/v2-5a6a11c78bbc828cda6603abf0f9634b_b.jpg)

## 生成Navmesh的流程

Recast 中的模块用于创建网格数据，Detour会用这些数据创建导航网格。

创建导航网格需要好多步骤， 一般流程如下:

1. 体素化（Voxelization）：从源几何结构创建一个实体高度场（Solid Heightfield）
2. 筛选可走表面（Filter walkables surfaces） ，进一步过滤可行走表面
3. 划分可走表面为简单区域（ Partition walkable surface to simple regions）：检测实体高度场的顶部表面，并将其划分为由相邻span组成的区域
4. 生成轮廓（Contour Generation）：检测区域的轮廓，并将它们形成[简单多边形（Polygon）](https://link.zhihu.com/?target=https%3A//en.wikipedia.org/wiki/Polygon%23Convexity_and_types_of_non-convexity)
5. 生成多边形网格（Convex Polygon Generation）：将轮廓细分为凸多边形
6. 生成详细网格（Detailed Mesh Generation）：将多边形网格[三角形化](https://link.zhihu.com/?target=https%3A//mathworld.wolfram.com/Triangulation.html)，并添加高度细节

对应源码函数是：

```cpp
bool Sample_SoloMesh::handleBuild()
bool Sample_TileMesh::handleBuild()
bool Sample_TempObstacles::handleBuild()
```

1. 准备三角面
2. 构建 rcHeightfield：描述高度域阻挡，rcCreateHeightfield，rcAllocHeightfield
3. 构建 rcCompactHeightfield：描述无阻挡高度域
4. 构建 rcContourSet：描述所有轮廓的集合
5. 构建 rcPolyMesh：描述多边形网格，用于构建导航网格（未三角化的凸多边形集合）
6. 构建 rcPolyMeshDetail：三角面网格
7. 用 rcPolyMesh 和 rcPolyMeshDetail 构建 Detour 的导航网格

## 体素化（Voxelization）

体素化（Voxelization）表示再形成过程。也叫光栅化，将基于边界的表现形式（例如：多边形模型，曲面等）转换成为体积的表现形式（例如：体素块）。

### 高度场（Heightfield）简介

高度场是容纳所有体素格子的AABB包围盒，为了理解生成导航网格的过程，首先了解如何使用**高度场（heightfields）**来表示[体素（voxel ）](https://link.zhihu.com/?target=https%3A//en.wikipedia.org/wiki/Voxel)数据是很重要的。高度场提供了良好的压缩和数据结构，这对于从几何图形中提取上表面信息特别有用。

### 基本的高度场结构

考虑[欧几里得空间](https://link.zhihu.com/?target=http%3A//en.wikipedia.org/wiki/Euclidean_space)中任意位置的轴对齐的盒子，它的边界由最小和最大顶点定义：

![](https://pic4.zhimg.com/80/v2-9b5192f7aead6b9a2d0cc6c5293eeb57_1440w.webp)

现在将盒子切成宽度和深度相同的垂直列，这些列构成了一个网格：

![](https://pic2.zhimg.com/80/v2-e19eaa601c63e1053ae106f0d5d6b7ad_1440w.webp)

现在沿高度轴（竖轴）以均匀的增量对列进行切片，将列分成与轴对齐的小盒子。 这种结构很好地表示了体素空间：

![](https://pic3.zhimg.com/80/v2-257be653cedb22ba30817aedef19d80a_1440w.webp)

### CellSize对高度场的影响

较低的值使得生成的网格更接近源几何形状，但需要更高的处理和内存成本。

**示例**：cellSize与体素场的关系。

![](https://pic4.zhimg.com/80/v2-d26f71589377d111c6eb123dcaf4c723_1440w.webp)

有时你可能会注意到障碍物的网格边界出乎意料地宽，或物体一侧与另一侧的边界的宽度不同。 这是网格生成过程的固有行为。 看一下上面体素场的可视化，最终网格中的顶点只能存在于体素的角上。 顶点被对齐到体素网格，体素尺寸越大，最终网格顶点的潜在 xz 平面偏移就越大，边界“错误”就越明显。

**边界示例 1**：这个网格是使用较大的cellSize生成的。

![](https://pic1.zhimg.com/80/v2-efa6cb9fc1c60dada4ee5985f7e771c8_1440w.webp)

**边界示例2**：这个网格是使用更小的cellSize生成的。所以偏移的表现并不明显（尽管从技术上讲，它仍然存在）。

![](https://pic1.zhimg.com/80/v2-d0d89cd269bde5b753059816e05995c0_1440w.webp)

### CellHeight对高度场的影响

较小的值使得最终的网格更接近源几何形状，代价是更高的处理成本。与cellSize不同，使用较低的cellHeight值不会显著增加内存使用。

这是一个核心配置值，影响几乎所有其他参数。walkableHeight、walkableClimb 和 detailSampleMaxError需要大于这个值才能正常工作。walkableClimb特别容易受到cellHeight值的影响。

示例：cellHeight与体素场的关系。

![](https://pic3.zhimg.com/80/v2-e8ea042d8cd21f405926bc5fd3867e72_1440w.webp)

与 cellSize 一样，cellHeight 控制顶点的位置。 最终网格的表面将“对齐”到包含源几何体的体素的顶部，而不是几何体本身。 这是网格生成过程的固有行为。看一下上面体素场的可视化，最终网格中的顶点只能存在于体素的角上，所以顶点被对齐到体素网格。体素的高度越大，顶点与实际源几何体的在y轴可能的偏移量就越大。

**示例1**：这个细节网格是使用较大的cellHeight生成的。

![](https://pic3.zhimg.com/80/v2-825b3c5f26b38f773a7f7eacf8c54e4e_1440w.webp)

**示例2**：这个细节网格是使用更小的cellHeight生成的。因此，偏移的表现并不明显。

![](https://pic2.zhimg.com/80/v2-de8da4cf6f32471e3545c045c1479b55_1440w.webp)

### 高度Span（Height Spans）

Solid Span的概念

考虑一列体素， 每个体素定义的区域要么是实体的**（solid）**，代表有障碍的空间，要么是开放的**（open）**，代表不包含任何障碍的空间：

![](https://pic1.zhimg.com/80/v2-0f24545e12d9a7b596c4642e48253460_1440w.webp)

通常我们只关心高度场的实体区域。所以我们合并列内连续的实体体素，得到一个实体体素的跨度（span），使得结构更简单。这是一个“**高度跨度（heightspan）**”或简称“**跨度（span）**”：

![](https://pic4.zhimg.com/80/v2-d42b4b80ebea6b23e2b695428e4ccf93_1440w.webp)

Solid Span在源码里的数据结构（rcSpan）

![](https://pic1.zhimg.com/80/v2-08d2b59ad8551408617d02bba77765c0_1440w.webp)

注意到Span中有一个area字段，该字段在体素化的这一步，作为“是否可走”的特殊标识

将span放入高度场结构中，我们得到一个实体高度场。这是该区域内阻塞区域的一种表示方式：

![](https://pic2.zhimg.com/80/v2-958e160e61cc444245eae0680866b8f5_1440w.webp)

### 搜索邻居

高度场提供了一些操作，这些操作使得遍历所有span和列中的span变得容易，但是有些算法通常需要执行邻居搜索。单元格列（Cell columns）有邻居定义：

![](https://pic4.zhimg.com/80/v2-8fa626543a541ebfcb6c6be7bc0f7b4f_1440w.webp)

Span可以有邻居，只有当邻居span在高度上与当前span足够接近时才被认为是邻居。只有开放的高度场实现提供了一种方法来搜索邻居span…

![](https://pic2.zhimg.com/80/v2-bb062a1c12000748d455681c9f803131_1440w.webp)

邻居通过它们从当前列或当前span开始的偏移量来引用。例如(referenceWidthIndex+widthOffset, referenceDepthIndex+depthOffset)

### 轴邻居（Axis-neighbors）

轴邻居（Axis-neighbors）是沿宽度轴/深度轴偏移的四个邻居:(- 1, 0) (0, 1) (1, 0) (0, -1)。

在所有的高度场实现中，轴邻居的搜索都以标准方式执行，从 (0, -1) 开始并使用标准索引沿顺时针方向进行。

|Index|Neighbor Offset|
|---|---|
|0|(-1, 0)|
|1|(0, 1)|
|2|(1, 0)|
|3|(0, -1)|

![](https://pic1.zhimg.com/80/v2-84477c3da165d3edfaa189396475cea8_1440w.webp)

### 对角线邻居（Diagonal-neighbors ）

对角线邻居（Diagonal-neighbors ）是四个沿着对角线轴偏移的邻居:(- 1, 1) (1, 1) (1, -1) (-1, -1)：

![](https://pic1.zhimg.com/80/v2-44c3a74008be8609866db894536a25e8_1440w.webp)

对角线邻居没有标准的搜索索引，通常的做法是顺时针查找轴邻居对应的对角线邻居：

![](https://pic3.zhimg.com/80/v2-5128b45b42c8d9da707271877b93de16_1440w.webp)

### 创建实体高度场（Solid Heightfield）

构建导航网格的第一阶段是使用体素化（voxelization）创建实体高度场（solid heightfield）。在体素化过程中，源几何结构被抽象为一个表示障碍空间的高度场。

源几何结构中的每个三角形都使用**保守体素化**的方式进行体素化并添加到高度场中。保守体素化是一种确保多边形表面完全被生成的体素包围的算法。下面是使用保守体素化包围三角形的一个例子：

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

体素化后，实体高度场包含了将源几何结构中所有多边形表面完全包围的span。在检测到源网格的轴对齐包围盒，并创建实体高度场以保存体素信息后，我们对源网格中的每个多边形执行以下过程：

### 裁剪多边形的表面是否是可行走（RC_WALKABLE_AREA）

最后一段数据是根据span的顶部体素所表示的几何体的斜率（slope）和 **walkableSlopeAngle **的值确定的（可通过的最大坡度slope，以度为单位）。如果源多边形的 y 斜度低于配置的设置（例如 45 度），则它的表面是可穿越的。（坡度足够低，使得agent可以通过）。

**示例 1**：正确设置这个值可以让网格向上延伸一个可通过的坡道。

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

**示例 2**：将该值设置得太低会阻止网格向上延伸到应该可通过的斜坡。

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

计算坡度是否满足要求的代码：**rcMarkWalkableTriangles**，

```cpp
/// The maximum slope that is considered walkable. [Limits: 0 <= value < 90] [Units: Degrees] 
   float walkableSlopeAngle;

//标记可行走三角面
void rcMarkWalkableTriangles(rcContext* ctx, const float walkableSlopeAngle,
							 const float* verts, int nv,
							 const int* tris, int nt,
							 unsigned char* areas)
{
	rcIgnoreUnused(ctx);
	rcIgnoreUnused(nv);
	
	const float walkableThr = cosf(walkableSlopeAngle/180.0f*RC_PI);
       // 三角形面的法向量
	float norm[3];
	
	for (int i = 0; i < nt; ++i)
	{
               // 三角形三个顶点的索引
		const int* tri = &tris[i*3];
               // 计算垂直与三角形面的法向量，传入的参数是三角形三个顶点地址
		calcTriNormal(&verts[tri[0]*3], &verts[tri[1]*3], &verts[tri[2]*3], norm);
		// Check if the face is walkable.
		if (norm[1] > walkableThr)
			areas[i] = RC_WALKABLE_AREA;
	}
}

static void calcTriNormal(const float* v0, const float* v1, const float* v2, float* norm)
{
	float e0[3], e1[3];
	rcVsub(e0, v1, v0);
	rcVsub(e1, v2, v0);
	rcVcross(norm, e0, e1);
	rcVnormalize(norm);
}
```

代码示意图：

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='1064' height='667'></svg>)

这里的代码很简单，计算垂直于三角面的法线normal，然后使用normal[1]和cos(walkableSlopeAngle)值进行比较，满足条件就设置这个三角面是可行走的RC_WALKABLE_AREA，使用normal[1]来判断是由下面公式决定的：

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='788' height='229'></svg>)

### 光珊化三角形，添加span到实体高度场（rcRasterizeTriangles）

光栅化三角形

在讲体素化之前，我们先来看下如何将一个凸多边形分隔成两个凸多边形。如下图的五边形，我们分析下分割线分隔凸多边形的流程（**dividePoly**）

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='1473' height='454'></svg>)

基于上面分隔凸多边形的原理，我们每相隔cellsize个单位(体素精度)，分别在平行于x轴和z轴的方向设置分隔线就可以将三角形平面切割成CellSize精度的体素格子，体素格子的y坐标下沿，取多边形顶点中的最小y坐标，体素格子的y坐标上沿，取多边形顶点中的最大y坐标。

代码如下：

```cpp
/**
 * 多边形切割
 * 切割线x把多边形切割成两个多边形
 *
 *
 * @param in 多边形
 * @param nin 输入点的个数,意味着此次切割的是几边形
 * @param out1 切割后左边的多边形
 * @param nout1 切割后左边多边形的顶点数
 * @param out2 切割后右边的多边形
 * @param nout2 切割后右边多边形的顶点数
 * @param x 切割线
 * @param axis 轴，0=x轴，1=y轴，2=z轴
 */
// divides a convex polygons into two convex polygons on both sides of a line
static void dividePoly(const float* in, int nin,
					  float* out1, int* nout1,
					  float* out2, int* nout2,
					  float x, int axis)
{

   
	float d[12];
    for (int i = 0; i < nin; ++i)
		d[i] = x - in[i*3+axis];  //取出多边形每个点相对于分割线的距离
	
    int m = 0, n = 0;
    // 遍历所有的边与切割线的相交关系，i和j两个点即为一条边
    for (int i = 0, j = nin-1; i < nin; j=i, ++i)
	{
        bool ina = d[j] >= 0;
        bool inb = d[i] >= 0;
        // different side
        // 如果边上的2个点在切割线两边，则新产生一个顶点，即切割点，切割点属于被切开的两个多边形，所以out1和out2中都要加入此切割点
        if (ina != inb)
		{
            //计算切割比（由于i和j两个点在分割线两侧，所以d[j] - d[i]为到分割线距离的和）
            float s = d[j] / (d[j] - d[i]);
            // 根据切割比，计算切割点的三维坐标
            out1[m*3+0] = in[j*3+0] + (in[i*3+0] - in[j*3+0])*s;
			out1[m*3+1] = in[j*3+1] + (in[i*3+1] - in[j*3+1])*s;
			out1[m*3+2] = in[j*3+2] + (in[i*3+2] - in[j*3+2])*s;
            // 新产生的切割点，也加入out2
            rcVcopy(out2 + n*3, out1 + m*3);
			m++;
			n++;
			// add the i'th point to the right polygon. Do NOT add points that are on the dividing line
			// since these were already added above
            // 将第 i 个点添加到正确的多边形。不要添加分界线上的点，因为上面已经添加了这些点
            if (d[i] > 0)
			{
				rcVcopy(out1 + m*3, in + i*3);
				m++;
			}
            else if (d[i] < 0)
			{
				rcVcopy(out2 + n*3, in + i*3);
				n++;
			}
		}
		else // same side
		{
			// add the i'th point to the right polygon. Addition is done even for points on the dividing line
            // 将第 i 个点添加到正确的多边形。甚至对分界线上的点也进行加法
			if (d[i] >= 0)
			{
				rcVcopy(out1 + m*3, in + i*3);
				m++;
				if (d[i] != 0)
					continue;
			}
			rcVcopy(out2 + n*3, in + i*3);
			n++;
		}
	}

	*nout1 = m;
	*nout2 = n;
}
```

3D空间中，看这个过程：确定多边形在高度场网格上的覆盖区（footprint）。 这是多边形的 2D 轴对齐包围盒，它限定了找出与该多边形相交的网格列，所需要的相交测试次数。

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

遍历覆盖区内的所有高度场网格列，并导出源多边形与网格列列相交的部分。 如果发生相交，则导出一个新的“裁剪（clipped）”多边形。 然后确定裁剪多边形的最小-最大高度。 这表示网格列被源多边形遮挡的部分：

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

在2D上看这个过程，比如看三角形在xz轴上的投影看，会更好理解一点：

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='1032' height='728'></svg>)

**紫色的线**：横着从下往上切，高度是cellsize，把ABC切分成了多边形ADEB和和多边形CDE

**绿色的线**：竖着从左向右切，宽度是cellsize，把CDE切分成了多边形CDGF和多边形FGE

循环横切和竖切即完成了体素化过程，把切分得到的span添加到Heightfield（addSpan函数）

上述单个三角形进行光栅化整个过程，可以对照Recast源码来看：**rasterizeTri()**

在xz平面,对三角形进行分割，循环调用dividePoly，先平行x轴模向切割, 再平行z轴纵切：

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='825' height='587'></svg>)

**rasterizeTri**完整代码注释

```cpp
static bool rasterizeTri(const float* v0, const float* v1, const float* v2,
                   const unsigned char area, rcHeightfield& hf,
                   const float* bmin, const float* bmax,
                   const float cs, const float ics, const float ich,
                   const int flagMergeThr)
{
   const int w = hf.width;
   const int h = hf.height;
   float tmin[3], tmax[3];
   const float by = bmax[1] - bmin[1];

   // 计算三角形的包围盒
   rcVcopy(tmin, v0);
   rcVcopy(tmax, v0);
   rcVmin(tmin, v1);
   rcVmin(tmin, v2);
   rcVmax(tmax, v1);
   rcVmax(tmax, v2);

   // If the triangle does not touch the bbox of the heightfield, skip the triagle.
   if (!overlapBounds(bmin, bmax, tmin, tmax))
      return true;

   // Calculate the footprint of the triangle on the grid's y-axis
    // 在xz平面上，三角形在z轴上的最小值和最大值
   int y0 = (int)((tmin[2] - bmin[2])*ics);
   int y1 = (int)((tmax[2] - bmin[2])*ics);
   y0 = rcClamp(y0, 0, h-1);
   y1 = rcClamp(y1, 0, h-1);

   // Clip the triangle into all grid cells it touches.
   // 定义一块连续内存 buf[7*3*4]
   // 7的意义:对三角形切割时，切出来的一个格子，最多可变成一个7边形
   // 3的意义:存的是顶点，所以3个float， x y z
    // 4的意义:下面的in inrow p1 p2四份等长数据，这些数据里存的是多边形，即顶点坐标
   float buf[7*3*4];
   float *in = buf, *inrow = buf+7*3, *p1 = inrow+7*3, *p2 = p1+7*3;

   rcVcopy(&in[0], v0);
   rcVcopy(&in[1*3], v1);
   rcVcopy(&in[2*3], v2);

   int nvrow, nvIn = 3;  // 初始为三角形，所以nvIn = 3

   for (int y = y0; y <= y1; ++y)
   {
      // Clip polygon to row. Store the remaining polygon as well
      const float cz = bmin[2] + y*cs;
        // 多边形切割，将in以cz+cs为切割线,划分为inrow和p1两个多边形（z轴递进横向切割）
        dividePoly(in, nvIn, inrow, &nvrow, p1, &nvIn, cz+cs, 2);
        // inrow在本次循环内按x0到x1切割完成，另一半p1下次循环在进行切分，所以p1和in交换下 下次再切分in即p1

      rcSwap(in, p1);
        // 这次要处理的这一半多边形没切到东西，可能切到了in（多边形）的一个顶点或者与y轴平行的一条边，顶点和边没必要体素化，所以continue
        if (nvrow < 3) continue;

      // find the horizontal bounds in the row
      float minX = inrow[0], maxX = inrow[0];
        // 遍历多边形inrow的顶点，找到最小x和最大x
      for (int i=1; i<nvrow; ++i)
      {
         if (minX > inrow[i*3]) minX = inrow[i*3];
         if (maxX < inrow[i*3]) maxX = inrow[i*3];
      }
      int x0 = (int)((minX - bmin[0])*ics);
      int x1 = (int)((maxX - bmin[0])*ics);
      x0 = rcClamp(x0, 0, w-1);
      x1 = rcClamp(x1, 0, w-1);

      int nv, nv2 = nvrow;

      for (int x = x0; x <= x1; ++x)
      {
         // Clip polygon to column. store the remaining polygon as well
         const float cx = bmin[0] + x*cs;
            //再竖着切割多边形inrow
         dividePoly(inrow, nv2, p1, &nv, p2, &nv2, cx+cs, 0);
            // p1为切割线左边的多边形，p2为切割线右边的多边形，此时p1已经被切割完毕（已经是一个格子内的多边形了）
            // p2还没切割完成，与inrow交换，下次遍历切割
         rcSwap(inrow, p2);
            // 切分失败，不会进行下一步
         if (nv < 3) continue;

         // Calculate min and max of the span.
            // smin：多边形p1的最低高度
            // smax：多边形p1的最高高度
         float smin = p1[1], smax = p1[1];
         for (int i = 1; i < nv; ++i)
         {
            smin = rcMin(smin, p1[i*3+1]);
            smax = rcMax(smax, p1[i*3+1]);
         }

         smin -= bmin[1];
         smax -= bmin[1];
         // Skip the span if it is outside the heightfield bbox
         // 如果span在高度场 bbox 之外，则跳过span
         if (smax < 0.0f) continue;
         if (smin > by) continue;
         // Clamp the span to the heightfield bbox.
         // 将span限制在高度域 bbox 上
         if (smin < 0.0f) smin = 0;
         if (smax > by) smax = by;

         // Snap the span to the heightfield height grid.
         // 包围盒最低体素ismin，最高体素ismax
         unsigned short ismin = (unsigned short)rcClamp((int)floorf(smin * ich), 0, RC_SPAN_MAX_HEIGHT);
         unsigned short ismax = (unsigned short)rcClamp((int)ceilf(smax * ich), (int)ismin+1, RC_SPAN_MAX_HEIGHT);
         //把切分下来的span合并到到高度场中
         if (!addSpan(hf, x, y, ismin, ismax, area, flagMergeThr))
            return false;
      }
   }

   return true;
}
```

添加span到实体高度场（addSpan）

我们使用以下信息向实体高度场添加span信息：

- 与多边形相交的网格列。
- 裁剪多边形的最小-最大高度范围（网格列被阻挡的部分）。

将新的span数据添加到高度场时，会发生以下情况：

- 如果新span不与网格列中的任何已经存在的span相交，则会创建一个新span。如果新span与已经存在的span相交或被已经存在的span所包含，则合并这两个span。
- 当新span与已经存在的span合并时，必须评估生成的聚合span是否是可行走的。这个“可行走标志”只适用于span的顶部表面。如果设置了，就意味着span顶部表示多边形，该多边形有足够低的斜率是可行走的。
- 如果新span的顶部高于它正在合并到的span，则新span的可行走标志用于聚合span；如果新span的顶部低于它正在合并到的span，那么我们不关心新span的标志，新span的标志被丢弃：

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

- 如果新span的顶部与其合并到的span处于同一高度，则如果其中任意一个被认为是可行走的，聚合span就被标记为可行走：

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

3D示意图可能更清晰，如下图所示：

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

addSpan代码如下：

```cpp
static bool addSpan(rcHeightfield& hf, const int x, const int y,
               const unsigned short smin, const unsigned short smax,
               const unsigned char area, const int flagMergeThr)
{

   int idx = x + y*hf.width;

   rcSpan* s = allocSpan(hf);
   if (!s)
      return false;
   s->smin = smin;
   s->smax = smax;
   s->area = area;
   s->next = 0;

   // Empty cell, add the first span.
   if (!hf.spans[idx])
   {
      hf.spans[idx] = s;
      return true;
   }
   rcSpan* prev = 0;
   rcSpan* cur = hf.spans[idx];

   // Insert and merge spans.
   while (cur)
   {
      if (cur->smin > s->smax) //cur在s上面
      {
         // Current span is further than the new span, break.
         break;
      }
      else if (cur->smax < s->smin) //cur在s下面
      {
         // Current span is before the new span advance.
         prev = cur;
         cur = cur->next;
      }
      else //重叠了
      {
         // Merge spans. 合并span
         if (cur->smin < s->smin)
            s->smin = cur->smin;
         if (cur->smax > s->smax)
            s->smax = cur->smax;

         // Merge flags. 合并area标志，如果一个span比另一个span高出不大于walkableClimb，则可行走
         if (rcAbs((int)s->smax - (int)cur->smax) <= flagMergeThr)
            s->area = rcMax(s->area, cur->area);

         // Remove current span.
         rcSpan* next = cur->next;
         freeSpan(hf, cur);
         if (prev)
            prev->next = next;
         else
            hf.spans[idx] = next;
         cur = next;
      }
   }

   // Insert new span.
   if (prev) //span插入到pre后面
   {
      s->next = prev->next;
      prev->next = s;
   }
   else //在最前面插入span
   {
      s->next = hf.spans[idx];
      hf.spans[idx] = s;
   }

   return true;
}

```

### 体素化的阶段效果

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

## 筛选可走表面（Filter walkables surfaces）

span有一个标志，指示其顶面是否被认为是可行走的。 但是此标志仅根据与span相交的多边形的斜率来设置。现在是进行更多过滤的好时机。此过滤从某些span中删除可行走标志。

支持三种过滤方式，各方式之间相互独立，且都非必须执行（可选项）。但是，Low_Hanging_Obstacles的效果会覆盖Ledge_Spans，所以如果上述两个选项被同时选择，则后者必须在前者之后执行:

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='643' height='110'></svg>)

### 筛选低垂的可行走障碍（rcFilterLowHangingWalkableObstacles）

为了在低洼区域形成可走区域。比如楼梯，将不可走标记为可走。

算法比较简单：迭代每一列，从下往上遍历span，对于同列任意两个相邻的span1（下）和span2（上），当span1可走，并且span2不可走的时候，计算这两个span的上表面高度差Diff，如果Diff小于配置参数“walkableClimb”，则将span2设置为“可走”

```cpp
void rcFilterLowHangingWalkableObstacles(rcContext* ctx, const int walkableClimb, rcHeightfield& solid)
{
   rcAssert(ctx);

   rcScopedTimer timer(ctx, RC_TIMER_FILTER_LOW_OBSTACLES);

   const int w = solid.width;
   const int h = solid.height;

   for (int y = 0; y < h; ++y)
   {
      for (int x = 0; x < w; ++x)
      {
         rcSpan* ps = 0;
         bool previousWalkable = false;
         unsigned char previousArea = RC_NULL_AREA; 

         for (rcSpan* s = solid.spans[x + y*w]; s; ps = s, s = s->next)
         {
            const bool walkable = s->area != RC_NULL_AREA;  
            // 如果当前跨度不可行走，但其下方有可行走跨度，则将其上方的跨度也标记为可行走。
            if (!walkable && previousWalkable)
            {
               if (rcAbs((int)s->smax - (int)ps->smax) <= walkableClimb)
                  s->area = previousArea;
            }
            // Copy walkable flag so that it cannot propagate
            // past multiple non-walkable objects.
            previousWalkable = walkable;
            previousArea = s->area;
         }
      }
   }
}
```

### 过滤可行走的低高度区间（rcFilterWalkableLowHeightSpans）

这个是同列相邻两区间之间距离的校验，保证最小可通过距离，可走变不可走。如果在span上方有太近的障碍物，那么span的顶面是不能穿越的。也就是说span的顶部与其上方span的底部至少有一个最小距离（最高的agent可以站在span上而不会与上方的障碍物发生碰撞）。想象一张桌子放在地板上，桌子下面的地板表面是平的，但由于桌子比较矮，不能在桌子底下行走，所以不能被认为是可穿越的（traversable）：

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

算法也比较简单：

- 迭代每一列，从下往上遍历span，如果当前span不可走，直接跳过。
- 否则，计算当前span_B的上表面和其上相邻的span_A的下表面之间的高度差Diff。span_A不存在的话，高度差Diff设为无穷大。
- 如果“高度差Diff”小于“walkableHeight”，则将当前span的标识位置为“不可走”。

```cpp
void rcFilterWalkableLowHeightSpans(rcContext* ctx, int walkableHeight, rcHeightfield& solid)
{
   rcAssert(ctx);

   rcScopedTimer timer(ctx, RC_TIMER_FILTER_WALKABLE);

   const int w = solid.width;
   const int h = solid.height;
   const int MAX_HEIGHT = 0xffff;

   //从上面没有足够空间让 agent 站在那里的 span 中移除可行走标志。
   for (int y = 0; y < h; ++y)
   {
      for (int x = 0; x < w; ++x)
      {
         for (rcSpan* s = solid.spans[x + y*w]; s; s = s->next)
         {
            const int bot = (int)(s->smax);
            const int top = s->next ? (int)(s->next->smin) : MAX_HEIGHT;
            if ((top - bot) <= walkableHeight)
               s->area = RC_NULL_AREA;
         }
      }
   }
}
```

### 过滤有效区间和陡峭区间**（**rcFilterLedgeSpans**）**

### 有效邻居区间过滤

同列相邻两区间之间距离的校验，保证最小可通过距离。可走变不可走，这种过滤器会首先查找当前span的所有“有效邻居区间”。需要注意的是：如果当前span已经不可走，则直接跳过了。

具体实现：

- 当前迭代区间为span1，遍历四个方向的轴邻居列，从下往上迭代轴邻居列的高度区间span2。
- 假设span1的上面还与其有同列相邻的span4，span2上面有与其同列相邻的span3。如果span3或span4不存在，则认为其高度为无穷大。
- 计算max(span1, span2)和min(span3, span4)的差值H，如果diff大于“配置可走高度walkableHeight”，则认为span2是一个“有效邻居区间”。可以证明，每一个轴邻居列上最多只会存在一个“有效邻居区间”。

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='1031' height='744'></svg>)

可以很明显的看出，“有效邻居区间”限定了两种情况。如图所示，当diff大于“配置可走高度walkableHeight”的时候，1️⃣说明“可下坡”且“不碰头”，2️⃣说明“可上坡”且“不碰头”。

### 陡峭区间过滤

找到所有“有效邻居区间”后，过滤器会继续过滤“峭壁区间”。

具体实现：

- 如果有任意轴邻居列上没有任何“高度区间”，则认为当前span是”峭壁区间“。
- 如果有任意轴邻居列上没有”有效邻居区间“，则认为当前span是“峭壁区间”。
- 如果有任意轴邻居列上存在“有效邻居区间”span2，span2的上表面低于span的上表面，且span和span2的上表面高度差Diff大于配置参数“可爬坡高度”，则认为当前span是”峭壁区间”。
- 如果当前span本来可走，且判断为“峭壁区间”，则设置为“不可走”。

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

在不满足峭壁区间的情况下，才会继续判断“间接峭壁区间”。

过滤间接峭壁区间，具体实现：

前提：在当前span的四个轴邻居上的“有效邻居区间”中。

所有上表面高于span上表面的“有效邻居区间”中，上表面最高的“有效邻居区间”的上表面高度设为a。

所有上表面低于span上表面的“有效邻居区间”中，上表面最低的“有效邻居区间”的上表面高度设为b。

如果a减b大于配置参数“爬坡高度walkableClimb”，则认为当前span处于峭壁上--间接峭壁区间。

如果当前span本来可走，且判断为“间接峭壁区间”，则置为“不可走”。

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='917' height='539'></svg>)

rcFilterLedgeSpans代码：

```cpp
void rcFilterLedgeSpans(rcContext* ctx, const int walkableHeight, const int walkableClimb,
                  rcHeightfield& solid)
{
   rcAssert(ctx);

   rcScopedTimer timer(ctx, RC_TIMER_FILTER_BORDER);

   const int w = solid.width;
   const int h = solid.height;
   const int MAX_HEIGHT = 0xffff;

   // 标记边界Span
   for (int y = 0; y < h; ++y)
   {
      for (int x = 0; x < w; ++x)
      {
         for (rcSpan* s = solid.spans[x + y*w]; s; s = s->next)
         {
            // 跳过不可走span
            if (s->area == RC_NULL_AREA)
               continue;

            const int bot = (int)(s->smax);
            const int top = s->next ? (int)(s->next->smin) : MAX_HEIGHT;

            // 查找邻居的最小高度
            int minh = MAX_HEIGHT;

            // Min and max height of accessible neighbours.
            int asmin = s->smax;
            int asmax = s->smax;

            for (int dir = 0; dir < 4; ++dir)
            {
               int dx = x + rcGetDirOffsetX(dir);
               int dy = y + rcGetDirOffsetY(dir);
               //跳过越界的邻居
               if (dx < 0 || dy < 0 || dx >= w || dy >= h)
               {
                  minh = rcMin(minh, -walkableClimb - bot);
                  continue;
               }

               // From minus infinity to the first span.
               rcSpan* ns = solid.spans[dx + dy*w];
               int nbot = -walkableClimb;
               int ntop = ns ? (int)ns->smin : MAX_HEIGHT;
               //如果 spans 之间的间隙太小，则跳过 neightbour
               if (rcMin(top,ntop) - rcMax(bot,nbot) > walkableHeight)
                  minh = rcMin(minh, nbot - bot);

               //其余的跨度
               for (ns = solid.spans[dx + dy*w]; ns; ns = ns->next)
               {
                  nbot = (int)ns->smax;
                  ntop = ns->next ? (int)ns->next->smin : MAX_HEIGHT;
                  // 如果Span之间的间隙太小，则跳过 neightbour
                  if (rcMin(top,ntop) - rcMax(bot,nbot) > walkableHeight)
                  {
                     minh = rcMin(minh, nbot - bot);

                     // 查找最小/最大可访问邻居高度
                     if (rcAbs(nbot - bot) <= walkableClimb)
                     {
                        if (nbot < asmin) asmin = nbot;
                        if (nbot > asmax) asmax = nbot;
                     }

                  }
               }
            }

            //如果下降到任何邻居Span小于 walkableClimb，将Span标记为RC_NULL_AREA
            if (minh < -walkableClimb)
            {
               s->area = RC_NULL_AREA;
            }
            //如果所有邻居之间的差异太大，我们在陡坡上，将Span标记为RC_NULL_AREA
            else if ((asmax - asmin) > walkableClimb)
            {
               s->area = RC_NULL_AREA;
            }
         }
      }
   }
}

```

### 筛选可走表面的阶段效果

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='1269' height='726'></svg>)

## 划分可走表面为简单区域（Region Generation）

这一阶段的目标，即生成**表面区域**，用以表示源几何结构的可通过表面，是进一步定义实体表面的哪些部分是可通过的，并将可通过的区域分割成可以最终形成简单多边形的相邻的span（表面）区域。

### 创建开放高度场CompactHeightField（由open span组成）

先给出过程，后面会分别说明：先遍历每个可行走的区间，它与上方的另一个区间之间的部分就是一个开放区间；得到所有开放空间后，再计算每个开放区间与相邻的4个区间之间的连通关系，这里是基于walkableHeight和walkableClimb判断是否合法，计算出的连通关系以编码的形式存于rcCompactSpan的con属性中。这一步结束后会完成对rcCompactHeightfield中spans、cells和areas数组的赋值。

### 有了实体高度场，为什么还需要开放高度场？

注意：无论是用实体高度场还是开放高度场，只是数据结构的不同，在逻辑上没有任何差别，Recast采用了开放高度场的数据结构进行体素化之后的所有算法。换句话说，再进行体素化构建实体高度场后，进行了一步实体高度场到开放高度场的转换。注意，开放高度场是在整个体素化过程结束之后才转换的，此时已经经过了高度区间的合并和过滤，换句话说，其实此时实体高度区间的下表面已经没有任何意义了。至于为什么选择开放高度场，更多的考虑可能是Recast关心的是场景中的“无实体障碍可通过空间”，而不关心“实体空间”。但是需要理解，本质上，使用哪一个高度场并没有什么区别。

因此，open span使用“地板（floor）”和“天花板（ceiling）”的术语。open span的地板是其关联的solid span的顶部。 open span的天花板是它所属的列中下一个更高的solid span的底部。如果没有更高的solid span，则open span的天花板是任意的最大值，例如整数的最大值：

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

### Open Span在源码里的数据结构（rcCompactSpan）

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='1517' height='780'></svg>)

CompactHeightField也叫紧缩高度场。我们不只关心实体空间，许多算法都是在solid span上方的空间上操作的。对于导航网格的生成来说，solid span的上表面是其最重要的部分，需要注意的是，开放高度场不是实体空间的简单反转。如果一个高度场列不包含任何solid span，则它也没有任何open span。 最低的solid span以下的区域也被忽略，只有solid span上方的空间由open span来表示。

其中areas字段，就是实体高度场中对应高度区间是否可走的标识，其用一个一维数组直接标识了三维矩阵，其数组大小就是整个三维矩阵中所有Span的数量，索引序列依然是一列一列的依次排列。

rcCompactCell和rcCompactSpan的关系

```cpp
struct rcCompactCell
{
	unsigned int index : 24;	///< 竖直列中第一个span的索引
	unsigned int count : 8;	///< 当前列中span的个数
};

```

已知rcCompactHeightfield& chf ，计算在位置（x,z）处的垂直列（CompaceCell）的方法如下：

```cpp
const rcCompactCell& nc = chf.cells[x+z*width];
```

而遍历这一列体素rcCompactSpan的代码如下

```cpp
    for (int k = (int)nc.index, nk = (int)(nc.index+nc.count); k < nk; ++k)
    {
    const rcCompactSpan& ns = chf.spans[k];
}
```

### 创建CompactHeightField的具体做法

第一步是将实体高度场转换为开放高度场，该高度场表示实体空间顶部的潜在可通过表面。开放高度场代表实体空间表面的潜在地板区域。如高度场简介中所述，开放高度场表示实体高度场中span上方的区域。开放高度场的创建相对简单，循环遍历所有实体span，如果span被标记为可通过，则确定它的最高值与其所在列中下一个更高span的最低值之间的开放空间。 这些值分别形成了新的开放span的地板和天花板。如果一个实体span是它所在列中最高的span，则其关联的开放span将其天花板设置为任意高值（例如 Integer.MAX_VALUE）。新生成的开放span形成所谓的开放高度场：

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

对应代码rcBuildCompactHeightfield：

```cpp
bool rcBuildCompactHeightfield(rcContext* ctx, const int walkableHeight, const int walkableClimb,
                        rcHeightfield& hf, rcCompactHeightfield& chf)
{
   const int w = hf.width;
   const int h = hf.height;
   const int spanCount = rcGetHeightFieldSpanCount(ctx, hf);
    
    //这里省略一些非核心代码......

   const int MAX_HEIGHT = 0xffff;

   // 填充rcCompactCell和rcCompactSpan
   int idx = 0;
   for (int y = 0; y < h; ++y)
   {
      for (int x = 0; x < w; ++x)
      {
         const rcSpan* s = hf.spans[x + y*w];
         // If there are no spans at this cell, just leave the data to index=0, count=0.
         if (!s) continue;
         rcCompactCell& c = chf.cells[x+y*w];
         c.index = idx;
         c.count = 0;
         while (s)
         {
            if (s->area != RC_NULL_AREA)
            {
               const int bot = (int)s->smax;
               const int top = s->next ? (int)s->next->smin : MAX_HEIGHT;
               chf.spans[idx].y = (unsigned short)rcClamp(bot, 0, 0xffff);
               chf.spans[idx].h = (unsigned char)rcClamp(top - bot, 0, 0xff);
               chf.areas[idx] = s->area;
               idx++;
               c.count++;
            }
            s = s->next;
         }
      }
   }

   return true;
}
```

注意：开放高度场是在整个体素化过程结束之后才转换的，此时已经经过了高度区间的合并和过滤，换句话说，其实此时实体高度区间的下表面已经没有任何意义了。至于为什么选择开放高度场，更多的考虑可能是Recast关心的是场景中的“无实体障碍可通过空间”，而不关心“实体空间”。但是需要理解，本质上，使用哪一个高度场并没有什么区别。

### 创建邻居链接（源码：rcSetCon)

我们现在有一个开放高度场，里面充满了不相关的开放span。下一步是找出哪些span形成了连续span的潜在表面。这是通过创建轴邻居链接（axis-neighbor links）来实现的。 对于每个span，搜索其轴相邻列以查找候选对象。如果满足以下两个条件，则相邻列中的span被视为邻居span：

1. **两个span的顶部上升或下降的步长小于 ****walkableClimb ****的值。 这允许将楼梯台阶和路缘这样的表面检测为有效的邻居。**

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

这里面有个参数

```cpp
/// Maximum ledge height that is considered to still be traversable. [Limit: >=0] [Units: vx] 
int walkableClimb;
```

作用是防止微小的高度偏差就被不正确地显示为障碍物。使得检测楼梯状结构、路缘等成为可能。

**示例1**：正确设置该值使得网格沿楼梯向上延伸。

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

**示例2**：将值设置得太低会使楼梯看起来无法通过（agent无法走上楼梯）。

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

**示例3**：将值设置得太高会导致网格流过（flow up）桌子和柜台。

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

walkableClimb 应该大于 cellHeight 的两倍(walkableClimb > cellHeight * 2)。 否则体素场的分辨率可能不够高，无法准确检测可通过的窗台（ledge）。窗台可以合并，有效地将它们的台阶高度加倍。对于楼梯来说，这尤其是个问题。

2. **当前span的地板和潜在邻居span的天花板之间的开放空间间隙足够大（大于walkableHeight）**

例如，如果agent要从一个span跨到另一个span，它会用头撞到邻居的天花板上吗？这是与潜在邻居之间的间隙检查。我们已经知道地板到天花板的高度对于每个单独的span来说都是足够的，该检查是在构建实体高度场时进行的。 但是不能保证在潜在邻居之间移动时的间隙满足相同的高度要求。

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

没有必要搜索和存储所有8个邻居的链接，因为如果需要，可以通过执行邻居的邻居的链接遍历（neighbor-of-neighbor link traversal）来找到对角线邻居。

创建邻居链接的代码，最终目的是调用rcSetCon，构建每个rcCompactSpan的邻居关系

```cpp
bool rcBuildCompactHeightfield(rcContext* ctx, const int walkableHeight, const int walkableClimb,
                        rcHeightfield& hf, rcCompactHeightfield& chf)
{
    //创建邻居链接
   const int MAX_LAYERS = RC_NOT_CONNECTED-1; //static const int RC_NOT_CONNECTED = 0x3f , 最多63层
   int tooHighNeighbour = 0;
   for (int y = 0; y < h; ++y)
   {
      for (int x = 0; x < w; ++x)
      {
         const rcCompactCell& c = chf.cells[x+y*w];
         for (int i = (int)c.index, ni = (int)(c.index+c.count); i < ni; ++i)
         {
            rcCompactSpan& s = chf.spans[i];

            for (int dir = 0; dir < 4; ++dir)
            {
               rcSetCon(s, dir, RC_NOT_CONNECTED); //先设置默认值RC_NOT_CONNECTED二进制是 111111
               const int nx = x + rcGetDirOffsetX(dir);
               const int ny = y + rcGetDirOffsetY(dir);
               // First check that the neighbour cell is in bounds.
               if (nx < 0 || ny < 0 || nx >= w || ny >= h)
                  continue;

               // 检查当前span的所有邻居span，看这个span是否和当前span有邻居关系
               const rcCompactCell& nc = chf.cells[nx+ny*w];
               for (int k = (int)nc.index, nk = (int)(nc.index+nc.count); k < nk; ++k)
               {
                  const rcCompactSpan& ns = chf.spans[k];
                  const int bot = rcMax(s.y, ns.y);
                  const int top = rcMin(s.y+s.h, ns.y+ns.h);

                  //检查2个span间的gap是否满足walkableHeight和walkableClimb的限制
                  if ((top - bot) >= walkableHeight && rcAbs((int)ns.y - (int)s.y) <= walkableClimb)
                  {
                     // Mark direction as walkable.
                     const int lidx = k - (int)nc.index;
                     if (lidx < 0 || lidx > MAX_LAYERS)
                     {
                        tooHighNeighbour = rcMax(tooHighNeighbour, lidx);
                        continue;
                     }
                     rcSetCon(s, dir, lidx);
                     break;
                  }
               }

            }
         }
      }
   }

   return true;
}
```

其中rcSetCon的实现比较有意思：

使用了掩码实现，函数的含义是设置当前的span的某个dir方向是否有邻居，如果有就把connection data(这个邻居在当前竖列中属于第几个span)的值设置到当前span的con中

```cpp
/// Represents a span of unobstructed space within a compact heightfield.
struct rcCompactSpan
{
	unsigned short y;			///< The lower extent of the span. (Measured from the heightfield's base.)
	unsigned short reg;			///< The id of the region the span belongs to. (Or zero if not in a region.)
	unsigned int con : 24;		        ///< Packed neighbor connection data.
	unsigned int h : 8;			///< The height of the span.  (Measured from #y.)
};
```

举例说明：比如con字段的二进制值为000001 000010 000000 000100时，意义如下：

1. 左方向，该体素与layer为1的体素连通
2. 上方向，该体素与layer为2的体素连通
3. 右方向，该体素无连通体素
4. 下方向，该体素与layer为4的体素连通

rcSetCon的实现代码：

```cpp
/// Sets the neighbor connection data for the specified direction.
///  @param[in]       s     The span to update.
///  @param[in]       dir       The direction to set. [Limits: 0 <= value < 4]
///  @param[in]       i     The index of the neighbor span.
inline void rcSetCon(rcCompactSpan& s, int dir, int i)
{
   const unsigned int shift = (unsigned int)dir*6;
   unsigned int con = s.con;
   s.con = (con & ~(0x3f << shift)) | (((unsigned int)i & 0x3f) << shift);
}
```

总的来说，上面这段代码的目的是按位操作，将变量 `i` 的二进制数插入到变量 `con` 的某一位，并将结果赋值给变量 `con`。在rcCompactSpan 中的con来记录往那个方向可以移动。con是24位的，原始的里面有（0，1，2，3）四个方向来记录是否可移动，每个方向6位，也就注定了一个cell中最大只能有(2^6 - 1)=63层的span。

取dir方向上的connection data的时候，代码如下：

```cpp
inline int rcGetCon(const rcCompactSpan& s, int dir)
{
   const unsigned int shift = (unsigned int)dir*6;
   return (s.con >> shift) & 0x3f;
}
```

取真正的nidx = **rcCompactCell中第一个span的index** 加上 **connection data的值**

```cpp
const int nidx = (int)chf.cells[nx+ny*w].index + rcGetCon(s, dir);
if (chf.areas[nidx] != RC_NULL_AREA)
```

### 根据walkableRadius剔除边缘

对应源码：

```cpp
/// Erodes the walkable area within the heightfield by the specified radius. 
///  @ingroup recast
///  @param[in,out]    ctx       The build context to use during the operation.
///  @param[in]       radius The radius of erosion. [Limits: 0 < value < 255] [Units: vx]
///  @param[in,out]    chf       The populated compact heightfield to erode.
///  @returns True if the operation completed successfully.
bool rcErodeWalkableArea(rcContext* ctx, int radius, rcCompactHeightfield& chf);
```

Erode的中文含义为侵蚀，即将障碍物周围可行走区域按radius值适当扩散不可行走区域，**目的是寻路目标贴墙走时，留出一定的宽度，否则身体模型可能会穿到墙里。**

到目前为止，已经根据配置参数“爬坡角度”、“可走高度”、“爬坡高度”进行了一些逻辑上的过滤。此时考虑游戏可移动物Agent在场景边界移动的情况，如果距离边界过近，会出现Agent的一部分在边界之外，如果边界是墙体等可视化阻挡的话，会较低游戏的视觉体验。所以提供配置参数“AgentRadius”，然后根据该半径进行“边缘剔除”。

此时的做法是对每一个“可走高度区间”加上一个“逻辑距离dist”的概念。该距离在逻辑上标识当前高度区间距离某个最近边界的距离。做两遍扫描，计算出每个span离边缘的距离,放到char*类型的dist 中。每次操作都是用小值替换大值，第一遍扫描从左上角往右下角，第二遍从右下角往左上角。这样就能保证每个位置的span计算出来的离边缘距离的准确性。然后再将离边距离小于两倍半径的span从可行走的span中剔除

举例说明：下图中中间红色的是当前遍历到的的span

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='453' height='261'></svg>)

1. 从左下到右上扫描邻居体素时，黄色的那4个邻居体素已先被扫描到
2. 从右上到左下扫描邻居体素时，蓝色的那4个邻居体素已先被扫描到

具体实现：以每一层的二维矩阵为例，所有区间初始逻辑距离均dist=oxff

```cpp
unsigned char* dist = (unsigned char*)rcAlloc(sizeof(unsigned char)*chf.spanCount, RC_ALLOC_TEMP);
memset(dist, 0xff, sizeof(unsigned char)*chf.spanCount);
```

特殊情况：所有不可走区间，逻辑距离为0。所有“有效邻居数量”不为4的区间，逻辑距离为0。

```cpp
// Mark boundary cells.
for (int y = 0; y < h; ++y)
{
   for (int x = 0; x < w; ++x)
   {
      const rcCompactCell& c = chf.cells[x+y*w];
      for (int i = (int)c.index, ni = (int)(c.index+c.count); i < ni; ++i)
      {
         if (chf.areas[i] == RC_NULL_AREA)
         {
            dist[i] = 0;  //所有不可走区间，逻辑距离为0
         }
         else
         {
            const rcCompactSpan& s = chf.spans[i];
            int nc = 0;
            for (int dir = 0; dir < 4; ++dir)
            {
               if (rcGetCon(s, dir) != RC_NOT_CONNECTED)
               {
                  const int nx = x + rcGetDirOffsetX(dir);
                  const int ny = y + rcGetDirOffsetY(dir);
                  const int nidx = (int)chf.cells[nx+ny*w].index + rcGetCon(s, dir);
                  if (chf.areas[nidx] != RC_NULL_AREA)
                  {
                     nc++;
                  }
               }
            }
            // At least one missing neighbour.
            if (nc != 4)
               dist[i] = 0; //所有“有效邻居数量”不为4的区间，逻辑距离为0
         }
      }
   }
}
```

先从左下角向右上角遍历，由正左方轴邻居开始，逆时针遍历四个邻居。原因很简单，其他邻居位置还没有被遍历到，也就是在算法执行过程中，还是无效的。

遇到轴邻居，当Dist(轴邻居)+2<dist(自己)的时候，将自己的逻辑距离更新为Dist(轴邻居)+2。

遇到对角邻居，当Dist(对角邻居)+3<dist(自己)的时候，将自己的逻辑距离更新为Dist(对角邻居)+3。

```cpp
 从左下到右上遍历
for (int y = 0; y < h; ++y)
{
   for (int x = 0; x < w; ++x)
   {
      const rcCompactCell& c = chf.cells[x+y*w];
      for (int i = (int)c.index, ni = (int)(c.index+c.count); i < ni; ++i)
      {
         const rcCompactSpan& s = chf.spans[i];

         if (rcGetCon(s, 0) != RC_NOT_CONNECTED)
         {
            // (-1,0)	左邻居
            const int ax = x + rcGetDirOffsetX(0);
            const int ay = y + rcGetDirOffsetY(0);
            const int ai = (int)chf.cells[ax+ay*w].index + rcGetCon(s, 0);
            const rcCompactSpan& as = chf.spans[ai];
           // 轴邻居距离+2
            nd = (unsigned char)rcMin((int)dist[ai]+2, 255);
            if (nd < dist[i])
               dist[i] = nd;

            // (-1,-1) 左下邻居
            if (rcGetCon(as, 3) != RC_NOT_CONNECTED)
            {
               const int aax = ax + rcGetDirOffsetX(3);
               const int aay = ay + rcGetDirOffsetY(3);
               const int aai = (int)chf.cells[aax+aay*w].index + rcGetCon(as, 3);
               // 斜方向的邻居距离+3
               nd = (unsigned char)rcMin((int)dist[aai]+3, 255);
               if (nd < dist[i])
                  dist[i] = nd;
            }
         }
         if (rcGetCon(s, 3) != RC_NOT_CONNECTED)
         {
            // (0,-1) 下邻居
            const int ax = x + rcGetDirOffsetX(3);
            const int ay = y + rcGetDirOffsetY(3);
            const int ai = (int)chf.cells[ax+ay*w].index + rcGetCon(s, 3);
            const rcCompactSpan& as = chf.spans[ai];
            nd = (unsigned char)rcMin((int)dist[ai]+2, 255);
            if (nd < dist[i])
               dist[i] = nd;

            // (1,-1) 右下邻居
            if (rcGetCon(as, 2) != RC_NOT_CONNECTED)
            {
               const int aax = ax + rcGetDirOffsetX(2);
               const int aay = ay + rcGetDirOffsetY(2);
               const int aai = (int)chf.cells[aax+aay*w].index + rcGetCon(as, 2);
               nd = (unsigned char)rcMin((int)dist[aai]+3, 255);
               if (nd < dist[i])
                  dist[i] = nd;
            }
         }
      }
   }
}
```

当上述一次遍历完成后，再逆向遍历即可，此时的计算将以正右方轴邻居开始，逆时针遍历四个邻居。由此，完成对每一个高度区间的所有八个邻居的相对关系的计算。

当计算出所有高度区间的逻辑距离后，对比每一个高度区间的逻辑距离diff和配置参数“可走半径”的2倍，也就是可走直径作对比，所有不满足可走直径的高度区间，全置为“不可走”。

```cpp
const unsigned char thr = (unsigned char)(radius*2);
for (int i = 0; i < chf.spanCount; ++i)
   if (dist[i] < thr) // radius范围内的设置为不可走
      chf.areas[i] = RC_NULL_AREA;
```

上述计算过程中的2,3这些自然数全是逻辑数值，代表的是几个单元格边长。算法执行之前，也会将参与运算的配置参数，转换成以单元格边长为单位的逻辑数值

小结：这一步是用dist存储span到障碍或边界的最小距离，最后通过dist筛选出所有小于指定距离的span并标记为不可走。

### 开放高度场（CompactHeightfield）的阶段效果

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='1485' height='704'></svg>)

### 根据ConvexVolume标记体素Area掩码（源码：rcMarkConvexPolyArea）

这个是动态障碍用的，获取所有的ConvexVolume，然后把一些span标记成凸体里的标记，关于动态障碍的寻路方法，将在下一篇Detour寻路原理时候进行讲解

**ConvexVolune可视化：**

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='807' height='520'></svg>)

**数据结构ConvexVolume:**

```cpp
static const int MAX_CONVEXVOL_PTS = 12;
struct ConvexVolume
{
   float verts[MAX_CONVEXVOL_PTS*3];//Volume顶点数据
   float hmin;//Volume高度最低值
   float hmax;//Volume高度最高值
   int nverts;//Volume顶点数
   int area;  //区域类型,可自定义类型，比如Ground,Water,Grass等等
};
```

**可视化数据信息：**

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='250' height='267'></svg>)

areaId （AreaType）表示该体素属于那种地貌，是否是山地、水体，草地之类的，最终设置到 chf.areas[i] = areaId

**换算关系：**

Shape Height = Volume高度最高值hmax - Volume高度最低值hmin

Shape Descent = 坐标y最低值 - Volume高度最低值hmin，Shape Descent代表坐标面下沉值

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

```cpp
void rcMarkConvexPolyArea(rcContext* ctx, const float* verts, const int nverts,
                    const float hmin, const float hmax, unsigned char areaId,
                    rcCompactHeightfield& chf)
{
   // 遍历多边形范围内的体素
   for (int z = minz; z <= maxz; ++z)
   {
      for (int x = minx; x <= maxx; ++x)
      {
         const rcCompactCell& c = chf.cells[x+z*chf.width];
         for (int i = (int)c.index, ni = (int)(c.index+c.count); i < ni; ++i)
         {
            rcCompactSpan& s = chf.spans[i];
            if (chf.areas[i] == RC_NULL_AREA)
               continue;
            if ((int)s.y >= miny && (int)s.y <= maxy)
            {
               float p[3];
               p[0] = chf.bmin[0] + (x+0.5f)*chf.cs; 
               p[1] = 0;
               p[2] = chf.bmin[2] + (z+0.5f)*chf.cs; 

               if (pointInPoly(nverts, verts, p))  //判断点是否在poly范围内
               {
                  chf.areas[i] = areaId; //设置体素area类型
               }
            }
         }
      }
   }
}
```

其中，上述判断点是否在poly内部的函数pointInPoly，详细原理可以看这里：[判断点是否在多边形内（任意多边形）](https://link.zhihu.com/?target=https%3A//blog.csdn.net/u012138730/article/details/79927778)

### 创建区域（rcBuildRegions）

到目前为止，所有的一切都是在为区域创建做准备。区域（region）是一组连续的span，表示可走表面的范围。它应该满足尽量大的、连续的、不重叠的、中间没有“洞”的“区域，区域的一个重要方面是，当投影到xz平面上时，它们会形成简单的多边形。

Recast里提供了三种方式的区域切分方法：

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='229' height='102'></svg>)

### 分水岭算法（Watershed）

分水岭算法（watershed algorithm）用于初始区域的创建。使用分水岭类比，距离边界最远的span代表分水岭中的最低点，边界span代表可能的最高水位（和盆地的概念类似）。主循环从分水岭的最低点开始迭代，然后随着每个循环递增，直到达到允许的最高水位。这从最低点开始缓慢地“淹没”span。在循环的每次迭代期间，都会定位出低于当前水位的span，并尝试将它们添加到现有区域或创建新的区域。在区域扩展（region expansion）阶段，如果新淹没的span与一个已经存在的区域接壤，则通常会将其添加到该区域中。任何在区域扩展阶段，残留下来的新淹没的span都被用作创建新区域的种子。

分水岭算法通常用于图形处理领域，基于图像的灰度值来分割图像。这里唯一的不同点是用**距离域**来取代灰度值。距离域是指每个区间与可行走区域边缘的最近距离。距离域越大，等同于地势越低

![动图封面](https://pic3.zhimg.com/v2-8e432b20bd469e11f937c25ff2024c9a_b.jpg)

image

**分水岭算法的特点**

- 经典的Recast分区
- 创建最好的细分
- 通常最慢，一般用于离线处理，适合大地图
- 将Heightfield划分为没有孔或重叠的良好区域。
- 在某些极端情况下，此方法创建会产生孔洞和重叠  
    

- 当小的障碍物靠近较大的开放区域时，可能会出现孔（三角剖分可以解决此问题）
- 如果您有狭窄的螺旋形走廊（即楼梯），则可能会发生重叠，这会使三角剖分失败

- 如果是预处理网格，通常是最佳选择，如果您有较大的开放区域，这种方法也适用。

**分水岭算法的过程**

先算出每个区间的距离域，再执行分水岭算法，算出的区域id保存在rcCompactSpan.reg中。

1. **创建距离场（calculateDistanceField）**

距离场（distance field）由每个span与其最近的边界span之间的距离估计组成。生成区域的算法需要这些信息。边界span（border span）是指少于8个邻居的span。边界span表示源几何体的可穿越表面与障碍物(如墙壁)或空间之间的边界。空白空间可能是下降点(例如阳台或悬崖的边缘)或源几何的边缘。

下面的示例显示了一种特殊情况，在这种情况下，非边界span被错误地标识为边界span。这是使用距离场算法使用的标准8邻居搜索的一个缺点。然而，在实践中，这并不会造成问题。

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

这里需要注意：

- 创建距离场的代码和rcErodeWalkableArea类似，只是边界的判断不同，rcErodeWalkableArea通过标记距离小于agentRaius的span，以不可行走的span为边界，而calculateDistanceField认为area不同就是边界
- calculateDistanceField计算每个span到区域边界的距离（四个方向中最小的一个），距离越大，颜色越浅，越接近中心
- 创建完距离场后，每个span与邻居之间距离差距可能比较大，需要用**boxblur函数**进行平滑处理（计算周围8个邻居距离的均值）
- calculateDistanceField的目的是以region为单位计算距离，由于region内的area都相同，因此以area不同的span为边界

**距离场的效果如下图所示：**

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='1483' height='692'></svg>)

1. **按距离场进行区域划分（rcBuildRegions）**这一步的目的就是想把Map划分成许多区域region，比如将无锡划分为南长区，锡山区、惠山区、滨湖区，新吴区类似。recast划分region的规则是依据之前说的距离场来划分的。  
    首先按照距离把所有span分配到Stacks中，每间距2为一个level，相同level的span保存到同一个的Stacks[i]中，然后对每个level的span进行如下操作:

```cpp
//由于level每次迭代步长为2，所以需要向上取整，偶数不变，奇数变奇数+1
unsigned short level = (chf.maxDistance+1) & ~1; 
// 遍历处理水位为level的spans
while(level > 0){
    // level每次减少2
    level = level >= 2 ? level-2 : 0;
    // 水位蔓延到那些大于等于level的
    expandRegions(level);
    // 泛洪
    floodRegion(level);
}
```

**2.1 关键函数作用**

**expandRegions()：水位蔓延，向水源四周蔓**延

- 作用是扩展对应点集的区域id。扩展过程中会进行反复迭代，通过迭代扩散区域id。
- 扩散方式：检查四周（紧缩span的connection）是否有满足条件的span，条件为：被标记过 && 非边界 &&可走。
- 迭代停止条件：点集周围（包括点集）无任何标记 或 达到最大迭代次数（自定义的）

expandRegion运行结束后的结果：

- 函数运行结束后，点集内会出现两种点，分别有区域标记的点和空白的点。
- 在本函数大量迭代之后，如果仍有空白的点，则代表这些空白的点是一个新的区域。因为，这些点是无法通过上一层水位的点扩展到的。

**floodRegion()：寻找新的水源，为水源分配新的regionId**

以一个span作为起始点，按4邻域泛洪填充它所能扩展到的区域

遍历搜索的方式采用深度优先(dfs),而dfs的实现则使用了手动压栈的非递归方式

而每次处理新的节点时，会先判断它的8个邻接节点是否已经有了更早的填充标记，如果有，则说明当前节点处于区域相交处，不扩展该节点（该节点会在下一层的expandRegions时被处理）

**2.2 算法思想：**

下图中**灰色为障碍物**，白色为高度场，数值越大水位越深。

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='698' height='598'></svg>)

第一次遍历进行蔓延并寻找水源，从最深水位6、5找水源，找到的水源为红色：

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='711' height='600'></svg>)

然后开始下一次遍历，让水位变为4、3，红色水位发生了蔓延，同时发现了三个新的水源，分别用黄色，蓝色，绿色表示：

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='695' height='602'></svg>)

然后进行下一次遍历，水位变成2、1，继续蔓延：

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='702' height='602'></svg>)

当水位变为0时候，蔓延结束，如下图：

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='704' height='602'></svg>)

**2.3 源码具体过程**

- 首先，将边界标记出来
- 然后，按距离场中的数值进行排序，从大到小排序（sortCellsByLevel）
- 再然后，开始逐渐”加水“，从大的地方慢慢扩散（expandRegions），对于每一层操作如下：  
    

- 将上一层span加入当前层
- 调用expandRegions函数，将当前已有的区域id扩散直到无法扩散。这时候，对于当前点集存在两种状态。一种是有标记的，这种是扩散成功的。另一种则是没有标记的，表示扩散失败。对于扩散失败的span可以认为这些都是一些新的区域

  

- 将expandRegion的点集复制一份到另一个点集stack中，并调用（floodRegion）函数。该函数也会扩散区域id（条件为大于等于指定水深并且当前无标记），并且对于临界的span（即临界点有更早标记）会被强制清除标记。
- 上一步结束之后，会再次调用expandRegions函数，这时处理的点集是stack的，这次调用把所有未被标记且可走的span也加入stack中在进行之前expandRegions的流程。

**注意**：由于在Stacks中一次性分配max层水位的空间导致内存过大，为了循环利用Stacks内存， 代码中Stacks是分批划分水位。例如如果最深水位是100，不会一次性把水位划分成100层，而是分批次处理，每8层处理一次，由此产生了两个额外函数sortCellsByLevel和appendStacks，本质上都是为了节省内存

1. **区域过滤和裁减（对应源码：mergeAndFilterRegions）**

这一步的目的是创建region对象，建立region之间的连通关系，滤掉合并region。

Denis Haumont、Olivier Debeir和François Sillion[在Volumetric Cell-and-Portal Generation](https://link.zhihu.com/?target=http%3A//artis.imag.fr/Publications/2003/HDS03/)(PDF和Postscript)中已经有一些很棒的分水岭算法可视化，这里我就不重复了。分水岭算法并不完美，由于区域生成算法是依赖于上层场景的，由于游戏场景的无限制性，无论我们如何[shoppingmode 控制](https://link.zhihu.com/?target=https%3A//codeantenna.com/a/JYPxBK9HKc%23)区域生成算法，都必然由于原场景的特异性导致一些奇怪的或者我们不希望出现的区域。此时就需要“过滤”某些区域，以此来增强对区域生成的控制。例如，石子路面上有大量的石子，有的石子很大，但是其相对于路来说，不值一提，我们不希望这些石子形成自己的区域。所以在初始区域生成之后，会使用各种算法进行清理。区域的大小会被优化，太小而无法使用的孤岛区域(例如桌面)会被剔除（黑色表示被剔除的span）

下图中，桌子表面的区域由于太小，被认为是无效的，因此看做是无法使用的岛屿区域被丢弃(标记为不可行走):

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

有时分水岭算法会导致一个区域绕着小的空区域（small null regions）流动。例如：如果一个区域只用一个span包裹着一个角，那么轮廓生成步骤可能会创建一个自交多边形。此时存在合并的最小区域的配置， 当区域大小小于这个配置参数的区域，将在任何有条件的情况下，与其他较大的区域进行合并。

**区域裁剪的算法步骤：**

1. 针对每个区域，采用深度优先遍历其所有邻接区域，如果最终包含的体素数目小于minRegionArea，则将遍历到的所有区域裁剪掉。这个操作是为了减少比较小的孤立区域。

```cpp
// Remove small regions
for (int i = 0; i < nreg; ++i)
{
   if (regions[i].spanCount > 0 && regions[i].spanCount < minRegionArea && !regions[i].connectsToBorder)
   {
      unsigned short reg = regions[i].id;
      for (int j = 0; j < nreg; ++j)
         if (regions[j].id == reg)
            regions[j].id = 0;
   }
}
```

1. 对体素数量过少的区域A进行合并，合并到最小的邻接区域B中。合并过程中，需要将A的邻接区域合并到B的邻接区域中，针对所有区域的邻接区域，需要将其中的A区域需要替换为B区域。

```text
// Check to see if the region should be merged.
if (reg.spanCount > mergeRegionSize && isRegionConnectedToBorder(reg))
   continue;

// Small region with more than 1 connection.
// Or region which is not connected to a border at all.
// Find smallest neighbour region that connects to this one.
int smallest = 0xfffffff;
unsigned short mergeId = reg.id;
// 找到spanCount最少并且可合并的的邻居region
for (int j = 0; j < reg.connections.size(); ++j)  //reg.connections:是与该region连通的其他region
{
   if (reg.connections[j] & RC_BORDER_REG) continue;
  // 邻居region
   rcRegion& mreg = regions[reg.connections[j]];
   if (mreg.id == 0 || (mreg.id & RC_BORDER_REG) || mreg.overlap) continue;
   if (mreg.spanCount < smallest &&
      canMergeWithRegion(reg, mreg) &&
      canMergeWithRegion(mreg, reg))
   {
      smallest = mreg.spanCount;
      mergeId = mreg.id;
   }
}
```

其中canMergeWithRegion函数规定了符合下图的黄色和蓝色region是无法合并的，因为这2个Region之间有多处连接，中间有绿色的region

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='579' height='432'></svg>)

1. 经过区域裁剪和合并后，region会变少，需要对区域的regionID重新remap赋值，以此来降低regionID的最大值。

```cpp
// Compress region Ids.
for (int i = 0; i < nreg; ++i)
{
   regions[i].remap = false;
   if (regions[i].id == 0) continue;           // 跳过空区域
   if (regions[i].id & RC_BORDER_REG) continue;    // 跳过边界区域
   regions[i].remap = true;
}

unsigned short regIdGen = 0;
for (int i = 0; i < nreg; ++i)
{
   if (!regions[i].remap)
      continue;
   unsigned short oldId = regions[i].id;
   unsigned short newId = ++regIdGen;
   for (int j = i; j < nreg; ++j)
   {
      if (regions[j].id == oldId)
      {
         regions[j].id = newId;
         regions[j].remap = false;
      }
   }
}
maxRegionId = regIdGen;

// Remap regions.
for (int i = 0; i < chf.spanCount; ++i)
{
   if ((srcReg[i] & RC_BORDER_REG) == 0)
      srcReg[i] = regions[srcReg[i]].id;
}

return true;
```

示例：在应用算法之前:

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

示例：应用算法之后:

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

过滤和裁减区域算法的目的就是帮助减少不必要的小区域的数量，这一步会很大程度的减少后续处理的复杂度。

### 单调分区（Monotone）

特点是：

- 单调算法，注重效率，在性能上是最快的
- 能将高度场划分为无洞和重叠的区域
- 创建长而细的多边形，有时会导致路径走弯
- 如果要快速生成导航网格，请使用此选项

### 按层分区（Layers）

- 分层算法，折中思想，效果与性能都处于上述两种算法之间
- 将heighfield划分为非重叠区域
- 依靠三角剖分来处理孔（因此比单调分区要慢）
- 产生比单调分区更好的三角形
- 没有分水岭分区的特殊情况
- 速度可能很慢，并且会产生一些难看的镶嵌效果（仍然比单调效果更好），如果您的开放区域较大且障碍物较小（如果使用瓷砖则没有问题）
- 用于中小型瓷砖的导航网格的好选择

### 使用分水岭算法的区域效果

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='1294' height='818'></svg>)

### Region局限性

Region 虽然是不重叠且没有洞的区域，但仍然有可能是凹多边形，无法保证 Region 内任意两点在二维平面一定可以直线到达。后续需要进行轮廓生成和凸多边形生成，为寻路做准备。

## 轮廓生成（Contour Generation）

在经过区域生成之后，region的描述是以span为颗粒度的，复杂度是否可以更简化一下？此时区域与区域之间的分界就是非常重要的信息了。其实我们只需要region的轮廓，而轮廓（Contour）就是描述区域边界的概念。

这个阶段生成表示源几何体的可行走表面的简单多边形（凸多边形和凹多边形）。轮廓仍然以体素空间为单位表示，但这是从体素空间（voxel space）回到向量空间（vector space）的过程中的第一步。

### 搜索区域边缘（Region Edges）

从开放高度场结构转向轮廓结构时，最大的概念变化是从关注span的表面（surface）转变为关注span的边（edges）。

对于轮廓，我们关心span的边，有两种类型的边：

1. 区域边（region ）：区域边是其邻居位于另一个区域中的span的边
2. 内部边（internal）： 内部边是其邻居在同一区域中的span的边

为了便于可视化，接下来的几个示例仅处理 2D，稍后我们将回到 3D：

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

在此步骤中，我们希望将边分类为区域边或内部边。这些信息很容易找到。我们遍历所有span，对于每个span，我们检查所有轴邻居，如果轴邻居与当前span不在同一区域中，则该边将被标记为区域边。

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

这一步的代码：在函数**rcBuildContours**中，遍历判断每个span与邻居span的关系，如果region相同则记录在该dir方向上连通（内部边），不连通则该dir方向为边界（区域边）

```cpp
// Mark boundaries.
for (int y = 0; y < h; ++y)
{
   for (int x = 0; x < w; ++x)
   {
      const rcCompactCell& c = chf.cells[x+y*w];
      for (int i = (int)c.index, ni = (int)(c.index+c.count); i < ni; ++i)
      {
         unsigned char res = 0;
         const rcCompactSpan& s = chf.spans[i];
        //如果span不存在region的ID,或者是边界，就不考虑这种span
         if (!chf.spans[i].reg || (chf.spans[i].reg & RC_BORDER_REG))
         {
            flags[i] = 0;
            continue;
         }
         for (int dir = 0; dir < 4; ++dir)
         {
            unsigned short r = 0;
            if (rcGetCon(s, dir) != RC_NOT_CONNECTED)
            {
               const int ax = x + rcGetDirOffsetX(dir);
               const int ay = y + rcGetDirOffsetY(dir);
               const int ai = (int)chf.cells[ax+ay*w].index + rcGetCon(s, dir);
               r = chf.spans[ai].reg;
            }
            //周围邻居所属的region和当前span的region相同，说明是连通的，标记为1
            if (r == chf.spans[i].reg)
               res |= (1 << dir);
         }
        //flags保存每个span四个方向是否为边界, 值是按位保存：1是边界（区域边），0不是边界（内部边）
         flags[i] = res ^ 0xf; //不连通的方向标记为1
      }
   }
}
```

### 查找区域轮廓（Region Contours）

一旦我们有了关于哪些span的边是区域边的信息，我们就可以通过“遍历这些边”来构建一个轮廓，我们再一次遍历这些span，如果一个span有一个区域边，我们做以下工作:

### 算法描述：

- 找到区域任意一个区域边A，以当前区域边开始算法。
- 如果当前是区域边，将当前区域边添加到轮廓中，然后顺时针旋转90度，继续判断旋转后的边。
- 如果当前是内部边，则进入到共当前边的邻居内，然后逆时针旋转90度，继续判断旋转后的边。
- 直到回到区域边A为止，结束，此时依次添加进轮廓中的所有边界边全部查找完毕。

### 举例说明

- 首先面向已知的区域边，把它加到轮廓中：

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

- 顺时针旋转90度，继续判断旋转后的边，由于还是区域边，因此继续增加这个区域边到轮廓中，直到找到一个内部边。向前步进到邻居span：

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

- 逆时针旋转90度，继续判断旋转后的边，由于是内部边，则进入到当前边的邻居内，然后逆时针旋转90度（下图第3步）继续判断旋转后的边是区域边还是内部边，重复上一步操作。一直继续到我们回到起始span，面对起始方向：

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

  
与寻找邻接区域类似，都是沿着区域边界顺时针行走。行走过程中取轮廓点的规则为:

1. 体素左方是边界，轮廓点取其上方体素。
2. 体素上方是边界，轮廓点取其右上方体素。
3. 体素右方是边界，轮廓点取其右方体素。
4. 体素下方是边界，轮廓点取其自身。

**这样做的目的是，使得各个区域的轮廓线多边形的边互相重合，因为最终生成的navimesh数据多边形之间是共用一个边的**，最终效果如下图所示：

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='652' height='602'></svg>)

### 查找轮廓点的代码

```cpp
static void walkContour(int x, int y, int i,
                  rcCompactHeightfield& chf,
                  unsigned char* flags, rcIntArray& points)
{
   // 找到第一个区域边的方向dir
   unsigned char dir = 0;
   while ((flags[i] & (1 << dir)) == 0)
      dir++;

   unsigned char startDir = dir;
   int starti = i;

   const unsigned char area = chf.areas[i];

   int iter = 0;
   while (++iter < 40000)  //迭代次数限制
   {
       // dir方向指向区域边界，则保存轮廓点后，顺时针旋转后再循环尝试
      if (flags[i] & (1 << dir))  //当前边是区域边
      {
         // Choose the edge corner
         bool isBorderVertex = false;
         bool isAreaBorder = false;
        //默认轮廓点取其自身。
         int px = x;
         int py = getCornerHeight(x, y, i, dir, chf, isBorderVertex);
         int pz = y;
        // 为了使相邻region walk出来的轮廓一样，所以并不一定是以自身为轮廓，而是按照一下规则
         switch(dir)
         {
            case 0: pz++; break;       //1. 体素左方是边界，轮廓点取其上方体素。
            case 1: px++; pz++; break; //2. 体素上方是边界，轮廓点取其右上方体素。
            case 2: px++; break;       //3. 体素右方是边界，轮廓点取其右方体素。
         }
         int r = 0;
         const rcCompactSpan& s = chf.spans[i];
         if (rcGetCon(s, dir) != RC_NOT_CONNECTED)
         {
            const int ax = x + rcGetDirOffsetX(dir);
            const int ay = y + rcGetDirOffsetY(dir);
            const int ai = (int)chf.cells[ax+ay*chf.width].index + rcGetCon(s, dir);
            r = (int)chf.spans[ai].reg;
            if (area != chf.areas[ai]) // area的边界
               isAreaBorder = true;   
         }
         if (isBorderVertex)
            r |= RC_BORDER_VERTEX;
         if (isAreaBorder)
            r |= RC_AREA_BORDER;
        //添加到轮廓中
         points.push(px);
         points.push(py);
         points.push(pz);
         points.push(r);
         //去掉该dir上的边界标记
         flags[i] &= ~(1 << dir);
         //然后顺时针旋转90度，继续判断旋转后的边
         dir = (dir+1) & 0x3; 
      }
      else   // // 如果不是区域边界，当前边是内部边，则移动到邻居内，并将dir逆时针旋转
      {
         int ni = -1;
         const int nx = x + rcGetDirOffsetX(dir);
         const int ny = y + rcGetDirOffsetY(dir);
         const rcCompactSpan& s = chf.spans[i];
         if (rcGetCon(s, dir) != RC_NOT_CONNECTED)
         {
            const rcCompactCell& nc = chf.cells[nx+ny*chf.width];
            ni = (int)nc.index + rcGetCon(s, dir);
         }
         if (ni == -1)
         {
            // Should not happen.
            return;
         }
        //进入到共当前边的邻居内
         x = nx;
         y = ny;
         i = ni;
         dir = (dir+3) & 0x3; //然后逆时针旋转90度，等待继续判断旋转后的边
      }

      if (starti == i && startDir == dir) //我们回到起始span，面对起始方向，结束查找
      {
         break;
      }
   }
}
```

### 从边到顶点

如果我们想回到向量空间，我们需要的是顶点，而不是边。顶点的(x, z)值很容易确定。对于每条边，我们从边沿顺时针方向取span顶部的角（corner）的(x, z)值(从span内部看)：

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

确定顶点的y值就比较棘手了。这就是我们回归3D可视化的地方。在下面的例子中，我们选择哪个顶点?

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

所有的边的顶点最多有四个可能的y值（译注：4个高低不同的span相邻）。选择最高的y值有两个原因：它确保最终顶点(x, y, z)位于源网格表面的上方。它还提供了一个通用的选择机制，以便所有使用该顶点的轮廓将使用相同的高度：

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

计算顶点的Y值的代码

```cpp
static int getCornerHeight(int x, int y, int i, int dir,
                     const rcCompactHeightfield& chf,
                     bool& isBorderVertex)
{
   const rcCompactSpan& s = chf.spans[i];
   int ch = (int)s.y; //span的下表面高度
   int dirp = (dir+1) & 0x3;
    // 假设dir=上，则regs[4]每个索引分别表示 0=自己 1=上 2=右上 3=右
   unsigned int regs[4] = {0,0,0,0};

   //把regionid和areaid组合
   regs[0] = chf.spans[i].reg | (chf.areas[i] << 16);
   //可行走的边界，即邻居为其他region
   if (rcGetCon(s, dir) != RC_NOT_CONNECTED)
   {
      const int ax = x + rcGetDirOffsetX(dir);
      const int ay = y + rcGetDirOffsetY(dir);
      const int ai = (int)chf.cells[ax+ay*chf.width].index + rcGetCon(s, dir);
      const rcCompactSpan& as = chf.spans[ai];
      ch = rcMax(ch, (int)as.y);
      regs[1] = chf.spans[ai].reg | (chf.areas[ai] << 16);
      if (rcGetCon(as, dirp) != RC_NOT_CONNECTED)
      {
         const int ax2 = ax + rcGetDirOffsetX(dirp);
         const int ay2 = ay + rcGetDirOffsetY(dirp);
         const int ai2 = (int)chf.cells[ax2+ay2*chf.width].index + rcGetCon(as, dirp);
         const rcCompactSpan& as2 = chf.spans[ai2];
         ch = rcMax(ch, (int)as2.y);
         regs[2] = chf.spans[ai2].reg | (chf.areas[ai2] << 16);
      }
   }
   if (rcGetCon(s, dirp) != RC_NOT_CONNECTED)
   {
      const int ax = x + rcGetDirOffsetX(dirp);
      const int ay = y + rcGetDirOffsetY(dirp);
      const int ai = (int)chf.cells[ax+ay*chf.width].index + rcGetCon(s, dirp);
      const rcCompactSpan& as = chf.spans[ai];
      ch = rcMax(ch, (int)as.y);
      regs[3] = chf.spans[ai].reg | (chf.areas[ai] << 16);
      if (rcGetCon(as, dir) != RC_NOT_CONNECTED)
      {
         const int ax2 = ax + rcGetDirOffsetX(dir);
         const int ay2 = ay + rcGetDirOffsetY(dir);
         const int ai2 = (int)chf.cells[ax2+ay2*chf.width].index + rcGetCon(as, dir);
         const rcCompactSpan& as2 = chf.spans[ai2];
         ch = rcMax(ch, (int)as2.y);
         regs[2] = chf.spans[ai2].reg | (chf.areas[ai2] << 16);
      }
   }

   //检查顶点是否为特殊边顶点，这些顶点稍后会被移除。
   for (int j = 0; j < 4; ++j)
   {
      const int a = j;
      const int b = (j+1) & 0x3;
      const int c = (j+2) & 0x3;
      const int d = (j+3) & 0x3;

      // 自己和上的region和area相同，并且都是边界
      const bool twoSameExts = (regs[a] & regs[b] & RC_BORDER_REG) != 0 && regs[a] == regs[b];
      // 右和右上都不是边界
      const bool twoInts = ((regs[c] | regs[d]) & RC_BORDER_REG) == 0;
      const bool intsSameArea = (regs[c]>>16) == (regs[d]>>16);
      // 右和右上area相同
      const bool noZeros = regs[a] != 0 && regs[b] != 0 && regs[c] != 0 && regs[d] != 0;
      if (twoSameExts && twoInts && intsSameArea && noZeros)
      {
         isBorderVertex = true;
         break;
      }
   }

   return ch;
}
```

### 简化轮廓

至此，我们已经为所有区域生成了轮廓。到这一步时，轮廓点由一系列**连续的点**组成的，在这些点里，有一些点是共线的，有一些点忽略后与最终轮廓形状差距不大。下面是一个宏观的视角。请注意，有两种类型的轮廓部分（contour sections）：

1. 两个相邻区域之间门户（portal）的部分，即连接两个有效区域之间的边界边。
2. 与“无效”区域接壤的部分。无效区域被称为“空区域”（null region），我在这里使用同样的术语。

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

真的需要这么多顶点吗？

即使在直线轮廓上，构成边的每个span都有一个顶点。显然，答案是否定的。唯一真正必需的顶点是那些在区域连接中发生变化的顶点。

去除一些对轮廓形状影响不大的点，得到更加丝滑的轮廓，可以有效减少锯齿轮廓。

这里约定一种顶点的概念：强制性顶点（Mandatory Vertices），它的含义是区域连接发生变化的顶点，可以看出有两种顶点：

- 连接有效区域的边界边上的顶点
- 连接有效区域和无效区域边界上的顶点

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

考虑一种特殊情况，可以很容易证明出，并不是所有的区域都有“强制性顶点”，此时如何进行上述算法呢？很简单，随便找两个相对较远的顶点作为强制性顶点即可。Recast的做法是，使用连接有效区域和无效区域边界上的顶点，从轮廓点中选择最左下和最右上的两个点作为初始简化点。

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='499' height='406'></svg>)

### Region-Region Portals的轮廓简化

区域-区域门户（region-region portals）的简化很容易。我们丢弃除强制性顶点（mandatory vertices）之外的所有顶点：

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

### 轮廓简化具体代码

simplifyContour代码：

先找到初始简化点

```cpp
// Add initial points.
bool hasConnections = false;
for (int i = 0; i < points.size(); i += 4)
{
   // point索引：0=x 1=y 2=z 3=r
   // 在之前的walkContour中产生，r的低16位如果是0，说明边界是不可行走的，否则该point有邻居region
   if ((points[i+3] & RC_CONTOUR_REG_MASK) != 0)
   {
        hasConnections = true;
	 break;
   }
}
 // 如果轮廓有邻居region
if (hasConnections)
{
   for (int i = 0, ni = points.size()/4; i < ni; ++i)
   {
      //下一个point
      int ii = (i+1) % ni;
      //邻近的两个轮廓点接壤不同的region
      const bool differentRegs = (points[i*4+3] & RC_CONTOUR_REG_MASK) != (points[ii*4+3] & RC_CONTOUR_REG_MASK);
      //邻近的两个轮廓点里，一个接壤其他region，另一个接壤不可行走
      const bool areaBorders = (points[i*4+3] & RC_AREA_BORDER) != (points[ii*4+3] & RC_AREA_BORDER);
      // 总之邻近的两个轮廓点接壤不是同一个region，则记录这个点
      if (differentRegs || areaBorders)
      {
         simplified.push(points[i*4+0]);
         simplified.push(points[i*4+1]);
         simplified.push(points[i*4+2]);
         simplified.push(i);
      }
   }
}
```

如果不连接任何region则没有简化点，那么选择左下和右上的两个点作为简化点

```cpp
// 如果不连接任何region则没有simplified点，那么选择左下和右上的两个点作为simplified点
if (simplified.size() == 0)
{
   int llx = points[0];
   int lly = points[1];
   int llz = points[2];
   int lli = 0;
   int urx = points[0];
   int ury = points[1];
   int urz = points[2];
   int uri = 0;
   for (int i = 0; i < points.size(); i += 4)
   {
      int x = points[i+0];
      int y = points[i+1];
      int z = points[i+2];
      if (x < llx || (x == llx && z < llz))
      {
         llx = x;
         lly = y;
         llz = z;
         lli = i/4;
      }
      if (x > urx || (x == urx && z > urz))
      {
         urx = x;
         ury = y;
         urz = z;
         uri = i/4;
      }
   }
   simplified.push(llx);
   simplified.push(lly);
   simplified.push(llz);
   simplified.push(lli);

   simplified.push(urx);
   simplified.push(ury);
   simplified.push(urz);
   simplified.push(uri);
}
```

根据**maxSimplificationError**参数来决定丢弃哪些顶点以得到简化的线段。

```cpp
/// The maximum distance a simplfied contour's border edges should deviate 
/// the original raw contour. [Limit: >=0] [Units: vx]
float maxSimplificationError;
```

代表网格的边可以偏离源几何体的最大距离，较低的值将导致网格边缘更准确地遵循 xz 平面的几何轮廓，但会增加三角形数量。  
不建议将值设为0，因为它会导致最终网格中的多边形数量大幅增加，处理成本很高。

**示例1**：完全关闭边缘匹配。生成器尽可能创建最简单的边，但结果很差：

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

**示例2**：适度的边缘匹配可以使网格更好地遵循源几何体的边缘：

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

**示例3**：最大边缘匹配的结果是网格非常接近源几何体边缘，但多边形数量过多：

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

它从强制顶点（mandatory vertices）开始，然后将最远顶点添加回来，这样原始顶点与简化边之间的距离都不会超过**maxSimplificationError**。

一步一步地来：从最简单的边开始：

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

找到离简化边最远的点，如果它到简化轮廓的距离超过了maxSimplificationError，则将顶点添加回轮廓：

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

重复这个过程，直到不再有顶点到简化边的距离超过允许值：

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

这部分的轮廓简化代码：

```cpp
// Add points until all raw points are within
// error tolerance to the simplified shape.
const int pn = points.size()/4;
for (int i = 0; i < simplified.size()/4; )
{
   int ii = (i+1) % (simplified.size()/4);
   // simplified索引：0=x 1=y 2=z 3=在points中的索引
   int ax = simplified[i*4+0];
   int az = simplified[i*4+2];
   int ai = simplified[i*4+3];

   int bx = simplified[ii*4+0];
   int bz = simplified[ii*4+2];
   int bi = simplified[ii*4+3];

   // Find maximum deviation from the segment.
   float maxd = 0;
   int maxi = -1;
   // ci、endi为points中的索引，cinc为索引每次遍历的偏移方向
   // ci=从此索引开始遍历，endi=从此索引遍历结束
   int ci, cinc, endi;
  // 选择偏左下的点为遍历的起点，偏右上的点为遍历的终点
   if (bx > ax || (bx == ax && bz > az))
   {
     // 沿着正方向
      cinc = 1;
      ci = (ai+cinc) % pn;
      endi = bi;
   }
   else
   {
      // 沿着负方向
      cinc = pn-1;
      ci = (bi+cinc) % pn;
      endi = ai;
      rcSwap(ax, bx);
      rcSwap(az, bz);
   }

   // 考虑有效区域和有效区域连接，或者有效区域和空区域的边界情况. 
   if ((points[ci*4+3] & RC_CONTOUR_REG_MASK) == 0 ||
      (points[ci*4+3] & RC_AREA_BORDER))
   {
      while (ci != endi)
      {
        //计算点到直线的距离
         float d = distancePtSeg(points[ci*4+0], points[ci*4+2], ax, az, bx, bz);
         if (d > maxd)
         {
            maxd = d;
            maxi = ci;
         }
         ci = (ci+cinc) % pn;
      }
   }


   // 找到离简化边最远的点，如果它到简化轮廓的距离超过了maxError，则将顶点添加回轮廓
   if (maxi != -1 && maxd > (maxError*maxError))
   {
      // Add space for the new point.
      simplified.resize(simplified.size()+4);
      const int n = simplified.size()/4;
      for (int j = n-1; j > i; --j)
      {
         simplified[j*4+0] = simplified[(j-1)*4+0];
         simplified[j*4+1] = simplified[(j-1)*4+1];
         simplified[j*4+2] = simplified[(j-1)*4+2];
         simplified[j*4+3] = simplified[(j-1)*4+3];
      }
      // Add the point.
      simplified[(i+1)*4+0] = points[maxi*4+0];
      simplified[(i+1)*4+1] = points[maxi*4+1];
      simplified[(i+1)*4+2] = points[maxi*4+2];
      simplified[(i+1)*4+3] = maxi;
   }
   else
   {
      ++i;
   }
}
```

### 轮廓简化效果

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

### 长边轮廓二分为短边

如前所述，还有一种算法用于连接到空区域（null region）的边。第一种算法可以生成长线段，以至在随后的网格生成过程中生成长而细的三角形。第二个算法，使用maxEdgeLen参数重新插入顶点，以确保没有线段超过最大长度。

```cpp
/// The maximum allowed length for contour edges along the border of the mesh. [Limit: >=0] [Units: vx] 
int maxEdgeLen;
```

它是通过检测长边，然后将它们分成两半来实现这一点的。它会继续这个过程，直到检测不到过长的边为止。

**优化长边的代码：**

```cpp
// Split too long edges.
if (maxEdgeLen > 0 && (buildFlags & (RC_CONTOUR_TESS_WALL_EDGES|RC_CONTOUR_TESS_AREA_EDGES)) != 0)
{
   for (int i = 0; i < simplified.size()/4; )
   {
      const int ii = (i+1) % (simplified.size()/4);

      const int ax = simplified[i*4+0];
      const int az = simplified[i*4+2];
      const int ai = simplified[i*4+3];

      const int bx = simplified[ii*4+0];
      const int bz = simplified[ii*4+2];
      const int bi = simplified[ii*4+3];

      // Find maximum deviation from the segment.
      int maxi = -1;
      int ci = (ai+1) % pn;

      // Tessellate only outer edges or edges between areas.
      bool tess = false;
      // Wall edges. 不可行走边界
      if ((buildFlags & RC_CONTOUR_TESS_WALL_EDGES) && (points[ci*4+3] & RC_CONTOUR_REG_MASK) == 0)
         tess = true;
      // Edges between areas. region边界
      if ((buildFlags & RC_CONTOUR_TESS_AREA_EDGES) && (points[ci*4+3] & RC_AREA_BORDER))
         tess = true;

      if (tess)
      {
         int dx = bx - ax;
         int dz = bz - az;
         if (dx*dx + dz*dz > maxEdgeLen*maxEdgeLen)  //线段超过最大长度maxEdgeLen，就分为两个线段
         {
            // Round based on the segments in lexilogical order so that the
            // max tesselation is consistent regardles in which direction
            // segments are traversed.
            const int n = bi < ai ? (bi+pn - ai) : (bi - ai);// ai与bi相差n个索引
            if (n > 1) // n > 1，说明ai bi之间有轮廓点，可切分
            {
               if (bx > ax || (bx == ax && bz > az))
                  maxi = (ai + n/2) % pn;
               else
                  maxi = (ai + (n+1)/2) % pn;
            }
         }
      }

      // If the max deviation is larger than accepted error,
      // add new point, else continue to next segment.
      if (maxi != -1)  // maxi位置的点插入到simplified中
      {
         // Add space for the new point.
         simplified.resize(simplified.size()+4);
         const int n = simplified.size()/4;
         for (int j = n-1; j > i; --j)
         {
            simplified[j*4+0] = simplified[(j-1)*4+0];
            simplified[j*4+1] = simplified[(j-1)*4+1];
            simplified[j*4+2] = simplified[(j-1)*4+2];
            simplified[j*4+3] = simplified[(j-1)*4+3];
         }
         // Add the point.
         simplified[(i+1)*4+0] = points[maxi*4+0];
         simplified[(i+1)*4+1] = points[maxi*4+1];
         simplified[(i+1)*4+2] = points[maxi*4+2];
         simplified[(i+1)*4+3] = maxi;
      }
      else
      {
         ++i;
      }
   }
}
```

长边优化前

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

长边优化后

设置了maxEdgeLen之后：

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

### 检查和合并空洞

### 检查空洞

算法原理

再说检查空洞之前，先讲一下三角形面积和向量叉乘的关系，可以看这个文章的讲解：[向量 求 面积 calcAreaOfPolygon2D 凹凸多边形 顺时针 逆时针](https://link.zhihu.com/?target=https%3A//codeantenna.com/a/HSIoSUjZXt)

文章的结论是如果是顺时针方向，求取的面积值是负的，如果是逆时针方向，求取的面积值是正的

Recast也提供了计算多边形面积的函数

```cpp
static int calcAreaOfPolygon2D(const int* verts, const int nverts)
{
   int area = 0;
   for (int i = 0, j = nverts-1; i < nverts; j=i++)
   {
      const int* vi = &verts[i*4];
      const int* vj = &verts[j*4];
      area += vi[0] * vj[2] - vj[0] * vi[2];
   }
   return (area+1) / 2;
}
```

而正常轮廓线的顶点是顺时针存储，空洞轮廓线的顶点是逆时针存储。如下图所示：

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

所以根据叉乘算出每个轮廓多边形的有向面积，如果结果为小于0，则为轮廓点的顺序为逆时针，这个轮廓就是一个空洞。

检查空洞个数的代码：

```cpp
int nholes = 0;
for (int i = 0; i < cset.nconts; ++i)
{
   rcContour& cont = cset.conts[i];
   // If the contour is wound backwards, it is a hole.
   winding[i] = calcAreaOfPolygon2D(cont.verts, cont.nverts) < 0 ? -1 : 1;
   if (winding[i] < 0)
      nholes++;
}
```

### 合并空洞

算法原理

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

1. 找到空洞的左下方顶点B4
2. 将轮廓线所有顶点与B4相连，如果连线与轮廓线、空洞都不相交，则连线构成1条对角线。上图满足条件的有A5B4，A5B4，A4B4
3. 选择其中长度最短的1条对角线A5B4，将空洞合并到轮廓线中

最终轮廓线的顶点序列为A5、A6、A1、A2、A3、A4、A5、B4、B1、B2、B3、B4。(如果包含多个空洞的话，将空洞按左下方顶点排序，依次迭代将外围轮廓与空洞进行合并。)

空洞的数据结构：

```text
struct rcContourHole //空洞的数据结构
{
   rcContour* contour;
   int minx, minz, leftmost;
};

struct rcContourRegion //一个外轮廓和内部N个空洞
{
   rcContour* outline;
   rcContourHole* holes;
   int nholes;
};
```

region相同的M个轮廓放入rcContourRegion中，M个轮廓中只有一个外轮廓和N（N>=0）个空洞轮廓。

```cpp
for (int i = 0; i < cset.nconts; ++i)
{
   rcContour& cont = cset.conts[i];
   if (winding[i] > 0) //这个轮廓是外轮廓
   {
      if (regions[cont.reg].outline)
         ctx->log(RC_LOG_ERROR, "rcBuildContours: Multiple outlines for region %d.", cont.reg);
      regions[cont.reg].outline = &cont;
   }
   else  //这个轮廓是空洞
   {
      regions[cont.reg].nholes++;
   }
}
```

空洞进行合并（mergeRegionHoles）

找到空洞左下方的点：

```cpp
// 找到每个空洞的最左点
for (int i = 0; i < region.nholes; i++)
   findLeftMostVertex(region.holes[i].contour, ®ion.holes[i].minx, ®ion.holes[i].minz, ®ion.holes[i].leftmost);

 //按照最左点排序空洞
qsort(region.holes, region.nholes, sizeof(rcContourHole), compareHoles);
```

将外轮廓线所有顶点与空洞的最左点相连，如果连线与外轮廓线、空洞都不相交，并且连线长度最短的，把此时的空洞的最优的最左点索引**bestVertex**和外轮廓点的**索引index，传入**mergeContours函数，将空洞合并到轮廓线中：

```cpp
int bestVertex = region.holes[i].leftmost; 

int index = -1;
// 遍历筛选出来的外轮廓点与corner的连线，是否与外轮廓相交
for (int j = 0; j < ndiags; j++)
{
     const int* pt = &outline->verts[diags[j].vert*4];
     // 连线是否与外轮廓相交
     bool intersect = intersectSegCountour(pt, corner, diags[j].vert, outline->nverts, outline->verts);
     // 连线是否与其他空洞相交
     for (int k = i; k < region.nholes && !intersect; k++)
	 intersect |= intersectSegCountour(pt, corner, -1, region.holes[k].contour->nverts, region.holes[k].contour->verts);
     if (!intersect)
     {
         index = diags[j].vert;
	  break;
      }
}
// 合并外轮廓和洞
if (!mergeContours(*region.outline, *hole, index, bestVertex))
```

### 轮廓生成（Contour Generation）阶段总结

首先，从区域生成高度详细的多边形轮廓：

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='1245' height='748'></svg>)

接下来，使用各种算法来简化轮廓：

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='1234' height='715'></svg>)

在这个阶段结束时，我们有形成简化多边形的轮廓。顶点仍然在体素空间中，但是我们正在回到向量空间的路上。

## 凸多边形生成（Convex Polygon Generation）

有了轮廓的数据之后，就有了一片区域的边，那么此时就需要对区域进行更加详细的定位，例如寻路是要具体寻到某一个点，并且区域内部任意两点并不是一定直线联通的，所以要将区域划分成更加细化的可以描述整个区域面的信息的数据。此时采用的是将区域划分成一些凸多边形的集合，这些凸多边形的并集就是整个区域。本阶段是从由轮廓表示的简单多边形生成凸多边形。这也是我们从体素空间回到向量空间的地方。

### 为什么必须使用凸多边形呢？

是因为凸多边形在几何上就限定了其形状的规范性，至少其能保证，其内部任意两点之间是直线联通的，这对于后续算法有很重要的意义。

对应的源码函数：

```cpp
bool rcBuildPolyMesh(rcContext* ctx, rcContourSet& cset, const int nvp, rcPolyMesh& mesh)
```

### 概述

本阶段的主要任务如下：

- 轮廓的顶点在体素空间中，这意味着它们的坐标采用整数格式并表示到高度场原点的距离。因此轮廓顶点数据被转换为原始源几何体的向量空间坐标。
- 每个轮廓完全独立于所有其他轮廓。在此阶段，我们合并重复的数据并将所有内容合并到一个单一网格中。
- 轮廓只能保证表示简单的多边形，包括凸多边形和凹多边形。凹多边形对导航网格来说是没有用的，所以我们根据需要细分轮廓以获得凸多边形。
- 我们收集指示每个多边形的哪些边连接到另一个多边形的连接信息（多边形邻居信息）。

坐标转换和顶点数据合并是相对简单的过程，所以我不会在这里讨论它们。如果您对这些算法感兴趣，可以查看文档详尽的源代码。这里我们将专注于凸多边形的细分。对每个轮廓会执行以下步骤：

1. 对每个轮廓进行三角面化（triangulate）。
2. 合并三角形以形成最大可能的凸多边形。

我们通过生成邻居连接信息来结束这个阶段。

### 三角形剖分（Triangulation）

有很多经典的“三角剖分”算法，比如比较经典的“翻边算法”、“逐点插入算法”、“分割合并算法”——这些都是基于离散点的Delaunay三角剖分算法。而现在的需求其实是对多边形进行三角划分，此处只讨论使用的针对“凹多边形”的三角划分算法（凸多边形同样适用，并且更简单）。

针对凹多边形的三角划分：**耳裁法**

- 每次迭代凹多边形，将其分成两部分，一个三角形和剩余的部分。然后迭代“剩余的部分”，继续划分，直到没有三角形可以划分位置。
- 算法的核心点是如何对每次的“剩余多边形”划分出一个三角形。
- 采用的方法很简单，基于“任意不共线三点构成三角形”的理论，每次寻找多边形相邻的两条边，如果其不共直线，那么连接这两条边的三个端点，就可以形成一个三角形。
- 但是需要注意的是，这样形成的三角形可能会是多边形外部的，注意区分剔除即可。

从所有潜在的候选者中，选择具有最短的新边（译注：即下图中的虚线）的那个。新边被称为“分割边”（partition edges），或简称为“分割”（partition）。该过程继续处理剩余的顶点，直到三角形剖分完成。

为了提高性能，三角形剖分在轮廓的 xz 平面投影上运行。

### 构建潜在的分割边（partitions）:

三角形剖分是通过沿着轮廓的边往前走（walking the edges of the contour）来完成的，以任意端点开始，沿着一个方向依次去找两个边，如果两个边不共线，则连接两边不重合的点，形成一条连线，称为“分割边”。对每一个点进行该过程，会形成很多分割边，剔除那些在多边形外部的分割边，剩余的就是有效分割边。

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

### 耳裁法划分三角形

在上述所有有效分割边中，找出一条最短的分割边，然后将该分割边与“形成该分割边的其他两条多边形边”形成一个三角形。此时就将凹多边形划分出了一个三角形。剩余多边形继续重新划分“分割边”，重复该过程即可。

使用最短分割边的原因是，在概率上试图每次分出去一个尽可能小的三角形，以此增加最终分割的三角形的数量，进而增强分割后的信息量。

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

继续分割…

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

…直到三角形剖分完成:

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

耳裁法的思想

**找到最小的耳朵，然后把最小耳朵裁切掉，再从剩下的多边形中找最小的耳朵裁切……直到所有多边形被裁切为三角形**

耳尖的概念

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

### 检测有效分割边（Valid Partitions）

使用**内角算法**和**边相交算法，**两种算法来确定一组三个顶点是否可以形成有效的内部三角形。第一种算法（内角算法）很快，可以快速剔除完全位于多边形之外的分割边。如果分割边在多边形内部，则使用更昂贵的算法来确保它不与任何现有多边形的边相交。

* 内角算法（The Internal Angle Algorithm）

这个算法可能有点难以描述。所以这里有一些例子。首先看一个有效分割边的例子。对于顶点 A、B 和 C，其中 AB 是潜在的分割边…

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

沿着连接到顶点 A 的边打出两条射线。内角是多边形方向上的角。 如果分割边的端点（顶点 B）在内角内，则它是潜在的有效分割边:

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

第二个例子是使用不同顶点的相同场景。因为分割边的端点（顶点B）在内角之外，所以它不是一个有效分割边:

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

* 边相交算法（The Edge Intersection Algorithm）

这个算法容易理解一些。它只是循环遍历多边形中的所有边并检查潜在的分割边是否与它们中的任何一个相交。如果是，则它不是有效的分割边。只有当两个算法都通过时，分割边才被认为是有效的。

### 三角形剖分代码（Triangulation）

```cpp
/// 三角形剖分
/// n verts顶点个数
/// verts 顶点数据
/// indices 顶点索引
/// tris 三角形的索引
/// 返回值 三角形的个数
static int triangulate(int n, const int* verts, int* indices, int* tris)
{
   int ntris = 0;
   int* dst = tris;

   // The last bit of the index is used to indicate if the vertex can be removed.
   for (int i = 0; i < n; i++)
   {
      int i1 = next(i, n);
      int i2 = next(i1, n);
        //i1 是一个耳尖点，并且与所有的边都不相交
      if (diagonal(i, i2, n, verts, indices))
         indices[i1] |= 0x80000000;
   }

   while (n > 3)
   {
      int minLen = -1;
      int mini = -1;
      // 找最小的耳朵
      for (int i = 0; i < n; i++)
      {
         int i1 = next(i, n);
         if (indices[i1] & 0x80000000)
         {
            // i1是耳尖点，找到最小的p0到p2的距离
            const int* p0 = &verts[(indices[i] & 0x0fffffff) * 4];
            const int* p2 = &verts[(indices[next(i1, n)] & 0x0fffffff) * 4];

            int dx = p2[0] - p0[0];
            int dy = p2[2] - p0[2];
            int len = dx*dx + dy*dy;

            if (minLen < 0 || len < minLen)
            {
               minLen = len;
               mini = i;
            }
         }
      }

      if (mini == -1)
      {
         // We might get here because the contour has overlapping segments, like this:
         //
         //  A o-o=====o---o B
         //   /  |C   D|    \.
         //  o   o     o     o
         //  :   :     :     :
         // We'll try to recover by loosing up the inCone test a bit so that a diagonal
         // like A-B or C-D can be found and we can continue.
         minLen = -1;
         mini = -1;
         for (int i = 0; i < n; i++)
         {
            int i1 = next(i, n);
            int i2 = next(i1, n);
            if (diagonalLoose(i, i2, n, verts, indices))
            {
               const int* p0 = &verts[(indices[i] & 0x0fffffff) * 4];
               const int* p2 = &verts[(indices[next(i2, n)] & 0x0fffffff) * 4];
               int dx = p2[0] - p0[0];
               int dy = p2[2] - p0[2];
               int len = dx*dx + dy*dy;

               if (minLen < 0 || len < minLen)
               {
                  minLen = len;
                  mini = i;
               }
            }
         }
         if (mini == -1)
         {
            // The contour is messed up. This sometimes happens
            // if the contour simplification is too aggressive.
            return -ntris;
         }
      }

      int i = mini;
      int i1 = next(i, n);
      int i2 = next(i1, n);

      *dst++ = indices[i] & 0x0fffffff;
      *dst++ = indices[i1] & 0x0fffffff;
      *dst++ = indices[i2] & 0x0fffffff;
      ntris++;

      // Removes P[i1] by copying P[i+1]...P[n-1] left one index.
      n--;
      for (int k = i1; k < n; k++)
         indices[k] = indices[k+1];

      if (i1 >= n) i1 = 0;
      i = prev(i1,n);
      // Update diagonal flags.
        // 判断i点是否为耳尖点
      if (diagonal(prev(i, n), i1, n, verts, indices))
         indices[i] |= 0x80000000;
      else
         indices[i] &= 0x0fffffff;
        // 判断i1点是否为耳尖点
      if (diagonal(i, next(i1, n), n, verts, indices))
         indices[i1] |= 0x80000000;
      else
         indices[i1] &= 0x0fffffff;
   }

   // Append the remaining triangle.
    // 把最后的三个点加入到tris
   *dst++ = indices[0] & 0x0fffffff;
   *dst++ = indices[1] & 0x0fffffff;
   *dst++ = indices[2] & 0x0fffffff;
   ntris++;

   return ntris;
}
```

### 合并为凸多边形（凸多边形化）

合并只能发生在从单个轮廓创建的多边形之间。不会尝试合并来自相邻轮廓的多边形。

请注意，我已切换到一般形式的“多边形”（polygon）而不是三角形（triangle）。虽然初始合并将在三角形之间进行，但随着合并过程的进行，非三角形多边形可能会相互合并。

该过程如下：

1. 找出所有可以合并的多边形。
2. 从该列表中，选择共享边最长的两个多边形并将它们合并。
3. 重复这个过程直到没有可以进行的合并。

如果满足以下所有条件，则可以合并两个多边形：

- 多边形共享一条边。
- 合并后的多边形仍然是凸多边形。
- 合并后的多边形的边数不会超过**maxVertsPerPoly**

### 为什么选择最长边进行合并？

这是一种“伪贪心”思想，试图从概率上使每次合并的多边形更大，从而减少合并后多边形的数量，数量越少，后续的Detour算法越简单。

### 如何确定合并后的多边形是否为凸多边形？

检测共享边和确定合并后的边数都很容易。确定合并后的多边形是否为凸多边形有点复杂。此检查的关键就是保证合并后的多边形所有内角不超过180度。此处采用的是”内角判断“。

合并多边形ABC和多边形ADC，两者的公共边是AC，前提条件是，∠ABC和∠ADC都是小于180度的。问题是证明合并后的∠BAD和∠BCD在什么情况下会小于180度，在什么情况下会大于180度。

采用的方式是，连接BD作为参考线。BD产生的条件，从公共边两端点中选择一点A，点A在两个多边形中分别有两条边，选择不是公共边的那一条，即图中的AB和AD作为“校验边”，此时连接两条校验边的另外一个端点B和D，形成参考线。

形成参考线后，只需要保证公共边两端点分属在参考线的两侧，即证明以刚才选择点A所形成的内角∠BAD是一个小于180度的角。

然后再以公共边另外一个端点C重复上述过程，证明∠BCD是否满足条件。

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='762' height='415'></svg>)

反证法，理解上述方法的合理性（以下图为例）：

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='861' height='416'></svg>)

很明显两个多边形合并之后不是一个凸多边形，公共边是AD。用点D先来校验合并后的内角∠BDC。

当形成参考线后，发现公共边两点A和D在参考线BC的同侧。同时，点D，B，C三点可以形成一个三角形，三角形的内角一定小于180度，所以在三角形BDC中，∠BDC是小于180度的。

三角形BDC中的内角∠BDC恰好是合并多边形的一个外角。外角小于180度，对应的内角则大于180度了。

其实很好理解，用公共边一个端点和参考线一定可以形成一个三角形，只需要判断这个三角形的当前内角是合并多边形的内角还是外角就可以了。而当公共边的两端点在参考线两侧的时候，恰好对应的是内角。至于以公共边的两端点来进行这个校验，是因为我们保证了，合并之前的多边形都是凸多边形，意味着所有不参与合并的角本来就是满足条件的，不需要检查了，而参与合并的角，就是公共边两端点的角。

一定要理解：上述算法的所有思路，都是建立在合并之前的两个多边形都是凸多边形的基础上。

凸多边形化的代码：

```cpp
// Merge polygons.
if (nvp > 3)
{
   for(;;)
   {
      // Find best polygons to merge.
      int bestMergeVal = 0; //共边的长度值
      int bestPa = 0, bestPb = 0, bestEa = 0, bestEb = 0;

      for (int j = 0; j < npolys-1; ++j)
      {         
         unsigned short* pj = &polys[j*nvp];
         for (int k = j+1; k < npolys; ++k)
         {
            unsigned short* pk = &polys[k*nvp];
            int ea, eb;
            // 返回可合并的边的长度
            int v = getPolyMergeValue(pj, pk, mesh.verts, ea, eb, nvp);
            // 找到最长的边进行合并
            if (v > bestMergeVal)
            {
               bestMergeVal = v;
               bestPa = j;
               bestPb = k;
               bestEa = ea;
               bestEb = eb;
            }
         }
      }

      if (bestMergeVal > 0)
      {
         // Found best, merge.
         unsigned short* pa = &polys[bestPa*nvp];
         unsigned short* pb = &polys[bestPb*nvp];
          //pa和pb合并成一个，最后放到pa里
         mergePolyVerts(pa, pb, bestEa, bestEb, tmpPoly, nvp);
         //最后的poly放到pb
         unsigned short* lastPoly = &polys[(npolys-1)*nvp];
         if (pb != lastPoly)
            memcpy(pb, lastPoly, sizeof(unsigned short)*nvp);
         npolys--;
      }
      else
      {
         // Could not merge any polygons, stop.
         break;
      }
   }
}
```

### 构建边的邻接关系

虽然有了凸多边形信息，但是每个凸多边形的邻接关系是不知道的，因此这一步的目的就是要遍历整个网格中的所有多边形并生成邻接信息（connection information），方便后续寻路使用。

在Recast中，这种邻接关系是用unsigned short* polys变量来表示的，它的长度是 maxpolys * 2 * nvp，其中nvp规定了一个凸多边形中最大有几个点，乘以2是因为每个poly用长度为2*maxVertsPerPoly的short表示，[0, maxVertsPerPoly)表示多边形的顶点索引，[maxVertsPerPoly, 2*maxVertsPerPoly)表示顶点邻接的多边形索引，为什么用顶点邻接多边形，其实这个顶点代表了该顶点与下一个顶点形成的边，也就是边邻接的多边形索引。邻接的凸多边形肯定共用同一条边，并且边的顶点顺序相反。

```cpp
/// Represents a polygon mesh suitable for use in building a navigation mesh. 
/// @ingroup recast
struct rcPolyMesh
{
   rcPolyMesh();
   ~rcPolyMesh();
   unsigned short* verts; ///< The mesh vertices. [Form: (x, y, z) * #nverts]
   unsigned short* polys; ///< Polygon and neighbor data. [Length: #maxpolys * 2 * #nvp]
   unsigned short* regs;  ///< The region id assigned to each polygon. [Length: #maxpolys]
   unsigned short* flags; ///< The user defined flags for each polygon. [Length: #maxpolys]
   unsigned char* areas;  ///< The area id assigned to each polygon. [Length: #maxpolys]
   int nverts;             ///< The number of vertices.
   int npolys;             ///< The number of polygons.
   int maxpolys;        ///< The number of allocated polygons.
   int nvp;            ///< The maximum number of vertices per polygon.
   float bmin[3];       ///< The minimum bounds in world space. [(x, y, z)]
   float bmax[3];       ///< The maximum bounds in world space. [(x, y, z)]
   float cs;           ///< The size of each cell. (On the xz-plane.)
   float ch;           ///< The height of each cell. (The minimum increment along the y-axis.)
   int borderSize;          ///< The AABB border size used to generate the source data from which the mesh was derived.
   float maxEdgeError;       ///< The max error of the polygon edges in the mesh.
};
```

可视化绘制rcPolyMesh

我们通过绘制这个PolyMesh数据来理解对应的数据结构，下图为原始场景数据：

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='1003' height='646'></svg>)

绘制Poly的范围：

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='533' height='477'></svg>)

```cpp
dd->begin(DU_DRAW_TRIS);

for (int i = 0; i < mesh.npolys; ++i)
{
   const unsigned short* p = &mesh.polys[i*nvp*2];
   const unsigned char area = mesh.areas[i];

   unsigned int color;
   if (area == RC_WALKABLE_AREA)
      color = duRGBA(0,192,255,64);
   else if (area == RC_NULL_AREA)
      color = duRGBA(0,0,0,64);
   else
      color = dd->areaToCol(area);

   unsigned short vi[3];
   for (int j = 2; j < nvp; ++j)
   {
      if (p[j] == RC_MESH_NULL_IDX) break;
      vi[0] = p[0];
      vi[1] = p[j-1];
      vi[2] = p[j];
      for (int k = 0; k < 3; ++k)
      {
         const unsigned short* v = &mesh.verts[vi[k]*3];
         const float x = orig[0] + v[0]*cs;
         const float y = orig[1] + (v[1]+1)*ch;
         const float z = orig[2] + v[2]*cs;
         dd->vertex(x,y,z, color);
      }
   }
}
dd->end();
```

再此基础上，再绘制邻接边：

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='624' height='569'></svg>)

```cpp
// Draw neighbours edges
// 绘制公共边 如果 p[nvp+j] & 0x8000 = 0，说明p[j]是公共边的顶点
const unsigned int coln = duRGBA(0,48,64,32);
dd->begin(DU_DRAW_LINES, 1.5f);
for (int i = 0; i < mesh.npolys; ++i)
{
   const unsigned short* p = &mesh.polys[i*nvp*2];
   for (int j = 0; j < nvp; ++j)
   {
      if (p[j] == RC_MESH_NULL_IDX) break;
      if (p[nvp+j] & 0x8000) continue;  //忽略poly的非公共边
      const int nj = (j+1 >= nvp || p[j+1] == RC_MESH_NULL_IDX) ? 0 : j+1; 
      const int vi[2] = {p[j], p[nj]};

      for (int k = 0; k < 2; ++k)
      {
         const unsigned short* v = &mesh.verts[vi[k]*3];
         const float x = orig[0] + v[0]*cs;
         const float y = orig[1] + (v[1]+1)*ch + 0.1f;
         const float z = orig[2] + v[2]*cs;
         dd->vertex(x, y, z, coln);
      }
   }
}
dd->end();
```

最后绘制边界和顶点，白色线代表poly的公共边：

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='797' height='762'></svg>)

```cpp
// 绘制poly边界
const unsigned int colb = duRGBA(0,48,64,220);
dd->begin(DU_DRAW_LINES, 2.5f);
for (int i = 0; i < mesh.npolys; ++i)
{
   const unsigned short* p = &mesh.polys[i*nvp*2];
   for (int j = 0; j < nvp; ++j)
   {
      if (p[j] == RC_MESH_NULL_IDX) break;
      // if ((p[nvp+j] & 0x8000) == 0) continue;
      const int nj = (j+1 >= nvp || p[j+1] == RC_MESH_NULL_IDX) ? 0 : j+1; 
      const int vi[2] = {p[j], p[nj]}; //边的2个顶点索引分别为p[j], p[nj]，保存在vi数组里

      unsigned int col = colb;
     //用白色绘制poly的公共边的2个顶点
      if ((p[nvp+j] & 0xf) != 0xf) 
         col = duRGBA(255,255,255,128);
      //循环2次，每次绘制边的一个顶点dd->vertex(x, y, z, col)
      for (int k = 0; k < 2; ++k) 
      {
         const unsigned short* v = &mesh.verts[vi[k]*3];
         const float x = orig[0] + v[0]*cs;
         const float y = orig[1] + (v[1]+1)*ch + 0.1f;
         const float z = orig[2] + v[2]*cs;
         dd->vertex(x, y, z, col);
      }
   }
}
dd->end();

//绘制poly所有顶点
dd->begin(DU_DRAW_POINTS, 3.0f);
const unsigned int colv = duRGBA(0,0,0,220);
for (int i = 0; i < mesh.nverts; ++i)
{
   const unsigned short* v = &mesh.verts[i*3];
   const float x = orig[0] + v[0]*cs;
   const float y = orig[1] + (v[1]+1)*ch + 0.1f;
   const float z = orig[2] + v[2]*cs;
   dd->vertex(x,y,z, colv);
}
dd->end();
```

构建邻接边算法

```cpp
struct rcEdge
{
    unsigned short vert[2];     // 边的两个点
    unsigned short polyEdge[2]; // 邻接的两个多边形的边的索引
    unsigned short poly[2];     // 邻接的两个多边形的索引
};
```

1. 遍历多边形，初始化边信息rcEdge，每条边两个顶点的索引v0、索引v1，保证v0<v1
2. 再进行一次遍历，这次筛选出顶点索引v0 > 顶点索引v1形成的边，如果两个顶点与某个rcEdge相同，则补全rcEdge的邻接信息
3. 最后把rcEdge信息保存到mesh.polys中，每个poly用长度为2*maxVertsPerPoly的short表示，[0, maxVertsPerPoly)表示多边形的顶点索引，[maxVertsPerPoly, 2*maxVertsPerPoly)表示**顶点**邻接的多边形索引

构建邻接边信息的代码：

```cpp
/// 构建边信息，每个边与哪个poly相邻
static bool buildMeshAdjacency(unsigned short* polys, const int npolys, const int nverts, const int vertsPerPoly)
```

### 凸多边形生成（Convex Polygon Generation）阶段总结

许多算法只能用于凸多边形。因此，这一步将构成轮廓的简单多边形细分为凸多边形网格。这是通过使用一个适用于简单多边形的三角形划分（triangulation），然后再将三角形合并为最大可能的凸多边形来实现的。

下面是这个阶段生成的凸多边形的效果：

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='1245' height='707'></svg>)

在这一阶段的最后，我们终于回到了向量空间，并得到一个可通过的表面，可通过的表面由凸多边形网格表示。

## 详细网格生成（Detailed Mesh Generation）

构建导航网格的第五个也是最后一个阶段，即生成具有详细高度信息的三角形网格。对应源码函数

```cpp
bool rcBuildPolyMeshDetail(rcContext* ctx, const rcPolyMesh& mesh, const rcCompactHeightfield& chf,
						   const float sampleDist, const float sampleMaxError,
						   rcPolyMeshDetail& dmesh)
```

### 为什么要执行这一步

在3空间中，多边形网格可能无法充分遵循源网格的高度轮廓 ，无论拆分单元的粒度有多小，都不可能完全拟合原实物空间，总是存在误差。而经过多步针对“体素块”的操作后，这些误差可能被放大，导致了Mesh导航面其实只是“原场景”的一个“大概表面”。比如楼梯。此时就需要，在Mesh多边形的基础上，去对比“原场景表面”，然后对Mesh多边形进行再加工——添加高度细节，使其最大限度的贴合原场景表面，减少误差。

该阶段的主要步骤如下，对于每个多边形：

1. 对多边形的外壳边缘（hull edges）进行采样。向任何偏离高度补丁数据超过**detailSampleMaxError**的边添加顶点。
2. 对多边形执行 [Delaunay 三角形剖分](https://link.zhihu.com/?target=https%3A//en.wikipedia.org/wiki/Delaunay_triangulation)。
3. 对多边形的内部表面进行采样。 如果表面与高度补丁数据的偏差超过 detailSampleMaxError的值，则添加顶点。 更新新顶点的三角形剖分。

这个阶段增加了高度细节，这样细节网格（detail mesh）将在所有轴上与源网格的表面相匹配。为了实现这一点，**我们遍历所有多边形，并在多边形与源网格过度偏离时沿着多边形的边和其表面插入顶点**。

**在下面的例子中，楼梯附近的多边形网格在****xz平面上匹配，但在y轴上偏差很大：**

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

在添加了高度细节后，y轴匹配要好得多：

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

从技术上来讲，从寻路的角度来看，这一步不是必需的。凸多边形网格是生成适合使用寻路算法的[图（graph）](https://link.zhihu.com/?target=http%3A//en.wikipedia.org/wiki/Graph_theory)所需要的全部，而在上一阶段创建的多边形网格提供了所有必要的数据。这尤其适用于使用物理或ray-casting将agent放置在源网格表面的情况。事实上，[Recast Navigation](https://link.zhihu.com/?target=https%3A//github.com/memononen/recastnavigation)的Detour库只使用多边形网格来寻路。这个阶段生成的高度细节被认为是可选的，当包含它时，它仅用于完善由各种Detour函数返回的点的位置。

同样重要的是要注意，这个过程仍然只会产生原始网格表面的一个更好的近似。体素化过程已经决定了精确的位置是不可能的。除此之外，由于搜索性能和内存的考虑，过多的高度细节通常比太少的细节更糟糕。

注意，添加高度信息只是为Mesh多边形增加了新的额外的信息，无论如何操作，不改变原Mesh多边形的数据，两者在设计上解耦。因为高度细节只是让我们有更好的数据表示“原场景”的表面，但是仅仅是“更好”而已，体素化永远不可能完全拟合“原场景”的实体表面。但是在技术上，需要斟酌性能和内存方面的影响，过于优化从整体考虑，并不一定是必要的。

### 高度补丁（The Height Patch）

为了添加高度细节，我们需要能够确定一个多边形的表面是否它的开放高度场的span距离太远。高度补丁（height patch）即用于此目的。它包含与多边形相交的每个开放高度场网格位置的预期高度。基本上，它是具有以下特征的开放高度场的部分的简化切口（simplified cutout）。

- 它只包含单个多边形的AABB的高度信息。
- 它只包含每个网格位置（grid location）的地板高度(没有span)。
- 对于每个网格位置，它只有一个高度(不重叠)。

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

```cpp
struct rcHeightPatch
{
   inline rcHeightPatch() : data(0), xmin(0), ymin(0), width(0), height(0) {}
   inline ~rcHeightPatch() { rcFree(data); }
   unsigned short* data;
   int xmin, ymin, width, height;
};
```

**getHeightData**函数从rcCompactHeightfield中取到对应体素的高度，设置体素高度的规则为

- 与poly的region相同的体素：相同region即设置高度。
- 与region连通的体素：找到region边界体素，以边界体素为基础进行广度优先遍历，遍历到的体素设置高度。

为什么要搞出来两个规则，直接根据高度场设置poly包围盒内每个位置高度不就行了吗？

因为只知道包围盒xz的范围，但是cell上可能有多个span，不知道取哪一层的span高度，所以先取判断span的region是否为poly的region，确定在哪一层，然后找到region border的体素，泛洪到同层体素。

```cpp
/// 根据高度场把hp.data赋值上高度
static void getHeightData(rcContext* ctx, const rcCompactHeightfield& chf,
                    const unsigned short* poly, const int npoly,
                    const unsigned short* verts, const int bs,
                    rcHeightPatch& hp, rcIntArray& queue,
                    int region)
{
   queue.clear();
   memset(hp.data, 0xff, sizeof(unsigned short)*hp.width*hp.height);
   bool empty = true;
   if (region != RC_MULTIPLE_REGS)
   {
      // Copy the height from the same region, and mark region borders
      // as seed points to fill the rest.
        // 遍历poly包围盒内所有的点，如果同region则设置高度，并把region边界保存到queue
      for (int hy = 0; hy < hp.height; hy++)
      {
         int y = hp.ymin + hy + bs;
         for (int hx = 0; hx < hp.width; hx++)
         {
            int x = hp.xmin + hx + bs;
            const rcCompactCell& c = chf.cells[x + y*chf.width];
            for (int i = (int)c.index, ni = (int)(c.index + c.count); i < ni; ++i)
            {
               const rcCompactSpan& s = chf.spans[i];
               // 一个cell上有多层，确实是和region相同的一层
               if (s.reg == region)
               {
                  // Store height
                        // 设置高度
                  hp.data[hx + hy*hp.width] = s.y;
                  empty = false;

                  // If any of the neighbours is not in same region,
                  // add the current location as flood fill start
                  bool border = false;
                  for (int dir = 0; dir < 4; ++dir)
                  {
                     if (rcGetCon(s, dir) != RC_NOT_CONNECTED)
                     {
                        const int ax = x + rcGetDirOffsetX(dir);
                        const int ay = y + rcGetDirOffsetY(dir);
                        const int ai = (int)chf.cells[ax + ay*chf.width].index + rcGetCon(s, dir);
                        const rcCompactSpan& as = chf.spans[ai];
                        if (as.reg != region)
                        {
                           border = true;
                           break;
                        }
                     }
                  }
                        // 把region边界保存到queue
                  if (border)
                     push3(queue, x, y, i);
                  break;
               }
            }
         }
      }
   }

   if (empty)
      seedArrayWithPolyCenter(ctx, chf, poly, npoly, verts, bs, hp, queue);

   static const int RETRACT_SIZE = 256;
   int head = 0;

    // 广度优先遍历边界，所有与region连通的设置高度
   // 为什么要用region border上的span进行广度遍历，而不是直接找到所有包围盒内的高度场对应的高度呢
   // 因为有多层，region border上的span是为了确定在哪一层，以这一层的span再进行泛洪
   while (head*3 < queue.size())
   {
      int cx = queue[head*3+0];
      int cy = queue[head*3+1];
      int ci = queue[head*3+2];
      head++;
      if (head >= RETRACT_SIZE)
      {
         head = 0;
         if (queue.size() > RETRACT_SIZE*3)
            memmove(&queue[0], &queue[RETRACT_SIZE*3], sizeof(int)*(queue.size()-RETRACT_SIZE*3));
         queue.resize(queue.size()-RETRACT_SIZE*3);
      }

      const rcCompactSpan& cs = chf.spans[ci];
      for (int dir = 0; dir < 4; ++dir)
      {
         if (rcGetCon(cs, dir) == RC_NOT_CONNECTED) continue;

         const int ax = cx + rcGetDirOffsetX(dir);
         const int ay = cy + rcGetDirOffsetY(dir);
            // 相对索引
         const int hx = ax - hp.xmin - bs;
         const int hy = ay - hp.ymin - bs;

         if ((unsigned int)hx >= (unsigned int)hp.width || (unsigned int)hy >= (unsigned int)hp.height)
            continue;

            // 已经设置过高度了
         if (hp.data[hx + hy*hp.width] != RC_UNSET_HEIGHT)
            continue;

         const int ai = (int)chf.cells[ax + ay*chf.width].index + rcGetCon(cs, dir);
         const rcCompactSpan& as = chf.spans[ai];

         hp.data[hx + hy*hp.width] = as.y;

         push3(queue, ax, ay, ai);
      }
   }
}
```

### 向多边形的边添加细节

这一步更好地将多边形边的高度与高度补丁中的数据匹配起来。它是两次采样的第一次，第二次采样处理多边形的表面。

为2D做好准备，对于下一组可视化，我们从侧面观察网格，y轴向上。

### 细节采样距离detailSampleDist

对于多边形中的每条边：根据**detailSampleDist**的值将边分割成段。比如，如果边长是10个单位，采样距离为2个单位，那么将边分成5个等长段。并不是所有的“样本顶点”都会被使用，这些只是此时的潜在顶点。

```cpp
/// Sets the sampling distance to use when generating the detail mesh.
/// (For height detail only.) [Limits: 0 or >= 0.9] [Units: wu] 
float detailSampleDist;
```

detailSampleDist：将细节网格与原始几何体的表面匹配时要使用的采样距离。  
影响最终细节网格与原始几何体的表面轮廓的一致性。较高的值会导致细节网格更接近原始几何体的表面，但代价是最终三角形数量更多和处理成本更高。

这个参数和边匹配的区别在于，这个参数作用于高度而不是xz平面。它还将整个细节网格表面匹配到原始几何形状的轮廓。边缘匹配只是将网格的边缘与原始几何图形的轮廓进行匹配。

**示例1**：禁用轮廓匹配，适度的边缘匹配。所以边缘跟随轮廓线，但网格的中心区域不跟随：

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

**示例2**：适度的轮廓匹配和边缘匹配。更多的三角形被添加到网格的中心区域：

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

**示例3**：高度的轮廓匹配：

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

### 添加细节（添加顶点）的具体实现

* 添加细节前

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

* 使用高度补丁数据将每个样本顶点的高度（y 值）对齐到高度场

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

检查样本顶点与原始边的距离，如果超过最大偏差detailSampleMaxError，则插入距离原始边最远的样本顶点：

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

detailSampleMaxError的含义

这里的 detailSampleMaxError的代码解释：

```cpp
/// The maximum distance the detail mesh surface should deviate from heightfield
/// data. (For height detail only.) [Limit: >=0] [Units: wu] 
float detailSampleMaxError;
```

细节网格的表面可以偏离原始几何体表面的最大距离。  
精度受 细节采样距离 detailSampleDist 的影响，如果detailSampleDist 设置为0，则该参数没有效果。  
如果将 detailSampleDist 设置为0，则此参数的值没有意义。  
不建议将该值设置为0，因为这会导致最终细节网格中的三角形数量大幅增加，处理成本很高。

* 重复距离检查，直到多边形的新部分完成：

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

```cpp
/// #in 多边形的点
/// #nin 多边形的点个数
/// #verts 拆分后的多边形顶点
if (sampleDist > 0)
{
   for (int i = 0, j = nin-1; i < nin; j=i++)
   {
      const float* vj = &in[j*3];
      const float* vi = &in[i*3];
      bool swapped = false;
      // Make sure the segments are always handled in same order
      // using lexological sort or else there will be seams.
      if (fabsf(vj[0]-vi[0]) < 1e-6f)
      {
         if (vj[2] > vi[2])
         {
            rcSwap(vj,vi);
            swapped = true;
         }
      }
      else
      {
         if (vj[0] > vi[0])
         {
            rcSwap(vj,vi);
            swapped = true;
         }
      }
      //沿边创建样本
      float dx = vi[0] - vj[0];
      float dy = vi[1] - vj[1];
      float dz = vi[2] - vj[2];
      float d = sqrtf(dx*dx + dz*dz);

      int nn = 1 + (int)floorf(d/sampleDist);
      if (nn >= MAX_VERTS_PER_EDGE) nn = MAX_VERTS_PER_EDGE-1;
      if (nverts+nn >= MAX_VERTS)
         nn = MAX_VERTS-1-nverts; //nn为采样个数

      // 边按采样点分割成不同点
      for (int k = 0; k <= nn; ++k)
      {
         float u = (float)k/(float)nn;
         float* pos = &edge[k*3];
         pos[0] = vj[0] + dx*u;
         pos[1] = vj[1] + dy*u;
         pos[2] = vj[2] + dz*u;
         // 获得一定半径范围内与pos[1]最接近的高度
         pos[1] = getHeight(pos[0],pos[1],pos[2], cs, ics, chf.ch, heightSearchRadius, hp)*chf.ch;
      }
      // Simplify samples.
      int idx[MAX_VERTS_PER_EDGE] = {0,nn};
      int nidx = 2;
      for (int k = 0; k < nidx-1; )
      {
         const int a = idx[k];
         const int b = idx[k+1];
         const float* va = &edge[a*3];
         const float* vb = &edge[b*3];
         // 找到沿线段的最大偏差maxd
         float maxd = 0;
         int maxi = -1;
         // 遍历a和b之间的采样点，计算与ab连线距离最远的点maxi
         for (int m = a+1; m < b; ++m)
         {
            float dev = distancePtSeg(&edge[m*3],va,vb);
            if (dev > maxd)
            {
               maxd = dev;
               maxi = m;
            }
         }
         // 如果最大偏差大于可接受的误差，添加新点到idx，否则继续下一段。
         if (maxi != -1 && maxd > rcSqr(sampleMaxError))
         {  
            for (int m = nidx; m > k; --m)
               idx[m] = idx[m-1];
            idx[k+1] = maxi;
            nidx++;
         }
         else
         {
            ++k;
         }
      }

      // hull中已经保存了poly所有简化点
      hull[nhull++] = j;
      // Add new vertices.
      if (swapped)
      {
         for (int k = nidx-2; k > 0; --k)
         {
            rcVcopy(&verts[nverts*3], &edge[idx[k]*3]);
            hull[nhull++] = nverts;
            nverts++;
         }
      }
      else
      {
         for (int k = 1; k < nidx-1; ++k)
         {
            rcVcopy(&verts[nverts*3], &edge[idx[k]*3]);
            hull[nhull++] = nverts;
            nverts++;
         }
      }
   }
}
```

### 三角化（triangulateHull）

此时hull中已经保存了poly所有简化点，需要对hull围成的形状进行三角形化，这一步是在为边添加细节后用来对多边形进行三角形剖分的，在此之后，原始多边形不再存在，所有操作都在三角形网格上进行。从这开始，当任何新顶点添加到网格时，都将发生重新三角形剖分。

它的基本思想和耳裁法类似，但是规则不一样：

1. 耳裁法的规则是使用最短的分割边形成的三角形作为裁剪的三角形，而这里的规则是选择三角形中周长最短的作为裁剪的三角形
2. 然后去掉耳尖顶点，留下两个耳根顶点，判断这两个耳根顶点作为耳尖的两个三角形的周长大小，周长较短的作为要裁减的三角形，加入结果列表中，再后再继续这一步
3. 直到裁剪后只剩下一个三角形，将三角形加入结果列表中

算法示意图如下：

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='746' height='501'></svg>)

上图中，所有相邻三个点组成的三角形中，BCD三角形周长最短，所以BCD为加入结果列表

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='777' height='501'></svg>)

上图三角形BCD被剪裁掉后，判断以BD为边的两个三角形ABD和BDE的周长，BDE周长更短，则把BDE放入结果列表中

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='755' height='502'></svg>)

上图最后只剩下三角形ABE，把ABE加入到结果列表中

三角化代码：

```cpp
//hull中已经保存了poly所有简化点
static void triangulateHull(const int /*nverts*/, const float* verts, const int nhull, const int* hull, const int nin, rcIntArray& tris)
{

	int start = 0, left = 1, right = nhull-1;
	
	// Start from an ear with shortest perimeter.
	// This tends to favor well formed triangles as starting point.
    //hull中每相邻的三个点可以组成一个三角形,找到周长最小的三角形,作为要裁切的三角形
	float dmin = FLT_MAX;
	for (int i = 0; i < nhull; i++)
	{
		if (hull[i] >= nin) continue; // Ears are triangles with original vertices as middle vertex while others are actually line segments on edges
		int pi = prev(i, nhull);
		int ni = next(i, nhull);
		const float* pv = &verts[hull[pi]*3];
		const float* cv = &verts[hull[i]*3];
		const float* nv = &verts[hull[ni]*3];

		//d为由顶点pi,cv,nv组成的三角形的周长
		const float d = vdist2(pv,cv) + vdist2(cv,nv) + vdist2(nv,pv);
		if (d < dmin)
		{
			start = i;
			left = ni;
			right = pi;
			dmin = d;
		}
	}
	
	// Add first triangle
	tris.push(hull[start]);
	tris.push(hull[left]);
	tris.push(hull[right]);
	tris.push(0);
	
	// Triangulate the polygon by moving left or right,
	// depending on which triangle has shorter perimeter.
	// This heuristic was chose emprically, since it seems
	// handle tesselated straight edges well.
    // 沿着left移动，比较left、left的left、right、right的right四个点，以left-right为边的两个三角形中面积小的三角形切下来，继续遍历
	while (next(left, nhull) != right)
	{
		// Check to see if se should advance left or right.
		int nleft = next(left, nhull);
		int nright = prev(right, nhull);
		
		const float* cvleft = &verts[hull[left]*3];
		const float* nvleft = &verts[hull[nleft]*3];
		const float* cvright = &verts[hull[right]*3];
		const float* nvright = &verts[hull[nright]*3];
		const float dleft = vdist2(cvleft, nvleft) + vdist2(nvleft, cvright);
		const float dright = vdist2(cvright, nvright) + vdist2(cvleft, nvright);
		
		if (dleft < dright)
		{
			tris.push(hull[left]);
			tris.push(hull[nleft]);
			tris.push(hull[right]);
			tris.push(0);
			left = nleft;
		}
		else
		{
			tris.push(hull[left]);
			tris.push(hull[nright]);
			tris.push(hull[right]);
			tris.push(0);
			right = nright;
		}
	}
}
```

### 向多边形内部表面添加细节

此时，我们有一个小的三角形网格，而不是一个单一的多边形(如果原来的多边形是一个三角形，并且在边缘细节步骤中没有添加新的顶点，我们可能仍然有一个单一的三角形)。所有的顶点仍然在网格的边上。在这一步中，我们检查网格的内部表面，看看它是否偏离高度补丁中的数据太多。

下一组可视化仍然是2D的。但是我们切换到xz平面的自上而下的视图。

在三角形网格的内表面添加高度细节的概念类似于在边缘添加细节。先求出最小能包围“多边形”的AABB采样面，对采样面进行“二维矩阵”等大裁剪，生成“采样点”。

在网格的AABB的xz平面上构建一个**样本顶点网格（grid**），间距基于detailSampleDist的值。样本顶点的y轴值被对齐到高度补丁中的数据：

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

网格（mesh）外的样本顶点被丢弃：

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

```cpp
float bmin[3], bmax[3]; //网格的AABB
rcVcopy(bmin, in);
rcVcopy(bmax, in);
for (int i = 1; i < nin; ++i)
{
   rcVmin(bmin, &in[i*3]);
   rcVmax(bmax, &in[i*3]);
}
int x0 = (int)floorf(bmin[0]/sampleDist);
int x1 = (int)ceilf(bmax[0]/sampleDist);
int z0 = (int)floorf(bmin[2]/sampleDist);
int z1 = (int)ceilf(bmax[2]/sampleDist);
samples.clear();
for (int z = z0; z < z1; ++z)
{
   for (int x = x0; x < x1; ++x)
   {
      float pt[3];
      pt[0] = x*sampleDist;
      pt[1] = (bmax[1]+bmin[1])*0.5f;
      pt[2] = z*sampleDist;
      // Make sure the samples are not too close to the edges.
      //distToPoly返回p与poly边的最近距离
      if (distToPoly(nin,in,pt) > -sampleDist/2) continue;
      samples.push(x);
      samples.push(getHeight(pt[0], pt[1], pt[2], cs, ics, chf.ch, heightSearchRadius, hp));
      samples.push(z);
      samples.push(0); // Not added
   }
}
```

在幸存的样本顶点中，找出离三角形网格表面最远的那个。如果它比**detailSampleMaxError**的值更远，那么将它添加到网格中并重新进行三角形剖分。

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

  

重复这个过程，直到没有样本顶点超过**detailSampleMaxError** 的值：

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='0' height='0'></svg>)

```cpp
const int nsamples = samples.size()/4;
for (int iter = 0; iter < nsamples; ++iter)
{
   if (nverts >= MAX_VERTS)
      break;

   //查找错误最多的样本
   float bestpt[3] = {0,0,0};
   float bestd = 0;
   int besti = -1;
   for (int i = 0; i < nsamples; ++i)
   {
      const int* s = &samples[i*4];
      if (s[3]) continue; // skip added.
      float pt[3];
      // The sample location is jittered to get rid of some bad triangulations
      // which are cause by symmetrical data from the grid structure.
      pt[0] = s[0]*sampleDist + getJitterX(i)*cs*0.1f;
      pt[1] = s[1]*chf.ch;
      pt[2] = s[2]*sampleDist + getJitterY(i)*cs*0.1f;
      float d = distToTriMesh(pt, verts, nverts, &tris[0], tris.size()/4);
      if (d < 0) continue; // did not hit the mesh.
      if (d > bestd)
      {
         bestd = d;
         besti = i;
         rcVcopy(bestpt,pt);
      }
   }
   // 如果最大误差在可接受的阈值sampleMaxError 内，则停止细分
   if (bestd <= sampleMaxError || besti == -1)
      break;
   // Mark sample as added.
   samples[besti*4+3] = 1;
   // 添加样本点
   rcVcopy(&verts[nverts*3],bestpt);
   nverts++;

    //创建新的三角剖分
   edges.clear();
   tris.clear();
   delaunayHull(ctx, nverts, verts, nhull, hull, tris, edges);
}
```

其中**delaunayHull**三角剖分的算法解释可以看这个：[技术分享：Delaunay三角剖分算法介绍](https://zhuanlan.zhihu.com/p/459884570)

### 详细网格生成阶段总结

在最后阶段，使用 [Delaunay 三角剖分](https://link.zhihu.com/?target=http%3A//en.wikipedia.org/wiki/Delaunay_triangulation)（[Delaunay triangulation](https://link.zhihu.com/?target=http%3A//en.wikipedia.org/wiki/Delaunay_triangulation)）对凸多边形网格进行三角剖分，以便添加高度细节。顶点在内部添加到多边形的边缘，以确保充分遵循原始几何体的表面。

该阶段生成的详细网格可用作简单导航系统中的主要导航网格。但有一些方法可以将不同阶段的数据结合起来，用于导航决策。前面提到的[Detour](https://link.zhihu.com/?target=https%3A//github.com/memononen/recastnavigation)就是一个例子。Mikko Mononen的[一篇博客](https://link.zhihu.com/?target=http%3A//digestingduck.blogspot.com/2010/05/constrained-movement-along-navmesh-pt-2.html)提供了如何在寻路中使用这些数据的提示。在这篇博客中，他使用凸多边形生成阶段的输出来计算受约束的运动。

下面的例子展示了一个详细的网格：

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='1243' height='722'></svg>)

在这一阶段的最后，可通过表面由与源几何体的高度轮廓相匹配的三角形网格表示。由单个多边形创建的详细三角形网格被合并到一个网格中，并加载到一个TriangleMesh实例中。整个过程完成了，我们得到一个导航网格。

### Recast中Tile的概念

Recast将场景进行Tile划分后，会形成二维投影矩阵（实际是三维矩阵，高度维度由其他逻辑实现），矩阵的每一个元素都是一个Tile，注意tile本身并不是一个面，而是一个包含内部所有多边形的AABB包围盒（这是因为Tile内部的多边形并不共面）。Tile中维护了其在矩阵中的X和Y的位置，以及XY定位列中的序列。

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='1353' height='587'></svg>)

![](data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='1196' height='645'></svg>)

## 附录

### Navmesh相关参数解释（rcConfig）

```cpp
/// Specifies a configuration to use when performing Recast builds.
/// @ingroup recast
struct rcConfig
{
   /// The width of the field along the x-axis. [Limit: >= 0] [Units: vx]
   int width; //体素块底部边长

   /// The height of the field along the z-axis. [Limit: >= 0] [Units: vx]
   int height; //体素块高度

   /// The width/height size of tile's on the xz-plane. [Limit: >= 0] [Units: vx]
   int tileSize; //Tile在xz平面的宽/高大小

   /// The size of the non-navigable border around the heightfield. [Limit: >=0] [Units: vx]
   int borderSize; //不可导航的边界大小

   /// The xz-plane cell size to use for fields. [Limit: > 0] [Units: wu] 
   float cs;  //x和z方向上的体素精度

   /// The y-axis cell size to use for fields. [Limit: > 0] [Units: wu]
   float ch;  //y方向的体素精度

   /// The minimum bounds of the field's AABB. [(x, y, z)] [Units: wu]
   float bmin[3]; //整个包围盒的最小点

   /// The maximum bounds of the field's AABB. [(x, y, z)] [Units: wu]
   float bmax[3]; //整个包围盒的最大点

   /// The maximum slope that is considered walkable. [Limits: 0 <= value < 90] [Units: Degrees] 
   float walkableSlopeAngle; //agent的可行走最大坡度

   /// Minimum floor to 'ceiling' height that will still allow the floor area to 
   /// be considered walkable. [Limit: >= 3] [Units: vx] 
   int walkableHeight; //agent的可行走的最小高度空间

   /// Maximum ledge height that is considered to still be traversable. [Limit: >=0] [Units: vx] 
   int walkableClimb; //agent的可攀爬高度

   /// The distance to erode/shrink the walkable area of the heightfield away from 
   /// obstructions.  [Limit: >=0] [Units: vx] 
   int walkableRadius; //agent的行走半径

   /// The maximum allowed length for contour edges along the border of the mesh. [Limit: >=0] [Units: vx] 
   int maxEdgeLen; //沿网格边界的轮廓边的最大允许长度

   /// The maximum distance a simplfied contour's border edges should deviate 
   /// the original raw contour. [Limit: >=0] [Units: vx]
   float maxSimplificationError; //简化轮廓的边界边缘应偏离的最大距离

   /// The minimum number of cells allowed to form isolated island areas. [Limit: >=0] [Units: vx] 
   int minRegionArea; //允许形成孤岛区域的最小单元数

   /// Any regions with a span count smaller than this value will, if possible, 
   /// be merged with larger regions. [Limit: >=0] [Units: vx] 
   int mergeRegionArea; //如果可能，跨度计数小于此值的任何区域都将与更大的区域合并

   /// The maximum number of vertices allowed for polygons generated during the 
   /// contour to polygon conversion process. [Limit: >= 3] 
   int maxVertsPerPoly; //在轮廓到多边形转换过程中生成的多边形允许的最大顶点数

   /// Sets the sampling distance to use when generating the detail mesh.
   /// (For height detail only.) [Limits: 0 or >= 0.9] [Units: wu] 
   float detailSampleDist; //设置生成细节网格时要使用的采样距离

   /// The maximum distance the detail mesh surface should deviate from heightfield
   /// data. (For height detail only.) [Limit: >=0] [Units: wu] 
   float detailSampleMaxError; //细节网格表面偏离高度场的最大距离
};
```

## RecastNavigation生成的Navmesh的局限性

（1）RecastNavigation所有操作都基于地表面，因此，对空中的对象的交互，用它是无法完成的。现在国产武侠类 MMORPG 里大行其道的轻功、甚至御剑飞行，是无法只单纯依赖 RecastNavigation 的数据去实现的。特别是对于某些具有层次错落结构的地形，就非常容易出现掉到两片导航网格的夹缝里的情况。这类机制的实现需要其他场景数据的支持，通常这时会结合其他引擎，如physx

（2）像《塞尔达传说：旷野之息》的爬山、《忍者龙剑传》的踩墙这种机制，则会在生成导航网格的阶段就会遇到麻烦。因为设计前提2的存在，RecastNavigation 是无法对与地面夹角小于或等于90°的墙面生成导航网格的。因此需要从另外的机制、设计上去规避或处理。不过，Unity 2017 已经可以支持了在各种角度的墙面生成导航网格了：Ceiling and Wall Navigation in Unity3D

## 导出obj插件

[luyuancpp/ue5-export-nav-data (github.com)](https://link.zhihu.com/?target=https%3A//github.com/luyuancpp/ue5-export-nav-data)

[hxhb/ue4-export-nav-data: Export ue4 navigation data to outside. (github.com)](https://link.zhihu.com/?target=https%3A//github.com/hxhb/ue4-export-nav-data)

[monitor1394/ExportSceneToObj: Export scene (including objects and terrain ) or fbx to .obj file for Unity. | 导出Unity的场景或FBX到obj文件 (github.com)](https://link.zhihu.com/?target=https%3A//github.com/monitor1394/ExportSceneToObj)

## 后续文章计划

结合源码讲解Detour寻路的内容

结合源码讲解UE的寻路模块原理

结合实例讲解寻路模块的优化方法

## 参考文献：

[https://zhuanlan.zhihu.com/p/35100455](https://zhuanlan.zhihu.com/p/35100455)

[https://codeantenna.com/a/JYPxBK9HKc](https://link.zhihu.com/?target=https%3A//codeantenna.com/a/JYPxBK9HKc)

[http://digestingduck.blogspot.com/](https://link.zhihu.com/?target=http%3A//digestingduck.blogspot.com/)

编辑于 2022-12-16 20:23・IP 属地上海

[

游戏AI

](https://www.zhihu.com/topic/20190693)

​赞同 80​​7 条评论

​分享

​喜欢​收藏​申请转载

​

赞同 80

​

分享

![](https://picx.zhimg.com/737b8792d99051bd09bcf734e784abde_l.jpg?source=32738c0c)

发布一条带图评论吧

  

7 条评论

默认

最新

[![daodao9997](https://pic1.zhimg.com/v2-abed1a8c04700ba7d72b45195223e0ff_l.jpg?source=06d4cd63)](https://www.zhihu.com/people/e1552bcf5cd92b6ae1b1e2bb91f1bdf7)

[daodao9997](https://www.zhihu.com/people/e1552bcf5cd92b6ae1b1e2bb91f1bdf7)

佩服![[大笑]](https://pic1.zhimg.com/v2-3ac403672728e5e91f5b2d3c095e415a.png)

11-03 · IP 属地美国

​回复​喜欢

[![LawAias233](https://picx.zhimg.com/v2-b9338221be8f95127d1479b798bfeffb_l.jpg?source=06d4cd63)](https://www.zhihu.com/people/1f55bdba0f83773e2985aacc3c05a1fe)

[LawAias233](https://www.zhihu.com/people/1f55bdba0f83773e2985aacc3c05a1fe)

​

太他妈屌了卧槽

11-02 · IP 属地广东

​回复​喜欢

[![种豆南山下](https://pic1.zhimg.com/v2-59a47da938134ff5658bfbfaae622303_l.jpg?source=06d4cd63)](https://www.zhihu.com/people/904a100fd71bc3a07000041a19c8e71b)

[种豆南山下](https://www.zhihu.com/people/904a100fd71bc3a07000041a19c8e71b)

作者您好，这篇文章反复看了好几遍，受益很多，看到文章里面有译者注的字样，请问这篇文章是翻译的吗， 如果是的话可以给下英文版地址吗。如果是自己原创的话，催更Detour![[发呆]](https://pic2.zhimg.com/v2-7f09d05d34f03eab99e820014c393070.png)

10-10 · IP 属地广东

​回复​喜欢

[![WKPL](https://picx.zhimg.com/v2-f2c62548d55504158223b582335c26fa_l.jpg?source=06d4cd63)](https://www.zhihu.com/people/9f9c5e41825506087bc977f00a2e9f30)

[WKPL](https://www.zhihu.com/people/9f9c5e41825506087bc977f00a2e9f30)

写的太好了，催更Detour

09-13 · IP 属地浙江

​回复​喜欢

[![夏风](https://pic1.zhimg.com/v2-e797b498de31825b04993c0560e0da42_l.jpg?source=06d4cd63)](https://www.zhihu.com/people/459ef915d5a3cb243568700fe369b5ac)

[夏风](https://www.zhihu.com/people/459ef915d5a3cb243568700fe369b5ac)

深度好文![[赞]](https://pic1.zhimg.com/v2-c71427010ca7866f9b08c37ec20672e0.png)![[赞]](https://pic1.zhimg.com/v2-c71427010ca7866f9b08c37ec20672e0.png)

08-11 · IP 属地广东

​回复​喜欢

[![wz小明明明](https://picx.zhimg.com/8587af278a8858e540eb521df89749c2_l.jpg?source=06d4cd63)](https://www.zhihu.com/people/18674f36c1a4a9502340f9cb545a1408)

[wz小明明明](https://www.zhihu.com/people/18674f36c1a4a9502340f9cb545a1408)

写的太好了，感谢分享![[爱]](https://pic1.zhimg.com/v2-0942128ebfe78f000e84339fbb745611.png)

06-07 · IP 属地上海

​回复​喜欢

[![freeeeeG](https://picx.zhimg.com/v2-e2abcc6b8ea1f639e33eee1f95c2d166_l.jpg?source=06d4cd63)](https://www.zhihu.com/people/3090eff7bb744536181b5924d4613265)

[freeeeeG](https://www.zhihu.com/people/3090eff7bb744536181b5924d4613265)

写的好详细，催更Detour

01-31 · IP 属地广东

​回复​喜欢

### 推荐阅读

[

# 用户态操作系统之一 Seastar简介

引言有一句老生常谈的话：CPU不是瓶颈，网络才是。在此前提下，服务端开发使用什么语言关系并不大，因为CPU还没用用满的情况下，网络就已经满了。 然而，世殊事异，这句话的真实性已经大大…

吴乎



](https://zhuanlan.zhihu.com/p/38771059)[

![Dynamo应用秘籍：2：利用自定义节点端口数据类型实现程序的自动嵌套运行](https://pic1.zhimg.com/v2-b50999b2bfcedafa7607315bb85778de_250x0.jpg?source=172ae18b)

# Dynamo应用秘籍：2：利用自定义节点端口数据类型实现程序的自动嵌套运行

建筑师的魔术手



](https://zhuanlan.zhihu.com/p/24850784)[

![Merlin HugeCTR 分级参数服务器简介](https://pica.zhimg.com/v2-6da85eace352e2985d0936f767eafd9d_250x0.jpg?source=172ae18b)

# Merlin HugeCTR 分级参数服务器简介

NVIDIA英伟达中国



](https://zhuanlan.zhihu.com/p/453502358)[

![Kubernetes集群的全天候一站式访问工具介绍](https://picx.zhimg.com/v2-8a26aca2c33190c2c0bcf3ad220fdd49_250x0.jpg?source=172ae18b)

# Kubernetes集群的全天候一站式访问工具介绍

Jimmy...发表于Cloud...



](https://zhuanlan.zhihu.com/p/31756070)