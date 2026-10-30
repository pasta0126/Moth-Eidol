using MothEidol.Core.Logs;
using MothEidol.Core.Projects;

namespace MothEidol.Core.Alerts;

/// <summary>Alert deliveries (individual alerts and summaries) for the notification channels.</summary>
public sealed class DeliveryBus : Broadcast<AlertDelivery>;

public sealed record AlertOptions
{
    /// <summary>Occurrences kept with their full text per group; the group count keeps growing beyond it.</summary>
    public int MaxOccurrencesPerGroup { get; init; } = 50;

    /// <summary>How long a seen group is kept without new occurrences.</summary>
    public TimeSpan Retention { get; init; } = TimeSpan.FromDays(30);

    /// <summary>How often cooldowns and intervals are checked.</summary>
    public TimeSpan TickInterval { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>Entries kept before each occurrence to show what led to it.</summary>
    public int ContextEntries { get; init; } = 10;

    /// <summary>Longest text kept per context entry.</summary>
    public int MaxContextTextLength { get; init; } = 600;

    /// <summary>How often old seen groups are purged.</summary>
    public TimeSpan PurgeInterval { get; init; } = TimeSpan.FromHours(1);
}

/// <summary>
/// Turns the entries of enabled projects into recorded event groups (every alert-level entry is recorded,
/// whatever the delivery policy does) and into deliveries shaped by each project's frequency policy.
/// </summary>
public sealed class AlertPipeline : IAsyncDisposable
{
    private readonly EntryBus _entries;
    private readonly ProjectService _projects;
    private readonly IEventStore _store;
    private readonly AlertOptions _options;
    private readonly TimeProvider _time;
    private readonly object _gate = new();
    private readonly FrequencyPolicySet _policies = new();
    private readonly HashSet<long> _knownProjects = [];
    private readonly Dictionary<long, Queue<ContextEntry>> _recent = [];
    private readonly CancellationTokenSource _cancellation = new();
    private Subscription<LogEntry>? _subscription;
    private Task _reader = Task.CompletedTask;
    private Task _ticker = Task.CompletedTask;

    public AlertPipeline(
        EntryBus entries, ProjectService projects, IEventStore store,
        AlertOptions? options = null, TimeProvider? time = null)
    {
        _entries = entries;
        _projects = projects;
        _store = store;
        _options = options ?? new AlertOptions();
        _time = time ?? TimeProvider.System;
    }

    public DeliveryBus Deliveries { get; } = new();

    /// <summary>Subscribes to the entries. Call it before the sources start so nothing is missed.</summary>
    public void Start()
    {
        _subscription = _entries.Subscribe();
        _reader = Task.Run(() => ReadAsync(_subscription, _cancellation.Token));
        _ticker = Task.Run(() => TickAsync(_cancellation.Token));
    }

    public async ValueTask DisposeAsync()
    {
        await _cancellation.CancelAsync();
        _subscription?.Dispose();
        try { await Task.WhenAll(_reader, _ticker); }
        catch (OperationCanceledException) { }
        _cancellation.Dispose();
    }

    private async Task ReadAsync(Subscription<LogEntry> subscription, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var entry in subscription.Reader.ReadAllAsync(cancellationToken))
            {
                IReadOnlyList<AlertDelivery> deliveries;
                try
                {
                    deliveries = Process(entry);
                }
                catch (Exception)
                {
                    continue; // A storage hiccup on one entry must not stop alerting for the rest.
                }
                if (deliveries.Count > 0) await Deliveries.PublishAsync(deliveries, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal stop.
        }
    }

    private IReadOnlyList<AlertDelivery> Process(LogEntry entry)
    {
        if (_projects.ProjectOfSource(entry.SourceId) is not { } projectId) return [];

        var at = entry.Timestamp ?? entry.ReadAt;
        var context = RecentBefore(entry, at);
        if (_projects.Lookup(projectId) is not { Enabled: true } project) return [];
        if (!AlertEvaluator.IsAlertLevel(entry.Level, project.Alerts)) return [];

        var description = EventDescriber.Describe(entry.Message);
        _store.Record(
            projectId, entry.Level, EventGrouping.Key(entry.Level, entry.Message), description.Title,
            entry.SourceId, at, entry.Message, _options.MaxOccurrencesPerGroup, description.Location, context);

        var alert = new AlertEvent(projectId, entry.SourceId, entry.Level, entry.Message, at);
        lock (_gate)
        {
            _knownProjects.Add(projectId);
            return _policies.OnAlert(alert, project.Alerts, _time.GetUtcNow());
        }
    }

    /// <summary>
    /// What this source logged just before the entry, then remembers the entry itself for the ones after it.
    /// Only the reader task calls this, so the rings need no lock.
    /// </summary>
    private IReadOnlyList<ContextEntry> RecentBefore(LogEntry entry, DateTimeOffset at)
    {
        if (!_recent.TryGetValue(entry.SourceId, out var ring)) _recent[entry.SourceId] = ring = new Queue<ContextEntry>();

        var before = ring.ToArray();
        var text = entry.Message.Length > _options.MaxContextTextLength
            ? entry.Message[.._options.MaxContextTextLength] + "…"
            : entry.Message;
        ring.Enqueue(new ContextEntry(at, entry.Level, text));
        while (ring.Count > _options.ContextEntries) ring.Dequeue();
        return before;
    }

    private async Task TickAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(_options.TickInterval);
        var lastPurge = DateTimeOffset.MinValue;
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                var now = _time.GetUtcNow();
                List<AlertDelivery> due = [];
                lock (_gate)
                {
                    // Settings may have changed: make each policy match its project, flushing what it held.
                    foreach (var id in _knownProjects.ToList())
                    {
                        if (_projects.Lookup(id) is { } project) due.AddRange(_policies.Reconfigure(id, project.Alerts, now));
                        else { _policies.Remove(id); _knownProjects.Remove(id); }
                    }
                    due.AddRange(_policies.OnTick(now));
                }
                if (due.Count > 0) await Deliveries.PublishAsync(due, cancellationToken);

                if (now - lastPurge >= _options.PurgeInterval)
                {
                    lastPurge = now;
                    try
                    {
                        _store.PurgeQuiet(now - _options.Retention);
                        _store.PruneBuckets(now - TimeSpan.FromDays(8)); // The timeline reaches back 7 days.
                    }
                    catch (Exception) { /* Retry on the next purge. */ }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal stop.
        }
    }
}
