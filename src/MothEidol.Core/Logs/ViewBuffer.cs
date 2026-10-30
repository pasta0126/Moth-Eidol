namespace MothEidol.Core.Logs;

/// <summary>
/// The most recent lines of a source, bounded in size. Readers ask for what is new since a sequence number,
/// so a view can append incrementally and never needs the whole history.
/// </summary>
public sealed class ViewBuffer<T>(int maxLines)
{
    private readonly object _gate = new();
    private readonly Queue<T> _lines = new();
    private long _firstSequence;
    private int _maxLines = Math.Max(1, maxLines);

    public int MaxLines
    {
        get { lock (_gate) return _maxLines; }
        set
        {
            lock (_gate)
            {
                _maxLines = Math.Max(1, value);
                Trim();
            }
        }
    }

    /// <summary>Sequence number the next added line will get.</summary>
    public long NextSequence
    {
        get { lock (_gate) return _firstSequence + _lines.Count; }
    }

    public void Add(T line)
    {
        lock (_gate)
        {
            _lines.Enqueue(line);
            Trim();
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _firstSequence += _lines.Count;
            _lines.Clear();
        }
    }

    /// <summary>Lines added at or after <paramref name="sequence"/> that are still retained, and the sequence to ask for next.</summary>
    public (IReadOnlyList<T> Lines, long Next) ReadSince(long sequence)
    {
        lock (_gate)
        {
            var skip = (int)Math.Max(0, Math.Min(sequence - _firstSequence, _lines.Count));
            return ([.. _lines.Skip(skip)], _firstSequence + _lines.Count);
        }
    }

    private void Trim()
    {
        while (_lines.Count > _maxLines)
        {
            _lines.Dequeue();
            _firstSequence++;
        }
    }
}
