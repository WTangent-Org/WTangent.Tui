
using WTangent.Tui.Store;

namespace WTangent.Tui;

/// <summary>客户端顶级行为（[AgentDefault] 标记，源生成器生成 Entry.Default）</summary>
public static class Defaults
{
    /// <summary>启动 TUI 终端聊天（目标 serve = 本地已装 → 缓存 remote）</summary>
    [AgentDefault]
    public static int RunTui(string[] args)
    {
        var url = ResolveServeUrl();
        if (url is null)
        {
            Console.Error.WriteLine("[tui] 未找到服务器：先 wtangent remote add <name> <ip> [port]，或本机 wtangent serve");
            return 1;
        }
        Tui.TuiRepl.RunAsync(url).GetAwaiter().GetResult();
        return 0;
    }

    /// <summary>目标 serve：本地已装 serve 组件 → 回环；否则缓存 remote（last-used，名字/URL）</summary>
    private static string? ResolveServeUrl()
    {
        var dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "agent");
        if (Directory.Exists(Path.Combine(dataDir, "components", "serve")))
            return "http://127.0.0.1:8890";
        var last = ServerRegistry.GetLastUsed();
        if (last is { Length: > 0 })
        {
            if (last.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return last;
            return new ServerRegistry().Find(last)?.Url;
        }
        return null;
    }
}
