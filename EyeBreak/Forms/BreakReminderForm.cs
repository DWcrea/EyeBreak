using EyeBreak.Models;

namespace EyeBreak.Forms;

public sealed class BreakReminderForm : Form
{
    private readonly Label _countdown;
    private readonly Label _message;
    private readonly Button _postpone;
    public event Action? Postponed;
    public event Action? FinishedEarly;

    public BreakReminderForm()
    {
        Text = "EyeBreak · 休息提醒";
        Font = new Font("Microsoft YaHei UI", 10);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        BackColor = Color.FromArgb(246, 250, 255);
        ClientSize = new Size(400, 187);
        AutoScaleMode = AutoScaleMode.Dpi;
        _message = new Label { Location = new Point(18, 18), Size = new Size(365, 66),
            Text = "该休息眼睛了：请看 6 米以外至少 20 秒，并自然眨眼。" };
        _countdown = new Label { Location = new Point(18, 88), Size = new Size(365, 27),
            ForeColor = Color.FromArgb(19, 89, 160) };
        _postpone = new Button { Text = "推迟 5 分钟", Location = new Point(18, 137), Size = new Size(119, 34) };
        var finish = new Button { Text = "结束休息", Location = new Point(254, 137), Size = new Size(126, 34) };
        _postpone.Click += (_, _) => Postponed?.Invoke();
        finish.Click += (_, _) => FinishedEarly?.Invoke();
        Controls.AddRange([_message, _countdown, _postpone, finish]);
        Shown += (_, _) => PlaceNearCurrentScreen();
    }

    public void RefreshSnapshot(TimerSnapshot snapshot, bool canPostpone)
    {
        _message.Text = snapshot.IsLongBreak
            ? "该长休息了：起身活动、放松眼睛，远眺窗外。"
            : "该休息眼睛了：请看 6 米以外至少 20 秒，并自然眨眼。";
        _countdown.Text = $"剩余休息时间：{Format(snapshot.BreakRemaining)}";
        _postpone.Enabled = canPostpone;
    }

    private static string Format(TimeSpan time) => $"{(int)Math.Ceiling(time.TotalSeconds) / 60:00}:{(int)Math.Ceiling(time.TotalSeconds) % 60:00}";

    private void PlaceNearCurrentScreen()
    {
        // Use the foreground window's display if available; fallback to pointer display.
        var screen = Screen.FromPoint(Cursor.Position);
        Rectangle area = screen.WorkingArea;
        Location = new Point(area.Right - Width - 16, area.Bottom - Height - 16);
    }
}
