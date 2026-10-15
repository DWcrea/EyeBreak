using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

internal static class UiSmoke
{
    private delegate bool EnumWindowsProc(nint hwnd, nint param);
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc callback, nint param);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(nint window, StringBuilder text, int maxCount);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(nint window);

    private static int ReminderWindows()
    {
        int count = 0;
        EnumWindows((window, _) =>
        {
            if (!IsWindowVisible(window)) return true;
            var title = new StringBuilder(256);
            GetWindowText(window, title, title.Capacity);
            if (title.ToString() == "EyeBreak · 休息提醒") count++;
            return true;
        }, 0);
        return count;
    }

    public static void Run(string executable)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        using var app = Process.Start(new ProcessStartInfo(executable, "--test-mode") { UseShellExecute = false })
            ?? throw new Exception("无法启动发布版程序");
        try
        {
            Thread.Sleep(1500);
            if (app.HasExited) throw new Exception($"发布程序提前退出：{app.ExitCode}");
            Console.WriteLine($"PASS 发布版启动（PID {app.Id}，无控制台窗口的 WinExe）");
            using var duplicate = Process.Start(new ProcessStartInfo(executable, "--test-mode") { UseShellExecute = false })
                ?? throw new Exception("无法启动第二实例");
            if (!duplicate.WaitForExit(5000)) throw new Exception("第二实例未自行退出");
            if (app.HasExited) throw new Exception("原实例异常终止");
            Console.WriteLine($"PASS 单实例：第二次启动退出码 {duplicate.ExitCode}");
            bool appeared = false;
            var wait = Stopwatch.StartNew();
            while (wait.Elapsed < TimeSpan.FromSeconds(27))
            {
                if (ReminderWindows() > 0) { appeared = true; break; }
                Thread.Sleep(250);
            }
            if (!appeared) throw new Exception("27 秒内未检测到休息提醒窗口（当前会话可能不支持交互桌面）");
            Console.WriteLine($"PASS 测试模式约 {wait.Elapsed.TotalSeconds:F1} 秒弹出可见的休息窗口");
            if (ReminderWindows() != 1) throw new Exception("出现重复提醒窗口");
            wait.Restart();
            while (wait.Elapsed < TimeSpan.FromSeconds(8) && ReminderWindows() > 0) Thread.Sleep(250);
            if (ReminderWindows() != 0) throw new Exception("休息窗口未在约 5 秒后自动关闭");
            Console.WriteLine($"PASS 休息窗口约 {wait.Elapsed.TotalSeconds:F1} 秒自动关闭");
            app.Refresh();
            Console.WriteLine($"运行时 WorkingSet = {app.WorkingSet64 / 1024.0 / 1024.0:F1} MiB");
        }
        finally
        {
            if (!app.HasExited) { app.Kill(); app.WaitForExit(5000); }
        }
    }
}
