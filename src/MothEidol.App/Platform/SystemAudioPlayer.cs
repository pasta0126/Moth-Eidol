using System.Diagnostics;
using System.Runtime.InteropServices;
using MothEidol.Core.Notifications;

namespace MothEidol.App.Platform;

/// <summary>
/// Plays the platform's own alert sound, so no audio package and no bundled file are needed:
/// <c>afplay</c> on macOS, <c>MessageBeep</c> on Windows and the freedesktop sound theme on Linux.
/// A failure surfaces as an exception, which the dispatcher records against the sound channel.
/// </summary>
public sealed class SystemAudioPlayer : IAudioPlayer
{
    private const uint MessageBeepExclamation = 0x30;

    public Task PlayAlertAsync(CancellationToken cancellationToken)
    {
        if (OperatingSystem.IsMacOS())
        {
            Start("afplay", "/System/Library/Sounds/Glass.aiff");
        }
        else if (OperatingSystem.IsWindows())
        {
            if (!MessageBeep(MessageBeepExclamation)) throw new InvalidOperationException("The system sound could not be played.");
        }
        else
        {
            Start("canberra-gtk-play", "-i dialog-warning");
        }

        return Task.CompletedTask;
    }

    /// <summary>Starts the player without waiting for it, so a long sound never delays the other channels.</summary>
    private static void Start(string program, string arguments)
    {
        var process = Process.Start(new ProcessStartInfo(program, arguments)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
        }) ?? throw new InvalidOperationException($"'{program}' could not be started.");

        process.EnableRaisingEvents = true;
        process.Exited += (_, _) => process.Dispose();
    }

    [DllImport("user32.dll")]
    private static extern bool MessageBeep(uint type);
}
