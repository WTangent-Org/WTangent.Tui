using System.CommandLine;
using WTangent.Tui.Store;

namespace WTangent.Tui.Commands;


/// <summary>agent git：init/clone 是 agent 包装（.agent 清单 + 服务器解析），**其余参数完全透传给真 git**（在项目目录执行）。
/// 透传示例：agent git push / add -A / commit -m "..." / pull / branch / status / checkout —— 等价于在该目录跑 git。
/// 服务器注册表在顶层命令 agent remote（不套 git）。</summary>
[AgentComponent]
public sealed class GitCommand : Command
{
    private static readonly Option<string?> DirOption = new("--dir") { Description = "项目目录（缺省当前目录；透传时 git 在此执行）" };

    public GitCommand() : base("git", "git 透传（init/clone 为 agent 包装，其余参数直跑真 git）")
    {
        TreatUnmatchedTokensAsErrors = false;
        Add(DirOption);
        Add(BuildInitCommand());
        Add(BuildCloneCommand());

        // 其余任意参数（含 git remote/add/commit/push/pull/branch/status…）→ 透传给真 git
        SetAction(pr => new GitStore(pr.GetValue(DirOption) ?? ".").RunGit([.. pr.UnmatchedTokens]));
    }

    /// <summary>本地项目：git init -b main + 身份 + .agent 清单（之后 add/commit/push 全是透传 git）。</summary>
    private static Command BuildInitCommand()
    {
        var initDir = new Argument<string?>("dir") { Arity = ArgumentArity.ZeroOrOne, Description = "目标目录（缺省当前目录）" };
        var initName = new Option<string?>("--name") { Description = "项目名（缺省目录名）" };
        var init = new Command("init", "新建本地项目（git init + .agent 清单）") { initDir, initName };
        init.SetAction(pr =>
        {
            var dir = pr.GetValue(initDir) ?? ".";
            var name = pr.GetValue(initName) ?? new DirectoryInfo(dir).Name;
            GitStore.Init(dir, name);
            Console.WriteLine($"[git init] 项目 {name} 就绪：{Path.GetFullPath(dir)}");
            return 0;
        });
        return init;
    }

    /// <summary>从服务器克隆：按名查服务器 URL → git clone + .agent 清单。</summary>
    private static Command BuildCloneCommand()
    {
        var serverArg = new Argument<string>("server");
        var projectArg = new Argument<string>("project");
        var cloneDir = new Argument<string?>("dir") { Arity = ArgumentArity.ZeroOrOne, Description = "目标目录（缺省项目名）" };
        var clone = new Command("clone", "从服务器克隆项目（服务器名 / ET 加入码）") { serverArg, projectArg, cloneDir };
        clone.SetAction(async pr =>
        {
            var server = pr.GetValue(serverArg);
            var project = pr.GetValue(projectArg);
            if (server is null || project is null) return 1;
            var hit = new ServerRegistry().Find(server);
            if (hit is null)
            {
                await Console.Error.WriteLineAsync($"[git clone] 服务器 {server} 未配置，先 agent git server add <name> <url> [--code]");
                return 1;
            }
            var dir = pr.GetValue(cloneDir) ?? project;
            GitStore.Clone(server, hit.Url, project, dir);
            Console.WriteLine($"[git clone] {server}:{project} → {Path.GetFullPath(dir)}");
            return 0;
        });
        return clone;
    }
}
