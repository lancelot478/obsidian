# 从零构建 GPT

> Let's build GPT: from scratch, in code, spelled out.
> 时长：约 1 小时 56 分钟

---

## 视频目标与动机

- **目标：** 在不使用黑盒库的情况下，使用 PyTorch 纯手动实现一个类似 ChatGPT 的小规模 **GPT（生成式预训练 Transformer）**模型
- **动机：** 揭开大语言模型（LLM）的神秘面纱。通过逐行编写代码，证明 Transformer 的核心原理其实非常简单直观

---

## Transformer 架构完整实现

视频按照从简单到复杂的顺序，逐步构建 Transformer 的各个组件：

### Self-Attention（自注意力机制）

- **核心逻辑：** 每个 Token 生成三个向量：**Query（查询）**、**Key（键）**和 **Value（值）**
- **计算：** 通过 Query 和 Key 的点积计算"注意力权重"，表示当前字符对序列中其他字符的关注程度
- **缩放：** 采用 **Scaled Dot-Product Attention**，通过除以 $\sqrt{d_k}$（维度开方）防止点积结果过大导致梯度消失

### Multi-head Attention（多头注意力）

- 并行运行多个自注意力头
- 允许模型在不同的子空间内同时学习文本的不同特征
  - 例如一个头关注语法，另一个头关注语义

### Feed-Forward Network（前向传播网络）

- 在注意力层之后，对每个位置的特征进行独立的非线性变换
- 通常包含两个线性层和一个激活函数（如 ReLU）

### Residual Connections（残差连接）

- 采用 $x + \text{Sublayer}(x)$ 的结构
- 对训练深层网络至关重要，允许梯度直接通过"捷径"流回较早的层
- 有效缓解梯度消失问题

### LayerNorm（层归一化）

- 与之前的 BatchNorm 不同，LayerNorm 是在**单个样本内部的特征维度**进行归一化
- 采用放置在注意力层和前向传播层**之前**的 **"Pre-norm"** 结构，是训练稳定性的关键

---

## 位置编码（Positional Encoding）

### 必要性
- Transformer 是并行处理整个序列的，本身没有"顺序"的概念

### 实现
- 使用一个**可学习的 Positional Embedding 表**
- 将位置索引转换为向量，直接加到字符嵌入向量（Token Embedding）上
- 赋予模型感知字符物理位置的能力

---

## 因果掩码（Causal Masking）

### 原理
- 在 GPT 这种 Decoder-only 架构中，模型在预测第 $n$ 个字符时，不能看到第 $n+1$ 个及以后的字符

### 实现
- 使用一个**下三角矩阵（Lower Triangular Matrix）**对注意力权重进行掩码
- 将"未来"位置的权重设为负无穷
- 经过 Softmax 后，这些位置的概率变为 0

---

## 训练过程

### 数据集
- 使用 **"Tiny Shakespeare"** 数据集（包含莎士比亚的所有作品）

### 训练循环
1. **数据分块：** 将文本切割成固定长度的块（Context Window）
2. **损失函数：** 使用**交叉熵损失**衡量模型预测下一个字符的准确度
3. **优化器：** 使用 **AdamW** 优化器进行参数更新

### 文本生成
- 实现 `generate` 函数
- 输入一个起始 Token，让模型根据概率分布反复预测并采样下一个 Token
- 最终生成一段具有"莎士比亚风格"的文本

---

## 编程技巧与注意事项

- **向量化：** 使用 PyTorch 的矩阵运算（如 `torch.matmul`）高效地一次性计算所有注意力和前向传播
- **维度管理：** 不断强调张量的形状（Batch, Time, Channels），是编写复杂模型时不崩溃的关键
- **扩展性：** 通过调整超参数（层数、头数、嵌入维度）可以将模型从小型 Bigram 扩展为深层 Transformer

---

## 本集定位

这个视频是整个系列的**核心**，标志着从基础神经元到复杂 LLM 构建的飞跃。
