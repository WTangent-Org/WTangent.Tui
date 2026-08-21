using System.Diagnostics;
using WTangent.Tui.Session;
using WTangent.Tui.Tui.Chat;
using Terminal.Gui.Drawing;
using Terminal.Gui.Editor;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace WTangent.Tui.Tui;

public static partial class TuiRepl
{
    /// <summary>TUI 聊天会话：serve 的终端客户端。UI（输入 + ChatView 流式）→ RemoteAgentClient（SSE）→ serve。
    /// LLM 全在 serve；本文件只有展示与收集。slash：/quit /reset /thinking。</summary>
    private sealed class TuiReplSession
    {
        private readonly string _url;
        private readonly Window _win;
        private readonly ChatView _messages;
        private readonly Editor _input;
        private readonly ListView _palette;
        private readonly RemoteAgentClient _client;
        private readonly InputHistory _history = new();
        private readonly Label _status;
        private readonly Stopwatch _turnWatch = new();
        private string _turnInfo = "";
        private bool _busy;

        public TuiReplSession(string url)
        {
            _url = url;
            _win = new Window { Title = "Agent TUI", X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };

            // 消息区：完整 Markdown 渲染 + thinking/工具折叠 + 滚动跟随
            _messages = new ChatView() { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() - 5 };
            var sep = FullWidthSep(Pos.Bottom(_messages));
            _status = new Label { X = 1, Y = Pos.Bottom(sep), Width = Dim.Fill(1) };
            var sep2 = FullWidthSep(Pos.Bottom(_status));
            _input = new Editor { X = 0, Y = Pos.Bottom(sep2), Width = Dim.Fill(), Height = 3, Multiline = true, CanFocus = true };

            // 命令面板：输入 "/" 时浮在输入框上方
            _palette = new ListView
            {
                X = 1,
                Y = Pos.AnchorEnd(13),
                Width = Dim.Fill(2),
                Height = 7,
                Visible = false,
                BorderStyle = LineStyle.Single
            };

            _win.Add(_messages, sep, _status, sep2, _input, _palette);
            ApplyTheme(_win);
            _input.SetFocus();

            // Agent 事件 → ChatView（RemoteAgentClient 桥接 serve 的流）
            var events = new AgentEventsImpl(
                onDelta: d => { _messages.CommitThinking(); _messages.AppendAssistant(d, stream: true); },
                onReasoning: d => { _messages.StartThinking(); _messages.OnThinkingDelta(d); },
                onToolStart: _messages.OnToolStart,
                onToolEnd: _messages.OnToolEnd,
                onTurnEnd: _ => _messages.CommitThinking());
            _client = new RemoteAgentClient(new Uri(url)) { Events = events };
        }

        public void Start()
        {
            _status.Text = $"serve: {_url}";
            _status.SetNeedsDraw();
            SetupInput(_input, _messages, _palette, Submit, _history);
            // ApplicationImpl.Run 内部 finally 已自动 End(token) 并释放 Runnable，无需手动 End
            App.Run(_win);
        }

        private void Submit(string? forced = null)
        {
            var prompt = (forced ?? _input.Text).Trim();
            if (prompt.Length == 0) return;
            // 记入历史（bash 风格 ↑/↓ 翻查；连续重复去重）
            if (_history.Items.Count == 0 || _history.Items[^1] != prompt) _history.Items.Add(prompt);
            _history.Index = _history.Items.Count;
            _input.Text = "";
            _input.SetNeedsDraw();
            var app = App;
            switch (prompt)
            {
                case "/quit":
                    app.RequestStop();
                    return;
                case "/reset":
                    _messages.ClearAll();
                    _messages.AppendAssistant("[消息区已清空]");
                    return;
                case var p when p.StartsWith("/thinking"):
                    _messages.CycleThinkingMode();
                    _messages.AppendUser($"/thinking → {_messages.ThinkingMode}");
                    return;
            }

            if (_busy) return;
            _busy = true;
            _messages.FollowBottom();
            _messages.AppendUser(prompt);
            _turnWatch.Restart();
            _turnInfo = "";
            _ = Task.Run(async () =>
            {
                try
                {
                    await _client.AskAsync(prompt);
                }
                catch (Exception ex)
                {
                    _messages.AppendAssistant($"[error] {ex.Message}");
                }
                _messages.CommitThinking();
                var dur = _turnWatch.Elapsed.TotalSeconds;
                app.Invoke(() =>
                {
                    _busy = false;
                    _turnInfo = $"耗时 {dur:0.0}s";
                    RefreshStatus();
                });
            });
        }

        private void RefreshStatus()
        {
            var parts = new List<string> { $"serve: {_url}" };
            if (_turnInfo.Length > 0) parts.Add(_turnInfo);
            _status.Text = string.Join("  |  ", parts);
            _status.SetNeedsDraw();
        }
    }
}
