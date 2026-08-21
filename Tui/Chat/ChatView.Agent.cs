using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace WTangent.Tui.Tui.Chat;

/// <summary>Agent 事件接线：thinking/工具调用的 spinner 动画与折叠块生命周期。
/// 位置字段的读写都在 Invoke 回调内（FIFO 保证一致）。</summary>
public sealed partial class ChatView
{
    private readonly string[] _spinnerFrames = ["⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏"];
    private int _spinnerIdx;
    private Spinner? _thinkingSpinner;
    private Spinner? _toolSpinner;
    private string? _toolTitle;   // 后台线程写/读（同一事件线程），title 经闭包进主线程
    private readonly StringBuilder _streamBuf = new();
    private object? _flushToken;
    private readonly Stopwatch _thinkingWatch = new();
    private readonly StringBuilder _currentThinking = new();
    private bool _thinkingPending;

    /// <summary>工具标题主参优先级：bash→command，文件/搜索→path/pattern 等，避免标题显示原始 JSON 大括号</summary>
    private static readonly string[] MainArgKeys = ["command", "path", "pattern", "search", "query", "file"];

    /// <summary>spinner 状态（thinking/工具共用）：占位行起点 + 动画定时器</summary>
    private sealed class Spinner(int start, object? token)
    {
        public int Start { get; } = start;
        public object? Token { get; set; } = token;
    }

    /// <summary>流式正文：段落边界 flush（150ms 防表格/代码中途渲染）</summary>
    private void AppendStreaming(string text)
    {
        _streamBuf.Append(text);
        _flushToken ??= TuiRepl.App.AddTimeout(TimeSpan.FromMilliseconds(150), () =>
        {
            _flushToken = null;
            var buf = _streamBuf.ToString();
            var lastBreak = buf.LastIndexOf("\n\n", StringComparison.Ordinal);
            if (lastBreak <= 0) return _streamBuf.Length > 0;
            _streamBuf.Remove(0, lastBreak + 2);
            Invoke(() =>
            {
                Text += buf[..(lastBreak + 2)];
                ScrollToEnd();
                SetNeedsDraw();
            });
            return _streamBuf.Length > 0;
        });
    }

    private void FlushStream()
    {
        if (_streamBuf.Length == 0) return;
        var chunk = _streamBuf.ToString();
        _streamBuf.Clear();
        Invoke(() => { Text += chunk; ScrollToEnd(); SetNeedsDraw(); });
    }

    /// <summary>启动 thinking：spinner 行 + 计时</summary>
    public void StartThinking()
    {
        if (_thinkingPending) return;
        _thinkingPending = true;
        _thinkingWatch.Restart();
        _spinnerIdx = 0;
        Invoke(() => _thinkingSpinner = BeginSpinner("Thinking"));
    }

    public void OnThinkingDelta(string delta) => _currentThinking.Append(delta);

    /// <summary>结束 thinking：spinner 行 → 折叠块（hide 折叠 / expand 展开），带摘要+时长</summary>
    public void CommitThinking()
    {
        FlushStream();
        if (!_thinkingPending) return;
        _thinkingPending = false;
        _thinkingWatch.Stop();
        var content = _currentThinking.ToString();
        _currentThinking.Clear();
        Invoke(() =>
        {
            var spinner = TakeSpinner(ref _thinkingSpinner);
            if (spinner is null || Text.Length < spinner.Start) return;
            var dur = $" · {_thinkingWatch.Elapsed.TotalSeconds:0.0}s";
            if (string.IsNullOrWhiteSpace(content))
            {
                Text = Text[..spinner.Start] + "\n\n";
            }
            else
            {
                var f = new Foldable
                {
                    Kind = "thinking",
                    Title = Foldable.Summarize(content),
                    Content = content.TrimEnd() + "\n\n",
                    Name = "think",
                    Icon = "\uE734",   // Segoe MDL2 星标（sparkle）
                    StartOffset = spinner.Start,
                    Length = Text.Length - spinner.Start,
                    Collapsed = _thinkingMode == ThinkingMode.Hide,
                    Duration = dur,
                };
                ReplaceFoldable(f, f.Render(ContentWidth));
                _foldables.Add(f);
            }
            ScrollToEnd();
            SetNeedsDraw();
            ScheduleRebuildMarkers();
        });
    }

    /// <summary>工具调用开始：spinner 标记行（title 闭包传递，不跨线程读字段）</summary>
    public void OnToolStart(string name, string args)
    {
        CommitThinking();
        _toolTitle = FormatToolTitle(name, args);
        var title = _toolTitle;
        _spinnerIdx = 0;
        Invoke(() => _toolSpinner = BeginSpinner($"→ {title}"));
    }

