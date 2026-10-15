using EyeBreak.Models;
using EyeBreak.Services;
using System.Threading;

// WinForms 相关验证必须在 STA 线程上运行。
static void RunSta(Action action)
{
    Exception? failure = null;
    var thread = new Thread(() =>
    {
        try { action(); }
        catch (Exception ex) { failure = ex; }
    });
    thread.SetApartmentState(ApartmentState.STA);
    thread.Start();
    thread.Join();
    if (failure != null) throw failure;
}

if (args.Length == 2 && args[0] == "--ui-smoke")
{
    UiSmoke.Run(args[1]);
    return;
}

if (args.Length == 1 && args[0] == "--verify-shell")
{
    RunSta(ShellSmoke.Run);
    return;
}

if (args.Length == 2 && args[0] == "--verify-break")
{
    RunSta(() => BreakMinimizeSmoke.Run(args[1]));
    return;
}

int passed = 0;
void Check(string name, Action action)
{
    action();
    Console.WriteLine($"PASS {name}");
    passed++;
}
void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new Exception($"Expected {expected}, got {actual}");
}
void Advance(EyeBreakTimerService t, int seconds, bool idle = false) =>
    t.Advance(TimeSpan.FromSeconds(seconds), idle);

Check("默认设置与范围校验", () =>
{
    var defaults = new AppSettings();
    Equal(20, defaults.WorkMinutes);
    Equal(20, defaults.ShortBreakSeconds);
    Equal(3, defaults.LongBreakEvery);
    Equal(false, defaults.MinimizeWindowsOnBreak);
    var valid = new AppSettings { WorkMinutes = -10, IdleThresholdSeconds = 77,
        ShortBreakSeconds = 1000, LongBreakEvery = 99 }.Validated();
    Equal(1, valid.WorkMinutes);
    Equal(120, valid.IdleThresholdSeconds);
    Equal(300, valid.ShortBreakSeconds);
    Equal(12, valid.LongBreakEvery);
});
Check("空闲暂停与恢复保留进度", () =>
{
    var t = new EyeBreakTimerService(new AppSettings { WorkMinutes = 1 });
    Advance(t, 20);
    Advance(t, 500, true);
    Equal(TimerState.IdlePaused, t.State);
    Equal(TimeSpan.FromSeconds(40), t.Snapshot.WorkRemaining);
    Advance(t, 39);
    Equal(TimerState.Working, t.State);
    Advance(t, 1);
    Equal(TimerState.ShortBreak, t.State);
});
Check("手动暂停和恢复", () =>
{
    var t = new EyeBreakTimerService(new AppSettings { WorkMinutes = 1 });
    Advance(t, 10);
    t.Pause();
    Advance(t, 180);
    Equal(TimerState.UserPaused, t.State);
    Equal(TimeSpan.FromSeconds(50), t.Snapshot.WorkRemaining);
    t.Resume(); Advance(t, 50);
    Equal(TimerState.ShortBreak, t.State);
});
Check("短休息到时自动进入新周期", () =>
{
    var t = new EyeBreakTimerService(new AppSettings { WorkMinutes = 1, ShortBreakSeconds = 5 });
    Advance(t, 60);
    Equal(TimerState.ShortBreak, t.State);
    Advance(t, 5);
    Equal(TimerState.Working, t.State);
    Equal(TimeSpan.FromMinutes(1), t.Snapshot.WorkRemaining);
    Equal(1, t.Snapshot.CyclesSinceLongBreak);
});
Check("每三轮仅触发一次长休息", () =>
{
    var t = new EyeBreakTimerService(new AppSettings { WorkMinutes = 1,
        ShortBreakSeconds = 5, LongBreakMinutes = 5 });
    for (int round = 1; round <= 3; round++)
    {
        Advance(t, 60);
        Equal(round == 3 ? TimerState.LongBreak : TimerState.ShortBreak, t.State);
        Advance(t, round == 3 ? 300 : 5);
        Equal(TimerState.Working, t.State);
    }
    Equal(0, t.Snapshot.CyclesSinceLongBreak);
    Advance(t, 60);
    Equal(TimerState.ShortBreak, t.State);
});
Check("关闭长休息不会触发", () =>
{
    var t = new EyeBreakTimerService(new AppSettings { WorkMinutes = 1,
        LongBreakEnabled = false, ShortBreakSeconds = 5 });
    for (int i = 0; i < 4; i++)
    {
        Advance(t, 60); Equal(TimerState.ShortBreak, t.State); Advance(t, 5);
    }
});
Check("推迟提醒无重复且空闲到期不会弹", () =>
{
    var t = new EyeBreakTimerService(new AppSettings { WorkMinutes = 1 });
    Advance(t, 60); t.Postpone();
    Equal(TimerState.Deferred, t.State);
    Advance(t, 299); Equal(TimerState.Deferred, t.State);
    Advance(t, 10, true);
    Equal(TimerState.Deferred, t.State);
    Equal(TimeSpan.Zero, t.Snapshot.DeferRemaining);
    Advance(t, 1);
    Equal(TimerState.ShortBreak, t.State);
    Advance(t, 1); Equal(TimerState.ShortBreak, t.State);
});
Check("手动休息不增加周期且暂停状态保持", () =>
{
    var t = new EyeBreakTimerService(new AppSettings { WorkMinutes = 1, StartImmediately = false });
    Equal(TimerState.UserPaused, t.State);
    t.TakeBreakNow();
    Equal(TimerState.ShortBreak, t.State);
    Equal(false, t.CanPostpone);
    t.FinishBreakEarly();
    Equal(TimerState.UserPaused, t.State);
    Equal(0, t.Snapshot.CyclesSinceLongBreak);
});
Check("改工作间隔下轮生效", () =>
{
    var t = new EyeBreakTimerService(new AppSettings { WorkMinutes = 1, ShortBreakSeconds = 5 });
    Advance(t, 20);
    t.ApplySettings(new AppSettings { WorkMinutes = 2, ShortBreakSeconds = 5 });
    Equal(TimeSpan.FromSeconds(40), t.Snapshot.WorkRemaining);
    Advance(t, 40); Advance(t, 5);
    Equal(TimeSpan.FromMinutes(2), t.Snapshot.WorkRemaining);
});
Check("损坏 JSON 恢复默认并可持久化", () =>
{
    string file = Path.Combine(Path.GetTempPath(), "EyeBreak-test-" + Guid.NewGuid().ToString("N"), "settings.json");
    var store = new SettingsStore(file);
    try
    {
        Equal(20, store.Load().WorkMinutes);
        store.Save(new AppSettings { WorkMinutes = 42, MinimizeWindowsOnBreak = true });
        Equal(42, store.Load().WorkMinutes);
        Equal(true, store.Load().MinimizeWindowsOnBreak);
        File.WriteAllText(file, "not json");
        Equal(20, store.Load().WorkMinutes);
        Equal(false, store.Load().MinimizeWindowsOnBreak);
    }
    finally { Directory.Delete(Path.GetDirectoryName(file)!, true); }
});
Check("20 秒测试工作 + 5 秒测试休息", () =>
{
    var t = new EyeBreakTimerService(new AppSettings(), testMode: true);
    Advance(t, 20); Equal(TimerState.ShortBreak, t.State);
    Advance(t, 5); Equal(TimerState.Working, t.State);
    Equal(TimeSpan.FromSeconds(20), t.Snapshot.WorkRemaining);
});
Check("托盘提示显示剩余时间", () =>
{
    Equal("EyeBreak · 学习中 · 距提醒 19:32",
        DisplayText.TrayTooltip(TimerState.Working, TimeSpan.FromSeconds(19 * 60 + 32)));
    Equal("EyeBreak · 休息中 · 休息剩余 00:15",
        DisplayText.TrayTooltip(TimerState.ShortBreak, TimeSpan.FromSeconds(15)));
    Equal("EyeBreak · 长休息中 · 休息剩余 04:32",
        DisplayText.TrayTooltip(TimerState.LongBreak, TimeSpan.FromSeconds(4 * 60 + 32)));
    Equal("EyeBreak · 提醒已推迟 · 推迟剩余 04:59",
        DisplayText.TrayTooltip(TimerState.Deferred, TimeSpan.FromSeconds(4 * 60 + 59)));
    Equal("EyeBreak · 已暂停 · 距提醒 12:00",
        DisplayText.TrayTooltip(TimerState.UserPaused, TimeSpan.FromMinutes(12)));
    Equal("EyeBreak · 空闲暂停 · 距提醒 12:00",
        DisplayText.TrayTooltip(TimerState.IdlePaused, TimeSpan.FromMinutes(12)));
    Equal("00:00", DisplayText.Clock(TimeSpan.FromSeconds(-5)));
});
Check("托盘提示长度不超过系统上限", () =>
{
    TimeSpan[] samples = [TimeSpan.Zero, TimeSpan.FromSeconds(1),
        TimeSpan.FromMinutes(120), TimeSpan.FromMinutes(59)];
    foreach (TimerState state in Enum.GetValues<TimerState>())
        foreach (TimeSpan sample in samples)
        {
            string text = DisplayText.TrayTooltip(state, sample);
            if (text.Length > DisplayText.TooltipMaxLength)
                throw new Exception($"{state} 提示过长（{text.Length}）：{text}");
        }
});
Console.WriteLine($"ALL PASS ({passed} tests)");
