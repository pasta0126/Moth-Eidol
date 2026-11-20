using MothEidol.Core.Logs;

namespace MothEidol.Core.Alerts;

/// <summary>Non-persistent event store, used when the database cannot be used and in tests.</summary>
public sealed class InMemoryEventStore : IEventStore
{
    private sealed class Entry
    {
        public required EventGroup Group { get; set; }
        public required string Key { get; init; }
        public List<EventOccurrence> Occurrences { get; } = [];
        public Dictionary<long, int> Buckets { get; } = [];
    }

    private static long HourOf(DateTimeOffset at) => at.ToUnixTimeSeconds() / 3600;

    private readonly object _gate = new();
    private readonly List<Entry> _groups = [];
    private long _nextGroupId = 1;
    private long _nextOccurrenceId = 1;

    public EventGroup Record(
        long projectId, EntryLevel level, string key, string title,
        long sourceId, DateTimeOffset at, string message, int maxOccurrences,
        string? location = null, IReadOnlyList<ContextEntry>? context = null)
    {
        lock (_gate)
        {
            var entry = _groups.FirstOrDefault(e => e.Group.ProjectId == projectId && e.Key == key);
            if (entry is null)
            {
                entry = new Entry
                {
                    Key = key,
                    Group = new EventGroup(_nextGroupId++, projectId, level, title, 0, at, at, null, 0),
                };
                _groups.Add(entry);
            }

            var group = entry.Group;
            entry.Group = group with
            {
                Title = title,
                Location = location,
                Count = group.Count + 1,
                PendingCount = group.PendingCount + 1,
                FirstSeen = at < group.FirstSeen ? at : group.FirstSeen,
                LastSeen = at > group.LastSeen ? at : group.LastSeen,
            };

            entry.Occurrences.Add(new EventOccurrence(_nextOccurrenceId++, group.Id, sourceId, at, message, context));
            entry.Buckets[HourOf(at)] = entry.Buckets.GetValueOrDefault(HourOf(at)) + 1;
            if (entry.Occurrences.Count > maxOccurrences)
                entry.Occurrences.RemoveRange(0, entry.Occurrences.Count - maxOccurrences);
            return entry.Group;
        }
    }

    public IReadOnlyList<EventGroup> Groups(long projectId)
    {
        lock (_gate) return [.. _groups.Where(e => e.Group.ProjectId == projectId).Select(e => e.Group)];
    }

    public IReadOnlyList<EventOccurrence> Occurrences(long groupId)
    {
        lock (_gate)
        {
            var entry = _groups.FirstOrDefault(e => e.Group.Id == groupId);
            return entry is null ? [] : [.. entry.Occurrences.AsEnumerable().Reverse()];
        }
    }

    public void MarkSeen(long groupId, DateTimeOffset at)
    {
        lock (_gate)
        {
            var entry = _groups.FirstOrDefault(e => e.Group.Id == groupId);
            if (entry is not null) entry.Group = entry.Group with { PendingCount = 0, SeenAt = at };
        }
    }

    public void MarkAllSeen(long projectId, DateTimeOffset at)
    {
        lock (_gate)
        {
            foreach (var entry in _groups.Where(e => e.Group.ProjectId == projectId))
                entry.Group = entry.Group with { PendingCount = 0, SeenAt = at };
        }
    }

    public PendingSummary Pending(long projectId)
    {
        lock (_gate)
        {
            var pending = _groups.Where(e => e.Group.ProjectId == projectId && e.Group.IsPending).Select(e => e.Group).ToList();
            return pending.Count == 0
                ? PendingSummary.None
                : new PendingSummary(pending.Sum(g => g.PendingCount), pending.Max(g => g.Level));
        }
    }

    public int PurgeQuiet(DateTimeOffset lastSeenBefore)
    {
        lock (_gate) return _groups.RemoveAll(e => !e.Group.IsPending && e.Group.LastSeen < lastSeenBefore);
    }

    public void RemoveProject(long projectId)
    {
        lock (_gate) _groups.RemoveAll(e => e.Group.ProjectId == projectId);
    }