    /// <summary>工具调用结束：spinner 行 → 结果块（超 16 行折叠，点击标题行展开）。
    /// bash 结果首行 `[exit N]` 并入标题；bash 命令进 $ 提示行（终端样式），行头 = 图标 + 名称徽标。</summary>
    public void OnToolEnd(string name, string result)
    {
        var main = ArgsOf(name, _toolTitle);
        var title = main;   // 名称进徽标（ToolBadge），标题只留参数/命令
        var content = result.TrimEnd();
        var firstLine = content.Split('\n')[0].Trim();
        if (firstLine.StartsWith("[exit ", StringComparison.Ordinal) && firstLine.EndsWith(']')
            && int.TryParse(firstLine["[exit ".Length..^1], out var code))
        {
            title += $" [exit {code}]";
            content = content.Length > firstLine.Length ? content[(firstLine.Length + 1)..] : "";
        }
        var command = name == "bash" ? main : "";
        var (label, icon) = ToolBadge(name);
        Invoke(() =>
        {
            var spinner = TakeSpinner(ref _toolSpinner);
            if (spinner is null || Text.Length < spinner.Start) return;
            var (_, overflow) = Foldable.CollapseResult(content, Foldable.MaxToolLines, Foldable.MaxToolLines * Math.Max(20, ContentWidth - 6));
            var f = new Foldable
            {
                Kind = "tool",
                Title = title,
                Content = content,
                Command = command,
                Name = label,
                Icon = icon,
                StartOffset = spinner.Start,
                Length = Text.Length - spinner.Start,
                Collapsed = overflow,
                CanFold = overflow,
            };
            ReplaceFoldable(f, f.Render(ContentWidth));
            _foldables.Add(f);
            ScrollToEnd();
            SetNeedsDraw();
            ScheduleRebuildMarkers();
        });
    }

    /// <summary>在文本末尾启动 spinner 动画行（Invoke 内调用）</summary>
    private Spinner BeginSpinner(string line)
    {
        EnsureBlockEnd();
        var start = Text.Length;
        Text += $"{_spinnerFrames[0]} {line}\n\n";
        var token = TuiRepl.App.AddTimeout(TimeSpan.FromMilliseconds(80), () =>
        {
            _spinnerIdx = (_spinnerIdx + 1) % _spinnerFrames.Length;
            Invoke(() =>
            {
                if (Text.Length >= start)
                    Text = Text[..start] + $"{_spinnerFrames[_spinnerIdx]} {line}\n\n";
                SetNeedsDraw();
            });
            return true;
        });
        return new Spinner(start, token);
    }

    /// <summary>取出并停止 spinner（Invoke 内调用）：置空字段 + 移除定时器，返回 null 表示不存在</summary>
    private Spinner? TakeSpinner(ref Spinner? field)
    {
        var s = field;
        field = null;
        if (s is null) return null;
        if (s.Token == null) return s;
        TuiRepl.App.RemoveTimeout(s.Token);
        s.Token = null;
        return s;
    }

    private static string ArgsOf(string name, string? fullTitle) =>
        fullTitle is { Length: > 0 } && fullTitle.StartsWith(name, StringComparison.Ordinal) ? fullTitle[name.Length..].TrimStart() : "";

    /// <summary>工具行头徽标：显示名 + 图标（Segoe MDL2 Assets，XAML 同源图标字体）。
    /// bash 在 Windows 实为 pwsh（E756 命令提示符）；glob/grep 用放大镜（E721）；其余 sparkle 星标（E734）。
    /// 注意：PUA 字形依赖终端字体回退（Windows Terminal 通常可渲染，部分终端显示方框，届时换回 Unicode）。</summary>
    private static (string Label, string Icon) ToolBadge(string name) => name switch
    {
        "bash" => ("pwsh", "\uE756"),
        "glob" => ("glob", "\uE721"),
        "grep" => ("grep", "\uE721"),
        _ => (name, "\uE734"),
    };

    /// <summary>工具标题美化：args 是 JSON（{"command":"..."} 等），提取主参显示，不裸贴 JSON 大括号</summary>
    private static string FormatToolTitle(string name, string args)
    {
        var main = ExtractMainArg(args);
        return main.Length > 0 ? $"{name} {main}" : $"{name} {args}".TrimEnd();
    }

    /// <summary>从工具参数 JSON 提取展示主参：按 MainArgKeys 优先级找，找不到取首个字符串值</summary>
    private static string ExtractMainArg(string args)
    {
        try
        {
            using var doc = JsonDocument.Parse(args);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return "";
            foreach (var key in MainArgKeys)
            {
                if (!root.TryGetProperty(key, out var el) || el.ValueKind != JsonValueKind.String) continue;
                var s = el.GetString();
                if (s is { Length: > 0 }) return CollapseSpaces(s);
            }
            foreach (var prop in root.EnumerateObject())
            {
                if (prop.Value.ValueKind != JsonValueKind.String) continue;
                var s = prop.Value.GetString();
                if (s is { Length: > 0 }) return CollapseSpaces(s);
            }
        }
        catch { }
        return "";
    }

    /// <summary>标题单行化：折叠 \r\n\t（命令可能含换行）</summary>
    private static string CollapseSpaces(string s) =>
        string.Join(' ', s.Split(['\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries));
}
