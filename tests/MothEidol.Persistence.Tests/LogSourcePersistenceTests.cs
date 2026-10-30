using Microsoft.Data.Sqlite;
using MothEidol.Core.Logs;
using MothEidol.Persistence;

namespace MothEidol.Persistence.Tests;

public sealed class LogSourcePersistenceTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "moth-eidol-tests-" + Guid.NewGuid().ToString("N"));

    private string DbPath => Path.Combine(_dir, "test.db");

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public void Sources_and_their_enabled_state_survive_a_restart()
    {
        var first = new SqliteLogSourceRepository(SqliteDatabase.Open(DbPath));
        var file = first.Add(SourceKind.File, "/var/log/app.log", null);
        var folder = first.Add(SourceKind.Folder, "/var/log/svc", "Svc*.log");
        first.SetEnabled(folder.Id, false);

        var second = new SqliteLogSourceRepository(SqliteDatabase.Open(DbPath));
        var sources = second.List();

        Assert.Equal([file, folder with { Enabled = false }], sources);
    }

    [Fact]
    public void Removing_a_source_also_removes_its_read_position()
    {
        var database = SqliteDatabase.Open(DbPath);
        var sources = new SqliteLogSourceRepository(database);
        var positions = new SqliteReadPositionStore(database);
        var source = sources.Add(SourceKind.File, "/var/log/app.log", null);
        positions.Save(source.Id, new FileCursor("/var/log/app.log", 10, "5:AB", LineEncoding.Utf8));

        sources.Remove(source.Id);

        Assert.Empty(sources.List());
        Assert.Null(positions.Get(source.Id));
    }

    [Fact]
    public void Read_positions_round_trip_including_encoding_and_a_missing_fingerprint()
    {
        var database = SqliteDatabase.Open(DbPath);
        var positions = new SqliteReadPositionStore(database);

        positions.Save(1, new FileCursor("/a/b.log", 1234, "128:ABCDEF", LineEncoding.Utf16Le));
        positions.Save(2, new FileCursor("/c/d.log", 0, null, LineEncoding.Utf8));
        positions.Save(1, new FileCursor("/a/b.log", 2000, "128:ABCDEF", LineEncoding.Utf16Le));

        var reopened = new SqliteReadPositionStore(SqliteDatabase.Open(DbPath));
        Assert.Equal(new FileCursor("/a/b.log", 2000, "128:ABCDEF", LineEncoding.Utf16Le), reopened.Get(1));
        Assert.Equal(new FileCursor("/c/d.log", 0, null, LineEncoding.Utf8), reopened.Get(2));
        Assert.Null(reopened.Get(3));
    }

    [Fact]
    public void A_version_one_database_is_migrated_keeping_its_settings()
    {
        var v1 = SqliteDatabase.Open(DbPath, Migrations.All.Take(1).ToList());
        new SqliteSettingsRepository(v1).Set("keep", "me");

        var current = SqliteDatabase.Open(DbPath);

        Assert.Equal(DatabaseStatus.Ready, current.Status);
        Assert.Equal("me", new SqliteSettingsRepository(current).Get("keep"));
        Assert.Empty(new SqliteLogSourceRepository(current).List());
    }
}
