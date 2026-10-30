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
    }

    private readonly object _gate = new();
    private readonly List<Entry> _groups = [];
    private long _nextGroupId = 1;
    private long _nextOccurrenceId = 1;

    public EventGroup Record(
        long projectId, EntryLevel level, string key, string title,
        long sourceId, DateTimeOffset at, string message, int maxOccurrences)
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
                Count = group.Count + 1,
                PendingCount = group.PendingCount + 1,
                FirstSeen = at < group.FirstSeen ? at : group.FirstSeen,
                LastSeen = at > group.LastSeen ? at : group.LastSeen,
            };

            entry.Occurrences.Add(new EventOccurrence(_nextOccurrenceId++, group.Id, sourceId, at, message));
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
}
