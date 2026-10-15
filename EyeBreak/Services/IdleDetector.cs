using System.Runtime.InteropServices;

namespace EyeBreak.Services;

public static class IdleDetector
{
    [StructLayout(LayoutKind.Sequential)]
    private struct LastInputInfo
    {
        public uint Size;
        public uint Time;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetLastInputInfo(ref LastInputInfo info);

    public static bool IsIdle(int thresholdSeconds)
    {
        var info = new LastInputInfo { Size = (uint)Marshal.SizeOf<LastInputInfo>() };
        if (!GetLastInputInfo(ref info)) return true;
        // Both operands share the Windows millisecond tick base; unchecked uint handles wraparound.
        uint now = unchecked((uint)Environment.TickCount64);
        uint idleMilliseconds = unchecked(now - info.Time);
        return idleMilliseconds >= (uint)thresholdSeconds * 1000;
    }
}
