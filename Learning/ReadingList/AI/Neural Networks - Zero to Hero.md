# Neural Networks: Zero to Hero

> Andrej Karpathy 的神经网络系列课程，从零构建深度学习模型直至复现 GPT-2。

---

## 1. 神经网络与反向传播入门：构建 micrograd

- 数学基础：标量导数、偏导数、链式法则
- 自动微分引擎：实现 `Value` 类（加法、乘法、tanh 等操作）
- 计算图：构建 DAG，使用拓扑排序实现 `.backward()`
- 神经网络模块：定义 Neuron、Layer、MLP 类
- 优化：均方误差损失函数、梯度下降

## 2. 语言建模入门：构建 makemore

- 数据处理：字符级数据集、整数-字符映射
- Bigram 模型：字符对计数、概率矩阵、`torch.multinomial` 采样
- 神经网络方法：One-Hot 编码 + 单层线性层
- 评估：负对数似然（NLL）与交叉熵

## 3. makemore Part 2：MLP

- 架构：基于 Bengio et al. 2003 的 MLP
- 嵌入层：字符查找表（look-up embeddings）
- 层结构：张量展平、tanh 隐藏层、输出 logits
- 训练技巧：mini-batch、学习率搜索与衰减
- 模型选择：训练集/验证集/测试集划分，监控过拟合

## 4. makemore Part 3：激活值、梯度与 BatchNorm

- 初始化：死神经元问题、Kaiming 初始化
- Batch Normalization：逐步实现（均值/方差/gamma/beta）
- 训练监控：激活值分布、梯度分布、更新与数据比率可视化

## 5. makemore Part 4：成为反向传播忍者

- 手动反向传播：逐层手推梯度（Linear、Tanh、BatchNorm、Cross-Entropy）
- 向量化梯度：多维张量梯度计算、Jacobian 矩阵

## 6. makemore Part 5：构建 WaveNet

- 层级架构：从"扁平"MLP 到 WaveNet 风格的层级结构
- 代码重构：实现模块化（Linear、BatchNorm1d、Sequential、Embedding 类）
- 空洞卷积：通过层级分组实现感受野扩大

## 7. 从零构建 GPT

- 自注意力机制：Query、Key、Value，缩放点积注意力
- 因果掩码：三角矩阵防止"窥视未来"
- Transformer Block：多头注意力、残差连接、LayerNorm、前馈网络
- 位置编码：位置嵌入（Positional Embeddings）

## 8. GPT 现状（State of GPT）

- 训练流程：预训练 → SFT（监督微调）→ RLHF（人类反馈强化学习）
- 趋势与局限：幻觉问题、提示工程、思维链（Chain-of-Thought）

## 9. 构建 GPT Tokenizer

- 编码基础：Unicode 与 UTF-8 字节表示
- BPE 算法：迭代合并最高频字节/token 对
- GPT-2/GPT-4 细节：文本分割正则、特殊 token（`<|endoftext|>`）

## 10. 复现 GPT-2（124M）

- 大规模优化：混合精度训练（BF16）、Flash Attention
- 工程实践：加载 OpenAI 预训练权重、分布式数据并行（DDP）多 GPU 训练
- 基准评测：HellaSwag 零样本性能对比
