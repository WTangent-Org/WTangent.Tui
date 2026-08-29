# Changelog

## [0.6.1](https://github.com/WTangent-Org/WTangent.Tui/compare/v0.6.0...v0.6.1) (2026-08-29)


### ✨ 新功能

* App 静态属性构造注入（PCL-CE 式）：生成器产 static App + ctor，IEntry 移除 App 成员 ([e99d375](https://github.com/WTangent-Org/WTangent.Tui/commit/e99d37587997cb035825635e3709cd0f2174e8c8))
* 最终特性集 [AgentEntry(id,name,isAsync)]/[EntryStart]/[EntryStop]/[AgentCommand(parent)]/[AgentTool] ([85a8ad4](https://github.com/WTangent-Org/WTangent.Tui/commit/85a8ad4ac453c16966a1ca51213ee752a632f22b))
* 构造注入 App（无 null!）+ Current 静态桥（PCL-CE 式）；钩子实例方法，纯业务 ([584af01](https://github.com/WTangent-Org/WTangent.Tui/commit/584af01f4681fda8e3c350e7281a833f6fb04853))


### 🐛 修复

* CI 布局——本仓 checkout 进同名子目录复刻本地工作区布局（ProjectReference 的 ../ 不再越出工作区），构建路径加前缀 ([c8b0531](https://github.com/WTangent-Org/WTangent.Tui/commit/c8b0531cbbafab13e6ee2edc5bc4f4763741183d))


### 🧹 其他

* Components 升 0.0.10（Application 移除 Logger/Config，统一走门面） ([60fd0c2](https://github.com/WTangent-Org/WTangent.Tui/commit/60fd0c274c462fe807ae9b3aee2ed7bf1c047d0f))
* Components 升 0.0.11（StartAsync 无参）；R# 扫描修复 ([0f2e214](https://github.com/WTangent-Org/WTangent.Tui/commit/0f2e2144352d62e35a9f84fadf86f79e894b5732))
* Components 升 0.0.9（ExcludeAssets=runtime）；agent-component.json 由生成器产出 ([919cf3c](https://github.com/WTangent-Org/WTangent.Tui/commit/919cf3ce8153751a3ed1da4fd45d706192fb8336))
* csproj nuget 组补 WTangentDev 条件（dev restore 直拉模式关闭包引用，引用由 wtangent.dev.props 注入） ([85a2319](https://github.com/WTangent-Org/WTangent.Tui/commit/85a2319404bafc6603aa587a99055c13cdb05180))
* csproj 移除无引用的 McMaster.NETCore.Plugins（旧架构残留） ([5bd1cac](https://github.com/WTangent-Org/WTangent.Tui/commit/5bd1cac4eafc1266d5eba3ad3a65420ea38bcd02))
* csproj 补 CompilerVisibleProperty ComponentDepends（本地模式手动声明，配套生成器 depends） ([fa3e0fe](https://github.com/WTangent-Org/WTangent.Tui/commit/fa3e0feb6a805281e2cc99df696468b72e6f339d))
* release-please 统一 always-bump-patch（版本只走 patch） ([4c6b4ba](https://github.com/WTangent-Org/WTangent.Tui/commit/4c6b4ba06dafca28b3e29ce35f1eb99f5f5eed97))

## [0.6.0](https://github.com/WTangent-Org/WTangent.Tui/compare/v0.5.0...v0.6.0) (2026-08-22)


### ✨ 新功能

* IEntry 元组命令（父路径挂接）+ 三形态（cmd/sub/tool）+ 类型字段废弃 ([013070b](https://github.com/WTangent-Org/WTangent.Tui/commit/013070bb317bf374e1fc12ff59376b13f6889eb7))
* IEntry 手写入口（0.0.3）——类型字段废弃，能力由 Entry 声明（Commands/Default/Tools + StartAsync 生命周期） ([8272d4f](https://github.com/WTangent-Org/WTangent.Tui/commit/8272d4f4f12e9b834657dd5a8d5ff3c1c8b5fb55))
* tui 组件（WTangent.Tui 命名空间，WTangent.Components 单包） ([144b611](https://github.com/WTangent-Org/WTangent.Tui/commit/144b6116d3cbcf1d6d5dc7d6221536c25372a9ef))
* 组件类型收敛 ui/cmd/tool + client 组件拆分（remote/run/web 归 client；tui 纯 UI；serve type=cmd；官方组件自动安装） ([246cf45](https://github.com/WTangent-Org/WTangent.Tui/commit/246cf4595d61353c7188d19bbc60522a9e212161))


### 🐛 修复

* release.yml 重复 name/on 头部（workflow startup_failure） ([e6e71de](https://github.com/WTangent-Org/WTangent.Tui/commit/e6e71de729adbb6b0c9bded3398024ba195aba61))


### 🧹 其他

* csproj 文件名统一 WTangent.*（workflow/release-please/deps 引用同步） ([88fd0df](https://github.com/WTangent-Org/WTangent.Tui/commit/88fd0dff4a0edb7f58db15e5567f8c6117524a9b))
* WTangent.Components 0.0.1→0.0.2（Application 契约） ([5aa267c](https://github.com/WTangent-Org/WTangent.Tui/commit/5aa267cf999f70267f68c57615ec958deecaac03))
* WTangent.Components 引用 0.4.0→0.0.1（对齐发布版本） ([076e6e4](https://github.com/WTangent-Org/WTangent.Tui/commit/076e6e42e927469192854a302fa15b502bdf3074))

## [0.5.0](https://github.com/wtommy932/WtAgent.Client/compare/v0.4.0...v0.5.0) (2026-08-19)


### ✨ 新功能

* HttpClient 统一为 WtAgent.Core.Http（共享单例/New） ([884276a](https://github.com/wtommy932/WtAgent.Client/commit/884276aeb009129cb337fd0a4a4b7c9355ede68a))
* remote 缺省优先级——本地已装 serve → 缓存 remote → 自动下载本地 serve ([070f275](https://github.com/wtommy932/WtAgent.Client/commit/070f2755958692f74f5dcf46781acda031aa6b44))
* 命名空间改 WtAgent（前缀统一，包引用 WtAgent.Components） ([c617559](https://github.com/wtommy932/WtAgent.Client/commit/c61755980353f4e1e425cb796a5ecc6a9a654e7c))
* 恢复 run/remote/git 命令（RemoteAgentClient 远程问答 + 服务器表 + git 透传） ([5ad5ec8](https://github.com/wtommy932/WtAgent.Client/commit/5ad5ec82fbd367a8f120b2d4a6a0ba3f99abd2a1))
* 组件入口改 Command 列表（Entry.Commands + Default） ([4160448](https://github.com/wtommy932/WtAgent.Client/commit/41604484e5252aa35d83414de8c55b53e5867c53))
* 重组单项目——顶级 TUI + web + Entry 入口 + 组件 zip 发布 ([3fb6f22](https://github.com/wtommy932/WtAgent.Client/commit/3fb6f228b4fe6ef8f05fabcaa33f2faae53e97f0))


### 🐛 修复

* csproj 改名 WtAgent.* + 显式 RootNamespace（生成器 Entry 命名空间归位） ([85d635e](https://github.com/wtommy932/WtAgent.Client/commit/85d635e2bc468e3a9357a9345f86d9e83ad491fa))

## [0.4.0](https://github.com/wtommy932/Agent.Client/compare/v0.3.0...v0.4.0) (2026-08-18)


### ✨ 新功能

* 新增 agent web 命令 + remote 优先级（本地服务器→缓存→回环） ([d0e39a5](https://github.com/wtommy932/Agent.Client/commit/d0e39a5a7e5a18d5eb39972f274b74e78167308a))

## [0.3.0](https://github.com/wtommy932/Agent.Client/compare/v0.2.0...v0.3.0) (2026-08-18)


### ✨ 新功能

* 构建失败禁合并（轮询只等 CLEAN） ([401eb45](https://github.com/wtommy932/Agent.Client/commit/401eb45c25ac8962a9b468c647c6fc809d38dcd8))


### 🐛 修复

* 自动合并轮询显式 -R 仓库并暴露错误（诊断 UNKNOWN） ([1ce3058](https://github.com/wtommy932/Agent.Client/commit/1ce3058f57aad2b3ae05ca7d693b99f12a27a208))


### 🧹 其他

* 移除误提交的构建产物（bin/obj，.gitignore 生效） ([4ac21a9](https://github.com/wtommy932/Agent.Client/commit/4ac21a96cae938626c6572c0ea7bd04729355443))

## [0.2.0](https://github.com/wtommy932/Agent.Client/compare/v0.1.0...v0.2.0) (2026-08-18)


### ✨ 新功能

* agent-client 初始——run/tui 命令 + Core/Tui 完整 ([1ddecf5](https://github.com/wtommy932/Agent.Client/commit/1ddecf56b9136efb944b58ca0753bef6de4a0926))


### 🧹 其他

* 换官方 Dotnet.gitignore（github/gitignore） ([6980b65](https://github.com/wtommy932/Agent.Client/commit/6980b65ac4ed5d404d08bf522e40b319eb8729cc))
* 配置 release-please + CI（七平台 agent-client 资产） ([117d4b4](https://github.com/wtommy932/Agent.Client/commit/117d4b4da3d532372fb24b0240a9aa4ee3a6dd31))
