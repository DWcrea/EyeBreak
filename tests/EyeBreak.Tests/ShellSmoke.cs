using System.Drawing;
using System.Runtime.InteropServices;
using EyeBreak.Services;

/// <summary>
/// 实测「显示桌面」：先用测试进程自己创建的可见窗口做受控验证，
/// 再统计整个桌面的最小化窗口数作为旁证。
/// 注意：调用 MinimizeAll 会短暂最小化当前桌面的所有窗口，随后立即还原。
/// </summary>
internal static class ShellSmoke
{
    private delegate bool EnumWindowsProc(nint hwnd, nint param);
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc callback, nint param);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(nint hwnd);
    [DllImport("user32.dll")] private static extern bool IsIconic(nint hwnd);

    private static int MinimizedCount()
    {
        int count = 0;
        EnumWindows((window, _) =>
        {
            if (IsWindowVisible(window) && IsIconic(window)) count++;
            return true;
        }, 0);
        return count;
    }

    public static void Run()
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();

        using var probe = new Form
        {
            Text = "EyeBreak 最小化验证窗口",
            Width = 340,
            Height = 220,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(60, 60),
            ShowInTaskbar = true
        };
        probe.Show();
        Pump(300);
        if (IsIconic(probe.Handle)) throw new Exception("测试窗口初始即为最小化，无法验证");
        Console.WriteLine("PASS 测试窗口已创建且处于正常状态");

        int before = MinimizedCount();
        Console.WriteLine($"桌面已最小化的可见顶层窗口：{before} 个");

        if (!DesktopMinimizer.MinimizeAll()) throw new Exception("MinimizeAll 调用失败");
        Pump(800);
        if (!IsIconic(probe.Handle)) throw new Exception("MinimizeAll 未生效：受控测试窗口未被最小化");
        int during = MinimizedCount();
        Console.WriteLine($"PASS MinimizeAll 生效（等同 Win+D），受控窗口已最小化；桌面 {before} → {during} 个");

        if (!DesktopMinimizer.UndoMinimizeAll()) throw new Exception("UndoMinimizeAll 调用失败");
        Pump(800);
        if (IsIconic(probe.Handle))
        {
            DesktopMinimizer.UndoMinimizeAll();
            throw new Exception("UndoMinimizeAll 未生效：受控测试窗口仍处于最小化");
        }
        Console.WriteLine($"PASS UndoMinimizeAll 还原，受控窗口恢复为正常状态；桌面 {MinimizedCount()} 个");
    }

    private static void Pump(int milliseconds)
    {
        var until = Environment.TickCount64 + milliseconds;
        while (Environment.TickCount64 < until)
        {
            Application.DoEvents();
            Thread.Sleep(30);
        }
    }
}
