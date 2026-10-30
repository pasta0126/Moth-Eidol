namespace MothEidol.Core.Logs;

/// <summary>
/// The recent entries of every project, collected in the background even while a project is not on screen,
/// so opening its tail shows what arrived in the meantime. Entries from sources without a project are ignored.
/// </summary>
public sealed class ProjectTails : IDisposable
{
    private readonly Func<long, long?> _projectOfSource;
    private readonly Subscription<LogEntry> _subscription;
    private readonly System.Collections.Concurrent.ConcurrentDictionary<long, ViewBuffer<LogEntry>> _buffers = new();
    private int _maxLines;

    public ProjectTails(EntryBus entries, Func<long, long?> projectOfSource, int maxLines)
    {
        _projectOfSource = projectOfSource;
        _maxLines = maxLines;
        _subscription = entries.Subscribe();
        _ = Task.Run(PumpAsync);
    }

    public ViewBuffer<LogEntry> For(long projectId) =>
        _buffers.GetOrAdd(projectId, _ => new ViewBuffer<LogEntry>(_maxLines));

    /// <summary>The line limit of every project's buffer, now and for buffers created later.</summary>
    public int MaxLines
    {
        get => _maxLines;
        set
        {
            _maxLines = value;
            foreach (var buffer in _buffers.Values) buffer.MaxLines = value;
        }
    }

    public void Dispose() => _subscription.Dispose();

    private async Task PumpAsync()
    {
        await foreach (var entry in _subscription.Reader.ReadAllAsync())
        {
            if (_projectOfSource(entry.SourceId) is { } projectId) For(projectId).Add(entry);
        }
    }
}
