using Microsoft.Data.Sqlite;
using MothEidol.Core.Logs;
using MothEidol.Core.Projects;
using MothEidol.Persistence;

namespace MothEidol.Persistence.Tests;

public sealed class ProjectPersistenceTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "moth-eidol-tests-" + Guid.NewGuid().ToString("N"));

    private string DbPath => Path.Combine(_dir, "test.db");

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public void Projects_and_their_alert_settings_survive_a_restart()
    {
        var first = new SqliteProjectRepository(SqliteDatabase.Open(DbPath));
        var billing = first.Add("Billing");
        first.Update(billing with
        {
            Enabled = false,
            Alerts = new AlertSettings(true, FrequencyMode.Summary, 45),
        });
        first.Add("Orders");

        var second = new SqliteProjectRepository(SqliteDatabase.Open(DbPath));
        var projects = second.List();

        Assert.Equal(["Billing", "Orders"], projects.Select(p => p.Name));
        Assert.False(projects[0].Enabled);
        Assert.Equal(new AlertSettings(true, FrequencyMode.Summary, 45), projects[0].Alerts);
        Assert.True(projects[1].Enabled);
        Assert.Equal(AlertSettings.Default, projects[1].Alerts);
    }

    [Fact]
    public void Accent_and_order_survive_a_restart()
    {
        var first = new SqliteProjectRepository(SqliteDatabase.Open(DbPath));
        var a = first.Add("Alpha");
        var b = first.Add("Beta");
        var c = first.Add("Gamma");
        first.Update(b with { AccentColor = "#3FA7D6" });
        first.Reorder([c.Id, a.Id, b.Id]);

        var projects = new SqliteProjectRepository(SqliteDatabase.Open(DbPath)).List();

        Assert.Equal(["Gamma", "Alpha", "Beta"], projects.Select(p => p.Name));
        Assert.Equal("#3FA7D6", projects[2].AccentColor);
        Assert.Null(projects[0].AccentColor);
    }

    [Fact]
    public void New_projects_go_last_and_removal_keeps_the_order()
    {
        var repository = new SqliteProjectRepository(SqliteDatabase.Open(DbPath));
        var a = repository.Add("Alpha");
        var b = repository.Add("Beta");
        repository.Reorder([b.Id, a.Id]);

        var c = repository.Add("Gamma");
        repository.Remove(a.Id);
        var d = repository.Add("Delta");

        Assert.Equal([b.Id, c.Id, d.Id], repository.List().Select(p => p.Id));
    }

    [Fact]
    public void Reorder_ignores_unknown_ids_and_keeps_unlisted_projects_after_the_listed_ones()
    {
        var repository = new SqliteProjectRepository(SqliteDatabase.Open(DbPath));
        var a = repository.Add("Alpha");
        var b = repository.Add("Beta");
        var c = repository.Add("Gamma");

        repository.Reorder([c.Id, 999, c.Id]);

        Assert.Equal([c.Id, a.Id, b.Id], repository.List().Select(p => p.Id));
    }

    [Fact]
    public void Existing_projects_are_ordered_by_name_when_the_position_column_is_added()
    {
        var beforeOrder = Migrations.All.Where(m => m.Version < 6).ToList();
        var old = SqliteDatabase.Open(DbPath, beforeOrder);
        using (var connection = old.CreateConnection())
        {
            using var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO projects (name) VALUES ('orders'), ('Billing'), ('accounts');";
            command.ExecuteNonQuery();
        }

        var projects = new SqliteProjectRepository(SqliteDatabase.Open(DbPath)).List();

        Assert.Equal(["accounts", "Billing", "orders"], projects.Select(p => p.Name));
        Assert.Equal([1, 2, 3], projects.Select(p => p.Position));
        Assert.All(projects, p => Assert.Null(p.AccentColor));
    }

    [Fact]
    public void Project_names_are_unique_ignoring_case()
    {
        var repository = new SqliteProjectRepository(SqliteDatabase.Open(DbPath));
        repository.Add("Billing");

        Assert.Throws<SqliteException>(() => repository.Add("BILLING"));
    }

    [Fact]
    public void Sources_keep_their_project()
    {
        var database = SqliteDatabase.Open(DbPath);
        var projects = new SqliteProjectRepository(database);
        var sources = new SqliteLogSourceRepository(database);
        var billing = projects.Add("Billing");
        sources.Add(SourceKind.Folder, "/logs/billing", "Billing*.log", billing.Id);

        var source = new SqliteLogSourceRepository(SqliteDatabase.Open(DbPath)).List().Single();

        Assert.Equal(billing.Id, source.ProjectId);
        Assert.Equal("Billing*.log", source.Pattern);
    }

    [Fact]
    public void Existing_sources_move_into_an_Unassigned_project_keeping_their_read_positions()
    {
        // A database as an earlier version left it: sources and positions, no projects yet.
        var before = SqliteDatabase.Open(DbPath, Migrations.All.Take(2).ToList());
        using (var connection = before.CreateConnection())
        using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                INSERT INTO log_sources (id, kind, path, pattern, enabled) VALUES
                    (1, 'File', '/logs/a.log', NULL, 1),
                    (2, 'Folder', '/logs/svc', 'Svc*.log', 0);
                """;
            command.ExecuteNonQuery();
        }
        var a = new LogSource(1, SourceKind.File, "/logs/a.log", null, true);
        var b = new LogSource(2, SourceKind.Folder, "/logs/svc", "Svc*.log", false);
        var cursor = new FileCursor("/logs/a.log", 1234, "10:AB", LineEncoding.Utf16Le);
        new SqliteReadPositionStore(before).Save(a.Id, cursor);

        var current = SqliteDatabase.Open(DbPath);

        Assert.Equal(DatabaseStatus.Ready, current.Status);
        var project = Assert.Single(new SqliteProjectRepository(current).List());
        Assert.Equal("Unassigned", project.Name);
        Assert.True(project.Enabled);
        var sources = new SqliteLogSourceRepository(current).List();
        Assert.Equal(2, sources.Count);
        Assert.All(sources, s => Assert.Equal(project.Id, s.ProjectId));
        Assert.False(sources.Single(s => s.Id == b.Id).Enabled);
        Assert.Equal(cursor, new SqliteReadPositionStore(current).Get(a.Id));
    }

    [Fact]
    public void Without_existing_sources_no_Unassigned_project_is_created()
    {
        SqliteDatabase.Open(DbPath, Migrations.All.Take(2).ToList());

        var current = SqliteDatabase.Open(DbPath);

        Assert.Empty(new SqliteProjectRepository(current).List());
    }

    [Fact]
    public async Task Project_created_by_the_wizard_survives_a_restart_with_sources_and_alerts()
    {
        var db = SqliteDatabase.Open(DbPath);
        var projects = new SqliteProjectRepository(db);
        await using (var tailing = new TailingService(new SqliteLogSourceRepository(db), new SqliteReadPositionStore(db), new TailOptions()))
        {
            var service = new ProjectService(projects, tailing);
            var draft = new ProjectDraft { Name = "Billing", Alerts = new AlertSettings(true, FrequencyMode.Summary, 30) };
            draft.AddSource(new SourceDraft(SourceKind.Folder, _dir, "*.log"));
            await service.CreateWithSourcesAsync(draft);
        }

        var restarted = SqliteDatabase.Open(DbPath);
        var project = Assert.Single(new SqliteProjectRepository(restarted).List());
        var source = Assert.Single(new SqliteLogSourceRepository(restarted).List());

        Assert.Equal(new AlertSettings(true, FrequencyMode.Summary, 30), project.Alerts);
        Assert.Equal(project.Id, source.ProjectId);
        Assert.Equal("*.log", source.Pattern);
    }
}
