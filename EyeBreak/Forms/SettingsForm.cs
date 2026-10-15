using EyeBreak.Models;
using EyeBreak.Services;

namespace EyeBreak.Forms;

public sealed class SettingsForm : Form
{
    private readonly NumericUpDown _work = Number(1, 120);
    private readonly NumericUpDown _short = Number(5, 300);
    private readonly NumericUpDown _long = Number(1, 30);
    private readonly NumericUpDown _frequency = Number(1, 12);
    private readonly ComboBox _idle = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };
    private readonly CheckBox _longEnabled = new() { Text = "启用长休息", AutoSize = true };
    private readonly CheckBox _startup = new() { Text = "开机启动", AutoSize = true };
    private readonly CheckBox _sound = new() { Text = "轻提示音", AutoSize = true };
    private readonly CheckBox _immediate = new() { Text = "启动后立即计时", AutoSize = true };
    private readonly Label _status = new() { AutoSize = true };
    public AppSettings? Result { get; private set; }

    public SettingsForm(AppSettings settings)
    {
        Text = "EyeBreak · 设置";
        Font = new Font("Microsoft YaHei UI", 9);
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(450, 490);
        _idle.Items.AddRange(["60 秒", "120 秒", "180 秒", "300 秒"]);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 12,
            Padding = new Padding(20, 15, 20, 12) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        for (int i = 0; i < 9; i++) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 47));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Row(layout, 0, "工作间隔（分钟）", _work);
        Row(layout, 1, "短休息（秒）", _short);
        Row(layout, 2, "长休息（分钟）", _long);
        Row(layout, 3, "每几轮安排长休息", _frequency);
        Row(layout, 4, "键鼠空闲阈值", _idle);
        layout.Controls.Add(_longEnabled, 0, 5);
        layout.Controls.Add(_startup, 1, 5);
        layout.Controls.Add(_sound, 0, 6);
        layout.Controls.Add(_immediate, 1, 6);
        var note = new Label { Text = "工作间隔修改后，从下一轮开始生效；当前计时进度保留。\n每次启动重新计时，不保存上次剩余时间。",
            AutoSize = true, ForeColor = Color.DimGray };
        layout.Controls.Add(note, 0, 8);
        layout.SetColumnSpan(note, 2);
        var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill };
        var save = new Button { Text = "保存", Width = 90, Height = 32 };
        var cancel = new Button { Text = "取消", Width = 90, Height = 32 };
        var reset = new Button { Text = "恢复默认", Width = 90, Height = 32 };
        save.Click += (_, _) =>
        {
            Result = new AppSettings
            {
                WorkMinutes = (int)_work.Value, ShortBreakSeconds = (int)_short.Value,
                LongBreakMinutes = (int)_long.Value, LongBreakEvery = (int)_frequency.Value,
                IdleThresholdSeconds = int.Parse(_idle.SelectedItem!.ToString()!.Split(' ')[0]),
                LongBreakEnabled = _longEnabled.Checked, StartWithWindows = _startup.Checked,
                SoundEnabled = _sound.Checked, StartImmediately = _immediate.Checked
            };
            DialogResult = DialogResult.OK;
            Close();
        };
        cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        reset.Click += (_, _) => Fill(new AppSettings());
        buttons.Controls.AddRange([save, cancel, reset]);
        layout.Controls.Add(buttons, 0, 10);
        layout.SetColumnSpan(buttons, 2);
        _status.Text = "保存后立即应用新设置（工作间隔下一轮生效）。";
        layout.Controls.Add(_status, 0, 11);
        layout.SetColumnSpan(_status, 2);
        Controls.Add(layout);
        Fill(settings);
    }

    private void Fill(AppSettings settings)
    {
        _work.Value = settings.WorkMinutes;
        _short.Value = settings.ShortBreakSeconds;
        _long.Value = settings.LongBreakMinutes;
        _frequency.Value = settings.LongBreakEvery;
        _idle.SelectedIndex = Array.IndexOf(new[] { 60, 120, 180, 300 }, settings.IdleThresholdSeconds);
        if (_idle.SelectedIndex < 0) _idle.SelectedIndex = 1;
        _longEnabled.Checked = settings.LongBreakEnabled;
        _startup.Checked = settings.StartWithWindows;
        _sound.Checked = settings.SoundEnabled;
        _immediate.Checked = settings.StartImmediately;
    }

    private static NumericUpDown Number(int min, int max) => new() { Minimum = min, Maximum = max, Width = 180 };
    private static void Row(TableLayoutPanel table, int row, string label, Control input)
    {
        table.Controls.Add(new Label { Text = label, Anchor = AnchorStyles.Left, AutoSize = true }, 0, row);
        table.Controls.Add(input, 1, row);
    }
}
