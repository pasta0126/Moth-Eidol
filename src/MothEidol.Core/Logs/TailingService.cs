namespace MothEidol.Core.Logs;

public sealed class SourceStatusChangedEventArgs(long sourceId, SourceStatus status, string? detail) : EventArgs
{
    public long SourceId { get; } = sourceId;

    public SourceStatus Status { get; } = status;

    public string? Detail { get; } = detail;
}

/// <summary>Owns the configured sources and one background monitor per enabled source.</summary>
public sealed class TailingService : IAsyncDisposable
{
    private readonly ILogSourceRepository _sources;
    private readonly IReadPositionStore _positions;
    private readonly TailOptions _options;
    private readonly Func<long, bool> _projectEnabled;
    private readonly object _gate = new();
    private readonly Dictionary<long, SourceMonitor> _monitors = [];
    private readonly Dictionary<long, (SourceStatus Status, string? Detail)> _statuses = [];

    /// <param name="projectEnabled">Whether a project is switched on; a source runs only when it and its project are enabled.</param>
    public TailingService(
        ILogSourceRepository sources,
        IReadPositionStore positions,
        TailOptions? options = null,
        Func<long, bool>? projectEnabled = null)
    {
        _sources = sources;
        _positions = positions;
        _options = options ?? new TailOptions();
        _projectEnabled = projectEnabled ?? (_ => true);
    }

    public LineBus Lines { get; } = new();

    /// <summary>Raised from a background thread whenever a source's status changes.</summary>
    public event EventHandler<SourceStatusChangedEventArgs>? StatusChanged;

    public IReadOnlyList<LogSource> Sources => _sources.List();

    public (SourceStatus Status, string? Detail) GetStatus(long id)
    {
        lock (_gate) return _statuses.GetValueOrDefault(id, (SourceStatus.Paused, null));
    }

    /// <summary>Starts monitoring every enabled source saved from previous runs.</summary>
    public void Start()
    {
        foreach (var source in _sources.List())
        {
            if (IsActive(source)) StartMonitor(source);
            else SetStatus(source.Id, SourceStatus.Paused, null);
        }
    }

    /// <summary>Starts or stops the sources of a project after the project was enabled or disabled.</summary>
    public async Task RefreshProjectAsync(long projectId)
    {
        foreach (var source in _sources.List().Where(s => s.ProjectId == projectId))
        {
            if (IsActive(source))
            {
                StartMonitor(source);
            }
            else
            {
                await StopMonitorAsync(source.Id);
                SetStatus(source.Id, SourceStatus.Paused, null);
            }
        }
    }

    public LogSource Add(SourceKind kind, string path, string? pattern, long projectId = 0)
    {
        path = path?.Trim() ?? "";
        pattern = string.IsNullOrWhiteSpace(pattern) ? null : pattern.Trim();

        if (path.Length == 0) throw new ArgumentException("A path is required.");
        if (kind == SourceKind.Folder && pattern is null) throw new ArgumentException("A file-name pattern is required for a folder.");
        if (kind == SourceKind.File) pattern = null;

        var source = _sources.Add(kind, path, pattern, projectId);
        if (IsActive(source)) StartMonitor(source);
        else SetStatus(source.Id, SourceStatus.Paused, null);
        return source;
    }

    public async Task RemoveAsync(long id)
    {
        await StopMonitorAsync(id);
        _sources.Remove(id);
        _positions.Delete(id);
        lock (_gate) _statuses.Remove(id);
    }

    public async Task SetEnabledAsync(long id, bool enabled)
    {
        var source = _sources.List().FirstOrDefault(s => s.Id == id)
                     ?? throw new KeyNotFoundException($"Unknown source {id}.");

        if (enabled)
        {
            _sources.SetEnabled(id, true);
            var enabledSource = source with { Enabled = true };
            if (IsActive(enabledSource)) StartMonitor(enabledSource);
            else SetStatus(id, SourceStatus.Paused, null);
        }
        else
        {
            await StopMonitorAsync(id);
            _sources.SetEnabled(id, false);
            SetStatus(id, SourceStatus.Paused, null);
        }
    }

    public async ValueTask DisposeAsync()
    {
        long[] ids;
        lock (_gate) ids = [.. _monitors.Keys];
        foreach (var id in ids) await StopMonitorAsync(id);
    }

    private bool IsActive(LogSource source) =>
        source.Enabled && (source.ProjectId == 0 || _projectEnabled(source.ProjectId));

    private void StartMonitor(LogSource source)
    {
        SourceMonitor monitor;
        lock (_gate)
        {
            if (_monitors.ContainsKey(source.Id)) return;
            monitor = new SourceMonitor(source, _positions.Get(source.Id), _options, Lines, _positions, SetStatus);
            _monitors[source.Id] = monitor;
            _statuses[source.Id] = (SourceStatus.Active, null);
        }
        monitor.Start();
    }

    private async Task StopMonitorAsync(long id)
    {
        SourceMonitor? monitor;
        lock (_gate)
        {
            if (!_monitors.Remove(id, out monitor)) return;
        }
        await monitor.StopAsync();
    }

    private void SetStatus(long id, SourceStatus status, string? detail)
    {
        lock (_gate)
        {
            if (_statuses.TryGetValue(id, out var current) && current == (status, detail)) return;
            _statuses[id] = (status, detail);
        }
        StatusChanged?.Invoke(this, new SourceStatusChangedEventArgs(id, status, detail));
    }
}
