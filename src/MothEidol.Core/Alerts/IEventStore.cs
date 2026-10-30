using MothEidol.Core.Logs;

namespace MothEidol.Core.Alerts;

/// <summary>
/// A group of alert-level entries of one project. <see cref="PendingCount"/> counts the occurrences since the
/// user last marked the group as seen.
/// </summary>
public sealed record EventGroup(
    long Id,
    long ProjectId,
    EntryLevel Level,
    string Title,
    long Count,
    DateTimeOffset FirstSeen,
    DateTimeOffset LastSeen,
    DateTimeOffset? SeenAt,
    long PendingCount,
    string? Location = null)
{
    public bool IsPending => PendingCount > 0;

    /// <summary>First seen within the last 24 hours.</summary>
    public bool IsNew(DateTimeOffset now) => now - FirstSeen < TimeSpan.FromHours(24);

    /// <summary>Came back after being marked as seen: it has occurrences newer than the seen mark.</summary>
    public bool IsRegressed => SeenAt is not null && PendingCount > 0;
}

/// <summary>An entry that preceded an occurrence in the same source, kept to show what led to it.</summary>
public sealed record ContextEntry(DateTimeOffset At, MothEidol.Core.Logs.EntryLevel Level, string Text);

/// <summary>One recorded occurrence of a group, with its full text (including any stack trace).</summary>
public sealed record EventOccurrence(
    long Id, long GroupId, long SourceId, DateTimeOffset At, string Message,
    IReadOnlyList<ContextEntry>? Context = null);

/// <summary>What is waiting for the user in a project: pending occurrences and the worst level among them.</summary>
public sealed record PendingSummary(long Occurrences, EntryLevel? WorstLevel)
{
    public static PendingSummary None { get; } = new(0, null);
}

public interface IEventStore
{
    /// <summary>
    /// Adds an occurrence to the group identified by <paramref name="key"/> (creating it if needed), counts it
    /// as pending and keeps only the latest <paramref name="maxOccurrences"/> occurrences with their text.
    /// </summary>
    EventGroup Record(
        long projectId, EntryLevel level, string key, string title,
        long sourceId, DateTimeOffset at, string message, int maxOccurrences,
        string? location = null, IReadOnlyList<ContextEntry>? context = null);

    IReadOnlyList<EventGroup> Groups(long projectId);

    /// <summary>The retained occurrences of a group, newest first.</summary>
    IReadOnlyList<EventOccurrence> Occurrences(long groupId);

    void MarkSeen(long groupId, DateTimeOffset at);

    void MarkAllSeen(long projectId, DateTimeOffset at);

    PendingSummary Pending(long projectId);

    /// <summary>Removes groups that have nothing pending and were last seen before the cutoff. Returns how many.</summary>
    int PurgeQuiet(DateTimeOffset lastSeenBefore);

    void RemoveProject(long projectId);

    /// <summary>Occurrences of a group per hour for the last <paramref name="hours"/> hours, oldest first; the last is the current hour.</summary>
    IReadOnlyList<int> GroupSeries(long groupId, DateTimeOffset now, int hours);

    /// <summary>Occurrences of all the groups of a project per hour, oldest first.</summary>
    IReadOnlyList<int> ProjectSeries(long projectId, DateTimeOffset now, int hours);

    /// <summary>Drops hourly counts older than the cutoff. Returns how many were removed.</summary>
    int PruneBuckets(DateTimeOffset olderThan);
}

public static class EventGroupOrder
{
    /// <summary>Pending groups first, then the most recently seen first.</summary>
    public static IReadOnlyList<EventGroup> ForDisplay(IEnumerable<EventGroup> groups) =>
        [.. groups.OrderByDescending(g => g.IsPending).ThenByDescending(g => g.LastSeen)];
}
