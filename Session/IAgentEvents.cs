namespace WTangent.Tui.Session;

/// <summary>Agent 会话事件回调（Pi 风格：turn / tool / message 生命周期）</summary>
public interface IAgentEvents
{
    /// <summary>一轮开始（LLM 收到 prompt）</summary>
    void OnTurnStart() { }
    /// <summary>LLM 回复增量（流式文本）</summary>
    void OnMessageDelta(string delta) { }
    /// <summary>思维链增量（reasoning 模型，可选显示）</summary>
    void OnReasoningDelta(string delta) { }
    /// <summary>工具开始执行</summary>
    void OnToolStart(string name, string arguments) { }
    /// <summary>工具执行完成</summary>
    void OnToolEnd(string name, string result) { }
    /// <summary>一轮完成（含工具结果）</summary>
    void OnTurnEnd(string? finalText) { }
}
