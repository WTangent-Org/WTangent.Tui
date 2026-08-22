using WTangent.Core;

namespace WTangent.Tui;

/// <summary>tui 组件入口（[AgentEntry] 元数据 + 生命周期钩子；纯 UI：只有顶级 Default）。</summary>
[AgentEntry("tui", "tui 终端", false)]
public sealed partial class Entry : IEntry
{
    /// <summary>顶级行为：TUI 终端聊天</summary>
    public Func<string[], int>? Default => Defaults.RunTui;

    [EntryStart]
    private void OnStart(Application app) { }

    [EntryStop]
    private void OnStop() { }
}
