using MothEidol.Core.Alerts;
using MothEidol.Core.Logs;

namespace MothEidol.Core.Projects;

/// <summary>
/// Creates, edits and removes projects and their sources, keeping the running monitors in step
/// so that no change needs a restart.
/// </summary>
public sealed class ProjectService
{
    private readonly IProjectRepository _projects;
    private readonly TailingService _tailing;
    private readonly IEventStore? _events;
    private readonly IProjectStorage? _storage;
    private readonly TimeProvider _time;
    private readonly object _gate = new();
    private readonly SemaphoreSlim _maintenance = new(1, 1);
    private Dictionary<long, long> _sourceProject = [];
    private Dictionary<long, Project> _snapshot = [];
    private Dictionary<long, long> _sizes = [];

    /// <param name="events">When given, a project's recorded events are removed together with the project.</param>
    /// <param name="storage">When given, project storage can be measured and cleaned up, and the file compacted.</param>
    public ProjectService(
        IProjectRepository projects, TailingService tailing, IEventStore? events = null,
        IProjectStorage? storage = null, TimeProvider? time = null)
    {
        _projects = projects;
        _tailing = tailing;
        _events = events;
        _storage = storage;
        _time = time ?? TimeProvider.System;
        RefreshSourceMap();
        RefreshSnapshot();
    }

    public const int MinKeepDays = 1, MaxKeepDays = 365, DefaultKeepDays = 7;

    /// <summary>Raised after any change to projects or their configuration.</summary>
    public event EventHandler? Changed;

    /// <summary>Projects in the user's order (by name until the user moves one; new projects last).</summary>
    public IReadOnlyList<Project> Projects => _projects.List();

    public Project? Get(long id) => _projects.List().FirstOrDefault(p => p.Id == id);

    /// <summary>A project from memory, cheap enough to call for every entry; refreshed after every change.</summary>
    public Project? Lookup(long id)
    {
        lock (_gate) return _snapshot.GetValueOrDefault(id);
    }

    public bool IsEnabled(long id) => Get(id)?.Enabled ?? false;

    public IReadOnlyList<LogSource> SourcesOf(long projectId) =>
        [.. _tailing.Sources.Where(s => s.ProjectId == projectId)];

    /// <summary>The project a source belongs to, from memory (cheap enough to call for every entry).</summary>
    public long? ProjectOfSource(long sourceId)
    {
        lock (_gate) return _sourceProject.TryGetValue(sourceId, out var id) ? id : null;
    }

    public ProjectStatus StatusOf(long projectId)
    {
        var project = Get(projectId) ?? throw new KeyNotFoundException($"Unknown project {projectId}.");
        return ProjectStatus.Calculate(project, SourcesOf(projectId), id => _tailing.GetStatus(id).Status);
    }

    /// <summary>Creates a project from just a name; its sources and alert settings are set afterwards.</summary>
    public Project Create(string name)
    {
        name = ValidName(name, exceptId: null);
        var project = _projects.Add(name);
        RaiseChanged();
        return project;
    }

    /// <summary>
    /// Creates a project together with its sources and alert settings, all or nothing: everything is validated
    /// first, and if saving fails midway what was already saved is undone.
    /// </summary>
    public async Task<Project> CreateWithSourcesAsync(ProjectDraft draft)
    {
        var name = ValidName(draft.Name, exceptId: null);
        draft.Alerts.Validate();
        if (draft.ValidateSources() is { } noSources) throw new ArgumentException(noSources);
        var sources = draft.Sources.Select(s => s.Normalized()).ToList();
        foreach (var source in sources)
        {
            if (ProjectDraft.ValidateSource(source) is { } invalid) throw new ArgumentException(invalid);
        }

        var project = _projects.Add(name);
        var added = new List<LogSource>();
        try
        {
            _projects.Update(project with { Alerts = draft.Alerts });
            foreach (var source in sources) added.Add(_tailing.Add(source.Kind, source.Path, source.Pattern, project.Id));
        }
        catch
        {
            foreach (var source in added) await _tailing.RemoveAsync(source.Id);
            _projects.Remove(project.Id);
            RefreshSourceMap();
            throw;
        }

        RefreshSourceMap();
        RaiseChanged();
        return project with { Alerts = draft.Alerts };
    }

