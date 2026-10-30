using MothEidol.Core.Alerts;
using MothEidol.Core.Logs;
using MothEidol.Core.Notifications;
using MothEidol.Core.Projects;
using MothEidol.Core.Settings;

namespace MothEidol.Core.Tests.Notifications;

internal sealed class RecordingNotifier(string id, bool enabledByDefault = true, Exception? fail = null) : INotifier
{
    public List<AlertDelivery> Received { get; } = [];

    public string Id => id;

    public string DisplayName => id;

    public bool EnabledByDefault => enabledByDefault;

    public Task NotifyAsync(AlertDelivery delivery, CancellationToken cancellationToken)
    {
        lock (Received) Received.Add(delivery);
        return fail is null ? Task.CompletedTask : Task.FromException(fail);
    }
}

internal static class Deliveries
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

    public static AlertDelivery Alert(string message = "Failed to send", EntryLevel level = EntryLevel.Error, long project = 1) =>
        new(project, DeliveryKind.Alert, new AlertEvent(project, 10, level, message, T0), null);

    public static AlertDelivery Summary(int count, params (EntryLevel Level, int Count)[] levels) =>
        new(1, DeliveryKind.Summary, null,
            new AlertSummary(count, levels.ToDictionary(l => l.Level, l => l.Count), [], T0, T0));
}

public class NotificationDispatcherTests
{
    private static NotificationDispatcher Create(params INotifier[] notifiers)
    {
        var dispatcher = new NotificationDispatcher(n => n.EnabledByDefault);
        foreach (var notifier in notifiers) dispatcher.Register(notifier);
        return dispatcher;
    }

    [Fact]
    public async Task Every_enabled_channel_receives_the_delivery()
    {
        var a = new RecordingNotifier("a");
        var b = new RecordingNotifier("b");
        var dispatcher = Create(a, b);

        await dispatcher.DispatchAsync(Deliveries.Alert());

        Assert.Single(a.Received);
        Assert.Single(b.Received);
    }

    [Fact]
    public async Task A_failing_channel_does_not_keep_the_others_from_delivering_and_the_failure_is_recorded()
    {
        var boom = new InvalidOperationException("speaker unplugged");
        var failing = new RecordingNotifier("failing", fail: boom);
        var healthy = new RecordingNotifier("healthy");
        var dispatcher = Create(failing, healthy);
        ChannelFailedEventArgs? reported = null;
        dispatcher.ChannelFailed += (_, e) => reported = e;

        await dispatcher.DispatchAsync(Deliveries.Alert());

        Assert.Single(healthy.Received);
        Assert.Equal("failing", reported!.ChannelId);
        Assert.Same(boom, reported.Error);
        Assert.Same(boom, dispatcher.LastError("failing"));
        Assert.Null(dispatcher.LastError("healthy"));
    }

    [Fact]
    public async Task A_channel_that_throws_synchronously_is_isolated_too()
    {
        var throwing = new ThrowingNotifier();
        var healthy = new RecordingNotifier("healthy");
        var dispatcher = Create(throwing, healthy);

        await dispatcher.DispatchAsync(Deliveries.Alert());

        Assert.Single(healthy.Received);
        Assert.NotNull(dispatcher.LastError("throwing"));
    }

    [Fact]
    public async Task A_disabled_channel_receives_nothing_and_changes_apply_at_the_next_delivery()
    {
        var a = new RecordingNotifier("a");
        var b = new RecordingNotifier("b");
        var enabled = new HashSet<string> { "a" };
        var dispatcher = new NotificationDispatcher(n => enabled.Contains(n.Id));
        dispatcher.Register(a);
        dispatcher.Register(b);

        await dispatcher.DispatchAsync(Deliveries.Alert());
        enabled.Add("b");
        enabled.Remove("a");
        await dispatcher.DispatchAsync(Deliveries.Alert());

        Assert.Single(a.Received);
        Assert.Single(b.Received);
    }

    [Fact]
    public async Task A_channel_registered_later_gets_deliveries_through_the_same_path()
    {
        var first = new RecordingNotifier("first");
        var dispatcher = Create(first);
        await dispatcher.DispatchAsync(Deliveries.Alert());

        var late = new RecordingNotifier("late");
        dispatcher.Register(late);
        await dispatcher.DispatchAsync(Deliveries.Alert());

        Assert.Equal(2, first.Received.Count);
        Assert.Single(late.Received);
    }

