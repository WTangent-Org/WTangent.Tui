
namespace WTangent.Tui;

/// <summary>客户端顶级行为（[AgentDefault] 标记，源生成器生成 Entry.Default）</summary>
public static class Defaults
{
    /// <summary>启动 TUI 终端聊天（目标 serve = 本地已装 → 缓存 remote → 自动下载本地 serve）</summary>
    [AgentDefault]
    public static int RunTui(string[] args)
    {
        var url = ClientPaths.ResolveUrl(null);
        if (url is null)
        {
            Console.Error.WriteLine("[agent] 未找到服务器：先启动 serve（agent serve），或 agent web <remote>");
            return 1;
        }
        Tui.TuiRepl.RunAsync(url).GetAwaiter().GetResult();
        return 0;
    }
}