    public void Rename(long id, string name)
    {
        var project = Require(id);
        _projects.Update(project with { Name = ValidName(name, exceptId: id) });
        RaiseChanged();
    }

    /// <summary>Sets the project's accent color; null (or blank) returns to the default. Invalid hex is rejected.</summary>
    public void SetAccent(long id, string? color)
    {
        string? normalized = null;
        if (!string.IsNullOrWhiteSpace(color))
        {
            normalized = AccentColor.Normalize(color)
                ?? throw new ArgumentException("Enter a color as a hex value, for example #3FA7D6.");
        }
        _projects.Update(Require(id) with { AccentColor = normalized });
        RaiseChanged();
    }

    /// <summary>Stores the order of the tiles: the given projects first, in that order.</summary>
    public void Reorder(IReadOnlyList<long> orderedIds)
    {
        _projects.Reorder(orderedIds);
        RaiseChanged();
    }

    public void UpdateAlerts(long id, AlertSettings alerts)
    {
        alerts.Validate();
        _projects.Update(Require(id) with { Alerts = alerts });
        RaiseChanged();
    }

    public async Task SetEnabledAsync(long id, bool enabled)
    {
        _projects.Update(Require(id) with { Enabled = enabled });
        await _tailing.RefreshProjectAsync(id);
        RaiseChanged();
    }

    /// <summary>
    /// Removes the project with everything stored for it. The repository does it in one operation; if that fails
    /// the project's monitors start again and nothing is lost. The freed space is returned to the disk afterwards,
    /// in the background.
    /// </summary>
    public async Task RemoveAsync(long id)
    {
        Require(id);
        await _tailing.UnloadProjectAsync(id);
        try
        {
            _projects.Remove(id);
        }
        catch
        {
            await _tailing.RefreshProjectAsync(id);
            throw;
        }

        // Stores that are not removed with the project (the in-memory ones) are cleaned up in step.
        _tailing.ForgetProjectSources(id);
        _events?.RemoveProject(id);
        lock (_gate) _sizes.Remove(id);
        RefreshSourceMap();
        RaiseChanged();
        _ = Task.Run(CompactQuietly);
    }

    /// <summary>Changes the storage threshold of a project (whole MB within the allowed range).</summary>
    public void SetStorageWarning(long id, int megabytes)
    {
        if (megabytes is < Project.MinStorageWarningMb or > Project.MaxStorageWarningMb)
            throw new ArgumentException(
                $"The storage threshold must be between {Project.MinStorageWarningMb} and {Project.MaxStorageWarningMb:N0} MB.");
        _projects.Update(Require(id) with { StorageWarningMb = megabytes });
        RaiseChanged();
    }

    /// <summary>The project's last measured size against its threshold; cheap (reads only memory).</summary>
    public StorageWarning StorageWarningOf(long projectId)
    {
        lock (_gate)
        {
            var threshold = _snapshot.GetValueOrDefault(projectId)?.StorageWarningMb ?? Project.DefaultStorageWarningMb;
            return new StorageWarning(_sizes.TryGetValue(projectId, out var size) ? size : null, threshold);
        }
    }

    /// <summary>Measures the project's storage off the UI thread and remembers its size for the warning.</summary>
    public Task<ProjectStorageUsage> UsageOfAsync(long projectId) => Task.Run(() => Measure(projectId));

    public Task<DatabaseFileUsage> DatabaseFileAsync() =>
        Task.Run(() => _storage?.File() ?? DatabaseFileUsage.None);

    /// <summary>Measures every project, for the storage warnings (at startup and periodically).</summary>
    public Task RefreshStorageSizesAsync() => Task.Run(RefreshStorageSizes);

    /// <summary>Periodic upkeep: checkpoints the write-ahead log and measures every project again.</summary>
    public void MaintainStorage()
    {
        _storage?.Checkpoint();
        RefreshStorageSizes();
    }

    /// <summary>Removes every event of the project (pending ones included) and returns the space to the disk.</summary>
    public Task<CleanupResult> ClearEventsAsync(long projectId)
    {
        Require(projectId);
        return CleanupAsync(projectId, storage => storage.ClearAll(projectId));
    }

