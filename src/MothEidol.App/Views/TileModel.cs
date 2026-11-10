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

    public bool HasDetail { get; private init; }

    /// <summary>Alert-level events per hour over the last 24 hours (oldest first), or null when not shown.</summary>
    public IReadOnlyList<int>? Series { get; private init; }

    public bool HasSeries => Series is not null;

    /// <summary>"12 events in 24 h · ▲ up from 4", empty when the project is not being monitored.</summary>
    public string HealthText { get; private init; } = "";

    public static TileModel NewProject() =>
        new() { IsNew = true, Name = "New project", FrameBrush = ThemeBrushes.Get("MothBorderBrush") };

    /// <summary>The project's accent (or the default one) for the tile stripe and name.</summary>
    public IBrush? AccentBrush { get; private init; }

    /// <summary>The tile frame: the accent for a project, the plain border for the "New project" tile.</summary>
    public IBrush? FrameBrush { get; private init; }

    /// <summary>
    /// The tile of a project: the traffic-light status (see <see cref="TileStatus"/>) with the project's accent,
    /// the activity trend and, only when it says something the status does not, a detail line.
    /// </summary>
    /// <param name="last48Hours">Alert-level events per hour for the last 48 hours, oldest first.</param>
    public static TileModel For(
        Project project, ProjectStatus status, PendingSummary pending, IReadOnlyList<int> last48Hours)
    {
        var tile = TileStatus.From(status, pending);
        var monitored = status.Health is ProjectHealth.Healthy or ProjectHealth.Degraded;
        var accent = Accent(project.AccentColor);
        return new TileModel
        {
            ProjectId = project.Id,
            Name = project.Name,
            StatusText = tile.Text,
            StatusBrush = ThemeBrushes.Get(BrushKey(tile.Light)),
            AccentBrush = accent,
            FrameBrush = accent,
            Detail = tile.Detail,
            HasDetail = tile.Detail.Length > 0,
            Series = monitored ? [.. last48Hours.Skip(Math.Max(0, last48Hours.Count - 24))] : null,
            HealthText = monitored ? Describe(Trend.From(last48Hours)) : "",
        };
    }

    private static string BrushKey(StatusLight light) => light switch
    {
        StatusLight.Green => "MothStatusGreenBrush",
        StatusLight.Yellow => "MothStatusYellowBrush",
        StatusLight.Orange => "MothStatusOrangeBrush",
        StatusLight.Red => "MothStatusRedBrush",
        _ => "MothTextMutedBrush",
    };

    /// <summary>The user's accent, or the theme's primary color when none is set (or it cannot be read).</summary>
    private static IBrush? Accent(string? hex) =>
        hex is not null && Color.TryParse(hex, out var color)
            ? new SolidColorBrush(color)
            : ThemeBrushes.Get("MothPrimaryBrush");

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
}
