using MothEidol.Core.Alerts;
using MothEidol.Core.Logs;
using MothEidol.Core.Projects;

namespace MothEidol.Core.Tests.Alerts;

public class FrequencyPolicyTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

    private static AlertEvent Alert(int second, EntryLevel level = EntryLevel.Error, long project = 1) =>
        new(project, 10, level, $"failure {second}", T0 + TimeSpan.FromSeconds(second));

    [Fact]
    public void Immediate_mode_delivers_the_first_alert_at_once()
    {
        var policy = new ImmediateCooldownPolicy(1, TimeSpan.FromMinutes(5));

        var deliveries = policy.OnAlert(Alert(0), T0);

        var delivery = Assert.Single(deliveries);
        Assert.Equal(DeliveryKind.Alert, delivery.Kind);
        Assert.Equal("failure 0", delivery.Alert!.Message);
    }

    [Fact]
    public void Alerts_during_the_cooldown_are_held_back()
    {
        var policy = new ImmediateCooldownPolicy(1, TimeSpan.FromMinutes(5));
        policy.OnAlert(Alert(0), T0);

        Assert.Empty(policy.OnAlert(Alert(10), T0.AddSeconds(10)));
        Assert.Empty(policy.OnAlert(Alert(20), T0.AddSeconds(20)));
        Assert.Empty(policy.OnTick(T0.AddMinutes(4)));
    }

    [Fact]
    public void When_the_cooldown_ends_one_summary_with_the_suppressed_count_is_delivered()
    {
        var policy = new ImmediateCooldownPolicy(1, TimeSpan.FromMinutes(5));
        policy.OnAlert(Alert(0), T0);
        for (var i = 1; i <= 37; i++) policy.OnAlert(Alert(i), T0.AddSeconds(i));

        var deliveries = policy.OnTick(T0.AddMinutes(5));

        var summary = Assert.Single(deliveries);
        Assert.Equal(DeliveryKind.Summary, summary.Kind);
        Assert.Equal(37, summary.Summary!.Count);
        Assert.Equal(37, summary.Summary.ByLevel[EntryLevel.Error]);
        Assert.Equal(5, summary.Summary.Samples.Count);
        Assert.Empty(policy.OnTick(T0.AddMinutes(10)));
    }

    [Fact]
    public void A_cooldown_that_ends_without_suppressed_alerts_delivers_nothing_and_the_next_alert_is_immediate()
    {
        var policy = new ImmediateCooldownPolicy(1, TimeSpan.FromMinutes(5));
        policy.OnAlert(Alert(0), T0);

        Assert.Empty(policy.OnTick(T0.AddMinutes(5)));

        Assert.Single(policy.OnAlert(Alert(400), T0.AddMinutes(6)));
    }

    [Fact]
    public void Summary_mode_waits_for_the_interval_and_then_delivers_one_aggregated_alert()
    {
        var policy = new PeriodicSummaryPolicy(1, TimeSpan.FromMinutes(15));

        Assert.Empty(policy.OnAlert(Alert(0, EntryLevel.Error), T0));
        Assert.Empty(policy.OnAlert(Alert(30, EntryLevel.Warning), T0.AddSeconds(30)));
        Assert.Empty(policy.OnAlert(Alert(60, EntryLevel.Error), T0.AddSeconds(60)));
        Assert.Empty(policy.OnTick(T0.AddMinutes(14)));

        var summary = Assert.Single(policy.OnTick(T0.AddMinutes(15))).Summary!;
        Assert.Equal(3, summary.Count);
        Assert.Equal(2, summary.ByLevel[EntryLevel.Error]);
        Assert.Equal(1, summary.ByLevel[EntryLevel.Warning]);
        Assert.Equal(T0, summary.From);
        Assert.Equal(T0.AddSeconds(60), summary.To);
    }

    [Fact]
    public void Summary_mode_delivers_nothing_for_an_interval_without_alerts()
    {
        var policy = new PeriodicSummaryPolicy(1, TimeSpan.FromMinutes(15));

        Assert.Empty(policy.OnTick(T0.AddMinutes(15)));
        Assert.Empty(policy.OnTick(T0.AddMinutes(30)));
    }

    [Fact]
    public void The_interval_starts_with_the_first_alert_of_each_window()
    {
        var policy = new PeriodicSummaryPolicy(1, TimeSpan.FromMinutes(10));
        policy.OnAlert(Alert(0), T0);
        Assert.Single(policy.OnTick(T0.AddMinutes(10)));

        policy.OnAlert(Alert(1000), T0.AddMinutes(30));

        Assert.Empty(policy.OnTick(T0.AddMinutes(35)));
        Assert.Single(policy.OnTick(T0.AddMinutes(40)));
    }

    [Fact]
    public void Each_project_follows_its_own_mode_interval_and_cooldown()
    {
        var set = new FrequencyPolicySet();
        var immediate = AlertSettings.Default with { Minutes = 5 };
        var summary = new AlertSettings(false, FrequencyMode.Summary, 15);

        var first = set.OnAlert(Alert(0, project: 1), immediate, T0);
        var second = set.OnAlert(Alert(0, project: 2), summary, T0);

        Assert.Single(first);
        Assert.Empty(second);
        Assert.Empty(set.OnTick(T0.AddMinutes(5)));
        var due = Assert.Single(set.OnTick(T0.AddMinutes(15)));
        Assert.Equal(2, due.ProjectId);
        Assert.Equal(DeliveryKind.Summary, due.Kind);
    }

    [Fact]
    public void Changing_the_settings_delivers_what_the_old_policy_held_and_applies_the_new_one()
    {
        var set = new FrequencyPolicySet();
        var summary = new AlertSettings(false, FrequencyMode.Summary, 60);
        set.OnAlert(Alert(0), summary, T0);
        set.OnAlert(Alert(5), summary, T0.AddSeconds(5));

        var flushed = set.Reconfigure(1, AlertSettings.Default, T0.AddMinutes(1));

        var delivery = Assert.Single(flushed);
        Assert.Equal(2, delivery.Summary!.Count);
        Assert.Single(set.OnAlert(Alert(70), AlertSettings.Default, T0.AddMinutes(2)));
    }

    [Fact]
    public void Unchanged_settings_keep_the_running_policy()
    {
        var set = new FrequencyPolicySet();
        set.OnAlert(Alert(0), AlertSettings.Default, T0);

        Assert.Empty(set.Reconfigure(1, AlertSettings.Default with { WarningAlerts = true }, T0.AddSeconds(1)));
        Assert.Empty(set.OnAlert(Alert(2), AlertSettings.Default, T0.AddSeconds(2))); // still cooling down
    }
}
