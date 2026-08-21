using System.CommandLine;
using WTangent.Tui.Store;

namespace WTangent.Tui.Commands;


/// <summary>服务器注册表（顶层命令）：agent remote add/list/remove/user/passwd。
/// add：&lt;name&gt; &lt;ip&gt; [port] [加入码]（有加入码=et，无=lan）；账号是**全局**凭据：agent remote user &lt;名&gt; / passwd &lt;密码&gt;。
/// default-server = last-used 缓存（run/clone 自动更新），无需配置。</summary>
[AgentComponent]
public sealed class RemoteCommand : Command
{
    public RemoteCommand() : base("remote", "服务器注册表：list / add <name> <ip> [port] [加入码] / remove / user / passwd")
    {
        var list = new Command("list", "列出服务器（类型/名称/IP:端口/加入码）+ 全局用户");
        list.SetAction(_ =>
        {
            var cred = AgentCredentials.Load();
            var userLine = cred.User is { Length: > 0 } ? $"  全局用户:{cred.User}" : "  全局用户:（未设置，agent remote user <名>）";
            Console.WriteLine(userLine);
            var items = new ServerRegistry().List();
            if (items.Count == 0) Console.WriteLine("（无服务器，用 agent remote add <name> <ip> [port] [加入码] 添加）");
            foreach (var r in items)
            {
                var code = r.EtCode is { Length: > 0 } ? $"  加入码:{r.EtCode}" : "";
                Console.WriteLine($"{r.Kind}\t{r.Name}\t{r.Host}:{r.Port}{code}");
            }
            return 0;
        });

        var nameArg = new Argument<string>("name");
        var ipArg = new Argument<string>("ip") { Description = "IP 地址（lan=局域网 IP；et=ET 虚拟地址；不要 http://）" };
        var portArg = new Argument<int?>("port")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "端口（ip 后可选，缺省 8890）",
        };
        var codeArg = new Argument<string?>("code")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "EasyTier 加入码（可选：有=et 跨网；无=lan 局域网直连）",
        };
        var add = new Command("add", "添加服务器：add <name> <ip> [port] [加入码]（有加入码=et 跨网，无=lan 局域网）")
        {
            nameArg,
            ipArg,
            portArg,
            codeArg,
        };
        add.SetAction(pr =>
        {
            var name = pr.GetValue(nameArg);
            var ip = pr.GetValue(ipArg);
            if (name is null || ip is null) return 1;
            var port = pr.GetValue(portArg) is { } p and > 0 ? p : 8890;
            var code = pr.GetValue(codeArg);
            var kind = code is { Length: > 0 } ? "et" : "lan";
            new ServerRegistry().Add(name, ip, port, code, kind);
            Console.WriteLine(kind == "et"
                ? $"[remote] et {name} → {ip}:{port}  加入码:{code}"
                : $"[remote] lan {name} → {ip}:{port}");
            return 0;
        });

        var remove = new Command("remove", "移除服务器") { nameArg };
        remove.SetAction(pr =>
        {
            var name = pr.GetValue(nameArg);
            if (name is null) return 1;
            new ServerRegistry().Remove(name);
            return 0;
        });

        // 全局账号（所有 remote 共用，鉴权用）
        var userArg = new Argument<string>("user");
        var userCmd = new Command("user", "设置全局用户名（所有 remote 共用）") { userArg };
        userCmd.SetAction(pr =>
        {
            var u = pr.GetValue(userArg);
            if (u is null) return 1;
            var cred = AgentCredentials.Load();
            cred.User = u;
            cred.Save();
            Console.WriteLine($"[remote] 全局用户 = {u}");
            return 0;
        });

        var passwdArg = new Argument<string>("passwd");
        var passwdCmd = new Command("passwd", "设置全局密码（所有 remote 共用；明文存储，未来加密）") { passwdArg };
        passwdCmd.SetAction(pr =>
        {
            var p = pr.GetValue(passwdArg);
            if (p is null) return 1;
            var cred = AgentCredentials.Load();
            cred.Passwd = p;
            cred.Save();
            Console.WriteLine("[remote] 全局密码已设置");
            return 0;
        });

        Add(list);
        Add(add);
        Add(remove);
        Add(userCmd);
        Add(passwdCmd);
    }
}
