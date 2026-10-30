using MothEidol.Core.Projects;

namespace MothEidol.Core.Alerts;

public enum DeliveryKind
{
    /// <summary>One alert-level entry, delivered individually.</summary>
    Alert,

    /// <summary>Several alerts folded into one ("+37 more", or the periodic summary).</summary>
    Summary,
}

public sealed record AlertSummary(
    int Count,
    IReadOnlyDictionary<MothEidol.Core.Logs.EntryLevel, int> ByLevel,
    IReadOnlyList<AlertEvent> Samples,
    DateTimeOffset From,
    DateTimeOffset To);

/// <summary>What the notification channels receive: either an alert or a summary of several.</summary>
public sealed record AlertDelivery(long ProjectId, DeliveryKind Kind, AlertEvent? Alert, AlertSummary? Summary);

/// <summary>Decides when alerts of one project are delivered. Timeless: the caller passes the clock.</summary>
public interface IFrequencyPolicy
{
    IReadOnlyList<AlertDelivery> OnAlert(AlertEvent alert, DateTimeOffset now);

    /// <summary>Called periodically so cooldowns and intervals can end without a new alert.</summary>
    IReadOnlyList<AlertDelivery> OnTick(DateTimeOffset now);

    /// <summary>Delivers whatever is waiting (used when the settings change).</summary>
    IReadOnlyList<AlertDelivery> Flush(DateTimeOffset now);
}

/// <summary>Accumulates alerts into a summary; shared by both policies.</summary>
internal sealed class SummaryBuilder(long projectId)
{
    private const int MaxSamples = 5;
    private readonly Dictionary<MothEidol.Core.Logs.EntryLevel, int> _byLevel = [];
    private readonly List<AlertEvent> _samples = [];
    private DateTimeOffset _from;
    private DateTimeOffset _to;

    public int Count { get; private set; }

    public void Add(AlertEvent alert)
    {
        if (Count == 0) _from = alert.At;
        _to = alert.At;
        Count++;
        _byLevel[alert.Level] = _byLevel.GetValueOrDefault(alert.Level) + 1;
        if (_samples.Count < MaxSamples) _samples.Add(alert);
    }

    public AlertDelivery? Build()
    {
        if (Count == 0) return null;

        var summary = new AlertSummary(Count, new Dictionary<MothEidol.Core.Logs.EntryLevel, int>(_byLevel), [.. _samples], _from, _to);
        Count = 0;
        _byLevel.Clear();
        _samples.Clear();
        return new AlertDelivery(projectId, DeliveryKind.Summary, null, summary);
    }
}

/// <summary>
/// The first alert is delivered at once; the following ones are counted until the cooldown ends, when one
/// summary ("+N more") is delivered.
/// </summary>
public sealed class ImmediateCooldownPolicy(long projectId, TimeSpan cooldown) : IFrequencyPolicy
{
    private readonly SummaryBuilder _suppressed = new(projectId);
    private DateTimeOffset? _cooldownUntil;

    public IReadOnlyList<AlertDelivery> OnAlert(AlertEvent alert, DateTimeOffset now)
    {
        if (_cooldownUntil is { } until && now < until)
        {
            _suppressed.Add(alert);
            return [];
        }

        _cooldownUntil = now + cooldown;
        return [new AlertDelivery(projectId, DeliveryKind.Alert, alert, null)];
    }

    public IReadOnlyList<AlertDelivery> OnTick(DateTimeOffset now)
    {
        if (_cooldownUntil is not { } until || now < until) return [];

        _cooldownUntil = null;
        return _suppressed.Build() is { } summary ? [summary] : [];
    }

    public IReadOnlyList<AlertDelivery> Flush(DateTimeOffset now)
    {
        _cooldownUntil = null;
        return _suppressed.Build() is { } summary ? [summary] : [];
    }
}

/// <summary>One aggregated delivery per interval, only when there were alerts in it.</summary>
public sealed class PeriodicSummaryPolicy(long projectId, TimeSpan interval) : IFrequencyPolicy
{
    private readonly SummaryBuilder _window = new(projectId);
    private DateTimeOffset? _windowEnd;

    public IReadOnlyList<AlertDelivery> OnAlert(AlertEvent alert, DateTimeOffset now)
    {
        _windowEnd ??= now + interval;
        _window.Add(alert);
        return [];
    }

    public IReadOnlyList<AlertDelivery> OnTick(DateTimeOffset now)
    {
        if (_windowEnd is not { } end || now < end) return [];

        _windowEnd = null;
        return _window.Build() is { } summary ? [summary] : [];
    }

    public IReadOnlyList<AlertDelivery> Flush(DateTimeOffset now)
    {
        _windowEnd = null;
        return _window.Build() is { } summary ? [summary] : [];
    }
}

/// <summary>Keeps one policy per project and swaps it when the project's settings change.</summary>
public sealed class FrequencyPolicySet
{
    private readonly Dictionary<long, (AlertSettings Settings, IFrequencyPolicy Policy)> _policies = [];

    public IReadOnlyList<AlertDelivery> OnAlert(AlertEvent alert, AlertSettings settings, DateTimeOffset now)
    {
        var flushed = Reconfigure(alert.ProjectId, settings, now);
        var deliveries = _policies[alert.ProjectId].Policy.OnAlert(alert, now);
        return [.. flushed, .. deliveries];
    }

    public IReadOnlyList<AlertDelivery> OnTick(DateTimeOffset now) =>
        [.. _policies.Values.SelectMany(p => p.Policy.OnTick(now))];

    /// <summary>Makes the project's policy match its settings, delivering what the old one was holding.</summary>
    public IReadOnlyList<AlertDelivery> Reconfigure(long projectId, AlertSettings settings, DateTimeOffset now)
    {
        if (_policies.TryGetValue(projectId, out var current))
        {
            var sameShape = current.Settings.Mode == settings.Mode && current.Settings.Minutes == settings.Minutes;
            if (sameShape) return [];

            var flushed = current.Policy.Flush(now);
            _policies[projectId] = (settings, Create(projectId, settings));
            return flushed;
        }

        _policies[projectId] = (settings, Create(projectId, settings));
        return [];
    }

    public void Remove(long projectId) => _policies.Remove(projectId);

    private static IFrequencyPolicy Create(long projectId, AlertSettings settings)
    {
        var span = TimeSpan.FromMinutes(settings.Minutes);
        return settings.Mode == FrequencyMode.Summary
            ? new PeriodicSummaryPolicy(projectId, span)
            : new ImmediateCooldownPolicy(projectId, span);
    }
}
