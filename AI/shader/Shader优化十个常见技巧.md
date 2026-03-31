# Shader优化的十个常见技巧——技术摘要

> 来源参考：知乎文章《Shader优化的十个常见技巧——你的GPU正在燃烧，而你甚至不知道是谁点的火》  
> 整理日期：2026-03-31

---

## 核心问题：GPU性能瓶颈从何而来？

GPU的性能瓶颈主要集中在三个层面：
- **顶点处理**：过多的逐顶点计算或三角形数量
- **片元处理**：复杂的逐片元计算、高分辨率、Overdraw
- **内存带宽**：未压缩纹理、频繁的显存读写

---

## 技巧一：合理分配计算位置

**原则**：CPU → 顶点着色器 → 片元着色器，计算量依次升高，应尽量前移。

- 所有顶点共享的常量计算，放在 **CPU** 端，通过 `uniform` 传入
- 逐顶点计算放在 **顶点着色器**，通过插值传给片元
- 只有真正需要逐像素精度的计算，才放在 **片元着色器**

```hlsl
// Bad：法线归一化在片元着色器
float3 norm = normalize(i.normalWS); // 每像素执行

// Better：在顶点着色器归一化，仅轻度牺牲精度
o.normalWS = normalize(v.normalOS); // 每顶点执行，然后插值
```

---

## 技巧二：减少分支语句（if/else/for）

GPU 采用 SIMD 架构，一个 Warp/Wavefront 内所有线程必须执行相同指令。分支会导致**线程发散（Thread Divergence）**，未命中分支的线程空转，GPU 利用率直线下降。

**替代方案**：用数学函数消除分支

```hlsl
// Bad
if (x > 0.5) result = a;
else result = b;

// Good：使用 step / lerp 完全消除分支
float mask = step(0.5, x);       // x >= 0.5 时为 1，否则为 0
result = lerp(b, a, mask);
```

常用关系运算符替代：

| 逻辑          | HLSL 无分支写法             |
|-------------|--------------------------|
| `x == y`    | `1.0 - abs(sign(x - y))` |
| `x > y`     | `max(sign(x - y), 0)`    |
| `x < y`     | `max(sign(y - x), 0)`    |

---

## 技巧三：MAD 指令合并（Multiply-Add）

GPU 有专门的 **MAD（Multiply-Add）** 指令，可以将乘法和加法合并为一条指令执行，编译器能自动识别 `a * b + c` 的形式。

```hlsl
// Bad：无法合并为 MAD
result = 0.5 * (1.0 + variable);

// Good：编译器可识别并合并为一条 MAD 指令
result = 0.5 + 0.5 * variable;
```

**注意**：先写乘法部分，再加常数，有助于编译器识别 MAD 模式。

---

## 技巧四：使用 Swizzle 操作简化向量赋值

Swizzle 是零成本的寄存器重排操作，用于替代多行逐分量赋值：

```hlsl
// Bad：四条独立指令
result.x = pos.x;
result.y = pos.y;
result.z = pos.z;

// Good：一条 Swizzle 指令
result.xyz = pos.xyz;
```

---

## 技巧五：优先使用 Shader 内置函数

内置函数（如 `normalize`、`dot`、`lerp`、`clamp`、`saturate`）由硬件厂商深度优化，通常在**单个时钟周期**内完成，远快于自行实现。

```hlsl
// Bad：手动实现点积求和
result = v.x + v.y + v.z;

// Good：使用内置 dot，硬件加速
result = dot(v, float3(1, 1, 1));

// Bad：手动线性插值
result = a * (1.0 - t) + b * t;

// Good：使用内置 lerp
result = lerp(a, b, t);
```

---

## 技巧六：避免高成本数学函数

以下函数属于**超越函数（Transcendental Functions）**，计算成本极高：`pow`、`sin`、`cos`、`tan`、`log`、`exp`、`sqrt`

**替代策略**：
1. **查找纹理（LUT）**：将复杂函数结果预计算并存入纹理，运行时采样代替计算
2. **多项式近似**：用低阶多项式拟合函数曲线
3. **硬件指令**：使用 `rsqrt`（快速平方根倒数）代替 `1.0 / sqrt(x)`

```hlsl
// Bad：高成本
float atten = pow(dist, 2.2);

// Good：改用 LUT 纹理采样（预计算）
float atten = tex2D(_LUT, float2(dist, 0)).r;

// Good：rsqrt 替代 normalize 中的 sqrt
float3 n = v * rsqrt(dot(v, v));
```

