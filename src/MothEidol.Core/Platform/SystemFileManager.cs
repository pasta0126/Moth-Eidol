using System.Diagnostics;

namespace MothEidol.Core.Platform;

/// <summary>File manager of the current OS. The path is passed as an argument, never through a shell.</summary>
public sealed class SystemFileManager : IFileManager
{
    public bool TryReveal(string path, bool isFile, out string? error)
    {
        try
        {
            var info = Build(path, isFile);
            if (Process.Start(info) is null)
            {
                error = "The file manager could not be started.";
                return false;
            }

            error = null;
            return true;
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException or PlatformNotSupportedException)
        {
            error = "No file manager is available.";
            return false;
        }
    }

    private static ProcessStartInfo Build(string path, bool isFile)
    {
        ProcessStartInfo info;
        if (OperatingSystem.IsMacOS())
        {
            info = new ProcessStartInfo("open");
            if (isFile) info.ArgumentList.Add("-R");
            info.ArgumentList.Add(path);
        }
        else if (OperatingSystem.IsWindows())
        {
            // explorer parses its own command line: /select,"path" must be a single token.
            info = new ProcessStartInfo("explorer.exe") { Arguments = isFile ? $"/select,\"{path}\"" : $"\"{path}\"" };
        }
        else
        {
            info = new ProcessStartInfo("xdg-open");
            info.ArgumentList.Add(isFile ? (Path.GetDirectoryName(path) ?? path) : path);
        }

        info.UseShellExecute = false;
        info.CreateNoWindow = true;
        return info;
    }
}
