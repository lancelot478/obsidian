# 射击游戏透视轮廓 Shader（X-Ray Outline）

> 问题：帮我写一个射击游戏中常见的，开了透视后能看见静态物体（建筑物）轮廓的shader并把关键的地方逐句解释  
> 日期：2026-03-31

---

## 效果说明

- 物体**未被遮挡**时：显示绿色轮廓
- 物体**被建筑/墙体遮挡**时：仍能看见橙色透视轮廓（X-Ray）
- 物体本体：正常纹理渲染

核心原理是**反转外壳法（Inverted Hull）** + **深度测试反转（ZTest Greater）**

---

## Shader 完整代码

```hlsl
Shader "Game/XRayOutline"
{
    Properties
    {
        _MainTex      ("主贴图",              2D)            = "white" {}
        _Color        ("物体颜色",            Color)         = (1, 1, 1, 1)
        _OutlineColor ("轮廓颜色（正常可见）", Color)         = (0, 1, 0, 1)
        _OccludedColor("轮廓颜色（透视可见）", Color)         = (1, 0.3, 0, 0.8)
        _OutlineWidth ("轮廓宽度",            Range(0.001, 0.05)) = 0.008
    }

    SubShader
    {
        // 渲染队列设在默认不透明物体（Geometry=2000）之后
        // 确保场景中的建筑已写入深度缓冲，透视的 ZTest Greater 才能正确判断遮挡关系
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry+100" }

        // ═══════════════════════════════════════════════════════════
        // Pass 1：透视轮廓 —— 只在物体被遮挡时才显示
        // ═══════════════════════════════════════════════════════════
        Pass
        {
            Name "OCCLUDED_OUTLINE"

            // ★ 核心：深度测试取反
            // ZTest Greater = "当前像素深度 > 深度缓冲中的值"
            // 深度缓冲里存的是挡在前面的建筑深度
            // 当前像素深度更大 → 物体在建筑后面 → 正常会被丢弃
            // 这里改成 Greater 反而让它通过 → 实现"透视"
            ZTest Greater

            // 不写深度：此 Pass 只负责视觉效果，不能破坏已经建立好的深度缓冲
            // 否则会遮挡后续物体的渲染
            ZWrite Off

            // 正面剔除（反转外壳法关键）：
            // 正常渲染剔除背面(Cull Back)，这里剔除正面
            // 剩下向外膨胀的背面，从轮廓缝隙里露出来 → 形成描边环
            Cull Front

            // Alpha 混合：让透视轮廓半透明，不要太突兀
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            half4 _OccludedColor;
            float _OutlineWidth;

            struct appdata
            {
                float4 vertex : POSITION; // 模型空间顶点
                float3 normal : NORMAL;   // 模型空间法线
            };

            struct v2f
            {
                float4 pos : SV_POSITION; // 裁剪空间顶点（输出给光栅化）
            };

            v2f vert(appdata v)
            {
                v2f o;

                // ── 在观察空间（View Space）做法线膨胀 ──
                // 为什么不在模型空间膨胀？
                // 模型空间膨胀：近处轮廓细、远处轮廓粗（透视变形）
                // 观察空间膨胀：膨胀在投影前完成，轮廓粗细与距离无关，更均匀

                // 第一步：将顶点从模型空间变换到观察空间
                // UNITY_MATRIX_MV = Model × View 矩阵
                float4 viewPos = mul(UNITY_MATRIX_MV, v.vertex);

                // 第二步：将法线变换到观察空间
                // 必须用逆转置矩阵(IT_MV)变换法线，而不是直接用 MV
                // 原因：模型有非均匀缩放时，直接用 MV 变换法线会导致法线方向错误
                float3 viewNormal = mul((float3x3)UNITY_MATRIX_IT_MV, v.normal);

                // 第三步：将 z 分量归零
                // 只在屏幕平面（xy）方向膨胀，忽略深度方向
                // 否则法线向深度方向偏移会导致轮廓部分被自身遮挡（轮廓穿插）
                viewNormal.z = 0.0;
                viewNormal = normalize(viewNormal);

                // 第四步：在观察空间 xy 方向按法线移动顶点（膨胀外壳）
                viewPos.xy += viewNormal.xy * _OutlineWidth;

                // 第五步：观察空间 → 裁剪空间（乘投影矩阵）
                o.pos = mul(UNITY_MATRIX_P, viewPos);

                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                // 直接输出透视轮廓颜色（含 Alpha 做半透明）
                return _OccludedColor;
            }
            ENDCG
        }

        // ═══════════════════════════════════════════════════════════
        // Pass 2：正常轮廓 —— 只在物体可见（未遮挡）时显示
        // ═══════════════════════════════════════════════════════════
        Pass
        {
            Name "VISIBLE_OUTLINE"

            // ZTest LEqual = "深度 ≤ 深度缓冲" → 物体正常可见时才通过
            // 与 Pass 1 的 Greater 互补，两个 Pass 合起来覆盖所有情况
            ZTest LEqual
            ZWrite Off
            Cull Front // 同样使用反转外壳法

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            half4 _OutlineColor;
            float _OutlineWidth;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                // 与 Pass 1 完全相同的观察空间膨胀逻辑
                float4 viewPos    = mul(UNITY_MATRIX_MV, v.vertex);
                float3 viewNormal = mul((float3x3)UNITY_MATRIX_IT_MV, v.normal);
                viewNormal.z = 0.0;
                viewNormal   = normalize(viewNormal);
                viewPos.xy  += viewNormal.xy * _OutlineWidth;
                o.pos        = mul(UNITY_MATRIX_P, viewPos);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                // 输出正常轮廓颜色（不透明）
                return _OutlineColor;
            }
            ENDCG
        }

        // ═══════════════════════════════════════════════════════════
        // Pass 3：物体本体渲染
        // ═══════════════════════════════════════════════════════════
        Pass
        {
            Name "BASE"

            ZTest LEqual  // 正常深度测试
            ZWrite On     // 写入深度，本体遮挡关系正常
            Cull Back     // 正常背面剔除

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4    _MainTex_ST; // 存储 tiling 和 offset，TRANSFORM_TEX 宏需要
            half4     _Color;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                // UnityObjectToClipPos = 模型空间 → 裁剪空间（MVP 矩阵一步完成）
                o.pos = UnityObjectToClipPos(v.vertex);
                // TRANSFORM_TEX 宏：应用 tiling/offset，等价于 v.uv * _ST.xy + _ST.zw
                o.uv  = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                half4 tex = tex2D(_MainTex, i.uv);
                return tex * _Color; // 贴图颜色 × 自定义颜色叠加
            }
            ENDCG
        }
    }
}
```

