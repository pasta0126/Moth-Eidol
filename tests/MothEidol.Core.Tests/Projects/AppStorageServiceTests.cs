using MothEidol.Core.Alerts;
using MothEidol.Core.Logs;
using MothEidol.Core.Projects;
using MothEidol.Core.Tests.Logs;

namespace MothEidol.Core.Tests.Projects;

public sealed class AppStorageServiceTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    /// <summary>Counts leftovers and compactions so the service's calls can be checked.</summary>
    private sealed class Storage(InMemoryEventStore events) : IProjectStorage
    {
        private readonly InMemoryProjectStorage _inner = new(events);
        public LeftoverUsage NextLeftovers { get; set; } = LeftoverUsage.None;
        public int Compactions { get; private set; }

        public ProjectStorageUsage Usage(long projectId) => _inner.Usage(projectId);
        public IReadOnlyDictionary<long, ProjectStorageUsage> UsageOfAll() => _inner.UsageOfAll();
        public DatabaseFileUsage File() => DatabaseFileUsage.None;
        public long ReclaimableBytes() => 0;
        public LeftoverUsage Leftovers() => NextLeftovers;
        public CleanupResult ClearAll(long projectId) => _inner.ClearAll(projectId);
        public CleanupResult KeepLastDays(long projectId, DateTimeOffset cutoff) => _inner.KeepLastDays(projectId, cutoff);
        public void Compact() => Compactions++;
        public void Checkpoint() { }

        public int RemoveOrphans()
        {
            var removed = (int)NextLeftovers.Total;
            NextLeftovers = LeftoverUsage.None;
            return removed;
        }
    }

    private sealed class Folder : IDataFolder
    {
        public List<DataFile> Backups { get; } = [];
        public HashSet<string> Locked { get; } = [];
        public string? Location => "/data";
        public IReadOnlyList<DataFile> CorruptBackups() => [.. Backups];

        public void Delete(DataFile file)
        {
            if (Locked.Contains(file.Name)) throw new IOException("in use");
            Backups.Remove(file);
        }
    }

    private readonly TempFolder _temp = new();
    private readonly InMemoryEventStore _events = new();
    private readonly InMemoryReadPositionStore _positions = new();
    private readonly Storage _storage;
    private readonly Folder _folder = new();
    private readonly TailingService _tailing;
    private readonly ProjectService _projects;
    private readonly AppStorageService _service;

    public AppStorageServiceTests()
    {
        _storage = new Storage(_events);
        _tailing = new TailingService(new InMemoryLogSourceRepository(), _positions, new TailOptions());
        _projects = new ProjectService(new InMemoryProjectRepository(), _tailing, _events, _storage, new FixedTime(Now));
        _service = new AppStorageService(_projects, _storage, _folder);
    }

    public void Dispose()
    {
        _tailing.DisposeAsync().AsTask().Wait();
        _temp.Dispose();
    }

    private void Record(Project project, string key, int size = 10) =>
        _events.Record(project.Id, EntryLevel.Error, key, key, 1, Now, new string('x', size), 50);

    [Fact]
    public async Task Projects_are_listed_by_size_with_their_state_and_only_idle_ones_with_events_are_candidates()
    {
        var enabled = _projects.Create("Enabled");
        _projects.AddSource(enabled.Id, SourceKind.File, _temp.File("a.log"), null);
        Record(enabled, "a", size: 100);
        var disabled = _projects.Create("Disabled");
        _projects.AddSource(disabled.Id, SourceKind.File, _temp.File("b.log"), null);
        await _projects.SetEnabledAsync(disabled.Id, false);
        Record(disabled, "b", size: 5000);
        _projects.Create("Empty");

        var overview = await _service.OverviewAsync();

        Assert.Equal(["Disabled", "Enabled", "Empty"], overview.Projects.Select(p => p.Project.Name));
        Assert.Equal(
            [StoredProjectState.Disabled, StoredProjectState.Enabled, StoredProjectState.NotConfigured],
            overview.Projects.Select(p => p.State));
        Assert.Equal([true, false, false], overview.Projects.Select(p => p.IsCleanupCandidate));
        Assert.Equal(overview.Projects.Sum(p => p.Usage.ApproxBytes), overview.TotalApproxBytes);
        Assert.Equal("/data", overview.DataFolder);
        Assert.False(overview.HasLeftovers);
    }

    [Fact]
    public async Task A_project_without_sources_holding_events_is_a_candidate()
    {
        var project = _projects.Create("Old");
        Record(project, "a");

        var row = Assert.Single((await _service.OverviewAsync()).Projects);

        Assert.Equal(StoredProjectState.NotConfigured, row.State);
        Assert.True(row.IsCleanupCandidate);
    }

    [Fact]
    public async Task Clearing_a_project_removes_its_events_and_keeps_the_project_and_its_read_positions()
    {
        var project = _projects.Create("Billing");
        var source = _projects.AddSource(project.Id, SourceKind.File, _temp.File("a.log"), null);
        _positions.Save(source.Id, new FileCursor("a.log", 77, null, LineEncoding.Utf8));
        Record(project, "a");
        Record(project, "b");

        var result = await _service.ClearProjectAsync(project.Id);

        Assert.Equal(2, result.Groups);
        Assert.Empty(_events.Groups(project.Id));
        Assert.NotNull(_projects.Get(project.Id));
        Assert.Equal(77, _positions.Get(source.Id)?.Offset);
    }

    [Fact]
    public async Task Removing_a_project_removes_it_with_its_events()
    {
        var project = _projects.Create("Billing");
        _projects.AddSource(project.Id, SourceKind.File, _temp.File("a.log"), null);
        Record(project, "a");

        var result = await _service.RemoveProjectAsync(project.Id);

        Assert.Equal(1, result.Projects);
        Assert.Equal(1, result.Groups);
        Assert.Null(_projects.Get(project.Id));
        Assert.Empty(_events.Groups(project.Id));
        Assert.Empty((await _service.OverviewAsync()).Projects);
    }

    [Fact]
    public async Task Leftovers_are_removed_with_the_backups_and_a_locked_backup_is_reported_and_kept()
    {
        _storage.NextLeftovers = new LeftoverUsage(1, 1, 2, 5, 3);
        _folder.Backups.Add(new DataFile("moth-eidol.db.corrupt-1", "/data/moth-eidol.db.corrupt-1", 100));
        _folder.Backups.Add(new DataFile("moth-eidol.db.corrupt-2", "/data/moth-eidol.db.corrupt-2", 200));
        _folder.Locked.Add("moth-eidol.db.corrupt-2");
        Assert.True((await _service.OverviewAsync()).HasLeftovers);

        var result = await _service.RemoveLeftoversAsync();

        Assert.Equal(12, result.Rows);
        Assert.Equal(1, result.Files);
        Assert.Equal(100, result.FreedBytes);
        Assert.Contains("moth-eidol.db.corrupt-2", Assert.Single(result.Failures));
        Assert.Equal("moth-eidol.db.corrupt-2", Assert.Single(_folder.Backups).Name);
        Assert.True(_storage.Compactions >= 1);
    }

    [Fact]
    public async Task Without_leftovers_nothing_is_reported()
    {
        var overview = await _service.OverviewAsync();

        Assert.False(overview.HasLeftovers);
        Assert.Equal("Nothing was removed. 0 B freed on the disk.", (await _service.CompactAsync()).Summary);
    }

    [Fact]
    public void The_summary_lists_what_was_removed()
    {
        var result = new AppStorageResult(1, 3, 1, 0, 2, 2048, []);

        Assert.Equal("Removed 1 project, 3 event groups, 1 occurrence and 2 backup files. 2 KB freed on the disk.", result.Summary);
    }
}