    [Fact]
    public async Task Deliveries_published_by_the_alert_pipeline_reach_the_channels()
    {
        var notifier = new RecordingNotifier("a");
        await using var dispatcher = Create(notifier);
        var bus = new DeliveryBus();
        dispatcher.Start(bus);

        await bus.PublishAsync([Deliveries.Alert("first"), Deliveries.Summary(3, (EntryLevel.Error, 3))], CancellationToken.None);

        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
        while (notifier.Received.Count < 2 && DateTime.UtcNow < deadline) await Task.Delay(10);
        Assert.Equal([DeliveryKind.Alert, DeliveryKind.Summary], notifier.Received.Select(d => d.Kind));
    }

    private sealed class ThrowingNotifier : INotifier
    {
        public string Id => "throwing";
        public string DisplayName => "Throwing";
        public bool EnabledByDefault => true;
        public Task NotifyAsync(AlertDelivery delivery, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("sync failure");
    }
}

public class UnreadCounterTests
{
    [Fact]
    public void An_alert_counts_one_and_a_summary_counts_the_alerts_it_stands_for()
    {
        var counter = new UnreadCounter();

        counter.Add(Deliveries.Alert());
        counter.Add(Deliveries.Summary(37, (EntryLevel.Error, 37)));

        Assert.Equal(38, counter.Count);
    }

    [Fact]
    public void Acknowledging_resets_the_counter_and_announces_it_only_when_there_was_something()
    {
        var counter = new UnreadCounter();
        var changes = 0;
        counter.Changed += (_, _) => changes++;

        counter.Acknowledge();
        Assert.Equal(0, changes);

        counter.Add(Deliveries.Alert());
        counter.Acknowledge();

        Assert.Equal(0, counter.Count);
        Assert.Equal(2, changes);
    }
}

public class ToastStackTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

    private static ToastItem Add(ToastStack stack, string headline, int second = 0) =>
        stack.Add("Billing", EntryLevel.Error, headline, null, T0.AddSeconds(second));

    [Fact]
    public void Toasts_stack_oldest_first()
    {
        var stack = new ToastStack(4);

        Add(stack, "one");
        Add(stack, "two");

        Assert.Equal(["one", "two"], stack.Visible.Select(t => t.Headline));
        Assert.Equal(0, stack.Overflow);
    }

    [Fact]
    public void Beyond_the_cap_the_oldest_are_folded_into_the_overflow_counter()
    {
        var stack = new ToastStack(3);

        for (var i = 1; i <= 7; i++) Add(stack, $"toast {i}");

        Assert.Equal(["toast 5", "toast 6", "toast 7"], stack.Visible.Select(t => t.Headline));
        Assert.Equal(4, stack.Overflow);
    }

    [Fact]
    public void Dismissing_a_toast_removes_it_and_dismissing_the_last_one_clears_the_overflow()
    {
        var stack = new ToastStack(2);
        var a = Add(stack, "a");
        var b = Add(stack, "b");
        var c = Add(stack, "c");

        stack.Dismiss(b.Id);
        Assert.Equal(["c"], stack.Visible.Select(t => t.Headline));
        Assert.Equal(1, stack.Overflow);

        stack.Dismiss(c.Id);
        Assert.Empty(stack.Visible);
        Assert.Equal(0, stack.Overflow);
        Assert.NotEqual(a.Id, c.Id);
    }

    [Fact]
    public void The_overflow_toast_can_be_dismissed_on_its_own()
    {
        var stack = new ToastStack(1);
        Add(stack, "a");
        Add(stack, "b");

        stack.DismissOverflow();

        Assert.Equal(0, stack.Overflow);
        Assert.Single(stack.Visible);
    }

    [Fact]
    public void Toasts_disappear_when_their_display_time_is_over()
    {
        var stack = new ToastStack(4, TimeSpan.FromSeconds(8));
        Add(stack, "old", second: 0);
        Add(stack, "new", second: 6);

        stack.Expire(T0.AddSeconds(8));

        Assert.Equal(["new"], stack.Visible.Select(t => t.Headline));

        stack.Expire(T0.AddSeconds(14));
        Assert.Empty(stack.Visible);
    }

    [Fact]
    public void Changes_are_announced_only_when_something_changed()
    {
        var stack = new ToastStack(4, TimeSpan.FromSeconds(8));
        var changes = 0;
        stack.Changed += (_, _) => changes++;

        var toast = Add(stack, "a");
        stack.Expire(T0.AddSeconds(1));
        stack.Dismiss(999);
        stack.Dismiss(toast.Id);

        Assert.Equal(2, changes);
    }
}

public class AlertTextTests
{
    [Fact]
    public void An_alert_reads_as_the_readable_title_and_where_it_happens()
    {
        var text = AlertText.For(
            Deliveries.Alert("Failed to send\nSystem.InvalidOperationException: boom\n   at Acme.Billing.Worker.Run() in C:\\x.cs:line 9"),
            "Billing");

        Assert.Equal("Billing", text.ProjectName);
        Assert.Equal(EntryLevel.Error, text.Level);
        Assert.Equal("InvalidOperationException: boom", text.Headline);
        Assert.Equal("Worker.Run", text.Detail);
    }

