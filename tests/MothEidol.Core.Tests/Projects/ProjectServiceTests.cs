using MothEidol.Core.Logs;
using MothEidol.Core.Projects;
using MothEidol.Core.Tests.Logs;

namespace MothEidol.Core.Tests.Projects;

public sealed class ProjectServiceTests : IDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private static readonly TailOptions Fast = new()
    {
        PollInterval = TimeSpan.FromMilliseconds(20),
        RetryInterval = TimeSpan.FromMilliseconds(40),
        SaveInterval = TimeSpan.FromMilliseconds(20),
    };

    private readonly TempFolder _temp = new();
    private readonly InMemoryProjectRepository _repository = new();
    private readonly TailingService _tailing;
    private readonly ProjectService _projects;

    public ProjectServiceTests()
    {
        ProjectService? projects = null;
        _tailing = new TailingService(
            new InMemoryLogSourceRepository(), new InMemoryReadPositionStore(), Fast,
            projectEnabled: id => projects?.IsEnabled(id) ?? true);
        _projects = projects = new ProjectService(_repository, _tailing);
    }

    public void Dispose()
    {
        _tailing.DisposeAsync().AsTask().Wait();
        _temp.Dispose();
    }

    [Fact]
    public void New_projects_go_last_and_the_stored_order_is_kept()
    {
        var b = _projects.Create("Billing");
        var a = _projects.Create("Accounts");
        var c = _projects.Create("Catalog");

        Assert.Equal([b.Id, a.Id, c.Id], _projects.Projects.Select(p => p.Id));

        _projects.Reorder([c.Id, b.Id, a.Id]);
        var d = _projects.Create("Delivery");

        Assert.Equal([c.Id, b.Id, a.Id, d.Id], _projects.Projects.Select(p => p.Id));
    }

    [Fact]
    public async Task Removing_a_project_keeps_the_order_of_the_others()
    {
        var a = _projects.Create("A");
        var b = _projects.Create("B");
        var c = _projects.Create("C");
        _projects.Reorder([c.Id, a.Id, b.Id]);

        await _projects.RemoveAsync(a.Id);

        Assert.Equal([c.Id, b.Id], _projects.Projects.Select(p => p.Id));
    }

    [Fact]
    public void Reorder_places_unlisted_projects_after_the_listed_ones()
    {
        var a = _projects.Create("A");
        var b = _projects.Create("B");
        var c = _projects.Create("C");

        _projects.Reorder([c.Id]);

        Assert.Equal([c.Id, a.Id, b.Id], _projects.Projects.Select(p => p.Id));
    }

    [Fact]
    public void Accent_is_normalized_stored_and_cleared()
    {
        var project = _projects.Create("Billing");

        _projects.SetAccent(project.Id, "3fa7d6");
        Assert.Equal("#3FA7D6", _projects.Get(project.Id)!.AccentColor);

        _projects.SetAccent(project.Id, null);
        Assert.Null(_projects.Get(project.Id)!.AccentColor);
    }

    [Fact]
    public void Invalid_accent_is_rejected_and_the_previous_one_kept()
    {
        var project = _projects.Create("Billing");
        _projects.SetAccent(project.Id, "#3FA7D6");

        Assert.Throws<ArgumentException>(() => _projects.SetAccent(project.Id, "#12ZZ99"));

        Assert.Equal("#3FA7D6", _projects.Get(project.Id)!.AccentColor);
    }

    [Fact]
    public void Other_updates_do_not_change_the_position_or_the_accent()
    {
        var a = _projects.Create("A");
        var b = _projects.Create("B");
        _projects.SetAccent(b.Id, "#3FA7D6");
        _projects.Reorder([b.Id, a.Id]);

        _projects.Rename(b.Id, "B2");
        _projects.UpdateAlerts(b.Id, new AlertSettings(true, FrequencyMode.Immediate, 3));

        Assert.Equal([b.Id, a.Id], _projects.Projects.Select(p => p.Id));
        Assert.Equal("#3FA7D6", _projects.Get(b.Id)!.AccentColor);
    }

    private static async Task WaitForAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + Timeout;
        while (!condition())
        {
            if (DateTime.UtcNow > deadline) throw new TimeoutException("Condition was not met in time.");
            await Task.Delay(10);
        }
    }

    private static async Task<List<string>> ReadAsync(Subscription<LogLine> subscription, int count)
    {
        using var cancellation = new CancellationTokenSource(Timeout);
        var lines = new List<string>();
        await foreach (var line in subscription.Reader.ReadAllAsync(cancellation.Token))
        {
            lines.Add(line.Text);
            if (lines.Count == count) break;
        }
        return lines;
    }

    private string LogFile(string name, string content = "start\n")
    {
        var path = _temp.File(name);
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public void A_project_can_be_created_with_only_a_name_and_has_the_default_alert_settings()
    {
        var project = _projects.Create("  Billing  ");

        Assert.Equal("Billing", project.Name);
        Assert.True(project.Enabled);
        Assert.Equal(AlertSettings.Default, project.Alerts);
        Assert.Empty(_projects.SourcesOf(project.Id));
        Assert.Equal(ProjectHealth.NotConfigured, _projects.StatusOf(project.Id).Health);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void An_empty_name_is_rejected(string name)
    {
        Assert.Throws<ArgumentException>(() => _projects.Create(name));
    }

    [Fact]
    public void Names_must_be_unique_ignoring_case_on_create_and_rename()
    {
        _projects.Create("Billing");
        var other = _projects.Create("Orders");

        Assert.Throws<ArgumentException>(() => _projects.Create("billing"));
        Assert.Throws<ArgumentException>(() => _projects.Rename(other.Id, "BILLING"));
        _projects.Rename(other.Id, "Orders v2");
        Assert.Equal("Orders v2", _projects.Get(other.Id)!.Name);
    }

    [Fact]
    public void Renaming_a_project_to_its_own_name_with_other_case_is_allowed()
    {
        var project = _projects.Create("billing");

        _projects.Rename(project.Id, "Billing");

        Assert.Equal("Billing", _projects.Get(project.Id)!.Name);
    }

    [Fact]
    public void Alert_settings_are_saved_and_validated()
    {
        var project = _projects.Create("Billing");

        _projects.UpdateAlerts(project.Id, new AlertSettings(true, FrequencyMode.Summary, 30));

        Assert.Equal(new AlertSettings(true, FrequencyMode.Summary, 30), _projects.Get(project.Id)!.Alerts);
        Assert.Throws<ArgumentException>(() => _projects.UpdateAlerts(project.Id, new AlertSettings(false, FrequencyMode.Immediate, 0)));
        Assert.Throws<ArgumentException>(() => _projects.UpdateAlerts(project.Id, new AlertSettings(false, FrequencyMode.Immediate, AlertSettings.MaxMinutes + 1)));
    }

    [Fact]
    public void Adding_a_source_assigns_it_to_the_project_and_makes_the_project_healthy()
    {
        var project = _projects.Create("Billing");
        var path = LogFile("a.log");

        var source = _projects.AddSource(project.Id, SourceKind.File, path, null);

        Assert.Equal(project.Id, source.ProjectId);
        Assert.Equal(project.Id, _projects.ProjectOfSource(source.Id));
        Assert.Equal([source.Id], _projects.SourcesOf(project.Id).Select(s => s.Id));
    }

    [Fact]
    public async Task A_folder_source_can_be_added_to_a_project()
    {
        var project = _projects.Create("Billing");
        var source = _projects.AddSource(project.Id, SourceKind.Folder, _temp.Path, "Svc*.log");

        await WaitForAsync(() => _projects.StatusOf(project.Id).Health == ProjectHealth.Degraded);

        Assert.Equal(SourceKind.Folder, source.Kind);
        Assert.Equal("Svc*.log", source.Pattern);
    }

    [Fact]
    public async Task Status_is_healthy_degraded_not_configured_or_disabled()
    {
        var project = _projects.Create("Billing");
        Assert.Equal(ProjectHealth.NotConfigured, _projects.StatusOf(project.Id).Health);

        var good = LogFile("good.log");
        _projects.AddSource(project.Id, SourceKind.File, good, null);
        await WaitForAsync(() => _projects.StatusOf(project.Id).Health == ProjectHealth.Healthy);

        var missing = _temp.File("missing.log");
        _projects.AddSource(project.Id, SourceKind.File, missing, null);
        await WaitForAsync(() => _projects.StatusOf(project.Id).Health == ProjectHealth.Degraded);
        Assert.Equal([missing], _projects.StatusOf(project.Id).AffectedSources);

        await _projects.SetEnabledAsync(project.Id, false);
        Assert.Equal(ProjectHealth.Disabled, _projects.StatusOf(project.Id).Health);
    }

    [Fact]
    public async Task A_source_that_comes_back_makes_the_project_healthy_again()
    {
        var project = _projects.Create("Billing");
        var path = _temp.File("later.log");
        _projects.AddSource(project.Id, SourceKind.File, path, null);
        await WaitForAsync(() => _projects.StatusOf(project.Id).Health == ProjectHealth.Degraded);

        File.WriteAllText(path, "hello\n");

        await WaitForAsync(() => _projects.StatusOf(project.Id).Health == ProjectHealth.Healthy);
    }

    [Fact]
    public async Task Disabling_a_project_stops_all_its_sources_and_enabling_resumes_them()
    {
        var project = _projects.Create("Billing");
        var path = LogFile("a.log");
        using var subscription = _tailing.Lines.Subscribe();
        var source = _projects.AddSource(project.Id, SourceKind.File, path, null);
        await Task.Delay(150);

        await _projects.SetEnabledAsync(project.Id, false);
        File.AppendAllText(path, "while off\n");
        await Task.Delay(200);

        Assert.Equal(SourceStatus.Paused, _tailing.GetStatus(source.Id).Status);
        Assert.False(subscription.Reader.TryRead(out _));
        Assert.True(_tailing.Sources.Single().Enabled); // The source's own setting is kept.

        await _projects.SetEnabledAsync(project.Id, true);

        Assert.Equal(["while off"], await ReadAsync(subscription, 1));
    }

    [Fact]
    public async Task A_source_disabled_on_its_own_stays_off_when_the_project_is_re_enabled()
    {
        var project = _projects.Create("Billing");
        var kept = LogFile("kept.log");
        var off = LogFile("off.log");
        _projects.AddSource(project.Id, SourceKind.File, kept, null);
        var offSource = _projects.AddSource(project.Id, SourceKind.File, off, null);
        await _projects.SetSourceEnabledAsync(offSource.Id, false);

        await _projects.SetEnabledAsync(project.Id, false);
        await _projects.SetEnabledAsync(project.Id, true);

        Assert.Equal(SourceStatus.Paused, _tailing.GetStatus(offSource.Id).Status);
    }

    [Fact]
    public async Task Removing_a_project_stops_and_removes_its_sources()
    {
        var project = _projects.Create("Billing");
        var other = _projects.Create("Orders");
        var path = LogFile("a.log");
        var otherPath = LogFile("b.log");
        using var subscription = _tailing.Lines.Subscribe();
        var source = _projects.AddSource(project.Id, SourceKind.File, path, null);
        var otherSource = _projects.AddSource(other.Id, SourceKind.File, otherPath, null);
        await Task.Delay(150);

        await _projects.RemoveAsync(project.Id);
        File.AppendAllText(path, "ignored\n");
        File.AppendAllText(otherPath, "kept\n");

        Assert.Null(_projects.Get(project.Id));
        Assert.Null(_projects.ProjectOfSource(source.Id));
        Assert.Equal([otherSource.Id], _tailing.Sources.Select(s => s.Id));
        Assert.Equal(["kept"], await ReadAsync(subscription, 1));
    }

    [Fact]
    public async Task Removing_a_source_leaves_the_project_in_place()
    {
        var project = _projects.Create("Billing");
        var source = _projects.AddSource(project.Id, SourceKind.File, LogFile("a.log"), null);

        await _projects.RemoveSourceAsync(source.Id);

        Assert.Empty(_projects.SourcesOf(project.Id));
        Assert.NotNull(_projects.Get(project.Id));
    }

    [Fact]
    public void Adding_a_source_to_an_unknown_project_is_rejected()
    {
        Assert.Throws<KeyNotFoundException>(() => _projects.AddSource(999, SourceKind.File, LogFile("a.log"), null));
    }

    [Fact]
    public void Changes_are_announced()
    {
        var changes = 0;
        _projects.Changed += (_, _) => changes++;

        var project = _projects.Create("Billing");
        _projects.Rename(project.Id, "Billing 2");
        _projects.UpdateAlerts(project.Id, AlertSettings.Default with { WarningAlerts = true });

        Assert.Equal(3, changes);
    }

    [Fact]
    public async Task Sources_of_a_project_disabled_at_startup_do_not_run()
    {
        var project = _projects.Create("Billing");
        var path = LogFile("a.log");
        var source = _projects.AddSource(project.Id, SourceKind.File, path, null);
        await _projects.SetEnabledAsync(project.Id, false);
        await _tailing.DisposeAsync();

        var tailing = new TailingService(
            new InMemoryLogSourceRepository(), new InMemoryReadPositionStore(), Fast, projectEnabled: _ => false);
        var added = tailing.Add(SourceKind.File, path, null, projectId: project.Id);
        tailing.Start();

        Assert.Equal(SourceStatus.Paused, tailing.GetStatus(added.Id).Status);
        Assert.NotEqual(0, source.Id);
        await tailing.DisposeAsync();
    }

    [Fact]
    public async Task Create_with_sources_saves_project_sources_and_alerts_together()
    {
        var draft = new ProjectDraft { Name = " Billing ", Alerts = new AlertSettings(true, FrequencyMode.Summary, 15) };
        draft.AddSource(new SourceDraft(SourceKind.Folder, _temp.Path, "*.log"));
        draft.AddSource(new SourceDraft(SourceKind.File, _temp.File("a.log"), null));

        var project = await _projects.CreateWithSourcesAsync(draft);

        Assert.Equal("Billing", project.Name);
        Assert.Equal(new AlertSettings(true, FrequencyMode.Summary, 15), _projects.Get(project.Id)!.Alerts);
        Assert.Equal(2, _projects.SourcesOf(project.Id).Count);
    }

    [Fact]
    public async Task Create_with_sources_rejects_a_duplicate_name_and_saves_nothing()
    {
        _projects.Create("Billing");
        var draft = new ProjectDraft { Name = "billing" };
        draft.AddSource(new SourceDraft(SourceKind.File, _temp.File("a.log"), null));

        await Assert.ThrowsAsync<ArgumentException>(() => _projects.CreateWithSourcesAsync(draft));

        Assert.Single(_projects.Projects);
        Assert.Empty(_tailing.Sources);
    }

    [Fact]
    public async Task Create_with_sources_rejects_a_draft_without_sources_and_saves_nothing()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _projects.CreateWithSourcesAsync(new ProjectDraft { Name = "Empty" }));

        Assert.Empty(_projects.Projects);
    }

    [Fact]
    public async Task Create_with_sources_undoes_everything_when_saving_fails_midway()
    {
        var failing = new TailingService(
            new FailingSecondAddRepository(), new InMemoryReadPositionStore(), Fast);
        await using var _ = failing;
        var projects = new ProjectService(_repository, failing);
        var draft = new ProjectDraft { Name = "Billing" };
        draft.AddSource(new SourceDraft(SourceKind.File, _temp.File("a.log"), null));
        draft.AddSource(new SourceDraft(SourceKind.File, _temp.File("b.log"), null));

        await Assert.ThrowsAsync<InvalidOperationException>(() => projects.CreateWithSourcesAsync(draft));

        Assert.Empty(_repository.List());
        Assert.Empty(failing.Sources);
    }

    private sealed class FailingSecondAddRepository : ILogSourceRepository
    {
        private readonly InMemoryLogSourceRepository _inner = new();
        private int _adds;

        public IReadOnlyList<LogSource> List() => _inner.List();

        public LogSource Add(SourceKind kind, string path, string? pattern, long projectId = 0) =>
            ++_adds == 2 ? throw new InvalidOperationException("disk full") : _inner.Add(kind, path, pattern, projectId);

        public void Remove(long id) => _inner.Remove(id);

        public void SetEnabled(long id, bool enabled) => _inner.SetEnabled(id, enabled);
    }
}