---

## 为什么 Cull Front + 膨胀顶点 = 只显示轮廓？

这是整个描边技术最精妙的地方，需要分三层来理解。

### 第一层：什么是 Cull（面剔除）？

GPU 渲染一个三角面时，会根据顶点的**绕序（Winding Order）**判断你看到的是正面还是背面：

```
顶点逆时针排列 → 正面（朝向摄像机）
顶点顺时针排列 → 背面（背对摄像机）
```

- `Cull Back`（默认）：丢弃背面三角形，只渲染正面 → 正常效果
- `Cull Front`：丢弃**正面**三角形，只渲染背面
- `Cull Off`：正反面都渲染

### 第二层：膨胀后，背面在哪里？

顶点着色器把每个顶点**沿法线方向向外推**了一圈，得到一个比原始模型略大的"外壳"：

```
原始模型（截面）         膨胀后的外壳（截面）

    ┌───┐                  ╔═══════╗
    │   │       →          ║ ┌───┐ ║
    │   │                  ║ │   │ ║
    └───┘                  ║ └───┘ ║
                           ╚═══════╝
                         外壳比原模型大一圈
```

外壳的**正面**朝外（面向摄像机），**背面**朝内（面向原始模型内部）。

### 第三层：Cull Front 剔除正面后，剩下什么？

```
摄像机视角下看外壳：

         摄像机
            ↓
   ┌──────────────────┐
   │  正面（被剔除）   │  ← Cull Front 丢掉这部分
   │  ┌────────┐      │
   │  │ 原始模 │      │
   │  │ 型遮挡 │      │
   │  └────────┘      │
   │  背面边缘露出来  │  ← 这圈背面就是轮廓！
   └──────────────────┘
```

