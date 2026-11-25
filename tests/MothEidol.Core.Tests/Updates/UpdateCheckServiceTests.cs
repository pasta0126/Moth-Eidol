using MothEidol.Core.Settings;
using MothEidol.Core.Updates;

namespace MothEidol.Core.Tests.Updates;

public sealed class UpdateCheckServiceTests
{
    private sealed class FakeSource(Func<CancellationToken, Task<UpdateCheckResult>> answer) : IUpdateSource
    {
        public int Calls { get; private set; }

        public Task<UpdateCheckResult> GetLatestAsync(CancellationToken cancellationToken)
        {
            Calls++;
            return answer(cancellationToken);
        }

        public static FakeSource Returning(UpdateCheckResult result) => new(_ => Task.FromResult(result));
    }

    private static UpdateCheckResult Available(string version) => new UpdateCheckResult.Available(Version.Parse(version));

    [Fact]
    public async Task A_newer_version_becomes_the_notice_and_is_raised_once()
    {
        var service = new UpdateCheckService(FakeSource.Returning(Available("1.12.0.0")), new AppSettings(new InMemorySettingsRepository()), "1.11.0");
        var raised = new List<Version>();
        service.NoticeAvailable += raised.Add;

        await service.CheckAsync();

        Assert.Equal(new Version(1, 12, 0), service.Notice);
        Assert.Equal([new Version(1, 12, 0)], raised);
    }

    [Fact]
    public async Task The_check_runs_only_once_per_start()
    {
        var source = FakeSource.Returning(Available("1.12.0.0"));
        var service = new UpdateCheckService(source, new AppSettings(new InMemorySettingsRepository()), "1.11.0");

        await service.CheckAsync();
        await service.CheckAsync();

        Assert.Equal(1, source.Calls);
    }

    [Fact]
    public async Task With_the_check_off_the_source_is_not_contacted()
    {
        var source = FakeSource.Returning(Available("1.12.0.0"));
        var settings = new AppSettings(new InMemorySettingsRepository()) { CheckForUpdates = false };

        await new UpdateCheckService(source, settings, "1.11.0").CheckAsync();

        Assert.Equal(0, source.Calls);
    }

    [Fact]
    public void The_check_is_on_by_default()
    {
        Assert.True(new AppSettings(new InMemorySettingsRepository()).CheckForUpdates);
    }

    [Fact]
    public async Task A_failing_source_shows_nothing()
    {
        var service = new UpdateCheckService(new FakeSource(_ => throw new HttpRequestException("offline")), new AppSettings(new InMemorySettingsRepository()), "1.11.0");

        await service.CheckAsync();

        Assert.Null(service.Notice);
        Assert.Equal("offline", service.LastFailure);
    }

    [Fact]
    public async Task A_reported_failure_shows_nothing()
    {
        var service = new UpdateCheckService(FakeSource.Returning(new UpdateCheckResult.Failed("store error")), new AppSettings(new InMemorySettingsRepository()), "1.11.0");

        await service.CheckAsync();

        Assert.Null(service.Notice);
        Assert.Equal("store error", service.LastFailure);
    }

    [Fact]
    public async Task A_source_that_never_answers_times_out_silently()
    {
        var never = new FakeSource(_ => new TaskCompletionSource<UpdateCheckResult>().Task);
        var service = new UpdateCheckService(never, new AppSettings(new InMemorySettingsRepository()), "1.11.0");

        await service.CheckAsync(TimeSpan.FromMilliseconds(50));

        Assert.Null(service.Notice);
        Assert.NotNull(service.LastFailure);
    }

    [Fact]
    public async Task Postponing_hides_the_notice_until_the_next_start()
    {
        var repository = new InMemorySettingsRepository();
        var service = new UpdateCheckService(FakeSource.Returning(Available("1.12.0.0")), new AppSettings(repository), "1.11.0");
        await service.CheckAsync();

        service.Postpone();

        Assert.Null(service.Notice);
        var nextStart = new UpdateCheckService(FakeSource.Returning(Available("1.12.0.0")), new AppSettings(repository), "1.11.0");
        await nextStart.CheckAsync();
        Assert.Equal(new Version(1, 12, 0), nextStart.Notice);
    }

    [Fact]
    public async Task A_dismissed_version_stays_silent_after_a_restart()
    {
        var repository = new InMemorySettingsRepository();
        var service = new UpdateCheckService(FakeSource.Returning(Available("1.12.0.0")), new AppSettings(repository), "1.11.0");
        await service.CheckAsync();

        service.Dismiss();

        Assert.Null(service.Notice);
        Assert.Equal("1.12.0", new AppSettings(repository).DismissedUpdateVersion);
        var nextStart = new UpdateCheckService(FakeSource.Returning(Available("1.12.0.0")), new AppSettings(repository), "1.11.0");
        var raised = 0;
        nextStart.NoticeAvailable += _ => raised++;
        await nextStart.CheckAsync();
        Assert.Null(nextStart.Notice);
        Assert.Equal(0, raised);
    }

    [Fact]
    public async Task A_newer_version_after_a_dismissal_is_announced_again()
    {
        var repository = new InMemorySettingsRepository();
        new AppSettings(repository).DismissedUpdateVersion = "1.12.0";

        var service = new UpdateCheckService(FakeSource.Returning(Available("1.13.0.0")), new AppSettings(repository), "1.11.0");
        await service.CheckAsync();

        Assert.Equal(new Version(1, 13, 0), service.Notice);
    }

    [Fact]
    public async Task Turning_the_check_off_hides_the_notice_and_is_remembered()
    {
        var repository = new InMemorySettingsRepository();
        var service = new UpdateCheckService(FakeSource.Returning(Available("1.12.0.0")), new AppSettings(repository), "1.11.0");
        await service.CheckAsync();
        var changes = 0;
        service.NoticeChanged += () => changes++;

        service.SetEnabled(false);

        Assert.Null(service.Notice);
        Assert.Equal(1, changes);
        Assert.False(new AppSettings(repository).CheckForUpdates);
    }
}
