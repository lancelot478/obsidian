# Topic Modeling with NMF and SVD

fast.ai 《Computational Linear Algebra》的**第 2 讲**。这是整门课"自顶向下"教学法的第一个真正例子:从一个"给我一堆新闻文档,自动告诉我里面有哪些话题"的应用问题出发,推出两个核心矩阵分解 —— **SVD(奇异值分解)** 和 **NMF(非负矩阵分解)**。

---

## 1. 问题:什么是 Topic Modeling

给定一个文档集合(corpus),无监督地找出潜在的"话题",每个话题表现为**一组相关词**,每篇文档表现为**若干话题的混合**。

课程使用的数据集是 **20 Newsgroups**(Scikit-learn 自带),从中挑 4 个类别:
- `alt.atheism`
- `talk.religion.misc`
- `comp.graphics`
- `sci.space`

直觉:抽出的话题应该大致能对应到"宗教 / 太空 / 图形学"这些主题。

---

## 2. 数据预处理:词袋模型 → 词项-文档矩阵

用 `CountVectorizer` 或 `TfidfVectorizer` 把文本转成稀疏矩阵 $A$:

$$A \in \mathbb{R}^{m \times n}, \quad m = \text{文档数}, \quad n = \text{词表大小}$$

$A_{ij}$ = 第 $j$ 个词在第 $i$ 篇文档中的(加权)出现次数。

**关键观察**:这个矩阵秩很高、维度很大、稀疏。直接看没法理解话题 —— 需要"压缩 + 解释"。

---

## 3. 方法一:SVD(奇异值分解)

### 数学形式
$$A = U \Sigma V^T$$

- $U \in \mathbb{R}^{m \times m}$:正交,左奇异向量 → "文档-话题"矩阵
- $\Sigma \in \mathbb{R}^{m \times n}$:对角,奇异值降序排列(话题重要性)
- $V^T \in \mathbb{R}^{n \times n}$:正交,右奇异向量 → "话题-词"矩阵

### 在 Topic Modeling 中的解读
- **$V^T$ 的每一行** = 一个"话题"在词空间的分布
- **$U$ 的每一列** = 文档在该话题轴上的投影
- 取前 $k$ 个奇异值即得"截断 SVD",等价于 **LSA(Latent Semantic Analysis)**

### 课程动手内容
- `np.linalg.svd(vectors, full_matrices=False)` 做完整 SVD
- 打印每个 topic 的 top 8 个词,验证语义合理性
- 讨论:为什么 SVD 是"最优低秩近似"(Eckart–Young 定理:对 Frobenius 范数最优)

### SVD 的不足(为下一节铺垫)
- $U$、$V$ 含**负数** → "一个话题里有负的词"不好解释
- 计算 $O(\min(mn^2, m^2n))$,对大语料太重
- 解不唯一(符号、旋转)

---

## 4. 方法二:NMF(非负矩阵分解)

### 数学形式
$$A \approx W H, \quad W \ge 0, \quad H \ge 0$$

- $A \in \mathbb{R}^{m \times n}_{\ge 0}$:非负词频矩阵
- $W \in \mathbb{R}^{m \times k}_{\ge 0}$:文档 → 话题
- $H \in \mathbb{R}^{k \times n}_{\ge 0}$:话题 → 词

约束"非负"让结果天然可解释:**每个话题就是若干词的正向加权,每篇文档就是若干话题的正向叠加**(没有"负话题"这种怪东西)。

### 与 SVD 的对比

| 维度 | SVD | NMF |
|---|---|---|
| 是否非负 | 否 | **是** |
| 解是否唯一 | 是(差符号) | **否**(局部最优) |
| 是否正交 | 是 | 否 |
| 是否最优近似 | 是(Eckart–Young) | 否 |
| 可解释性 | 较差 | **强** |
| 求解方法 | 直接(LAPACK) | 迭代优化 |

### 求解算法
课程介绍三种递进的实现:
1. **Scikit-learn `decomposition.NMF`** — 黑盒调用,先看效果
2. **自己写交替最小二乘(ALS)** — 固定 $W$ 解 $H$,固定 $H$ 解 $W$,反复迭代,每步加非负投影
3. **PyTorch + 自动求导 + GPU 加速** — 用梯度下降优化 $\|A - WH\|_F^2 + \text{penalty}$,演示如何把数值线代算法搬到 GPU

### 优化目标
$$\min_{W, H \ge 0} \, \|A - WH\|_F^2$$

非凸 → 多次随机初始化、用 ALS / 投影梯度 / 乘性更新等启发式求解。

---

## 5. 数值线代视角的"为什么"

这一讲埋下了贯穿全课的几条线:
- **低秩近似**是把高维数据"看清楚"的基本工具
- **稀疏性**决定算法选择(稠密 SVD 对 30 万 × 2 万的词项矩阵不现实)
- **正交 vs 非负**是两种本质不同的"结构先验"
- **直接法 vs 迭代法**的取舍 —— 后面 Randomized SVD 一讲会回来解决"SVD 太慢"的问题

---

## 6. 课后练习 / 延伸

- 改 `k`(话题数),观察话题如何分裂/合并
- 比较 `CountVectorizer` 和 `TfidfVectorizer` 对话题质量的影响
- 把 NMF 换成 **Truncated SVD**(`sklearn.decomposition.TruncatedSVD`),对比速度与可解释性
- 思考:为什么概率主题模型 **LDA** 没在这门课讲?(答:LDA 是贝叶斯模型,不属于"矩阵分解"框架,在这门以数值线代为主线的课中不合适)

---

## 7. 配套资源

- **Notebook**:`numerical-linear-algebra/nbs/2. Topic Modeling with NMF and SVD.ipynb`
- **视频**:fast.ai YouTube,Lesson 2(约 1h20m)
- **延伸阅读**:
  - Trefethen & Bau, *Numerical Linear Algebra*, Lecture 4–5(SVD)
  - Lee & Seung (1999) *Learning the parts of objects by non-negative matrix factorization*, Nature —— NMF 的奠基论文
