using System.Diagnostics;
using MothEidol.Core.Settings;

namespace MothEidol.Core.Updates;

/// <summary>
/// Runs the single version check of an application start and holds the resulting notice for the dashboard banner.
/// "Not now" hides the notice for this run only; dismissing remembers the exact version between runs. Failures are
/// never shown to the user: they are traced and the next start tries again.
/// </summary>
public sealed class UpdateCheckService(IUpdateSource source, AppSettings settings, string runningVersion)
{
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    private readonly object _gate = new();
    private int _started;
    private Version? _notice;

    /// <summary>Raised once when the startup check finds a version to announce.</summary>
    public event Action<Version>? NoticeAvailable;

    /// <summary>Raised whenever <see cref="Notice"/> changes (found, postponed, dismissed or the setting turned off).</summary>
    public event Action? NoticeChanged;

    /// <summary>The version the banner announces, or null when nothing is to be shown.</summary>
    public Version? Notice
    {
        get { lock (_gate) return _notice; }
    }

    /// <summary>The reason of the last failed check, if any (diagnostics only).</summary>
    public string? LastFailure { get; private set; }

    /// <summary>Runs the check of this start; later calls do nothing.</summary>
    public async Task CheckAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        if (Interlocked.Exchange(ref _started, 1) == 1) return;
        if (!settings.CheckForUpdates) return;

        UpdateCheckResult result;
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout ?? DefaultTimeout);
        try
        {
            var query = source.GetLatestAsync(cts.Token);
            var finished = await Task.WhenAny(query, Task.Delay(Timeout.Infinite, cts.Token)).ConfigureAwait(false);
            cts.Cancel(); // Releases the pending delay (or tells a source that ignored the timeout to stop).
            result = finished == query
                ? await query.ConfigureAwait(false)
                : new UpdateCheckResult.Failed("The version check timed out.");
        }
        catch (OperationCanceledException)
        {
            result = new UpdateCheckResult.Failed("The version check timed out.");
        }
        catch (Exception ex)
        {
            result = new UpdateCheckResult.Failed(ex.Message);
        }

        if (result is UpdateCheckResult.Failed failed)
        {
            LastFailure = failed.Reason;
            Trace.WriteLine($"Moth-Eidol: version check failed: {failed.Reason}");
            return;
        }

        var version = UpdateNoticePolicy.Decide(settings.CheckForUpdates, runningVersion, settings.DismissedUpdateVersion, result);
        if (version is null) return;

        lock (_gate) _notice = version;
        NoticeChanged?.Invoke();
        NoticeAvailable?.Invoke(version);
    }

    /// <summary>"Not now": hides the notice until the next start.</summary>
    public void Postpone() => Clear();

    /// <summary>"Don't remind me about this version": remembers the version and hides the notice.</summary>
    public void Dismiss()
    {
        Version? version;
        lock (_gate) version = _notice;
        if (version is null) return;
        settings.DismissedUpdateVersion = UpdateNoticePolicy.Format(version);
        Clear();
    }

    /// <summary>Turns the "Check for new versions" setting on or off; off also hides the current notice.</summary>
    public void SetEnabled(bool enabled)
    {
        settings.CheckForUpdates = enabled;
        if (!enabled) Clear();
    }

    private void Clear()
    {
        lock (_gate)
        {
            if (_notice is null) return;
            _notice = null;
        }
        NoticeChanged?.Invoke();
    }
}
