using EyeBreak.Models;

namespace EyeBreak.Services;

// Pure state machine: elapsed time comes from a monotonic clock in the Windows host.
public sealed class EyeBreakTimerService
{
    private AppSettings _settings;
    private TimeSpan _workTarget;
    private TimeSpan _worked;
    private TimeSpan _breakRemaining;
    private TimeSpan _deferRemaining;
    private bool _userPaused;
    private bool _idle;
    private bool _breaking;
    private bool _deferred;
    private bool _longBreak;
    private bool _automaticBreak;
    private bool _resumePausedAfterBreak;
    private int _cycles;
    private readonly bool _testMode;

    public EyeBreakTimerService(AppSettings settings, bool testMode = false)
    {
        _settings = settings.Validated();
        _testMode = testMode;
        _workTarget = WorkDuration();
        _userPaused = !_settings.StartImmediately;
    }

    public TimerSnapshot Snapshot => new(State, MaxZero(_workTarget - _worked),
        MaxZero(_breakRemaining), MaxZero(_deferRemaining), _cycles,
        _longBreak, _userPaused);

    public TimerState State => _breaking ? (_longBreak ? TimerState.LongBreak : TimerState.ShortBreak)
        : _userPaused ? TimerState.UserPaused
        : _deferred ? TimerState.Deferred
        : _idle ? TimerState.IdlePaused : TimerState.Working;

    public bool IsBreaking => _breaking;
    public bool CanPostpone => _breaking && _automaticBreak;
    public bool IsUserPaused => _userPaused;

    public void Advance(TimeSpan elapsed, bool isIdle)
    {
        if (elapsed < TimeSpan.Zero) elapsed = TimeSpan.Zero;
        _idle = isIdle;
        if (_breaking)
        {
            _breakRemaining -= elapsed;
            if (_breakRemaining <= TimeSpan.Zero) FinishBreak();
            return;
        }
        if (_userPaused) return;
        if (_deferred)
        {
            _deferRemaining = MaxZero(_deferRemaining - elapsed);
            if (_deferRemaining == TimeSpan.Zero && !isIdle)
            {
                _deferred = false;
                BeginBreak(automatic: true);
            }
            return;
        }
        if (isIdle) return;
        _worked = Min(_workTarget, _worked + elapsed);
        if (_worked >= _workTarget) BeginBreak(automatic: true);
    }

    public void Pause() { if (!_breaking) _userPaused = true; }
    public void Resume() { if (!_breaking) _userPaused = false; }

    public void TakeBreakNow()
    {
        if (_breaking) return;
        _resumePausedAfterBreak = _userPaused;
        _deferred = false;
        BeginBreak(automatic: false);
    }

    public void Postpone()
    {
        if (!CanPostpone) return;
        _breaking = false;
        _longBreak = false;
        _breakRemaining = TimeSpan.Zero;
        _deferred = true;
        _deferRemaining = TimeSpan.FromMinutes(5);
        _worked = _workTarget;
    }

    public void FinishBreakEarly() { if (_breaking) FinishBreak(); }

    public void ApplySettings(AppSettings settings)
    {
        // The current work target stays unchanged until the next cycle.
        _settings = settings.Validated();
    }

    private void BeginBreak(bool automatic)
    {
        _automaticBreak = automatic;
        _longBreak = automatic && _settings.LongBreakEnabled &&
            _cycles + 1 >= _settings.LongBreakEvery;
        _breakRemaining = _longBreak ? TimeSpan.FromMinutes(_settings.LongBreakMinutes)
            : TimeSpan.FromSeconds(_testMode ? 5 : _settings.ShortBreakSeconds);
        _breaking = true;
    }

    private void FinishBreak()
    {
        if (_automaticBreak) _cycles = _longBreak ? 0 : _cycles + 1;
        _breaking = false;
        _automaticBreak = false;
        _longBreak = false;
        _deferred = false;
        _breakRemaining = TimeSpan.Zero;
        _deferRemaining = TimeSpan.Zero;
        _worked = TimeSpan.Zero;
        _workTarget = WorkDuration();
        _userPaused = _resumePausedAfterBreak;
        _resumePausedAfterBreak = false;
    }

    private TimeSpan WorkDuration() => _testMode ? TimeSpan.FromSeconds(20)
        : TimeSpan.FromMinutes(_settings.WorkMinutes);
    private static TimeSpan Min(TimeSpan a, TimeSpan b) => a < b ? a : b;
    private static TimeSpan MaxZero(TimeSpan t) => t < TimeSpan.Zero ? TimeSpan.Zero : t;
}
