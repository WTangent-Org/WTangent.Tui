using Terminal.Gui.Configuration;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Attr = Terminal.Gui.Drawing.Attribute;

namespace WTangent.Tui.Tui;

public static partial class TuiRepl
{
    /// <summary>注册自定义深色主题并应用到主窗口（子视图继承）：agent = 主主题，agent-dim = 分隔线/弱化色</summary>
    private static void ApplyTheme(Window win)
    {
        if (SchemeManager.GetSchemeNames().Contains(ThemeName)) return;   // 幂等（多次 Run 不重复注册）
        var bg = new Color(24, 24, 31);
        var fg = new Color(205, 214, 244);
        var dim = new Color(110, 115, 141);
        var accent = new Color(137, 180, 250);
        var focusBg = new Color(49, 50, 68);
        SchemeManager.AddScheme(ThemeName, new Scheme(new Attr(fg, bg))
        {
            Focus = new Attr(fg, focusBg),
            HotNormal = new Attr(accent, bg),
            HotFocus = new Attr(accent, focusBg),
            Disabled = new Attr(dim, bg),
        });
        SchemeManager.AddScheme("agent-dim", new Scheme(new Attr(dim, bg)));
        win.SchemeName = ThemeName;
    }

    /// <summary>全宽弱化分隔线：初始化即铺满（超宽自动裁剪），resize 后随视口宽度刷新</summary>
    private static Label FullWidthSep(Pos y)
    {
        var sep = new Label { X = 0, Y = y, Width = Dim.Fill(), SchemeName = "agent-dim", Text = new string('─', 200) };
        sep.ViewportChanged += (_, _) => sep.Text = new string('─', Math.Max(1, sep.Viewport.Width));
        return sep;
    }
}
