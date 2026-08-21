using System.CommandLine;
using System.Diagnostics;

namespace WTangent.Tui.Commands;


/// <summary>用默认浏览器打开目标 serve 的 Web UI（agent web [<remote>]）</summary>
[AgentComponent]
public sealed class WebCommand : Command
{
    public WebCommand() : base("web", "浏览器打开目标 serve 的 Web UI")
    {
        var remote = new Argument<string?>("remote")
        {
            Description = "服务器名 / ET 加入码 / URL（缺省：本地服务器 → 缓存 → 回环 8890）",
            Arity = ArgumentArity.ZeroOrOne
        };
        Add(remote);

        SetAction(pr =>
        {
            var url = ClientPaths.ResolveUrl(pr.GetValue(remote));
            if (url is null) return 1;
            Console.WriteLine($"[agent-client] 打开 {url} …");
            try
            {
                if (OperatingSystem.IsWindows())
                    Process.Start(new ProcessStartInfo("cmd", $"/c start \"\" \"{url}\"") { UseShellExecute = false });
                else if (OperatingSystem.IsMacOS())
                    Process.Start("open", url);
                else
                    Process.Start("xdg-open", url);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"[agent-client] 打开浏览器失败：{e.Message}（手动访问 {url}）");
                return 1;
            }
            return 0;
        });
    }
}
