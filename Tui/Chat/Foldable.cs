namespace WTangent.Tui.Tui.Chat;

/// <summary>thinking 显示模式：Hide=折叠成一行，Expand=恒展开</summary>
public enum ThinkingMode { Hide, Expand }

/// <summary>折叠块：标记行 + 内容。折叠/展开 = 替换 [StartOffset, StartOffset+Length) 区间。
/// Render 按状态生成文档文本：thinking=加粗标记行 + 思维链；tool=加粗标题行 + fenced 结果块。</summary>
internal sealed class Foldable
{
    /// <summary>工具结果默认显示行数上限（dsh TerminalBlock 同款 16 行；超出折叠，点标题展开）</summary>
    internal const int MaxToolLines = 16;

    public required string Kind { get; init; }        // "thinking" | "tool"
    public required string Title { get; init; }       // 无前缀标题（tool: 参数/命令；thinking: 摘要）
    public required string Content { get; init; }     // 思维链 / 工具结果全文
    public required int StartOffset { get; set; }     // 标记行在 Text 中的起点（替换/平移时更新）
    public required int Length { get; set; }          // 当前渲染占位长度
    public required bool Collapsed { get; set; }
    public bool CanFold { get; init; }                // 工具结果是否超限（短结果不显示折叠按钮）
    public string Duration { get; init; } = "";
    public string Command { get; init; } = "";        // bash 命令（非空 → 终端样式：$ 命令 + 输出同块）
    public string Name { get; init; } = "";           // 显示名（bash→pwsh、think、glob…）
    public string Icon { get; init; } = "";           // 行头图标（❯ ✦ ⌕…，dsh 风格）

    /// <summary>图标 + 名称徽标（如 "❯ pwsh"）</summary>
    private string Badge => $"{Icon} {Name}".Trim();

    /// <summary>标记行前缀（行号定位用，与当前折叠状态/按钮一致；markdown 加粗渲染后可见文本不含 **）</summary>
    public string Marker => Kind == "thinking"
        ? (Collapsed ? $"+ {Badge} · {Title}{Duration}" : $"- {Badge}{Duration}")
        : Collapsed ? $"+ {Badge} · {ShortTitle}"
        : CanFold ? $"- {Badge} · {ShortTitle}" : $"{Badge} · {ShortTitle}";

    /// <summary>长标题截断（工具名 + 参数过长时折叠展示）</summary>
    private string ShortTitle => Title.Length <= 44 ? Title : Title[..41] + "…";

    /// <summary>渲染用标题：markdown 特殊字符转义后（glob 的 ** 等与加粗标记冲突 → 标记行解析错乱、点击失效）</summary>
    private string DisplayTitle => MdEscape(ShortTitle);

    /// <summary>转义标题里的 markdown 内联标记（\ * _ `）。Marker 匹配仍用原始文本：转义渲染后可见文本 == 原始文本。</summary>
    private static string MdEscape(string s) =>
        s.Replace(@"\", @"\\").Replace("*", @"\*").Replace("_", @"\_").Replace("`", @"\`");

    /// <summary>匹配用前缀：截断防窄终端 wrap 导致完整标题不匹配</summary>
    public string MarkerPrefix => Marker[..Math.Min(Marker.Length, 15)];

    /// <summary>渲染折叠块文本。thinking：加粗标记行 + 思维链；tool：加粗徽标行 + fenced 结果块。</summary>
    public string Render(int viewportWidth)
    {
        if (Kind == "thinking")
        {
            var mark = Collapsed ? "+" : "-";
            if (Collapsed)
                return $"**{mark} {Badge} · {MdEscape(Title)}{Duration}**\n\n";
            // 展开：标记行（加粗、无摘要），思维链全文（普通文本，不带 > 前缀）
            return $"**{mark} {Badge}{Duration}**\n\n{Content}";
        }
        if (string.IsNullOrWhiteSpace(Content))
            return $"**{Badge} · {DisplayTitle}**（无输出）\n\n";
        // bash：终端样式 —— $ 命令提示行 + 输出同块（fence 带语言标签；代码块自带 dimmed 深色底）
        var transcript = Command.Length > 0;
        var fence = transcript ? "````bash" : "````";
        var prompt = transcript ? $"$ {Command}\n" : "";
        if (Collapsed)
        {
            var (outText, overflow) = CollapseResult(Content, MaxToolLines, MaxToolLines * Math.Max(20, viewportWidth - 6));
            var totalLines = Content.Count(c => c == '\n') + 1;
            var hint = overflow
                ? transcript ? $"… 其余 {totalLines - MaxToolLines} 行" : $"…（共 {totalLines} 行）"
                : "";
            // 注意：hint 独占一行、收尾围栏独占一行（围栏与文字粘同行会被 Markdown 视为未闭合 → 下方全部变代码色）
            return $"**+ {Badge} · {DisplayTitle}**\n\n{fence}\n{prompt}{outText}{hint}\n````\n\n";
        }
        // 展开态：标记行加粗（可折叠回），结果在代码块内
        var title = CanFold ? $"**- {Badge} · {DisplayTitle}**" : $"**{Badge} · {DisplayTitle}**";
        return $"{title}\n\n{fence}\n{prompt}{Content}\n````\n\n";
    }

    /// <summary>思维链摘要：首个非空行，strip markdown 标记，60 字符截断</summary>
    public static string Summarize(string md)
    {
        foreach (var t in md.Split('\n').Select(line => line.Trim().TrimStart('#', '-', '>', '*', ' ', '\t')).Where(t => t.Length > 0))
        {
            return t.Length <= 60 ? t : t[..57] + "...";
        }

        return "thought";
    }

    /// <summary>结果折叠：超 maxLines 行或超 maxChars 字符 → 截断（折叠状态由标题行 +/- 指示）</summary>
    public static (string Output, bool Overflow) CollapseResult(string result, int maxLines, int maxChars)
    {
        var trimmed = result.TrimEnd();
        var lines = trimmed.Split('\n');
        if (lines.Length <= maxLines && trimmed.Length <= maxChars) return (trimmed + "\n", false);
        var outText = string.Join('\n', lines[..maxLines]);
        if (outText.Length > maxChars) outText = outText[..maxChars];
        return (outText.TrimEnd() + "\n", true);
    }
}
