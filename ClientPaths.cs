using System.Diagnostics;
using WTangent.Tui.Store;

namespace WTangent.Tui;

/// <summary>客户端工具：目标 serve 解析（本地已装 → 缓存 remote → 本地自动下载）</summary>
public static class ClientPaths
{
    /// <summary>组件安装目录（%APPDATA%\agent\components，空壳 install 的落点）</summary>
    private static string ComponentsDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "agent", "components");

    /// <summary>解析目标 serve。显式 remote：名/ET加入码/URL。
    /// 缺省优先级：1. 本地（serve 组件已下载 → 回环）；2. 缓存 remote（last-used）；
    /// 3. 本地（自动下载 serve 组件 → 回环）。</summary>
    internal static string? ResolveUrl(string? remote)
    {
        if (remote is { Length: > 0 })
        {
            var hit = new ServerRegistry().Find(remote);
            if (hit is not null) return hit.Url;
            if (remote.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return remote;
            Console.Error.WriteLine($"[agent-client] 服务器 {remote} 未配置（agent remote add <name> <ip> [port]）");
            return null;
        }
        // 1. 本地：serve 组件已下载 → 回环
        if (Directory.Exists(Path.Combine(ComponentsDir, "serve")))
            return "http://127.0.0.1:8890";
        // 2. 缓存 remote（last-used）
        var last = ServerRegistry.GetLastUsed();
        if (last is { Length: > 0 })
        {
            var url = last.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? last
                : new ServerRegistry().Find(last)?.Url;
            if (url is not null) return url;
        }
        // 3. 本地（自动下载 serve 组件）→ 回环
        Console.WriteLine("[agent-client] serve 组件未安装，自动下载…");
        RunAgentInstall("serve");
        return "http://127.0.0.1:8890";
    }

    /// <summary>调空壳安装组件（wtangent 在 PATH；组件可自行触发 install）</summary>
    private static void RunAgentInstall(string component)
    {
        try
        {
            using var p = Process.Start(new ProcessStartInfo("wtangent", $"install {component}")
            {
                UseShellExecute = false,
            });
            p?.WaitForExit();
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"[agent-client] 自动安装 {component} 失败（可手动 wtangent install {component}）：{e.Message}");
        }
    }
}
