using System.Text.Json;

namespace WTangent.Tui.Tools;

/// <summary>LLM 工具：名称、OpenAI function 定义、执行逻辑</summary>
public interface ITool
{
    /// <summary>工具名（LLM 调用时使用）</summary>
    string Name { get; }

    /// <summary>OpenAI function calling 定义</summary>
    object Definition { get; }

    /// <summary>执行工具，arguments 为 JSON 字符串，返回文本结果</summary>
    Task<string> RunAsync(string arguments, CancellationToken ct = default);
}

/// <summary>从 arguments JSON 读取字符串参数</summary>
internal static class ToolArgs
{
    public static string GetString(string arguments, string prop)
    {
        try
        {
            using var doc = JsonDocument.Parse(arguments);
            if (doc.RootElement.TryGetProperty(prop, out var p))
                return p.GetString() ?? "";
        }
        catch { }
        return "";
    }
}
