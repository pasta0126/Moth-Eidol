using Microsoft.Data.Sqlite;
using MothEidol.Core.Projects;

namespace MothEidol.Persistence;

/// <summary>Per-project usage and cleanups of the local database, and returning freed space to the disk.</summary>
public sealed class SqliteProjectStorage(SqliteDatabase database) : IProjectStorage
{
    private const int SqliteBusy = 5, SqliteLocked = 6;

    private const string ProjectGroups = "SELECT id FROM event_groups WHERE project_id = $p";

    public ProjectStorageUsage Usage(long projectId)
    {
        using var connection = database.CreateConnection();
        using var command = connection.CreateCommand();
        // length(CAST(... AS BLOB)) counts bytes, not characters.
        command.CommandText = $"""
            SELECT
                (SELECT COUNT(*) FROM event_groups WHERE project_id = $p),
                (SELECT COUNT(*) FROM event_groups WHERE project_id = $p AND pending_count > 0),
                (SELECT COUNT(*) FROM event_occurrences WHERE group_id IN ({ProjectGroups})),
                (SELECT COUNT(*) FROM event_buckets WHERE project_id = $p),
                (SELECT COALESCE(SUM(length(CAST(title AS BLOB)) + length(CAST(key AS BLOB))
                                     + COALESCE(length(CAST(location AS BLOB)), 0)), 0)
                   FROM event_groups WHERE project_id = $p),
                (SELECT COALESCE(SUM(length(CAST(message AS BLOB)) + COALESCE(length(CAST(context AS BLOB)), 0)), 0)
                   FROM event_occurrences WHERE group_id IN ({ProjectGroups})),
                (SELECT MIN(at) FROM event_occurrences WHERE group_id IN ({ProjectGroups})),
                (SELECT MAX(at) FROM event_occurrences WHERE group_id IN ({ProjectGroups}));
            """;
        command.Parameters.AddWithValue("$p", projectId);

        using var reader = command.ExecuteReader();
        reader.Read();
        var groups = reader.GetInt64(0);
        var occurrences = reader.GetInt64(2);
        var buckets = reader.GetInt64(3);
        var bytes = reader.GetInt64(4) + reader.GetInt64(5)
                    + groups * ProjectStorageUsage.GroupOverhead
                    + occurrences * ProjectStorageUsage.OccurrenceOverhead
                    + buckets * ProjectStorageUsage.BucketOverhead;
        return new ProjectStorageUsage(
            groups, reader.GetInt64(1), occurrences, buckets, bytes,
            reader.IsDBNull(6) ? null : DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(6)),
            reader.IsDBNull(7) ? null : DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(7)));
    }

    /// <summary>One pass per table grouped by project, instead of measuring each project on its own.</summary>
    public IReadOnlyDictionary<long, ProjectStorageUsage> UsageOfAll()
    {
        var groups = new Dictionary<long, (long Count, long Pending, long Bytes)>();
        var occurrences = new Dictionary<long, (long Count, long Bytes, long Oldest, long Newest)>();
        var buckets = new Dictionary<long, long>();

        using var connection = database.CreateConnection();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT project_id, COUNT(*), SUM(pending_count > 0),
                       COALESCE(SUM(length(CAST(title AS BLOB)) + length(CAST(key AS BLOB))
                                    + COALESCE(length(CAST(location AS BLOB)), 0)), 0)
                  FROM event_groups GROUP BY project_id;
                """;
            using var reader = command.ExecuteReader();
            while (reader.Read()) groups[reader.GetInt64(0)] = (reader.GetInt64(1), reader.GetInt64(2), reader.GetInt64(3));
        }
        using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT g.project_id, COUNT(*),
                       COALESCE(SUM(length(CAST(o.message AS BLOB)) + COALESCE(length(CAST(o.context AS BLOB)), 0)), 0),
                       MIN(o.at), MAX(o.at)
                  FROM event_occurrences o JOIN event_groups g ON g.id = o.group_id
                 GROUP BY g.project_id;
                """;
            using var reader = command.ExecuteReader();
            while (reader.Read())
                occurrences[reader.GetInt64(0)] = (reader.GetInt64(1), reader.GetInt64(2), reader.GetInt64(3), reader.GetInt64(4));
        }
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT project_id, COUNT(*) FROM event_buckets GROUP BY project_id;";
            using var reader = command.ExecuteReader();
            while (reader.Read()) buckets[reader.GetInt64(0)] = reader.GetInt64(1);
        }

        var result = new Dictionary<long, ProjectStorageUsage>();
        foreach (var id in groups.Keys.Union(occurrences.Keys).Union(buckets.Keys))
        {
            var g = groups.GetValueOrDefault(id);
            var hasOccurrences = occurrences.TryGetValue(id, out var o);
            var b = buckets.GetValueOrDefault(id);
            var bytes = g.Bytes + o.Bytes
                        + g.Count * ProjectStorageUsage.GroupOverhead
                        + o.Count * ProjectStorageUsage.OccurrenceOverhead
                        + b * ProjectStorageUsage.BucketOverhead;
            result[id] = new ProjectStorageUsage(
                g.Count, g.Pending, o.Count, b, bytes,
                hasOccurrences ? DateTimeOffset.FromUnixTimeMilliseconds(o.Oldest) : null,
                hasOccurrences ? DateTimeOffset.FromUnixTimeMilliseconds(o.Newest) : null);
        }
        return result;
    }

    public long ReclaimableBytes()
    {
        using var connection = database.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA freelist_count;";
        var pages = Convert.ToInt64(command.ExecuteScalar());
        command.CommandText = "PRAGMA page_size;";
        return pages * Convert.ToInt64(command.ExecuteScalar());
    }

    public LeftoverUsage Leftovers()
    {
        using var connection = database.CreateConnection();
        using var command = connection.CreateCommand();
        // The same conditions as RemoveOrphans; a row counted once even if its parent is an orphan too.
        command.CommandText = """
            SELECT
                (SELECT COUNT(*) FROM log_sources WHERE project_id NOT IN (SELECT id FROM projects)),
                (SELECT COUNT(*) FROM read_positions WHERE source_id NOT IN
                    (SELECT id FROM log_sources WHERE project_id IN (SELECT id FROM projects))),
                (SELECT COUNT(*) FROM event_groups WHERE project_id NOT IN (SELECT id FROM projects)),
                (SELECT COUNT(*) FROM event_occurrences WHERE group_id NOT IN
                    (SELECT id FROM event_groups WHERE project_id IN (SELECT id FROM projects))),
                (SELECT COUNT(*) FROM event_buckets WHERE group_id NOT IN
                    (SELECT id FROM event_groups WHERE project_id IN (SELECT id FROM projects)));
            """;
        using var reader = command.ExecuteReader();
        reader.Read();
        return new LeftoverUsage(reader.GetInt64(0), reader.GetInt64(1), reader.GetInt64(2), reader.GetInt64(3), reader.GetInt64(4));
    }

    public DatabaseFileUsage File()
    {
        static long SizeOf(string path) => System.IO.File.Exists(path) ? new FileInfo(path).Length : 0;
        return new DatabaseFileUsage(SizeOf(database.Path), SizeOf(database.Path + "-wal"));
    }

    public CleanupResult ClearAll(long projectId)
    {
        using var connection = database.CreateConnection();
        using var transaction = connection.BeginTransaction();
        var occurrences = Execute(connection, transaction,
            $"DELETE FROM event_occurrences WHERE group_id IN ({ProjectGroups});", ("$p", projectId));
        Execute(connection, transaction, "DELETE FROM event_buckets WHERE project_id = $p;", ("$p", projectId));
        var groups = Execute(connection, transaction, "DELETE FROM event_groups WHERE project_id = $p;", ("$p", projectId));
        transaction.Commit();
        return new CleanupResult(groups, occurrences);
    }

    public CleanupResult KeepLastDays(long projectId, DateTimeOffset cutoff)
    {
        var ms = cutoff.ToUnixTimeMilliseconds();
        var hour = cutoff.ToUnixTimeSeconds() / 3600;
        const string Stale = "SELECT id FROM event_groups WHERE project_id = $p AND last_seen < $cutoff";

        using var connection = database.CreateConnection();
        using var transaction = connection.BeginTransaction();
        // Groups with no activity since the cutoff go entirely, pending ones included.
        var occurrences = Execute(connection, transaction,
            $"DELETE FROM event_occurrences WHERE group_id IN ({Stale});", ("$p", projectId), ("$cutoff", ms));
        Execute(connection, transaction,
            $"DELETE FROM event_buckets WHERE group_id IN ({Stale});", ("$p", projectId), ("$cutoff", ms));
        var groups = Execute(connection, transaction,
            "DELETE FROM event_groups WHERE project_id = $p AND last_seen < $cutoff;", ("$p", projectId), ("$cutoff", ms));
        // Kept groups lose only what is older than the cutoff; their totals and pending state stay.
        occurrences += Execute(connection, transaction,
            $"DELETE FROM event_occurrences WHERE group_id IN ({ProjectGroups}) AND at < $cutoff;",
            ("$p", projectId), ("$cutoff", ms));
        Execute(connection, transaction, "DELETE FROM event_buckets WHERE project_id = $p AND hour < $hour;",
            ("$p", projectId), ("$hour", hour));
        transaction.Commit();
        return new CleanupResult(groups, occurrences);
    }

    /// <summary>Rewrites the file without its free pages, then truncates the WAL. Retried once if the file is busy.</summary>
    public void Compact()
    {
        try
        {
            CompactOnce();
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode is SqliteBusy or SqliteLocked)
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(500));
            CompactOnce();
        }
    }

    private void CompactOnce()
    {
        using var connection = database.CreateConnection();
        Execute(connection, null, "VACUUM;");
        Execute(connection, null, "PRAGMA wal_checkpoint(TRUNCATE);");
    }

    public void Checkpoint()
    {
        using var connection = database.CreateConnection();
        Execute(connection, null, "PRAGMA optimize;"); // First: it may write statistics to the WAL.
        Execute(connection, null, "PRAGMA wal_checkpoint(TRUNCATE);");
    }

    public int RemoveOrphans()
    {
        using var connection = database.CreateConnection();
        using var transaction = connection.BeginTransaction();
        var removed = 0;
        foreach (var sql in new[]
                 {
                     "DELETE FROM log_sources WHERE project_id NOT IN (SELECT id FROM projects);",
                     "DELETE FROM read_positions WHERE source_id NOT IN (SELECT id FROM log_sources);",
                     "DELETE FROM event_groups WHERE project_id NOT IN (SELECT id FROM projects);",
                     "DELETE FROM event_occurrences WHERE group_id NOT IN (SELECT id FROM event_groups);",
                     "DELETE FROM event_buckets WHERE group_id NOT IN (SELECT id FROM event_groups);",
                 })
        {
            removed += Execute(connection, transaction, sql);
        }
        transaction.Commit();
        return removed;
    }

    private static int Execute(
        SqliteConnection connection, SqliteTransaction? transaction, string sql, params (string Name, object Value)[] parameters)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value);
        return command.ExecuteNonQuery();
    }
}
