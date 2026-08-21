namespace WTangent.Tui.Session;

public enum MessageRole
{
    System,
    User,
    Assistant,
    Tool,
}

public class ChatMessage
{
    public MessageRole Role { get; init; } = MessageRole.User;
    public string Content { get; init; } = "";
    /// <summary>assistant 消息的思维链（reasoning 模型，工具调用时须原样回传）</summary>
    public string? ReasoningContent { get; init; }
    /// <summary>assistant 消息的工具调用（OpenAI tool_calls）</summary>
    public List<ToolCall>? ToolCalls { get; init; }
    /// <summary>tool 角色的工具调用 ID</summary>
    public string? ToolCallId { get; init; }
}

/// <summary>工具调用（OpenAI function calling）</summary>
public class ToolCall
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Arguments { get; init; } = ""; // JSON 字符串
}

public class LlmResponse
{
    public string? Content { get; init; }
    public string? ReasoningContent { get; init; }
    public List<ToolCall>? ToolCalls { get; init; }
    public string? Model { get; init; }
    /// <summary>token 用量（不相交：InputTokens 已减去缓存命中）</summary>
    public TokenUsage Usage { get; init; } = new();
}

/// <summary>流式产出：Text=文本增量，ToolCall=完成的工具调用，ReasoningDelta=思维链增量。
/// Usage=流尾 usage 块（仅当请求带 stream_options.include_usage 且提供商上报时出现一次）。</summary>
public class LlmStreamChunk
{
    public string? Text { get; init; }
    public string? ReasoningDelta { get; init; }
    public ToolCall? ToolCall { get; init; }
    public TokenUsage? Usage { get; init; }
}

/// <summary>LLM 客户端抽象：真实 HTTP 实现或假实现（测试/模拟）</summary>
public interface ILlmClient
{
    Task<LlmResponse> ChatAsync(IEnumerable<ChatMessage> messages, string? model = null, IEnumerable<object>? tools = null, CancellationToken ct = default);
    IAsyncEnumerable<LlmStreamChunk> ChatStreamAsync(IEnumerable<ChatMessage> messages, string? model = null, IEnumerable<object>? tools = null, CancellationToken ct = default);
    Task<string[]> ListModelsAsync(CancellationToken ct = default);
}


