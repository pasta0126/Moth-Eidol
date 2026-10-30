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

    public static TileModel NewProject() => new() { IsNew = true, Name = "New project" };

    /// <summary>
    /// The tile of a project. What waits for the user outweighs a healthy status: pending events take the
    /// headline, colored by the worst pending level, while the source health stays in the detail.
    /// </summary>
    public static TileModel For(Project project, ProjectStatus status, int sourceCount, PendingSummary pending)
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

        return new TileModel
        {
            ProjectId = project.Id,
            Name = project.Name,
            StatusText = text,
            StatusBrush = ThemeBrushes.Get(brush),
            Detail = detail,
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
