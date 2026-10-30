using MothEidol.Core.Alerts;
using MothEidol.Core.Logs;

namespace MothEidol.Core.Notifications;

/// <summary>
/// Hands every alert delivery to each enabled channel, independently: a channel that fails never keeps
/// the others from delivering, and its failure is recorded.
/// </summary>
public sealed class NotificationDispatcher : IAsyncDisposable
{
    private readonly object _gate = new();
    private readonly List<INotifier> _notifiers = [];
    private readonly Dictionary<string, Exception> _lastErrors = [];
    private readonly Func<INotifier, bool> _isEnabled;
    private readonly CancellationTokenSource _cancellation = new();
    private Subscription<AlertDelivery>? _subscription;
    private Task _reader = Task.CompletedTask;

    /// <param name="isEnabled">Whether a channel is switched on; asked at every delivery so changes apply at once.</param>
    public NotificationDispatcher(Func<INotifier, bool> isEnabled)
    {
        _isEnabled = isEnabled;
    }

    /// <summary>Raised from a background thread when a channel throws while delivering.</summary>
    public event EventHandler<ChannelFailedEventArgs>? ChannelFailed;

    public IReadOnlyList<INotifier> Notifiers
    {
        get { lock (_gate) return [.. _notifiers]; }
    }

    /// <summary>Adds a channel; it receives deliveries through the same path as the built-in ones.</summary>
    public void Register(INotifier notifier)
    {
        lock (_gate) _notifiers.Add(notifier);
    }

    public Exception? LastError(string channelId)
    {
        lock (_gate) return _lastErrors.GetValueOrDefault(channelId);
    }

    /// <summary>Starts delivering what the alert pipeline produces.</summary>
    public void Start(DeliveryBus deliveries)
    {
        _subscription = deliveries.Subscribe();
        _reader = Task.Run(() => ReadAsync(_subscription, _cancellation.Token));
    }

    public async Task DispatchAsync(AlertDelivery delivery, CancellationToken cancellationToken = default)
    {
        var targets = Notifiers.Where(_isEnabled).ToList();
        await Task.WhenAll(targets.Select(n => DeliverAsync(n, delivery, cancellationToken)));
    }

    public async ValueTask DisposeAsync()
    {
        await _cancellation.CancelAsync();
        _subscription?.Dispose();
        try { await _reader; }
        catch (OperationCanceledException) { }
        _cancellation.Dispose();
    }

    private async Task ReadAsync(Subscription<AlertDelivery> subscription, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var delivery in subscription.Reader.ReadAllAsync(cancellationToken))
                await DispatchAsync(delivery, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Normal stop.
        }
    }

    private async Task DeliverAsync(INotifier notifier, AlertDelivery delivery, CancellationToken cancellationToken)
    {
        try
        {
            await notifier.NotifyAsync(delivery, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            lock (_gate) _lastErrors[notifier.Id] = ex;
            ChannelFailed?.Invoke(this, new ChannelFailedEventArgs(notifier.Id, ex));
        }
    }
}
