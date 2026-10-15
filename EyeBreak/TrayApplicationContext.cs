using System.Diagnostics;
using System.Media;
using EyeBreak.Forms;
using EyeBreak.Models;
using EyeBreak.Services;
using Microsoft.Win32;

namespace EyeBreak;

public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly SettingsStore _store = new();
    private readonly EyeBreakTimerService _engine;
    private readonly NotifyIcon _tray;
    private readonly ContextMenuStrip _menu = new();
    private readonly ToolStripMenuItem _stateItem = new() { Enabled = false };
    private readonly ToolStripMenuItem _remainingItem = new() { Enabled = false };
    private readonly ToolStripMenuItem _pauseItem = new();
    private readonly ToolStripMenuItem _deferItem = new("推迟提醒 5 分钟");
    private readonly ToolStripMenuItem _startupItem = new("开机启动");
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 250 };
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private TimeSpan _lastTick;
    private AppSettings _settings;
    private SettingsForm? _settingsForm;
    private BreakReminderForm? _breakForm;
    private readonly bool _testMode;
    private bool _sessionLocked;
    private bool _systemSuspended;
    private bool _desktopMinimized;
    private string _lastDisplayKey = string.Empty;

    public TrayApplicationContext(bool testMode)
    {
        _testMode = testMode;
        _settings = _store.Load();
        _engine = new EyeBreakTimerService(_settings, testMode);
        _menu.Items.AddRange([_stateItem, _remainingItem, new ToolStripSeparator(),
            _pauseItem, new ToolStripMenuItem("立即休息", null, (_, _) => TakeBreakNow()),
            _deferItem, new ToolStripSeparator(),
            new ToolStripMenuItem("设置", null, (_, _) => ShowSettings()),
            _startupItem, new ToolStripSeparator(),
            new ToolStripMenuItem("退出 EyeBreak", null, (_, _) => ExitThread())]);
        _pauseItem.Click += (_, _) =>
        {
            if (_engine.IsUserPaused) _engine.Resume(); else _engine.Pause();
            RefreshDisplay();
        };
        _deferItem.Click += (_, _) => Postpone();
        _startupItem.Click += (_, _) => ToggleStartup();
        _menu.Opening += (_, _) => RefreshDisplay(force: true);
        _tray = new NotifyIcon
        {
            Icon = SystemIcons.Information,
            Text = "EyeBreak · 护眼小助手",
            ContextMenuStrip = _menu,
            Visible = true
        };
        _tray.DoubleClick += (_, _) => ShowSettings();
        _timer.Tick += (_, _) => Tick();
        SystemEvents.SessionSwitch += OnSessionSwitch;
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
        _timer.Start();
        RefreshDisplay(force: true);
    }

    private void Tick()
    {
        TimeSpan now = _clock.Elapsed;
        TimeSpan elapsed = now - _lastTick;
        _lastTick = now;
        // A suspend/resume or blocked UI loop must not be counted as continuous work.
        if (elapsed > TimeSpan.FromSeconds(2)) elapsed = TimeSpan.Zero;
        bool wasBreaking = _engine.IsBreaking;
        bool idle = _sessionLocked || _systemSuspended || IdleDetector.IsIdle(_settings.IdleThresholdSeconds);
        _engine.Advance(elapsed, idle);
        if (!wasBreaking && _engine.IsBreaking) ShowBreak();
        else if (wasBreaking && !_engine.IsBreaking) HideBreak();
        if (_breakForm is { IsDisposed: false } breakForm)
        {
            // 显示桌面可能连带最小化提醒窗，这里保证它始终可见。
            if (breakForm.WindowState != FormWindowState.Normal)
                breakForm.WindowState = FormWindowState.Normal;
            breakForm.RefreshSnapshot(_engine.Snapshot, _engine.CanPostpone);
        }
        RefreshDisplay();
    }

    private void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
    {
        if (e.Reason == SessionSwitchReason.SessionLock) _sessionLocked = true;
        if (e.Reason == SessionSwitchReason.SessionUnlock)
        {
            _sessionLocked = false;
            _lastTick = _clock.Elapsed;
        }
    }

    private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Suspend) _systemSuspended = true;
        if (e.Mode == PowerModes.Resume)
        {
            _systemSuspended = false;
            _lastTick = _clock.Elapsed;
        }
    }

    private void ShowBreak()
    {
        if (_breakForm != null) return;
        // 先显示桌面再创建提醒窗，避免提醒窗被一并最小化。
        if (_settings.MinimizeWindowsOnBreak)
            _desktopMinimized = DesktopMinimizer.MinimizeAll();
        var form = new BreakReminderForm();
        _breakForm = form;
        form.Postponed += Postpone;
        form.FinishedEarly += FinishBreakEarly;
        form.FormClosing += (_, e) =>
        {
            if (e.CloseReason == CloseReason.UserClosing && _engine.IsBreaking)
                _engine.FinishBreakEarly();
            _breakForm = null;
        };
        form.RefreshSnapshot(_engine.Snapshot, _engine.CanPostpone);
        form.Show();
        form.BringToFront();
        if (_settings.SoundEnabled) SystemSounds.Asterisk.Play();
    }

    private void HideBreak()
    {
        BreakReminderForm? form = _breakForm;
        _breakForm = null;
        form?.Close();
        RestoreDesktop();
    }

    /// <summary>休息结束、被推迟或程序退出时，还原休息前最小化的窗口。</summary>
    private void RestoreDesktop()
    {
        if (!_desktopMinimized) return;
        _desktopMinimized = false;
        DesktopMinimizer.UndoMinimizeAll();
    }

    private void TakeBreakNow()
    {
        _engine.TakeBreakNow();
        if (_engine.IsBreaking) ShowBreak();
        RefreshDisplay();
    }

    private void FinishBreakEarly()
    {
        _engine.FinishBreakEarly();
        HideBreak();
        RefreshDisplay();
    }

    private void Postpone()
    {
        if (!_engine.CanPostpone) return;
        _engine.Postpone();
        HideBreak();
        RefreshDisplay();
    }

    private void ShowSettings()
    {
        if (_settingsForm is { IsDisposed: false })
        {
            _settingsForm.Activate();
            return;
        }
        var form = new SettingsForm(_settings);
        _settingsForm = form;
        form.FormClosed += (_, _) =>
        {
            _settingsForm = null;
            if (form.DialogResult != DialogResult.OK || form.Result == null) return;
            try
            {
                if (form.Result.StartWithWindows != StartupManager.IsEnabled())
                    StartupManager.SetEnabled(form.Result.StartWithWindows);
                _store.Save(form.Result);
                _settings = form.Result;
                _engine.ApplySettings(_settings);
                RefreshDisplay(force: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or System.Security.SecurityException)
            {
                MessageBox.Show($"保存设置失败：{ex.Message}", "EyeBreak", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };
        form.Show();
    }

    private void ToggleStartup()
    {
        try
        {
            bool enabled = !StartupManager.IsEnabled();
            StartupManager.SetEnabled(enabled);
            _settings.StartWithWindows = enabled;
            _store.Save(_settings);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or System.Security.SecurityException)
        {
            MessageBox.Show($"更改开机启动失败：{ex.Message}", "EyeBreak", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        RefreshDisplay(force: true);
    }

    private void RefreshDisplay(bool force = false)
    {
        TimerSnapshot s = _engine.Snapshot;
        string state = DisplayText.StateLabel(s.State);
        TimeSpan remaining = s.State switch
        {
            TimerState.ShortBreak or TimerState.LongBreak => s.BreakRemaining,
            TimerState.Deferred => s.DeferRemaining,
            _ => s.WorkRemaining
        };
        string clock = DisplayText.Clock(remaining);
        string tooltip = DisplayText.TrayTooltip(s.State, remaining);

        // 每秒最多刷新一次界面，避免无意义地反复改写托盘与菜单文本。
        string key = $"{state}|{tooltip}|{s.IsPaused}|{_engine.CanPostpone}";
        if (!force && key == _lastDisplayKey) return;
        _lastDisplayKey = key;

        _stateItem.Text = $"当前状态：{state}{(_testMode ? "（测试模式）" : "")}";
        _remainingItem.Text = $"{(s.State == TimerState.Deferred ? "推迟提醒剩余" : "距离下一次提醒 / 休息剩余")}：{clock}";
        _pauseItem.Text = s.IsPaused ? "继续计时" : "暂停计时";
        _pauseItem.Enabled = !_engine.IsBreaking;
        _deferItem.Enabled = _engine.CanPostpone;
        _tray.Text = tooltip;
        if (force) RefreshStartupCheck();
    }

    private void RefreshStartupCheck()
    {
        try { _startupItem.Checked = StartupManager.IsEnabled(); }
        catch (System.Security.SecurityException) { _startupItem.Checked = false; }
    }

    protected override void ExitThreadCore()
    {
        _timer.Stop();
        SystemEvents.SessionSwitch -= OnSessionSwitch;
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        _timer.Dispose();
        HideBreak();
        _settingsForm?.Close();
        _tray.Visible = false;
        _tray.Dispose();
        _menu.Dispose();
        base.ExitThreadCore();
    }
}
