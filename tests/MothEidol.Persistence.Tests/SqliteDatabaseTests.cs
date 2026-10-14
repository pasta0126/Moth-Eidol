using Microsoft.Data.Sqlite;
using MothEidol.Core.Settings;
using MothEidol.Persistence;

namespace MothEidol.Persistence.Tests;

public sealed class SqliteDatabaseTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "moth-eidol-tests-" + Guid.NewGuid().ToString("N"));

    private string DbPath => Path.Combine(_dir, "test.db");

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    [Fact]
    public void FirstStartup_CreatesDatabaseWithCurrentSchema()
    {
        var db = SqliteDatabase.Open(DbPath);

        Assert.Equal(DatabaseStatus.Ready, db.Status);
        Assert.True(File.Exists(DbPath));
        Assert.Equal(new SqliteSettingsRepository(db).Get("missing"), null);
    }

    [Fact]
    public void Settings_PersistAcrossReopen()
    {
        var first = SqliteDatabase.Open(DbPath);
        new AppSettings(new SqliteSettingsRepository(first)).StartWithSession = true;

        var second = SqliteDatabase.Open(DbPath);

        Assert.True(new AppSettings(new SqliteSettingsRepository(second)).StartWithSession);
    }

    [Fact]
    public void UnsetSetting_ReturnsDefault()
    {
        var db = SqliteDatabase.Open(DbPath);

        Assert.False(new AppSettings(new SqliteSettingsRepository(db)).StartWithSession);
    }

    [Fact]
    public void OlderSchema_IsMigratedPreservingData()
    {
        var v1 = Migrations.All;
        var db1 = SqliteDatabase.Open(DbPath, v1);
        new SqliteSettingsRepository(db1).Set("keep", "me");

        var v2 = v1.Append(new Migration(2, "CREATE TABLE extra (id INTEGER PRIMARY KEY);")).ToList();
        var db2 = SqliteDatabase.Open(DbPath, v2);

        Assert.Equal(DatabaseStatus.Ready, db2.Status);
        Assert.Equal("me", new SqliteSettingsRepository(db2).Get("keep"));
        using var connection = db2.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT count(*) FROM sqlite_master WHERE name = 'extra';";
        Assert.Equal(1L, command.ExecuteScalar());
    }

    [Fact]
    public void NewerSchema_IsLeftUntouched()
    {
        var v2 = Migrations.All.Append(new Migration(2, "CREATE TABLE extra (id INTEGER PRIMARY KEY);")).ToList();
        SqliteDatabase.Open(DbPath, v2);
        var before = File.ReadAllBytes(DbPath);

        var db = SqliteDatabase.Open(DbPath, Migrations.All);

        Assert.Equal(DatabaseStatus.IncompatibleSchema, db.Status);
        Assert.Equal(2, db.FoundVersion);
        Assert.False(db.IsUsable);
        Assert.Equal(before, File.ReadAllBytes(DbPath));
    }

    [Fact]
    public void CorruptFile_IsBackedUpAndRecreated()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(DbPath, new string('x', 4096));

        var db = SqliteDatabase.Open(DbPath);

        Assert.Equal(DatabaseStatus.RecoveredFromCorruption, db.Status);
        Assert.NotNull(db.BackupPath);
        Assert.True(File.Exists(db.BackupPath));
        var settings = new AppSettings(new SqliteSettingsRepository(db));
        settings.StartWithSession = true;
        Assert.True(settings.StartWithSession);
    }
}
