using WTangent.Tui.Tui.Chat;
using Terminal.Gui.Editor;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace WTangent.Tui.Tui;

public static partial class TuiRepl
{
    /// <summary>命令目录（命令面板）：名称 + 解释，匹配按两者包含匹配</summary>
    private static readonly (string Name, string Desc)[] SlashCommands =
    [
        ("quit", "退出 TUI"),
        ("reset", "清空消息区"),
        ("thinking", "切换思维链显示模式"),
    ];

    /// <summary>输入历史（bash 风格 ↑/↓ 翻查）：供按键处理与 Submit 共用的可变状态</summary>
    private sealed class InputHistory
    {
        public readonly List<string> Items = [];
        public int Index;      // 指向 Items.Count 时 = 回到当前输入
        public bool Recalling; // 历史回填中（抑制 Index 重置）
    }

    /// <summary>输入框：Enter 发送、Shift/Ctrl+Enter 换行；输入 "/" 弹出命令面板；↑/↓ 翻查历史</summary>
    private static void SetupInput(Editor input, ChatView messages, ListView palette, Action<string> submit, InputHistory history)
    {
        // 解除 Enter 默认换行，Shift+Enter / Ctrl+Enter 绑定原生 NewLine，普通 Enter 兜底发送
        input.KeyBindings.Remove(Key.Enter);
        input.KeyBindings.Add(Key.Enter.WithShift, Command.NewLine);
        input.KeyBindings.Add(Key.Enter.WithCtrl, Command.NewLine);

        // 命令面板按键：KeyDown 先于编辑器绑定，面板可见时劫持方向键/Enter/Esc；隐藏时 ↑/↓ 翻历史
        var filtered = Array.Empty<(string Name, string Desc)>();
        input.KeyDown += (_, e) =>
        {
            if (palette.Visible)
            {
                if (e.KeyCode == Key.CursorUp)
                {
                    palette.SelectedItem = Math.Max(0, (palette.SelectedItem ?? 0) - 1);
                    e.Handled = true;
                    return;
                }
                if (e.KeyCode == Key.CursorDown)
                {
                    palette.SelectedItem = Math.Min(filtered.Length - 1, (palette.SelectedItem ?? 0) + 1);
                    e.Handled = true;
                    return;
                }
                if (e.KeyCode == Key.Esc)
                {
                    palette.Visible = false;
                    e.Handled = true;
                    return;
                }

                if (e.KeyCode != Key.Enter) return;
                if (palette.SelectedItem is { } sel && sel < filtered.Length)
                {
                    palette.Visible = false;
                    submit("/" + filtered[sel].Name);
                }
                e.Handled = true;
                return;
            }

            // bash 风格历史：单行文本时 ↑/↓ 翻历史（多行时保留编辑器光标导航）
            if (input.Text.Contains('\n')) return;
            if (e.KeyCode == Key.CursorUp)
            {
                if (history.Index > 0)
                {
                    history.Index--;
                    SetRecall(history.Items[history.Index]);
                }
                e.Handled = true;
                return;
            }

            if (e.KeyCode != Key.CursorDown) return;
            if (history.Index < history.Items.Count)
            {
                history.Index++;
                SetRecall(history.Index >= history.Items.Count ? "" : history.Items[history.Index]);
            }
            e.Handled = true;
        };

        // 输入变化 → 刷新/隐藏命令面板；手动编辑时历史位置复位
        input.ContentChanged += (_, _) =>
        {
            if (!history.Recalling) history.Index = history.Items.Count;
            UpdatePalette();
        };
        input.KeyDownNotHandled += (_, e) =>
        {
            if (e.KeyCode == Key.T.WithCtrl)
            {
                messages.CycleThinkingMode();
                e.Handled = true;
                return;
            }
            if (e.KeyCode != Key.Enter) return;
            e.Handled = true;
            submit(input.Text);
        };
        return;

        void SetRecall(string text)
        {
            history.Recalling = true;
            input.Text = text;
            input.CaretOffset = text.Length;
            input.SetNeedsDraw();
            history.Recalling = false;
        }

        void UpdatePalette()
        {
            var text = input.Text;
            if (!text.StartsWith('/') || text.Contains('\n'))
            {
                palette.Visible = false;
                return;
            }
            var query = text[1..].Trim();
            filtered = [.. SlashCommands.Where(c => query.Length == 0
                || c.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                || c.Desc.Contains(query, StringComparison.OrdinalIgnoreCase))];
            if (filtered.Length == 0)
            {
                palette.Visible = false;
                return;
            }
            palette.SetSource([.. filtered.Select(c => $"/{c.Name}  {c.Desc}")]);
            palette.SelectedItem = 0;
            var h = Math.Min(filtered.Length, 7) + 2;
            palette.Height = h;
            palette.Y = Pos.AnchorEnd(h + 6);
            palette.Visible = true;
        }
    }
}
