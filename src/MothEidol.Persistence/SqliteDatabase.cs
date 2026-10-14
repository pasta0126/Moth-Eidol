using Microsoft.Data.Sqlite;

namespace MothEidol.Persistence;

public enum DatabaseStatus
{
    /// <summary>Opened normally (created or migrated as needed).</summary>
    Ready,

    /// <summary>The previous file was corrupt; it was kept as a backup and a new database created.</summary>
    RecoveredFromCorruption,

    /// <summary>The schema is newer than this build supports; the file was left untouched.</summary>
    IncompatibleSchema,
}

/// <summary>Local SQLite database with versioned schema migrations.</summary>
public sealed class SqliteDatabase
{
    private const int SqliteCorrupt = 11;
    private const int SqliteNotADatabase = 26;

    private SqliteDatabase(string path, DatabaseStatus status, string? backupPath, int? foundVersion)
    {
        Path = path;
        Status = status;
        BackupPath = backupPath;
        FoundVersion = foundVersion;
    }

    public string Path { get; }

    public DatabaseStatus Status { get; }

    /// <summary>Location of the preserved corrupt file when <see cref="Status"/> is RecoveredFromCorruption.</summary>
    public string? BackupPath { get; }

    /// <summary>Schema version found in the file when <see cref="Status"/> is IncompatibleSchema.</summary>
    public int? FoundVersion { get; }

    public bool IsUsable => Status != DatabaseStatus.IncompatibleSchema;

    public SqliteConnection CreateConnection()
    {
        if (!IsUsable)
        {
            throw new InvalidOperationException("The database schema is newer than this version supports.");
        }

        var connection = new SqliteConnection(ConnectionString(Path));
        connection.Open();
        return connection;
    }

    public static SqliteDatabase Open(string path, IReadOnlyList<Migration>? migrations = null)
    {
        migrations ??= Migrations.All;
        var directory = System.IO.Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        try
        {
            return OpenCore(path, migrations, DatabaseStatus.Ready, backupPath: null);
        }
        catch (SqliteException ex) when (IsCorruption(ex))
        {
            var backup = BackupCorruptFile(path);
            return OpenCore(path, migrations, DatabaseStatus.RecoveredFromCorruption, backup);
        }
    }

    private static SqliteDatabase OpenCore(
        string path, IReadOnlyList<Migration> migrations, DatabaseStatus status, string? backupPath)
    {
        var target = migrations.Count == 0 ? 0 : migrations[^1].Version;

        using var connection = new SqliteConnection(ConnectionString(path));
        connection.Open();

        // The first query on a non-database file raises NOTADB, handled by the caller.
        var current = Convert.ToInt32(Scalar(connection, "PRAGMA user_version;"));

        if (current > target)
        {
            return new SqliteDatabase(path, DatabaseStatus.IncompatibleSchema, backupPath, current);
        }

        if (current > 0 || File.Exists(path) && new FileInfo(path).Length > 0)
        {
            var check = Convert.ToString(Scalar(connection, "PRAGMA quick_check;"));
            if (!string.Equals(check, "ok", StringComparison.OrdinalIgnoreCase))
            {
                throw new SqliteException("Database integrity check failed.", SqliteCorrupt);
            }
        }

        Execute(connection, "PRAGMA journal_mode=WAL;");

        foreach (var migration in migrations.Where(m => m.Version > current).OrderBy(m => m.Version))
        {
            using var transaction = connection.BeginTransaction();
            Execute(connection, migration.Sql, transaction);
            Execute(connection, $"PRAGMA user_version = {migration.Version};", transaction);
            transaction.Commit();
        }

        return new SqliteDatabase(path, status, backupPath, foundVersion: null);
    }

    private static string BackupCorruptFile(string path)
    {
        var stamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var backup = $"{path}.corrupt-{stamp}";
        File.Move(path, backup, overwrite: true);

        foreach (var suffix in new[] { "-wal", "-shm" })
        {
            var sidecar = path + suffix;
            if (File.Exists(sidecar))
            {
                File.Delete(sidecar);
            }
        }

        return backup;
    }

    private static bool IsCorruption(SqliteException ex) =>
        ex.SqliteErrorCode is SqliteCorrupt or SqliteNotADatabase;

    private static string ConnectionString(string path) =>
        new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString();

    private static object? Scalar(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return command.ExecuteScalar();
    }

    private static void Execute(SqliteConnection connection, string sql, SqliteTransaction? transaction = null)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}
