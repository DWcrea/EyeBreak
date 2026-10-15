namespace EyeBreak.Models;

public sealed class AppSettings
{
    public int WorkMinutes { get; set; } = 20;
    public int ShortBreakSeconds { get; set; } = 20;
    public int LongBreakMinutes { get; set; } = 5;
    public int LongBreakEvery { get; set; } = 3;
    public bool LongBreakEnabled { get; set; } = true;
    public int IdleThresholdSeconds { get; set; } = 120;
    public bool StartWithWindows { get; set; }
    public bool SoundEnabled { get; set; }
    public bool StartImmediately { get; set; } = true;

    public AppSettings Validated() => new()
    {
        WorkMinutes = Math.Clamp(WorkMinutes, 1, 120),
        ShortBreakSeconds = Math.Clamp(ShortBreakSeconds, 5, 300),
        LongBreakMinutes = Math.Clamp(LongBreakMinutes, 1, 30),
        LongBreakEvery = Math.Clamp(LongBreakEvery, 1, 12),
        LongBreakEnabled = LongBreakEnabled,
        IdleThresholdSeconds = new[] { 60, 120, 180, 300 }.Contains(IdleThresholdSeconds) ? IdleThresholdSeconds : 120,
        StartWithWindows = StartWithWindows,
        SoundEnabled = SoundEnabled,
        StartImmediately = StartImmediately
    };
}
