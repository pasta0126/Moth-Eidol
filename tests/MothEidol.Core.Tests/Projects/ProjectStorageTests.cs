using MothEidol.Core.Alerts;
using MothEidol.Core.Logs;
using MothEidol.Core.Projects;
using MothEidol.Core.Tests.Logs;

namespace MothEidol.Core.Tests.Projects;

public sealed class ProjectStorageTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private readonly TempFolder _temp = new();
    private readonly InMemoryEventStore _events = new();
    private readonly InMemoryReadPositionStore _positions = new();
    private readonly TailingService _tailing;
    private readonly ProjectService _projects;

    public ProjectStorageTests()
    {
        _tailing = new TailingService(new InMemoryLogSourceRepository(), _positions, new TailOptions());
        _projects = new ProjectService(
            new InMemoryProjectRepository(), _tailing, _events, new InMemoryProjectStorage(_events), new FixedTime(Now));
    }

    public void Dispose()
    {
        _tailing.DisposeAsync().AsTask().Wait();
        _temp.Dispose();
    }

    private void Record(Project project, string key, DateTimeOffset at, string message = "boom") =>
        _events.Record(project.Id, EntryLevel.Error, key, key, 1, at, message, 50);

    [Fact]
    public async Task Clear_all_removes_every_event_but_keeps_sources_read_positions_and_other_projects()
    {
        var billing = _projects.Create("Billing");
        var orders = _projects.Create("Orders");
        var source = _projects.AddSource(billing.Id, SourceKind.File, _temp.File("a.log"), null);
        _positions.Save(source.Id, new FileCursor("a.log", 123, null, LineEncoding.Utf8));
        Record(billing, "a", Now.AddDays(-1));
        Record(billing, "b", Now);
        Record(orders, "c", Now);

        var result = await _projects.ClearEventsAsync(billing.Id);

        Assert.Equal(new CleanupResult(2, 2), result);
        Assert.Empty(_events.Groups(billing.Id));
        Assert.Equal(PendingSummary.None, _events.Pending(billing.Id));
        Assert.Single(_events.Groups(orders.Id));
        Assert.Single(_projects.SourcesOf(billing.Id));
        Assert.Equal(123, _positions.Get(source.Id)?.Offset);
    }

    [Fact]
    public async Task Keep_last_days_removes_quiet_groups_pending_included_and_trims_kept_ones()
    {
        var project = _projects.Create("Billing");
        Record(project, "old", Now.AddDays(-20));
        Record(project, "recent", Now.AddDays(-10));
        Record(project, "recent", Now.AddDays(-1));
        Record(project, "recent", Now);

        var result = await _projects.KeepLastDaysAsync(project.Id, 7);

        Assert.Equal(new CleanupResult(1, 2), result);
        var kept = Assert.Single(_events.Groups(project.Id));
        Assert.Equal("recent", kept.Title);
        Assert.Equal(3, kept.Count);
        Assert.Equal(3, kept.PendingCount);
        Assert.Equal(Now.AddDays(-10), kept.FirstSeen);
        Assert.Equal(2, _events.Occurrences(kept.Id).Count);
        Assert.Equal(0, _events.GroupSeries(kept.Id, Now, 24 * 30).Take(24 * 30 - 24 * 7).Sum());
    }

    [Fact]
    public void Keep_last_days_preview_counts_the_groups_to_remove_and_the_pending_ones()
    {
        var project = _projects.Create("Billing");
        Record(project, "old-seen", Now.AddDays(-20));
        Record(project, "old-pending", Now.AddDays(-15));
        Record(project, "recent", Now);
        _events.MarkSeen(_events.Groups(project.Id).Single(g => g.Title == "old-seen").Id, Now);

        Assert.Equal((2, 1), _projects.KeepLastDaysPreview(project.Id, 7));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(366)]
    public async Task Keep_last_days_rejects_a_number_of_days_out_of_range(int days)
    {
        var project = _projects.Create("Billing");
        Record(project, "old", Now.AddDays(-20));

        await Assert.ThrowsAsync<ArgumentException>(() => _projects.KeepLastDaysAsync(project.Id, days));
        Assert.Single(_events.Groups(project.Id));
    }

    [Fact]
    public async Task Usage_counts_the_project_and_estimates_its_size()
    {
        var project = _projects.Create("Billing");
        Record(project, "a", Now.AddHours(-3), message: new string('x', 1000));
        Record(project, "a", Now, message: new string('y', 1000));

        var usage = await _projects.UsageOfAsync(project.Id);

        Assert.Equal(1, usage.Groups);
        Assert.Equal(1, usage.PendingGroups);
        Assert.Equal(2, usage.Occurrences);
        Assert.Equal(2, usage.HourlyCounts);
        Assert.InRange(usage.ApproxBytes, 2000, 2300);
        Assert.Equal(Now.AddHours(-3), usage.Oldest);
        Assert.Equal(Now, usage.Newest);
    }

    [Fact]
    public void New_projects_have_the_default_threshold_and_are_not_over_before_the_first_measure()
    {
        var project = _projects.Create("Billing");
        Record(project, "a", Now, message: new string('x', 3 * 1024 * 1024));
        _projects.SetStorageWarning(project.Id, 1);

        Assert.Equal(Project.DefaultStorageWarningMb, project.StorageWarningMb);
        var warning = _projects.StorageWarningOf(project.Id);
        Assert.Null(warning.SizeBytes);
        Assert.False(warning.IsOver);
    }

    [Fact]
    public async Task The_warning_follows_the_measured_size_and_the_threshold()
    {
        var project = _projects.Create("Billing");
        Record(project, "a", Now, message: new string('x', 3 * 1024 * 1024));

        await _projects.RefreshStorageSizesAsync();
        Assert.False(_projects.StorageWarningOf(project.Id).IsOver);

        _projects.SetStorageWarning(project.Id, 2); // Re-evaluated at once, without measuring again.
        Assert.True(_projects.StorageWarningOf(project.Id).IsOver);
        Assert.Equal(2, _projects.StorageWarningOf(project.Id).ThresholdMb);

        await _projects.ClearEventsAsync(project.Id);
        Assert.False(_projects.StorageWarningOf(project.Id).IsOver);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10241)]
    public void A_threshold_out_of_range_is_rejected(int megabytes)
    {
        var project = _projects.Create("Billing");

        Assert.Throws<ArgumentException>(() => _projects.SetStorageWarning(project.Id, megabytes));
        Assert.Equal(Project.DefaultStorageWarningMb, _projects.Get(project.Id)!.StorageWarningMb);
    }

    [Fact]
    public async Task Removing_a_project_leaves_nothing_of_it_and_a_new_one_with_the_same_name_starts_empty()
    {
        var billing = _projects.Create("Billing");
        var orders = _projects.Create("Orders");
        var source = _projects.AddSource(billing.Id, SourceKind.File, _temp.File("a.log"), null);
        _positions.Save(source.Id, new FileCursor("a.log", 123, null, LineEncoding.Utf8));
        Record(billing, "a", Now);
        Record(orders, "b", Now);

        await _projects.RemoveAsync(billing.Id);

        Assert.Null(_projects.Get(billing.Id));
        Assert.DoesNotContain(_tailing.Sources, s => s.ProjectId == billing.Id);
        Assert.Null(_positions.Get(source.Id));
        Assert.Empty(_events.Groups(billing.Id));
        Assert.Single(_events.Groups(orders.Id));

        var again = _projects.Create("Billing");
        Assert.Empty(_events.Groups(again.Id));
        Assert.Empty(_projects.SourcesOf(again.Id));
    }

    [Fact]
    public async Task A_failed_removal_keeps_the_project_monitored()
    {
        var repository = new FailingRemoveRepository();
        var projects = new ProjectService(repository, _tailing, _events);
        var project = projects.Create("Billing");
        var source = projects.AddSource(project.Id, SourceKind.File, _temp.File("a.log"), null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => projects.RemoveAsync(project.Id));

        Assert.NotNull(projects.Get(project.Id));
        Assert.NotEqual(SourceStatus.Paused, _tailing.GetStatus(source.Id).Status); // Its monitor runs again.
        Assert.Single(projects.SourcesOf(project.Id));
    }

    [Fact]
    public void The_tile_line_appears_only_over_the_threshold()
    {
        Assert.Equal("Storage over 100 MB", new StorageWarning(101L * 1024 * 1024, 100).TileText);
        Assert.Equal("", new StorageWarning(100L * 1024 * 1024, 100).TileText);
        Assert.Equal("", new StorageWarning(null, 1).TileText);
    }

    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(1023, "1,023 B")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(5L * 1024 * 1024, "5 MB")]
    [InlineData(3L * 1024 * 1024 * 1024 / 2, "1.5 GB")]
    public void Sizes_are_shown_in_binary_units(long bytes, string expected)
    {
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        try { Assert.Equal(expected, ByteSize.Format(bytes)); }
        finally { System.Globalization.CultureInfo.CurrentCulture = culture; }
    }

    private sealed class FailingRemoveRepository : IProjectRepository
    {
        private readonly InMemoryProjectRepository _inner = new();

        public IReadOnlyList<Project> List() => _inner.List();

        public Project Add(string name) => _inner.Add(name);

        public void Update(Project project) => _inner.Update(project);

        public void Remove(long id) => throw new InvalidOperationException("disk full");

        public void Reorder(IReadOnlyList<long> orderedIds) => _inner.Reorder(orderedIds);
    }
}
