namespace MothEidol.Core.Logs;

public sealed record TailOptions
{
    /// <summary>Delay between polls of a healthy source; the worst-case latency of a new line.</summary>
    public TimeSpan PollInterval { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>Delay between retries of an unavailable source.</summary>
    public TimeSpan RetryInterval { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>Upper bound on bytes read per source per cycle, so write bursts are processed in batches.</summary>
    public int MaxBytesPerCycle { get; init; } = 1024 * 1024;

    /// <summary>How often the read position is persisted (it is always persisted on stop).</summary>
    public TimeSpan SaveInterval { get; init; } = TimeSpan.FromSeconds(2);
}
