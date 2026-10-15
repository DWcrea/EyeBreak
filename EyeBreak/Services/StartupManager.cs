using Microsoft.Win32;

namespace EyeBreak.Services;

public static class StartupManager
{
    private const string KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "EyeBreak";

    public static bool IsEnabled()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(KeyPath);
        return string.Equals(key?.GetValue(ValueName) as string,
            QuoteExecutable(), StringComparison.OrdinalIgnoreCase);
    }

    public static void SetEnabled(bool enabled)
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(KeyPath, writable: true)
            ?? throw new InvalidOperationException("无法打开当前用户的启动项。 ");
        if (enabled)
        {
            key.SetValue(ValueName, QuoteExecutable(), RegistryValueKind.String);
        }
        else
        {
            // Never remove an unrelated entry that happens to use the same value name.
            string? existing = key.GetValue(ValueName) as string;
            if (existing != null && Path.GetFileName(existing.Trim('"'))
                .Equals("EyeBreak.exe", StringComparison.OrdinalIgnoreCase))
                key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }

    private static string QuoteExecutable() => $"\"{Environment.ProcessPath}\"";
}
