using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using WTangent.Tui.Store;

namespace WTangent.Tui.Tui;

/// <summary>终端聊天界面（serve 的客户端，与 WUI 平级）：输入框 + ChatView 流式渲染（thinking/工具折叠）。
/// LLM 全在 serve；TUI 只连 RemoteAgentClient（SSE），headless 终端也能聊。入口：agent tui [remote]。</summary>
public static partial class TuiRepl
{
    /// <summary>自定义主题名（ApplyTheme 注册）</summary>
    public const string ThemeName = "agent";

    /// <summary>当前 TUI 应用实例（Run 期间非 null；null! 初始化免除使用处空判断）</summary>
    public static IApplication App { get; private set; } = null!;

    /// <summary>异步入口：TG 生命周期（Create/Init/Run/Dispose）固定在同一线程（Task.Run）。</summary>
    public static Task RunAsync(string url) => Task.Run(() => RunCore(url));

    private static void RunCore(string url)
    {
        App = Application.Create();
        App.Init();
        // 危险命令确认：TUI 内联 Dialog（ShowDialog 内部走公开的 App + UiDispatcher）
        ConfirmProvider.Confirm = prompt => ShowDialog("危险命令确认", prompt);
        try
        {
            new TuiReplSession(url).Start();
        }
        finally
        {
            ConfirmProvider.Confirm = ConfirmProvider.DefaultConfirm;
            App.Dispose();
        }
    }

    /// <summary>通用模态消息弹窗（agent 主题；MessageBox 用默认主题会跳色，统一走这里）。
    /// 经公开的 App + UiDispatcher 保证在 UI 主线程运行。返回点击的按钮下标（0 起；Esc/关闭 = -1），
    /// 最后一个按钮为默认（Enter 触发）。</summary>
    public static int ShowMessage(string title, string message, params string[] buttons)
    {
        return UiDispatcher.Invoke(() =>
        {
            var result = -1;
            var dialog = new Dialog
            {
                Title = title,
                Width = Math.Max(50, Math.Min(90, message.Length / 2 + 20)),
                Height = Math.Clamp(message.Split('\n').Length + 7, 9, 18),
                X = Pos.Center(),
                Y = Pos.Center(),
                SchemeName = ThemeName,
            };
            dialog.Add(new Label { X = 1, Y = 1, Width = Dim.Fill(2), Text = message });
            for (var i = 0; i < buttons.Length; i++)
            {
                var idx = i;
                var b = new Button { Text = buttons[i] };
                b.Accepting += (_, _) => { result = idx; dialog.RequestStop(); };
                dialog.AddButton(b);
            }
            if (buttons.Length > 0)
                dialog.DefaultAcceptView = dialog.Buttons[^1];   // 最后一个按钮 = 默认（Enter）
            App.Run(dialog);
            return result;
        });
    }

    /// <summary>模态确认（危险命令）：拒绝/允许，Enter=允许（最后一个按钮）。统一走 ShowMessage。</summary>
    private static bool ShowDialog(string title, string message) =>
        ShowMessage(title, message, "拒绝", "允许") == 1;
}
