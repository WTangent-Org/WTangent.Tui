# WtAgent.Client

.NET 10 自研 AI 助手客户端（**tui 组件**）：终端聊天（TUI）+ 浏览器打开 serve 的 Web UI。

## 架构（五仓 + 组件化）

| 仓库 | 组件 | 形态 |
|---|---|---|
| [WtAgent](https://github.com/wtommy932/WtAgent) | 空壳启动器 | self-contained 单 exe，install/upgrade/加载组件 |
| [WtAgent.Server](https://github.com/wtommy932/WtAgent.Server) | serve | 服务端（会话 API / git 仓库 / Web UI） |
| **本仓（WtAgent.Client）** | tui | 纯 dll，顶级 TUI + `web` 命令 |
| [WtAgent.Components](https://github.com/wtommy932/WtAgent.Components) | — | 共享源生成器（`[AgentComponent]`/`[AgentDefault]`） |
| [WtAgent.Core](https://github.com/wtommy932/WtAgent.Core) | — | 共享设施（统一 HttpClient 等） |

组件为 framework-dependent dll，由空壳进程加载（共享运行时）。

## 安装

```powershell
# Windows（PowerShell）
irm https://raw.githubusercontent.com/wtommy932/WtAgent/main/install.ps1 | iex
wtagent install tui
agent          # 顶级启动 TUI（目标 serve：本地已装 → 缓存 remote → 自动下载本地 serve）
```

```bash
# Linux / macOS
curl -fsSL https://raw.githubusercontent.com/wtommy932/WtAgent/main/install.sh | bash
wtagent install tui
agent
```

## 命令

- `agent`：顶级启动 TUI 聊天
- `agent run <prompt> [<remote>]`：一次性问答（LLM 归 serve）
- `agent web [<remote>]`：浏览器打开目标 serve 的 Web UI
- `agent remote`：服务器注册表（list/add/remove/user/passwd）
- `agent git`：git 透传（init/clone 为 agent 包装，其余直跑真 git）

## 开发

- `dotnet build WtAgent.Client.csproj`
- 命令类标 `[AgentComponent]` → 源生成器生成 `Entry.Commands`；顶级行为 `[AgentDefault]`（见 `Defaults.cs`）
- 版本手动指定，**手动发版**（Actions 页 run release workflow，可填版本号）
