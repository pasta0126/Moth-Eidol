using Microsoft.Data.Sqlite;
using MothEidol.Core.Alerts;
using MothEidol.Core.Logs;
using MothEidol.Core.Projects;
using MothEidol.Persistence;

namespace MothEidol.Persistence.Tests;

public sealed class ProjectStoragePersistenceTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly string _dir = Path.Combine(Path.GetTempPath(), "moth-eidol-tests-" + Guid.NewGuid().ToString("N"));
    private readonly SqliteDatabase _db;
    private readonly SqliteProjectRepository _projects;
    private readonly SqliteLogSourceRepository _sources;
    private readonly SqliteReadPositionStore _positions;
    private readonly SqliteEventStore _events;
    private readonly SqliteProjectStorage _storage;

    public ProjectStoragePersistenceTests()
    {
        _db = SqliteDatabase.Open(Path.Combine(_dir, "test.db"));
        _projects = new SqliteProjectRepository(_db);
        _sources = new SqliteLogSourceRepository(_db);
        _positions = new SqliteReadPositionStore(_db);
        _events = new SqliteEventStore(_db);
        _storage = new SqliteProjectStorage(_db);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    private void Record(long project, string key, DateTimeOffset at, string message = "boom") =>
        _events.Record(project, EntryLevel.Error, key, key, 1, at, message, 50, location: "Billing.cs:10");

    private long Count(string table)
    {
        using var connection = _db.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM {table};";
        return Convert.ToInt64(command.ExecuteScalar());
    }

    private void Run(string sql)
    {
        using var connection = _db.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    [Fact]
    public void Usage_counts_the_project_only_and_estimates_its_size()
    {
        var billing = _projects.Add("Billing");
        var orders = _projects.Add("Orders");
        Record(billing.Id, "a", Now.AddHours(-3), new string('x', 1000));
        Record(billing.Id, "a", Now, new string('y', 1000));
        _events.MarkSeen(_events.Groups(billing.Id)[0].Id, Now);
        Record(billing.Id, "b", Now);
        Record(orders.Id, "c", Now, new string('z', 50_000));

        var usage = _storage.Usage(billing.Id);

        Assert.Equal(2, usage.Groups);
        Assert.Equal(1, usage.PendingGroups);
        Assert.Equal(3, usage.Occurrences);
        Assert.Equal(3, usage.HourlyCounts);
        Assert.InRange(usage.ApproxBytes, 2000, 2400);
        Assert.Equal(Now.AddHours(-3), usage.Oldest);
        Assert.Equal(Now, usage.Newest);
        Assert.True(_storage.File().MainBytes > 0);
    }

    [Fact]
    public void Usage_of_a_project_without_events_is_empty()
    {
        var usage = _storage.Usage(_projects.Add("Billing").Id);

        Assert.True(usage.IsEmpty);
        Assert.Null(usage.Oldest);
        Assert.Null(usage.Newest);
    }

    [Fact]
    public void Clear_all_keeps_the_sources_read_positions_and_other_projects()
    {
        var billing = _projects.Add("Billing");
        var orders = _projects.Add("Orders");
        var source = _sources.Add(SourceKind.File, "/logs/a.log", null, billing.Id);
        _positions.Save(source.Id, new FileCursor("/logs/a.log", 42, null, LineEncoding.Utf8));
        Record(billing.Id, "a", Now);
        Record(billing.Id, "a", Now);
        Record(billing.Id, "b", Now);
        Record(orders.Id, "c", Now);

        var result = _storage.ClearAll(billing.Id);

        Assert.Equal(new CleanupResult(2, 3), result);
        Assert.Empty(_events.Groups(billing.Id));
        Assert.Single(_events.Groups(orders.Id));
        Assert.Single(_sources.List());
        Assert.Equal(42, _positions.Get(source.Id)?.Offset);
        Assert.Equal(1, Count("event_buckets"));
    }

    [Fact]
    public void Keep_last_days_removes_quiet_groups_and_trims_kept_ones_without_touching_their_totals()
    {
        var project = _projects.Add("Billing");
        var other = _projects.Add("Orders");
        Record(project.Id, "old", Now.AddDays(-20));
        Record(project.Id, "recent", Now.AddDays(-10));
        Record(project.Id, "recent", Now);
        Record(other.Id, "other-old", Now.AddDays(-20));

        var result = _storage.KeepLastDays(project.Id, Now.AddDays(-7));

        Assert.Equal(new CleanupResult(1, 2), result);
        var kept = Assert.Single(_events.Groups(project.Id));
        Assert.Equal("recent", kept.Title);
        Assert.Equal(2, kept.Count);
        Assert.Equal(2, kept.PendingCount);
        Assert.Equal(Now.AddDays(-10), kept.FirstSeen);
        Assert.Single(_events.Occurrences(kept.Id));
        Assert.Equal(1, _events.GroupSeries(kept.Id, Now, 24 * 30).Sum());
        Assert.Single(_events.Groups(other.Id));
    }

    [Fact]
    public void Cleanup_and_compaction_shrink_the_file()
    {
        var project = _projects.Add("Billing");
        var text = new string('x', 4000);
        for (var i = 0; i < 1500; i++) Record(project.Id, $"k{i}", Now, text);
        _storage.Checkpoint();
        var before = _storage.File().TotalBytes;

        _storage.ClearAll(project.Id);
        _storage.Compact();

        Assert.True(_storage.File().TotalBytes < before / 4, $"{_storage.File().TotalBytes} vs {before}");
    }

    [Fact]
    public void Checkpoint_truncates_the_write_ahead_log()
    {
        var project = _projects.Add("Billing");
        for (var i = 0; i < 200; i++) Record(project.Id, $"k{i}", Now, new string('x', 2000));

        _storage.Checkpoint();

        Assert.Equal(0, _storage.File().WalBytes);
    }

    [Fact]
    public void Orphans_are_removed_and_existing_projects_keep_their_data()
    {
        var kept = _projects.Add("Billing");
        var source = _sources.Add(SourceKind.File, "/logs/a.log", null, kept.Id);
        _positions.Save(source.Id, new FileCursor("/logs/a.log", 1, null, LineEncoding.Utf8));
        Record(kept.Id, "a", Now);
        // What an interrupted removal of project 999 would leave behind.
        Record(999, "lost", Now);
        var orphanSource = _sources.Add(SourceKind.File, "/logs/b.log", null, 999);
        _positions.Save(orphanSource.Id, new FileCursor("/logs/b.log", 1, null, LineEncoding.Utf8));
        Run("INSERT INTO read_positions (source_id, file_path, \"offset\", encoding) VALUES (12345, 'x', 0, 'Utf8');");

        var removed = _storage.RemoveOrphans();

        Assert.True(removed >= 6, removed.ToString());
        Assert.Empty(_events.Groups(999));
        Assert.Single(_events.Groups(kept.Id));
        Assert.Equal(source.Id, Assert.Single(_sources.List()).Id);
        Assert.Equal(1, Count("read_positions"));
        Assert.Equal(1, Count("event_occurrences"));
        Assert.Equal(1, Count("event_buckets"));
        Assert.Equal(0, _storage.RemoveOrphans());
    }

    [Fact]
    public async Task Removing_a_project_leaves_nothing_of_it_in_any_table()
    {
        await using var tailing = new TailingService(_sources, _positions, new TailOptions());
        var service = new ProjectService(_projects, tailing, _events, _storage);
        var billing = service.Create("Billing");
        var orders = service.Create("Orders");
        var source = service.AddSource(billing.Id, SourceKind.File, Path.Combine(_dir, "a.log"), null);
        service.AddSource(orders.Id, SourceKind.File, Path.Combine(_dir, "b.log"), null);
        _positions.Save(source.Id, new FileCursor("a.log", 7, null, LineEncoding.Utf8));
        Record(billing.Id, "a", Now);
        Record(orders.Id, "b", Now);

        await service.RemoveAsync(billing.Id);

        Assert.Equal(["Orders"], _projects.List().Select(p => p.Name));
        Assert.DoesNotContain(_sources.List(), s => s.ProjectId == billing.Id);
        Assert.Null(_positions.Get(source.Id));
        Assert.Empty(_events.Groups(billing.Id));
        Assert.Single(_events.Groups(orders.Id));
        Assert.Equal(1, Count("event_occurrences"));
        Assert.Equal(1, Count("event_buckets"));

        var again = service.Create("Billing");
        Assert.Empty(_events.Groups(again.Id));
        Assert.Empty(service.SourcesOf(again.Id));
    }

    [Fact]
    public void The_storage_threshold_survives_a_restart_and_defaults_to_100_MB()
    {
        var billing = _projects.Add("Billing");
        _projects.Add("Orders");
        _projects.Update(billing with { StorageWarningMb = 20 });

        var projects = new SqliteProjectRepository(SqliteDatabase.Open(_db.Path)).List();

        Assert.Equal(20, projects[0].StorageWarningMb);
        Assert.Equal(Project.DefaultStorageWarningMb, projects[1].StorageWarningMb);
    }

    [Fact]
    public void Projects_from_before_the_threshold_get_the_default()
    {
        var path = Path.Combine(_dir, "old.db");
        var old = SqliteDatabase.Open(path, [.. Migrations.All.Where(m => m.Version <= 6)]);
        new SqliteProjectRepository(old).Add("Billing");
        SqliteConnection.ClearAllPools();

        var project = Assert.Single(new SqliteProjectRepository(SqliteDatabase.Open(path)).List());

        Assert.Equal(100, project.StorageWarningMb);
    }

    [Fact]
    public async Task Concurrent_writers_do_not_fail_with_busy()
    {
        var project = _projects.Add("Billing");
        var source = _sources.Add(SourceKind.File, "/logs/a.log", null, project.Id);

        var writers = Enumerable.Range(0, 4).Select(w => Task.Run(() =>
        {
            for (var i = 0; i < 150; i++)
            {
                if (w % 2 == 0) Record(project.Id, $"w{w}-{i % 10}", Now);
                else _positions.Save(source.Id, new FileCursor("/logs/a.log", i, null, LineEncoding.Utf8));
            }
        }));

        await Task.WhenAll(writers);

        Assert.Equal(300, _events.Groups(project.Id).Sum(g => g.Count));
    }
}