- 外壳**中间大块的正面**：被 Cull Front 丢弃，看不见 ✅
- 外壳**四周边缘露出的背面**：原始模型的边界处，背面从侧面探出来，形成一圈描边 ✅

**关键点**：外壳中间部分的背面，被原始模型（Pass 3）遮挡住了（ZTest LEqual），所以看不见。只有超出原始模型轮廓的那一圈背面才能被看到——这就是描边。

### 完整流程总结

```
Step 1: 顶点沿法线膨胀
        原始模型 → 略大的外壳

Step 2: Cull Front 剔除外壳的正面
        外壳中间 → 看不见
        外壳边缘背面 → 探出来

Step 3: Pass 3 渲染原始模型（ZWrite On）
        原始模型写入深度缓冲
        外壳中间的背面被原始模型深度遮挡（ZTest LEqual 失败）→ 消失

Step 4: 剩下的只有轮廓那一圈背面
        → 显示出均匀的描边 ✅
```

### 如果不膨胀，只用 Cull Front 会怎样？

```
不膨胀直接 Cull Front：外壳 = 原始模型大小

背面被自身正面（Pass 3）完全遮挡（深度相同或更大）
→ 全部消失，什么都看不见 ❌
```

**所以膨胀和 Cull Front 必须配合使用**，膨胀创造出"让背面露出来"的空间，Cull Front 负责隐藏多余的部分，两者缺一不可。

---

## 核心原理图解

```
场景深度缓冲（已有建筑写入）:
┌──────────────────────────┐
│  墙体深度 = 5.0          │
└──────────────────────────┘

目标物体（被墙遮挡）像素深度 = 8.0

Pass 1 深度测试:
  ZTest Greater → 8.0 > 5.0 → ✅ 通过 → 画出透视轮廓
  
Pass 2 深度测试:
  ZTest LEqual  → 8.0 > 5.0 → ❌ 不通过 → 不画（被遮挡时不显示正常轮廓）
```

```
反转外壳法（Inverted Hull）原理:

正常渲染（Cull Back）:      膨胀后剔除正面（Cull Front）:
    ▲                              ▲▲▲
   ╱│╲   ← 只看到正面             ╱   ╲  ← 看到膨胀的背面边缘
  ╱ │ ╲                          ╱  ↑  ╲    形成轮廓环
 ╱  │  ╲                        ╱ 轮廓 ╲
```

---

## 渲染顺序与 Pass 执行关系

| Pass | ZTest | Cull | 作用 |
|------|-------|------|------|
| Pass 1 (OCCLUDED_OUTLINE) | Greater | Front | 透视轮廓，被遮挡时显示 |
| Pass 2 (VISIBLE_OUTLINE)  | LEqual  | Front | 正常轮廓，可见时显示   |
| Pass 3 (BASE)             | LEqual  | Back  | 物体本体正常渲染       |

> Pass 1 和 Pass 2 的 ZTest 条件互补，两者合起来等价于 `ZTest Always`，即轮廓在任何情况下都显示，只是颜色不同。

---

## Unity 使用方式

1. 将 `.shader` 文件放入项目 `Assets/Shaders/` 目录
2. 新建 Material，选择 `Game/XRayOutline` 着色器
3. 将 Material **赋给目标物体**（需要被透视的人物/道具等）
4. 建筑/场景物体使用**普通不透明 Shader**，不需要修改

**注意**：建筑/场景物体必须**先于目标物体渲染**（Queue 更小），否则深度缓冲里没有建筑数据，ZTest Greater 永远失败。

---

## 常见变体与扩展

### 动态开关透视效果（C# 控制）
```csharp
// 通过 Material 属性动态切换
material.SetColor("_OccludedColor", new Color(1, 0.3f, 0, enableXRay ? 0.8f : 0));
```

### 添加描边动画（脉冲效果）
```hlsl
// 在 frag 中加入时间驱动的 Alpha 波动
half alpha = _OccludedColor.a * (0.5 + 0.5 * sin(_Time.y * 3.0));
return half4(_OccludedColor.rgb, alpha);
```

### 移动端性能优化
- Pass 2（正常轮廓）可选择性去掉，只保留透视轮廓，减少 DrawCall
- 将 `_OutlineWidth` 改为 `half` 精度
