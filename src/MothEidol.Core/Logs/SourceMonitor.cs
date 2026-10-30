namespace MothEidol.Core.Logs;

/// <summary>Runs one source's polling loop in the background, isolated from every other source.</summary>
internal sealed class SourceMonitor
{
    private readonly LogSource _source;
    private readonly FileCursor? _initialCursor;
    private readonly TailOptions _options;
    private readonly LineBus _bus;
    private readonly IReadPositionStore _positions;
    private readonly Action<long, SourceStatus, string?> _report;
    private readonly CancellationTokenSource _cancellation = new();
    private Task _task = Task.CompletedTask;

    public SourceMonitor(
        LogSource source,
        FileCursor? cursor,
        TailOptions options,
        LineBus bus,
        IReadPositionStore positions,
        Action<long, SourceStatus, string?> report)
    {
        _source = source;
        _initialCursor = cursor;
        _options = options;
        _bus = bus;
        _positions = positions;
        _report = report;
    }

    public void Start() => _task = Task.Run(() => RunAsync(_cancellation.Token));

    public async Task StopAsync()
    {
        await _cancellation.CancelAsync();
        await _task;
        _cancellation.Dispose();
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        var tailer = new SourceTailer(_source, _initialCursor);
        FileCursor? published = _initialCursor;
        FileCursor? saved = _initialCursor;
        var lastSave = DateTime.UtcNow;
        SourceStatus? lastStatus = null;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var result = tailer.Poll(_options.MaxBytesPerCycle);

                if (result.Lines.Count > 0)
                {
                    var now = DateTimeOffset.Now;
                    await _bus.PublishAsync([.. result.Lines.Select(text => new LogLine(_source.Id, text, now))], cancellationToken);
                }

                // Only what was actually published counts as read, so a stop never skips lines.
                published = tailer.Cursor;

                if (result.Status != lastStatus)
                {
                    lastStatus = result.Status;
                    _report(_source.Id, result.Status, result.Error);
                }

                if (published != saved && DateTime.UtcNow - lastSave >= _options.SaveInterval)
                {
                    saved = TrySave(published, saved);
                    lastSave = DateTime.UtcNow;
                }

                if (result.HasMore)
                {
                    await Task.Yield();
                    continue;
                }

                var delay = result.Status is SourceStatus.Unavailable or SourceStatus.ReadError
                    ? _options.RetryInterval
                    : _options.PollInterval;
                await Task.Delay(delay, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal stop.
        }
        finally
        {
            if (published != saved) TrySave(published, saved);
        }
    }

    private FileCursor? TrySave(FileCursor? cursor, FileCursor? previous)
    {
        if (cursor is null) return previous;
        try
        {
            _positions.Save(_source.Id, cursor);
            return cursor;
        }
        catch (Exception)
        {
            return previous; // Storage trouble must not stop monitoring; the next save retries.
        }
    }
}
