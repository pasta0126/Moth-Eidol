using MothEidol.Core.Projects;

namespace MothEidol.Persistence;

public sealed class SqliteProjectRepository(SqliteDatabase database) : IProjectRepository
{
    public IReadOnlyList<Project> List()
    {
        using var connection = database.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, name, enabled, warning_alerts, frequency_mode, frequency_minutes
            FROM projects ORDER BY id;
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
                    (int)reader.GetInt64(5))));
        }
        return projects;
    }

    public Project Add(string name)
    {
        using var connection = database.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO projects (name) VALUES ($name);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$name", name);
        var id = Convert.ToInt64(command.ExecuteScalar());
        return new Project(id, name, Enabled: true, AlertSettings.Default);
    }

    public void Update(Project project)
    {
        using var connection = database.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE projects
               SET name = $name, enabled = $enabled, warning_alerts = $warnings,
                   frequency_mode = $mode, frequency_minutes = $minutes
             WHERE id = $id;
            """;
        command.Parameters.AddWithValue("$id", project.Id);
        command.Parameters.AddWithValue("$name", project.Name);
        command.Parameters.AddWithValue("$enabled", project.Enabled ? 1 : 0);
        command.Parameters.AddWithValue("$warnings", project.Alerts.WarningAlerts ? 1 : 0);
        command.Parameters.AddWithValue("$mode", project.Alerts.Mode.ToString());
        command.Parameters.AddWithValue("$minutes", project.Alerts.Minutes);
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
}