    /// <summary>The project's load, estimated like the database does (UTF-8 text plus a per-row overhead).</summary>
    public MothEidol.Core.Projects.ProjectStorageUsage UsageOf(long projectId)
    {
        static long Bytes(string? text) => text is null ? 0 : System.Text.Encoding.UTF8.GetByteCount(text);
        lock (_gate)
        {
            var entries = _groups.Where(e => e.Group.ProjectId == projectId).ToList();
            var occurrences = entries.SelectMany(e => e.Occurrences).ToList();
            var buckets = entries.Sum(e => (long)e.Buckets.Count);
            var bytes =
                entries.Sum(e => Bytes(e.Group.Title) + Bytes(e.Key) + Bytes(e.Group.Location)
                                 + MothEidol.Core.Projects.ProjectStorageUsage.GroupOverhead)
                + occurrences.Sum(o => Bytes(o.Message) + (o.Context?.Sum(c => Bytes(c.Text)) ?? 0)
                                       + MothEidol.Core.Projects.ProjectStorageUsage.OccurrenceOverhead)
                + buckets * MothEidol.Core.Projects.ProjectStorageUsage.BucketOverhead;
            return new MothEidol.Core.Projects.ProjectStorageUsage(
                entries.Count, entries.Count(e => e.Group.IsPending), occurrences.Count, buckets, bytes,
                occurrences.Count == 0 ? null : occurrences.Min(o => o.At),
                occurrences.Count == 0 ? null : occurrences.Max(o => o.At));
        }
    }

    /// <summary>Removes every group of the project, pending ones included.</summary>
    public MothEidol.Core.Projects.CleanupResult ClearProject(long projectId)
    {
        lock (_gate)
        {
            var removed = _groups.Where(e => e.Group.ProjectId == projectId).ToList();
            _groups.RemoveAll(e => e.Group.ProjectId == projectId);
            return new(removed.Count, removed.Sum(e => e.Occurrences.Count));
        }
    }

    /// <summary>Keeps only what happened since the cutoff; kept groups keep their totals and pending state.</summary>
    public MothEidol.Core.Projects.CleanupResult KeepSince(long projectId, DateTimeOffset cutoff)
    {
        var cutoffHour = HourOf(cutoff);
        lock (_gate)
        {
            var entries = _groups.Where(e => e.Group.ProjectId == projectId).ToList();
            var stale = entries.Where(e => e.Group.LastSeen < cutoff).ToList();
            var occurrences = stale.Sum(e => e.Occurrences.Count);
            _groups.RemoveAll(stale.Contains);
            foreach (var entry in entries.Except(stale))
            {
                occurrences += entry.Occurrences.RemoveAll(o => o.At < cutoff);
                foreach (var hour in entry.Buckets.Keys.Where(h => h < cutoffHour).ToList()) entry.Buckets.Remove(hour);
            }
            return new(stale.Count, occurrences);
        }
    }

    public IReadOnlyList<int> GroupSeries(long groupId, DateTimeOffset now, int hours)
    {
        lock (_gate) return Series(_groups.Where(e => e.Group.Id == groupId), now, hours);
    }

    public IReadOnlyList<int> ProjectSeries(long projectId, DateTimeOffset now, int hours)
    {
        lock (_gate) return Series(_groups.Where(e => e.Group.ProjectId == projectId), now, hours);
    }

    public int PruneBuckets(DateTimeOffset olderThan)
    {
        var cutoff = HourOf(olderThan);
        var removed = 0;
        lock (_gate)
        {
            foreach (var entry in _groups)
            {
                foreach (var hour in entry.Buckets.Keys.Where(h => h < cutoff).ToList())
                {
                    entry.Buckets.Remove(hour);
                    removed++;
                }
            }
        }
        return removed;
    }

    private static int[] Series(IEnumerable<Entry> entries, DateTimeOffset now, int hours)
    {
        var result = new int[hours];
        var current = HourOf(now);
        foreach (var entry in entries)
        {
            foreach (var (hour, count) in entry.Buckets)
            {
                var index = hours - 1 - (int)(current - hour);
                if (index >= 0 && index < hours) result[index] += count;
            }
        }
        return result;
    }
}
