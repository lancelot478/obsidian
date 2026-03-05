#Unity
方法	代码示例 (简化)	效果描述	常用场景
线性插值 (Lerp)	lerp(A, B, t)	        A 和 B 之间的平滑混合、染色。	       选中、受伤提示、颜色过渡。
加法混合 (Add)	 A + B * intensity	 增亮、发光。	                                   能量护盾、火焰、魔法高亮。
乘法混合 (Multiply)	A * B	        变暗、染色、滤镜。	                           中毒效果、阴影、场景滤镜。
Smoothstep 遮罩. 	lerp(A, B, smoothstep(...))	动态、平滑的局部混合。	边缘发光、地形纹理融合。


```Csharp
// N是模型法线, V是视角方向
float fresnel = 1 - saturate(dot(N, V)); 
// 使用 smoothstep 创建一个平滑的边缘遮罩
// fresnel值在0.2到0.5之间会平滑地从0过渡到1
float mask = smoothstep(0.2, 0.5, fresnel); 

// 使用这个动态计算出的 mask 作为 lerp 的权重
col.rgb = lerp(col.rgb, float3(1, 0, 0), mask);
```