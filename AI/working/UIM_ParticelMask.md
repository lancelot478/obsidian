[[#Q1 UIM_ParticleMask 遮挡上层 UI 的原因分析]]
[[#Q2 UIM_ParticleMask 遮挡上层 UI 的真正原因]]
## Q1:UIM_ParticleMask 遮挡上层 UI 的原因分析

### 根本原因：Stencil Buffer 冲突

`UIM_ParticleMask` 使用的是 `FX_Mask.shader`（`Assets/Shaders/Effects/FX_Mask.shader`），其核心逻辑如下：

```glsl
ColorMask 0          // 不输出任何颜色像素（不可见）
Pass {
    Stencil {
        Ref 5        // Stencil 参考值 = 5
        Comp Always  // 总是通过 Stencil 测试
        Pass Replace // 将覆盖区域的 Stencil Buffer 写入值 5
    }
}
```

**问题在于：**
1. `UIM_ParticleMask` 会把它所覆盖区域的 Stencil Buffer **强制写成 5**
2. Unity 的 UI 系统（`Mask` 组件）也依赖 Stencil Buffer 来做裁剪，默认使用 Stencil 值 1、2、3...（嵌套层级递增）
3. 当上层 UI 元素渲染时，如果它们处于 Unity `Mask` 组件的管理下，会检查 Stencil Buffer 期望的值（比如 0 或 1），但实际读到的是被 `UIM_ParticleMask` 写入的 **5**，导致 Stencil 测试失败，UI 元素被丢弃不渲染 → **表现为被"遮挡"**

简而言之：**`UIM_ParticleMask` 的 Stencil 写入污染了 Stencil Buffer，干扰了上层 UI 的 Stencil 测试。**

---

### 项目中已有的解决方案

**1. 初始设计方案（commit `a23f5eb3d48`，作者 sunhaoxiang）**

`FX_Mask.shader` 和 `FX_Ui.shader` 是配套设计的：
- `FX_Mask.shader`（用于 `UIM_ParticleMask`）：写入 Stencil 值 5
- `FX_Ui.shader`（用于 UI 粒子特效）：支持可配置的 Stencil 参数

```glsl
// FX_Ui.shader 中的 Stencil 配置（可在材质面板中调整）
_StencilComp = 8    // 默认 Always（不做 Stencil 测试）
_Stencil = 0        // 默认参考值 0
_StencilOp = 0      // 默认 Keep
```

粒子特效材质可设置 `_Stencil = 5, _StencilComp = Equal`，使特效只在 Mask 区域内显示。

**2. 常见修复手段（从 git 历史中看到的做法）**

- **修改层级关系**：调整 `UIM_ParticleMask` 在 UI 层级中的位置，确保它不与上层 UI 重叠（如 TTDBL-32144、TTDBL-20569 等修复）
- **使用 Stencil ReadMask/WriteMask 隔离**：通过位掩码让粒子 Mask 使用不同的 Stencil Bit，避免和 Unity UI Mask 系统冲突。例如 Unity UI 用低位(bit 0-3)，粒子 Mask 用高位(bit 4-7)
- **使用 `Shader_Ui_Mask.shader`**（`Assets/Art/TA/UiShader/`）：支持 BlendMode 枚举（含 Mask 模式），且 Stencil 参数完全可配

**3. 推荐的通用解决方案**

如果需要 `UIM_ParticleMask` 不影响上层 UI，最干净的方式是修改 `FX_Mask.shader`，使用 **WriteMask** 隔离 Stencil 位：

```glsl
Stencil {
    Ref 5
    Comp Always
    Pass Replace
    WriteMask 240  // 0xF0 - 只写高4位，不影响 Unity UI 使用的低4位
}
```

同时配套修改 `FX_Ui.shader` 的粒子材质的 `ReadMask` 为 240，让粒子只读高4位的 Stencil 值。这样两套 Stencil 系统互不干扰。
## Q2 :UIM_ParticleMask 遮挡上层 UI 的真正原因

### 根本原因：`FX_Mask.shader` 缺少 `ZWrite Off`

`FX_Mask.shader`（第11-12行）只写了 `ColorMask 0`（不输出颜色），但**没有写 `ZWrite Off`**，Unity 默认 `ZWrite On`。

这意味着 `UIM_ParticleMask` 虽然是不可见的，但它**向深度缓冲(Z-Buffer)写入了深度值**。后续渲染的上层 UI 元素默认使用 `ZTest LEqual`，如果它们的 Z 值 ≥ Mask 的 Z 值，就会被深度测试剔除，表现为"被遮挡"。

这解释了为什么**把上层界面的 Transform.position.z 改小（更靠近相机）就能显示** —— 因为 Z 值变小后通过了深度测试。

### 修复方案

**方案1（推荐 - 从根源修复）：** 在 `FX_Mask.shader` 中加 `ZWrite Off`：
```glsl
cull off
ColorMask 0
ZWrite Off    // ← 加上这一行，不写深度
```

**方案2（当前的临时方案）：** 把上层 UI 的 `Transform.position.z` 改小，使其比 Mask 更靠近相机，从而通过深度测试。这只是绕过问题，不是根治。

**方案3：** 上层 UI 的材质设置 `ZTest Always`（始终通过深度测试），但这可能影响其他层级关系。

方案1是最干净的修复，因为 `UIM_ParticleMask` 的设计意图就是一个纯 Stencil 遮罩，不应该影响深度缓冲。




