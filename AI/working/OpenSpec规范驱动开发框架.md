# OpenSpec 是什么

## 问题

OpenSpec 是什么？

## 回答

OpenSpec 是一个**轻量级的规范驱动开发（Spec-Driven Development, SDD）框架**，专门为 AI 编码助手（如 Claude Code、Cursor、Windsurf 等）设计。核心思想是：**让 AI 写代码之前，先把"要做什么"用规范定义清楚，然后按规范实施，全程可追溯。**

### 解决的核心问题

用 AI 写代码时经常遇到的痛点：
- 需求藏在聊天记录里，越聊越散
- AI 容易"幻觉"或偏离需求
- 代码与预期不符，难以维护和追溯

### 核心架构

OpenSpec 将项目知识分为两部分：

| 目录 | 作用 |
|---|---|
| `openspec/specs/` | 系统规范的"单一真相源"（当前行为的完整描述） |
| `openspec/changes/` | 待开发的变更提案（每个变更一个文件夹） |

### 工作流程：四步走

1. **Propose（提案）** — 告诉 AI 要加什么功能，AI 自动生成提案、规范变更、设计方案和任务清单
2. **Review（评审）** — 核对规范，与 AI 反复打磨直到需求明确
3. **Apply（实施）** — AI 按规范逐项完成任务，每完成一项标记进度
4. **Archive（归档）** — 功能完成后归档，规范自动合并到主目录更新系统状态

### OpenSpec 的三个本质能力

1. **能力标准化（Spec）** — AI 可以"理解"每个能力的输入输出
2. **调用协议统一（Tool Calling）** — AI 以统一方式调用不同工具
3. **模型与能力解耦** — 模型负责决策，Skill 负责执行，可以随意替换模型

### 适用场景

- 已有项目的功能迭代和团队协作
- 跨模块修改需要统一需求追溯
- 混用多款 AI 编码工具（Cursor、Claude Code 等）需统一输出标准

### 安装使用

```bash
npm install -g @fission-ai/openspec@latest
cd your-project
openspec init
```

### 一句话总结

OpenSpec = AI 世界的"能力操作系统规范"。它不是让 AI 直接输出答案，而是让 AI 输出**行动（Action）**，按照标准化的规范去调度和执行任务。
