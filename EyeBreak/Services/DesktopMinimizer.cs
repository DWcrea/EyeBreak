using System.Reflection;
using System.Runtime.InteropServices;

namespace EyeBreak.Services;

/// <summary>
/// 通过 Shell 对象模型实现「显示桌面」：最小化所有顶层窗口而不关闭任何程序，
/// 效果等同于 Win+D。休息结束后可整体还原。
/// 所有方法均为尽力而为，失败时返回 false，绝不影响休息流程本身。
/// </summary>
public static class DesktopMinimizer
{
    public static bool MinimizeAll() => InvokeShell("MinimizeAll");

    public static bool UndoMinimizeAll() => InvokeShell("UndoMinimizeAll");

    private static bool InvokeShell(string method)
    {
        object? shell = null;
        try
        {
            Type? type = Type.GetTypeFromProgID("Shell.Application");
            if (type is null) return false;
            shell = Activator.CreateInstance(type);
            if (shell is null) return false;
            // Shell 对象是 IDispatch，使用后期绑定调用文档化的 MinimizeAll / UndoMinimizeAll。
            type.InvokeMember(method, BindingFlags.InvokeMethod, null, shell, null);
            return true;
        }
        catch (Exception ex) when (ex is COMException or InvalidOperationException
            or NotSupportedException or TargetInvocationException or MissingMethodException
            or System.Security.SecurityException)
        {
            return false;
        }
        finally
        {
            if (shell is not null && Marshal.IsComObject(shell)) Marshal.ReleaseComObject(shell);
        }
    }
}
