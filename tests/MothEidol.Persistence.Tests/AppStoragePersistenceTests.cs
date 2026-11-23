using Microsoft.Data.Sqlite;
using MothEidol.Core.Alerts;
using MothEidol.Core.Logs;
using MothEidol.Core.Projects;
using MothEidol.Persistence;

namespace MothEidol.Persistence.Tests;

public sealed class AppStoragePersistenceTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly string _dir = Path.Combine(Path.GetTempPath(), "moth-eidol-tests-" + Guid.NewGuid().ToString("N"));
    private readonly SqliteDatabase _db;
    private readonly SqliteProjectRepository _projects;
    private readonly SqliteLogSourceRepository _sources;
    private readonly SqliteReadPositionStore _positions;
    private readonly SqliteEventStore _events;
    private readonly SqliteProjectStorage _storage;

    public AppStoragePersistenceTests()
    {
        _db = SqliteDatabase.Open(Path.Combine(_dir, "moth-eidol.db"));
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

    [Fact]
    public void The_usage_of_all_projects_matches_each_project_measured_alone()
    {
        var billing = _projects.Add("Billing");
        var orders = _projects.Add("Orders");
        var empty = _projects.Add("Empty");
        Record(billing.Id, "a", Now.AddHours(-5), new string('x', 500));
        Record(billing.Id, "a", Now);
        Record(billing.Id, "b", Now.AddDays(-1));
        Record(orders.Id, "c", Now, new string('é', 300));

        var all = _storage.UsageOfAll();

        Assert.Equal(_storage.Usage(billing.Id), all[billing.Id]);
        Assert.Equal(_storage.Usage(orders.Id), all[orders.Id]);
        Assert.False(all.ContainsKey(empty.Id));
    }

    [Fact]
    public void Leftovers_count_what_the_orphan_sweep_removes()
    {
        var kept = _projects.Add("Billing");
        var source = _sources.Add(SourceKind.File, "/logs/a.log", null, kept.Id);
        _positions.Save(source.Id, new FileCursor("/logs/a.log", 1, null, LineEncoding.Utf8));
        Record(kept.Id, "a", Now);
        Record(999, "lost", Now);
        Record(999, "lost", Now.AddHours(-2));
        var orphan = _sources.Add(SourceKind.File, "/logs/b.log", null, 999);
        _positions.Save(orphan.Id, new FileCursor("/logs/b.log", 1, null, LineEncoding.Utf8));

        var leftovers = _storage.Leftovers();

        Assert.Equal(new LeftoverUsage(Sources: 1, ReadPositions: 1, Groups: 1, Occurrences: 2, HourlyCounts: 2), leftovers);
        Assert.Equal(leftovers.Total, _storage.RemoveOrphans());
        Assert.Equal(LeftoverUsage.None, _storage.Leftovers());
        Assert.Single(_events.Groups(kept.Id));
    }

    [Fact]
    public void Reclaimable_space_appears_after_a_cleanup_and_goes_after_compacting()
    {
        var project = _projects.Add("Billing");
        for (var i = 0; i < 500; i++) Record(project.Id, $"k{i}", Now, new string('x', 4000));
        _storage.Checkpoint();

        _storage.ClearAll(project.Id);
        _storage.Checkpoint();
        Assert.True(_storage.ReclaimableBytes() > 1024 * 1024, _storage.ReclaimableBytes().ToString());

        _storage.Compact();
        Assert.True(_storage.ReclaimableBytes() < 64 * 1024, _storage.ReclaimableBytes().ToString());
    }

    [Fact]
    public void The_retention_survives_a_restart_and_defaults_to_10_days()
    {
        var billing = _projects.Add("Billing");
        _projects.Add("Orders");
        _projects.Update(billing with { RetentionDays = 3 });

        var projects = new SqliteProjectRepository(SqliteDatabase.Open(_db.Path)).List();

        Assert.Equal(3, projects[0].RetentionDays);
        Assert.Equal(Project.DefaultRetentionDays, projects[1].RetentionDays);
    }

    [Fact]
    public void Projects_from_before_the_retention_get_10_days()
    {
        var path = Path.Combine(_dir, "old.db");
        var old = SqliteDatabase.Open(path, [.. Migrations.All.Where(m => m.Version <= 7)]);
        new SqliteProjectRepository(old).Add("Billing");
        SqliteConnection.ClearAllPools();

        var project = Assert.Single(new SqliteProjectRepository(SqliteDatabase.Open(path)).List());

        Assert.Equal(10, project.RetentionDays);
    }

    [Fact]
    public void Only_corrupt_copies_of_the_database_are_listed_and_deleted()
    {
        _projects.Add("Billing"); // Makes sure the live file and its WAL exist.
        var backup = Path.Combine(_dir, "moth-eidol.db.corrupt-20260301120000");
        File.WriteAllText(backup, new string('x', 1234));
        File.WriteAllText(Path.Combine(_dir, "notes.txt"), "keep");
        File.WriteAllText(Path.Combine(_dir, "other.db.corrupt-20260301120000"), "keep");
        var folder = new SqliteDataFolder(_db.Path);

        var listed = Assert.Single(folder.CorruptBackups());
        Assert.Equal("moth-eidol.db.corrupt-20260301120000", listed.Name);
        Assert.Equal(1234, listed.Bytes);
        Assert.Equal(Path.GetFullPath(_dir), folder.Location);

        folder.Delete(listed);
        Assert.False(File.Exists(backup));
        Assert.Throws<InvalidOperationException>(() => folder.Delete(new DataFile("moth-eidol.db", _db.Path, 0)));
        Assert.Throws<InvalidOperationException>(
            () => folder.Delete(new DataFile("x", Path.Combine(_dir, "notes.txt"), 0)));
        Assert.True(File.Exists(_db.Path));
        Assert.True(File.Exists(Path.Combine(_dir, "notes.txt")));
    }
}
