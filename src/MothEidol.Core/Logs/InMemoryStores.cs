namespace MothEidol.Core.Logs;

/// <summary>Non-persistent source list, used when the database cannot be used and in tests.</summary>
public sealed class InMemoryLogSourceRepository : ILogSourceRepository
{
    private readonly object _gate = new();
    private readonly List<LogSource> _sources = [];
    private long _nextId = 1;

    public IReadOnlyList<LogSource> List()
    {
        lock (_gate) return [.. _sources];
    }

    public LogSource Add(SourceKind kind, string path, string? pattern)
    {
        lock (_gate)
        {
            var source = new LogSource(_nextId++, kind, path, pattern, Enabled: true);
            _sources.Add(source);
            return source;
        }
    }

    public void Remove(long id)
    {
        lock (_gate) _sources.RemoveAll(s => s.Id == id);
    }

    public void SetEnabled(long id, bool enabled)
    {
        lock (_gate)
        {
            var index = _sources.FindIndex(s => s.Id == id);
            if (index >= 0) _sources[index] = _sources[index] with { Enabled = enabled };
        }
    }
}

public sealed class InMemoryReadPositionStore : IReadPositionStore
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<long, FileCursor> _cursors = new();

    public FileCursor? Get(long sourceId) => _cursors.GetValueOrDefault(sourceId);

    public void Save(long sourceId, FileCursor cursor) => _cursors[sourceId] = cursor;

    public void Delete(long sourceId) => _cursors.TryRemove(sourceId, out _);
}
