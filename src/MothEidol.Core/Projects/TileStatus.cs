using MothEidol.Core.Alerts;
using MothEidol.Core.Logs;

namespace MothEidol.Core.Projects;

/// <summary>The traffic-light color of a project's status.</summary>
public enum StatusLight
{
    /// <summary>Not configured or disabled.</summary>
    Neutral,

    /// <summary>Healthy, nothing pending.</summary>
    Green,

    /// <summary>Warnings pending.</summary>
    Yellow,

    /// <summary>Degraded: a source is unavailable or has a read error.</summary>
    Orange,

    /// <summary>Errors pending.</summary>
    Red,
}

/// <summary>What a dashboard tile says about a project: a light, the status text and an optional detail line.</summary>
/// <param name="Detail">Only what the light and the text do not already say; empty for a healthy project.</param>
public sealed record TileStatus(StatusLight Light, string Text, string Detail)
{
    /// <summary>
    /// Worst state wins (red over orange over yellow over green). Pending events only count while the project
    /// is monitored; whatever the headline does not cover (an unavailable source behind pending errors, pending
    /// warnings behind a degraded source) goes in the detail so nothing is hidden.
    /// </summary>
    public static TileStatus From(ProjectStatus status, PendingSummary pending)
    {
        switch (status.Health)
        {
            case ProjectHealth.Disabled:
                return new TileStatus(StatusLight.Neutral, "Disabled", "Monitoring is off");
            case ProjectHealth.NotConfigured:
                return new TileStatus(StatusLight.Neutral, "Not configured", "Add a log source in its settings");
        }

        var degraded = status.Health == ProjectHealth.Degraded;
        var hasPending = pending.Occurrences > 0;
        var errors = hasPending && pending.WorstLevel >= EntryLevel.Error;
        var pendingText = $"{pending.Occurrences:N0} pending";

        if (errors)
            return new TileStatus(StatusLight.Red, pendingText, Join("Errors to review", degraded ? Unavailable(status) : null));

        if (degraded)
            return new TileStatus(StatusLight.Orange, "Degraded", Join(Unavailable(status), hasPending ? $"{pendingText} (warnings)" : null));

        if (hasPending)
            return new TileStatus(StatusLight.Yellow, pendingText, "Warnings to review");

        return new TileStatus(StatusLight.Green, "Healthy", "");
    }

    private static string Join(string first, string? second) => second is null ? first : $"{first} · {second}";

    private static string Unavailable(ProjectStatus status)
    {
        var first = status.AffectedSources[0];
        return status.AffectedSources.Count == 1
            ? $"Unavailable: {first}"
            : $"Unavailable: {first} and {status.AffectedSources.Count - 1} more";
    }
}
