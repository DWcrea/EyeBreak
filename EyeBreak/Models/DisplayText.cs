namespace EyeBreak.Models;

/// <summary>
/// 界面文本拼装。与 UI 解耦，便于单元测试，也保证托盘提示长度满足 Win32 限制。
/// </summary>
public static class DisplayText
{
    /// <summary>NotifyIcon 提示文本的安全上限，留出余量以兼容旧版 Win32 提示长度限制。</summary>
    public const int TooltipMaxLength = 62;

    public static string StateLabel(TimerState state) => state switch
    {
        TimerState.Working => "学习中",
        TimerState.IdlePaused => "空闲暂停",
        TimerState.UserPaused => "已暂停",
        TimerState.Deferred => "提醒已推迟",
        TimerState.LongBreak => "长休息中",
        _ => "休息中"
    };

    public static bool IsBreak(TimerState state) =>
        state is TimerState.ShortBreak or TimerState.LongBreak;

    /// <summary>把剩余时间格式化为 MM:SS，向上取整到秒。</summary>
    public static string Clock(TimeSpan remaining)
    {
        int seconds = (int)Math.Ceiling(Math.Max(0, remaining.TotalSeconds));
        return $"{seconds / 60:00}:{seconds % 60:00}";
    }

    /// <summary>剩余时间的语义说明：休息剩余 / 推迟剩余 / 距提醒。</summary>
    public static string RemainingHint(TimerState state, TimeSpan remaining) =>
        IsBreak(state) ? $"休息剩余 {Clock(remaining)}"
        : state == TimerState.Deferred ? $"推迟剩余 {Clock(remaining)}"
        : $"距提醒 {Clock(remaining)}";

    /// <summary>托盘图标悬浮提示：状态 + 剩余时间。</summary>
    public static string TrayTooltip(TimerState state, TimeSpan remaining)
    {
        string text = $"EyeBreak · {StateLabel(state)} · {RemainingHint(state, remaining)}";
        return text.Length <= TooltipMaxLength ? text : text[..TooltipMaxLength];
    }
}
