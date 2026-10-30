using System.Text.Json;
using Microsoft.Data.Sqlite;
using MothEidol.Core.Alerts;
using MothEidol.Core.Logs;

namespace MothEidol.Persistence;

/// <summary>Event groups and their recent occurrences, times stored as UTC milliseconds.</summary>
public sealed class SqliteEventStore(SqliteDatabase database) : IEventStore
{
    private const string GroupColumns =
        "id, project_id, level, title, count, first_seen, last_seen, seen_at, pending_count, location";

    public EventGroup Record(
        long projectId, EntryLevel level, string key, string title,
        long sourceId, DateTimeOffset at, string message, int maxOccurrences,
        string? location = null, IReadOnlyList<ContextEntry>? context = null)
    {
        using var connection = database.CreateConnection();
        using var transaction = connection.BeginTransaction();

        var ms = at.ToUnixTimeMilliseconds();
        long groupId;
        using (var upsert = connection.CreateCommand())
        {
            upsert.Transaction = transaction;
            upsert.CommandText = """
                INSERT INTO event_groups (project_id, level, key, title, location, count, first_seen, last_seen, pending_count)
                VALUES ($project, $level, $key, $title, $location, 1, $at, $at, 1)
                ON CONFLICT (project_id, key) DO UPDATE SET
                    title         = excluded.title,
                    location      = excluded.location,
                    count         = count + 1,
                    pending_count = pending_count + 1,
                    first_seen    = MIN(first_seen, excluded.first_seen),
                    last_seen     = MAX(last_seen, excluded.last_seen)
                RETURNING id;
                """;
            upsert.Parameters.AddWithValue("$project", projectId);
            upsert.Parameters.AddWithValue("$level", (int)level);
            upsert.Parameters.AddWithValue("$key", key);
            upsert.Parameters.AddWithValue("$title", title);
            upsert.Parameters.AddWithValue("$location", (object?)location ?? DBNull.Value);
            upsert.Parameters.AddWithValue("$at", ms);
            groupId = Convert.ToInt64(upsert.ExecuteScalar());
        }

        Execute(connection, transaction,
            "INSERT INTO event_occurrences (group_id, source_id, at, message, context) VALUES ($g, $s, $at, $m, $c);",
            ("$g", groupId), ("$s", sourceId), ("$at", ms), ("$m", message),
            ("$c", context is { Count: > 0 } ? JsonSerializer.Serialize(context) : DBNull.Value));
        Execute(connection, transaction, """
            INSERT INTO event_buckets (group_id, project_id, hour, count) VALUES ($g, $p, $hour, 1)
            ON CONFLICT (group_id, hour) DO UPDATE SET count = count + 1;
            """, ("$g", groupId), ("$p", projectId), ("$hour", at.ToUnixTimeSeconds() / 3600));
        Execute(connection, transaction, """
            DELETE FROM event_occurrences
             WHERE group_id = $g
               AND id NOT IN (SELECT id FROM event_occurrences WHERE group_id = $g ORDER BY id DESC LIMIT $max);
            """, ("$g", groupId), ("$max", maxOccurrences));

        transaction.Commit();
        return Group(connection, groupId)!;
    }

    public IReadOnlyList<EventGroup> Groups(long projectId)
    {
        using var connection = database.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT {GroupColumns} FROM event_groups WHERE project_id = $p ORDER BY last_seen DESC;";
        command.Parameters.AddWithValue("$p", projectId);

        var groups = new List<EventGroup>();
        using var reader = command.ExecuteReader();
        while (reader.Read()) groups.Add(ReadGroup(reader));
        return groups;
    }

