using System.Threading;

namespace EyeBreak;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        using var singleInstance = new Mutex(true, @"Local\EyeBreak.Standalone.Tray.V1", out bool created);
        if (!created) return;
        ApplicationConfiguration.Initialize();
        Application.Run(new TrayApplicationContext(args.Contains("--test-mode", StringComparer.OrdinalIgnoreCase)));
    }
}
