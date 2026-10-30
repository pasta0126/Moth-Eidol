using Avalonia.Media;
using MothEidol.Core.Alerts;
using MothEidol.Core.Logs;
using MothEidol.Core.Projects;

namespace MothEidol.App.Views;

/// <summary>A dashboard tile: either a project with its status or the "New project" tile.</summary>
public sealed class TileModel
{
    private TileModel() { }

    public long ProjectId { get; private init; }

    public bool IsNew { get; private init; }

    public bool IsProject => !IsNew;

    public string Name { get; private init; } = "";

    public string StatusText { get; private init; } = "";

    public IBrush? StatusBrush { get; private init; }

    public string Detail { get; private init; } = "";

    /// <summary>Alert-level events per hour over the last 24 hours (oldest first), or null when not shown.</summary>
    public IReadOnlyList<int>? Series { get; private init; }

    public bool HasSeries => Series is not null;

    /// <summary>"12 events in 24 h · ▲ up from 4", empty when the project is not being monitored.</summary>
    public string HealthText { get; private init; } = "";

    public static TileModel NewProject() => new() { IsNew = true, Name = "New project" };

    /// <summary>
    /// The tile of a project. What waits for the user outweighs a healthy status: pending events take the
    /// headline, colored by the worst pending level, while the source health stays in the detail.
    /// </summary>
    /// <param name="last48Hours">Alert-level events per hour for the last 48 hours, oldest first.</param>
    public static TileModel For(
        Project project, ProjectStatus status, int sourceCount, PendingSummary pending, IReadOnlyList<int> last48Hours)
    {
        var sources = sourceCount == 1 ? "1 source" : $"{sourceCount} sources";
        var (text, brush, detail) = status.Health switch
        {
            ProjectHealth.Healthy => ("Healthy", "MothInfoBrush", $"Nothing pending · {sources}"),
            ProjectHealth.Degraded => ("Degraded", "MothWarningBrush", Unavailable(status)),
            ProjectHealth.NotConfigured => ("Not configured", "MothTextMutedBrush", "Add a log source in its settings"),
            _ => ("Disabled", "MothTextMutedBrush", "Monitoring is off"),
        };

        if (pending.Occurrences > 0 && status.Health is ProjectHealth.Healthy or ProjectHealth.Degraded)
        {
            var errors = pending.WorstLevel >= EntryLevel.Error;
            text = $"{pending.Occurrences:N0} pending";
            brush = errors ? "MothErrorBrush" : "MothWarningBrush";
            detail = $"{(errors ? "Errors" : "Warnings")} to review · " +
                     (status.Health == ProjectHealth.Degraded ? Unavailable(status) : sources);
        }

        var monitored = status.Health is ProjectHealth.Healthy or ProjectHealth.Degraded;
        return new TileModel
        {
            ProjectId = project.Id,
            Name = project.Name,
            StatusText = text,
            StatusBrush = ThemeBrushes.Get(brush),
            Detail = detail,
            Series = monitored ? [.. last48Hours.Skip(Math.Max(0, last48Hours.Count - 24))] : null,
            HealthText = monitored ? Describe(Trend.From(last48Hours)) : "",
        };
    }

    private static string Describe(Trend trend)
    {
        var events = trend.Last24Hours == 1 ? "1 event" : $"{trend.Last24Hours:N0} events";
        return trend.Direction switch
        {
            TrendDirection.None => "No events in 24 h",
            TrendDirection.Rising => $"{events} in 24 h · ▲ up from {trend.Previous24Hours:N0}",
            TrendDirection.Falling => $"{events} in 24 h · ▼ down from {trend.Previous24Hours:N0}",
            _ => $"{events} in 24 h · same as the day before",
        };
    }

    private static string Unavailable(ProjectStatus status)
    {
        var first = status.AffectedSources[0];
        return status.AffectedSources.Count == 1
            ? $"Unavailable: {first}"
            : $"Unavailable: {first} and {status.AffectedSources.Count - 1} more";
    }
}
