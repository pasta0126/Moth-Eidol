using MothEidol.Core.Alerts;
using MothEidol.Core.Logs;

namespace MothEidol.Core.Notifications;

/// <param name="Level">The level to color the notification with (the worst one for a summary).</param>
public sealed record AlertText(string ProjectName, EntryLevel Level, string Headline, string? Detail)
{
    /// <summary>Words for a person: the readable title and where it happens, or the counts for a summary.</summary>
    public static AlertText For(AlertDelivery delivery, string projectName)
    {
        if (delivery.Kind == DeliveryKind.Alert && delivery.Alert is { } alert)
        {
            var description = EventDescriber.Describe(alert.Message);
            return new AlertText(projectName, alert.Level, description.Title, description.Location);
        }

        var summary = delivery.Summary!;
        var worst = summary.ByLevel.Keys.DefaultIfEmpty(EntryLevel.Error).Max();
        var headline = summary.Count == 1 ? "1 more alert" : $"{summary.Count:N0} more alerts";
        return new AlertText(projectName, worst, headline, Breakdown(summary));
    }

    private static string Breakdown(AlertSummary summary)
    {
        var parts = summary.ByLevel
            .OrderByDescending(p => p.Key)
            .Select(p => $"{p.Value:N0} {Word(p.Key)}{(p.Value == 1 ? "" : "s")}");
        return string.Join(", ", parts);
    }

    private static string Word(EntryLevel level) => level switch
    {
        EntryLevel.Fatal => "fatal error",
        EntryLevel.Error => "error",
        EntryLevel.Warning => "warning",
        _ => "event",
    };
}
