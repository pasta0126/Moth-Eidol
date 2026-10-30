using MothEidol.Core.Logs;

namespace MothEidol.Core.Tests.Logs;

public sealed class TailingServiceTests : IDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private static readonly TailOptions Fast = new()
    {
        PollInterval = TimeSpan.FromMilliseconds(20),
        RetryInterval = TimeSpan.FromMilliseconds(40),
        SaveInterval = TimeSpan.FromMilliseconds(20),
    };

    private readonly TempFolder _temp = new();

    public void Dispose() => _temp.Dispose();

    private static async Task<List<LogLine>> ReadAsync(Subscription<LogLine> subscription, int count)
    {
        using var cancellation = new CancellationTokenSource(Timeout);
        var lines = new List<LogLine>();
        await foreach (var line in subscription.Reader.ReadAllAsync(cancellation.Token))
        {
            lines.Add(line);
            if (lines.Count == count) break;
        }
        return lines;
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

    [Fact]
    public async Task Lines_appended_to_a_source_reach_subscribers()
    {
        var path = _temp.File("app.log");
        File.WriteAllText(path, "start\n");
        await using var service = new TailingService(new InMemoryLogSourceRepository(), new InMemoryReadPositionStore(), Fast);
        using var subscription = service.Lines.Subscribe();
        service.Start();
        var source = service.Add(SourceKind.File, path, null);
        await WaitForAsync(() => service.GetStatus(source.Id).Status == SourceStatus.Active);
        await Task.Delay(100); // let the first poll anchor the source at the end

        File.AppendAllText(path, "one\ntwo\n");

        var lines = await ReadAsync(subscription, 2);
        Assert.Equal(["one", "two"], lines.Select(l => l.Text));
        Assert.All(lines, l => Assert.Equal(source.Id, l.SourceId));
    }

    [Fact]
    public async Task Every_subscriber_receives_every_line()
    {
        var path = _temp.File("app.log");
        File.WriteAllText(path, "start\n");
        await using var service = new TailingService(new InMemoryLogSourceRepository(), new InMemoryReadPositionStore(), Fast);
        using var first = service.Lines.Subscribe();
        using var second = service.Lines.Subscribe();
        service.Add(SourceKind.File, path, null);
        await Task.Delay(100);

        File.AppendAllText(path, "x\ny\n");

        Assert.Equal(["x", "y"], (await ReadAsync(first, 2)).Select(l => l.Text));
        Assert.Equal(["x", "y"], (await ReadAsync(second, 2)).Select(l => l.Text));
    }

    [Fact]
    public async Task A_missing_file_is_saved_as_unavailable_and_becomes_active_when_it_appears()
    {
        var path = _temp.File("later.log");
        await using var service = new TailingService(new InMemoryLogSourceRepository(), new InMemoryReadPositionStore(), Fast);
        using var subscription = service.Lines.Subscribe();
        var source = service.Add(SourceKind.File, path, null);

        await WaitForAsync(() => service.GetStatus(source.Id).Status == SourceStatus.Unavailable);
        Assert.Single(service.Sources);

        File.WriteAllText(path, "hello\n");

        await WaitForAsync(() => service.GetStatus(source.Id).Status == SourceStatus.Active);
        Assert.Equal(["hello"], (await ReadAsync(subscription, 1)).Select(l => l.Text));
    }

    [Fact]
    public async Task One_unavailable_source_does_not_stop_the_others()
    {
        var good = _temp.File("good.log");
        File.WriteAllText(good, "start\n");
        await using var service = new TailingService(new InMemoryLogSourceRepository(), new InMemoryReadPositionStore(), Fast);
        using var subscription = service.Lines.Subscribe();
        service.Add(SourceKind.File, _temp.File("missing.log"), null);
        var healthy = service.Add(SourceKind.File, good, null);
        await Task.Delay(100);

        File.AppendAllText(good, "still works\n");

        var line = (await ReadAsync(subscription, 1)).Single();
        Assert.Equal(healthy.Id, line.SourceId);
    }

    [Fact]
    public async Task Disabling_a_source_stops_reading_and_keeps_it_configured()
    {
        var path = _temp.File("app.log");
        File.WriteAllText(path, "start\n");
        await using var service = new TailingService(new InMemoryLogSourceRepository(), new InMemoryReadPositionStore(), Fast);
        using var subscription = service.Lines.Subscribe();
        var source = service.Add(SourceKind.File, path, null);
        await Task.Delay(100);

        await service.SetEnabledAsync(source.Id, false);
        File.AppendAllText(path, "while disabled\n");
        await Task.Delay(200);

        Assert.Equal(SourceStatus.Paused, service.GetStatus(source.Id).Status);
        Assert.False(service.Sources.Single().Enabled);
        Assert.False(subscription.Reader.TryRead(out _));
    }

    [Fact]
    public async Task Re_enabling_resumes_without_repeating_earlier_lines()
    {
        var path = _temp.File("app.log");
        File.WriteAllText(path, "start\n");
        await using var service = new TailingService(new InMemoryLogSourceRepository(), new InMemoryReadPositionStore(), Fast);
        using var subscription = service.Lines.Subscribe();
        var source = service.Add(SourceKind.File, path, null);
        await Task.Delay(100);
        File.AppendAllText(path, "before\n");
        await ReadAsync(subscription, 1);
        await service.SetEnabledAsync(source.Id, false);
        File.AppendAllText(path, "during\n");

        await service.SetEnabledAsync(source.Id, true);

        Assert.Equal(["during"], (await ReadAsync(subscription, 1)).Select(l => l.Text));
    }

    [Fact]
    public async Task Removing_a_source_stops_monitoring_and_forgets_it()
    {
        var path = _temp.File("app.log");
        File.WriteAllText(path, "start\n");
        var positions = new InMemoryReadPositionStore();
        await using var service = new TailingService(new InMemoryLogSourceRepository(), positions, Fast);
        using var subscription = service.Lines.Subscribe();
        var source = service.Add(SourceKind.File, path, null);
        await Task.Delay(100);

        await service.RemoveAsync(source.Id);
        File.AppendAllText(path, "ignored\n");
        await Task.Delay(200);

        Assert.Empty(service.Sources);
        Assert.Null(positions.Get(source.Id));
        Assert.False(subscription.Reader.TryRead(out _));
    }

    [Fact]
    public async Task A_restart_resumes_from_the_saved_position_without_repeating_lines()
    {
        var path = _temp.File("app.log");
        File.WriteAllText(path, "start\n");
        var sources = new InMemoryLogSourceRepository();
        var positions = new InMemoryReadPositionStore();

        await using (var first = new TailingService(sources, positions, Fast))
        {
            using var subscription = first.Lines.Subscribe();
            first.Start();
            first.Add(SourceKind.File, path, null);
            await Task.Delay(100);
            File.AppendAllText(path, "seen 1\nseen 2\n");
            await ReadAsync(subscription, 2);
        } // disposing saves the position

        File.AppendAllText(path, "written while stopped\n");

        await using var second = new TailingService(sources, positions, Fast);
        using var secondSubscription = second.Lines.Subscribe();
        second.Start();

        Assert.Equal(["written while stopped"], (await ReadAsync(secondSubscription, 1)).Select(l => l.Text));
    }

    [Fact]
    public async Task Folder_sources_require_a_pattern_and_empty_paths_are_rejected()
    {
        await using var service = new TailingService(new InMemoryLogSourceRepository(), new InMemoryReadPositionStore(), Fast);

        Assert.Throws<ArgumentException>(() => service.Add(SourceKind.Folder, _temp.Path, "  "));
        Assert.Throws<ArgumentException>(() => service.Add(SourceKind.File, "  ", null));
    }

    [Fact]
    public async Task Status_changes_are_reported()
    {
        var statuses = new List<SourceStatus>();
        await using var service = new TailingService(new InMemoryLogSourceRepository(), new InMemoryReadPositionStore(), Fast);
        service.StatusChanged += (_, e) => { lock (statuses) statuses.Add(e.Status); };
        var path = _temp.File("later.log");

        service.Add(SourceKind.File, path, null);
        await WaitForAsync(() => { lock (statuses) return statuses.Contains(SourceStatus.Unavailable); });
        File.WriteAllText(path, "x\n");

        await WaitForAsync(() => { lock (statuses) return statuses.Last() == SourceStatus.Active; });
    }
}
