namespace WTangent.Tui.Tui;

/// <summary>TUI 主线程调度器（Dispatcher 语义）：静态成员，内部统一走公开的 TuiRepl.App 的 Invoke 机制
/// （仅 TUI 运行期间调用）；Invoke&lt;T&gt; 同步等待结果（给 Confirm 这类需要返回值的场景）。
/// 用法：UiDispatcher.Invoke(() => ...)。
/// 注：不用 BCL DispatcherQueue——纯 TUI 无消息泵，TG 的 app.Invoke 才是主循环调度源。</summary>
public static class UiDispatcher
{
    /// <summary>当前是否已在 UI 主线程</summary>
    public static bool IsMainThread =>
        TuiRepl.App.MainThreadId is { } main && Environment.CurrentManagedThreadId == main;

    /// <summary>在 UI 线程执行并返回结果：已在主线程则直接执行，否则投递并阻塞等待</summary>
    public static T Invoke<T>(Func<T> action)
    {
        if (IsMainThread) return action();
        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        TuiRepl.App.Invoke(() => tcs.TrySetResult(action()));
        return tcs.Task.GetAwaiter().GetResult();
    }

    /// <summary>在 UI 线程执行（无返回值）</summary>
    public static void Invoke(Action action) => Invoke(() => { action(); return true; });
}