    public IReadOnlyList<EventOccurrence> Occurrences(long groupId)
    {
        using var connection = database.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, group_id, source_id, at, message, context FROM event_occurrences WHERE group_id = $g ORDER BY id DESC;";
        command.Parameters.AddWithValue("$g", groupId);

        var occurrences = new List<EventOccurrence>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            occurrences.Add(new EventOccurrence(
                reader.GetInt64(0), reader.GetInt64(1), reader.GetInt64(2),
                DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(3)), reader.GetString(4),
                reader.IsDBNull(5) ? null : JsonSerializer.Deserialize<List<ContextEntry>>(reader.GetString(5))));
        }
        return occurrences;
    }

    public void MarkSeen(long groupId, DateTimeOffset at)
    {
        using var connection = database.CreateConnection();
        Execute(connection, null, "UPDATE event_groups SET pending_count = 0, seen_at = $at WHERE id = $g;",
            ("$at", at.ToUnixTimeMilliseconds()), ("$g", groupId));
    }

    public void MarkAllSeen(long projectId, DateTimeOffset at)
    {
        using var connection = database.CreateConnection();
        Execute(connection, null, "UPDATE event_groups SET pending_count = 0, seen_at = $at WHERE project_id = $p;",
            ("$at", at.ToUnixTimeMilliseconds()), ("$p", projectId));
    }

    public PendingSummary Pending(long projectId)
    {
        using var connection = database.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COALESCE(SUM(pending_count), 0), MAX(level)
              FROM event_groups WHERE project_id = $p AND pending_count > 0;
            """;
        command.Parameters.AddWithValue("$p", projectId);

        using var reader = command.ExecuteReader();
        reader.Read();
        var occurrences = reader.GetInt64(0);
        return occurrences == 0 ? PendingSummary.None : new PendingSummary(occurrences, (EntryLevel)reader.GetInt32(1));
    }

    public int PurgeQuiet(DateTimeOffset lastSeenBefore)
    {
        using var connection = database.CreateConnection();
        using var transaction = connection.BeginTransaction();
        var cutoff = lastSeenBefore.ToUnixTimeMilliseconds();

        Execute(connection, transaction, """
            DELETE FROM event_occurrences WHERE group_id IN
                (SELECT id FROM event_groups WHERE pending_count = 0 AND last_seen < $cutoff);
            """, ("$cutoff", cutoff));
        Execute(connection, transaction, """
            DELETE FROM event_buckets WHERE group_id IN
                (SELECT id FROM event_groups WHERE pending_count = 0 AND last_seen < $cutoff);
            """, ("$cutoff", cutoff));
        var removed = Execute(connection, transaction,
            "DELETE FROM event_groups WHERE pending_count = 0 AND last_seen < $cutoff;", ("$cutoff", cutoff));

        transaction.Commit();
        return removed;
    }

    public void RemoveProject(long projectId)
    {
        using var connection = database.CreateConnection();
        using var transaction = connection.BeginTransaction();
        Execute(connection, transaction,
            "DELETE FROM event_occurrences WHERE group_id IN (SELECT id FROM event_groups WHERE project_id = $p);", ("$p", projectId));
        Execute(connection, transaction, "DELETE FROM event_buckets WHERE project_id = $p;", ("$p", projectId));
        Execute(connection, transaction, "DELETE FROM event_groups WHERE project_id = $p;", ("$p", projectId));
        transaction.Commit();
    }

    private static EventGroup? Group(SqliteConnection connection, long id)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT {GroupColumns} FROM event_groups WHERE id = $id;";
        command.Parameters.AddWithValue("$id", id);
        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadGroup(reader) : null;
    }

    private static EventGroup ReadGroup(SqliteDataReader reader) => new(
        reader.GetInt64(0),
        reader.GetInt64(1),
        (EntryLevel)reader.GetInt32(2),
        reader.GetString(3),
        reader.GetInt64(4),
        DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(5)),
        DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(6)),
        reader.IsDBNull(7) ? null : DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(7)),
        reader.GetInt64(8),
        reader.IsDBNull(9) ? null : reader.GetString(9));

    public IReadOnlyList<int> GroupSeries(long groupId, DateTimeOffset now, int hours) =>
        Series("group_id = $id", groupId, now, hours);

    public IReadOnlyList<int> ProjectSeries(long projectId, DateTimeOffset now, int hours) =>
        Series("project_id = $id", projectId, now, hours);

    public int PruneBuckets(DateTimeOffset olderThan)
    {
        using var connection = database.CreateConnection();
        return Execute(connection, null, "DELETE FROM event_buckets WHERE hour < $hour;",
            ("$hour", olderThan.ToUnixTimeSeconds() / 3600));
    }

    private int[] Series(string where, long id, DateTimeOffset now, int hours)
    {
        var result = new int[hours];
        var current = now.ToUnixTimeSeconds() / 3600;

        using var connection = database.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT hour, SUM(count) FROM event_buckets WHERE {where} AND hour > $from AND hour <= $to GROUP BY hour;";
        command.Parameters.AddWithValue("$id", id);
        command.Parameters.AddWithValue("$from", current - hours);
        command.Parameters.AddWithValue("$to", current);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var index = hours - 1 - (int)(current - reader.GetInt64(0));
            if (index >= 0 && index < hours) result[index] += (int)reader.GetInt64(1);
        }
        return result;
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
