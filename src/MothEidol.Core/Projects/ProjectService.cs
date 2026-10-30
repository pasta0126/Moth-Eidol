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
    private readonly object _gate = new();
    private Dictionary<long, long> _sourceProject = [];

    public ProjectService(IProjectRepository projects, TailingService tailing)
    {
        _projects = projects;
        _tailing = tailing;
        RefreshSourceMap();
    }

    /// <summary>Raised after any change to projects or their configuration.</summary>
    public event EventHandler? Changed;

    /// <summary>Projects ordered by name.</summary>
    public IReadOnlyList<Project> Projects =>
        [.. _projects.List().OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase)];

    public Project? Get(long id) => _projects.List().FirstOrDefault(p => p.Id == id);

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

    public void Rename(long id, string name)
    {
        var project = Require(id);
        _projects.Update(project with { Name = ValidName(name, exceptId: id) });
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

    public async Task RemoveAsync(long id)
    {
        Require(id);
        foreach (var source in SourcesOf(id)) await _tailing.RemoveAsync(source.Id);
        _projects.Remove(id);
        RefreshSourceMap();
        RaiseChanged();
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

    private void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);
}
