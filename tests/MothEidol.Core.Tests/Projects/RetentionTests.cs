using MothEidol.Core.Alerts;
using MothEidol.Core.Logs;
using MothEidol.Core.Projects;
using MothEidol.Core.Tests.Logs;

namespace MothEidol.Core.Tests.Projects;

public sealed class RetentionTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private readonly InMemoryEventStore _events = new();
    private readonly TailingService _tailing;
    private readonly ProjectService _projects;

    public RetentionTests()
    {
        _tailing = new TailingService(new InMemoryLogSourceRepository(), new InMemoryReadPositionStore(), new TailOptions());
        _projects = new ProjectService(
            new InMemoryProjectRepository(), _tailing, _events, new InMemoryProjectStorage(_events), new FixedTime(Now));
    }

    public void Dispose() => _tailing.DisposeAsync().AsTask().Wait();

    private void Record(Project project, string key, DateTimeOffset at) =>
        _events.Record(project.Id, EntryLevel.Error, key, key, 1, at, "boom", 50);

    [Fact]
    public void New_projects_keep_10_days()
    {
        Assert.Equal(10, Project.DefaultRetentionDays);
        Assert.Equal(10, _projects.Create("Billing").RetentionDays);
    }

    [Fact]
    public void Old_groups_go_pending_included_and_recent_ones_are_trimmed_keeping_their_totals()
    {
        var project = _projects.Create("Billing");
        Record(project, "old-pending", Now.AddDays(-12));
        Record(project, "old-seen", Now.AddDays(-15));
        _events.MarkSeen(_events.Groups(project.Id).Single(g => g.Title == "old-seen").Id, Now.AddDays(-14));
        Record(project, "recent", Now.AddDays(-11));
        Record(project, "recent", Now);

        var result = _projects.ApplyRetention(Now);

        Assert.Equal(new CleanupResult(2, 3), result);
        var kept = Assert.Single(_events.Groups(project.Id));
        Assert.Equal("recent", kept.Title);
        Assert.Equal(2, kept.Count);
        Assert.Equal(2, kept.PendingCount);
        Assert.Single(_events.Occurrences(kept.Id));
        Assert.Equal(2, _events.Pending(project.Id).Occurrences);
    }

    [Fact]
    public async Task Each_project_is_trimmed_to_its_own_retention()
    {
        var shortOne = _projects.Create("Short");
        var longOne = _projects.Create("Long");
        Record(shortOne, "a", Now.AddDays(-5));
        Record(longOne, "b", Now.AddDays(-5));
        Record(longOne, "c", Now.AddDays(-40));
        await _projects.SetRetentionAsync(shortOne.Id, 3);
        await _projects.SetRetentionAsync(longOne.Id, 30);
        Record(shortOne, "d", Now.AddDays(-4));

        _projects.ApplyRetention(Now);

        Assert.Empty(_events.Groups(shortOne.Id));
        Assert.Equal("b", Assert.Single(_events.Groups(longOne.Id)).Title);
    }

    [Fact]
    public async Task Changing_the_retention_saves_it_and_trims_at_once()
    {
        var project = _projects.Create("Billing");
        Record(project, "a", Now.AddDays(-5));
        Record(project, "b", Now);

        var result = await _projects.SetRetentionAsync(project.Id, 3);

        Assert.Equal(3, _projects.Get(project.Id)!.RetentionDays);
        Assert.Equal(new CleanupResult(1, 1), result);
        Assert.Equal("b", Assert.Single(_events.Groups(project.Id)).Title);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(366)]
    public async Task A_retention_out_of_range_is_rejected_and_not_saved(int days)
    {
        var project = _projects.Create("Billing");

        await Assert.ThrowsAsync<ArgumentException>(() => _projects.SetRetentionAsync(project.Id, days));
        Assert.Equal(Project.DefaultRetentionDays, _projects.Get(project.Id)!.RetentionDays);
    }

    [Theory]
    [InlineData(0, 0, false)]
    [InlineData(4L * 1024 * 1024, 8L * 1024 * 1024, false)] // Half the file but under the minimum.
    [InlineData(10L * 1024 * 1024, 100L * 1024 * 1024, false)] // Over the minimum but a tenth of the file.
    [InlineData(10L * 1024 * 1024, 40L * 1024 * 1024, true)]
    [InlineData(8L * 1024 * 1024, 32L * 1024 * 1024, true)]
    public void The_periodic_upkeep_compacts_only_when_it_is_worth_it(long reclaimable, long file, bool expected) =>
        Assert.Equal(expected, ProjectService.ShouldCompact(reclaimable, file));
}
