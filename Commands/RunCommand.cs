using System.CommandLine;
using WTangent.Tui.Store;
using WTangent.Tui.Session;

namespace WTangent.Tui.Commands;


/// <summary>一次性问答（**LLM 归 serve**：客户端只收集 prompt，serve 调模型）。
/// 目标 serve：[remote]（名/ET加入码/URL）缺省优先级：本地已装 → 缓存 remote → 本地自动下载（见 ClientPaths.ResolveUrl）。</summary>
[AgentComponent]
public sealed class RunCommand : Command
{
    public RunCommand() : base("run", "一次性问答：agent run <prompt> [<remote>]（客户端只发 prompt；LLM 由 serve 调用）")
    {
        var prompt = new Argument<string>("prompt")
        {
            Description = "一次性问答内容",
        };
        var remote = new Argument<string?>("remote")
        {
            Description = "服务器名 / ET 加入码 / URL（缺省：本地已装 → 缓存 remote → 自动下载本地 serve）",
            Arity = ArgumentArity.ZeroOrOne
        };
        Add(prompt);
        Add(remote);

        SetAction(async pr =>
        {
            var promptText = pr.GetValue(prompt);
            if (promptText is null) return 1;   // 必选参数（解析器已保证，防御可空）
            var given = pr.GetValue(remote);
            var url = ClientPaths.ResolveUrl(given);
            if (url is null) return 1;
            try
            {
                var answer = await new RemoteAgentClient(new Uri(url)).AskAsync(promptText);
                if (given is { Length: > 0 }) ServerRegistry.SetLastUsed(given);   // default-server = last-used 缓存
                Console.WriteLine(answer);
                return 0;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"[run] 无法连接 {url}：{e.Message}\n先启动 serve：agent serve [--mock]");
                return 1;
            }
        });
    }
}
