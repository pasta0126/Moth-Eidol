using MothEidol.Core.Logs;

namespace MothEidol.Core.Notifications;

public sealed record ToastItem(long Id, string ProjectName, EntryLevel Level, string Headline, string? Detail, DateTimeOffset CreatedAt);

/// <summary>
/// The toasts on screen: at most <c>maxVisible</c> at once, the oldest ones folded into a "+N more" counter,
/// and each one removed when dismissed or when its display time is over. It holds state only, so the
/// stacking rules can be tested without any window.
/// </summary>
public sealed class ToastStack(int maxVisible = 4, TimeSpan? timeToLive = null)
{
    private readonly object _gate = new();
    private readonly List<ToastItem> _visible = [];
    private readonly TimeSpan _timeToLive = timeToLive ?? TimeSpan.FromSeconds(8);
    private int _overflow;
    private long _nextId = 1;

    /// <summary>Raised (from whichever thread changed it) when what is on screen changes.</summary>
    public event EventHandler? Changed;

    /// <summary>The toasts to show, oldest first.</summary>
    public IReadOnlyList<ToastItem> Visible
    {
        get { lock (_gate) return [.. _visible]; }
    }

    /// <summary>How many older toasts were folded into the "+N more" toast.</summary>
    public int Overflow
    {
        get { lock (_gate) return _overflow; }
    }

    public ToastItem Add(string projectName, EntryLevel level, string headline, string? detail, DateTimeOffset now)
    {
        ToastItem item;
        lock (_gate)
        {
            item = new ToastItem(_nextId++, projectName, level, headline, detail, now);
            _visible.Add(item);
            while (_visible.Count > maxVisible)
            {
                _visible.RemoveAt(0);
                _overflow++;
            }
        }
        Changed?.Invoke(this, EventArgs.Empty);
        return item;
    }

    public void Dismiss(long id)
    {
        bool removed;
        lock (_gate)
        {
            removed = _visible.RemoveAll(t => t.Id == id) > 0;
            if (_visible.Count == 0) _overflow = 0;
        }
        if (removed) Changed?.Invoke(this, EventArgs.Empty);
    }

    public void DismissOverflow()
    {
        lock (_gate)
        {
            if (_overflow == 0) return;
            _overflow = 0;
        }
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Removes the toasts that have been shown for their full time.</summary>
    public void Expire(DateTimeOffset now)
    {
        bool changed;
        lock (_gate)
        {
            changed = _visible.RemoveAll(t => now - t.CreatedAt >= _timeToLive) > 0;
            if (_visible.Count == 0 && _overflow > 0)
            {
                _overflow = 0;
                changed = true;
            }
        }
        if (changed) Changed?.Invoke(this, EventArgs.Empty);
    }
}
