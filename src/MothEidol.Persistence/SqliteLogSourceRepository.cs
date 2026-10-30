using MothEidol.Core.Logs;

namespace MothEidol.Persistence;

public sealed class SqliteLogSourceRepository(SqliteDatabase database) : ILogSourceRepository
{
    public IReadOnlyList<LogSource> List()
    {
        using var connection = database.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, kind, path, pattern, enabled FROM log_sources ORDER BY id;";

        var sources = new List<LogSource>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            sources.Add(new LogSource(
                reader.GetInt64(0),
                Enum.Parse<SourceKind>(reader.GetString(1)),
                reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.GetInt64(4) != 0));
        }
        return sources;
    }

    public LogSource Add(SourceKind kind, string path, string? pattern)
    {
        using var connection = database.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO log_sources (kind, path, pattern, enabled) VALUES ($kind, $path, $pattern, 1);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$kind", kind.ToString());
        command.Parameters.AddWithValue("$path", path);
        command.Parameters.AddWithValue("$pattern", (object?)pattern ?? DBNull.Value);
        var id = Convert.ToInt64(command.ExecuteScalar());
        return new LogSource(id, kind, path, pattern, Enabled: true);
    }

    public void Remove(long id)
    {
        using var connection = database.CreateConnection();
        using var transaction = connection.BeginTransaction();
        Run(connection, transaction, "DELETE FROM read_positions WHERE source_id = $id;", id);
        Run(connection, transaction, "DELETE FROM log_sources WHERE id = $id;", id);
        transaction.Commit();
    }

    public void SetEnabled(long id, bool enabled)
    {
        using var connection = database.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE log_sources SET enabled = $enabled WHERE id = $id;";
        command.Parameters.AddWithValue("$enabled", enabled ? 1 : 0);
        command.Parameters.AddWithValue("$id", id);
        command.ExecuteNonQuery();
    }

    private static void Run(Microsoft.Data.Sqlite.SqliteConnection connection, Microsoft.Data.Sqlite.SqliteTransaction transaction, string sql, long id)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.Parameters.AddWithValue("$id", id);
        command.ExecuteNonQuery();
    }
}
