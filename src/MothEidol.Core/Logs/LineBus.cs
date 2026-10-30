using System.Threading.Channels;

namespace MothEidol.Core.Logs;

/// <summary>Fan-out of emitted lines: every subscriber gets its own channel, so consumers do not compete.</summary>
public sealed class LineBus
{
    private readonly object _gate = new();
    private readonly List<Channel<LogLine>> _subscribers = [];

    /// <summary>
    /// Subscribes to every line published from now on. A full subscriber slows the producers down
    /// (nothing is dropped); dispose the subscription to stop receiving.
    /// </summary>
    public LineSubscription Subscribe(int capacity = 10_000)
    {
        var channel = Channel.CreateBounded<LogLine>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
        });
        lock (_gate) _subscribers.Add(channel);
        return new LineSubscription(channel, () =>
        {
            lock (_gate) _subscribers.Remove(channel);
            channel.Writer.TryComplete();
        });
    }

    public async ValueTask PublishAsync(IReadOnlyList<LogLine> lines, CancellationToken cancellationToken)
    {
        Channel<LogLine>[] targets;
        lock (_gate) targets = [.. _subscribers];

        foreach (var channel in targets)
        {
            foreach (var line in lines)
            {
                try
                {
                    await channel.Writer.WriteAsync(line, cancellationToken);
                }
                catch (ChannelClosedException)
                {
                    break; // The subscriber went away while we were publishing.
                }
            }
        }
    }
}

public sealed class LineSubscription : IDisposable
{
    private readonly Channel<LogLine> _channel;
    private readonly Action _onDispose;

    internal LineSubscription(Channel<LogLine> channel, Action onDispose)
    {
        _channel = channel;
        _onDispose = onDispose;
    }

    public ChannelReader<LogLine> Reader => _channel.Reader;

    public void Dispose() => _onDispose();
}