    [Fact]
    public void An_alert_without_an_exception_uses_its_first_line()
    {
        var text = AlertText.For(Deliveries.Alert("Envío KO: registro 12"), "Billing");

        Assert.Equal("Envío KO: registro 12", text.Headline);
        Assert.Null(text.Detail);
    }

    [Fact]
    public void A_summary_gives_the_count_the_worst_level_and_a_breakdown()
    {
        var text = AlertText.For(Deliveries.Summary(37, (EntryLevel.Error, 30), (EntryLevel.Warning, 6), (EntryLevel.Fatal, 1)), "Billing");

        Assert.Equal("37 more alerts", text.Headline);
        Assert.Equal(EntryLevel.Fatal, text.Level);
        Assert.Equal("1 fatal error, 30 errors, 6 warnings", text.Detail);
    }

    [Fact]
    public void A_summary_of_one_is_singular()
    {
        Assert.Equal("1 more alert", AlertText.For(Deliveries.Summary(1, (EntryLevel.Error, 1)), "Billing").Headline);
    }
}

public class BuiltInChannelTests
{
    private sealed class CountingAudio : IAudioPlayer
    {
        public int Plays { get; private set; }

        public Task PlayAlertAsync(CancellationToken cancellationToken)
        {
            Plays++;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task The_sound_channel_plays_once_for_each_delivery()
    {
        var audio = new CountingAudio();
        var sound = new SoundNotifier(audio);

        await sound.NotifyAsync(Deliveries.Alert(), CancellationToken.None);
        await sound.NotifyAsync(Deliveries.Summary(5, (EntryLevel.Error, 5)), CancellationToken.None);

        Assert.Equal(2, audio.Plays);
        Assert.False(sound.EnabledByDefault);
    }

    [Fact]
    public async Task The_tray_channel_counts_unread_alerts()
    {
        var counter = new UnreadCounter();
        var tray = new TrayNotifier(counter);

        await tray.NotifyAsync(Deliveries.Alert(), CancellationToken.None);
        await tray.NotifyAsync(Deliveries.Summary(4, (EntryLevel.Error, 4)), CancellationToken.None);

        Assert.Equal(5, counter.Count);
        Assert.True(tray.EnabledByDefault);
    }

    [Fact]
    public async Task The_toast_channel_shows_the_project_the_level_and_the_readable_text()
    {
        var projects = new ProjectService(
            new InMemoryProjectRepository(),
            new TailingService(new InMemoryLogSourceRepository(), new InMemoryReadPositionStore()));
        var project = projects.Create("Billing");
        var stack = new ToastStack();
        var toast = new ToastNotifier(stack, projects);

        await toast.NotifyAsync(Deliveries.Alert("Failed to send", EntryLevel.Fatal, project.Id), CancellationToken.None);

        var shown = Assert.Single(stack.Visible);
        Assert.Equal("Billing", shown.ProjectName);
        Assert.Equal(EntryLevel.Fatal, shown.Level);
        Assert.Equal("Failed to send", shown.Headline);
        Assert.True(toast.EnabledByDefault);
    }

    [Fact]
    public async Task A_toast_for_an_unknown_project_still_shows()
    {
        var projects = new ProjectService(
            new InMemoryProjectRepository(),
            new TailingService(new InMemoryLogSourceRepository(), new InMemoryReadPositionStore()));
        var stack = new ToastStack();

        await new ToastNotifier(stack, projects).NotifyAsync(Deliveries.Alert(project: 999), CancellationToken.None);

        Assert.Equal("Unknown project", stack.Visible.Single().ProjectName);
    }

    [Fact]
    public void Channel_settings_use_the_channel_default_until_the_user_changes_them_and_are_remembered()
    {
        var repository = new InMemorySettingsRepository();
        var settings = new AppSettings(repository);

        Assert.True(settings.IsChannelEnabled(ChannelIds.Toast, defaultValue: true));
        Assert.False(settings.IsChannelEnabled(ChannelIds.Sound, defaultValue: false));

        settings.SetChannelEnabled(ChannelIds.Toast, false);
        settings.SetChannelEnabled(ChannelIds.Sound, true);

        var reopened = new AppSettings(repository);
        Assert.False(reopened.IsChannelEnabled(ChannelIds.Toast, defaultValue: true));
        Assert.True(reopened.IsChannelEnabled(ChannelIds.Sound, defaultValue: false));
    }
}
