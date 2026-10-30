namespace MothEidol.Core.Logs;

/// <summary>
/// Assembles the raw lines published on a <see cref="LineBus"/> into entries published on <see cref="Entries"/>,
/// one assembler per source, flushing the last entry of a burst after the idle timeout.
/// </summary>
public sealed class EntryPipeline : IAsyncDisposable
{
    private readonly LineBus _lines;
    private readonly EntryOptions _options;
    private readonly Func<long, ILogEntryParser> _parserFor;
    private readonly object _gate = new();
    private readonly Dictionary<long, EntryAssembler> _assemblers = [];
    private readonly CancellationTokenSource _cancellation = new();
    private Subscription<LogLine>? _subscription;
    private Task _reader = Task.CompletedTask;
    private Task _flusher = Task.CompletedTask;

    /// <param name="parserFor">Chooses the parser of a source; the Serilog text parser by default.</param>
    public EntryPipeline(LineBus lines, EntryOptions? options = null, Func<long, ILogEntryParser>? parserFor = null)
    {
        _lines = lines;
        _options = options ?? new EntryOptions();
        _parserFor = parserFor ?? (_ => SerilogTextParser.Instance);
    }

    public EntryBus Entries { get; } = new();

    /// <summary>Subscribes to the lines. Call it before the sources start so no line is missed.</summary>
    public void Start()
    {
        _subscription = _lines.Subscribe();
        _reader = Task.Run(() => ReadAsync(_subscription, _cancellation.Token));
        _flusher = Task.Run(() => FlushAsync(_cancellation.Token));
    }

    public async ValueTask DisposeAsync()
    {
        await _cancellation.CancelAsync();
        _subscription?.Dispose();
        try { await Task.WhenAll(_reader, _flusher); }
        catch (OperationCanceledException) { }
        _cancellation.Dispose();
    }

    private async Task ReadAsync(Subscription<LogLine> subscription, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var line in subscription.Reader.ReadAllAsync(cancellationToken))
            {
                LogEntry? entry;
                lock (_gate) entry = AssemblerFor(line.SourceId).Add(line.Text, line.ReadAt);
                if (entry is not null) await Entries.PublishAsync([entry], cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal stop.
        }
    }

    private async Task FlushAsync(CancellationToken cancellationToken)
    {
        var interval = TimeSpan.FromMilliseconds(Math.Clamp(_options.IdleTimeout.TotalMilliseconds / 4, 10, 200));
        using var timer = new PeriodicTimer(interval);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                List<LogEntry> idle = [];
                lock (_gate)
                {
                    var now = DateTimeOffset.Now;
                    foreach (var assembler in _assemblers.Values)
                    {
                        if (assembler.FlushIfIdle(now) is { } entry) idle.Add(entry);
                    }
                }
                if (idle.Count > 0) await Entries.PublishAsync(idle, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal stop.
        }
    }

    private EntryAssembler AssemblerFor(long sourceId)
    {
        if (!_assemblers.TryGetValue(sourceId, out var assembler))
        {
            assembler = new EntryAssembler(sourceId, _parserFor(sourceId), _options);
            _assemblers[sourceId] = assembler;
        }
        return assembler;
    }
}
