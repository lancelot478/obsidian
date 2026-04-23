# Claude Code 的 Harness 工程介绍

## 问题
介绍下 Claude Code 的 harness 工程

## 回答

### 什么是 Claude Code？

Claude Code 是 Anthropic 推出的一个**终端原生的 AI 编程代理工具**，它可以在命令行中直接运行，帮助开发者完成代码编写、调试、重构等任务。

### Harness（评估框架）工程

在 Claude Code 的语境中，**Harness** 主要指的是其**评估与测试框架**体系，核心包含以下几个方面：

#### 1. SWE-bench Harness（软件工程基准评估）

- **SWE-bench** 是一个基于真实 GitHub issues 和 pull requests 构建的基准测试集
- Harness 负责：
  - 自动化地将代码仓库检出到对应的 commit 版本
  - 设置隔离的运行环境（通常使用 Docker 容器）
  - 将 AI 生成的 patch 应用到代码库
  - 运行对应的测试用例，验证修复是否正确
  - 收集并汇总评测结果
- Claude Code 在 SWE-bench Verified 上取得了业界领先的成绩（72.7%+ 的解决率）

#### 2. Harness 的核心架构

| 组件 | 功能 |
|------|------|
| **Task Runner** | 任务调度器，管理评测任务的分发与执行 |
| **Environment Setup** | 环境初始化，包括代码仓库克隆、依赖安装、容器构建 |
| **Agent Interface** | AI agent 的调用接口，向 Claude 发送任务描述并接收响应 |
| **Patch Applier** | 将 AI 产出的代码变更应用到项目中 |
| **Test Executor** | 执行项目自带的测试套件，判断修改是否正确 |
| **Result Collector** | 收集测试结果、日志、指标等数据 |

#### 3. 工作流程

```
任务描述(Issue) → Agent推理 → 生成代码补丁 → 应用补丁 → 运行测试 → 判定通过/失败
      ↑                                                              |
      └──────────── 反馈循环（可选多轮）────────────────────────────────┘
```

#### 4. 关键设计理念

- **隔离性**：每个任务运行在独立的沙箱/容器环境中，避免互相干扰
- **可复现性**：通过固定 commit hash、依赖版本等确保结果可复现
- **可扩展性**：支持并行执行多个评测任务
- **工具使用（Tool Use）**：harness 为 agent 提供了文件读写、命令执行、搜索等工具接口，agent 通过调用这些工具与代码库交互

#### 5. 与 Agentic Coding 的关系

Harness 本质上是一个**自动化评测管道**，它的设计思路也反映了 Claude Code 的核心理念：
- Agent 拥有 **plan → act → observe → reflect** 的完整循环
- 通过工具调用（tool use）而非直接生成完整代码来与环境交互
- 支持多轮迭代，可以在测试失败后重新尝试

### 总结

Claude Code 的 harness 工程是连接"AI 能力"与"真实软件工程任务"的桥梁。它通过自动化的评测流水线，在受控环境中验证 AI agent 解决实际代码问题的能力，是推动 agentic coding 技术进步的关键基础设施。

> **注意**：以上内容基于公开信息和通用的 AI 评测框架知识整理。Anthropic 内部的具体实现细节可能有所不同。
