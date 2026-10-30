using System.Text;

namespace MothEidol.Core.Alerts;

public static class EventSummaryText
{
    /// <summary>A plain-text summary of a group, ready to paste into a chat or a ticket.</summary>
    public static string Build(string projectName, EventGroup group, EventOccurrence? latest)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[{group.Level.ToString().ToUpperInvariant()}] {group.Title}");
        if (!string.IsNullOrEmpty(group.Location)) sb.AppendLine($"Location: {group.Location}");
        sb.AppendLine($"Project: {projectName}");
        sb.AppendLine($"Occurrences: {group.Count:N0} ({(group.IsPending ? $"{group.PendingCount:N0} pending" : "all seen")})");
        sb.AppendLine($"First seen: {group.FirstSeen.ToLocalTime():yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Last seen: {group.LastSeen.ToLocalTime():yyyy-MM-dd HH:mm:ss}");

        if (latest is not null)
        {
            sb.AppendLine();
            sb.AppendLine($"Latest occurrence ({latest.At.ToLocalTime():yyyy-MM-dd HH:mm:ss}):");
            sb.AppendLine(latest.Message.TrimEnd());
        }
        return sb.ToString().TrimEnd();
    }
}
