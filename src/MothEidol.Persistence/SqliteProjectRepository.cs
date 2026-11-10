using MothEidol.Core.Projects;

namespace MothEidol.Persistence;

public sealed class SqliteProjectRepository(SqliteDatabase database) : IProjectRepository
{
    public IReadOnlyList<Project> List()
    {
        using var connection = database.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, name, enabled, warning_alerts, frequency_mode, frequency_minutes, accent_color, position
            FROM projects ORDER BY position, id;
            """;

        var projects = new List<Project>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            projects.Add(new Project(
                reader.GetInt64(0),
                reader.GetString(1),
                reader.GetInt64(2) != 0,
                new AlertSettings(
                    reader.GetInt64(3) != 0,
                    Enum.TryParse<FrequencyMode>(reader.GetString(4), out var mode) ? mode : FrequencyMode.Immediate,
                    (int)reader.GetInt64(5)),
                reader.IsDBNull(6) ? null : reader.GetString(6),
                (int)reader.GetInt64(7)));
        }
        return projects;
    }

    public Project Add(string name)
    {
        using var connection = database.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO projects (name, position)
            VALUES ($name, (SELECT COALESCE(MAX(position), 0) + 1 FROM projects));
            SELECT last_insert_rowid(), (SELECT position FROM projects WHERE id = last_insert_rowid());
            """;
        command.Parameters.AddWithValue("$name", name);
        using var reader = command.ExecuteReader();
        reader.Read();
        return new Project(reader.GetInt64(0), name, Enabled: true, AlertSettings.Default, Position: (int)reader.GetInt64(1));
    }

    public void Update(Project project)
    {
        using var connection = database.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE projects
               SET name = $name, enabled = $enabled, warning_alerts = $warnings,
                   frequency_mode = $mode, frequency_minutes = $minutes, accent_color = $accent
             WHERE id = $id;
            """;
        command.Parameters.AddWithValue("$id", project.Id);
        command.Parameters.AddWithValue("$name", project.Name);
        command.Parameters.AddWithValue("$enabled", project.Enabled ? 1 : 0);
        command.Parameters.AddWithValue("$warnings", project.Alerts.WarningAlerts ? 1 : 0);
        command.Parameters.AddWithValue("$mode", project.Alerts.Mode.ToString());
        command.Parameters.AddWithValue("$minutes", project.Alerts.Minutes);
        command.Parameters.AddWithValue("$accent", (object?)project.AccentColor ?? DBNull.Value);
        command.ExecuteNonQuery();
    }

    public void Remove(long id)
    {
        using var connection = database.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM projects WHERE id = $id;";
        command.Parameters.AddWithValue("$id", id);
        command.ExecuteNonQuery();
    }

    public void Reorder(IReadOnlyList<long> orderedIds)
    {
        var current = List().Select(p => p.Id).ToList();
        var order = orderedIds.Distinct().Where(current.Contains)
            .Concat(current.Where(id => !orderedIds.Contains(id)))
            .ToList();

        using var connection = database.CreateConnection();
        using var transaction = connection.BeginTransaction();
        for (var i = 0; i < order.Count; i++)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "UPDATE projects SET position = $position WHERE id = $id;";
            command.Parameters.AddWithValue("$position", i + 1);
            command.Parameters.AddWithValue("$id", order[i]);
            command.ExecuteNonQuery();
        }
        transaction.Commit();
    }
}
