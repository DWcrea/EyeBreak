using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

/// <summary>
/// 端到端验证「休息时显示桌面」：
/// 用一个受控探针窗口判断桌面是否真的被最小化，并确认提醒窗口在最小化后仍然可见。
/// 前提：settings.json 中 MinimizeWindowsOnBreak = true，且程序以 --test-mode 启动。
/// </summary>
internal static class BreakMinimizeSmoke
{
    private delegate bool EnumWindowsProc(nint hwnd, nint param);
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc callback, nint param);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(nint window, StringBuilder text, int maxCount);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(nint window);
    [DllImport("user32.dll")] private static extern bool IsIconic(nint window);

    private const string BreakWindowTitle = "EyeBreak · 休息提醒";

    private static nint FindBreakWindow()
    {
        nint found = 0;
        EnumWindows((window, _) =>
        {
            if (!IsWindowVisible(window)) return true;
            var title = new StringBuilder(256);
            GetWindowText(window, title, title.Capacity);
            if (title.ToString() != BreakWindowTitle) return true;
            found = window;
            return false;
        }, 0);
        return found;
    }

    public static void Run(string executable)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();

        using var probe = new Form
        {
            Text = "EyeBreak 端到端验证窗口",
            Width = 360,
            Height = 240,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(80, 80)
        };
        probe.Show();
        Pump(400);
        if (IsIconic(probe.Handle)) throw new Exception("探针窗口初始即为最小化，无法验证");
        Console.WriteLine("PASS 探针窗口已创建且处于正常状态（用于判断桌面是否被最小化）");

        using var app = Process.Start(new ProcessStartInfo(executable, "--test-mode") { UseShellExecute = false })
            ?? throw new Exception("无法启动发布版程序");
        try
        {
            var wait = Stopwatch.StartNew();
            nint breakWindow = 0;
            while (wait.Elapsed < TimeSpan.FromSeconds(40))
            {
                Pump(200);
                breakWindow = FindBreakWindow();
                if (breakWindow != 0) break;
                if (app.HasExited) throw new Exception($"EyeBreak 提前退出（退出码 {app.ExitCode}）");
            }
            if (breakWindow == 0) throw new Exception("40 秒内未出现休息提醒窗口");
            Console.WriteLine($"PASS 测试模式下 {wait.Elapsed.TotalSeconds:F1} 秒后出现休息提醒窗口");

            Pump(600);
            if (!IsIconic(probe.Handle)) throw new Exception("休息开始时桌面未被最小化：MinimizeAll 未生效");
            Console.WriteLine("PASS 休息开始时所有窗口被最小化，桌面已显示");

            breakWindow = FindBreakWindow();
            if (breakWindow == 0) throw new Exception("提醒窗口在最小化桌面后消失");
            if (IsIconic(breakWindow)) throw new Exception("提醒窗口被连带最小化（关键缺陷）");
            Console.WriteLine("PASS 提醒窗口在最小化桌面后仍然可见，未被连带最小化");

            wait.Restart();
            while (wait.Elapsed < TimeSpan.FromSeconds(12))
            {
                Pump(200);
                if (FindBreakWindow() == 0) break;
            }
            if (FindBreakWindow() != 0) throw new Exception("休息窗口未自动关闭");
            Console.WriteLine($"PASS 休息窗口约 {wait.Elapsed.TotalSeconds:F1} 秒后自动关闭");

            Pump(900);
            if (IsIconic(probe.Handle)) throw new Exception("休息结束后窗口未还原：UndoMinimizeAll 未生效");
            Console.WriteLine("PASS 休息结束后窗口自动还原到休息前的状态");
        }
        finally
        {
            if (!app.HasExited) { app.Kill(); app.WaitForExit(5000); }
        }
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
