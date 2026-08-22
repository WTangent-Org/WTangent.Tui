using WTangent.Core;

namespace WTangent.Tui;

/// <summary>tui 组件入口（手写实现 IEntry）：纯 UI 组件，只有顶级 Default（TUI 终端聊天）。
/// 生命周期：StartAsync 存宿主注入的 Application（组件内静态访问 Entry.App）。</summary>
public sealed class Entry : IEntry
{
    /// <summary>宿主运行时上下文（StartAsync 注入；组件内部静态访问）</summary>
    public static Application? App { get; private set; }

    public string Identifier => "tui";
    public string Name => "tui 终端";
    public Func<string[], int>? Default => Defaults.RunTui;

    public Task StartAsync(Application app)
    {
        App = app;
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        App = null;
        return Task.CompletedTask;
    }
}
