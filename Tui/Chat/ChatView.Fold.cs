using System.Text.RegularExpressions;
using Terminal.Gui.Input;

namespace WTangent.Tui.Tui.Chat;

/// <summary>折叠机制：offset 字符串替换、点击定位（RenderToAnsi 行号）、标记行扫描。</summary>
public sealed partial class ChatView
{
    /// <summary>替换折叠块占位，平移后续块 offset</summary>
    private void ReplaceFoldable(Foldable f, string newText)
    {
        var text = Text;
        var delta = newText.Length - f.Length;
        Text = text[..f.StartOffset] + newText + text[(f.StartOffset + f.Length)..];
        f.Length = newText.Length;
        foreach (var o in _foldables.Where(o => o.StartOffset > f.StartOffset)) o.StartOffset += delta;
    }

    /// <summary>滚轮一次滚 3 行（默认 1 行太慢）；点击标记行 → 展开/折叠</summary>
    protected override bool OnMouseEvent(Mouse mouse)
    {
        if (mouse.IsWheel)
        {
            if (mouse.Flags.FastHasFlags(MouseFlags.WheeledDown)) { ScrollVertical(3); _followBottom = IsAtBottom; return true; }
            if (mouse.Flags.FastHasFlags(MouseFlags.WheeledUp)) { ScrollVertical(-3); _followBottom = IsAtBottom; return true; }
        }
        if (mouse is not { IsSingleClicked: true, Position: { } pos }) return base.OnMouseEvent(mouse);

        var row = pos.Y + Viewport.Y;
        var idx = MarkerRowToIndex(row);
        if (idx < 0) return base.OnMouseEvent(mouse);

        var f = _foldables[idx];
        if (f.Kind == "thinking" && _thinkingMode != ThinkingMode.Hide) return true;
        if (f is { Kind: "tool", CanFold: false }) return true;   // 短结果（≤3 行）不可折叠
        f.Collapsed = !f.Collapsed;
        _pendingRestoreY = Viewport.Y;
        ReplaceFoldable(f, f.Render(ContentWidth));
        SetNeedsDraw();
        ScheduleRebuildMarkers();
        return true;
    }

    /// <summary>防抖重建标记行：Text/滚动/布局变化后等渲染稳定再扫描（立即扫描会因行号漂移错位）</summary>
    private void ScheduleRebuildMarkers()
    {
        if (_markerToken != null) return;
        _markerToken = TuiRepl.App.AddTimeout(TimeSpan.FromMilliseconds(40), () =>
        {
            _markerToken = null;
            RebuildMarkerRows();
            return false;
        });
    }

    /// <summary>重建标记行渲染行号：按 foldable 顺序逐个匹配其标记行（Marker 与当前折叠状态一致），防内容行误匹配。</summary>
    private void RebuildMarkerRows()
    {
        _markerRows.Clear();
        _lastMarkerWidth = Viewport.Width;
        if (_foldables.Count == 0) return;
        try
        {
            var ansi = RenderToAnsi(Text, Math.Max(1, Viewport.Width));
            var lines = ansi.Replace("\r", "").Split('\n');
            var cleaned = lines.Select(l => StripAnsi(l).TrimStart()).ToArray();
            var searchFrom = 0;
            foreach (var marker in _foldables.Select(f => f.MarkerPrefix))
            {
                for (var i = searchFrom; i < cleaned.Length; i++)
                {
                    if (!cleaned[i].StartsWith(marker, StringComparison.Ordinal)) continue;
                    _markerRows.Add(i);
                    searchFrom = i + 1;
                    break;
                }
            }
        }
        catch { }
        // 行号与折叠块必须一一对应，否则索引错位（点击会命中错误块）。不匹配时清空禁用点击，等下次结构变化重扫
        if (_markerRows.Count != _foldables.Count)
            _markerRows.Clear();
    }

    private static string StripAnsi(string s) => MyRegex().Replace(s, "");

    private int MarkerRowToIndex(int row)
    {
        for (var i = 0; i < _markerRows.Count && i < _foldables.Count; i++)
            if (_markerRows[i] == row) return i;
        return -1;
    }

    [GeneratedRegex(@"\x1B\[[0-9;]*[A-Za-z]")]
    private static partial Regex MyRegex();
}
