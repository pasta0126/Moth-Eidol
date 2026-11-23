namespace MothEidol.Core.Projects;

/// <summary>How a project stands for the application storage list.</summary>
public enum StoredProjectState
{
    Enabled,
    Disabled,

    /// <summary>The project has no sources.</summary>
    NotConfigured,
}

/// <summary>One project in the application storage list.</summary>
public sealed record StoredProject(Project Project, StoredProjectState State, int Sources, ProjectStorageUsage Usage)
{
    /// <summary>Not monitoring anything (disabled or without sources) yet still holding events.</summary>
    public bool IsCleanupCandidate => State != StoredProjectState.Enabled && Usage.Groups > 0;

    public string StateText => State switch
    {
        StoredProjectState.Enabled => "Enabled",
        StoredProjectState.Disabled => "Disabled",
        _ => "Not configured",
    };
}

/// <summary>Everything the local database holds, across projects.</summary>
public sealed record AppStorageOverview(
    string? DataFolder,
    DatabaseFileUsage File,
    long ReclaimableBytes,
    IReadOnlyList<StoredProject> Projects,
    LeftoverUsage Leftovers,
    IReadOnlyList<DataFile> CorruptBackups)
{
    public long TotalApproxBytes => Projects.Sum(p => p.Usage.ApproxBytes);

    public bool HasLeftovers => Leftovers.Total > 0 || CorruptBackups.Count > 0;
}

/// <summary>What an action of the application storage removed and how much the files shrank.</summary>
public sealed record AppStorageResult(
    int Projects, long Groups, long Occurrences, long Rows, int Files, long FreedBytes, IReadOnlyList<string> Failures)
{
    public string Summary
    {
        get
        {
            var parts = new List<string>();
            if (Projects > 0) parts.Add(Plural(Projects, "project"));
            if (Groups > 0) parts.Add(Plural(Groups, "event group"));
            if (Occurrences > 0) parts.Add(Plural(Occurrences, "occurrence"));
            if (Rows > 0) parts.Add(Plural(Rows, "leftover row"));
            if (Files > 0) parts.Add(Plural(Files, "backup file"));
            var removed = parts.Count == 0 ? "Nothing was removed" : "Removed " + Join(parts);
            return $"{removed}. {ByteSize.Format(Math.Max(0, FreedBytes))} freed on the disk.";
        }
    }

    private static string Plural(long count, string noun) => $"{count:N0} {noun}{(count == 1 ? "" : "s")}";

    private static string Join(List<string> parts) =>
        parts.Count == 1 ? parts[0] : string.Join(", ", parts[..^1]) + " and " + parts[^1];
}

/// <summary>
/// The application-wide view of the local database: every stored project with its load, leftovers of removed
/// projects and corrupt copies, and the actions to remove them. Clearing and removing projects go through
/// <see cref="ProjectService"/>, so they behave exactly as from the project screen.
/// </summary>
public sealed class AppStorageService(ProjectService projects, IProjectStorage storage, IDataFolder folder)
{
    /// <summary>Measures everything off the UI thread.</summary>
    public Task<AppStorageOverview> OverviewAsync() => Task.Run(Overview);

    public AppStorageOverview Overview()
    {
        var usage = storage.UsageOfAll();
        var rows = projects.Projects
            .Select(project =>
            {
                var sources = projects.SourcesOf(project.Id).Count;
                var state = sources == 0 ? StoredProjectState.NotConfigured
                    : project.Enabled ? StoredProjectState.Enabled
                    : StoredProjectState.Disabled;
                return new StoredProject(project, state, sources, usage.GetValueOrDefault(project.Id) ?? ProjectStorageUsage.Empty);
            })
            .OrderByDescending(p => p.Usage.ApproxBytes)
            .ThenBy(p => p.Project.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        return new AppStorageOverview(
            folder.Location, storage.File(), storage.ReclaimableBytes(), rows, storage.Leftovers(), folder.CorruptBackups());
    }

    /// <summary>Removes every event of the project, as "Clear all" in its own storage section.</summary>
    public async Task<AppStorageResult> ClearProjectAsync(long projectId)
    {
        var before = FileBytes();
        var removed = await projects.ClearEventsAsync(projectId);
        return new AppStorageResult(0, removed.Groups, removed.Occurrences, 0, 0, before - FileBytes(), []);
    }

    /// <summary>Removes the project with everything stored for it, as from its own settings.</summary>
    public async Task<AppStorageResult> RemoveProjectAsync(long projectId)
    {
        var usage = await Task.Run(() => storage.Usage(projectId));
        var before = FileBytes();
        await projects.RemoveAsync(projectId);
        await projects.WhenCompacted();
        return new AppStorageResult(1, usage.Groups, usage.Occurrences, 0, 0, before - FileBytes(), []);
    }

    /// <summary>
    /// Removes the rows of projects, sources and groups that no longer exist and the copies of a corrupt database,
    /// then compacts. A copy that cannot be deleted is reported and the rest goes on.
    /// </summary>
    public async Task<AppStorageResult> RemoveLeftoversAsync()
    {
        var backups = await Task.Run(folder.CorruptBackups);
        var before = FileBytes() + backups.Sum(b => b.Bytes);
        var rows = await Task.Run(storage.RemoveOrphans);
        var failures = new List<string>();
        var deleted = 0;
        long kept = 0;
        foreach (var backup in backups)
        {
            try
            {
                await Task.Run(() => folder.Delete(backup));
                deleted++;
            }
            catch (Exception ex)
            {
                kept += backup.Bytes;
                failures.Add($"{backup.Name}: {ex.Message}");
            }
        }
        await projects.CompactAsync();
        return new AppStorageResult(0, 0, 0, rows, deleted, before - FileBytes() - kept, failures);
    }

    /// <summary>Returns the database's free space to the disk.</summary>
    public async Task<AppStorageResult> CompactAsync()
    {
        var before = FileBytes();
        await projects.CompactAsync();
        return new AppStorageResult(0, 0, 0, 0, 0, before - FileBytes(), []);
    }

    private long FileBytes() => storage.File().TotalBytes;
}
