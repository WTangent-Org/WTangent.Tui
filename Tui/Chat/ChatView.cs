using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace WTangent.Tui.Tui.Chat;

/// <summary>聊天消息区：单 Markdown 文档全量渲染（复制/滚动/表格/代码块可靠）。
/// 折叠块（thinking/工具结果，见 Foldable.cs）用 offset 字符串替换实现：标记行可点击展开/折叠。
/// 流式正文按段落（\n\n）边界 flush；点击定位用 RenderToAnsi 行号。</summary>
public sealed partial class ChatView : Markdown
{
    private readonly List<Foldable> _foldables = [];
    private readonly List<int> _markerRows = [];
    private object? _markerToken;
    private object? _scrollToken;
    private ThinkingMode _thinkingMode = ThinkingMode.Hide;
    private int _lastMarkerWidth = -1;
    private int? _pendingRestoreY;   // 折叠/展开后待恢复的滚动位置（布局时生效，避免跳顶）
    private bool _followBottom = true;   // 跟随底部：用户上滚阅读历史时暂停（否则每次文本变化都会被 Markdown 内部回顶拽走）

    private int MaxY => Math.Max(0, LineCount - Viewport.Height);
    private bool IsAtBottom => Viewport.Y >= MaxY;

    public ChatView()
    {
        CanFocus = true;
        ShowHeadingPrefix = false;
        ViewportSettings &= ~ViewportSettingsFlags.HasVerticalScrollBar;
        // 终端 resize（宽度变化）→ 渲染行号变化，需重建 marker；纯滚动（Y 变）不需要
        ViewportChanged += (_, _) =>
        {
            SetNeedsDraw();
            if (Viewport.Width != _lastMarkerWidth) ScheduleRebuildMarkers();
        };
    }

    /// <summary>thinking 显示模式（/thinking 或 Ctrl+T 切换）</summary>
    public ThinkingMode ThinkingMode
    {
        get => _thinkingMode;
        private set
        {
            if (_thinkingMode == value) return;
            _thinkingMode = value;
            _pendingRestoreY = Viewport.Y;
            // expand 模式全展开，hide 模式全折叠（thinking 块联动）
            for (var i = _foldables.Count - 1; i >= 0; i--)
            {
                var f = _foldables[i];
                if (f.Kind != "thinking") continue;
                var target = _thinkingMode == ThinkingMode.Hide;
                if (f.Collapsed == target) continue;
                f.Collapsed = target;
                ReplaceFoldable(f, f.Render(ContentWidth));
            }
            SetNeedsDraw();
            ScheduleRebuildMarkers();
        }
    }

    public void CycleThinkingMode() =>
        ThinkingMode = _thinkingMode == ThinkingMode.Hide ? ThinkingMode.Expand : ThinkingMode.Hide;

    private int ContentWidth => Math.Max(1, Viewport.Width);

    /// <summary>追加内容。stream=true：流式正文（段落 \n\n 边界 flush）；false：块级消息</summary>
    private void Append(string text, bool stream = false)
    {
        if (stream)
        {
            AppendStreaming(text);
            return;
        }
        Invoke(() =>
        {
            EnsureBlockEnd();
            Text += text + "\n\n";
            ScrollToEnd();
            SetNeedsDraw();
        });
    }

    public void AppendUser(string text) => Append(text);

    public void AppendAssistant(string text, bool stream = false) => Append(text, stream);

    public void ClearAll() => Invoke(() =>
    {
        Text = "";
        _foldables.Clear();
        _markerRows.Clear();
        _streamBuf.Clear();
        _thinkingPending = false;
        _currentThinking.Clear();
    });

    /// <summary>布局钩子：Text 变化触发 Markdown 内部 scroll-to-top，在此守卫滚动位置——
    /// 跟随模式下重建后即时贴回底部（消除回顶↔回底抽搐）；折叠切换则恢复原位置。</summary>
    protected override void OnSubViewLayout(LayoutEventArgs args)
    {
        base.OnSubViewLayout(args);
        if (_pendingRestoreY is { } y)
        {
            _pendingRestoreY = null;
            Viewport = Viewport with { Y = Math.Min(y, MaxY) };
            _followBottom = IsAtBottom;
            return;
        }
        if (_followBottom)
            Viewport = Viewport with { Y = MaxY };
    }

    /// <summary>滚动键（上下/翻页/Home/End）手动处理：滚动后同步跟随状态（OnKeyDown 先于命令绑定执行，直接接管更可靠）</summary>
    protected override bool OnKeyDown(Key key)
    {
        switch (key.KeyCode)
        {
            case KeyCode.CursorUp:
                ScrollVertical(-1);
                break;
            case KeyCode.CursorDown:
                ScrollVertical(1);
                break;
            case KeyCode.PageUp:
                ScrollVertical(-Math.Max(1, Viewport.Height - 1));
                break;
            case KeyCode.PageDown:
                ScrollVertical(Math.Max(1, Viewport.Height - 1));
                break;
            case KeyCode.Home:
                Viewport = Viewport with { Y = 0 };
                break;
            case KeyCode.End:
                Viewport = Viewport with { Y = MaxY };
                break;
            default:
                return base.OnKeyDown(key);
        }
        _followBottom = IsAtBottom;
        SetNeedsDraw();
        return true;
    }

    private void EnsureBlockEnd()
    {
        var trimmed = Text.TrimEnd('\n');
        if (trimmed.Length > 0)
            Text = trimmed + "\n\n";
    }

    private void ScrollToEnd()
    {
        SetNeedsLayout();
        if (_scrollToken != null) return;
        _scrollToken = TuiRepl.App.AddTimeout(TimeSpan.FromMilliseconds(50), () =>
        {
            _scrollToken = null;
            if (_followBottom)
                Viewport = Viewport with { Y = MaxY };
            SetNeedsDraw();
            return false;
        });
    }

    /// <summary>发送消息后：恢复跟随并回到底部（用户此前上滚阅读时也会被拉回，符合聊天预期）</summary>
    public void FollowBottom()
    {
        _followBottom = true;
        ScrollToEnd();
    }

    private void Invoke(Action a) => TuiRepl.App.Invoke(a);
}
