using MothEidol.Core.Logs;

namespace MothEidol.Core.Tests.Logs;

public class EntryPipelineTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
    private static readonly EntryOptions Fast = new() { IdleTimeout = TimeSpan.FromMilliseconds(80) };

    private static string Header(string level, string message) =>
        $"2026-01-01 10:00:00.000 +00:00 [{level}] {message}";

    private static Task Publish(LineBus bus, long sourceId, params string[] lines) =>
        bus.PublishAsync([.. lines.Select(l => new LogLine(sourceId, l, DateTimeOffset.Now))], CancellationToken.None).AsTask();

    private static async Task<List<LogEntry>> ReadAsync(Subscription<LogEntry> subscription, int count)
    {
        using var cancellation = new CancellationTokenSource(Timeout);
        var entries = new List<LogEntry>();
        await foreach (var entry in subscription.Reader.ReadAllAsync(cancellation.Token))
        {
            entries.Add(entry);
            if (entries.Count == count) break;
        }
        return entries;
    }

    [Fact]
    public async Task Lines_become_grouped_entries_and_the_last_one_is_flushed_when_idle()
    {
        var lines = new LineBus();
        await using var pipeline = new EntryPipeline(lines, Fast);
        pipeline.Start();
        using var entries = pipeline.Entries.Subscribe();

        await Publish(lines, 1,
            Header("INF", "started"),
            Header("ERR", "failed"),
            "System.Exception: boom",
            "   at Frame()");

        var received = await ReadAsync(entries, 2);
        Assert.Equal(["started", "failed\nSystem.Exception: boom\n   at Frame()"], received.Select(e => e.Message));
        Assert.Equal([EntryLevel.Information, EntryLevel.Error], received.Select(e => e.Level));
    }

    [Fact]
    public async Task Each_source_is_assembled_separately()
    {
        var lines = new LineBus();
        await using var pipeline = new EntryPipeline(lines, Fast);
        pipeline.Start();
        using var entries = pipeline.Entries.Subscribe();

        await Publish(lines, 1, Header("ERR", "from one"));
        await Publish(lines, 2, Header("WRN", "from two"));
        await Publish(lines, 1, "   at One.Frame()");

        var received = (await ReadAsync(entries, 2)).OrderBy(e => e.SourceId).ToList();
        Assert.Equal("from one\n   at One.Frame()", received[0].Message);
        Assert.Equal("from two", received[1].Message);
    }

    [Fact]
    public async Task A_parser_can_be_chosen_per_source_and_feeds_the_same_stream()
    {
        var lines = new LineBus();
        await using var pipeline = new EntryPipeline(lines, Fast, sourceId => sourceId == 2 ? new EveryLineParser() : SerilogTextParser.Instance);
        pipeline.Start();
        using var entries = pipeline.Entries.Subscribe();

        await Publish(lines, 2, "plain one", "plain two");
        await Publish(lines, 1, Header("INF", "serilog"));

        var received = await ReadAsync(entries, 3);
        Assert.Equal(["plain one", "plain two", "serilog"], received.Select(e => e.Message).Order());
        Assert.All(received.Where(e => e.SourceId == 2), e => Assert.Equal(EntryLevel.Warning, e.Level));
    }

    [Fact]
    public async Task Entries_flow_end_to_end_from_a_growing_file()
    {
        var folder = Path.Combine(Path.GetTempPath(), "moth-pipe-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var path = Path.Combine(folder, "app.log");
            File.WriteAllText(path, Header("INF", "before") + "\r\n");
            var tail = new TailOptions { PollInterval = TimeSpan.FromMilliseconds(20), RetryInterval = TimeSpan.FromMilliseconds(40) };
            await using var service = new TailingService(new InMemoryLogSourceRepository(), new InMemoryReadPositionStore(), tail);
            await using var pipeline = new EntryPipeline(service.Lines, Fast);
            pipeline.Start();
            using var entries = pipeline.Entries.Subscribe();
            service.Add(SourceKind.File, path, null);
            await Task.Delay(150);

            File.AppendAllText(path, Header("ERR", "boom") + "\r\n   at Frame()\r\n" + Header("INF", "after") + "\r\n");

            var received = await ReadAsync(entries, 2);
            Assert.Equal(["boom\n   at Frame()", "after"], received.Select(e => e.Message));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    /// <summary>A format where every line is its own entry.</summary>
    private sealed class EveryLineParser : ILogEntryParser
    {
        public bool TryParseHeader(string line, out EntryHeader header)
        {
            header = new EntryHeader(null, EntryLevel.Warning, line);
            return true;
        }
    }
}
