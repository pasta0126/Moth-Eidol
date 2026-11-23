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

    /// <summary>Inserted between the database file name and a timestamp to name the copy of a corrupt file.</summary>
    public const string CorruptBackupMarker = ".corrupt-";
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
        // Per connection: with WAL this stays consistent on power loss and avoids one fsync per write.
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA synchronous = NORMAL;";
        command.ExecuteNonQuery();
        return connection;
    }

    /// <summary>Closes the pooled connections, so the file is released and the WAL is checkpointed.</summary>
    public static void ReleaseConnections() => SqliteConnection.ClearAllPools();

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
            ReleaseConnections(); // A pooled connection would keep the corrupt file open.
            var backup = BackupCorruptFile(path);
            return OpenCore(path, migrations, DatabaseStatus.RecoveredFromCorruption, backup);
        }
    }

    private static SqliteDatabase OpenCore(
        string path, IReadOnlyList<Migration> migrations, DatabaseStatus status, string? backupPath)
    {
        var target = migrations.Count == 0 ? 0 : migrations[^1].Version;

        // Not pooled: once opened and migrated the file is closed for real (WAL checkpointed, nothing held open).
        using var connection = new SqliteConnection(ConnectionString(path, pooling: false));
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
        var backup = $"{path}{CorruptBackupMarker}{stamp}";
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

    // Pooled connections avoid reopening the file (and checkpointing the WAL) on every call; the timeout makes
    // concurrent writers wait for each other instead of failing with SQLITE_BUSY.
    private static string ConnectionString(string path, bool pooling = true) =>
        new SqliteConnectionStringBuilder { DataSource = path, Pooling = pooling, DefaultTimeout = 5 }.ToString();

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
