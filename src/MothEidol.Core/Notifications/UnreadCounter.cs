using MothEidol.Core.Alerts;

namespace MothEidol.Core.Notifications;

/// <summary>Alerts delivered and not yet acknowledged, for the tray icon.</summary>
public sealed class UnreadCounter
{
    private readonly object _gate = new();
    private long _count;

    public long Count
    {
        get { lock (_gate) return _count; }
    }

    /// <summary>Raised (from whichever thread changed it) when the count changes.</summary>
    public event EventHandler? Changed;

    /// <summary>An individual alert counts one; a summary counts the alerts it stands for.</summary>
    public void Add(AlertDelivery delivery)
    {
        var amount = delivery.Kind == DeliveryKind.Summary ? delivery.Summary?.Count ?? 1 : 1;
        lock (_gate) _count += Math.Max(1, amount);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>The user has seen the alerts: the counter and the icon return to normal.</summary>
    public void Acknowledge()
    {
        lock (_gate)
        {
            if (_count == 0) return;
            _count = 0;
        }
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
