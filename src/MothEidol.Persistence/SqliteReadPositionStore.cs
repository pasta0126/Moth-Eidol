using MothEidol.Core.Logs;

namespace MothEidol.Persistence;

public sealed class SqliteReadPositionStore(SqliteDatabase database) : IReadPositionStore
{
    public FileCursor? Get(long sourceId)
    {
        using var connection = database.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT file_path, "offset", fingerprint, encoding FROM read_positions WHERE source_id = $id;
            """;
        command.Parameters.AddWithValue("$id", sourceId);

        using var reader = command.ExecuteReader();
        if (!reader.Read()) return null;

        return new FileCursor(
            reader.GetString(0),
            reader.GetInt64(1),
            reader.IsDBNull(2) ? null : reader.GetString(2),
            Enum.TryParse<LineEncoding>(reader.GetString(3), out var encoding) ? encoding : LineEncoding.Utf8);
    }

    public void Save(long sourceId, FileCursor cursor)
    {
        using var connection = database.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO read_positions (source_id, file_path, "offset", fingerprint, encoding)
            VALUES ($id, $path, $offset, $fingerprint, $encoding)
            ON CONFLICT(source_id) DO UPDATE SET
                file_path = excluded.file_path,
                "offset" = excluded."offset",
                fingerprint = excluded.fingerprint,
                encoding = excluded.encoding;
            """;
        command.Parameters.AddWithValue("$id", sourceId);
        command.Parameters.AddWithValue("$path", cursor.Path);
        command.Parameters.AddWithValue("$offset", cursor.Offset);
        command.Parameters.AddWithValue("$fingerprint", (object?)cursor.Fingerprint ?? DBNull.Value);
        command.Parameters.AddWithValue("$encoding", cursor.Encoding.ToString());
        command.ExecuteNonQuery();
    }

    public void Delete(long sourceId)
    {
        using var connection = database.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM read_positions WHERE source_id = $id;";
        command.Parameters.AddWithValue("$id", sourceId);
        command.ExecuteNonQuery();
    }
}
