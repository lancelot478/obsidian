# 目录

- [[#Q1: 分析下这个项目]]
- [[#Q2: 记录问答到 Heartopia.md]]
- [[#Q3: 项目入口 pnpm dev:desktop:remote]]
- [[#Q4: view 参数判断全流程]]
- [[#Q5: MAIN_WINDOW_VITE_DEV_SERVER_URL 流转]]
- [[#Q6: agent-instructions.base.md 生效机制]]
- [[#Q7: vite.renderer.config.ts 配置含义]]
- [[#Q8: initLogger 初始化逻辑]]
- [[#Q9: Claude Code CLI 的 ANTHROPIC_API_KEY 来源]]
- [[#Q10: smoke test 与 app-ready presence]]
- [[#Q11: localDb ensureReady 逻辑]]
- [[#Q12: openWithPragmas 详解]]
- [[#Q13: local-db:corruption-restored 通信流程]]
- [[#Q14: 为什么通过 preload 中转]]
- [[#Q15: 本地 DB 首次迁移日志流程]]
- [[#Q16: Drizzle 在项目里的实现方式]]
- [[#Q17: executeUpdateMacOS 更新实现]]
- [[#Q18: EnvCheckContext.tsx 职责与 hooks 用法]]
- [[#Q19: EnvCheck Phase 1 和 Phase 2 的关系]]
- [[#Q20: safeResolve 与 updateResult 流程]]
- [[#Q21: window.electronAPI.checkEnvironment 调用链]]
- [[#Q22: docs/design_docs/app-init.pen 生成与使用]]
- [[#Q23: Pencil 如何写出 .pen 设计稿]]
- [[#Q24: 请求使用 Pencil MCP 生成 settings 设计稿]]
- [[#Q25: 项目如何接入 Feishu]]
- [[#Q26: /api/auth/login 路径链路]]
- [[#Q27: req.body as Record<string, unknown> 解构]]
- [[#Q28: prisma.user.upsert 按 Feishu 用户创建或更新]]
- [[#Q29: prisma.ts 是什么]]
- [[#Q30: globalForPrisma 类型断言]]
- [[#Q31: refreshToken.upsert 按用户设备刷新登录态]]
- [[#Q32: SignJWT 签发 access token 流程]]
- [[#Q33: 该项目用了哪些数据库]]
- [[#Q34: 为什么要用这么多不同的数据库]]
- [[#Q35: PostgreSQL 怎么使用的]]
- [[#Q36: Prisma 关系约束与索引详解]]
- [[#Q37: findMany include _count 与 $queryRaw 详解]]
- [[#Q38: PostgreSQL 特性运用深度解读]]
- [[#Q39: prisma.session 怎么读到对应的数据库表]]
- [[#Q40: prisma 为什么要 migration]]
- [[#Q41: router.post login next 含义与路由原理]]
- [[#Q42: authenticate 中间件做了什么]]
- [[#Q43: JWT 是什么]]
- [[#Q44: access + refresh 双 token 模式详解]]
- [[#Q45: RefreshToken relation 与索引怎么理解]]
- [[#Q46: api:request IPC handler 自动 refresh 怎么理解]]
- [[#Q47: logout 最大 1 小时不一致与黑名单退化为 Session]]
- [[#Q48: scheduleRefresh 在干什么]]

## Q1: 分析下这个项目

**问题**

分析下这个项目

**回答**

我做了一轮只读分析，没有改文件。当前仓库在 `main...origin/main`，只有一个未跟踪目录 `.idea/`。

**项目画像**

这是一个 pnpm workspace monorepo，核心产品是心动内部的 Electron 桌面 AI 工作台。它把 Claude Code / Codex、飞书、Jira、GitLab、MCP、定时任务、SkillHub、本地 SQLite 会话存储串在一起。整体定位不是 SaaS，而是本地客户端 + 远端内部 API。

主要结构：

- `apps/desktop`：最大核心，Electron 三层架构：`main` 负责业务、网络、DB、agent/MCP；`preload` 做 IPC 桥；`renderer` 只做 React UI。
- `packages/maker-core`：agent 抽象核心，统一 Claude Code / Codex 会话、事件翻译、权限交互、memory。明确要求零 Electron 依赖。
- `packages/lizi-mcps`：可复用 MCP server 工厂，桌面 main 作为 host 注入能力。
- `packages/maker-scheduler`：定时任务引擎。
- `packages/voice-input-core`：语音输入状态机与后处理核心。
- `apps/server`：Express + Prisma + PostgreSQL，负责飞书 OAuth、SkillHub、GitLab/Jira 代理、issue、API key 同步等。
- `apps/heartbeat-server`：在线/心跳相关服务。
- `apps/landing-page`：静态下载页。

**关键架构判断**

1. renderer 不做业务逻辑。UI 通过 preload 暴露的 IPC 调 main。数据获取也要异步完成后再刷新，避免空白帧和跳变。
2. maker-core/package 必须可独立复用。`maker-core` 通过依赖注入拿 logger、auth、storage、binary path、MCP providers 等，不应该 import Electron 或 desktop localDb。
3. agent 功能以 `Maker -> BaseAgent -> ClaudeCodeAgent/CodexAgent` 为主线。renderer 不应该感知 vendor 差异，vendor 事件由 translator 转成统一 `AgentEvent`。
4. 本地数据逐步迁移到 desktop SQLite。`apps/desktop/src/main/localDb` + drizzle migrations 很重；服务端仍保留老 chat/session 路由和迁移路由。
5. 视觉规范很严格。UI 改动前必须读 `DESIGN.md`，整体是克制的黑白灰反色设计，少颜色、无阴影、扁平分隔。非 trivial UI 应先做 `.pen` 设计稿。

**开发入口**

日常开发默认走远端 API：

```bash
pnpm restart:desktop:remote
```

只有明确改后端路由、Prisma、服务端鉴权链路时才用本地 server。

常用验证：

```bash
pnpm --filter desktop test
pnpm --filter desktop lint
pnpm --filter server test
pnpm --filter maker-core build
pnpm lint
```

**比较容易踩坑的地方**

- 不要直接碰 `apps/desktop/xdt-updater`，规则里写了这个模块非常重要，任何修改都要确认。
- 不要在 renderer 里 `console.log` 或做 IO；日志走统一 logger。
- 不要把 agent 的确定性逻辑丢给 prompt，能代码判断的就代码实现。
- 跨平台要求很强：路径、spawn、文件监听、快捷键、SQLite/文件锁都要同时考虑 macOS / Windows。
- `.xdmaker/project-knowledge` 有模块知识库，但 `apps--desktop` 和 `packages--maker-core` 标记为 stale，只能当参考，最终仍以源码为准。
- 如果改了 `agent-use/docs/`，必须跑 `pnpm sync:agent-instructions` 同步 `CLAUDE.md` / `AGENTS.md`。

整体看，这是一个已经进入“复杂产品工程”阶段的仓库：抽象边界是清楚的，但桌面端历史包袱和迁移层很多。后续无论修 bug 还是加功能，最好先定位属于 `renderer UI`、`desktop main host`、`maker-core package`、还是 `server API`，然后沿着既有边界小步修改。

## Q2: 记录问答到 Heartopia.md

**问题**

将上一个回答和后续回答记录到 `/Users/gexianglin/self/obsidian/obsidian/Work/AI/Heartopia.md` 中，并采用一问一答的方式维护目录和问答内容。

**回答**

已在 `/Users/gexianglin/self/obsidian/obsidian/Work/AI/Heartopia.md` 中创建目录区域，并补录上一轮“分析下这个项目”的问答。后续在本对话中继续按 `## Q{序号}: {问题}` 的格式追加记录，并同步更新文件开头的 Obsidian wikilink 目录。

## Q3: 项目入口 pnpm dev:desktop:remote

**问题**

先介绍下项目入口，`pnpm dev:desktop:remote`

**回答**

`pnpm dev:desktop:remote` 是桌面端远端 API 开发入口。命令链路如下：

1. 根目录 `package.json`

```bash
pnpm dev:desktop:remote
```

实际执行：

```bash
node scripts/ensure-deps.mjs && pnpm --filter desktop dev:remote
```

第一步检查依赖和环境；第二步只进入 workspace 包 `apps/desktop` 执行它自己的 `dev:remote`。

2. `apps/desktop/package.json`

`dev:remote` 实际执行：

```bash
pnpm dev:winsetup && cross-env VITE_API_BASE_URL=https://xdt-api.magiclizi.com electron-forge start
```

含义：

- Windows 下先执行 `chcp 65001`，减少终端编码问题。
- 设置 `VITE_API_BASE_URL=https://xdt-api.magiclizi.com`，所以 desktop 连接远端 API，不需要本地 server。
- 用 `electron-forge start` 启动 Electron 开发模式。

3. Electron Forge + Vite 入口

`apps/desktop/forge.config.ts` 里配置了三个入口：

- main entry：`apps/desktop/src/main/index.ts`
- preload entry：`apps/desktop/src/preload/preload.ts`
- renderer：`apps/desktop/src/renderer`，通过 `vite.renderer.config.ts` 指向 renderer root

4. main 进程入口

`apps/desktop/src/main/index.ts` 是 Electron main 的第一入口。它先做三件关键事：

- 初始化统一 logger。
- 调用 `fixPath()`，把 macOS GUI 启动时缺失的 shell PATH 补回来，避免后续找不到 agent CLI、ripgrep 等工具。
- 调用 `stripSensitiveAnthropicEnv()`，清理用户环境里的 Anthropic / Claude Code 敏感变量，避免泄漏到子进程。

然后动态 import：

```ts
await import('./bootstrap-electron.js')
```

真正的大启动逻辑在 `bootstrap-electron.ts`。

5. bootstrap 主流程

`apps/desktop/src/main/bootstrap-electron.ts` 是桌面端 main 侧的核心启动文件。启动时会注册协议、IPC、认证/MCP/IM/本地 DB/调度器/更新/心跳等能力。

关键顺序：

- app ready 前注册自定义协议，例如 `xdt-image://`、`xdt-video://`、`xdt-file://`、`xdt-audio://`。
- 注册通用 IPC handlers。
- 注册 localDb、worktree、voice input、maker、scheduler、SkillHub、file browser 等 IPC。
- 初始化 Feishu / Google / Jira token 状态。
- 创建主窗口 `BrowserWindow`。
- 初始化 update service 和 heartbeat service。

6. BrowserWindow 加载 renderer

`createWindow()` 创建主窗口。开发模式下走：

```ts
mainWindow.loadURL(MAIN_WINDOW_VITE_DEV_SERVER_URL)
```

也就是加载 Electron Forge/Vite 起的 renderer dev server。生产模式才会加载打包后的 HTML 文件。

preload 通过：

```ts
preload: path.join(__dirname, 'preload.js')
```

注入到窗口里。

7. preload 桥接层

`apps/desktop/src/preload/preload.ts` 用 `contextBridge.exposeInMainWorld('electronAPI', ...)` 暴露能力给 renderer。

renderer 不直接碰 Node/Electron API，所有文件、网络、数据库、agent、MCP、系统窗口操作都通过这里转发到 main IPC。

8. renderer 入口

`apps/desktop/src/renderer/index.tsx` 是 React 入口。它会先初始化字体、i18n、主题、滚动条、memory/compat 设置镜像，然后根据 URL 参数判断是否是语音输入浮窗；普通主视图会渲染：

```tsx
<ConfirmDialogProvider>
  <App />
</ConfirmDialogProvider>
```

`App.tsx` 继续挂载 Provider：

- `ThemeProvider`
- `LocaleProvider`
- `EnvCheckProvider`
- `AuthProvider`
- `UserPreferencesProvider`
- `WorktreeProvider`
- `RouterProvider`

路由定义在 `apps/desktop/src/renderer/router.tsx`。

9. 首屏路由结构

路由主线：

- 未登录：`/login`
- 已登录：`ProtectedRoute`
- 本地 DB / migration 准备：`MigrationGate`
- 主布局：`MainLayout`
- 默认跳到：`/cc-agent`

主要功能路由：

- `/cc-agent`：agent 会话主功能
- `/cc-agent/new`：新建 maker 草稿
- `/cc-agent/scheduled`：调度任务页
- `/cc-agent/files/:sessionId`：工作区文件浏览
- `/skillhub`：SkillHub
- `/issues`：GitLab issue 引导页
- `/settings`：设置页

一句话总结：`pnpm dev:desktop:remote` 从根脚本进入 desktop 包，设置远端 API 地址后启动 Electron Forge；Forge 用 Vite 分别构建 main/preload/renderer；main 从 `src/main/index.ts` 进入，再由 `bootstrap-electron.ts` 注册所有主进程能力并创建窗口；窗口通过 preload 暴露 IPC 给 React renderer，renderer 从 `src/renderer/index.tsx` 挂载应用和路由。

## Q4: view 参数判断全流程

**问题**

`const view = new URLSearchParams(window.location.search).get('view');` 怎么判断是什么 view 的，全流程介绍。

**回答**

`view` 不是 React Router 的业务路由，而是 Electron main 进程创建“特殊窗口”时塞进 URL query 的启动标记。普通主窗口没有 `view`，所以进入完整 `<App />`；特殊窗口带 `view`，renderer 入口直接渲染对应轻量组件。

当前只有两个特殊 view：

- `voice-input-overlay`：全局语音输入浮窗。
- `voice-input-dictionary-toast`：语音输入词典学习 toast。

流程如下：

1. 普通主窗口路径

`bootstrap-electron.ts` 的 `createWindow()` 创建主窗口。dev 模式下调用：

```ts
mainWindow.loadURL(MAIN_WINDOW_VITE_DEV_SERVER_URL)
```

这里没有 `?view=...`，所以 renderer 里：

```ts
const view = new URLSearchParams(window.location.search).get('view');
```

拿到的是 `null`。随后进入普通主应用：

```tsx
root.render(
  <ConfirmDialogProvider>
    <App />
  </ConfirmDialogProvider>,
);
```

`App` 再挂 `RouterProvider`，进入 `/login`、`/cc-agent`、`/settings` 等正常路由。

2. 全局语音输入浮窗来源

语音输入浮窗由 main 进程 `apps/desktop/src/main/voice-input/global.ts` 创建。文件里定义：

```ts
const OVERLAY_QUERY = 'view=voice-input-overlay';
```

创建 overlay 窗口时：

```ts
if (MAIN_WINDOW_VITE_DEV_SERVER_URL) {
  const url = new URL(MAIN_WINDOW_VITE_DEV_SERVER_URL);
  url.search = OVERLAY_QUERY;
  window.loadURL(url.toString());
} else {
  window.loadFile(indexHtml, {
    query: { view: 'voice-input-overlay' },
  });
}
```

所以 dev 下 URL 类似：

```text
http://localhost:<vite-port>/?view=voice-input-overlay
```

prod 下 URL 类似：

```text
file:///.../index.html?view=voice-input-overlay
```

renderer 启动时读到 `view === 'voice-input-overlay'`，于是动态 import：

```tsx
const { VoiceInputOverlay } = await import('./voice-input/VoiceInputOverlay');
root.render(
  <ThemeProvider>
    <LocaleProvider>
      <VoiceInputOverlay />
    </LocaleProvider>
  </ThemeProvider>,
);
return;
```

这个 `return` 很关键：它不会渲染完整 `<App />`，也不会进入主路由。

3. 词典 toast 来源

词典 toast 也由 `global.ts` 创建。文件里定义：

```ts
const DICTIONARY_TOAST_QUERY = 'view=voice-input-dictionary-toast';
```

创建 toast 窗口时，除了 `view`，还会把词条数据放到 `entries` query：

```ts
const url = new URL(MAIN_WINDOW_VITE_DEV_SERVER_URL);
url.search = DICTIONARY_TOAST_QUERY;
url.searchParams.set('entries', JSON.stringify(payload.entries));
window.loadURL(url.toString());
```

prod 下则通过 `loadFile(..., { query: { view, entries } })` 传入。

renderer 读到 `view === 'voice-input-dictionary-toast'` 后：

```tsx
const { VoiceInputDictionaryToast } = await import('./voice-input/VoiceInputDictionaryToast');
root.render(
  <ThemeProvider>
    <LocaleProvider>
      <VoiceInputDictionaryToast />
    </LocaleProvider>
  </ThemeProvider>,
);
return;
```

`VoiceInputDictionaryToast` 内部再用：

```ts
new URLSearchParams(window.location.search)
```

读取 `entries`，解析成 toast 显示内容。

4. 为什么要在 renderer 入口判断

这些特殊窗口虽然和主窗口共用同一个 renderer bundle / `index.html`，但它们不是完整应用页面：

- 语音浮窗要透明背景、无主布局、无路由、轻量启动。
- toast 只显示一个小提示，不应该初始化完整 App。
- TapDB PV 只在主视图启用，避免 overlay/toast 弹出被算成页面访问。

所以 `index.tsx` 在最顶层分流，先判断 `view`，命中特殊 view 就提前渲染并返回。

5. 特殊窗口和 main 的通信

`preload.ts` 暴露 `window.electronAPI.voiceInput.*`。例如 overlay mount 后会：

```ts
window.electronAPI.voiceInput.notifyGlobalOverlayReady();
```

main 收到 `voice-input:global-overlay-ready` 后，如果有待启动的 overlay session，就发送：

```ts
window.webContents.send('voice-input:global-overlay-command', { type: 'start' });
```

renderer 的 `VoiceInputOverlay` 订阅 `onGlobalOverlayCommand`，收到：

- `start`：开始录音。
- `submit`：停止并粘贴。
- `cancel`：取消并关闭。

toast 则通过 `voice-input:dictionary-toast-close` 关闭窗口。

一句话总结：`view` 是 main 创建 BrowserWindow 时写进 URL query 的“窗口类型标记”；renderer 入口读取它，决定渲染完整主应用，还是只渲染语音输入 overlay / 字典 toast 这种特殊轻量窗口。

## Q5: MAIN_WINDOW_VITE_DEV_SERVER_URL 流转

**问题**

`MAIN_WINDOW_VITE_DEV_SERVER_URL` 是什么，怎么流转的。

**回答**

`MAIN_WINDOW_VITE_DEV_SERVER_URL` 是 Electron Forge 的 Vite 插件为 renderer 窗口生成并注入到 main 进程 bundle 里的全局常量。它表示开发模式下 renderer dev server 的 URL，例如：

```text
http://localhost:5173
```

它不是项目手写的 `.env` 变量，也不是 `import.meta.env`。项目里只在 `apps/desktop/env.d.ts` 声明了类型：

```ts
declare const MAIN_WINDOW_VITE_DEV_SERVER_URL: string;
declare const MAIN_WINDOW_VITE_NAME: string;
```

真正的值由 `@electron-forge/plugin-vite` 根据 `forge.config.ts` 的 renderer 配置生成。

流转链路：

1. `forge.config.ts` 注册 renderer 名称

```ts
new VitePlugin({
  build: [
    {
      entry: 'src/main/index.ts',
      config: 'vite.main.config.ts',
      target: 'main',
    },
    {
      entry: 'src/preload/preload.ts',
      config: 'vite.preload.config.ts',
      target: 'preload',
    },
  ],
  renderer: [
    {
      name: 'main_window',
      config: 'vite.renderer.config.ts',
    },
  ],
})
```

这里的 `name: 'main_window'` 很关键。Forge Vite 插件会基于这个 name 注入两类常量：

- `MAIN_WINDOW_VITE_DEV_SERVER_URL`
- `MAIN_WINDOW_VITE_NAME`

命名规则可以理解为：`main_window` 转成大写前缀 `MAIN_WINDOW`。

2. `electron-forge start` 启动 renderer dev server

执行 `pnpm dev:desktop:remote` 后，最终会跑：

```bash
electron-forge start
```

Forge Vite 插件会启动 renderer 的 Vite dev server。`vite.renderer.config.ts` 指定了 renderer root：

```ts
root: path.resolve(__dirname, 'src/renderer')
```

此时插件知道 dev server 实际 URL，于是把这个 URL 注入给 main bundle 的 `MAIN_WINDOW_VITE_DEV_SERVER_URL`。

3. main 进程创建窗口时读取它

`bootstrap-electron.ts` 的 `createWindow()` 里：

```ts
if (MAIN_WINDOW_VITE_DEV_SERVER_URL) {
  mainWindow.loadURL(MAIN_WINDOW_VITE_DEV_SERVER_URL);
} else {
  mainWindow.loadFile(
    path.join(__dirname, `../renderer/${MAIN_WINDOW_VITE_NAME}/index.html`),
  );
}
```

开发模式下 `MAIN_WINDOW_VITE_DEV_SERVER_URL` 有值，所以窗口直接加载 Vite dev server。

生产打包模式下它为空或不存在有效 URL，于是走 `loadFile()` 加载打包后的：

```text
.vite/renderer/main_window/index.html
```

这里 `MAIN_WINDOW_VITE_NAME` 对应 renderer 配置中的 `main_window`。

4. 特殊窗口复用同一个 dev server

语音输入 overlay 和 dictionary toast 也用同一个常量。它们先拿主 renderer dev server URL，再追加 query：

```ts
const url = new URL(MAIN_WINDOW_VITE_DEV_SERVER_URL);
url.search = 'view=voice-input-overlay';
window.loadURL(url.toString());
```

因此 dev 下特殊窗口也是加载同一个 renderer dev server，只是 URL 多了 `?view=...`，再由 `src/renderer/index.tsx` 分流到特殊组件。

5. 它和 `VITE_API_BASE_URL` 的区别

`MAIN_WINDOW_VITE_DEV_SERVER_URL`：

- Forge Vite 插件注入。
- 给 Electron main 用。
- 表示“renderer 页面从哪里加载”。
- 开发模式有值，生产模式走 `loadFile`。

`VITE_API_BASE_URL`：

- 脚本或 `.env` 提供。
- 给业务代码用。
- 表示“后端 API 请求发到哪里”。
- `pnpm dev:desktop:remote` 会设成 `https://xdt-api.magiclizi.com`。

一句话总结：`MAIN_WINDOW_VITE_DEV_SERVER_URL` 是 Forge/Vite 在开发模式下自动给 main 进程的 renderer dev server 地址；main 用它决定 `BrowserWindow` 加载热更新 dev server，还是生产包里的本地 `index.html`。

## Q6: agent-instructions.base.md 生效机制

**问题**

`agent-instructions.base.md` 这个文件是怎么生效的。

**回答**

`agent-use/docs/agent-instructions.base.md` 本身不会被 Codex 或 Claude Code 直接自动读取。它是“源模板”。真正被 agent 读取的是仓库根目录的：

- `AGENTS.md`
- `CLAUDE.md`

生效链路如下：

1. 修改源文件

通用规则写在：

```text
agent-use/docs/agent-instructions.base.md
```

如果有 Codex 专属追加内容，写在：

```text
agent-use/docs/agent-instructions.codex.md
```

如果有 Claude Code 专属追加内容，写在：

```text
agent-use/docs/agent-instructions.claude.md
```

当前两个专属文件只有注释，所以实际主要内容来自 base。

2. 执行同步脚本

根目录 `package.json` 里有脚本：

```bash
pnpm sync:agent-instructions
```

实际执行：

```bash
bash agent-use/scripts/sync-agent-instructions.sh
```

3. 同步脚本生成根目录文件

`sync-agent-instructions.sh` 做两次生成：

```bash
generate "Claude Code" "CLAUDE.md"  "agent-instructions.claude.md" "CLAUDE.md"
generate "Codex"       "Codex.md"   "agent-instructions.codex.md"  "AGENTS.md"
```

生成逻辑是：

- 读取 `agent-instructions.base.md`
- 替换占位符：
  - `{{AGENT_NAME}}`
  - `{{AGENT_TITLE}}`
- 追加对应 agent 的专属文件内容
- 输出到根目录 `CLAUDE.md` 或 `AGENTS.md`

所以：

```text
base.md + claude suffix -> CLAUDE.md
base.md + codex suffix  -> AGENTS.md
```

4. agent 启动时读取根目录规则

README 里写明：使用 Claude Code 或 Codex 时，将工作目录设置为仓库根目录，agent 会自动加载 `CLAUDE.md` / `AGENTS.md` 中的协作规范。

也就是说：

- Codex 主要读根目录 `AGENTS.md`
- Claude Code 主要读根目录 `CLAUDE.md`

`agent-use/docs/agent-instructions.base.md` 是维护源，不是最终运行时入口。

5. 为什么不能直接改 AGENTS.md / CLAUDE.md

因为这两个文件顶部写了：

```md
本文件由脚本自动生成，请勿直接修改 CLAUDE.md / AGENTS.md。
如果你修改了 agent-use/docs/ 下的任何文件，必须立即运行 pnpm sync:agent-instructions 刷新两份文件。
```

如果直接改 `AGENTS.md`，下次运行同步脚本会被覆盖。

正确改法：

```bash
# 1. 修改源模板
vim agent-use/docs/agent-instructions.base.md

# 2. 同步生成
pnpm sync:agent-instructions

# 3. 检查生成结果
git diff AGENTS.md CLAUDE.md agent-use/docs/agent-instructions.base.md
```

一句话总结：`agent-instructions.base.md` 通过 `pnpm sync:agent-instructions` 生成根目录 `AGENTS.md` 和 `CLAUDE.md`，Codex/Claude 真正加载的是这两个根目录文件；base 是统一维护入口。

## Q7: vite.renderer.config.ts 配置含义

**问题**

解释下面这段配置是什么意思：

```ts
export default defineConfig({
  root: path.resolve(__dirname, 'src/renderer'),
  envDir: __dirname,
  plugins: [react()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, 'src/renderer'),
    },
  },
  build: {
    // Force outDir relative to project root (not renderer root),
    // so electron-forge can find it at .vite/renderer/main_window/
    outDir: path.resolve(__dirname, '.vite/renderer/main_window'),
    emptyOutDir: true,
  },
});
```

**回答**

这是 `apps/desktop/vite.renderer.config.ts`，专门配置 Electron renderer，也就是 React 前端页面的 Vite 构建方式。

逐项解释：

1. `root`

```ts
root: path.resolve(__dirname, 'src/renderer')
```

告诉 Vite：renderer 前端项目的根目录是：

```text
apps/desktop/src/renderer
```

所以 Vite 会从这个目录找 `index.html`、`index.tsx`、前端源码等。

2. `envDir`

```ts
envDir: __dirname
```

告诉 Vite：读取 `.env` 文件时，不要去 `src/renderer` 下找，而是在 `apps/desktop` 下找。

这里 `__dirname` 对应：

```text
apps/desktop
```

所以 renderer 可以读取：

```text
apps/desktop/.env
apps/desktop/.env.local
apps/desktop/.env.production
```

这样 `VITE_API_BASE_URL`、`VITE_FEISHU_APP_ID` 这类变量就能从 desktop 包根目录读取。

3. `plugins`

```ts
plugins: [react()]
```

启用 Vite React 插件，用来支持 React JSX/TSX、Fast Refresh 等前端开发能力。

4. `resolve.alias`

```ts
alias: {
  '@': path.resolve(__dirname, 'src/renderer'),
}
```

配置路径别名。之后 renderer 代码里可以这样写：

```ts
import { Button } from '@/components/ui/button';
```

等价于：

```ts
import { Button } from 'apps/desktop/src/renderer/components/ui/button';
```

好处是避免大量 `../../../components/...`。

5. `build.outDir`

```ts
outDir: path.resolve(__dirname, '.vite/renderer/main_window')
```

指定 renderer 打包产物输出到：

```text
apps/desktop/.vite/renderer/main_window
```

这和 `forge.config.ts` 里的 renderer 名称对应：

```ts
renderer: [
  {
    name: 'main_window',
    config: 'vite.renderer.config.ts',
  },
]
```

Electron Forge 需要在这个位置找到打包后的 renderer 页面。生产模式下 main 进程会加载：

```ts
path.join(__dirname, `../renderer/${MAIN_WINDOW_VITE_NAME}/index.html`)
```

其中 `MAIN_WINDOW_VITE_NAME` 就是 `main_window`。

6. `emptyOutDir`

```ts
emptyOutDir: true
```

每次 build 前先清空输出目录，避免旧的 JS/CSS 产物残留，导致 Electron 加载到过期文件。

一句话总结：这段配置把 `apps/desktop/src/renderer` 定义成 React 前端根目录，允许用 `@` 指向 renderer 源码，读取 `apps/desktop/.env`，并把打包结果输出到 Electron Forge 约定的 `.vite/renderer/main_window` 目录。

## Q8: initLogger 初始化逻辑

**问题**

`export function initLogger(opts?: InitOptions): void`

**回答**

`initLogger` 位于 `apps/desktop/src/main/logger.ts`，作用是一次性初始化 Electron main 进程的统一日志系统。

函数签名：

```ts
export interface InitOptions {
  isDev?: boolean;
  level?: LogLevel;
  logDir?: string;
}

export function initLogger(opts?: InitOptions): void
```

参数含义：

- `isDev`：手动指定是否为 dev 模式；不传时默认用 `!app.isPackaged`。
- `level`：默认日志等级；优先级低于 `process.env.LOG_LEVEL` 和 `log-config.json`。
- `logDir`：手动指定日志目录；不传时按 dev / packaged 自动决定。

核心流程：

1. 幂等保护：如果已经初始化过，直接返回。所以 `src/main/index.ts` 和 `bootstrap-electron.ts` 都调用 `initLogger()` 也只会真正执行一次。
2. 判断 dev / packaged：`isDevMode = opts?.isDev ?? !app.isPackaged`。
3. 决定日志目录：dev 写到 `apps/desktop/logs/`，packaged 写到 `<userData>/logs/`。
4. 决定日志等级：优先级是 `process.env.LOG_LEVEL` > `log-config.json` > `opts.level` > 内置默认值。dev 默认 `trace`，packaged 默认 `info`。
5. 创建日志文件：打开 `main.log`、`maker.log`、`cc-proxy.log` 三个文件。
6. 按 scope 路由日志：`maker` / `maker/*` / `r:maker*` 进 `maker.log`；`cc-proxy` / `cc-proxy/*` 进 `cc-proxy.log`；其他进 `main.log`。
7. 日志文件裁剪：单文件超过 5MB 时保留尾部约 2MB，避免无限增长。
8. dev 模式额外双写终端，方便实时观察。
9. 接管 `console.*`，把第三方库或遗留代码的 console 输出也纳入统一日志。
10. 注册 `uncaughtException` 和 `unhandledRejection` 兜底，把未捕获异常写进日志。
11. 写入启动日志：`=== App started === v{version} level={level} isDev={isDevMode}`。

一句话总结：`initLogger()` 是 main 进程日志系统的启动开关，负责确定日志模式、目录、等级、文件路由、裁剪策略、console 接管和异常兜底；调用后其他代码通过 `createLogger(scope)` 写统一日志。

## Q9: Claude Code CLI 的 ANTHROPIC_API_KEY 来源

**问题**

Claude Code CLI 的 `ANTHROPIC_API_KEY` 从哪里获得的。

**回答**

Claude Code CLI 的 `ANTHROPIC_API_KEY` 不是从用户系统环境变量继承来的。项目启动时会主动清掉系统环境里的 `ANTHROPIC_API_KEY`、`ANTHROPIC_AUTH_TOKEN` 等敏感 Anthropic / Claude Code 变量，避免用户机器上的旧 key 泄漏到 CLI 子进程。

真正来源是 desktop 本地的 Electron `safeStorage` 加密文件：

```text
<userData>/safe-storage/api_key.enc
```

链路如下：

1. key 如何写入本地

主要有几条路径会写入同一个 key：

- 用户在 Settings 的 API Key 区域保存 key。
- 登录成功后，`authManager` 从服务端 `/api/users/me/api-key` 拉取用户级 API key 并写入本地。
- renderer 的 `useApiKey` 初始化时会做 reconcile：本地没有而服务端有，就写本地；服务端没有而本地有，就推到服务端。
- 运行中遇到 key 可能失效时，也可以通过 `api-key:refresh-from-server` 从服务端刷新并写入本地。

写入时走 main 的 IPC：

```ts
window.electronAPI.safeStorageStore('api_key', key)
```

main 侧使用：

```ts
safeStorage.encryptString(value)
```

然后写到：

```text
<userData>/safe-storage/api_key.enc
```

2. Claude AuthAdapter 如何读取

desktop main 侧的 `DesktopClaudeAuthAdapter` 实现了 maker-core 的 `AuthAdapter`。

它的 `readClaudeApiKey()` 会：

- 检查 `safeStorage.isEncryptionAvailable()`
- 读取 `<userData>/safe-storage/api_key.enc`
- base64 decode
- `safeStorage.decryptString(buffer)` 解密
- 返回明文 API key

然后：

```ts
async getAuthEnv(): Promise<Record<string, string>> {
  const apiKey = readClaudeApiKey();
  return apiKey ? { ANTHROPIC_API_KEY: apiKey } : {};
}
```

所以 Claude 鉴权 env 的直接来源是：

```text
DesktopClaudeAuthAdapter.getAuthEnv()
  -> readClaudeApiKey()
  -> safe-storage/api_key.enc
  -> { ANTHROPIC_API_KEY: apiKey }
```

3. Maker 如何把它交给 Claude Code Agent

`apps/desktop/src/main/maker-host/index.ts` 创建 Claude agent 时注入：

```ts
const claudeAgent = new ClaudeCodeAgent({
  auth: desktopClaudeAuthAdapter,
  runtimeConfig: buildDesktopClaudeRuntimeConfig(getClaudeEndpoint),
  ...
});
```

也就是 ClaudeCodeAgent 不自己知道 key 存在哪里，只依赖 `auth.getAuthEnv()`。

4. Claude env 如何组装

`ClaudeCodeAgent.startSession()` 里会调用：

```ts
const env = await buildClaudeEnv(this.deps.auth, this.deps.runtimeConfig);
```

`buildClaudeEnv()` 的关键顺序：

- `cleanProcessEnv()`：从 process.env 副本里剥掉敏感 Anthropic 字段
- 合并 behavior flags
- 注入 endpoint 为 `ANTHROPIC_BASE_URL`
- 最后合并 `auth.getAuthEnv()`，也就是 `{ ANTHROPIC_API_KEY }`
- 注入 `CLAUDE_CODE_PROVIDER_MANAGED_BY_HOST=1`

因此最终传给 Claude Code CLI 子进程的 env 里，`ANTHROPIC_API_KEY` 来自 desktop safeStorage，而不是系统环境。

5. endpoint 和 key 是分开的

`ANTHROPIC_API_KEY` 来自 `DesktopClaudeAuthAdapter`。

`ANTHROPIC_BASE_URL` 来自 runtime config，也就是 `buildDesktopClaudeRuntimeConfig(getClaudeEndpoint)`。默认上游是：

```text
https://llm-proxy.tapsvc.com
```

如果兼容模式开了，Claude Code 子进程可能看到的是本地 loopback proxy URL。

一句话总结：Claude Code CLI 的 `ANTHROPIC_API_KEY` 来源是 xdt-maker 本地 `safeStorage` 加密保存的 `api_key.enc`，由 `DesktopClaudeAuthAdapter.getAuthEnv()` 解密后注入到 `buildClaudeEnv()`，最终随 spawn env 传给 Claude Code CLI；系统环境里的同名变量会被清理，不作为可信来源。

## Q10: smoke test 与 app-ready presence

**问题**

解释：

```ts
const smoke = parseSmokeArgs();
if (smoke.enabled) {
  await runSmokeTest(smoke.userId);
  return;
}

await ensureMainAppPresence('app-ready');
```

**回答**

这段位于 `bootstrap-electron.ts` 的 `app.on('ready', async () => { ... })` 最前面，用来区分“发布/CI 冒烟测试模式”和“正常桌面启动模式”。

`parseSmokeArgs()` 会检查 Electron 启动参数：

- 如果 `process.argv` 包含 `--smoke-test`，则 `enabled = true`
- 如果包含 `--smoke-user=<id>`，就用这个 fake userId
- 否则 userId 默认是 `__smoke_test__`

命中 smoke 模式后：

```ts
await runSmokeTest(smoke.userId);
return;
```

`runSmokeTest()` 不创建窗口、不走 OAuth、不启动 auto-update、不启动 agent、不初始化 IM/MCP 主链路。它只做本地数据库初始化检查：

- 调用 `localDbEnsureReady(userId)`
- 查询 `migration_meta` 里的 `schema_version`
- 查询核心表计数：`sessions`、`messages`、`migration_meta`
- 把 JSON 结果写到 stdout
- 关闭 DB，然后退出 app

这主要给 release 脚本做打包后验证，确认 packaged app 至少能启动本地 DB、跑 migration、读核心表。

所以这里的 `return` 很重要：smoke test 完成后直接短路，不再进入正常 app 初始化。

如果不是 smoke 模式，才继续：

```ts
await ensureMainAppPresence('app-ready');
```

`ensureMainAppPresence()` 来自 `appPresence.ts`，作用是保证主应用在平台原生入口里保持可见：

- macOS：确保 app 是 regular activation policy，并显示在 Dock
- Windows：如果传了 mainWindow，则确保主窗口不被 `skipTaskbar`

这里传入 `'app-ready'` 是 reason，用于日志排查。如果恢复失败，会记录 reason、platform 和错误。

为什么 app ready 一开始就要做这个？因为项目里有语音输入 overlay 这种 transient / tool-like 窗口。它们可能影响 macOS Dock 或 Windows taskbar 表现。项目把“主应用必须保持可见”作为 app-level invariant，集中由 `appPresence.ts` 管。

一句话总结：这段先判断是否是发布脚本用的 `--smoke-test` 模式；如果是，就只做本地 DB 冒烟检查并退出。正常启动时，则先调用 `ensureMainAppPresence('app-ready')`，确保主应用在 Dock/taskbar 中保持正常可见，再继续后面的完整桌面端初始化。

## Q11: localDb ensureReady 逻辑

**问题**

`export async function ensureReady(userId: string): Promise<EnsureReadyResult>`

**回答**

`ensureReady(userId)` 是 desktop main 进程里本地 SQLite 数据库的准备入口。它位于 `apps/desktop/src/main/localDb/index.ts`。

它的职责是：根据当前登录用户 `userId` 打开对应的本地 DB 文件，完成修复、迁移、扩展加载和优化准备。只有它成功后，其他模块才能安全调用 `getDrizzle()` / `getRawDb()`。

返回类型：

```ts
export type EnsureReadyResult =
  | { ready: true }
  | {
      ready: false;
      error: {
        code: 'DB_INIT_FAILED' | 'DB_CORRUPT_NO_BACKUP' | 'MIGRATE_FAILED';
        message: string;
      };
    };
```

核心流程：

1. 校验 userId

如果 `userId` 为空或不是字符串，直接返回：

```ts
{ ready: false, error: { code: 'DB_INIT_FAILED', message: 'invalid userId' } }
```

2. 同一用户已就绪则复用

如果 `_currentUserId === userId` 且 `_db` 已存在，说明当前用户的 DB 已经打开，直接返回：

```ts
{ ready: true }
```

3. 切用户时先关闭旧库

如果当前已有 `_currentUserId`，但和新 `userId` 不同，就调用 `closeDb()`。

这是防止跨账号串库：不同用户对应不同 DB 文件。

4. 计算 DB 文件路径

```ts
path.join(app.getPath('userData'), `xdt-maker-${userId}.db`)
```

也就是每个用户一个本地数据库文件：

```text
<userData>/xdt-maker-{userId}.db
```

5. 打开 SQLite 并设置 PRAGMA

调用 `openWithPragmas(filePath)`，内部用 `better-sqlite3` 打开 DB，并设置：

- `journal_mode = WAL`
- `foreign_keys = ON`
- `synchronous = NORMAL`
- `temp_store = MEMORY`
- `mmap_size = 268435456`
- `cache_size = -65536`
- `busy_timeout = 5000`

这些配置用于性能、并发、外键约束和写入稳定性。

6. 处理数据库损坏

如果打开 DB 时遇到 `SQLITE_CORRUPT` 或错误信息包含 `corrupt`，会走：

```ts
tryRestoreWithFallback(filePath)
```

恢复策略是两级回落：

- 优先尝试 `.bak.clean`
- 再尝试 `.bak.{ISO}` 备份

如果恢复失败，弹 OS fatal dialog，并返回：

```ts
DB_CORRUPT_NO_BACKUP
```

如果恢复成功，会通知所有 renderer：

```ts
local-db:corruption-restored
```

用于弹一次性 toast。

7. 创建 Drizzle 实例并记录当前状态

```ts
_drizzle = drizzle(_db!, { schema });
_currentUserId = userId;
_currentDbPath = filePath;
```

从这一步开始，`getDrizzle()` 和 `getRawDb()` 才有可用对象。

8. 加载 sqlite-vec 扩展

在 migration 前加载：

```ts
loadSqliteVec(_db!)
```

原因是后续 migration 可能创建 `USING vec0(...)` 的虚拟表。如果加载顺序反了，会报 `no such module: vec0`。

加载失败不会立刻阻塞 app 启动，但相关 migration / 业务后续会自然报错。

9. 跑 migrations

```ts
await runMigrations(_db!, filePath)
```

如果 migration 失败：

- 记录日志
- `closeDb()`
- 弹 fatal dialog
- 返回 `MIGRATE_FAILED`

10. 处理 schema drift

migration 后调用：

```ts
await handleSchemaDrift(filePath)
```

用途是处理多人协作分支下 migration 编号 / hash 冲突导致的“schema_version 已推进，但物理表不一致”。

策略：

- packaged：只 log + 发 toast 警告，不动 DB
- dev：检测 drift，备份 DB，尝试反射 repair；修不了时弹 dev-only nuke 对话框

11. 启动 SQLite optimize

成功后：

```ts
runOptimize(0x10002);
startOptimizeSchedule();
```

启动时跑一次 `PRAGMA optimize`，之后每 24 小时周期跑一次，让 SQLite query planner 有较新的统计信息。

12. 返回 ready

全部成功后：

```ts
return { ready: true };
```

调用入口：

- renderer 登录/迁移流程通过 IPC `local-db:ensure-ready` 调用
- smoke test 直接调用 `localDbEnsureReady(userId)`
- `registerLocalDbIpc({ onReady })` 在 ready 后触发 scheduler 启动重试

一句话总结：`ensureReady(userId)` 是本地聊天数据 SQLite 的启动闸门，负责按用户打开 DB、恢复损坏备份、加载扩展、跑 migration、修 schema drift、启动 optimize；它成功后，本地会话/消息/scheduler 等依赖 localDb 的模块才可以工作。

## Q12: openWithPragmas 详解

**问题**

详细介绍：

```ts
function openWithPragmas(filePath: string): Database.Database {
  const db = new Database(filePath);
  db.pragma('journal_mode = WAL');
  db.pragma('foreign_keys = ON');
  db.pragma('synchronous = NORMAL');
  db.pragma('temp_store = MEMORY');
  db.pragma('mmap_size = 268435456');
  db.pragma('cache_size = -65536');
  db.pragma('busy_timeout = 5000');
  return db;
}
```

**回答**

`openWithPragmas(filePath)` 是“打开本地 SQLite 数据库，并立刻设置一组运行参数”的函数。它返回的是 `better-sqlite3` 的数据库连接对象。

第一句：

```ts
const db = new Database(filePath);
```

用 `better-sqlite3` 打开数据库文件。如果文件不存在，SQLite 会创建它。

本机示例路径：

```text
/Users/gexianglin/Library/Application Support/xdt-maker/xdt-maker-cmpkzzfkk0zchoc01q9566kox.db
```

接下来都是 SQLite PRAGMA，用来调整连接行为。

1. `journal_mode = WAL`

```ts
db.pragma('journal_mode = WAL');
```

开启 WAL 模式，全称 Write-Ahead Logging。

WAL 模式下：

```text
主 db 文件：保存稳定数据
-wal 文件：追加写入新事务
-shm 文件：共享内存索引
```

优点：

- 读写并发更好
- 写入性能更好
- app 崩溃后可通过 WAL 恢复
- 适合桌面应用长连接

所以目录里可能会看到：

```text
xdt-maker-xxx.db
xdt-maker-xxx.db-wal
xdt-maker-xxx.db-shm
```

这是正常的。

2. `foreign_keys = ON`

```ts
db.pragma('foreign_keys = ON');
```

启用外键约束。SQLite 默认外键不一定启用。开了之后，如果 schema 里有：

```sql
FOREIGN KEY (session_id) REFERENCES sessions(id)
```

插入不存在的 `session_id` 会报错，而不是产生孤儿数据。这对 sessions/messages 这种关系表很重要。

3. `synchronous = NORMAL`

```ts
db.pragma('synchronous = NORMAL');
```

控制 SQLite 写入时 fsync 的严格程度。

常见值：

```text
FULL    更安全，但 fsync 多，慢
NORMAL  WAL 模式下通常足够安全，性能更好
OFF     快但风险高
```

这里用 `NORMAL` 是常见生产组合：在 WAL 下，进程崩溃通常不丢已提交事务；只有极端情况，比如系统掉电，可能丢最近一笔事务。

不用 `FULL` 的原因是桌面端会频繁写消息、会话、调度记录，`FULL` 的 fsync 成本会明显拖慢。

4. `temp_store = MEMORY`

```ts
db.pragma('temp_store = MEMORY');
```

让 SQLite 的临时数据尽量放内存。SQLite 查询时可能需要临时表，例如：

- sort
- group by
- join 中间结果
- 临时 index

默认可能落磁盘。这里设成 `MEMORY`，可以减少磁盘 I/O，提高查询速度。代价是复杂查询会多用一点 RAM，对桌面端本地 DB 来说通常可接受。

5. `mmap_size = 268435456`

```ts
db.pragma('mmap_size = 268435456');
```

设置 memory-mapped I/O 大小，单位是字节。

```text
268435456 = 256 * 1024 * 1024 = 256MB
```

含义是允许 SQLite 把最多 256MB 数据库内容映射到进程地址空间。

好处：

- 读热数据时减少 read 系统调用
- 让 OS page cache 更自然接管
- 本地查询会更快

它不是立刻占用 256MB 物理内存，而是允许映射这么大范围；实际内存由 OS 按访问页调度。

6. `cache_size = -65536`

```ts
db.pragma('cache_size = -65536');
```

设置 SQLite page cache 大小。

SQLite 约定：

- 正数：表示 page 数量
- 负数：表示 KB

所以：

```text
-65536
```

表示：

```text
65536 KB = 64 MB
```

也就是 SQLite 连接内部最多用约 64MB page cache。

作用：

- 缓存常用页面
- 减少重复磁盘读取
- 对消息列表、搜索、会话切换更友好

7. `busy_timeout = 5000`

```ts
db.pragma('busy_timeout = 5000');
```

设置遇到数据库锁时等待多久，单位毫秒。

```text
5000ms = 5 秒
```

SQLite 同一时间只能有一个 writer。虽然这个项目 main 进程独占 DB，但仍可能出现短暂锁竞争，例如：

- migration 正在写
- scheduler 写入
- chat 消息落库
- clean snapshot / backup
- 某些事务还没结束

如果不设 `busy_timeout`，遇到锁可能立刻抛：

```text
SQLITE_BUSY
```

设了 5000 后，SQLite 会等待最多 5 秒，锁释放后继续执行。这能减少偶发写入失败。

最后：

```ts
return db;
```

返回已经配置好的连接。

注意这些 PRAGMA 是连接级/数据库级混合：

- 有些会写进数据库持久状态，例如 `journal_mode = WAL`
- 有些主要作用于当前连接，例如 `foreign_keys`、`busy_timeout`

所以每次打开连接都设置一遍是正确做法。

整体可以理解成：

```text
打开 db
  -> 开 WAL，提高本地读写并发
  -> 开 foreign_keys，保证关系约束
  -> 调低 fsync 成本
  -> 临时数据走内存
  -> 开 256MB mmap
  -> 给 SQLite 64MB page cache
  -> 遇锁最多等 5 秒
  -> 返回 db 连接
```

一句话总结：`openWithPragmas()` 是这个项目本地 SQLite 的性能和可靠性初始化，它把 better-sqlite3 默认连接调成更适合桌面端长连接、频繁读写、本地会话数据的配置。

## Q13: local-db:corruption-restored 通信流程

**问题**

`local-db:corruption-restored` 这个是怎么通信的，画出流程图。

**回答**

`local-db:corruption-restored` 是一个 main → preload → renderer 的单向 IPC push 事件，用来在本地 SQLite 损坏后成功从备份恢复时通知 UI 弹 toast。

触发点在 `apps/desktop/src/main/localDb/index.ts` 的 `ensureReady(userId)`。当打开 DB 抛出 `SQLITE_CORRUPT` 或错误信息包含 `corrupt` 时，main 会调用：

```ts
const restored = tryRestoreWithFallback(filePath);
```

如果恢复成功，会重新打开 DB，然后广播：

```ts
const payload = {
  source: restored.source,
  backupMtime: restored.mtime.toISOString(),
};

for (const w of BrowserWindow.getAllWindows()) {
  try {
    w.webContents.send('local-db:corruption-restored', payload);
  } catch {
    /* renderer 已销毁等 */
  }
}
```

payload 类型在 renderer 的 `vite-env.d.ts` 里定义：

```ts
interface CorruptionRestoredPayload {
  source: 'clean' | 'iso';
  backupMtime: string;
}
```

通信流程图：

```mermaid
sequenceDiagram
    participant R as Renderer MigrationGate
    participant P as Preload electronAPI.localDb
    participant M as Main localDb IPC
    participant DB as SQLite DB file
    participant B as Backup restore
    participant UI as Renderer MainLayout Toast Hook

    R->>P: window.electronAPI.localDb.ensureReady(user.id)
    P->>M: ipcRenderer.invoke("local-db:ensure-ready", userId)
    M->>DB: openWithPragmas(filePath)
    DB-->>M: throws SQLITE_CORRUPT / corrupt
    M->>B: tryRestoreWithFallback(filePath)
    B-->>M: { source, mtime }
    M->>DB: openWithPragmas(filePath) again
    DB-->>M: open success
    M->>P: webContents.send("local-db:corruption-restored", payload)
    P->>UI: fanOutCorruptionRestored callback(payload)
    UI->>UI: sessionStorage 去重
    UI->>UI: toast.warning("数据库已从 ... 的快照恢复...")
    M-->>P: ensureReady result { ready: true }
    P-->>R: Promise resolves
```

preload 侧在 `apps/desktop/src/preload/preload.ts` 里先创建 fan-out：

```ts
const fanOutCorruptionRestored = createIpcFanOut('local-db:corruption-restored');
```

然后暴露给 renderer：

```ts
localDb: {
  onCorruptionRestored: fanOutCorruptionRestored,
}
```

`createIpcFanOut` 的作用是：同一个 IPC channel 只绑定一个底层 `ipcRenderer.on`，再把收到的 payload 分发给多个 renderer callback。最后一个订阅者取消订阅时，会移除底层 listener。

renderer 消费方是 `useCorruptionRestoredToast()`：

```ts
const unsubscribe = window.electronAPI.localDb.onCorruptionRestored((info) => {
  if (sessionStorage.getItem(SHOWN_FLAG_KEY) === '1') return;
  sessionStorage.setItem(SHOWN_FLAG_KEY, '1');

  const formatted = formatLocalDateTime(new Date(info.backupMtime));
  toast.warning(
    t('logic.toasts.corruptionRestored', { time: formatted }),
    { duration: 8000 },
  );
});
```

这个 hook 在 `MainLayout` 里挂载：

```ts
useCorruptionRestoredToast();
```

完整结构可以看成：

```mermaid
flowchart TD
    A["MigrationGate 调 localDb.ensureReady(user.id)"] --> B["main: ensureReady(userId)"]
    B --> C["openWithPragmas(filePath)"]
    C -->|DB 正常| D["继续 migration / ready"]
    C -->|SQLITE_CORRUPT| E["tryRestoreWithFallback(filePath)"]
    E -->|无可用备份| F["showFatalDialog + ready:false"]
    E -->|恢复成功| G["重新 openWithPragmas"]
    G --> H["组装 payload: source + backupMtime"]
    H --> I["BrowserWindow.getAllWindows()"]
    I --> J["webContents.send('local-db:corruption-restored', payload)"]
    J --> K["preload fanOutCorruptionRestored"]
    K --> L["renderer onCorruptionRestored callback"]
    L --> M["sessionStorage 去重"]
    M --> N["toast.warning 恢复提示"]
```

需要注意：这是即时 push 事件，没有持久化队列，也没有 replay。main 发送时如果 renderer 还没订阅 `onCorruptionRestored`，事件不会自动补发。当前源码里触发发生在 `MigrationGate.ensureReady()` 过程中，而 toast hook 挂在 `MainLayout`，所以从严格时序看存在“恢复事件早于 MainLayout 订阅”的潜在丢事件风险。

一句话总结：`local-db:corruption-restored` 是 main 在 DB 损坏恢复成功后通过 `webContents.send` 广播的事件；preload 用 fan-out 包装成 `window.electronAPI.localDb.onCorruptionRestored`；renderer 的 `useCorruptionRestoredToast` 订阅后做 sessionStorage 去重并弹 warning toast。

## Q14: 为什么通过 preload 中转

**问题**

为什么要通过 preload 中转。

**回答**

在 Electron 里，main、preload、renderer 是三层不同职责：

- main：有完整 Node/Electron 权限，负责文件、数据库、网络、系统 API、子进程等。
- preload：运行在 renderer 页面加载前，能访问有限 Electron/Node API，也能通过 `contextBridge` 暴露安全接口。
- renderer：React 页面，原则上只负责 UI 渲染，不应该直接碰 Node、fs、数据库、ipcRenderer。

通过 preload 中转，主要有几个原因：

1. 安全隔离

如果 renderer 直接拿到 `ipcRenderer` 或 Node API，任何前端代码、依赖漏洞、XSS 都可能直接调用：

```ts
ipcRenderer.invoke(...)
fs.readFile(...)
child_process.spawn(...)
```

这对桌面应用非常危险。

preload 用 `contextBridge.exposeInMainWorld()` 只暴露白名单 API，例如：

```ts
window.electronAPI.localDb.ensureReady(userId)
window.electronAPI.localDb.onCorruptionRestored(cb)
```

renderer 看不到完整 `ipcRenderer`，只能调用项目允许的能力。

2. 符合项目架构约束

项目规则明确要求：

- renderer 只负责渲染
- main 负责业务逻辑、网络、数据存储、package 通信
- renderer 不直接访问 Node/Electron API

preload 正好是这条边界的桥。

3. API 收口与类型稳定

main 里的真实 channel 名是：

```text
local-db:ensure-ready
local-db:corruption-restored
maker:event
voice-input:event
```

preload 把它们包装成稳定、结构化的前端 API：

```ts
window.electronAPI.localDb.ensureReady(...)
window.electronAPI.localDb.onCorruptionRestored(...)
window.electronAPI.maker.send(...)
```

这样 renderer 不需要到处写 channel 字符串，也不需要知道底层是 `invoke`、`send` 还是 `on`。

4. 可以做 fan-out 和订阅清理

以 `local-db:corruption-restored` 为例，preload 不是简单透传，它用了：

```ts
createIpcFanOut('local-db:corruption-restored')
```

这个封装保证每个 IPC channel 只有一个底层 `ipcRenderer.on`，再分发给多个 React callback。最后一个订阅者取消时，底层 listener 也会移除。

这能避免 React HMR / StrictMode / 多组件订阅导致的重复 listener、重复 toast、内存泄漏。

5. 可以做运行时校验和兼容适配

preload 里很多 API 会做 payload 形状校验，例如收到系统网络错误时确认：

```ts
payload &&
typeof payload === 'object' &&
typeof payload.code === 'string'
```

这样 main push 过来的数据不会完全裸奔到 UI。

6. 支持 contextIsolation

现代 Electron 推荐开启 context isolation。renderer 页面和 preload 脚本运行在不同 JavaScript context。renderer 不能直接 import Electron，只能通过 preload 显式暴露的桥访问能力。

这让桌面 app 更接近“浏览器页面 + 安全原生桥”的模型。

用 `local-db:corruption-restored` 举例，链路是：

```mermaid
flowchart LR
    M["main: webContents.send('local-db:corruption-restored', payload)"]
    P1["preload: ipcRenderer.on(channel)"]
    P2["preload: fanOut callbacks"]
    R["renderer: window.electronAPI.localDb.onCorruptionRestored(cb)"]
    UI["React hook: toast.warning(...)"]

    M --> P1 --> P2 --> R --> UI
```

如果没有 preload，renderer 就必须直接接触：

```ts
ipcRenderer.on('local-db:corruption-restored', ...)
```

这会让 UI 层知道底层 channel，破坏安全边界，也更容易产生 listener 泄漏。

一句话总结：preload 是 Electron 的安全桥和项目的架构边界；它把 main 的高权限 IPC 能力包装成受控、类型化、可清理的 `window.electronAPI`，让 renderer 只消费安全 API，而不直接接触 Node/Electron 底层能力。

## Q15: 本地 DB 首次迁移日志流程

**问题**

解释下面这段 localDb 日志流程：

```text
localDb.ensureReady.open exists:false
localDb.migrate.start
localDb.migrate.scan currentVersion:-1 pendingCount:30
localDb.migrate.backup
localDb.migrate.apply.begin seq:0
localDb.migrate.apply.ok seq:0
```

**回答**

这是一条“当前用户首次创建本地 SQLite DB，并开始从 0000 跑完整 schema migration”的启动链路。

整体入口是 renderer 登录后触发 `localDb.ensureReady(userId)`，main 进程根据 `userId` 计算数据库路径：

```text
/Users/gexianglin/Library/Application Support/xdt-maker/xdt-maker-{userId}.db
```

`localDb.ensureReady.open` 里 `exists:false` 表示打开前这个用户的 DB 文件还不存在。随后 `openWithPragmas(filePath)` 用 `better-sqlite3` 打开该路径；SQLite 会在打开时创建一个空 DB 文件，并设置 WAL、foreign_keys、synchronous、cache、busy_timeout 等 pragma。

`localDb.migrate.start` 表示进入 migration。日志里的 `drizzleDir` 指向开发态迁移目录 `apps/desktop/drizzle`，`isPackaged:false` 表示当前不是打包后的 App，而是 dev 运行。

`localDb.migrate.scan` 会先读取当前 schema version：

```sql
SELECT value FROM migration_meta WHERE key = 'schema_version'
```

因为这是全新 DB，`migration_meta` 表还不存在，所以代码捕获异常并返回 `currentVersion:-1`。接着扫描 `apps/desktop/drizzle` 下所有 `0000_*.sql` 形式的迁移文件，只要 `seq > -1` 都算 pending，所以出现 `pendingCount:30`，也就是 `0000` 到 `0029` 全部待执行。`hasTsScript:true` 表示这条 SQL 旁边还有同名 TS 脚本，需要在 SQL 后继续执行补数据或修数据逻辑。

`localDb.migrate.backup` 是执行迁移前的安全快照。虽然前面的 `exists:false` 说明打开前文件不存在，但 `better-sqlite3` 打开后已经创建了一个空 DB 文件，所以这里可以备份出一个 `.bak.{ISO时间}` 文件。这个备份用于迁移失败时回滚；如果文件真的还不存在，日志会显示 `<no-db-to-backup>`。

`localDb.migrate.apply.begin seq:0` 表示开始执行第一条迁移 `0000_init.sql`。每条迁移都放在 SQLite transaction 里执行：

1. 读取并执行 SQL。
2. 如果有同名 TS script，则 `require()` 后调用 `run(db)`。
3. 写入 `migration_meta.schema_version = 当前 seq`。
4. 如果 `migration_history` 表已经存在，则记录文件名和 hash。

`localDb.migrate.apply.ok seq:0 durationMs:0` 表示 `0000_init.sql` 成功完成，耗时小于 1ms。此时初始表结构和 `migration_meta` 已经建立，schema version 至少变成 `0`。后续日志会继续执行 `0001` 到 `0029`；全部成功后会记录 `localDb.migrate.done`，然后 `ensureReady` 继续完成本地 DB ready 流程。

一句话总结：这段日志不是异常，而是新用户第一次打开 App 时，main 进程创建本地 SQLite 文件、扫描全部迁移、先备份空库、再从 `0000_init.sql` 开始逐条初始化 schema 的正常流程。

## Q16: Drizzle 在项目里的实现方式

**问题**

Drizzle 怎么实现的详细介绍。

**回答**

这个项目里的 Drizzle 分成两部分理解：

1. `drizzle-orm`：运行时 ORM，负责把 TypeScript schema 变成类型化查询 API。
2. `drizzle-kit`：开发期工具，负责根据 `schema.ts` 生成 SQL migration 和 meta snapshot。

项目入口在 `apps/desktop/drizzle.config.ts`：

```ts
export default {
  schema: './src/main/localDb/schema.ts',
  out: './drizzle',
  dialect: 'sqlite',
} satisfies Config;
```

意思是：schema 定义在 `src/main/localDb/schema.ts`，生成的 SQL migration 放到 `apps/desktop/drizzle/`，数据库方言是 SQLite。

运行时的核心初始化在 `apps/desktop/src/main/localDb/index.ts`：

```ts
_db = openWithPragmas(filePath);
_drizzle = drizzle(_db!, { schema });
await runMigrations(_db!, filePath);
```

这里 `_db` 是 `better-sqlite3` 的原始连接，`_drizzle` 是 Drizzle 包装出来的类型化数据库对象。`drizzle(_db, { schema })` 不会自动建表，也不会自动迁移；它只是把已有连接和 `schema.ts` 绑定起来，让后续代码可以写：

```ts
await db.select().from(sessions).where(eq(sessions.id, id));
await db.insert(sessions).values(row);
await db.update(sessions).set(patch).where(eq(sessions.id, id));
```

真正执行 migration 的不是 Drizzle 官方运行时 migrate，而是项目自写的 `runMigrations()`。它会读取 `migration_meta.schema_version`，扫描 `apps/desktop/drizzle/NNNN_*.sql`，找出 pending migration，迁移前备份 DB，然后逐条在 transaction 里执行 SQL、可选 TS script、更新 schema version、写 migration history。

`schema.ts` 是 Drizzle 的类型源头，例如：

```ts
export const sessions = sqliteTable('sessions', {
  id: text('id').primaryKey(),
  title: text('title').notNull().default('New Maker'),
  createdAt: integer('created_at').notNull(),
});
```

这份定义同时服务两件事：

- 开发期：`pnpm db:generate` 通过 drizzle-kit 对比 schema 和上一次 snapshot，生成新的 SQL migration。
- 运行时：main 进程通过 `drizzle(_db, { schema })` 获得类型化查询能力。

因此本项目的完整链路是：

```mermaid
flowchart TD
    A["修改 schema.ts"] --> B["pnpm db:generate"]
    B --> C["生成 drizzle/NNNN_xxx.sql"]
    B --> D["更新 drizzle/meta snapshot"]
    C --> E["App 启动 ensureReady(userId)"]
    E --> F["openWithPragmas 打开 SQLite"]
    F --> G["drizzle(_db, { schema }) 创建类型化 DB"]
    G --> H["runMigrations 执行 pending SQL"]
    H --> I["IPC / maker / scheduler 使用 getDrizzle 查询"]
```

一句话总结：Drizzle 在这里不是“数据库引擎”，SQLite + better-sqlite3 才是底层引擎；Drizzle 是类型化 ORM 和 migration 生成工具，项目再用自写 migration runner 把生成出来的 SQL 安全地应用到每个用户自己的本地 DB 文件上。

## Q17: executeUpdateMacOS 更新实现

**问题**

`executeUpdateMacOS` 怎么实现更新的。

**回答**

macOS 更新不是用 Windows 的 `xdt-updater.exe`，而是在 main 进程里动态生成一个临时 shell 脚本，让这个脚本在旧 App 退出后替换整个 `.app` bundle。

完整链路是：

1. 启动时 renderer 调 `checkAppUpdate()`，preload 转到 main 的 `update-check-startup`。
2. main 拉取 `manifest-darwin-{arch}.json`，比较 `manifest.app.version` 和 `app.getVersion()`。
3. 如果有新版本，下载 `manifest.app.hotfix` 指向的 zip，并校验 SHA256。
4. 下载成功后写 `patch-info.json`，记录版本、文件名、sha256、是否需要重新登录。
5. renderer 进入 `update_done`，短暂显示“更新完成，等待自动重启...”，然后调用 `relaunchToUpdate()`。
6. main 的 `executeRelaunch()` 检查补丁文件、App Translocation、递增 `applyAttempts`，macOS 下调用 `executeUpdateMacOS(zipPath)`。

`executeUpdateMacOS(zipPath)` 里先推导当前 `.app` 路径：

```ts
const appPath = path.dirname(path.dirname(path.dirname(app.getAppPath())));
```

打包后 `app.getAppPath()` 通常在：

```text
xdt-maker.app/Contents/Resources/app.asar
```

向上三层就是：

```text
xdt-maker.app
```

然后准备几个路径：

- `extractDir`：`/tmp/xdt-maker-update-{timestamp}`，用于解压新包。
- `scriptPath`：`/tmp/xdt-maker-update-{timestamp}.sh`，临时更新脚本。
- `lockFilePath`：`<userData>/updates/.updating`，更新锁。
- `logPath`：`<userData>/logs/maker-update.log`，更新日志。
- `pid`：当前旧 App 进程 PID。

核心做法是写出一个 bash 脚本，然后 detached 启动它：

```ts
fs.writeFileSync(scriptPath, script, { mode: 0o755 });
spawn('/bin/bash', [scriptPath], { detached: true, stdio: 'ignore' }).unref();
forceQuit();
```

脚本执行流程：

1. 等旧进程退出：

```bash
while kill -0 ${pid} 2>/dev/null; do sleep 1; done
sleep 2
```

因为正在运行的 `.app` 不能安全地边跑边替换，所以必须等 Electron main 进程真正退出。

2. 写更新锁：

```bash
echo updating > "${lockFilePath}"
```

新 App 如果在替换中途被启动，`bootstrap-electron.ts` 会在很早阶段检查这个 `.updating` 文件，最多等 30 秒，避免读到半替换状态。

3. 解压 hotfix zip：

```bash
/usr/bin/ditto -x -k "${zipPath}" "${extractDir}"
```

macOS 用 `ditto` 解 zip，比较贴合 `.app` bundle、权限和资源文件处理。

4. 找新 `.app`：

```bash
NEW_APP=$(find "${extractDir}" -name "*.app" -maxdepth 1 | head -1)
```

如果 zip 根目录下没有 `.app`，直接退出，旧 App 不会被删除。

5. 替换旧 App：

```bash
rm -rf "${appPath}"
mv "$NEW_APP" "${appPath}"
```

这里是整包替换，不是增量 patch。先删旧 `.app`，再把解压出来的新 `.app` 移到原路径。

6. 清理扩展属性：

```bash
/usr/bin/xattr -cr "${appPath}"
```

用于清掉 quarantine 等扩展属性，降低 Gatekeeper / 隔离标记影响启动的概率。

7. 移除更新锁，重新打开 App：

```bash
rm -f "${lockFilePath}"
open "${appPath}"
```

8. 启动后做一次验证：

```bash
pgrep -f "${appPath}/Contents/MacOS/"
```

如果 3 秒后能找到新进程，就写 `LAUNCH VERIFIED`；否则写 `LAUNCH FAILED`，方便查 `maker-update.log`。

9. 清理临时文件：

```bash
rm -rf "${extractDir}"
rm -f "${zipPath}"
rm -f "${scriptPath}"
```

一句话总结：`executeUpdateMacOS` 的策略是“下载整包 `.app` zip → 写临时 bash 脚本 → 旧进程退出 → ditto 解压 → 删除旧 `.app` → 移入新 `.app` → xattr 清理 → open 新 App”。它没有使用 Windows 的 Rust/Tauri `xdt-updater.exe`，macOS 侧主要依赖系统 shell、`ditto`、`mv` 和 `open` 完成替换。

## Q18: EnvCheckContext.tsx 职责与 hooks 用法

**问题**

介绍一下 `EnvCheckContext.tsx` 干嘛，`useCallback`、`useContext`、`useEffect`、`useMemo`、`useRef`、`useState` 为什么要用这些 hook。

**回答**

`EnvCheckContext.tsx` 是启动阶段的环境检查状态中心。它把两件事统一成一个全局状态机：

1. Phase 1：App hot-update 检查，负责拉 manifest、下载补丁、进入等待重启。
2. Phase 2：运行环境检查，负责检查 Claude/Codex 等二进制是否可用，必要时下载。

它导出三块能力：

- `EnvCheckProvider`：放在 App 外层，启动时自动跑检查，并保存状态、进度、结果。
- `EnvCheckGuard`：检查没通过前不渲染主界面，避免 maker IPC / 二进制环境还没准备好时提前进入业务页。
- `useEnvCheck()`：给 `SplashScreen`、`useSplash` 等组件读取状态和触发重试。

这些 hook 的用途：

- `useState`：保存会影响 UI 的响应式状态，例如 `status`、`downloadProgress`、`downloadInfo`、`updateVersion`、`step`、`result`。状态变化后 React 会重新渲染启动页。
- `useRef`：保存不需要触发渲染、但异步流程必须读到的可变值。例如 `suppressUpdateProgressRef` 用来临时屏蔽 App 更新进度，`callIdRef` 用来防止旧 retry promise 覆盖新流程，`statusRef` 让延迟 tick 能读到最新状态。
- `useEffect`：处理副作用。这里用于订阅 main 进程推送的下载进度、组件卸载时取消订阅、自动启动环境检查、同步 `statusRef.current = status`。
- `useCallback`：稳定 `checkEnvironment` 函数引用。因为它会被 `useEffect` 和子组件重试按钮依赖，如果每次 render 都创建新函数，自动检查 effect 容易被重复触发。
- `useMemo`：稳定 Provider 的 `value` 对象。只有 `status`、`result`、进度等依赖真的变化时才生成新 context value，减少 context consumer 的无意义刷新。
- `useContext`：让下层组件通过 `useEnvCheck()` 读取 Provider 里的全局启动状态，不需要层层 props 传递。

核心时序是：

```mermaid
flowchart TD
    A["EnvCheckProvider mount"] --> B["checkEnvironment()"]
    B --> C["Phase 1: checkAppUpdate() 后台开始"]
    B --> D["Phase 2: checkEnvironment() 检查二进制"]
    D -->|失败| E["status=failed"]
    D -->|通过| F["等待 Phase 1 结果或 3s grace"]
    C -->|下载补丁| G["status=updating"]
    C -->|补丁 ready| H["status=update_done"]
    F -->|无更新/超时| I["status=passed"]
    H --> J["Splash 自动 relaunchToUpdate"]
    I --> K["EnvCheckGuard 渲染主界面"]
```

一句话总结：`EnvCheckContext.tsx` 是启动页背后的“检查调度器 + 状态仓库”。它用 `useState` 驱动 UI，用 `useRef` 处理异步并发边界，用 `useEffect` 接 IPC 和启动副作用，用 `useCallback/useMemo` 稳定引用和减少重复触发，用 `useContext` 把状态提供给启动页和守卫组件。

## Q19: EnvCheck Phase 1 和 Phase 2 的关系

**问题**

Phase 1 和 Phase 2 是什么关系，顺序是什么。

**回答**

在 `EnvCheckContext.tsx` 里，Phase 1 和 Phase 2 是并行启动、前后台分工的关系。

Phase 1 是 App hot-update 检查：

- 调 `window.electronAPI.checkAppUpdate()`
- main 进程拉 manifest
- 判断是否有 App 新版本
- 有新版本就下载 hotfix zip
- 下载完成后返回 `{ hasUpdate: true, action: 'relaunch' }`
- 最后 renderer 进入 `update_done`，启动页触发自动重启更新

Phase 2 是运行环境检查：

- 调 `window.electronAPI.checkEnvironment()`
- 检查 Claude/Codex 等二进制
- 必要时下载 agent binary
- 通过后才允许进入主界面

真实顺序是：

```ts
const updatePromise = window.electronAPI.checkAppUpdate();

suppressUpdateProgressRef.current = true;
setStatus('checking');
const res = await window.electronAPI.checkEnvironment();
```

也就是说，Phase 1 先被启动成一个后台 Promise，但不会立刻 await；Phase 2 马上开始并作为前台启动流程驱动 splash。

Phase 2 检查期间，会用 `suppressUpdateProgressRef.current = true` 暂时屏蔽 Phase 1 的下载进度，避免 App 更新进度把启动页状态从 `checking/downloading` 抢成 `updating`，造成 UI 跳变。

Phase 2 结束后：

1. 如果 Phase 2 失败，直接 `status = failed`，启动页显示环境失败。
2. 如果 Phase 2 成功，会等待 Phase 1 的结果。
3. 如果 Phase 1 很快返回并发现更新已下载好，则 `status = update_done`，进入自动重启。
4. 如果 Phase 1 没有更新、失败、或者 3 秒 grace 到了还没明确进入下载，则 `status = passed`，主界面放行。
5. 如果 3 秒时 Phase 1 已经在 `updating`，不会强行放行，而是继续等下载完成，避免补丁下载完后不触发重启。

流程图：

```mermaid
flowchart TD
    A["checkEnvironment() 被调用"] --> B["启动 Phase 1: checkAppUpdate() 后台 Promise"]
    B --> C["立即启动 Phase 2: checkEnvironment()"]
    C -->|失败| D["status=failed"]
    C -->|成功| E["等待 Phase 1 或 3s grace"]
    B -->|正在下载 App 补丁| F["status=updating"]
    B -->|补丁 ready| G["Phase 1 返回 relaunch"]
    E -->|Phase 1 ready| H["status=update_done"]
    E -->|无更新/失败/超时| I["status=passed"]
    F -->|下载中 grace 不放行| E
    H --> J["Splash 自动 relaunchToUpdate"]
    I --> K["EnvCheckGuard 放行主界面"]
```

一句话总结：Phase 1 检查 App 更新，Phase 2 检查运行环境；两者并行启动，但 Phase 2 是前台主流程，Phase 1 是后台流程，只有当 Phase 1 真的进入下载或准备好重启时，才会拦住启动页并接管后续更新重启。

## Q20: safeResolve 与 updateResult 流程

**问题**

`safeResolve`、`updateResult`、`UpdateResult` 这些是怎么个流程。

**回答**

这段代码是在 Phase 2 通过后，等待 Phase 1 的 App 更新检查结果，但不会无限等待。

`updatePromise` 是 Phase 1：

```ts
const updatePromise = window.electronAPI.checkAppUpdate();
```

它最终可能返回：

```ts
{ hasUpdate: true, action: 'relaunch', version: '...' }
{ hasUpdate: false, action: 'none' }
null
```

`type UpdateResult = Awaited<typeof updatePromise>` 是 TypeScript 类型推导，意思是取出 `updatePromise` resolve 之后的结果类型。

`updateResult` 是最终等待出来的结果：

```ts
const updateResult = await new Promise<UpdateResult>((resolve) => {
  ...
});
```

这个新 Promise 有两个可能的完成来源：

1. `updatePromise` 自己先返回，则用它的真实结果。
2. 3 秒 grace 到了，而 Phase 1 没有进入 `updating/update_done`，则用 `null` 放行。

`safeResolve` 是一个“只允许 resolve 一次”的保护函数：

```ts
let settled = false;
const safeResolve = (v: UpdateResult) => {
  if (settled) return;
  settled = true;
  resolve(v);
};
```

因为这里同时有两条异步路径可能调用 resolve：

- `updatePromise.then(safeResolve)`
- `setTimeout(tick, UPDATE_GRACE_MS)` 里的 `safeResolve(null)`

`safeResolve` 保证谁先到谁生效，后到的直接忽略。

后续逻辑：

```ts
if (updateResult?.hasUpdate && updateResult?.action === 'relaunch') {
  setStatus('update_done');
  setUpdateVersion(updateResult.version);
  return;
}

setStatus('passed');
```

如果 Phase 1 明确告诉 renderer 有更新且准备重启，就进入 `update_done`；否则进入 `passed`，主界面放行。

一句话总结：`UpdateResult` 是结果类型，`safeResolve` 是只结算一次的保护器，`updateResult` 是“Phase 1 真实结果或 grace 超时后的 null”，最终决定启动页是进入更新重启还是放行主界面。

## Q21: window.electronAPI.checkEnvironment 调用链

**问题**

`window.electronAPI.checkEnvironment();` 调用链。

**回答**

这条链路从 renderer 出发，经过 preload 桥，进入 main 进程的 `check-environment` IPC handler，最终准备 Claude Code 和 Codex 两个 agent 二进制。

第一层：renderer 调用。

```ts
const res = await window.electronAPI.checkEnvironment();
```

调用位置在 `EnvCheckContext.tsx` 的 Phase 2。它负责检查 agent 运行环境，结果会写入 `result`，并通过 `res.allPassed` 决定启动页是否能继续。

第二层：preload 暴露安全 API。

```ts
checkEnvironment: () => ipcRenderer.invoke('check-environment')
```

renderer 不直接接触 `ipcRenderer`，而是通过 `window.electronAPI` 调用 preload 暴露的白名单方法。

第三层：main 进程 IPC handler。

```ts
ipcMain.handle('check-environment', async () => {
  ...
});
```

main 里做的事情是：

1. 读取当前平台 `process.platform`。
2. `binaryPeekNeedsDownload('claude-code')` 和 `binaryPeekNeedsDownload('codex')` 先判断两个 vendor 是否都需要下载，用来决定 splash 是否显示 `(1/2)`、`(2/2)`。
3. 先执行 `binaryPrepare('claude-code')`。
4. Claude 成功后再执行 `binaryPrepare('codex')`。
5. 如果两个都 ready，调用 `registerMakerIpcsAfterSplash()` 注册 maker IPC。
6. 返回 `{ claudeCode, codex, allPassed, platform }`。

第四层：agent-binaries 统一准备逻辑。

`binaryPrepare` 来自 `apps/desktop/src/main/agent-binaries/index.ts`，它按 kind 分派：

- `claude-code` 对应 `apps/claude-code-bin/<platform>/claude`
- `codex` 对应 `apps/codex-bin/<platform>/codex`

dev 模式下，直接走 `findDevBinary()` 查本地 LFS 二进制，命中就返回 ready，不走 manifest / SHA 校验。

prod 模式下，走 `createBinaryProvisioner().prepare()`：

1. 拉 manifest。
2. 找对应 vendor asset。
3. 检查 `<userData>/<installSubdir>/<version>/<binary>` 和 `.verified` 是否已安装。
4. 未安装则下载 `.gz`。
5. downloader 校验 size 和 SHA256。
6. 解压到最终 binary。
7. chmod 0o755。
8. 写 `.verified`。
9. 清理旧版本。
10. 广播 `binary-download-progress` 给 renderer 更新 splash 进度条。

第五层：成功后注册业务 IPC。

两个二进制都准备好后，`registerMakerIpcsAfterSplash()` 才会执行。这里会注册：

- maker core IPC
- title/help/plan/rewind/fork IPC
- auth/status/usage/binary version IPC
- file browser / search / gitlab / mivo 等 IPC
- 恢复 worktree pool
- 尝试启动 scheduler

这是因为 `getMakerCore()` 构造时需要同步读取已经准备好的 Claude/Codex binary path；如果太早注册 maker IPC，会拿不到路径。

完整流程：

```mermaid
flowchart TD
    A["EnvCheckContext: window.electronAPI.checkEnvironment()"] --> B["preload: ipcRenderer.invoke('check-environment')"]
    B --> C["main: ipcMain.handle('check-environment')"]
    C --> D["peek claude/codex 是否需要下载"]
    D --> E["binaryPrepare('claude-code')"]
    E -->|失败| F["返回 allPassed=false, codex skipped"]
    E -->|成功| G["binaryPrepare('codex')"]
    G -->|失败| H["返回 allPassed=false"]
    G -->|成功| I["registerMakerIpcsAfterSplash()"]
    I --> J["返回 allPassed=true"]
    J --> K["EnvCheckContext setResult + phase2Passed=true"]
```

一句话总结：`window.electronAPI.checkEnvironment()` 是启动 Phase 2 的核心 IPC，它不是只做前端检查，而是让 main 进程把 Claude Code 和 Codex 两个 CLI 准备好，并在准备成功后注册 maker 相关能力，最后返回给 renderer 决定能不能进入主界面。

## Q22: docs/design_docs/app-init.pen 生成与使用

**问题**

`docs/design_docs/app-init.pen` 这个文件是怎么生成，怎么使用的。

**回答**

`app-init.pen` 是 Pencil 设计稿文件，本质是一个 JSON 格式的设计文档，不是运行时代码。它由 Pencil 设计工具 / pencil MCP 生成或编辑，保存到仓库后给开发实现 UI 时做 1:1 对照。

当前文件顶层有 4 个 frame：

- `Splash Page - Light`
- `Splash Page - Dark`
- `Splash Page - Downloading - Light`
- `Splash Page - Downloading - Dark`

每个 frame 都是 1280×800 的启动页设计，包含：

- `Toolbar`：46px 高，右侧窗口控制按钮。
- `Body`：居中主体。
- `Logo Wrapper`：490×490。
- `Logo`：464×464，引用 `./res/logo.png`。
- `Loading Area`：提示文字和下载状态。
- 下载态额外包含进度条和速度 / 大小信息。

使用方式不是“程序读取 `.pen` 渲染 UI”，而是开发者按 `.pen` 里的尺寸、层级、颜色 token、文案和布局去实现 React/Tailwind 组件。对应实现主要是：

- `apps/desktop/src/renderer/components/splash/SplashScreen.tsx`
- `apps/desktop/src/renderer/hooks/useSplash.ts`
- `apps/desktop/src/renderer/contexts/EnvCheckContext.tsx`

例如 `SplashScreen.tsx` 里能看到和设计稿对应的尺寸：

- toolbar 高度：`h-[46px]`
- logo wrapper：`h-[490px] w-[490px]`
- logo：`h-[464px] w-[464px] rounded-[12px]`
- tip 文本：`text-[14px] leading-[1.43]`
- progress bar：`w-[192px] h-[4px] rounded-[2px]`

仓库规则要求：前端 UI 新增或修改前要读 `DESIGN.md`；非 trivial UI 变化建议先用 pencil MCP 画 `.pen` 设计稿，再按设计稿 1:1 还原。也就是说 `.pen` 是设计源文件和验收参考，不是构建产物。

需要注意：当前 `ApiKeyRequiredView.tsx` / `ApiKeyRequiredGate.tsx` 注释里写了参考 `app-init.pen` 的 `Api Key Required - Light/Dark` frame，但当前 `app-init.pen` 实际只包含 Splash 相关 4 个 frame，没有搜到 `Api Key Required`。这可能是历史设计稿被覆盖、拆分或注释没有同步。

一句话总结：`docs/design_docs/app-init.pen` 是 Pencil 生成的启动页设计稿，开发时用它对齐 Splash 页的布局、尺寸、颜色和状态；实际运行时由 React 组件实现，应用不会动态读取 `.pen`。

## Q23: Pencil 如何写出 .pen 设计稿

**问题**

Pencil 怎么写出 `.pen` 文件设计稿。

**回答**

`.pen` 文件本质是一份 JSON 设计稿。Pencil 会把画布、frame、文本、图标、图片、布局属性、颜色 token 等信息序列化成 JSON，然后保存成 `.pen` 文件。

典型结构如下：

```json
{
  "version": "2.10",
  "children": [
    {
      "type": "frame",
      "id": "abc12",
      "name": "Splash Page - Light",
      "x": 0,
      "y": 0,
      "width": 1280,
      "height": 800,
      "fill": "$color-bg-warm",
      "layout": "vertical",
      "children": []
    }
  ]
}
```

也就是说，一个 `.pen` 通常由这些部分组成：

- `version`：Pencil 文件格式版本。
- `children`：画布上的顶层对象。
- `frame`：类似 Figma frame / React 容器。
- `text`：文本节点。
- `icon_font`：图标节点，例如 lucide 图标。
- `fill`：背景色、文本色，可能是 hex，也可能是 token。
- `layout`、`gap`、`padding`、`alignItems`：布局规则。
- `theme`：light / dark 主题标记。
- `image fill`：引用相对路径资源，例如 `./res/logo.png`。

生成方式通常有两种：

1. 用 Pencil 设计工具或 pencil MCP 可视化创建 frame，最后保存为 `.pen`。
2. 由 agent / 脚本直接按 JSON 结构生成 `.pen` 文件。

人工也可以直接编辑 `.pen`，因为它是 JSON，但一般只适合小修，比如改文案、改尺寸、改颜色 token。复杂设计最好用 Pencil 工具生成，避免 id、层级、布局字段写错。

在这个项目里，`.pen` 文件主要放在：

```text
docs/design_docs/
```

使用流程是：

```text
先读 DESIGN.md
用 Pencil 画 .pen 设计稿
保存到 docs/design_docs/
实现 React/Tailwind 时按 .pen 里的 frame 1:1 还原
代码注释标明参考哪个 .pen / 哪个 frame
```

一句话总结：Pencil 写 `.pen` 的方式就是把设计画布保存成 JSON；开发时 `.pen` 是设计源文件和视觉验收参考，不是运行时代码。

## Q24: 请求使用 Pencil MCP 生成 settings 设计稿

**问题**

用 pencil MCP 基于 `DESIGN.md` 生成 settings 页面设计稿，输出 `docs/design_docs/settings-xxx.pen`，并记录这条到 `/Users/gexianglin/self/obsidian/obsidian/Work/AI/Heartopia.md`。

**回答**

当前 Codex 会话没有可用的 Pencil MCP 工具。已检查 MCP resources / templates，结果为空；当前工具列表也没有 `pencil` 相关 callable tool。因此不能真实调用 Pencil MCP 生成 `.pen` 文件。

如果后续接入 Pencil MCP，可以按这个目标执行：

```text
读取 DESIGN.md
基于项目黑白反色视觉规范绘制 Settings 页面
保存为 docs/design_docs/settings-xxx.pen
```

在当前环境下，不应声称已用 Pencil MCP 生成设计稿；如需继续，只能选择手写 Pencil-compatible JSON 作为降级方案，但那不等同于调用 Pencil MCP。

## Q25: 项目如何接入 Feishu

**问题**

这个项目是怎么接入 Feishu 的。

**回答**

项目里 Feishu 接入分三条线：

1. 登录身份线：desktop 通过 Feishu OAuth 登录，server 用 Feishu code 换用户 token，拿 open_id 后创建 / 更新 xdt 用户，再签发 xdt 自己的 JWT。
2. 飞书业务工具线：desktop main 持有 Feishu user token，通过 `lizi-mcps` 提供飞书文档、wiki、多维表格、消息等 MCP 工具给 agent。
3. 飞书 Bot / IM 线：用户在 Settings 里配置自己的飞书 bot appId/appSecret，desktop 通过 `lizi-im` 的 WebSocket 接入飞书私聊，让飞书消息能驱动本地 agent 会话。

登录链路：

```text
renderer authService.login()
→ preload authLogin()
→ main authManager.login()
→ 打开 Feishu OAuth BrowserWindow
→ Feishu redirect 到 /api/auth/callback，拿 code
→ desktop main 调 server /api/auth/login
→ server exchangeCodeForToken()
→ server getFeishuUserInfo()
→ server upsert user + 签发 xdt JWT/refreshToken
→ desktop 保存 xdt refresh_token 和 Feishu refresh_token
```

Feishu MCP 链路：

```text
authManager 登录成功
→ getFeishuService().token.storeFeishuToken()
→ Feishu refresh_token 加密保存到 safeStorage
→ getFeishuService().token.setJwt(xdtAccessToken)
→ agent 需要飞书工具
→ lizi-mcps ensureToken()
→ 必要时调 server /api/auth/refresh-feishu
→ server refreshFeishuToken()
→ 使用 lark SDK 调飞书 API
```

Feishu Bot 链路：

```text
Settings 保存 bot appId/appSecret
→ feishuBot:save IPC
→ lizi-im storage.writeCredentials()
→ wsClient.start()
→ Lark.WSClient 连接飞书事件流
→ 收到 p2p 消息
→ TOFU 绑定第一个 sender 为 owner
→ wireFeishuOrchestrator()
→ 创建/接管本地 session
→ runAgentTurn()
→ 回复飞书文本/卡片/文件
```
```mermaid
flowchart TD
    A["Feishu OAuth 登录"] --> B["server 换 user_access_token"]
    B --> C["server 获取 user_info/open_id"]
    C --> D["server upsert xdt user + 签发 JWT"]
    D --> E["desktop 保存 xdt refresh token"]
    D --> F["desktop 保存 Feishu refresh token"]

    F --> G["lizi-mcps Feishu service"]
    G --> H["agent 使用 lizi_feishu MCP 工具"]

    I["Settings 配置 Bot appId/appSecret"] --> J["lizi-im FeishuIM"]
    J --> K["Lark.WSClient 连接飞书事件流"]
    K --> L["收到 p2p 消息"]
    L --> M["TOFU 绑定 owner"]
    M --> N["创建/接管本地 session"]
    N --> O["runAgentTurn"]
    O --> P["回复飞书文本/卡片/文件"]
```
关键区别：

- xdt 登录用的是项目服务端配置的 Feishu App。
- Feishu Bot 用的是用户在 Settings 里配置的自己的 Feishu App。
- 飞书 open_id 是按 app 隔离的，所以登录得到的 `feishuId` 不能直接当 bot 白名单；bot owner 通过第一次私聊 TOFU 绑定。

一句话总结：这个项目把 Feishu 同时作为登录身份源、agent MCP 工具源、以及 IM Bot 入口；登录走 xdt-server OAuth，MCP 走用户 Feishu token，Bot 走用户自配 Feishu App 的 WebSocket。

## Q26: /api/auth/login 路径链路

**问题**

`/api/auth/login` 路径是怎样的。

**回答**

`/api/auth/login` 是 server 侧登录接口。完整路径来自：

```ts
app.use('/api/auth', authRouter);
router.post('/login', ...)
```

所以最终 URL 是：

```text
POST /api/auth/login
```

desktop main 在 Feishu OAuth 窗口里拿到 `code` 后调用这个接口，请求体包含：

```json
{
  "code": "Feishu OAuth 返回的授权码",
  "codeVerifier": "PKCE verifier",
  "deviceId": "本机设备 id"
}
```

route handler 做三件事：

1. 校验 `code` 必须是字符串。
2. 校验 `codeVerifier` 必须是字符串。
3. 校验 `deviceId` 必须是字符串。

校验通过后调用：

```ts
authService.login(code, codeVerifier, deviceId)
```
```mermaid
flowchart TD
    A["POST /api/auth/login"] --> B["校验 code / codeVerifier / deviceId"]
    B --> C["exchangeCodeForToken(code)"]
    C --> D["Feishu code 换 user access_token"]
    D --> E["校验 granted scopes"]
    E --> F["getFeishuUserInfo(accessToken)"]
    F --> G["拿 open_id / name / avatar / email"]
    G --> H["prisma.user.upsert by feishuId"]
    H --> I["signAccessToken 生成 xdt JWT"]
    I --> J["generateRefreshToken + hashToken"]
    J --> K["upsert refreshToken by userId + deviceId"]
    K --> L["buildMigrationStatus"]
    L --> M["deptSync.syncForUser 非阻塞同步部门"]
    M --> N["返回 xdt token + user + Feishu token"]
```
`authService.login()` 里主流程是：

1. `exchangeCodeForToken(code, codeVerifier)`：用 Feishu OAuth code 换 Feishu user access_token / refresh_token。
2. 校验 Feishu 返回的 scope，若返回 scope 且缺关键权限则报 `feishuScopeIncomplete`。
3. `getFeishuUserInfo(feishuToken.accessToken)`：用 Feishu user token 拉用户 open_id、姓名、头像、邮箱。
4. `prisma.user.upsert({ where: { feishuId: userInfo.openId } })`：按 Feishu open_id 创建或更新 xdt 用户。
5. `signAccessToken({ sub: user.id, device: deviceId })`：签发 xdt server 自己的 JWT accessToken。
6. `generateRefreshToken()` + `hashToken()`：生成 xdt refresh token，只把 hash 存数据库。
7. `prisma.refreshToken.upsert({ userId, deviceId })`：同一用户同一设备只保留一条 refresh token。
8. `buildMigrationStatus(user.id, deviceId)`：返回本地数据迁移状态。
9. `deptSync.syncForUser(user.id, feishuToken.accessToken)`：用 Feishu user token 同步部门，失败不阻断登录。
10. 返回 xdt token、用户信息、Feishu token、scope、migration。

返回给 desktop 的结构大致是：

```json
{
  "accessToken": "xdt JWT",
  "refreshToken": "xdt refresh token",
  "user": {
    "id": "...",
    "name": "...",
    "avatar": "...",
    "email": "...",
    "defaultModel": "...",
    "defaultEffort": "...",
    "isCanary": false,
    "feishuId": "Feishu open_id",
    "role": "user"
  },
  "feishuAccessToken": "Feishu user access_token",
  "feishuRefreshToken": "Feishu user refresh_token",
  "feishuExpiresIn": 7200,
  "grantedScopes": [],
  "migration": { "status": "none" }
}
```

一句话总结：`/api/auth/login` 把 Feishu OAuth code 转成 Feishu 用户身份，再把这个身份映射成 xdt-maker 用户，同时签发 xdt 自己的登录 token，并把 Feishu token 一并返回给 desktop main 保存和后续 MCP 使用。

## Q27: req.body as Record<string, unknown> 解构

**问题**

`const { code, codeVerifier, deviceId } = req.body as Record<string, unknown>;` 怎么理解。

**回答**

这句代码先把 Express 的 `req.body` 视为一个“字符串 key、未知 value”的对象，再从里面解构出 `code`、`codeVerifier`、`deviceId` 三个字段。

```ts
req.body as Record<string, unknown>
```

表示：

```ts
{
  [key: string]: unknown
}
```

因为 HTTP 请求体来自外部输入，不可信，所以 value 用 `unknown`。这句本身不保证字段类型，只是把字段取出来；真正的安全校验在后面：

```ts
if (!code || typeof code !== 'string') ...
if (!codeVerifier || typeof codeVerifier !== 'string') ...
if (!deviceId || typeof deviceId !== 'string') ...
```

一句话总结：这是从请求体中取登录参数的写法，先按未知类型读取，再通过运行时校验确认它们确实是字符串。

## Q28: prisma.user.upsert 按 Feishu 用户创建或更新

**问题**

详细解释：

```ts
const user = await prisma.user.upsert({
  where: { feishuId: userInfo.openId },
  create: {
    feishuId: userInfo.openId,
    name: userInfo.name,
    avatarUrl: userInfo.avatarUrl,
    email: userInfo.email,
    defaultModel: DEFAULT_PREFERENCES.defaultModel,
    defaultEffort: DEFAULT_PREFERENCES.defaultEffort,
  },
  update: {
    name: userInfo.name,
    avatarUrl: userInfo.avatarUrl,
    email: userInfo.email,
  },
});
```

**回答**

`upsert` 是 Prisma 的“有则更新、无则创建”。这里用 Feishu 的 `open_id` 作为 xdt 用户身份的唯一映射键：

```ts
where: { feishuId: userInfo.openId }
```

如果数据库里还没有这个 Feishu 用户，就走 `create` 分支，创建新 xdt 用户，并写入 Feishu 身份、姓名、头像、邮箱和默认偏好。

如果数据库里已经有这个用户，就走 `update` 分支，只同步姓名、头像、邮箱这些会随 Feishu 资料变化的字段。

`update` 分支故意不更新：

```ts
defaultModel
defaultEffort
```

因为这些是用户在 xdt-maker 里的个人偏好。如果每次登录都用默认值覆盖，会把用户已经改过的模型和 effort 重置掉。

返回值 `user` 是创建或更新后的数据库用户记录，后面会用它：

- 签发 xdt JWT：`sub = user.id`
- 创建 / 更新 refresh token
- 返回给 desktop renderer 显示用户信息
- 作为本地 DB 文件名中的 userId 来源

一句话总结：这段代码把 Feishu 用户映射成 xdt-maker 用户；第一次登录创建账号，后续登录只刷新 Feishu 资料，不覆盖用户自己的 xdt 偏好设置。

## Q29: prisma.ts 是什么

**问题**

`prisma.ts` 是什么

**回答**

`apps/server/src/lib/prisma.ts` 是服务端统一创建和导出 Prisma Client 的入口文件。

它做三件事：

1. 从 `@prisma/client` 引入 `PrismaClient`，用于连接 PostgreSQL 并操作 Prisma schema 里的模型。
2. 根据 `LOG_PRISMA_QUERY` 决定是否打印 SQL query 日志。
3. 在开发环境把 Prisma Client 缓存在 `globalThis.__prisma` 上，避免热重载时反复创建数据库连接。

核心代码：

```ts
export const prisma =
  globalForPrisma.__prisma ??
  new PrismaClient({
    log: logs,
  });
```

意思是：如果全局已经有一个 Prisma Client，就复用；否则新建一个。

后续服务端代码都通过它访问数据库，例如：

```ts
import { prisma } from '../lib/prisma.js';

await prisma.user.upsert(...);
await prisma.refreshToken.create(...);
```

一句话总结：`prisma.ts` 是 server 层的数据库访问单例入口，负责统一创建、复用并配置 Prisma Client。

## Q30: globalForPrisma 类型断言

**问题**

解释：

```ts
const globalForPrisma = globalThis as unknown as { __prisma?: PrismaClient };
```

**回答**

这句代码是在告诉 TypeScript：把 JS 的全局对象 `globalThis` 临时看成一个带有可选 `__prisma` 字段的对象。

`globalThis` 是 JavaScript 的全局对象。在 Node.js 里可以理解成全局运行时对象，模块之间可以通过它共享少量全局状态。

项目想在开发环境缓存 Prisma Client：

```ts
globalForPrisma.__prisma = prisma;
```

但 TypeScript 默认并不知道 `globalThis` 上会有一个自定义字段 `__prisma`，所以直接写：

```ts
globalThis.__prisma
```

会报类型错误。

于是代码通过类型断言把它转换成：

```ts
{ __prisma?: PrismaClient }
```

其中 `__prisma?` 的问号表示这个字段可有可无。第一次启动时通常没有，创建 Prisma Client 后才会写进去。

中间的 `as unknown as ...` 是双重断言。意思是先把 `globalThis` 转成 `unknown`，再转成目标类型。它比直接断言更“强制”，常用于两个类型结构差异较大、TypeScript 不允许直接转换的场景。

一句话总结：这句是为了让 TypeScript 允许在 `globalThis` 上读写 `__prisma`，从而在开发热重载时复用同一个 Prisma Client，避免重复创建数据库连接。

## Q31: refreshToken.upsert 按用户设备刷新登录态

**问题**

详细介绍：

```ts
await prisma.refreshToken.upsert({
  where: { userId_deviceId: { userId: user.id, deviceId } },
  create: { tokenHash, deviceId, userId: user.id, expiresAt },
  update: { tokenHash, expiresAt, createdAt: new Date() },
});
```

**回答**

这段代码是在登录成功后为“当前用户 + 当前设备”保存 refresh token。

`RefreshToken` 表里有一个复合唯一约束：

```prisma
@@unique([userId, deviceId])
```

所以 Prisma 会生成一个复合唯一查询字段：

```ts
userId_deviceId
```

它表示用 `(userId, deviceId)` 这一组值来唯一定位一条 refresh token 记录。

如果这个用户的这个设备第一次登录，数据库里还没有对应记录，就走 `create`：

```ts
create: { tokenHash, deviceId, userId: user.id, expiresAt }
```

也就是新建一条 refresh token 记录。

如果这个用户的这个设备之前已经登录过，就走 `update`：

```ts
update: { tokenHash, expiresAt, createdAt: new Date() }
```

也就是把旧 refresh token 替换成新的 hash，同时刷新过期时间，并把 `createdAt` 重置为当前时间。

这里保存的是 `tokenHash`，不是原始 refresh token。原始 token 只返回给客户端；服务端数据库只存 hash。之后客户端拿 refresh token 来续期时，服务端会重新 hash 后和数据库里的 `tokenHash` 比较，这样即使数据库泄漏，也不会直接暴露可用 token。

一句话总结：这段代码确保每个用户的每台设备只保留一条最新 refresh token；重复登录会轮换 token，而不是无限新增记录。

## Q32: SignJWT 签发 access token 流程

**问题**

怎么理解：

```ts
return new SignJWT({ device: payload.device })
  .setProtectedHeader({ alg: 'HS256' })
  .setSubject(payload.sub)
  .setIssuedAt()
  .setExpirationTime(config.jwt.accessExpiresIn)
  .sign(config.jwt.secret);
```

**回答**

这段代码使用 `jose` 库签发一个 JWT access token。

`new SignJWT({ device: payload.device })` 创建 JWT payload，其中自定义字段 `device` 表示这枚 token 属于哪个设备。

`.setProtectedHeader({ alg: 'HS256' })` 设置 JWT header，声明签名算法是 `HS256`，即用同一个密钥做 HMAC-SHA256 签名和校验。

`.setSubject(payload.sub)` 设置标准 JWT 字段 `sub`，这里通常是用户 id，表示 token 的主体是谁。

`.setIssuedAt()` 设置标准字段 `iat`，表示 token 签发时间。

`.setExpirationTime(config.jwt.accessExpiresIn)` 设置标准字段 `exp`，表示 access token 过期时间。项目里默认来自 `JWT_ACCESS_EXPIRES`，没有配置时是 `1h`。

`.sign(config.jwt.secret)` 使用服务端 JWT_SECRET 解码后的字节密钥完成签名，并返回最终 token 字符串。

签出来的 JWT 大致包含三部分：

```text
header.payload.signature
```

服务端后续用 `verifyAccessToken()` 通过同一个 secret 和 `HS256` 算法校验它，校验通过后取出：

```ts
sub
device
```

一句话总结：这段代码把“用户 id + 设备 id + 签发时间 + 过期时间”打包成一个带签名的短期 access token，客户端后续请求用它证明自己的登录身份。

## Q33: 该项目用了哪些数据库

**问题**

该项目用了那些数据库

**回答**

xdt-maker 这个 monorepo 在不同层使用了多种数据库 / 持久化方案：

### 1. 服务端

- **`apps/server` — PostgreSQL + Prisma**
  - 驱动：`@prisma/client` v6.19.3 + Prisma ORM
  - 用途：用户认证（User / RefreshToken）、会话存档（Session / Message）、Issue 追踪（Issue / IssueComment）、技能市场（Skill / SkillVersion）、设备级 API Key 管理（UserDeviceApiKey）
  - 配置：`DATABASE_URL` 环境变量

- **`apps/heartbeat-server` — Redis**
  - 驱动：`ioredis` v5.4.1
  - 用途：在线用户计数、心跳注册统计（onlineRegistry）、TapDB 数据上报
  - 配置：`redisUrl` 环境变量

### 2. 桌面端嵌入式

- **`apps/desktop` — SQLite (better-sqlite3) + Drizzle ORM**
  - 驱动：`better-sqlite3` v12.9.0 + `drizzle-orm` v0.36.4
  - 用途：本地 chat 会话 / 消息、agent 元信息、scheduler 状态追踪
  - 文件：`userData/xdt-maker-{userId}.db`（按用户隔离）
  - 含 36+ Drizzle migrations，并实现 SQLITE_CORRUPT 自动回落、Schema 漂移检测修复

- **`apps/desktop` — sqlite-vec（向量库扩展）**
  - 通过本地加载的 sqlite-vec 扩展（`vec0.dylib` / `vec0.dll`）
  - 用途：Chat 历史向量检索、Embedding 存储
  - 位置：`native/sqlite-vec/` 下各平台编译版本

### 3. 其它持久化

- **electron-store / JSON 文件**：Help Assistant 聊天历史、worktree 元信息（`userData/worktrees.json`）、Session 配置快照、Renderer 中 composer draft / maker chat state 等通过 JSON 增量持久化

一句话总结：服务端是 **PostgreSQL（业务核心）+ Redis（心跳）**；桌面端是 **SQLite（按用户隔离的本地库）+ sqlite-vec（向量检索）**，再辅以 electron-store / JSON 文件做轻量配置和缓存。

## Q34: 为什么要用这么多不同的数据库

**问题**

为什么要用这么多不同的数据库 详细介绍

**回答**

核心原则只有一句话：**“右数据库做右事”**。这不是技术堆砌，而是**每种数据库被选中，是因为其它数据库做这件事会非常糟糕**。

### 1. PostgreSQL（apps/server）— 业务核心数据

**为什么需要它**：服务端要存的是**多用户共享、强关系、强一致**的数据：

- User ↔ RefreshToken（一对多，设备登录态）
- User ↔ Session ↔ Message（用户的云端会话存档）
- Skill ↔ SkillVersion（技能市场，有版本树）
- Issue ↔ IssueComment（问题追踪）
- User ↔ UserDeviceApiKey（每设备的 API Key）

这些场景要求：

1. **ACID 事务**：用户上传一个 Skill 新版本时，要同时写 `Skill` + `SkillVersion` + 更新 `latestVersionId`，中途崩了不能脏写
2. **关联查询（JOIN）**：“查这个用户在某设备上最近 7 天的会话”，一句 SQL 解决
3. **唯一约束 / 外键**：Feishu unionId 必须唯一，删除用户时级联清理 RefreshToken
4. **跨实例共享**：服务端是多副本部署，所有副本必须看到同一份数据

**为什么不用 MongoDB / MySQL**：MongoDB 的文档存储难做关系查询；MySQL 也可以，但 Prisma 对 PostgreSQL 支持更完整，JSON 类型 / 部分索引 / CTE 等更成熟。

**为什么用 Prisma**：强类型、IDE 自动补全、Migration 自动生成、TS 端到端类型链。

### 2. Redis（apps/heartbeat-server）— 高频热数据

**为什么独立出一个 heartbeat-server**：心跳上报有几个特别的属性：

1. **极高 QPS**：每个在线用户每 N 秒上报一次，1000 人在线 = 每秒上百次写
2. **天然 TTL**：用户超过 60 秒没上报就该被视为下线，无需手动清理
3. **数据本身没价值**：重启丢光也无所谓，只是统计用
4. **不需要事务**：就是 `SADD online_users:{date} {userId}` + `EXPIRE`

**如果用 PostgreSQL 做这个**：每次写都要走磁盘 WAL，IO 会被打爆；要写 cron 清理过期数据；浪费 ACID 保证；会拖累主业务的连接池。

**Redis 优势**：内存操作、原子计数（INCR / SADD）、天然过期（EXPIRE）、丰富数据结构。

**为什么 ioredis**：对 Cluster / Sentinel 支持更好，Promise API 更现代，是 Node 端事实标准。

### 3. SQLite（better-sqlite3）在 desktop — 本地优先

**为什么客户端要有自己的数据库**：xdt-maker 是 **local-first** 设计：

- **离线可用**：断网时也要能查历史 chat、继续工作
- **响应速度**：本地查询微秒级，云端 API 百毫秒级
- **隐私**：用户对话默认存本地，不强制上传
- **数据量大**：重度用户几个月几十万条消息，云端存全量成本高

**为什么是 SQLite，不是 IndexedDB / LevelDB**：IndexedDB API 难用、不支持 SQL；LevelDB 是 KV 存储，不能 JOIN；SQLite 零运维、单文件、完整 SQL、跨平台一致、有成熟扩展生态。

**为什么用 better-sqlite3 而不是 node-sqlite3**：better-sqlite3 是**同步**的，直接在主线程执行。对 Electron 桌面应用本地查询微秒级场景，异步反而引入开销；性能可高 10 倍以上。Electron main 进程不在乎"阻塞"，因为它不是 Web 服务器，没有并发请求。

**为什么按用户隔离文件（`xdt-maker-{userId}.db`）**：多账号天然隔离不串号；删用户 = 删文件；备份 / 迁移更简单。

**为什么用 Drizzle 而不是 Prisma**：Prisma 客户端 ~10MB 太重；在 SQLite 上不支持完整 migration 流程；Drizzle 轻量、编译期生成 SQL，对 better-sqlite3 一等支持。

### 4. sqlite-vec — 向量检索

**为什么需要向量库**：Chat 历史需要**语义搜索**。用户问“上个月那次讨论 OAuth 的对话”，关键词搜索 OAuth 会漏掉用“飞书登录”这种说法的对话。Embedding + 余弦相似度能找到语义接近的内容，普通 SQL 做不了。

**为什么不用 Pinecone / Weaviate / Qdrant**：这些是独立服务，要起独立进程、要在主 DB 和向量 DB 之间同步、桌面端塞独立服务太重。

**sqlite-vec 的妙处**：

- **同一个 SQLite 文件**：向量数据和 chat 消息住一起，JOIN 直接做
- **作为 SQLite 扩展加载**：`SELECT load_extension('vec0')`
- **零额外进程**：不增加任何服务依赖
- **跨平台编译好**：`native/sqlite-vec/` 下放了 macOS / Windows 各架构的 `.dylib` / `.dll`

代价是性能不如专业向量库，但桌面端单用户数据量级（几万 ~ 几十万向量）完全够用。

### 5. electron-store / JSON 文件 — 轻量配置

**为什么不全用 SQLite**：有些数据**根本不值得**走 SQLite：主题偏好、窗口大小、Help Assistant 聊天历史、worktree 元信息等。

特点：数据量 KB 级、写入频率低、不需要查询只需读全量、人类可读（调试时直接打开 JSON）。

用 SQLite 反而要建表、写 migration，成本远大于读 JSON；出问题时不能直接 cat 排查。

**electron-store 优势**：原子写（防止写一半崩溃损坏）、API 像 Map、自动处理跨平台 userData 路径。

### 总结：为什么不能合并

| 数据库 | 访问模式 | 数据特征 | 部署位置 | 关键能力 |
|---|---|---|---|---|
| PostgreSQL | 关系查询、事务 | 强一致、长期 | 服务端共享 | ACID + JOIN |
| Redis | 高 QPS 读写 | 短期、可丢失 | 服务端独立 | 内存速度 + TTL |
| SQLite | 单进程嵌入 | 本地、按用户隔离 | 桌面端本地 | 离线 + 同步快 |
| sqlite-vec | 向量相似度 | 语义检索 | 桌面端嵌入 | 零额外进程 |
| electron-store | KV 读全量 | 配置、偏好 | 桌面端本地 | 人类可读 |

**反过来想，如果强行用一个数据库**：

- **只用 PostgreSQL**：心跳压垮主库；桌面端断网就 GG；每条配置都要走 HTTP
- **只用 Redis**：没有 ACID，Skill 版本树会脏写；数据丢失风险
- **只用 SQLite**：没法跨用户 / 跨设备共享；心跳服务无意义
- **只用 JSON 文件**：几十万条 chat 消息搜索会卡死

所以这不是“用了很多数据库”，而是**架构边界天然就把数据切成了这几块**：服务端共享业务（PostgreSQL）、服务端高频统计（Redis）、桌面端长期本地数据（SQLite + sqlite-vec）、桌面端零碎配置（electron-store）。每一块只用一种数据库，加起来才是五个。这是**典型成熟产品的多存储分层架构**。

## Q35: PostgreSQL 怎么使用的

**问题**

PostgreSQL 怎么使用的 详细介绍

**回答**

整体架构是 **PostgreSQL 16 + Prisma ORM v6 + Express**，所有服务端业务数据通过 Prisma Client 单例访问。

### 1. Prisma Client 初始化（单例模式）

`apps/server/src/lib/prisma.ts`：

```typescript
const globalForPrisma = globalThis as unknown as { __prisma?: PrismaClient };
const queryLogEnabled = process.env.LOG_PRISMA_QUERY === 'true';
const baseLogs: Prisma.LogLevel[] = ['warn', 'error'];
const logs = queryLogEnabled ? [...baseLogs, 'query'] : baseLogs;

export const prisma = globalForPrisma.__prisma ?? new PrismaClient({ log: logs });

if (process.env.NODE_ENV !== 'production') {
  globalForPrisma.__prisma = prisma;
}
```

要点：
- **全局单例**：HMR 热更新不会反复 new PrismaClient（否则连接池会一次次泄漏）
- **生产不挂全局**：避免污染
- **日志可控**：默认只 warn/error，`LOG_PRISMA_QUERY=true` 才打 SQL（生产 query log 量极大）
- **DATABASE_URL** 从环境变量读

### 2. Schema 全貌（9 张主表）

`apps/server/prisma/schema.prisma`，按业务域分组：

- **用户与认证**：User（CUID + feishuId @unique）、RefreshToken（[userId, deviceId] 复合唯一）、UserDeviceApiKey
- **会话存档**：Session（status active/archived/deleted）、Message（content Json，[sessionId, clientId] 防重传）、ChatMigrationStatus（[userId, deviceId] 复合主键）
- **问题追踪**：Issue（number 自增、visibility 控制）、IssueComment
- **技能市场**：Skill（soft delete via deletedAt、latestVersion 缓存）、SkillVersion（[skillName, version] 复合主键，manifest Json）

关键约束模式：
```prisma
refreshTokens RefreshToken[] @relation(onDelete: Cascade)  // 级联清理
assignee  User? @relation(onDelete: SetNull)               // 取消指派但保留
author    User  @relation(onDelete: Restrict)              // 防误删
visibleDeptIds String[]
@@index([visibleDeptIds], type: Gin)                        // GIN 索引
@@index([userId, deviceId, updatedAt(sort: Desc)])         // 排序复合索引
```

### 3. 五种典型查询模式

**3.1 Upsert（OAuth 登录）** `services/auth.ts`：

```typescript
const user = await prisma.user.upsert({
  where: { feishuId: userInfo.openId },
  create: { feishuId: userInfo.openId, name: userInfo.name, avatarUrl: userInfo.avatarUrl },
  update: { name: userInfo.name, avatarUrl: userInfo.avatarUrl },
});
```
首次登录创建、再次登录刷新昵称/头像，原子完成。

**3.2 事务（Skill 发版）** `services/skills.ts`：

```typescript
return prisma.$transaction(async tx => {
  const existing = await tx.skill.findFirst({ where: { name: p.name, deletedAt: null } });
  if (p.version !== existing.latestVersion + 1) throw versionRace();
  await tx.skillVersion.create({ data: { skillName: p.name, version: p.version, manifest: p.manifest } });
  await tx.skill.update({ where: { name: p.name }, data: { latestVersion: p.version } });
});
```
版本号 +1 校验 + 写 SkillVersion + 推进 latestVersion 必须原子，防并发竞争。

**3.3 复杂查询（include 聚合）** `services/sessions.ts`：

```typescript
return prisma.session.findMany({
  where: { userId, deviceId, status: statusWhere },
  orderBy: { updatedAt: 'desc' },
  take: limit,
  include: { _count: { select: { messages: true } } },  // 一次 SQL 带消息数
});
```

**3.4 Raw SQL（数组重叠）** `services/skills.ts`：

```typescript
const rows = await prisma.$queryRaw<Array<{ count: bigint }>>(Prisma.sql`
  SELECT COUNT(*)::bigint FROM "Skill" s
  WHERE s."deletedAt" IS NULL AND s."latestVersion" > 0
    AND (s.visibility = 'PUBLIC'
      OR (s.visibility = 'DEPARTMENT_SCOPED' AND s."visibleDeptIds" && ${userDeptIds}::text[]))
`);
```
PostgreSQL 数组 `&&` 重叠操作符 Prisma DSL 不支持，但 GIN 索引性能极好；`Prisma.sql` 模板自动参数化，仍防注入。

**3.5 Cursor 分页** `services/messages.ts`：

```typescript
return prisma.message.findMany({
  where: { sessionId, createdAt: { lt: cursorCreatedAt, gt: after } },
  orderBy: { createdAt: 'desc' },
  take: limit,
});
```
比 offset 分页好：O(log n) 索引查找，任何页性能恒定。

### 4. Migration 流程

`apps/server/prisma/migrations/` 下 33 个 migration，按时间戳命名：

```
20260408000000_add_refresh_token/
20260412032511_add_cc_agent_sessions/
20260420180000_add_issue_tracker/
20260424180000_add_skillhub/
20260525180000_add_user_device_api_key/
```

命令：
- `pnpm db:migrate`（= `prisma migrate dev`，开发用，会建 `_prisma_migrations` 表追踪状态）
- `prisma migrate deploy`（生产用，只 apply 已 review 的）
- `pnpm prisma generate`（重生成类型）

Seed（`prisma/seed.ts`）：

```typescript
await prisma.user.upsert({
  where: { feishuId: 'bot:agent@xdt-maker.bot' },
  create: { id: botUserId, feishuId: '...', name: 'Auto Triage Agent' },
  update: {},  // 幂等：不覆盖人工修改
});
```

### 5. 部署与连接管理

`apps/server/.env.example`：

```
DATABASE_URL=postgresql://user:password@localhost:5432/xdt_maker
```

`docker-compose.prod.yaml`：

```yaml
db:
  image: postgres:16-alpine
  ports: ["18888:5432"]
  environment:
    POSTGRES_USER: xdt_maker
    POSTGRES_PASSWORD: xdt_maker
    POSTGRES_DB: xdt_maker
  volumes:
    - /opt/xdt-maker/data:/var/lib/postgresql/data
  healthcheck:
    test: ["CMD-SHELL", "pg_isready -U xdt_maker"]
```

部署：默认外接 RDS，`--profile legacy` 才启本地 db；`sslmode` 通过环境变量切换。连接池默认 `num_cpus * 2 + 1`。

### 6. PostgreSQL 特性运用

| 特性 | 用在哪 | 为什么必须用 |
|---|---|---|
| Json | Message.content / SkillVersion.manifest | Claude 消息结构复杂 |
| String[] | User 部门 / Skill.visibleDeptIds | 飞书部门天然多值 |
| GIN 索引 | `@@index([visibleDeptIds], type: Gin)` | 加速数组 `&&` 查询 |
| Partial Unique Index | Skill.name 软删后释放 | `UNIQUE(name) WHERE deletedAt IS NULL`，允许同名复用 |
| Enum | Effort / SessionStatus / IssueVisibility | 强类型 + 数据库层校验 |
| @updatedAt | 所有主表 | 数据库自动审计时间 |
| 级联约束 | Cascade / SetNull / Restrict | 各表按业务语义选择删除策略 |

**Soft Delete + Partial Unique Index 的妙用**：普通 `@unique(name)` + soft delete 会冲突——删了一条同名再建会撞唯一约束。解法是手写 SQL：

```sql
CREATE UNIQUE INDEX skill_name_active_unique
  ON "Skill"(name) WHERE "deletedAt" IS NULL;
```

只对未删除的行生效，删的行退出约束，允许同名复用。Prisma DSL 不支持 partial index，必须在 migration 里手写。

### 核心文件速查

| 文件 | 用途 |
|-----|------|
| `apps/server/src/lib/prisma.ts` | PrismaClient 单例 |
| `apps/server/prisma/schema.prisma` | 完整数据模型 |
| `apps/server/prisma/seed.ts` | 系统账户初始化 |
| `services/auth.ts` | upsert 示例 |
| `services/skills.ts` | $transaction + raw SQL 示例 |
| `services/messages.ts` | cursor 分页示例 |
| `services/sessions.ts` | include 聚合示例 |
| `.env.example` / `docker-compose.prod.yaml` | 部署配置 |

### 总结

这不是把 PostgreSQL 当 KV 存的初级用法，而是把 ORM 类型安全 + PostgreSQL 数据库特性各自用到最佳位置的成熟工程实践：单例避免连接泄漏、事务用在版本号推进的关键路径、Raw SQL 只用在 Prisma 表达不了的地方、Cursor 分页让大表性能稳定、JSON/数组/GIN/partial index/enum 都用对位置、软删除 + partial index 是非常成熟的工程模式。

## Q36: Prisma 关系约束与索引详解

**问题**

详细介绍以下几行：

```prisma
refreshTokens RefreshToken[] @relation(onDelete: Cascade)  // 级联清理
assignee  User? @relation(onDelete: SetNull)               // 取消指派但保留
author    User  @relation(onDelete: Restrict)              // 防误删
visibleDeptIds String[]
@@index([visibleDeptIds], type: Gin)                        // GIN 索引
@@index([userId, deviceId, updatedAt(sort: Desc)])         // 排序复合索引
```

**回答**

这五行涵盖了 Prisma 在 PostgreSQL 上**两类核心能力**：**关系约束（onDelete）** 和 **索引设计**。

### 一、`onDelete: Cascade` — 级联删除

```prisma
refreshTokens RefreshToken[] @relation(onDelete: Cascade)
```

父表（User）一行删除时，子表（RefreshToken）所有引用它的行**自动删掉**。

生成 SQL：
```sql
ALTER TABLE "RefreshToken"
  ADD CONSTRAINT "RefreshToken_userId_fkey"
  FOREIGN KEY ("userId") REFERENCES "User"("id")
  ON DELETE CASCADE ON UPDATE CASCADE;
```

**为什么用 Cascade**：RefreshToken 完全依附 User，User 没了 token 就没意义且是安全隐患。约束在数据库层生效，psql 直删 User 也会自动清 token，少一个 bug 点。

**反例**：默认 `NoAction` 时删 User 会报 FK violation，必须手动 `deleteMany`，易遗漏。

### 二、`onDelete: SetNull` — 删除时置空

```prisma
assignee User? @relation(onDelete: SetNull)
```

User 删除时，Issue 的 `assigneeId` **置 NULL**，Issue 本身保留。

**关键前提**：`assignee User?` 末尾的 `?` 不是装饰——必须可空字段才能 SetNull，否则 Prisma 拒绝。

**为什么用 SetNull**：Issue 是业务历史，指派人离职但 Issue 内容、评论、状态依然有价值。指派关系应自动解除（不该在"我的待办"里出现离职人），但 Issue 留住。

### 三、`onDelete: Restrict` — 禁止删除

```prisma
author User @relation(onDelete: Restrict)
```

**禁止删除还被引用的 User**，强行删会报错回滚。

**为什么用 Restrict**：author 跟 assignee 不一样——assignee 是可变状态（可以改派、可以为空），author 是历史事实（这个 Issue 就是这个人创建的）。允许 SetNull 会让 UI 出现"匿名作者"，审计链断裂。

**触发场景**：删除 alice → PostgreSQL 报 FK violation。这是**好事**，强迫开发者面对"alice 的 Issue 怎么办"。解法通常是软删除：

```typescript
await prisma.user.update({
  where: { id: alice.id },
  data: { deletedAt: new Date(), email: null },
});
```

UI 显示"alice（已注销）"。

### 三种策略对比

| 策略 | 含义 | 适用 |
|---|---|---|
| Cascade | 父删 → 子也删 | 子表完全依附（token、子评论） |
| SetNull | 父删 → 子外键置空 | 关系可变（指派、归属） |
| Restrict | 父删 → 禁止 | 关系是历史事实（作者、创建人） |

### 四、`String[]` — PostgreSQL 原生数组

```prisma
visibleDeptIds String[]
```

PostgreSQL `TEXT[]` 类型，一个 cell 存多个字符串：`{"dept_001", "dept_002"}`。

**为什么用数组不拆表**：传统正规化要建关联表 `SkillDeptVisibility(skillId, deptId)`，但部门可见性的特点是：
1. 每个 Skill 部门列表很短（< 20）
2. 整体一起读 / 一起写
3. 不需要 JOIN 部门表拿名字

"一对多但整体读写"——数组优于关联表，单行操作，无 JOIN。

**PostgreSQL 数组操作符**：

| 操作符 | 含义 | 示例 |
|---|---|---|
| `&&` | 有交集 | `{1,2,3} && {3,4} → true` |
| `@>` | 包含 | `{1,2,3} @> {1,2} → true` |
| `<@` | 被包含 | - |
| `\|\|` | 拼接 | - |

`&&` 在权限查询的核心：`WHERE s."visibleDeptIds" && ARRAY['dept_eng']` → 用户在 eng 部门就能看到。

**为什么不用 JSON**：JSON 类型弱、索引繁琐（要 jsonb_path_ops）、操作符少。`TEXT[]` 是强类型 + 操作符丰富 + 索引友好的组合。

### 五、`@@index([visibleDeptIds], type: Gin)` — GIN 索引

```prisma
@@index([visibleDeptIds], type: Gin)
```

生成 SQL：
```sql
CREATE INDEX "Skill_visibleDeptIds_idx" ON "Skill" USING GIN ("visibleDeptIds");
```

**GIN（Generalized Inverted Index）** 是倒排索引：

```
B-tree:                  GIN:
"alice" → row 1          "dept_eng" → [row 1, 5, 8]
"bob"   → row 2          "dept_pm"  → [row 2, 5]
```

GIN 把数组里**每个元素**都建索引，从元素反查包含它的行。

**为什么数组必须用 GIN，不能用 B-tree**：B-tree 假设可比较有大小，数组没有自然顺序；更关键的是查询语义是"包含某个值"而不是"等于这个数组"，B-tree 做不到部分匹配。

**实际查询过程**：
```sql
WHERE "visibleDeptIds" && ARRAY['dept_eng', 'dept_pm']
```

无索引：全表扫，1 万行查 1 万次数组重叠。  
有 GIN：从倒排表查 dept_eng → [1,5,8]，查 dept_pm → [2,5]，取并集 → 直接定位。O(k log n)，毫秒级。

**代价**：写入慢、空间大、VACUUM 慢。适用"读多写少 + 数组字段 + 包含/重叠查询"——Skill 完美符合。

### 六、`@@index([userId, deviceId, updatedAt(sort: Desc)])` — 排序复合索引

```prisma
@@index([userId, deviceId, updatedAt(sort: Desc)])
```

生成 SQL：
```sql
CREATE INDEX "Session_userId_deviceId_updatedAt_idx"
  ON "Session" ("userId", "deviceId", "updatedAt" DESC);
```

**字段顺序的最左前缀原则**：

| 查询条件 | 能用？ |
|---|---|
| `WHERE userId = ?` | ✅ |
| `WHERE userId = ? AND deviceId = ?` | ✅ |
| `WHERE userId = ? AND deviceId = ? ORDER BY updatedAt DESC` | ✅ 完美 |
| `WHERE deviceId = ?` | ❌（跳过了 userId）|

规则：**等值字段在前，范围/排序字段在后**。

**为什么 `updatedAt` 标 `sort: Desc`**：业务查询是 `orderBy: { updatedAt: 'desc' } take: 20`。B-tree 是有序结构，如果索引 ASC 排，DESC 查询要反向扫描——能做但慢。建成 DESC 后，**正向扫描第一条就是最新的**，LIMIT 20 截断即可。

**没有索引会怎样**：
```
Seq Scan on Session    -- 全表扫
Filter: userId = ... AND deviceId = ...
Sort: updatedAt DESC   -- 内存/磁盘排序
Limit 20
```
1 千万行表秒级响应。

有索引：
```
Index Scan using Session_..._idx   -- 直接定位
Index Cond: userId = ... AND deviceId = ...
Limit 20                            -- 取 20 条停
```
毫秒级，无论表多大。

**复合索引 vs 多个单列索引**：PostgreSQL 虽能 Bitmap Index Scan 合并多索引，但三次扫描 + 位图合并 + 排序，比一次复合扫描慢；存储更大、写入要更新 3 个索引。**复合索引专为"这个查询模式"优化**，最高效。

### 总结：这五行体现的设计思想

| 行 | 类型 | 决策核心 |
|---|---|---|
| Cascade | 关系约束 | 子表依附父表，DB 自动清理 |
| SetNull | 关系约束 | 关系可解除，业务实体保留 |
| Restrict | 关系约束 | 关系是历史事实，禁止破坏 |
| `String[]` | 数据类型 | "一对多但整体读写"用数组优于关联表 |
| GIN 索引 | 索引类型 | 数组的包含/重叠查询必须用 GIN |
| 复合 DESC 索引 | 索引设计 | 字段顺序匹配查询模式，排序方向匹配业务方向 |

共同点：**让数据库本身保证正确性 + 高性能，而不是把责任甩给应用代码**。约束在 schema 里，索引为查询模式服务，这是把数据库当"专业工具"用，而不是当"哑存储"用。

## Q37: findMany include _count 与 $queryRaw 详解

**问题**

详细介绍以下两段查询：

```typescript
return prisma.session.findMany({
  where: { userId, deviceId, status: statusWhere },
  orderBy: { updatedAt: 'desc' },
  take: limit,
  include: { _count: { select: { messages: true } } },
});

const rows = await prisma.$queryRaw<Array<{ count: bigint }>>(Prisma.sql`
  SELECT COUNT(*)::bigint FROM "Skill" s
  WHERE s."deletedAt" IS NULL AND s."latestVersion" > 0
    AND (s.visibility = 'PUBLIC'
      OR (s.visibility = 'DEPARTMENT_SCOPED' AND s."visibleDeptIds" && ${userDeptIds}::text[]))
`);
```

**回答**

这两段代表 Prisma 的**两个能力极端**：类型安全的 DSL vs 必要时落 raw SQL。

### 一、`prisma.session.findMany` — 类型安全 DSL 的典型用法

#### 1. `where: { userId, deviceId, status }`

Prisma where 对象里**多个字段默认 AND 关系**：

```typescript
{ userId, deviceId, status }
// 等价于
{ AND: [{ userId }, { deviceId }, { status }] }
```

对应 SQL：
```sql
WHERE "userId" = $1 AND "deviceId" = $2 AND "status" = $3
```

**关键**：这正好命中复合索引 `@@index([userId, deviceId, updatedAt(sort: Desc)])`——where 用前两列、orderBy 用第三列降序，完美匹配，走 Index Scan，毫秒级。

#### 2. `orderBy: { updatedAt: 'desc' }`

配合上面的 DESC 复合索引，数据库**不需要 Sort 节点**，沿 B-tree 顺扫即可。

EXPLAIN：
```
Index Scan using ...
  Index Cond: userId = ... AND deviceId = ...
```
注意没有 Sort 节点——索引顺序就是结果顺序。

#### 3. `take: limit`

等于 SQL 的 `LIMIT ?`。配合索引，"沿索引取前 N 行就停"，100 万行表也只读 N 行。O(log n + k)。

#### 4. `include: { _count: { select: { messages: true } } }` — 精华

返回值类型：
```typescript
type Result = Session & { _count: { messages: number } };
```

**不用 _count 的烂写法（N+1）**：
```typescript
const sessions = await prisma.session.findMany({ ... });
for (const s of sessions) {
  const count = await prisma.message.count({ where: { sessionId: s.id } });
  // ❌ 20 个 session = 21 次 SQL
}
```

**用 _count**：Prisma 编译成**一句 SQL**，子查询 / JOIN 聚合：

```sql
SELECT s.*, 
  (SELECT COUNT(*) FROM "Message" m WHERE m."sessionId" = s.id) AS "_count_messages"
FROM "Session" s
WHERE s."userId" = $1 AND s."deviceId" = $2 AND s."status" = $3
ORDER BY s."updatedAt" DESC LIMIT $4;
```

**为什么不直接 `include: { messages: true }`**：会把每个 session 所有消息拉回来，一个 session 几千条 × 20 = 几万条塞内存，列表 UI 只要"5 条消息"角标，浪费。

#### 5. 整段查询的价值
- ✅ 类型安全：字段拼错编译报错
- ✅ 零 N+1：_count 一次 SQL 解决
- ✅ 完美命中索引：where + orderBy + limit 和索引设计严丝合缝
- ✅ 可读性高

### 二、`prisma.$queryRaw` — 原生 SQL 的合理用法

#### 1. 为什么不用 DSL

`s."visibleDeptIds" && ${userDeptIds}::text[]` 用的是 PostgreSQL **数组重叠操作符 `&&`**。

**Prisma DSL 不支持**：`has` / `hasSome` / `hasEvery` 虽能做类似事，但**不会用 GIN 索引**（Prisma v6 仍未优化）。性能关键路径必须 raw SQL 才能命中 GIN。

#### 2. `prisma.$queryRaw<T>`

| API | 用途 | 返回 |
|---|---|---|
| `$queryRaw` | SELECT | 数组 |
| `$executeRaw` | INSERT/UPDATE/DELETE | 受影响行数 |
| `$queryRawUnsafe` | 字符串拼接（有注入风险） | 数组 |

泛型 `<Array<{ count: bigint }>>` 告诉 TypeScript 返回行的结构。`bigint` 因为 PostgreSQL `COUNT(*)` 默认返回 BIGINT。

#### 3. `Prisma.sql\`...\`` — 安全的关键

**标签模板函数**，做两件事：
1. 字面量部分原样拼接
2. `${...}` 占位符转成参数化查询的 $1, $2, ...

实际发到数据库：
```sql
... s."visibleDeptIds" && $1::text[]
```
参数 `$1 = userDeptIds` 以参数化方式发送，**完全防 SQL 注入**。

**反例：`$queryRawUnsafe` 真危险**：
```typescript
prisma.$queryRawUnsafe(`SELECT ... WHERE deptId = '${userInput}'`);
// 用户传 '; DROP TABLE Skill; --'  → 整张表没了
```

#### 4. 逐句 SQL 解读

- `s."deletedAt" IS NULL`：软删除过滤
- `s."latestVersion" > 0`：过滤掉还没发布过任何版本的 Skill（latestVersion 默认 0）
- `s.visibility = 'PUBLIC'`：公开 Skill
- `OR (s.visibility = 'DEPARTMENT_SCOPED' AND s."visibleDeptIds" && ${userDeptIds}::text[])`：部门可见且与用户部门有交集

`&&` 操作符的妙用：
```
Skill.visibleDeptIds = {"dept_eng", "dept_pm"}
user.departmentIds   = {"dept_eng", "dept_design"}
两者有交集("dept_eng") → 用户能看到
```

#### 5. 为什么性能好

EXPLAIN（简化）：
```
Aggregate
  Bitmap Heap Scan on "Skill"
    ->  BitmapOr
        ->  Bitmap Index Scan on "Skill_visibility_idx"
        ->  Bitmap Index Scan on "Skill_visibleDeptIds_idx"  ← GIN!
              Index Cond: visibleDeptIds && '{dept_eng,...}'::text[]
```

GIN 倒排表查 dept_eng 拿到包含它的 Skill row 列表，O(log n)。

DSL 写 `hasSome` 不走 GIN → Seq Scan → 10 万行 = 10 万次数组重叠计算，性能差几十倍。

#### 6. 为什么用 `COUNT(*)` 而不是 `findMany().length`

```typescript
// ❌
const rows = await prisma.skill.findMany({ where });
return rows.length;  // 把 10 万行数据全拉回内存才数长度
```

`COUNT(*)` 只算个数，不返回行数据，传一个 bigint 回来。差几个量级的网络 + 内存开销。

### 三、两段查询的对比与设计哲学

| 维度 | findMany + _count | $queryRaw |
|---|---|---|
| 类型安全 | ✅ 全自动推导 | ⚠️ 需手写泛型 |
| 防注入 | ✅ 内置参数化 | ✅ Prisma.sql 才安全 |
| 表达能力 | ⚠️ DSL 范围内 | ✅ PostgreSQL 全功能 |
| 索引利用 | ✅ DSL SQL 简单，索引匹配清晰 | ✅ 完全可控，精确命中复杂索引 |
| 可维护性 | ✅ schema 变自动跟进 | ⚠️ SQL 不会自动改 |
| 重构友好 | ✅ rename 字段编译报错 | ⚠️ 字符串里字段名重构工具看不到 |

### 选择原则

1. **能用 DSL 就用 DSL**（类型安全 + 重构友好）
2. **DSL 表达不了或性能不达标时落 raw SQL**（数组操作、CTE、窗口函数、性能关键路径）
3. **raw SQL 必须用 `Prisma.sql` 模板**，绝不用 `$queryRawUnsafe`
4. **raw SQL 写清楚泛型**，让 TS 知道返回结构

DSL 处理 80% 常规查询，raw SQL 解决 20% 高难场景，各取所长。

## Q38: PostgreSQL 特性运用深度解读

**问题**

PostgreSQL 特性运用 详细介绍

**回答**

之前 Q36 讲了关系约束、数组基础、GIN 索引、复合索引；Q37 讲了 `&&` 操作符在 raw SQL 里的用法。这次深入展开那些一笔带过的特性。

### 一、`Json` 字段 — 半结构化数据容器

**底层是 JSONB 不是 JSON**：Prisma 在 PostgreSQL 上实际生成 `JSONB`（二进制 JSON）。

| 维度 | JSON | JSONB |
|---|---|---|
| 存储 | 字符串 | 解析后的二进制 |
| 读取 | 慢（每次解析） | 快 |
| 索引 | ❌ 不能 GIN | ✅ 能 GIN |
| 操作符 | 少 | 丰富 |

**写入**：`content: body.content as Prisma.InputJsonValue`。这层 cast 是安全网，确保你写的是可序列化 JSON（不是 Date/Map/Set/函数）。

**读取的类型擦除问题**：`msg.content` 类型是 `Prisma.JsonValue`，TS 拿不到具体形状——因为 JSONB 在 DB 层无 schema。解法是应用层定义 TS interface + Zod 校验。

**JSONB 操作符**（raw SQL 可用）：

| 操作符 | 含义 |
|---|---|
| `->` | 取字段（返回 JSONB） |
| `->>` | 取字段（返回 TEXT） |
| `#>` / `#>>` | 路径取 |
| `@>` | 包含 |
| `?` / `?\|` / `?&` | key 是否存在 |

可建 GIN 索引：`CREATE INDEX msg_content_gin ON "Message" USING GIN (content)`。

**为什么不拆关系表**：ClaudeMessage 的 blocks 可能是 text/image/tool_use/tool_result，多态结构；API 新增 block 类型只要改 TS 类型，不动 schema。Schema-on-Read 思路，写入快、灵活、迭代友好；代价是失去 SQL 层校验，由 Zod 兜底。

### 二、`String[]` 数组深入

**写入**：`firstLevelDepartmentIds: ['dept_eng', 'dept_pm']` → `ARRAY[...]`。

**Prisma DSL 数组操作不走 GIN**：

| Prisma | 等价 SQL | 走 GIN? |
|---|---|---|
| `{ has: 'x' }` | `'x' = ANY(field)` | ❌ |
| `{ hasSome: [...] }` | `field && ARRAY[...]` | ❌（Prisma v6 不优化） |
| `{ hasEvery: [...] }` | `field @> ARRAY[...]` | ❌ |

性能关键路径必须 raw SQL。

**raw SQL 数组常用操作**：

```sql
array_length(arr, 1)                      -- 长度
arr || ARRAY['new']                       -- 追加
array_remove(arr, 'old')                  -- 移除
array_agg(DISTINCT x)                     -- 聚合去重
unnest(arr) AS x                          -- 数组炸成行
```

`unnest` 强大，可以 GROUP BY 统计"每个 dept 能看到多少 skill"。

**数组 vs 关系表判断标准**：

| 场景 | 选数组 | 选关系表 |
|---|---|---|
| 元素数 | < ~100 | 无上限 |
| 独立查询 | 否 | 是 |
| 写入模式 | 整体替换 | 增删单条 |
| 子元素带元数据 | 否 | 是 |

### 三、Partial Unique Index — 软删除的关键魔法

**问题**：普通 `@unique(name)` + 软删除会冲突——删了同名再建会撞唯一约束。

**解法**：PostgreSQL 允许带 WHERE 子句的唯一索引：

```sql
CREATE UNIQUE INDEX skill_name_active_unique
  ON "Skill"(name)
  WHERE "deletedAt" IS NULL;
```

只对未删除的行生效，软删后退出索引，允许同名复用。

**Prisma 不支持 partial index**，必须 migration 里手写 SQL；schema.prisma 不能写 `@unique` 避免冲突。

**这是 PostgreSQL 杀手锏**：MySQL 不支持（InnoDB 没有），MSSQL 叫 filtered index，Oracle 用 function-based index 模拟。

**其他用途**：

```sql
-- 只为 active session 建索引（冷数据不浪费）
CREATE INDEX session_active ON "Session"(userId)
  WHERE status = 'active';

-- 只为高优先级 issue 建索引
CREATE INDEX issue_p0p1 ON "Issue"(createdAt)
  WHERE priority IN ('P0', 'P1');
```

核心价值：**只索引业务真正会查的子集，索引更小更快**。

### 四、Enum — 强类型枚举

```prisma
enum SessionStatus { active archived deleted }
model Session {
  status SessionStatus @default(active)
}
```

PostgreSQL 创建真正的 enum 类型：

```sql
CREATE TYPE "SessionStatus" AS ENUM ('active', 'archived', 'deleted');
```

**vs 字符串 + CHECK**：

| 维度 | Enum | String + CHECK |
|---|---|---|
| 存储 | 4 字节 | 字符串长度 |
| 比较 | 整数 | 字符串 |
| 新增值 | `ALTER TYPE ADD VALUE` | 改 CHECK，扫表 |
| 删除值 | 难（需重建 type） | 改 CHECK |
| TS 类型 | 自动生成 union | 手动维护 |

**编译期类型保护**：
```typescript
data: { status: 'completed' }  // ❌ TS 报错，不在 enum 内
```

**演进的坑**：删 enum 值需重建 type 切换字段；生产环境是高风险操作。通常只加不删，旧值标弃用。

### 五、`@updatedAt` / `@default(now())` — 时间戳自动化

`@default(now())` 是数据库默认：

```sql
"createdAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP
```

`TIMESTAMP(3)` 是毫秒精度（匹配 JS Date）。

**`@updatedAt` 不是数据库实现，是 Prisma 实现**：

```sql
"updatedAt" TIMESTAMP(3) NOT NULL   -- 没有 ON UPDATE
```

MySQL 有 `ON UPDATE CURRENT_TIMESTAMP`，PostgreSQL 没有。`@updatedAt` 由 Prisma Client 在 update 时主动塞值：

```sql
UPDATE "Session" SET status = $1, "updatedAt" = $2 WHERE id = $3
```

**坑**：绕过 Prisma 的更新不会自动更新 updatedAt：

```sql
-- psql 直接 update → updatedAt 不变
-- $executeRaw 也不变
```

raw SQL 业务依赖 updatedAt 必须手动带：

```typescript
await prisma.$executeRaw`
  UPDATE "Session" SET status = ${s}, "updatedAt" = NOW() WHERE id = ${id}
`;
```

**真要数据库强制**可用触发器，但 Prisma 不主动加（跨数据库不兼容、隐式行为不友好、migrate 不同步触发器）。

### 六、CUID 主键 — 为什么不用 UUID / 自增

```prisma
id String @id @default(cuid())
```

CUID 例子：`cl9k8s0d10000qz4x6h7a1b2c`（25 字符）。组成：前缀 + 时间戳 + 进程指纹 + 计数器 + 随机段。

**vs UUID v4**：

| 维度 | CUID | UUID v4 |
|---|---|---|
| 长度 | 25 | 36 |
| 顺序 | 时间有序 | 完全随机 |
| B-tree 性能 | ✅（追加在末尾） | ❌（随机插入，页分裂多） |

**vs 自增 ID**：

| 维度 | CUID | 自增 |
|---|---|---|
| 唯一性 | 全局 | 单表 |
| 跨服务 | ✅ | ❌ |
| URL 泄露信息 | 否 | 是（暴露用户量） |
| 客户端预生成 | ✅ | ❌ |

CUID 牺牲一点性能换分布式 + URL 安全 + 预生成。这个项目写性能不是瓶颈，CUID 是合理选择。

**Issue.number 是例外**：

```prisma
id     String  @id @default(cuid())
number Int     @unique  // #1234，UI 展示用
```

UI 显示 `#1234`，用户期望连续递增（像 GitHub），所以 number 用自增；但本质主键还是 CUID。

### 七、总览：所有特性如何配合

"用户创建 Skill 并发布版本"的完整流程涉及：

```typescript
// 1. CUID + Enum
const user = await prisma.user.findUnique({ where: { feishuId: openId } });

// 2. 事务保证 Skill + SkillVersion 原子
await prisma.$transaction(async tx => {
  // 3. partial unique index 允许复用已删除名字
  const skill = await tx.skill.create({
    data: {
      name: 'git-commit',
      visibility: 'DEPARTMENT_SCOPED',     // Enum 类型检查
      visibleDeptIds: ['dept_eng'],        // String[]
      latestVersion: 0,
      // createdAt / updatedAt 自动填
    },
  });
  await tx.skillVersion.create({
    data: {
      skillName: skill.name,
      version: 1,
      manifest: { /* ... */ } as Prisma.InputJsonValue,  // JSONB
    },
  });
  await tx.skill.update({
    where: { name: skill.name },
    data: { latestVersion: 1 },     // updatedAt 自动更新
  });
});

// 4. 查询利用 GIN + raw SQL
const visibleSkills = await prisma.$queryRaw`
  SELECT * FROM "Skill" s
  WHERE s."deletedAt" IS NULL
    AND (s.visibility = 'PUBLIC'
      OR s."visibleDeptIds" && ${user.firstLevelDepartmentIds}::text[])
`;

// 5. 软删后名字腾出
await prisma.skill.update({
  where: { name: 'git-commit' },
  data: { deletedAt: new Date() },
});
```

**每个特性都对应一个具体业务痛点**：

| 痛点 | PostgreSQL 解法 |
|---|---|
| 消息结构会变 | JSONB |
| 部门列表是天然多值 | TEXT[] |
| 数组查询要快 | GIN |
| 软删后名字要能复用 | Partial Unique Index |
| 状态值集合稳定 | Enum |
| 时间戳要自动 | @default(now()) + Prisma @updatedAt |
| 主键要分布式 + 索引友好 | CUID |
| 业务对外 ID 要连续 | 自增 number |
| 关系约束要在 DB 层 | Cascade / SetNull / Restrict |
| 多操作要原子 | $transaction |

**核心哲学**：把数据库特性当一等公民，而不是把数据库当哑存储。换 MongoDB 或 KV，这些痛点要在应用层重新实现，代码量翻几倍还更易出 bug。

## Q39: prisma.session 怎么读到对应的数据库表

**问题**

prisma.session 是怎么读到对应的数据库表的

**回答**

这背后是一条 **schema → 代码生成 → 运行时映射 → SQL 执行** 的完整链路。

### 一、起点：schema.prisma 声明

```prisma
model Session {
  id        String   @id @default(cuid())
  userId    String
  status    SessionStatus @default(active)
  ...
}
```

- `model` 关键字声明数据模型
- 模型名首字母大写（Prisma 命名规范）

### 二、`prisma generate` 代码生成

**何时触发**：`pnpm prisma generate` 显式，或 `pnpm install` / `prisma migrate dev` 隐式。

**生成到哪**：

```
node_modules/
  .prisma/client/
    index.js / index.d.ts        ← PrismaClient + 类型
    libquery_engine-*.node       ← Rust Query Engine 二进制
  @prisma/client/                ← 只是壳，re-export .prisma/client
```

**生成的 TS 类型（简化）**：

```typescript
export type Session = { id: string; userId: string; ... };

export interface SessionDelegate {
  findMany(args?: SessionFindManyArgs): Promise<Session[]>;
  findUnique(args: SessionFindUniqueArgs): Promise<Session | null>;
  create(args): Promise<Session>;
  ...
}

export class PrismaClient {
  readonly session: SessionDelegate;     // ← 关键
  readonly user: UserDelegate;
  readonly refreshToken: RefreshTokenDelegate;
}
```

**命名规则：model 名首字母小写 → delegate 属性**：

| Model | 访问 |
|---|---|
| `User` | `prisma.user` |
| `Session` | `prisma.session` |
| `RefreshToken` | `prisma.refreshToken` |
| `UserDeviceApiKey` | `prisma.userDeviceApiKey` |

纯字符串变换规则，编译时硬编码进生成代码，IDE 自动补全完美。

### 三、Delegate 是什么

`prisma.session` 是 `SessionDelegate` 实例，**记住自己代表哪个 model**（字符串 `'Session'`），调用时把这个名字传给 Query Engine：

```typescript
class SessionDelegate {
  async findMany(args) {
    return this.client._engine.request({
      action: 'findMany',
      modelName: 'Session',   // ← 关键
      args,
    });
  }
}
```

真实代码用 getter 延迟加载：

```javascript
Object.defineProperty(PrismaClient.prototype, 'session', {
  get() { return this._extensions.getDelegate('Session'); },
});
```

### 四、Model 名 → 表名映射

**默认规则**：model 名就是表名。

```prisma
model Session { ... }
```
→
```sql
CREATE TABLE "Session" (...);    -- 带双引号保留大小写
```

PostgreSQL 默认会把无引号标识符转小写，Prisma 永远加双引号保留原大小写。

**用 `@@map` 覆盖**：

```prisma
model Session { ...; @@map("chat_sessions") }
```
→ `CREATE TABLE "chat_sessions"`。TS 代码仍写 `prisma.session`。

**`@map` 改字段名**：`userId String @map("user_id")` → SQL `"user_id"`，TS `session.userId`。

**这个项目没用 `@@map` / `@map`**，TypeScript 名字直接是数据库名字。

### 五、运行时完整调用链

`prisma.session.findMany({ where: { userId: 'abc' } })`：

```
1. 业务代码 prisma.session.findMany({...})
   ↓
2. SessionDelegate 封装请求 { action: 'findMany', modelName: 'Session', query: {...} }
   ↓
3. Query Engine (Rust 写的 libquery_engine-*.node)
   - 查 DMMF → modelName='Session' 对应表 "Session"
   - 生成 SQL: SELECT ... FROM "public"."Session" WHERE "userId" = $1
   ↓
4. 连接池（DATABASE_URL 配置，默认 num_cpus * 2 + 1）
   ↓
5. PostgreSQL 执行 prepared statement
   ↓
6. Query Engine 反序列化：timestamp → Date, enum → union, bigint → bigint
   ↓
7. 业务代码拿到 Session[]，类型完美
```

### 六、DMMF — schema 和 client 之间的桥梁

**Data Model Meta Format**：generate 时把 schema 解析成结构化 JSON 内置进生成代码：

```typescript
{
  models: [{
    name: 'Session',
    dbName: 'Session',         // ← @@map 会改这个
    fields: [
      { name: 'userId', dbName: 'userId', type: 'String' },
      { name: 'status', dbName: 'status', type: 'SessionStatus' },
    ],
  }],
  enums: [{
    name: 'SessionStatus',
    values: [{ name: 'active', dbName: 'active' }, ...],
  }],
}
```

**谁用它**：
- TS 类型生成器（生成 `Session` / `SessionDelegate` 类型）
- Query Engine（运行时知道 model `Session` → 表 `Session`、字段 `userId` → 列 `userId`）
- Prisma Studio

DMMF 是**模型与物理表的元数据层**。改 `@@map("foo")` → `dbName='foo'` → Query Engine 生成 SQL 用 `"foo"`。

### 七、连接信息怎么找到 PostgreSQL

`apps/server/src/lib/prisma.ts`：

```typescript
export const prisma = new PrismaClient({ log: logs });
```

**没显式传连接串**，因为读 `process.env.DATABASE_URL`。

schema.prisma 声明：

```prisma
datasource db {
  provider = "postgresql"     // 决定 SQL 方言
  url      = env("DATABASE_URL")
}
```

**懒连接**：PrismaClient 第一次查询时才建连接池、`SELECT 1` 验证。`DATABASE_URL` 没设或失败，第一次请求才报错，不是启动时报错。

### 八、为什么不需要写 SQL

ORM 核心价值对比裸 pg：

```typescript
// 裸 pg：any 类型，改字段名要改 SQL，写错列名运行时才报
const r = await pool.query('SELECT * FROM "Session" WHERE "userId" = $1', [userId]);

// Prisma：Session[] 类型完美，改字段名 generate 后编译报错
const sessions = await prisma.session.findMany({ where: { userId } });
```

Prisma 做的事：JS 对象查询 → SQL；SQL 行 → TS 对象；schema 演进影响编译期暴露。

### 九、一句话总结

```
prisma.session.findMany(...)
  → Delegate 知道 modelName='Session'
  → Query Engine 查 DMMF 找到表 "Session"
  → 生成 SQL 走连接池（DATABASE_URL 配的）
  → PostgreSQL 执行返回行
  → Query Engine 反序列化成 Session[]
```

### 关键映射规则速记

| 层 | 规则 |
|---|---|
| schema → client 属性 | `model Session` → `prisma.session`（首字母小写） |
| schema → 表名 | `model Session` → `"Session"`，`@@map` 可改 |
| schema → 列名 | 字段名同名，`@map` 可改 |
| schema → TS 类型 | `model Session` → `type Session`（同名） |
| schema → 连接 | `datasource db { url = env(...) }` |

所有映射在 **`prisma generate`** 时凝固进生成代码，运行时**零反射、零动态查找**，IDE 提示即时、性能也好。

## Q40: prisma 为什么要 migration

**问题**

prisma 为什么要 migration

**回答**

这是个**根本性的数据库工程问题**：schema 会变，但数据库里的数据不能丢。migration 就是为了解决这件事。

### 一、核心痛点：schema 是活的，数据是历史

应用代码改一行重新部署就生效，但数据库改字段名：
```prisma
model User { fullName String }   // 原来是 name
```
数据库里 100 万行数据列名叫 `name`，必须执行 `ALTER TABLE` 才能改。这条 SQL 要**有人执行 + 记录 + 保证每环境都执行 + 只执行一次**——这就是 migration 要解决的事。

**多环境放大问题**：开发 / staging / 生产数据库独立，但 schema 必须一致。没 migration = 全靠人脑记忆 + 手敲 SQL，生产事故迟早发生。

### 二、不用 migration 行不行？`prisma db push` 的致命问题

`prisma db push` 直接 diff schema 和数据库，执行 SQL，不留记录。问题：

1. **数据会丢**：`name → fullName` 改名时，diff 推不出语义，会生成 `DROP COLUMN name; ADD COLUMN fullName;`，100 万用户名字消失
2. **不可复现**：每次 push 计算当前差异，跟历史无关；线上出 bug 查不到是哪次改 schema 引入的
3. **无法 review**：SQL 不存在文件里，code review 只看到 schema 改了，看不到 `ALTER TABLE` 是否用了 `CONCURRENTLY`
4. **多人协作冲突**：A 和 B 同时 push，谁覆盖谁靠运气

**db push 只适合**早期原型 + 个人玩具 + 没有重要数据的场景。**任何上线项目都不适合**。

### 三、migration 是什么 — 本质拆解

文件结构（`apps/server/prisma/migrations/`）：

```
20260408000000_add_refresh_token/
  └── migration.sql
20260412032511_add_cc_agent_sessions/
  └── migration.sql
... (33 个文件夹)
migration_lock.toml
```

每个文件夹：目录名 `{时间戳}_{语义描述}`，时间戳保证顺序；`migration.sql` 是 SQL 文本（git 里可 review）。

一个 SQL 长这样：
```sql
CREATE TABLE "Skill" ( "id" TEXT NOT NULL, ... );

-- Prisma DSL 表达不了的，手写
CREATE UNIQUE INDEX "skill_name_active_unique"
  ON "Skill"(name) WHERE "deletedAt" IS NULL;
```

**`_prisma_migrations` 元数据表**：Prisma 在数据库里建一张表记录"跑过哪些 migration"，每次 migrate 时：
1. 读 migrations/ 目录按时间戳排序
2. 对比表，找没跑过的
3. 按顺序执行，每跑成功插入一行

保证：任何环境跑同一份 migration 后 schema 状态一致。

**`migration_lock.toml`**：锁定 `provider = "postgresql"`，防止 schema 被改成 SQLite 时 SQL 误执行。

### 四、migration 解决的核心问题

**1. 数据保留**：生成的 SQL 是文本文件可手动改，把 `DROP+ADD` 改成 `RENAME COLUMN`——`db push` 没这个机会。

**2. 跨环境一致**：本地 `migrate dev`、CI `migrate deploy`、生产 `migrate deploy`，所有环境跑同一份 SQL；`_prisma_migrations` 保证不会重复执行。

**3. 版本可追溯**：`git log` migrations/ → 每次 schema 变更都是 commit，有作者、时间、PR 链接、review 评论。

**4. Code Review 友好**：PR diff 里能看到完整 SQL，reviewer 能看到加什么表、加什么索引、外键、有没有 `CONCURRENTLY`。

**5. 复杂操作支持**：Prisma DSL 表达不了的，migration 里手写 SQL：

```sql
-- Partial Unique Index（DSL 不支持）
CREATE UNIQUE INDEX ... WHERE "deletedAt" IS NULL;
-- 触发器
CREATE TRIGGER ...;
-- 数据迁移
UPDATE "User" SET "fullName" = "firstName" || ' ' || "lastName";
-- 扩展
CREATE EXTENSION IF NOT EXISTS "pg_trgm";
```

**6. 多人协作冲突可见**：A 和 B 各自生成的 migration 文件夹不冲突，时间戳顺序明确，部署按序执行。

**7. 故障恢复**：`prisma migrate status` 查状态、`prisma migrate resolve --rolled-back xxx` 标记修复。

### 五、`migrate dev` vs `migrate deploy`

**`prisma migrate dev`**（开发用）：
- 生成新 migration 文件
- 执行所有 pending migrations
- 跑 `prisma generate` 更新 TS 类型
- 触发 seed
- **会询问是否 drop database**（开发合理，生产灾难）

**`prisma migrate deploy`**（生产用）：
- 只执行已存在的 migration，不生成新的
- 不重置数据库
- 不调用 generate（类型应已在镜像里）
- 失败就退出，幂等、确定、安全

**分开命令 = 物理隔离风险**。

### 六、这个项目的演进史

33 个 migration 就是产品业务演进史：

```
20260408 add_refresh_token              ← JWT 刷新机制
20260412 add_cc_agent_sessions          ← Session 表核心上线
20260413 add_session_status             ← 加 status 字段
20260414 add_session_total_token_usage  ← 计费字段
20260420 chat_localization_phase1       ← 本地化迁移
20260420 add_issue_tracker              ← Issue 系统
20260424 add_skillhub                   ← 技能市场（含 partial unique index 手写）
20260429 add_user_all_departments       ← 部门字段
20260525 add_user_device_api_key        ← 设备级 API Key
```

看 migration 目录就知道项目什么时候做了什么核心改动。

### 七、不用 migration 的具体灾难

**场景**：线上 100 万用户，改 `name → firstName + lastName`。

**db push**：
1. `prisma db push`
2. `DROP COLUMN name; ADD COLUMN firstName; ADD COLUMN lastName;`
3. **100 万用户名字消失**
4. 半夜恢复备份

**migration**：
1. `prisma migrate dev --name split_user_name` 生成 SQL
2. **手动改**为：
   ```sql
   ALTER TABLE "User" ADD COLUMN "firstName" TEXT;
   ALTER TABLE "User" ADD COLUMN "lastName" TEXT;
   UPDATE "User" SET "firstName" = split_part(name, ' ', 1),
                     "lastName"  = split_part(name, ' ', 2);
   ALTER TABLE "User" DROP COLUMN "name";
   ```
3. PR review 确认数据迁移
4. CI 跑 migration → 数据完整保留

**migration 把"数据丢失"从默认行为变成了"必须刻意才会发生"**。

### 八、Forward-Only 哲学

Prisma 不支持自动 down migration。原因：生产环境的回滚通常是错误的——回滚 A 删字段 phone，期间写入的数据全没。

业界共识：**Schema 改动单向，前进比回退安全**。发现 A 有问题 → 生成 migration B 修正，不破坏 A 写入的数据。

### 九、一句话总结

**Prisma 必须 migration，因为它要让 schema 的每次演进都是有记录、可 review、跨环境一致、不丢数据、可追溯的工程操作。**

| 没 migration | 有 migration |
|---|---|
| schema 改动 = 危险操作 | schema 改动 = 受控代码提交 |
| 多环境一致靠人脑 | `_prisma_migrations` 机器保证 |
| 重命名 = 数据丢失 | 手写 SQL 保数据 |
| Partial index 等做不了 | 手写 SQL 任意能力 |
| Code review 看不到 SQL | PR 里有完整 diff |
| 多人协作靠运气 | 时间戳顺序明确 |
| 出问题查不到原因 | git blame 到 commit |

`db push` 是开发期玩具，`migration` 是工程实践。任何要上线的项目都必须用 migration——这就是 Prisma 为什么有 migration，以及这个项目 33 个 migration 全在 git 里的原因。

## Q41: router.post login next 含义与路由原理

**问题**

`router.post('/login'` 中 `next` 是什么意思，这里路由的原理和流程是怎样的

**回答**

Express 路由本质是一条**中间件链**（middleware chain），`next` 是这条链的**"放行器"**。

### 一、`next` 是什么

`(req, res, next)` 是 Express 中间件函数的标准三参数签名。`next` 是 Express 传给你的一个函数，调用它意味着"我处理完了，把控制权交给下一个中间件"。

两种调用方式：
```typescript
next();        // ① 放行：进入下一个普通中间件
next(err);     // ② 带错误：跳过所有普通中间件，直接去错误处理中间件
```

项目里典型用法：
```typescript
if (!code || typeof code !== 'string') {
  next(invalidParams('缺少必填参数: code'));   // 走错误流
  return;
}
```

**`next(err)` 是 Express 的"扔错误"机制**——比直接 `throw` 更可靠（异步函数 throw 不会被 Express 自动捕获）。

### 二、Express 中间件链的工作机制

整个 app 是一个**有序的中间件数组**：

```typescript
[
  cors(),                        // 跨域
  pinoHttp(...),                 // 日志
  express.json(),                // body 解析
  app.use('/api/auth', authRouter),     // 挂载子路由
  app.use('/api/sessions', ...),
  ...
  legacyChatGone,                // 兜底
  errorHandler,                  // 错误处理（必须最后）
]
```

每次 `app.use(...)` / `router.post(...)` 都是往数组追加一项。请求进来后 Express 从第 0 项开始挨个调用。每个中间件：
- 处理完调 `next()` 继续
- 或结束响应（`res.json` / `res.send` / `res.end`）
- 或调 `next(err)` 跳到错误流

### 三、POST /api/auth/login 的完整流转

```
请求进来 POST /api/auth/login { code, codeVerifier, deviceId }
  ↓
1. cors() → 加跨域头 → next()
  ↓
2. pinoHttp() → 记录开始时间 + 生成 requestId → next()
  ↓
3. express.json() → 解析 body 到 req.body → next()
  ↓
4. app.use('/api/auth', authRouter) → 削掉 /api/auth 前缀
  ↓
5. authRouter 内 router.post('/login', handler) 方法+路径匹配
  ↓
6. handler 执行：
   - 校验 code/codeVerifier/deviceId
   - 调 authService.login(...)
   - 成功 → res.status(200).json(result)
   - 失败 → next(err) → 跳过普通中间件 → errorHandler
  ↓
7. pinoHttp 监听 res.end 触发 → 输出日志
```

### 四、为什么不直接 throw err 要用 next(err)

**Express 4 的历史包袱**：

**同步函数 throw OK**：
```typescript
router.post('/sync', (req, res) => {
  throw new Error('boom');  // ✅ Express 能捕获
});
```

**异步函数 throw 不被捕获**：
```typescript
router.post('/async', async (req, res) => {
  throw new Error('boom');  // ❌ Express 看不到
  // 变成 unhandled promise rejection，请求超时
});
```

原因：`async` throw 实际返回 rejected Promise，**Express 4 不 await handler**，拿不到 rejection。

**解法 try/catch + next(err)**：
```typescript
router.post('/login', async (req, res, next) => {
  try {
    res.json(result);
  } catch (err) {
    next(err);   // 显式交给 Express
  }
});
```

Express 5 修了这个，但这个项目用的是 4。

**同步校验也用 next(err) 不用 throw**：风格一致，避免被自己的 catch 兜住。`return` 必须——next 只是注册下一步，当前函数不会自动结束，否则会 "Cannot set headers after they are sent"。

### 五、router vs app — 路由模块化

子路由器把相关路由打包：

```typescript
// routes/auth.ts
const router = Router();
router.post('/login', ...);
router.post('/logout', authenticate, ...);
export default router;

// app.ts
app.use('/api/auth', authRouter);
```

**路径前缀削减**：`app.use(prefix, router)` 时 Express 把 prefix 削掉再传给子路由器：

```
POST /api/auth/login → app 匹配 /api/auth → url 改为 /login → 传给 authRouter
```

所以 router 内写 `/login` 不是 `/api/auth/login`——前缀由挂载位置决定。

**中间件可挂在 prefix 上**：
```typescript
app.use('/api/skills', authenticate, skillsRouter);  // 先过 authenticate
app.use('/api/auth', authRouter);                    // /login 不鉴权
app.use('/api/me', authenticate, meRouter);          // 必须登录
```

需要单 route 鉴权的（如 `/refresh-feishu`）在 handler 前直接加：
```typescript
router.post('/refresh-feishu', authenticate, async (req, res, next) => { ... });
```

### 六、router.post 的匹配逻辑

1. **方法过滤**：只匹配 POST
2. **路径匹配**：精确 / 参数 / 通配符 / 正则

```typescript
router.post('/login', ...);              // 精确
router.post('/users/:id', ...);          // 参数（req.params.id）
router.get('/files/*', ...);             // 通配
router.post(/^\/admin\//, ...);          // 正则
```

**顺序很关键**：
```typescript
router.post('/users/:id', handler1);
router.post('/users/me', handler2);      // ❌ 永远不会触发！:id 吞了 me
```

正确：具体的写前面。

### 七、req / res 关键属性

**req**：
```typescript
req.method        // 'POST'
req.url           // '/login'（prefix 削后）
req.originalUrl   // '/api/auth/login'（原始）
req.params        // URL 路径参数
req.query         // ?a=1 解析
req.body          // POST body（express.json 解析后）
req.headers       // 请求头
req.user          // ← 不是 Express 自带的！是 authenticate 中间件挂的
req.id            // pinoHttp 加的 requestId
```

**res**：
```typescript
res.status(200).json(result);    // 链式
res.setHeader('X-Foo', 'bar');
res.end();                       // 无 body 结束
```

终结性方法（json/send/end）调用后响应已发出，不能再写。

### 八、错误中间件特殊签名 — 4 参数

```typescript
// errorHandler.ts
export const errorHandler: ErrorRequestHandler = (err, _req, res, _next) => {
  if (err instanceof AppError) {
    res.status(err.statusCode).json({ error: { code: err.code, message: err.message } });
    return;
  }
  logger.error(err);
  res.status(500).json({ error: { code: 'INTERNAL_ERROR', message: '服务内部错误' } });
};
```

**Express 通过参数个数识别**：
- 3 参数 `(req, res, next)` → 普通中间件
- 4 参数 `(err, req, res, next)` → 错误中间件

通过 JS 反射 `fn.length` 拿到，**不能省略不用的参数**：

```typescript
// ❌ Express 当普通中间件，永远不会被调用
const errorHandler = (err, res) => { ... };
// ✅ 必须 4 个参数，不用也要写
const errorHandler = (err, _req, res, _next) => { ... };
```

**errorHandler 必须放最后**：中间件按顺序执行，next(err) 跳到"下一个错误中间件"。如果 errorHandler 在前面注册，位置已过，后面 next(err) 找不到它，错误冒泡到 Express 默认处理（HTML 错误页，不是 JSON）。

### 九、一句话总结

| 概念 | 本质 |
|---|---|
| 路由 | URL + 方法到 handler 的映射，内部是有序中间件数组 |
| 中间件链 | 请求按顺序穿过函数，每个调 next 放行 |
| `next()` | 放行到下一个普通中间件 |
| `next(err)` | 放行到下一个错误中间件（跳过所有普通的） |
| router | 一组路由打包，可挂某 prefix 复用 |
| `app.use(prefix, mw, router)` | 任意位置可插中间件，挂载时削路径前缀 |
| 错误中间件 | 4 参数签名，必须放最后 |
| 异步必 try/catch + next(err) | Express 4 不捕获 async rejection，绕不开 |

`/login` 的全部魔法就是：**按顺序穿过中间件，正常调 res.json，异常调 next(err)**。`next` 不是 callback、不是 promise，就是 Express 给你的"我把控制权还给你"的钩子。

## Q42: authenticate 中间件做了什么

**问题**

`app.use('/api/me', authenticate, meRouter);` 中的 `authenticate` 是什么？

**回答**

`authenticate` 是项目自己写的一个 **Express 中间件函数**，文件在 `apps/server/src/middleware/authenticate.ts`，类型是 `RequestHandler`。它在 `meRouter` 真正处理请求**之前**被执行，作用是：**校验 JWT、查出当前用户、把用户信息挂到 `req.user` 上**。挂在 `/api/me` 路径前缀，等于声明"这条线下面所有的接口都必须登录才能访问"。

**1. 它是什么类型的东西**

```ts
export const authenticate: RequestHandler = async (req, _res, next) => { ... }
```

- `RequestHandler` 是 Express 提供的标准签名 `(req, res, next) => void | Promise<void>`
- 它是**普通中间件**（3 参签名），不是错误中间件（4 参签名）
- Express 看它接受 3 个参数，就把它当作"路由前置过滤器"调用

挂载方式有两种：

```ts
// 方式 A：在路由前缀级别挂（Q41 的例子）
app.use('/api/me', authenticate, meRouter);

// 方式 B：在单个 route 上挂（auth.ts 里 /refresh-feishu、/logout 用的就是这种）
router.post('/logout', authenticate, async (req, res, next) => { ... });
```

两者效果一样：先跑 `authenticate`，通过了再跑后面的 handler。

**2. 它一步步做了什么**

源码精简版：

```ts
export const authenticate: RequestHandler = async (req, _res, next) => {
  // ① 从请求头拿 Authorization
  const header = req.headers.authorization;
  if (!header || !header.startsWith('Bearer ')) {
    next(unauthorized('缺少认证信息'));   // → 跳到 errorHandler，返回 401
    return;
  }

  // ② 去掉 "Bearer " 前缀，剩下的就是 JWT 字符串
  const token = header.slice(7);

  try {
    // ③ 用 HS256 + 密钥验签 + 检查过期时间
    const payload = await verifyAccessToken(token);

    // ④ 用 payload.sub（= user id）去 PostgreSQL 查一次用户
    const user = await prisma.user.findUnique({ where: { id: payload.sub } });
    if (!user) {
      next(userNotFound());              // 用户被删了 → 401
      return;
    }

    // ⑤ 把用户信息塞进 req.user，后续 handler 可以直接用
    req.user = {
      id: user.id,
      device: payload.device,
      name: user.name,
      email: user.email,
      // ... 还有 role / firstLevelDepartmentIds / isCanary / feishuId 等
    };

    // ⑥ 给本次请求的 logger 挂上 userId，后续 req.log.* 自动带这个字段
    if (req.log) {
      req.log = req.log.child({ userId: user.id });
    }

    // ⑦ 放行，交给下一个中间件 / route handler
    next();
  } catch (err) {
    // verifyAccessToken 抛 TOKEN_EXPIRED / INVALID_TOKEN 都会进这里
    next(err);
  }
};
```

每一步对应的 HTTP 表现：

| 情况 | 走向 | 客户端看到 |
|---|---|---|
| 没带 `Authorization` 头 | `next(unauthorized(...))` | 401 `UNAUTHORIZED` |
| 头不是 `Bearer xxx` 格式 | 同上 | 401 `UNAUTHORIZED` |
| token 过期 | `verifyAccessToken` 抛 `tokenExpired()` → `next(err)` | 401 `TOKEN_EXPIRED` |
| token 签名错/被篡改 | 抛 `invalidToken()` | 401 `INVALID_TOKEN` |
| token 合法但用户被删了 | `next(userNotFound())` | 401 `USER_NOT_FOUND` |
| 都正常 | `next()` 放行 | 走到真正的业务 handler |

注意它从不调用 `res.send()` —— **失败靠 `next(err)` 抛给 `errorHandler` 统一返回**，成功靠 `next()` 让下一个中间件接手。这跟 Q41 讲的"洋葱链 + 错误冒泡"是配套的。

**3. `req.user` 是怎么"凭空多出来"的**

注意 `req.user = { ... }` 这一句——Express 自带的 `req` 类型里**没有** `user` 字段。这是项目通过 **TypeScript declaration merging** 给 Express 的 `Request` 接口扩展出来的（通常在 `apps/server/src/types/express.d.ts` 或类似文件里）：

```ts
declare global {
  namespace Express {
    interface Request {
      user?: { id: string; device: string; name: string; /* ... */ };
    }
  }
}
```

这样：

- 在 `authenticate` 里赋值不会被 TS 报错
- 在下游 handler 里 `req.user!.id`（注意感叹号——TS 不知道 authenticate 已经赋过值了，所以要断言非空）就能直接拿到

`auth.ts` 里的 `req.user!.id` 就是依赖这个机制：

```ts
router.post('/logout', authenticate, async (req, res, next) => {
  // 上面 authenticate 已经把 req.user 填好了，这里直接用
  await authService.logout(req.user!.id, deviceId);
});
```

**4. 为什么要抽成一个中间件，而不是在每个 handler 里都写一遍**

如果不抽，每个需要登录的接口都得手写：

```ts
const header = req.headers.authorization;
if (!header) return res.status(401)...;
const payload = await verifyAccessToken(...);
const user = await prisma.user.findUnique(...);
// ... 然后才是业务
```

20 个接口就要重复 20 遍，改一处要改 20 处。抽成中间件之后：

- **复用**：一行 `authenticate` 接到任何 route 上
- **统一**：错误码、token 格式、用户查询逻辑只有一处实现
- **声明式**：看 `app.use('/api/me', authenticate, meRouter)` 就知道这一片是登录态保护区
- **可组合**：以后加 `requireRole('admin')`、`requirePermission(...)` 也是同样套路，串成一条"前置过滤链"

这是 Express 中间件模式最经典的应用场景之一——**横切关注点（cross-cutting concern）**。

**5. 几个值得注意的设计细节**

① **`req.log = req.log.child({ userId })`**
pinoHttp 给每条请求生成了一个带 `requestId` 的子 logger（见 `app.ts` 的 `pinoHttp` 配置）。authenticate 又在它基础上再 child 一层，加上 `userId`。后续业务里 `req.log.info('...')` 输出的日志会自动带 `requestId + userId + method + url`，排查问题不需要手动拼。

② **`prisma.user.findUnique` 每次都查库**
有人会问：JWT 里已经有 `sub`（user id）了，为什么还要查一次？
- 拿到 user 实体后能拿到最新的 `name / role / defaultModel / isCanary` 等字段，避免 JWT 里塞太多易变信息
- 顺便确认"用户没被删"——签发 token 后管理员把用户禁用/删除了，也能立刻拦掉
- 代价是每个请求多一次 PG 查询，量大时通常会改成"短 TTL 内存缓存"，本项目目前选择了简单可靠

③ **不在这里区分 401/403**
authenticate 只管"是不是登录的"（401），不管"有没有权限做这件事"（403）。后者由各 route 自己（或专门的 `requireRole` 中间件）判断。职责单一。

④ **`_res` 下划线前缀**
TypeScript 风格——参数没用到但又必须出现在签名里，加 `_` 前缀告诉 lint "我知道没用，别报 unused"。

**一句话总结**

`authenticate` 是一个挂在 Express 请求链里的"门卫"中间件：**没 token / token 烂的直接 401 打回去；token 合法的，从 PG 把当前用户查出来塞到 `req.user`，然后放行**。这样下游每个 handler 都能假设"我能跑到这里，说明用户已登录、`req.user` 一定有值"，业务代码可以专心写业务。

## Q43: JWT 是什么

**问题**

JWT 是个什么东西？

**回答**

JWT = **JSON Web Token**，RFC 7519 规范。一句话：**一段用密码学签过名的 JSON 字符串**，服务器签发给客户端，客户端之后每次请求都带回来，服务器只要验签就能信任里面的内容——**不用查数据库就能知道你是谁**。

本项目用它做"短时登录态"：用户用飞书 OAuth 登录后，server 发一个 JWT（叫 `accessToken`），1 小时内有效；客户端把它放在请求头里 `Authorization: Bearer xxx`，server 的 `authenticate` 中间件验完签就知道 `req.user` 是谁。

**1. 长什么样**

JWT 是三段 base64url 编码字符串，用 `.` 连接：

```
eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiJjbHkxMjMiLCJkZXZpY2UiOiJtYWMtMDAxIiwiaWF0IjoxNzAwMDAwMDAwLCJleHAiOjE3MDAwMDM2MDB9.k3rGv6...
└────── Header ──────┘ └─────────────── Payload ────────────────────┘ └── Signature ──┘
```

base64url 解开后：

| 部分 | 内容（JSON） | 谁决定 |
|---|---|---|
| **Header** | `{"alg":"HS256","typ":"JWT"}` | 签发方选算法 |
| **Payload** | `{"sub":"cly123","device":"mac-001","iat":1700000000,"exp":1700003600}` | 业务自己塞 |
| **Signature** | `HMAC_SHA256(base64url(header) + "." + base64url(payload), 密钥)` | 算出来的 |

**关键认知**：
- Header 和 Payload **只是 base64 编码，不是加密**——任何人拿到 token 都能看见里面写了什么（去 https://jwt.io 贴一下就能解码）
- 真正起作用的是 **Signature**：篡改 Header 或 Payload 任何一个字符，Signature 就对不上了，验签会失败
- 所以 JWT 解决的是 **"不可伪造"，不是"不可读"**。**绝对不要把密码、手机号明文等敏感信息塞进 payload**

**2. 签名/验签的数学（HS256 版本）**

本项目用 `HS256` = **HMAC-SHA256**，对称密钥算法：

```
签名 = HMAC_SHA256(
  base64url(header) + "." + base64url(payload),
  SECRET   ← 只有 server 知道的密钥
)
```

- **签**：server 用密钥算出 signature，拼成 token 发给客户端
- **验**：server 拿到客户端送回来的 token，用同样的密钥重新算一遍 signature，和 token 里的对比
  - 一致 → 这 token 确实是我签的，没被改过
  - 不一致 → 要么不是我签的，要么被篡改了 → 拒绝

因为是**对称密钥**，只有 server 有 SECRET，客户端拿不到，所以客户端只能"使用" token，不能"伪造" token。

> 还有 RS256/ES256（非对称）——用私钥签、公钥验。适合多方场景（OAuth、SSO），任何持有公钥的服务都能验，但只有持私钥的能签。本项目是单 server 自签自验，用 HS256 足够。

**3. 标准字段（Registered Claims）**

JWT 规范规定了一些"保留字段名"，本项目也用了：

| 字段 | 全称 | 含义 | 本项目值 |
|---|---|---|---|
| `sub` | subject | 主体，通常是 user id | `user.id`（CUID） |
| `iat` | issued at | 签发时间（Unix 秒） | 当前时间 |
| `exp` | expiration | 过期时间（Unix 秒） | 签发时间 + 1h |
| `iss` | issuer | 签发方 | 本项目未用 |
| `aud` | audience | 接收方 | 本项目未用 |
| `nbf` | not before | 生效时间 | 本项目未用 |
| `jti` | JWT ID | 唯一 ID | 本项目未用 |

业务自定义字段可以随便加，本项目还加了一个 `device`（设备标识），方便区分"同一用户在不同设备的登录态"。

**4. 本项目里 JWT 的完整流程**

签发（`lib/jwt.ts` 的 `signAccessToken`）：

```ts
export async function signAccessToken(payload: AccessTokenPayload): Promise<string> {
  return new SignJWT({ device: payload.device })   // 自定义 claim
    .setProtectedHeader({ alg: 'HS256' })          // header
    .setSubject(payload.sub)                       // sub = userId
    .setIssuedAt()                                 // iat = 现在
    .setExpirationTime(config.jwt.accessExpiresIn) // exp = 现在 + '1h'
    .sign(config.jwt.secret);                      // HMAC-SHA256 签名
}
```

调用方在 `services/auth.ts` 的 login：
```ts
const accessToken = await signAccessToken({ sub: user.id, device: deviceId });
// 把 accessToken 返回给客户端
```

验证（`lib/jwt.ts` 的 `verifyAccessToken`）：

```ts
export async function verifyAccessToken(token: string): Promise<AccessTokenPayload> {
  try {
    const { payload } = await jwtVerify(token, config.jwt.secret, {
      algorithms: ['HS256'],   // ★ 强制只接受 HS256，防"alg: none"攻击
    });
    return { sub: payload.sub as string, device: payload.device as string };
  } catch (err) {
    if (err instanceof errors.JWTExpired) throw tokenExpired();   // exp 过期
    throw invalidToken();                                          // 签名/格式错
  }
}
```

`authenticate` 中间件每个请求都会调一次 `verifyAccessToken`（Q42 讲过）。

**5. 密钥从哪儿来**

`apps/server/src/config.ts`：

```ts
const JWT_SECRET_RAW = requireEnv('JWT_SECRET');   // 从环境变量读

jwtSecretBytes = new Uint8Array(Buffer.from(JWT_SECRET_RAW, 'base64'));
if (jwtSecretBytes.length === 0) throw new Error('Empty secret');
```

- `JWT_SECRET` 是 **base64 字符串**，写在 `.env`，绝不入 git
- 解码成 `Uint8Array` 作为 HMAC 的 key
- 这个 secret **泄露 = 全站登录态完蛋**，任何人都能伪造任意用户的 token

**6. JWT vs Session（传统方案）的对比**

| 维度 | 传统 Session | JWT |
|---|---|---|
| 状态保存 | server 端（DB/Redis） | 客户端（token 自带信息） |
| 验证 | 查 session store | 算 HMAC（CPU 快，无 IO） |
| 横向扩展 | 多机要共享 session 存储 | 任何机器只要有 SECRET 都能验 |
| 撤销 | 删 session 行立即生效 | **难** —— token 没到 exp 就一直有效 |
| 携带数据 | session id（小） | 整个 payload（KB 级） |

JWT 的天然短板就是**"签出去就收不回"**——这也是为什么本项目要配合"刷新令牌（refresh token）"和短 accessExpiresIn（1h）来用：

- access token：JWT，1 小时过期，验签快、能扩展
- refresh token：**存在 PG 的 `RefreshToken` 表里**（Q31 那个 upsert），随时能删
- 客户端 access token 过期了，就拿 refresh token 去 `/api/auth/refresh` 换新的
- 真要踢人下线？删 RefreshToken 行就行，最多 1 小时后他的 access token 也失效

这就是行业标准的 **access + refresh 双 token 模式**。

**7. 常见坑**

① **`alg: none` 攻击**
早期 JWT 库会接受 header 里 `"alg":"none"` 的 token（不用签名）。攻击者可以构造一个 `alg:none` 的假 token 骗过验证。本项目 `verifyAccessToken` 里写了 `algorithms: ['HS256']`，强制只认 HS256，杜绝这种攻击——**永远要白名单 algorithm**。

② **payload 不要塞敏感信息**
任何人拿到 token 就能 base64 解码看到 payload。手机号、邮箱可以放，密码、银行卡、session id **绝对不行**。

③ **`exp` 是必须的**
没有过期时间的 JWT 等于永久有效，泄露一次毁一辈子。本项目固定 1h。

④ **时钟漂移**
exp 是 server 算的，验 token 时如果 client 和 server 时钟差超过几分钟，可能"刚签出来就被判过期"。一般库默认有 ±5s 容差，jose 也是。

⑤ **存在哪里？**
浏览器端：放 `localStorage` 易被 XSS 偷；放 `HttpOnly Cookie` 安全但要防 CSRF。本项目是 Electron 桌面端，存在 `electron-store`/本地文件里，加上系统级隔离，攻击面比浏览器小。

**一句话总结**

JWT 就是 **"一段 server 用密钥签过名的 JSON"**：客户端拿着它当通行证，server 每次只用做一次 HMAC 校验就能信任里面写的 user id，不用查 session、可以多机部署。代价是**签出去的 token 在过期前撤不掉**，所以工程上几乎都要配 refresh token + 短过期时间一起用——本项目就是这么做的。

## Q44: access + refresh 双 token 模式详解

**问题**

行业标准的 access + refresh 双 token 模式 详细介绍。

**回答**

**为什么要"两个 token"**

这套模式的诞生只为解决 JWT 的一个根本矛盾：

- 想要**性能 + 可扩展**（验签不查库、多机部署）→ 用 JWT
- 但 JWT 一旦签出去**到期前撤不掉** → 安全隐患（token 泄露后只能任其作妖）

折中方案：
- 把"门票"做成 **短命**（几分钟到 1 小时）—— 就算泄露也很快失效
- 但短命门票会让用户**频繁过期掉线** —— 再发一个**长命的"换票券"**
- "换票券"是有状态的（存 DB），能随时撤销

→ 短命+无状态 的 **access token** 负责日常请求，长命+有状态 的 **refresh token** 负责续命。**两者职责严格分离**，互不替代。

**两个 token 的对比**

| 维度 | Access Token | Refresh Token |
|---|---|---|
| **生命周期** | 短（本项目 1h，业界 5min~1h） | 长（本项目 365 天） |
| **形式** | JWT，自带 payload | 不透明字符串（本项目 UUID） |
| **存在哪** | 客户端内存/本地存储 | 客户端本地存储 + server DB |
| **验证方式** | HMAC 验签，无 IO | 查 DB（按 hash） |
| **能否撤销** | ❌ 到期前撤不掉 | ✅ 删 DB 行立即失效 |
| **谁验** | 每一个业务请求 | 只有 `/api/auth/refresh` 接口 |
| **传输频率** | 每请求带一次 | 只在 access 过期时带一次 |
| **被偷的代价** | 1 小时内任意调用 | 365 天可换无限 access |

可以这样记：**access token = 一小时门票（验起来快）；refresh token = 一年的会员卡（撤起来快）**。

**完整生命周期（本项目实现）**

```
┌─ ① 登录 ────────────────────────────────────────────────────┐
│  飞书 OAuth code → server → exchangeCodeForToken            │
│  → upsert user                                              │
│  → signAccessToken({ sub: userId, device })  ← JWT (1h)     │
│  → generateRefreshToken() = randomUUID()      ← 长字符串    │
│  → tokenHash = sha256(refreshToken)                         │
│  → refreshToken.upsert({ userId_deviceId },                 │
│       { tokenHash, expiresAt: now + 365d })   ← 存 DB       │
│  返回: { accessToken, refreshToken (明文), user, ... }      │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼ 客户端保存 access + refresh
┌─ ② 业务请求（1 小时内） ────────────────────────────────────┐
│  GET /api/me/issues                                         │
│    Authorization: Bearer <accessToken>                      │
│  → authenticate 中间件                                       │
│  → verifyAccessToken(token)  ← 只算 HMAC，零 DB IO          │
│  → req.user 挂上，放行                                       │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼ 1 小时后
┌─ ③ access 过期，客户端收到 401 TOKEN_EXPIRED ──────────────┐
│  客户端拦截器：调 POST /api/auth/refresh                    │
│    body: { refreshToken, deviceId }                         │
│  → tokenHash = sha256(refreshToken)                         │
│  → 查 RefreshToken 表                                        │
│    - 没查到 → INVALID_REFRESH_TOKEN                          │
│    - deviceId 不匹配 → DEVICE_MISMATCH                       │
│    - expiresAt 过期 → 删行 + REFRESH_TOKEN_EXPIRED          │
│  → ★ 删旧 refreshToken 行                                    │
│  → 签新 accessToken                                          │
│  → 生成新 refreshToken（UUID） + 存 DB                       │
│  返回: { accessToken: 新, refreshToken: 新, ... }           │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼ 客户端用新 token 重发刚才的请求
                              │
                              ▼ ... 循环 ②③ ...
                              │
┌─ ④ 登出 ────────────────────────────────────────────────────┐
│  POST /api/auth/logout  (authenticate 保护)                 │
│  → refreshToken.deleteMany({ userId, deviceId })            │
│  refresh token 立即失效，access token 自然死等过期           │
└─────────────────────────────────────────────────────────────┘
```

**对应的项目代码（`apps/server/src/services/auth.ts`）**

签发（login，第 114-123 行）：
```ts
const accessToken = await signAccessToken({ sub: user.id, device: deviceId });
const rawRefreshToken = generateRefreshToken();                    // UUID
const tokenHash = hashToken(rawRefreshToken);                      // sha256
const expiresAt = new Date(Date.now() + 365 * 86_400_000);         // +365 天

await prisma.refreshToken.upsert({
  where: { userId_deviceId: { userId: user.id, deviceId } },       // 复合唯一键
  create: { tokenHash, deviceId, userId: user.id, expiresAt },
  update: { tokenHash, expiresAt, createdAt: new Date() },         // 复登录时覆盖
});
```

刷新（refresh，第 173-244 行）：
```ts
const tokenHash = hashToken(refreshToken);
const record = await prisma.refreshToken.findUnique({ where: { tokenHash } });

if (!record) throw invalidRefreshToken();                          // 不存在
if (record.deviceId !== deviceId) throw deviceMismatch();          // 设备不匹配
if (record.expiresAt < new Date()) {
  await prisma.refreshToken.delete({ where: { id: record.id } });
  throw refreshTokenExpired();
}

await prisma.refreshToken.delete({ where: { id: record.id } });    // ★ 删旧
const accessToken = await signAccessToken({ sub: record.userId, device: deviceId });
const newRawToken = generateRefreshToken();
await prisma.refreshToken.create({                                 // ★ 建新
  data: { tokenHash: hashToken(newRawToken), deviceId, userId: record.userId, expiresAt },
});
```

登出（logout）：
```ts
await prisma.refreshToken.deleteMany({ where: { userId, deviceId } });
```

**几个关键设计决策（本项目都做到了，每个都有讲究）**

**① Refresh Token Rotation（轮转）**

每次刷新都**销毁旧 refresh token、生成新 refresh token**——而不是返回同一个。
- **好处**：refresh token 真正变成"一次性"的，泄露后用一次就废
- 本项目实现：`refresh()` 里先 `delete` 旧的，再 `create` 新的
- 行业默认推荐做法（OAuth 2.0 Security BCP, RFC 6749 第 10.4 节）

**② Refresh Token 以 hash 形式存 DB（不存明文）**

```ts
const tokenHash = hashToken(rawRefreshToken);    // sha256
// 存 tokenHash，不存 rawRefreshToken
```

- 跟"密码不存明文存 bcrypt"是同一思路
- DB 被拖库后，攻击者拿到的是 hash，**反推不出原 token**，没法去 `/refresh` 换 access
- 验证时：客户端送来 raw token → server 算 sha256 → 拿 hash 去查

> ⚠️ 这里用 sha256 而不是 bcrypt：因为 refresh token 本身就是 128-bit UUID（高熵随机数），不存在"密码弱口令字典攻击"问题，sha256 足够；密码用 bcrypt 是因为人选的密码熵太低，必须靠慢哈希 + salt 拖时间。

**③ 设备维度绑定（userId + deviceId 复合唯一键）**

```prisma
@@unique([userId, deviceId])    // 每个用户每个设备只一行
```

- 同一用户在 mac/win/手机上登录 → 三行 RefreshToken，互不影响
- 一个设备重新登录 → upsert 覆盖该设备的行
- 撤销时可以做"踢这个设备下线" vs "踢全部设备下线"
- refresh 接口校验 `deviceId` 一致：攻击者偷了 mac 的 refresh token 想在自己设备上用，提供的 deviceId 对不上就 `DEVICE_MISMATCH`

**④ 两个错误码区分对待**

| 错误码 | 含义 | 客户端怎么办 |
|---|---|---|
| `TOKEN_EXPIRED` | access 过期 | 自动调 `/refresh`，无感 |
| `INVALID_TOKEN` | access 烂的 | 跳登录页 |
| `REFRESH_TOKEN_EXPIRED` | refresh 也过期了 | 跳登录页（365 天没用过） |
| `INVALID_REFRESH_TOKEN` | refresh 不存在 | 跳登录页 |
| `DEVICE_MISMATCH` | refresh 设备对不上 | 跳登录页 + 报警 |

客户端拦截器一般这样：
```
请求 → 401 TOKEN_EXPIRED → 调 refresh → 成功 → 重发原请求
                                      → 失败 → 跳登录
```

**⑤ 关于"refresh token 复用检测"（本项目暂未实现，值得知道）**

更高级的玩法（OAuth 2.1 推荐）：
- 给 refresh token 加一个 `family_id`，同一登录链上的所有 refresh token 同属一个 family
- 一旦发现**已被使用过（已轮转）的 refresh token 又被用了一次** → 说明被偷了 → **整族吊销**
- 本项目目前没做：旧 refresh 删了再用就只是 `INVALID_REFRESH_TOKEN`，无法区分"是攻击者还是正常网络重试"

如果将来要做盗用检测，模型大概是这样：
```
RefreshToken {
  familyId      String   // 同一登录的所有 refresh 共享
  parentId      String?  // 上一个 refresh 的 id
  isRevoked     Boolean  @default(false)
  ...
}
// 命中"已 delete 的 refresh"被使用 → 整 familyId 全部 isRevoked = true
```

**⑥ 过期时间怎么选**

本项目：
- access：1 小时（`JWT_ACCESS_EXPIRES=1h`）
- refresh：365 天（`JWT_REFRESH_EXPIRES_DAYS=365`）

行业经验：

| access TTL | refresh TTL | 适用 |
|---|---|---|
| 5-15 min | 1-7 天 | 高敏感（银行、政府） |
| 1 h | 30-90 天 | 一般 SaaS |
| 1 h | 365 天 | 桌面/移动客户端，重用户体验 ← 本项目 |

权衡：
- access 越短 → 泄露窗口越小，但刷新频率越高
- refresh 越长 → 用户越不容易掉线，但被偷代价越大
- 桌面客户端用户不希望每月重新登录飞书 → 365 天

**⑦ Logout 只删 refresh，不动 access**

```ts
async logout() {
  await prisma.refreshToken.deleteMany({ where: { userId, deviceId } });
}
```

- access token 自然死等过期（最多 1 小时）
- refresh 立刻没了 → 想再续命没门
- 这是"最大 1 小时的不一致"——可接受的代价，换来不需要维护 access token 黑名单（否则就退化成 Session 模式了）

如果业务需要"登出后所有 access 立即失效"（管理后台、金融），那只能：
- 引入 token 黑名单（Redis 存被撤销的 jti）
- 或者降低 access TTL 到 1-5 min
- 或者干脆回到 Session 模式

**⑧ access 不能写敏感操作必备**

某些敏感操作（修改密码、转账）会要求**重新输入密码**或**步进认证（step-up auth）**，因为 access token 可能是几十分钟前签的，期间用户离开过座位。本项目目前没这个场景。

**和飞书 token 的关系（本项目特别情况）**

本项目同时管理**两套** token：

| | 本项目自己的 | 飞书的 |
|---|---|---|
| access | JWT，1h | feishuAccessToken（飞书发的） |
| refresh | UUID 存 PG，365d | feishuRefreshToken（飞书发的） |
| 用途 | 调本项目自己的 API | 代用户调飞书 API（文档、Bitable 等） |
| 刷新接口 | `/api/auth/refresh` | `/api/auth/refresh-feishu` |

两套 token 互相独立，飞书 token 过期了不影响本项目登录态，反之亦然。前者由 server 完全控制（可自由设置 TTL/撤销策略），后者听飞书的（TTL 飞书定，刷新得调飞书接口）。

**对比常见替代方案**

| 方案 | 撤销能力 | 性能 | 多机扩展 | 评价 |
|---|---|---|---|---|
| 纯 Session（DB/Redis） | ✅ | ❌ 每请求查 store | ⚠️ 需共享 store | 简单可靠，但慢 |
| 纯 long-lived JWT | ❌ | ✅✅ | ✅ | 撤不掉，重大安全隐患 |
| **access + refresh** | ⚠️ 最长 1h 不一致 | ✅ | ✅ | **业界主流** |
| access + 黑名单 | ✅ | ⚠️ 每请求查黑名单 | ⚠️ 需共享黑名单 | 撤销强但失去 JWT 优势 |
| Opaque token + introspection | ✅ | ❌ 每请求 introspect | ⚠️ | OAuth 2.0 server-to-server |

**一句话总结**

access + refresh 双 token 是为了**把"快速验证"和"可撤销"两个矛盾的需求拆给两个 token 分别承担**：access 是短命 JWT（验签无 IO、多机可扩展、泄露窗口短），refresh 是长命有状态字符串（存 DB 可随时撤销、轮转后一次性使用）。本项目在此基础上加了**设备维度绑定 + sha256 哈希存储 + 每次刷新轮转**三件标配的安全增强，唯一没做的是"复用检测+ family 吊销"，留作后续可演进点。

## Q45: RefreshToken relation 与索引怎么理解

**问题**

```prisma
RefreshToken {
  user      User     @relation(fields: [userId], references: [id], onDelete: Cascade)
  @@unique([userId, deviceId])
  @@index([userId])
}
```
怎么理解？

**回答**

这三行是 `RefreshToken` 模型上三件**独立但配套**的约束：**外键关系、复合唯一约束、辅助索引**。完整模型长这样（`apps/server/prisma/schema.prisma:93-104`）：

```prisma
model RefreshToken {
  id        String   @id @default(cuid())
  tokenHash String   @unique           // 单列唯一：refresh token 必须全局唯一
  deviceId  String
  userId    String
  user      User     @relation(fields: [userId], references: [id], onDelete: Cascade)
  expiresAt DateTime
  createdAt DateTime @default(now())

  @@unique([userId, deviceId])         // 复合唯一：每个用户每个设备只一行
  @@index([userId])                    // 辅助索引：加速 by userId 的查询
}
```

下面拆开讲。

---

**① `user User @relation(...)` —— 外键 + 关系导航**

```prisma
user      User     @relation(fields: [userId], references: [id], onDelete: Cascade)
```

这一行**做了两件事**：

**(a) 在数据库层建外键约束**

翻译成 SQL 大致是：
```sql
ALTER TABLE "RefreshToken"
  ADD CONSTRAINT "RefreshToken_userId_fkey"
  FOREIGN KEY ("userId") REFERENCES "User"("id")
  ON DELETE CASCADE
  ON UPDATE CASCADE;
```

- `fields: [userId]`：本表的 `userId` 列是外键
- `references: [id]`：指向 `User` 表的 `id` 列
- `onDelete: Cascade`：**父行被删，子行级联一起删** ← 这是关键

具体行为：
```sql
DELETE FROM "User" WHERE id = 'cly123';
-- PG 自动同步执行：
-- DELETE FROM "RefreshToken" WHERE userId = 'cly123';
-- DELETE FROM "Session"      WHERE userId = 'cly123';  -- 其他 onDelete: Cascade 的子表也跟着
```

如果不写 `onDelete: Cascade`（默认是 `Restrict`），删用户会被外键拦住报错——因为还有 RefreshToken 引用着这个 user。

为什么 RefreshToken 选 Cascade？登录态本就依附于用户存在，**用户都没了，refresh token 留着只是垃圾数据**，跟着删干净。对比一下 Q36 讲过的：`Issue.assignee` 用 `SetNull`（用户走了工单还在，只是处理人字段置空），不同业务语义选不同策略。

**(b) 在 Prisma 客户端层提供"导航属性"**

```ts
const rt = await prisma.refreshToken.findUnique({
  where: { tokenHash },
  include: { user: true },     // ← 因为有 user 关系，可以 include 出来
});
console.log(rt.user.name);     // 直接拿到关联的 User 对象
```

也支持反向：
```prisma
// User 模型那边对应有
refreshTokens  RefreshToken[]   // 一个用户多条 refresh
```
```ts
const u = await prisma.user.findUnique({
  where: { id },
  include: { refreshTokens: true },
});
```

注意：**`user` 这一行本身在数据库里不是一列**，物理上只有 `userId` 这个 String 列。`user` 是 Prisma 在 ORM 层虚构出来的"对象引用"，让你写 JS 时面向对象。

---

**② `@@unique([userId, deviceId])` —— 复合唯一约束**

```prisma
@@unique([userId, deviceId])
```

翻译成 SQL：
```sql
CREATE UNIQUE INDEX "RefreshToken_userId_deviceId_key"
  ON "RefreshToken" ("userId", "deviceId");
```

含义：**`(userId, deviceId)` 这对组合在全表必须唯一**——同一用户在同一设备上最多只能有一行 RefreshToken。

- 同一用户在不同设备：`(u1, mac)`、`(u1, win)`、`(u1, iphone)` → 三行，✅
- 不同用户在同一设备：`(u1, mac)`、`(u2, mac)` → 两行，✅
- 同一用户在同一设备：`(u1, mac)`、`(u1, mac)` → ❌ 唯一约束报错

这条约束是为了配合 `auth.ts:119` 的 upsert 工作：

```ts
await prisma.refreshToken.upsert({
  where: { userId_deviceId: { userId: user.id, deviceId } },  // ← 用的就是这个复合唯一键
  create: { tokenHash, deviceId, userId: user.id, expiresAt },
  update: { tokenHash, expiresAt, createdAt: new Date() },
});
```

注意 where 子句的字段名 `userId_deviceId` ——Prisma 自动把复合唯一键的列名用 `_` 连起来，作为 where 入口。**没有这个 `@@unique`，就没有 `userId_deviceId` 这个查询入口**，upsert 也写不出来。

**业务效果**：
- 用户在 mac 上第二次登录 → upsert 命中已有行 → **覆盖** tokenHash（旧的 refresh 立刻失效）
- 用户在新设备 win 上登录 → upsert 没命中 → **插入新行**，mac 上的登录不受影响

> Q36 题目里讲过：`@@unique` 不仅是约束，还**顺带是个复合索引**。所以 `WHERE userId = ? AND deviceId = ?` 的查询会用上这个索引，无需再额外建。

---

**③ `@@index([userId])` —— 单独的辅助索引**

```prisma
@@index([userId])
```

翻译成 SQL：
```sql
CREATE INDEX "RefreshToken_userId_idx" ON "RefreshToken" ("userId");
```

**和 `@@unique([userId, deviceId])` 不重复吗？**——这是个好问题，关键在于**复合索引的"最左前缀"特性**。

复合索引 `(userId, deviceId)` 内部排序大致是：
```
(u1, iphone)
(u1, mac)
(u1, win)
(u2, mac)
(u2, win)
...
```
- `WHERE userId = 'u1' AND deviceId = 'mac'` → ✅ 能用（命中复合索引）
- `WHERE userId = 'u1'` → ✅ 也能用（最左前缀，相当于扫一段连续区间）
- `WHERE deviceId = 'mac'` → ❌ 用不了（跳过了 userId 这个最左列）

**那为什么还要单独建一个 `@@index([userId])`？**

理论上单查 userId 已经能蹭复合唯一索引的最左前缀，**确实存在冗余**。但项目里这么写有几个可能的考量：

| 原因 | 解释 |
|---|---|
| **保险/显式声明** | 让"按 userId 查询是热路径"这件事在 schema 里看得一清二楚，未来重构唯一键时不会顺手把性能干没 |
| **B+Tree 单列索引更窄** | `(userId)` 索引每行只存 userId + rowid；复合索引每行还要带 deviceId。**单列索引更小，扫描更快**，PG 优化器在某些查询下会优先选它 |
| **配合级联删除** | 删用户时触发 `ON DELETE CASCADE`，PG 需要按 userId 反查所有 RefreshToken 行，单列索引在这种场景下命中率高 |
| **逻辑/物理解耦** | `@@unique` 是业务约束（"每用户每设备一行"），`@@index` 是性能优化。哪天业务变了把 unique 拆掉，索引还在 |

具体本项目用到 `userId` 单列查询的地方，最典型就是 logout：

```ts
// services/auth.ts:247
await prisma.refreshToken.deleteMany({
  where: { userId, deviceId },
});
```

虽然这里同时带了 deviceId，但理论上"踢用户全设备下线"也只用 userId：
```ts
await prisma.refreshToken.deleteMany({ where: { userId } });
```

外加 `User -> RefreshToken[]` 反向 include 也是按 userId 拉一批，单列索引在这种"一对多查多端"的场景上确实更合适。

---

**三者的关系（一张图捋清）**

```
RefreshToken 表：
┌─────────────────────────────────────────────────────────────────────┐
│  id  │  tokenHash  │  userId  │  deviceId  │  expiresAt  │  ...    │
└─────────────────────────────────────────────────────────────────────┘
            │             │           │
            │             │           └─ 配合 userId 组成复合唯一键 ──┐
            │             │                                            │
            │             ├─ 外键 → User.id (ON DELETE CASCADE) ───┐   │
            │             │                                        │   │
            │             └─ 单列 @@index([userId]) ──┐           │   │
            │                                          │           │   │
            └─ @unique 单列唯一：refresh hash 不能重复 │           │   │
                                                       ▼           ▼   ▼
                                          按 userId 查最快   级联   upsert
                                          (logout, 反查)    删除    入口
```

---

**类比记忆**

把 `RefreshToken` 表想象成一栋"宿舍楼登记簿"：
- **外键 `user`** = 每条登记必须挂在某个住户名下；住户搬走（cascade），他名下的所有登记一起销毁
- **`@@unique([userId, deviceId])`** = 同一住户的同一把钥匙（设备）只能登记一次；换钥匙就覆盖旧的
- **`@@index([userId])`** = 一本"按住户姓名快速翻"的目录册；想知道某住户名下还有哪些登记，翻这本目录最快

---

**一句话总结**

`@relation(... onDelete: Cascade)` 建外键 + 给 Prisma 客户端虚构关系导航 + 用户删了 refresh token 跟着删；`@@unique([userId, deviceId])` 用复合唯一键保证"每用户每设备一行"并提供 upsert 入口；`@@index([userId])` 是为了让"按用户查/反向关联拉/级联删"这类只走 userId 的高频路径用上更窄的单列索引。三件事各管一个方面，叠加起来才是这张表完整的"约束 + 性能"画像。

## Q46: api:request IPC handler 自动 refresh 怎么理解

**问题**

```ts
ipcMain.handle('api:request', async (_event, params) => {
  const method = params.method ?? 'GET';
  const result = await doApiFetch(params.path, method, params.body);

  // Auto-refresh on TOKEN_EXPIRED and retry once
  if (result.status === 401) {
    const errData = result.data as { error?: { code?: string } } | null;
    if (errData?.error?.code === 'TOKEN_EXPIRED') {
      const refreshed = await authManager.refresh();
      if (refreshed) {
        return doApiFetch(params.path, method, params.body);
      }
    }
  }
  return result;
});
```
怎么理解？

**回答**

这是 Electron 主进程里注册的一个 **IPC handler**，名字叫 `api:request`，作用是**接住渲染进程发起的所有 HTTP API 调用**，集中做"带 Bearer token、发请求、收 401 自动刷 token、重试一次"。它是 Q44 那套 access+refresh 双 token 模式在**客户端这一侧的拦截器实现**。

文件位置：`apps/desktop/src/main/bootstrap-electron.ts:2500-2522`。

---

**1. 这段代码所在的"地方"——Electron IPC**

Electron 有两类进程：
- **main 进程**（Node.js 环境）：管窗口、文件、网络、原生能力
- **renderer 进程**（Chromium 沙箱）：跑 UI（React）

两者之间不能直接调函数，要通过 **IPC（进程间通信）** 喊话。本项目所有"网络请求 + token 存取"都按规范放在 main 进程（CLAUDE.md 设计规范 #1），renderer 不能直接 fetch、不能直接看到 access token。

`ipcMain.handle('频道名', handler)` 注册一个**可被渲染端 `invoke` 调用的 RPC**：

```
renderer 端（React）       preload 桥                  main 进程
─────────────────         ─────────────              ──────────────
window.electronAPI                                     ipcMain.handle(
  .apiRequest({...})  →   ipcRenderer.invoke(  →       'api:request',
                            'api:request', ...)         async (event, params) => {...}
                                                       )
                          ← Promise<返回值> ←
```

对应的 preload 桥在 `apps/desktop/src/preload/preload.ts:621`：
```ts
apiRequest: (params: { path, method?, body? }) =>
  ipcRenderer.invoke('api:request', params),
```

→ React 组件里写 `window.electronAPI.apiRequest({ path: '/api/me/issues' })`，背后走的就是这个 IPC handler。

---

**2. handler 签名拆解**

```ts
ipcMain.handle(
  'api:request',
  async (
    _event: Electron.IpcMainInvokeEvent,                // ← 哪个渲染窗口发的
    params: { path: string; method?: string; body?: unknown },
  ): Promise<{ ok: boolean; status: number; data: unknown }> => { ... }
);
```

- 第一个参数 `_event`：Electron 自动传，包含 sender、frame 等。这里用不到，下划线表示忽略
- 第二个参数 `params`：渲染端 `invoke` 时传过来的对象
- 返回类型 `Promise<{ok, status, data}>`：会被序列化后送回渲染端，渲染端的 `await invoke(...)` 就拿到这个值

`method` 默认 `'GET'`——业务里很多读接口不用传 method，省得每次写。

---

**3. 真正发请求的 `doApiFetch`**

注意 handler 自己不发网络请求，它委托给同一文件里的 `doApiFetch`（2476 行）：

```ts
async function doApiFetch(apiPath, method, body) {
  const url = API_BASE_URL + apiPath;
  const headers = { 'Content-Type': 'application/json' };
  const token = authManager.getAccessToken();           // ← 拿当前 access token
  if (token) headers['Authorization'] = 'Bearer ' + token;

  try {
    const response = await net.fetch(url, {              // ← Electron 的 net.fetch
      method, headers,
      body: body !== undefined ? JSON.stringify(body) : undefined,
    });
    const data = await response.json();
    return { ok: response.ok, status: response.status, data };
  } catch {
    return { ok: false, status: 0, data: null };        // ← 网络挂了用 status: 0 标记
  }
}
```

几个细节：
- **`net.fetch` 不是 `fetch`**：Electron 提供的版本走系统网络栈，自动应用代理设置、session 配置，比 Node 原生 fetch 更适合桌面端
- **token 来自 `authManager.getAccessToken()`**：authManager 在 login/refresh 后会把 access token 缓存在 main 进程的模块变量里
- **catch 永不抛**：网络层错误也包装成 `{ ok: false, status: 0, data: null }` 返回——保证 handler 永远 resolve 不 reject，渲染端不用 try/catch

---

**4. 核心拦截逻辑——3 步看懂**

```ts
const result = await doApiFetch(params.path, method, params.body);    // ① 先发一次

if (result.status === 401) {                                          // ② 401 才考虑刷
  const errData = result.data as { error?: { code?: string } } | null;
  if (errData?.error?.code === 'TOKEN_EXPIRED') {                     // ③ 必须是 TOKEN_EXPIRED
    const refreshed = await authManager.refresh();
    if (refreshed) {
      return doApiFetch(params.path, method, params.body);            // ④ 用新 token 重发一次
    }
  }
}
return result;                                                        // ⑤ 不满足条件，直接返原结果
```

**为什么必须三重判断？**

| 判断 | 缺了会怎样 |
|---|---|
| `status === 401` | 不判断的话，**每个请求**都会去刷一遍 token，性能崩 |
| `code === 'TOKEN_EXPIRED'` | 401 里还有 `INVALID_TOKEN`/`USER_NOT_FOUND`/`UNAUTHORIZED`（Q42 表）——这些刷 token 救不回来，只能让用户重新登录。这里**只对"能救的情况"出手** |
| `refreshed === true` | refresh 也可能失败（refresh token 也过期了等）；失败就不要重试了，重试只会再得一个 401 |

**为什么"只重试一次"？**

代码逻辑里写死了**没有循环**：刷完只重发一次。这是有意为之：
- 如果新 token 拿了还是 401 → 说明问题不在 token 过期（可能是用户被禁、权限被改），刷再多次也没用
- 防止"刷-401-再刷"的死循环把服务器打挂

---

**5. 这段代码和后端 `/api/auth/refresh` 是配套的**

Q44 讲过 server 端的 refresh 路由。客户端这套自动重试正是它的**消费方**：

```
              ┌─ doApiFetch ─→ Server /api/me/xxx
              │                    │
              │                    ▼ 1h 后
              │           ❌ 401 { code: 'TOKEN_EXPIRED' }
              │                    │
              ▼                    │
[api:request handler]              │
    │  ← 收到 401 + TOKEN_EXPIRED  │
    │                              │
    ├─→ authManager.refresh()      │
    │       │                      │
    │       └─→ Server /api/auth/refresh
    │              { refreshToken, deviceId }
    │              ↓
    │       ✅ { accessToken: 新, refreshToken: 新 }
    │       │
    │       ├─ accessToken = 新（写内存）
    │       ├─ writeSafe('refresh_token', 新)（写 safeStorage）
    │       └─ scheduleRefresh(新)（排下一次定时刷）
    │
    └─→ doApiFetch 再调一次 /api/me/xxx ← 这次带新 token
              │
              ▼
        ✅ 200 OK 返回数据
              │
              ▼
        返回给 renderer，业务代码毫无感知地拿到了数据
```

**对 renderer 来说，从头到尾只有一次 `apiRequest({...})` 调用**——下面这些它全不知道：
- 第一次请求其实失败了
- main 进程偷偷换了 token
- 第二次请求才成功

这就是"拦截器"模式的精髓：**把横切关注点（token 续命）藏到 transport 层，业务代码假装看不见**。如果没有这层拦截，每个组件都得自己写"401 就跳登录或刷 token"的样板代码，太丑了。

---

**6. 几个值得注意的工程细节**

**(a) 并发请求的"惊群"问题——authManager 的去重**

想象 renderer 同时发出 10 个请求，10 个都 401，触发 10 次 `authManager.refresh()`。10 次都去打 `/api/auth/refresh` 的话：
- 10 次往返服务器，浪费
- **更糟的是**：refresh token 会**轮转**（Q44 讲过）——第 1 次刷成功把旧 refresh 删了；第 2 次拿同样的旧 refresh 去刷会 `INVALID_REFRESH_TOKEN` → 用户被踢下线

所以 authManager.refresh 内部做了 **in-flight 去重**（`apps/desktop/src/main/authManager.ts:696`）：
```ts
let refreshPromise: Promise<boolean> | null = null;

export async function refresh(): Promise<boolean> {
  if (refreshPromise !== null) return refreshPromise;   // ← 已有刷请求飞着，直接复用
  refreshPromise = (async () => { /* 真正刷 */ })();
  try { return await refreshPromise; }
  finally { refreshPromise = null; }
}
```

10 个请求同时调 refresh，**只有第 1 个真发请求**，其他 9 个等同一个 Promise resolve。所有人拿到同一份新 token，再各自重发。完美避开了"刷出多份 token + refresh 被轮转死"。

**(b) 它和"定时主动刷新"配套**

authManager 不只在 401 时被动刷——login/refresh 完成后会调 `scheduleRefresh(token)`，根据 JWT 的 exp 算出剩余时间，提前几分钟主动刷一次。这意味着**正常路径根本不会走到本 handler 的 401 分支**——这段代码是**兜底**，处理"恰好掐在过期边缘"或"机器睡眠醒来定时器没跑"的边缘情况。

**(c) 类型断言 `as { error?: { code?: string } } | null`**

```ts
const errData = result.data as { error?: { code?: string } } | null;
if (errData?.error?.code === 'TOKEN_EXPIRED') { ... }
```

`result.data` 是 `unknown`（因为不同接口返回什么 main 端不知道）。这里用类型断言告诉 TS "我假设 401 的 data 长这个样"。**注意全是 optional + `?.` 链式**：万一对方返回的不是 `{error: {...}}` 结构，链路返 undefined，不会炸。这是处理外部数据的安全姿势。

**(d) 失败时返回 `result`，不抛**

```ts
if (refreshed) {
  return doApiFetch(params.path, method, params.body);
}
}
}
return result;          // ← 401 但不是 TOKEN_EXPIRED，或者 refresh 失败，原样返回
```

返回原 401 给 renderer，让 UI 层根据 code 决定是跳登录页还是显示错误提示。**main 进程不擅自做"全局副作用"，只做 transport 层的拦截**。

---

**7. 流程对照表（一图流）**

| 场景 | status | data.error.code | refresh 结果 | handler 返回 | UI 行为 |
|---|---|---|---|---|---|
| 正常请求 | 200 | - | - | 原结果 | 渲染数据 |
| access 过期 | 401 | TOKEN_EXPIRED | ✅ 成功 | **重发后的结果** | 渲染数据（无感） |
| access 过期+refresh 也过期 | 401 | TOKEN_EXPIRED | ❌ 失败 | 原 401 | UI 跳登录页 |
| access 烂的 | 401 | INVALID_TOKEN | （不尝试） | 原 401 | UI 跳登录页 |
| 用户被删 | 401 | USER_NOT_FOUND | （不尝试） | 原 401 | UI 跳登录页 |
| 没权限 | 403 | PERMISSION_DENIED | （不尝试） | 原 403 | UI 显示无权限 |
| 业务错 | 400/404/etc | 对应业务码 | （不尝试） | 原结果 | UI 显示业务错误 |

---

**一句话总结**

`api:request` IPC handler 是把 Q44 那套"access 过期就用 refresh 换一对新 token"的客户端实现做成**对业务透明的拦截器**：
- 渲染端永远只调一次 `apiRequest`，看不到任何 token 细节
- main 进程在 transport 层判 `401 + TOKEN_EXPIRED` → 调 `authManager.refresh()`（内部对并发去重）→ 拿到新 access 后**默默重发一次**
- 三重判断 + 仅重试一次 + 失败原样返回，避免循环刷新和误判

## Q47: logout 最大 1 小时不一致与黑名单退化为 Session

**问题**

> "这是'最大 1 小时的不一致'——可接受的代价，换来不需要维护 access token 黑名单（否则就退化成 Session 模式了）"

具体讲讲这句话。

**回答**

这句话是在描述 Q44 logout 实现的一个**关键工程权衡**。完整理解需要拆三层：

1. "**1 小时的不一致**" 到底是什么不一致
2. 为什么不修这个不一致——修了会发生什么
3. 修了之后为什么"等于退化成 Session"

---

**1. "1 小时不一致" 到底指什么**

先看本项目 logout 的代码（`services/auth.ts:247`）：

```ts
export async function logout(userId: string, deviceId: string): Promise<void> {
  await prisma.refreshToken.deleteMany({
    where: { userId, deviceId },
  });
}
```

注意：**只删了 `RefreshToken` 这一张表的行，完全没有动 access token。**

回想 Q44，access token 是 JWT，**它的"有效性"由谁判断？**

```ts
// authenticate 中间件里
const payload = await verifyAccessToken(token);   // 只查 JWT 自身的签名 + exp
```

JWT 校验只看两件事：
- 签名对不对（HMAC 用项目密钥重算一遍）
- `exp` 字段过没过

**没有任何一步会去查"这个 token 是不是被注销了"。** 因为 access token 是无状态的，server 端没有任何地方记录"哪些 access token 还活着"。

→ 后果：

```
T=0      用户登录，拿到 access_v1（exp = T+60min）
T=10min  用户点"退出登录" → /api/auth/logout
         ↓
         RefreshToken 表里 (userId, deviceId) 那行被 deleteMany
         ↓
         refresh 立刻死了（再也换不到新 access）
         ↓
         但 access_v1 还在客户端内存里 / 中间人手里 / 偷出去的副本里
         ↓
T=11min  有人拿 access_v1 调 /api/me/issues
         → authenticate 验签 ✅、查 exp 没过 ✅ → 放行 ✅
T=12min  同样有效
...
T=60min  access_v1 自然过期，从这一刻起才真正没用了
```

**这就是"最大 1 小时的不一致"**：
- "**逻辑上**"用户已经在 T=10min 登出了
- "**事实上**"那个 access token 在 T=60min 之前依然能调任何接口
- **最多差 50 分钟**——业务上口径取最坏情况 = access TTL 长度 = **1 小时**

这 50 分钟里，server 端**不知道**也**不会拒绝**那个 access token 的请求。

---

**2. 为什么不修这个不一致**

最自然的修法叫 **token 黑名单（blacklist / revocation list / deny list）**：

> server 维护一张"已被撤销的 access token 列表"。authenticate 中间件每次验签通过后，**再额外查一次黑名单**，命中就拒绝。

代码大概长这样：

```ts
// authenticate.ts（加了黑名单的版本）
export const authenticate: RequestHandler = async (req, _res, next) => {
  const token = req.headers.authorization?.slice(7);
  const payload = await verifyAccessToken(token);     // ① 验签 + exp
  // ★ 多出来的查询：
  const isRevoked = await redis.exists(`blacklist:${payload.jti}`);  // ② 查黑名单
  if (isRevoked) return next(invalidToken());
  // ③ 走业务
};

// logout 也要改
async function logout(userId, deviceId, currentAccessToken) {
  await prisma.refreshToken.deleteMany({ where: { userId, deviceId } });
  // ★ 把当前 access token 拉黑
  const payload = decodeJwt(currentAccessToken);
  const ttl = payload.exp - Math.floor(Date.now() / 1000);
  await redis.set(`blacklist:${payload.jti}`, '1', { EX: ttl });   // exp 之后自动清掉
}
```

这样 logout 后那个 access token **立即失效**——没了 1 小时不一致。**听起来挺好，那为什么不做？**

---

**3. 修了之后："退化成 Session"的代价**

代价分四个维度，每个都打在 JWT 引以为豪的优势上。

**(a) 每个请求多一次 IO**

| 不带黑名单 | 带黑名单 |
|---|---|
| authenticate = HMAC 验签（几微秒，纯 CPU） | authenticate = HMAC + Redis EXISTS 查询（毫秒级，过网络） |

JWT 最大的卖点之一是 **"无状态、零 IO 验证"**。一旦加黑名单，**每个登录态请求都要查一次黑名单**——QPS 1000 的接口 = 每秒 1000 次黑名单查询。

这正是 Session 模式的样子：Session 验证 = 拿 session id 去 Redis/DB 查 → 不带 session 表的查询，session 模式根本不工作。**只要在请求关键路径加一次"按 token 标识查存储"，就走上了 Session 的老路。**

**(b) 多机部署的扩展性丢了**

JWT 的另一个卖点：**任何机器拿着 SECRET 都能独立验签**，无需共享状态。

加了黑名单后：
- 黑名单必须在所有 server 实例之间共享 → 必须放 Redis/中心存储
- Redis 挂了 → 要么"宁可错杀拒绝所有请求"，要么"放行所有请求等于黑名单失效" → 都是事故
- 多区域部署还要考虑黑名单数据复制延迟

而 Session 模式天然就需要中心 store。**这又一次"退化"了。**

**(c) 黑名单 vs Session 的结构同构**

仔细看 Session 模式的存储结构：

```
session:abc123 → { userId, expiresAt, ... }    # session 存在 = 有效
```

再看黑名单的存储结构：

```
blacklist:xyz789 → "1"   (TTL = token 剩余寿命)   # blacklist 存在 = 无效
```

两者只差一个**布尔反转**：Session 是"白名单"，黑名单是黑名单。**但都得在请求路径上查一次中心存储**——本质完全一样。

如果黑名单里被拉黑的 access token 占比超过 50%（大规模事故场景），从存储/IO 角度看，黑名单方案甚至比 Session 还浪费。

**(d) 自动过期机制和 TTL 一致性**

黑名单条目要能"等到 token 自然过期后自动删掉"，否则越积越多。这要求：
- 黑名单 entry 的 TTL = JWT 剩余有效期
- 必须用 Redis 这种带 TTL 的存储

→ 这又给运维加了依赖。

---

**4. 那"1 小时不一致"到底有没有事？**

**关键洞察**：要看你的业务能不能容忍。

**能容忍的场景（本项目就是这类）**：
- 内部工具 / IDE 类客户端 / 协作平台
- 用户登出场景：基本是"换台机器用"或"程序结束"，没人会"登出后立刻担心 1 小时内有黑客调我账号"
- access token 存在 **客户端进程内存 / Electron safeStorage**，不在 cookie 里，**不会自动被浏览器带去其他地方**
- token 泄露的攻击面本身就很窄

**不能容忍的场景**：
- 银行 / 支付 / 转账
- 管理后台（一个 admin 离职马上要踢）
- 共享设备登录（机场 / 网吧 / 公共大屏）
- 怀疑被盗号（用户主动"全部设备登出"想立刻看到效果）

→ 不同业务对"撤销实时性"的要求差几个数量级。**本项目是内部 AI 工作台**，1 小时窗口在威胁建模里基本无影响。

---

**5. 三种主流缓解策略（不真上黑名单也能改善）**

如果想压缩这个窗口又不想真做"全量黑名单 + 每请求查"，业界有几种折中：

**(a) 缩短 access TTL**

把 1h 改成 5min：
- 不一致窗口从 60min → 5min
- 代价：刷新频率提高 12 倍，但因为 `/api/auth/refresh` 调用占总流量比例很小，整体性能压力可控
- 高敏感场景标配做法

**(b) "用户级别版本号"机制（user-level token version）**

User 表加一个 `tokenVersion: number` 列。JWT payload 里写当时签发时的 version：

```ts
// 登录时
const accessToken = signJwt({ sub: userId, tokenVersion: user.tokenVersion });

// authenticate 时
const user = await prisma.user.findUnique(...);   // 反正本项目就要查
if (user.tokenVersion !== payload.tokenVersion) throw invalidToken();

// "强制全设备登出"
await prisma.user.update({ where: { id }, data: { tokenVersion: { increment: 1 } } });
```

效果：管理员一执行"强制下线"，version+1，**所有现存 access token 立刻全废**。
代价：只有 **"全用户"** 撤销有意义，不能单独撤销某一个 token；但 99% 的"踢人下线"需求都是"踢这个用户所有设备"，正好够用。
**注意本项目 authenticate 本来就查用户**（Q42 提过），加一个字段对比几乎零成本——是性价比最高的增强。

**(c) Token introspection（OAuth 2.0 RFC 7662）**

让所有验证都不再本地验签，而是调一个中心化的 `/introspect` 接口由权威方判断"这个 token 现在有效吗"。
彻底放弃 JWT 自包含的优势，**等于走回 Session**。一般只在多方系统（OAuth 服务提供商）才会用。

---

**6. 本项目做了什么、没做什么、能怎样演进**

| 项 | 本项目 | 评价 |
|---|---|---|
| Logout 撤销 refresh | ✅ deleteMany | 必做，已做 |
| Logout 撤销 access | ❌ 不做 | 接受 ≤1h 窗口 |
| Access TTL | 1h | 内部场景合理 |
| 黑名单 | ❌ | 不做（避免退化成 Session） |
| User tokenVersion | ❌ | 暂未做，需要"强制踢人"时是首选演进 |
| Refresh 轮转 | ✅ | Q44 已实现 |
| Refresh 复用检测 | ❌ | Q44 提过，未来增强 |

**演进顺序**：未来如果业务需要"管理员一键强制踢人下线"，**优先加 `User.tokenVersion`**，远比上黑名单划算（不依赖 Redis、不增加 per-request 查询、复用现有的 prisma.user.findUnique 调用）。

---

**7. 一张总图——为什么"修了就退化成 Session"**

```
┌─ 纯 JWT（本项目）──────────────────────────────────────────┐
│  authenticate:                                              │
│   ① HMAC 验签（CPU，几微秒）                                │
│   ② 查 exp                                                  │
│   ③ findUnique(user)  ← 本来就有一次 PG 查询                │
│   ④ 放行                                                    │
│  ★ 不需要中心存储，多机部署纯线性扩展                       │
│  ★ 代价：access 撤不掉，最多 1h 不一致                       │
└─────────────────────────────────────────────────────────────┘

┌─ 加黑名单后 ───────────────────────────────────────────────┐
│  authenticate:                                              │
│   ① HMAC 验签                                               │
│   ② 查 exp                                                  │
│   ③ ★ 查 Redis 黑名单（毫秒，过网络） ← 退化点               │
│   ④ findUnique(user)                                        │
│   ⑤ 放行                                                    │
│  ★ 必须有 Redis 共享存储                                    │
│  ★ 每请求多一次 IO                                          │
│  ★ Redis 单点故障 = 全站登录态可用性问题                    │
└─────────────────────────────────────────────────────────────┘

┌─ 纯 Session ───────────────────────────────────────────────┐
│  authenticate:                                              │
│   ① 从 cookie 拿 sessionId                                  │
│   ② 查 Redis session:xxx → 不存在则 401                     │
│   ③ findUnique(user)（如果 session 里不存 user 信息）        │
│   ④ 放行                                                    │
│  ★ 必须有共享 session store                                 │
│  ★ 每请求一次 store IO                                      │
└─────────────────────────────────────────────────────────────┘
```

**看出来了吗——加黑名单的 JWT 和 Session 在请求路径开销、依赖、可用性模型上几乎完全等价。** "选 JWT 是为了省掉中心存储的 per-request 查询"——加了黑名单这个好处就没了，那当初为什么选 JWT？

所以才说："要么接受 1 小时窗口（享受 JWT 的所有优势），要么干脆用 Session（撤销实时、模型简单）。**加黑名单是两头不到岸**——既要 JWT 的复杂度（密钥管理、轮转、refresh 链路），又要 Session 的存储依赖，纯属自找麻烦。"

---

**一句话总结**

"1 小时不一致"指 logout 后旧 access token 在 exp 之前 server 端无从知道它该废、依旧会放行；想立刻废它就得在 authenticate 链路里加一次"按 token 查中心黑名单存储"的 IO——一旦加上，JWT "无状态、零 IO、多机线性扩展"的全部好处都没了，请求路径开销、依赖、可用性模型和 Session 模式完全等价，所以叫"退化成 Session"。本项目权衡后认为内部桌面工具这点窗口完全可接受，把这部分复杂度省下来；未来真要"管理员一键踢人"，更划算的方案是 `User.tokenVersion`，而不是黑名单。

## Q48: scheduleRefresh 在干什么

**问题**

`scheduleRefresh(accessToken)` 在干什么？

**回答**

`scheduleRefresh` 是 main 进程里**主动续命定时器**的设置函数（`apps/desktop/src/main/authManager.ts:359`）。每当客户端拿到一个新的 access token（login / refresh / 启动恢复登录态都会触发），它会**算出"再过多久 token 就要过期"，提前 5 分钟在本地排一个 setTimeout，到点自动调用 `refresh()`**——这样**业务请求基本上永远碰不到过期 token**。

它是 Q46 那个 "401 兜底重试" 的**前置主动方案**：

- `scheduleRefresh` = **主动**：到点就刷，请求路径完全无感
- 401 重试 = **被动**：主动定时器没及时跑的兜底

---

**1. 完整源码**

```ts
let refreshTimer: ReturnType<typeof setTimeout> | null = null;

function scheduleRefresh(token: string): void {
  // ① 清掉旧定时器
  if (refreshTimer !== null) {
    clearTimeout(refreshTimer);
    refreshTimer = null;
  }
  try {
    // ② 解码 JWT payload（不验签，只看 exp）
    const payload = JSON.parse(
      Buffer.from(token.split('.')[1], 'base64').toString('utf-8'),
    );
    // ③ 算还有多久到"exp 前 5 分钟"
    const delay = (payload.exp - 300) * 1000 - Date.now();
    // ④ 排定时器（或立即触发）
    if (delay <= 0) {
      refresh();
    } else {
      refreshTimer = setTimeout(() => refresh(), delay);
    }
  } catch {
    // Invalid JWT format — skip scheduling
  }
}
```

---

**2. 一步步拆开看**

**① 清旧定时器**
```ts
if (refreshTimer !== null) {
  clearTimeout(refreshTimer);
  refreshTimer = null;
}
```
- `refreshTimer` 是模块级单例变量
- 每次拿到新 token 都要**先撤掉前一个 timer**——不然新 token 排了新 timer，旧的还会到点触发，浪费一次 refresh 调用，还可能跟新 timer 抢着改 `accessToken`
- 这就是为什么这个函数能在 login、refresh 完成、启动恢复登录态三个地方反复调用而不出问题

**② 自己解码 JWT 拿 exp**
```ts
const payload = JSON.parse(
  Buffer.from(token.split('.')[1], 'base64').toString('utf-8'),
);
```
- JWT 是 `Header.Payload.Signature`，`split('.')[1]` = payload 段
- base64 解码 → JSON → 拿到 `{ sub, device, iat, exp }`
- **这里没验签，因为 server 刚发的 token，可以信任**；客户端只关心"什么时候过期"
- `Buffer.from(..., 'base64')` 用的是 Node 标准 base64，技术上 JWT 用的是 base64url（`-` 替 `+`，`_` 替 `/`，无填充），但 Node 16+ 的 `Buffer` 对 `-`/`_` 也能正常解析，对短 payload 不会出问题

**③ 算延迟**
```ts
const delay = (payload.exp - 300) * 1000 - Date.now();
//             └── exp 前 5 分钟 ──┘ × 1000 转毫秒
```
- `payload.exp` 单位是 **Unix 秒**（JWT 规范），Date.now() 是 **毫秒**——所以前者要乘 1000
- 减 300 = **提前 5 分钟刷**（safety margin）
- 本项目 access TTL = 1h → delay 大约是 55 分钟

**为什么必须留 safety margin？**
- 网络延迟：refresh 请求往返要时间，万一刚发起就过期了，server 还是返 `TOKEN_EXPIRED`
- 时钟漂移：客户端和 server 时钟可能差几秒
- 任务队列：JS 单线程，setTimeout 到点不一定立刻执行
- 防止"边界状态"：还剩 1 秒过期的时候用户正好点了个按钮发请求 → 一来一回必定 401
- → 提前 5 分钟绝对够覆盖以上所有情况

**④ 排 timer 或立即触发**
```ts
if (delay <= 0) {
  refresh();                                  // 已经过期 / 5 分钟内过期 → 立刻刷
} else {
  refreshTimer = setTimeout(() => refresh(), delay);   // 否则到点再刷
}
```
- `delay <= 0` 的场景：启动时从磁盘恢复了一个老的 access token，已经在"5 分钟剩余区"了→ 别等了直接刷
- 否则就 setTimeout，到点自动调 `refresh()`（Q44 里讲过的那个 refresh）

**⑤ try/catch 兜底**
```ts
} catch {
  // Invalid JWT format — skip scheduling
}
```
- 万一 token 格式坏了（人为 hack、磁盘损坏、版本不兼容）→ **不抛错，安静跳过**
- 后果是这次没有主动刷新；但 Q46 的 401 兜底依然会接住——**两层兜底，互相不假设对方一定能跑**

---

**3. 它在哪几处被调用**

`apps/desktop/src/main/authManager.ts` 里有 3 个调用点：

```ts
// L583  startup：从磁盘读到 refresh_token 后调用 refresh()，成功后排
scheduleRefresh(accessToken);

// L684  login 成功后
scheduleRefresh(accessToken);

// L728  refresh 成功后（这是题目里问的那一处）
scheduleRefresh(accessToken);
```

→ **每次 access token 发生变化都会重新排 timer**。形成了一个 **"自循环刷新链"**：

```
T=0   login → 拿到 access_v1（exp=T+60min）→ scheduleRefresh → timer 55min 后触发
T=55  timer 触发 → refresh() → 拿到 access_v2（exp=T+115min）→ scheduleRefresh → timer 55min 后触发
T=110 timer 触发 → refresh() → 拿到 access_v3 → ...
... 只要进程活着，链子永不断
```

logout 时 `clearAuth()`（L459）会把 timer 清掉，链子才停。

---

**4. 它和 Q46 的 401 兜底是什么关系**

| | scheduleRefresh（主动） | api:request 401 重试（被动） |
|---|---|---|
| **触发时机** | exp 前 5 分钟，定时器到点 | 业务请求收到 401+TOKEN_EXPIRED |
| **业务请求感知** | 完全不感知，请求一直拿新 token | 第一次失败、隐式重试一次 |
| **触发频率** | 每个 TTL 周期一次（1h 一次） | 罕见，作为兜底 |
| **失败影响** | 默默重试下一周期，业务无感 | 把 401 返给 UI，UI 跳登录 |

**协作关系**：
- 正常路径上：scheduleRefresh 把 token 续上，**业务请求 99% 走的是有效 token**
- 例外路径上（机器睡眠、网络断开、定时器没跑、首次启动 token 已过期）：401 兜底接住

**为什么需要两套**：
- 只有 401 兜底 → 每个 access 周期最后一次请求必定 401，浪费一来一回
- 只有 scheduleRefresh → 万一 timer 没按时跑（机器睡眠、进程暂停），过期了就没救
- → **两层防护，配合无缝**

---

**5. 几个值得展开的工程细节**

**(a) `setTimeout` 不是绝对精准的**

setTimeout 只保证"**不早于** delay 触发"，**机器睡眠后不会补跑**。Mac 合盖 / Windows 进入睡眠模式时，事件循环冻结，唤醒后：
- 如果只睡了几分钟 → timer 醒来后立刻触发，正常
- 如果睡了几小时 → timer 应该几小时前触发，但实际是醒来才执行；这时 token 早过期了

这种"睡过头"场景就是 Q46 401 兜底真正派上用场的时候。

**(b) 单 timer 单例 vs 多 timer**

模块顶层 `let refreshTimer = null` —— 全进程**只有一个 timer**。
- 不会出现"多个 timer 重叠触发多次刷"——这正好对应 Q46 讲的 refresh 并发去重（`refreshPromise` 单例），两层都防住了"并发刷把 refresh token 轮转坏"
- 缺点：如果将来要支持"多账号同时登录"，这个单例就不够用，得做成 `Map<userId, Timer>`。目前是单账号，不是问题

**(c) 为什么客户端 5 分钟、不和 server 的 access TTL 配置耦合**

5 分钟是 hardcode 的常数。理论上更"正确"的做法是：
- 客户端读 `payload.exp - payload.iat`（= TTL）
- 取 TTL 的 10%~20% 作为 margin

但 hardcode 5 分钟更简单：
- 对 1h TTL：提前 5 分钟 = 提前 8.3%，合理
- 对 5min TTL：提前 5 分钟 = 等于不缓存，每次都要刷 → 不合适
- → 这个常数隐式依赖 "TTL 至少几十分钟"，本项目 1h，没问题；将来如果 access TTL 改短到 10 分钟以下，这个常数就要重新评估

**(d) 失败重试机制？没有**

如果 timer 到点 `refresh()` 失败（网络掉了），代码里**没有"几秒后重试"的逻辑**：
- refresh 内部 catch 了所有错，返 `false`，**不再排下一个 timer**
- 之后业务请求该用旧 token 还用旧 token，最终走到 401 → Q46 兜底接管 → 那时再尝试 refresh

这是有意的简化：**主动刷失败不焦虑，等业务请求触发时再说**——避免在网络长时间断开时疯狂重试浪费资源。

---

**6. 一图流**

```
┌─ login / refresh / startup 拿到新 accessToken ─────────────────┐
│                                                                │
│         scheduleRefresh(accessToken)                           │
│                  │                                             │
│                  ├─ 1. clearTimeout(旧 timer)                  │
│                  │                                             │
│                  ├─ 2. 解 payload → exp                        │
│                  │                                             │
│                  ├─ 3. delay = (exp - 300) * 1000 - now        │
│                  │                                             │
│                  ├─ 4. setTimeout(refresh, delay)              │
│                  │      ↓                                      │
│                  │   55 分钟后到点                              │
│                  │      ↓                                      │
│                  │   refresh() → 新 token                      │
│                  │      ↓                                      │
│                  │   scheduleRefresh(新 token)  ← 自循环         │
│                  │                                             │
└────────────────────────────────────────────────────────────────┘

业务请求路径：
GET /api/me/xxx → 带的永远是 fresh token → 200 OK
                                  ↑
                  scheduleRefresh 在背后默默把 token 续好了
```

---

**一句话总结**

`scheduleRefresh(accessToken)` 是 main 进程的"**access token 主动续命定时器**"：解码 JWT 拿 `exp`，提前 5 分钟排一个 setTimeout，到点自动调 `refresh()` 拿新 token，再递归把自己排上 → 永不停的自循环刷新链。它把 Q44 的 refresh 模式从"被动出错才刷"提升到"几乎永远刷在前面"，让业务请求 99% 走的是新鲜 token；Q46 那套 401 重试只是"主动定时器没赶上"时的兜底，两层防护配合无缝。