---

## 技巧七：合理选择数据精度

| 精度类型       | HLSL       | GLSL         | 位宽  | 适用场景          |
|-------------|------------|--------------|-----|---------------|
| 全精度         | `float`    | `highp`      | 32位 | 世界坐标、UV坐标     |
| 半精度         | `half`     | `mediump`    | 16位 | 颜色、法线、方向向量   |
| 低精度         | `fixed`    | `lowp`       | 11位 | 0~1 范围的颜色值    |

移动端（Mali/Adreno/PowerVR）对 `half` 有硬件级加速，大量使用 `float` 会显著增加 ALU 消耗。

```hlsl
// 移动端优化示例
half4 frag(v2f i) : SV_Target {
    half3 color = tex2D(_MainTex, i.uv).rgb;  // half 而非 float
    half light = dot(i.normalWS, _LightDir);
    return half4(color * light, 1.0);
}
```

---

## 技巧八：纹理优化

- **使用压缩格式**：PC 用 DXT/BC，移动端用 ETC2/ASTC，显存占用降低 4~8 倍
- **开启 Mipmap**：减少远处纹理采样时的 Cache Miss，提升带宽效率
- **通道打包**：将多张灰度图（粗糙度、金属度、AO）打包到 RGBA 单张纹理中，减少采样次数
- **避免依赖纹理读取**：在片元着色器中，若 UV 坐标依赖前一次纹理采样结果，会严重破坏纹理预取流水线

```hlsl
// Bad：依赖纹理读取（第二次采样依赖第一次的结果）
float2 offset = tex2D(_OffsetTex, i.uv).rg;
float4 color = tex2D(_MainTex, i.uv + offset);

// Better：尽量在顶点阶段计算 offset UV
```

---

## 技巧九：谨慎使用 discard / clip

`discard`（GLSL）/ `clip`（HLSL）会丢弃当前片元，看似节省了后续计算，实则带来隐患：

- **破坏 Early-Z 优化**：GPU 通常在片元着色器执行前进行深度预测，`discard` 让 GPU 无法确定片元是否会被丢弃，导致 Early-Z 失效
- **移动端 TBDR 架构影响**：对 PowerVR 等 TBDR GPU 影响尤为显著

**替代方案**：对于简单透明裁切，尝试用 Alpha-to-Coverage 或 Alpha Blend 替代。

---

## 技巧十：使用 Index Buffer 而非纯 Vertex Buffer

绘制网格时，始终使用**索引绘制**（`glDrawElements` / `DrawIndexed`）而非非索引绘制（`glDrawArrays`）：

- **减少数据传输量**：共享顶点只存储一次，不重复
- **利用 Post-T&L Cache**：GPU 顶点着色器结果会被缓存，索引引用相同顶点时直接命中缓存，顶点着色器执行次数可以**少于顶点数**

```
顶点总数：100  索引总数：300（每个顶点平均被引用3次）
使用 Index Buffer → 顶点着色器执行约 100 次（借助缓存）
不使用 Index Buffer → 顶点着色器执行 300 次
```

---

## 总结：优化优先级参考

| 优先级 | 优化手段                 | 效果预期     |
|-----|----------------------|----------|
| ★★★ | 减少分支语句               | 显著，尤其移动端 |
| ★★★ | 计算任务前移（CPU/顶点阶段）     | 显著       |
| ★★★ | 精度降级（float→half）      | 移动端显著    |
| ★★  | 使用内置函数 / MAD / Swizzle | 中等       |
| ★★  | 避免超越函数，使用 LUT 替代    | 中等       |
| ★★  | 纹理压缩与通道打包            | 带宽优化明显   |
| ★   | 谨慎使用 discard          | 针对特定平台   |
| ★   | 使用 Index Buffer       | 顶点密集场景   |

---

## 推荐分析工具

| 工具                        | 适用平台         | 用途             |
|---------------------------|--------------|----------------|
| RenderDoc                 | PC/主机/移动端    | 帧捕获与 Shader 分析 |
| Mali Offline Compiler     | ARM Mali GPU | 指令数与寄存器分析      |
| Adreno GPU Profiler       | 高通 Adreno    | 移动端 GPU 性能分析   |
| NVIDIA Nsight Graphics    | NVIDIA GPU   | PC 端深度 Shader 调试 |
| AMD Radeon GPU Profiler   | AMD GPU      | PC 端性能分析       |
| Xcode GPU Frame Capture   | iOS/Apple    | Apple GPU 调试   |
