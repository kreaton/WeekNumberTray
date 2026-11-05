using Microsoft.Win32;

namespace WeekNumberTray;

/// <summary>
/// Registers (or unregisters) the app to start when the current user signs in, using the
/// standard per-user Run registry key. This needs no admin rights.
/// </summary>
internal static class AutoStartManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "WeekNumberTray";

    public static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            return key?.GetValue(ValueName) != null;
        }
        catch
        {
            return false;
        }
    }

    public static void SetEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
            if (key == null)
                return;

            if (enabled)
                key.SetValue(ValueName, $"\"{Environment.ProcessPath}\"");
            else
                key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
        catch
        {
            // Best-effort: if the registry write fails (e.g. restricted policy), the app still
            // runs fine. It just won't start by itself, and the menu shows that.
        }
    }
}
