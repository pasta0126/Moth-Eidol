using System.Runtime.Versioning;
using Microsoft.Win32;

namespace MothEidol.Core.Platform;

/// <summary>Start with session through the per-user <c>Run</c> registry key.</summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsAutostart(string valueName, string executablePath) : IAutostart
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public bool IsSupported => true;

    public string? UnsupportedReason => null;

    public bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(valueName) is string;
        }
    }

    public void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey);
        if (enabled)
        {
            key.SetValue(valueName, $"\"{executablePath}\"");
        }
        else
        {
            key.DeleteValue(valueName, throwOnMissingValue: false);
        }
    }
}
