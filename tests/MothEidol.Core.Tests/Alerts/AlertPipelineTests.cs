using MothEidol.Core.Alerts;
using MothEidol.Core.Logs;
using MothEidol.Core.Projects;
using MothEidol.Core.Tests.Logs;

namespace MothEidol.Core.Tests.Alerts;

public sealed class AlertPipelineTests : IAsyncDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

    private sealed class ManualTime(DateTimeOffset start) : TimeProvider
    {
        private DateTimeOffset _now = start;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan by) => _now += by;
    }

    private readonly TempFolder _temp = new();
    private readonly ManualTime _time = new(T0);
    private readonly EntryBus _entries = new();
    private readonly InMemoryEventStore _store = new();
    private readonly TailingService _tailing;
    private readonly ProjectService _projects;
    private readonly Project _project;
    private readonly long _sourceId;
    private AlertPipeline? _pipeline;

    public AlertPipelineTests()
    {
        ProjectService? projects = null;
        _tailing = new TailingService(
            new InMemoryLogSourceRepository(), new InMemoryReadPositionStore(),
            new TailOptions { PollInterval = TimeSpan.FromMinutes(5), RetryInterval = TimeSpan.FromMinutes(5) },
            projectEnabled: id => projects?.IsEnabled(id) ?? true);
        _projects = projects = new ProjectService(new InMemoryProjectRepository(), _tailing, _store);
        _project = _projects.Create("Billing");
        _sourceId = _projects.AddSource(_project.Id, SourceKind.File, _temp.File("a.log"), null).Id;
    }

    public async ValueTask DisposeAsync()
    {
        if (_pipeline is not null) await _pipeline.DisposeAsync();
        await _tailing.DisposeAsync();
        _temp.Dispose();
    }

    private AlertPipeline Start(AlertOptions? options = null)
    {
        _pipeline = new AlertPipeline(_entries, _projects, _store, options ?? new AlertOptions { TickInterval = TimeSpan.FromMilliseconds(20) }, _time);
        _pipeline.Start();
        return _pipeline;
    }

    private Task Publish(EntryLevel level, string message, long? sourceId = null) =>
        _entries.PublishAsync([new LogEntry(sourceId ?? _sourceId, T0, level, message, false, T0)], CancellationToken.None).AsTask();

    private static async Task WaitForAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + Timeout;
        while (!condition())
        {
            if (DateTime.UtcNow > deadline) throw new TimeoutException("Condition was not met in time.");
            await Task.Delay(10);
        }
    }

    private static async Task<List<AlertDelivery>> ReadAsync(Subscription<AlertDelivery> subscription, int count)
    {
        using var cancellation = new CancellationTokenSource(Timeout);
        var list = new List<AlertDelivery>();
        await foreach (var delivery in subscription.Reader.ReadAllAsync(cancellation.Token))
        {
            list.Add(delivery);
            if (list.Count == count) break;
        }
        return list;
    }

    private async Task SettleAsync() => await Task.Delay(150);

    [Fact]
    public async Task An_error_is_recorded_and_delivered_immediately()
    {
        var pipeline = Start();
        using var deliveries = pipeline.Deliveries.Subscribe();

        await Publish(EntryLevel.Error, "Failed to send");

        var delivery = (await ReadAsync(deliveries, 1)).Single();
        Assert.Equal(DeliveryKind.Alert, delivery.Kind);
        Assert.Equal(_project.Id, delivery.ProjectId);
        Assert.Equal("Failed to send", delivery.Alert!.Message);
        await WaitForAsync(() => _store.Groups(_project.Id).Count == 1);
        Assert.Equal(1, _store.Pending(_project.Id).Occurrences);
    }

    [Fact]
    public async Task Information_never_alerts_and_warnings_follow_the_project_setting()
    {
        Start();

        await Publish(EntryLevel.Information, "all good");
        await Publish(EntryLevel.Warning, "careful");
        await SettleAsync();
        Assert.Empty(_store.Groups(_project.Id));

        _projects.UpdateAlerts(_project.Id, AlertSettings.Default with { WarningAlerts = true });
        await Publish(EntryLevel.Warning, "careful");

        await WaitForAsync(() => _store.Groups(_project.Id).Count == 1);
        Assert.Equal(EntryLevel.Warning, _store.Groups(_project.Id).Single().Level);
    }

    [Fact]
    public async Task A_disabled_project_and_unknown_sources_produce_nothing()
    {
        Start();
        await _projects.SetEnabledAsync(_project.Id, false);

        await Publish(EntryLevel.Error, "while disabled");
        await Publish(EntryLevel.Error, "unknown source", sourceId: 9999);
        await SettleAsync();

        Assert.Empty(_store.Groups(_project.Id));
    }

    [Fact]
    public async Task Repeated_errors_with_different_ids_form_one_group_even_when_delivery_is_throttled()
    {
        var pipeline = Start();
        using var deliveries = pipeline.Deliveries.Subscribe();

        await Publish(EntryLevel.Error, "Envío KO: registro 100");
        await Publish(EntryLevel.Error, "Envío KO: registro 101");
        await Publish(EntryLevel.Error, "Envío KO: registro 102");

        await WaitForAsync(() => _store.Groups(_project.Id).Sum(g => g.Count) == 3);
        var group = _store.Groups(_project.Id).Single();
        Assert.Equal(3, group.Count);
        Assert.Equal(3, group.PendingCount);
        Assert.Single(await ReadAsync(deliveries, 1)); // only the first was delivered individually
    }

    [Fact]
    public async Task When_the_cooldown_ends_the_suppressed_alerts_arrive_as_one_summary()
    {
        var pipeline = Start();
        using var deliveries = pipeline.Deliveries.Subscribe();
        await Publish(EntryLevel.Error, "boom 1");
        await ReadAsync(deliveries, 1);
        await Publish(EntryLevel.Error, "boom 2");
        await Publish(EntryLevel.Error, "boom 3");
        await WaitForAsync(() => _store.Groups(_project.Id).Single().Count == 3);

        _time.Advance(TimeSpan.FromMinutes(6));

        var summary = (await ReadAsync(deliveries, 1)).Single();
        Assert.Equal(DeliveryKind.Summary, summary.Kind);
        Assert.Equal(2, summary.Summary!.Count);
    }

    [Fact]
    public async Task Summary_mode_delivers_one_aggregated_alert_per_interval()
    {
        _projects.UpdateAlerts(_project.Id, new AlertSettings(false, FrequencyMode.Summary, 15));
        var pipeline = Start();
        using var deliveries = pipeline.Deliveries.Subscribe();

        await Publish(EntryLevel.Error, "first failure");
        await Publish(EntryLevel.Fatal, "second failure");
        await WaitForAsync(() => _store.Groups(_project.Id).Count == 2);
        await SettleAsync();
        Assert.False(deliveries.Reader.TryRead(out _));

        _time.Advance(TimeSpan.FromMinutes(16));

        var summary = (await ReadAsync(deliveries, 1)).Single().Summary!;
        Assert.Equal(2, summary.Count);
    }

    [Fact]
    public async Task Changing_to_immediate_mode_releases_what_the_summary_window_held()
    {
        _projects.UpdateAlerts(_project.Id, new AlertSettings(false, FrequencyMode.Summary, 60));
        var pipeline = Start();
        using var deliveries = pipeline.Deliveries.Subscribe();
        await Publish(EntryLevel.Error, "held back");
        await WaitForAsync(() => _store.Groups(_project.Id).Count == 1);

        _projects.UpdateAlerts(_project.Id, AlertSettings.Default);

        var summary = (await ReadAsync(deliveries, 1)).Single();
        Assert.Equal(DeliveryKind.Summary, summary.Kind);
        Assert.Equal(1, summary.Summary!.Count);
    }

    [Fact]
    public async Task Old_seen_groups_are_purged_but_pending_ones_are_kept()
    {
        var options = new AlertOptions
        {
            TickInterval = TimeSpan.FromMilliseconds(20),
            PurgeInterval = TimeSpan.FromMinutes(1),
            Retention = TimeSpan.FromDays(30),
        };
        Start(options);
        await Publish(EntryLevel.Error, "seen and old");
        await Publish(EntryLevel.Fatal, "pending and old");
        await WaitForAsync(() => _store.Groups(_project.Id).Count == 2);
        _store.MarkSeen(_store.Groups(_project.Id).Single(g => g.Level == EntryLevel.Error).Id, T0);

        _time.Advance(TimeSpan.FromDays(31));

        await WaitForAsync(() => _store.Groups(_project.Id).Count == 1);
        Assert.Equal(EntryLevel.Fatal, _store.Groups(_project.Id).Single().Level);
    }

    [Fact]
    public async Task Removing_a_project_removes_its_events()
    {
        Start();
        await Publish(EntryLevel.Error, "boom");
        await WaitForAsync(() => _store.Groups(_project.Id).Count == 1);

        await _projects.RemoveAsync(_project.Id);

        Assert.Empty(_store.Groups(_project.Id));
    }
}
