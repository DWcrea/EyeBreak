namespace EyeBreak.Models;

public enum TimerState
{
    Working,
    IdlePaused,
    UserPaused,
    Deferred,
    ShortBreak,
    LongBreak
}

public readonly record struct TimerSnapshot(TimerState State, TimeSpan WorkRemaining,
    TimeSpan BreakRemaining, TimeSpan DeferRemaining, int CyclesSinceLongBreak,
    bool IsLongBreak, bool IsPaused);
