namespace MothEidol.Core;

/// <summary>How long the startup splash stays on screen.</summary>
public static class SplashTiming
{
    /// <summary>The splash is visible at least this long, long enough to be seen and so it does not flash on a fast start.</summary>
    public static readonly TimeSpan MinimumVisible = TimeSpan.FromSeconds(2);

    /// <summary>How much longer the splash must stay after the main window is ready (zero when it already had its time).</summary>
    public static TimeSpan RemainingVisible(TimeSpan visibleFor, TimeSpan? minimum = null)
    {
        var remaining = (minimum ?? MinimumVisible) - visibleFor;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }
}
