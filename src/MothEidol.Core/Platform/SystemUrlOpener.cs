using System.Diagnostics;

namespace MothEidol.Core.Platform;

/// <summary>Default browser of the current OS. Only the fixed addresses of <see cref="SupportLinks"/> are opened.</summary>
public sealed class SystemUrlOpener : IUrlOpener
{
    public bool TryOpen(string url, out string? error)
    {
        if (!SupportLinks.IsAllowed(url))
        {
            error = "That address is not allowed.";
            return false;
        }

        try
        {
            if (Process.Start(Build(url)) is null)
            {
                error = "The browser could not be started.";
                return false;
            }

            error = null;
            return true;
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException or PlatformNotSupportedException)
        {
            error = "No browser is available.";
            return false;
        }
    }

    private static ProcessStartInfo Build(string url)
    {
        // The address is one of our own constants, so handing it to the shell association on Windows is safe.
        if (OperatingSystem.IsWindows())
            return new ProcessStartInfo(url) { UseShellExecute = true };

        var info = new ProcessStartInfo(OperatingSystem.IsMacOS() ? "open" : "xdg-open");
        info.ArgumentList.Add(url);
        info.UseShellExecute = false;
        info.CreateNoWindow = true;
        return info;
    }
}