    /// <summary>Keeps only the last <paramref name="days"/> days of the project's history (1–365).</summary>
    public Task<CleanupResult> KeepLastDaysAsync(long projectId, int days)
    {
        Require(projectId);
        var cutoff = KeepCutoff(days);
        return CleanupAsync(projectId, storage => storage.KeepLastDays(projectId, cutoff));
    }

    /// <summary>The groups a "keep the last days" cleanup would remove, and how many of them are pending.</summary>
    public (int Groups, int Pending) KeepLastDaysPreview(long projectId, int days)
    {
        var cutoff = KeepCutoff(days);
        var stale = (_events?.Groups(projectId) ?? []).Where(g => g.LastSeen < cutoff).ToList();
        return (stale.Count, stale.Count(g => g.IsPending));
    }

    private DateTimeOffset KeepCutoff(int days)
    {
        if (days is < MinKeepDays or > MaxKeepDays)
            throw new ArgumentException($"Enter a number of days between {MinKeepDays} and {MaxKeepDays}.");
        return _time.GetUtcNow() - TimeSpan.FromDays(days);
    }

    private async Task<CleanupResult> CleanupAsync(long projectId, Func<IProjectStorage, CleanupResult> cleanup)
    {
        if (_storage is not { } storage) return CleanupResult.Nothing;
        var result = await Task.Run(async () =>
        {
            await _maintenance.WaitAsync();
            try
            {
                var removed = cleanup(storage);
                CompactCore(storage);
                return removed;
            }
            finally
            {
                _maintenance.Release();
            }
        });
        Measure(projectId);
        RaiseChanged();
        return result;
    }

    private ProjectStorageUsage Measure(long projectId)
    {
        if (_storage is null) return ProjectStorageUsage.Empty;
        var usage = _storage.Usage(projectId);
        lock (_gate) _sizes[projectId] = usage.ApproxBytes;
        return usage;
    }

    private void RefreshStorageSizes()
    {
        if (_storage is null) return;
        foreach (var project in _projects.List()) Measure(project.Id);
    }

    private async Task CompactQuietly()
    {
        if (_storage is null) return;
        await _maintenance.WaitAsync();
        try { CompactCore(_storage); }
        finally { _maintenance.Release(); }
    }

    /// <summary>A failed compaction is skipped: the next cleanup or removal compacts again.</summary>
    private static void CompactCore(IProjectStorage storage)
    {
        try { storage.Compact(); }
        catch (Exception) { }
    }

    public LogSource AddSource(long projectId, SourceKind kind, string path, string? pattern)
    {
        Require(projectId);
        var source = _tailing.Add(kind, path, pattern, projectId);
        RefreshSourceMap();
        RaiseChanged();
        return source;
    }

    public async Task RemoveSourceAsync(long sourceId)
    {
        await _tailing.RemoveAsync(sourceId);
        RefreshSourceMap();
        RaiseChanged();
    }

    public async Task SetSourceEnabledAsync(long sourceId, bool enabled)
    {
        await _tailing.SetEnabledAsync(sourceId, enabled);
        RaiseChanged();
    }

    private Project Require(long id) => Get(id) ?? throw new KeyNotFoundException($"Unknown project {id}.");

    private string ValidName(string name, long? exceptId)
    {
        name = name?.Trim() ?? "";
        if (name.Length == 0) throw new ArgumentException("A project needs a name.");
        if (_projects.List().Any(p => p.Id != exceptId && string.Equals(p.Name, name, StringComparison.CurrentCultureIgnoreCase)))
            throw new ArgumentException($"A project named '{name}' already exists.");
        return name;
    }

    private void RefreshSourceMap()
    {
        var map = _tailing.Sources.ToDictionary(s => s.Id, s => s.ProjectId);
        lock (_gate) _sourceProject = map;
    }

    private void RefreshSnapshot()
    {
        var snapshot = _projects.List().ToDictionary(p => p.Id);
        lock (_gate) _snapshot = snapshot;
    }

    private void RaiseChanged()
    {
        RefreshSnapshot();
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
