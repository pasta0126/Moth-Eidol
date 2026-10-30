using System.Threading.Channels;

namespace MothEidol.Core.Logs;

/// <summary>Fan-out of published items: every subscriber gets its own channel, so consumers do not compete.</summary>
public class Broadcast<T>
{
    private readonly object _gate = new();
    private readonly List<Channel<T>> _subscribers = [];

    /// <summary>
    /// Subscribes to every item published from now on. A full subscriber slows the producers down
    /// (nothing is dropped); dispose the subscription to stop receiving.
    /// </summary>
    public Subscription<T> Subscribe(int capacity = 10_000)
    {
        var channel = Channel.CreateBounded<T>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
        });
        lock (_gate) _subscribers.Add(channel);
        return new Subscription<T>(channel, () =>
        {
            lock (_gate) _subscribers.Remove(channel);
            channel.Writer.TryComplete();
        });
    }

    public async ValueTask PublishAsync(IReadOnlyList<T> items, CancellationToken cancellationToken)
    {
        Channel<T>[] targets;
        lock (_gate) targets = [.. _subscribers];

        foreach (var channel in targets)
        {
            foreach (var item in items)
            {
                try
                {
                    await channel.Writer.WriteAsync(item, cancellationToken);
                }
                catch (ChannelClosedException)
                {
                    break; // The subscriber went away while we were publishing.
                }
            }
        }
    }
}

public sealed class Subscription<T> : IDisposable
{
    private readonly Channel<T> _channel;
    private readonly Action _onDispose;

    internal Subscription(Channel<T> channel, Action onDispose)
    {
        _channel = channel;
        _onDispose = onDispose;
    }

    public ChannelReader<T> Reader => _channel.Reader;

    public void Dispose() => _onDispose();
}
