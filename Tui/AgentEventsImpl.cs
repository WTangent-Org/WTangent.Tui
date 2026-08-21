using WTangent.Tui.Session;

namespace WTangent.Tui.Tui;

/// <summary>Agent 事件 → TUI 回调的适配器（流式渲染 ChatView）</summary>
internal sealed class AgentEventsImpl(
    Action<string> onDelta,
    Action<string> onReasoning,
    Action<string, string> onToolStart,
    Action<string, string> onToolEnd,
    Action<string?> onTurnEnd) : IAgentEvents
{
    public void OnMessageDelta(string delta) => onDelta(delta);
    public void OnReasoningDelta(string delta) => onReasoning(delta);
    public void OnToolStart(string name, string arguments) => onToolStart(name, arguments);
    public void OnToolEnd(string name, string result) => onToolEnd(name, result);
    public void OnTurnEnd(string? finalText) => onTurnEnd(finalText);
}
