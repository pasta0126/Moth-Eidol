using MothEidol.Core.Logs;

namespace MothEidol.Core.Tests.Logs;

public class EntryAssemblerTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

    private static EntryAssembler Create(EntryOptions? options = null) =>
        new(7, SerilogTextParser.Instance, options ?? new EntryOptions());

    private static string Header(string level, string message, int second = 0) =>
        $"2026-01-01 10:00:{second:00}.000 +00:00 [{level}] {message}";

    private static List<LogEntry> Feed(EntryAssembler assembler, params string[] lines)
    {
        var entries = new List<LogEntry>();
        foreach (var line in lines)
        {
            if (assembler.Add(line, T0) is { } entry) entries.Add(entry);
        }
        return entries;
    }

    [Fact]
    public void An_entry_is_emitted_when_the_next_header_arrives()
    {
        var assembler = Create();

        var entries = Feed(assembler, Header("INF", "first"), Header("WRN", "second", 1));

        var entry = Assert.Single(entries);
        Assert.Equal("first", entry.Message);
        Assert.Equal(EntryLevel.Information, entry.Level);
        Assert.Equal(7, entry.SourceId);
    }

    [Fact]
    public void A_stack_trace_stays_attached_to_its_error_in_order()
    {
        var assembler = Create();

        var entries = Feed(assembler,
            Header("ERR", "Failed to send"),
            "System.InvalidOperationException: boom",
            "   at App.Worker.Run() in C:\\src\\Worker.cs:line 60",
            "   at App.Program.Main()",
            Header("INF", "next", 1));

        var entry = Assert.Single(entries);
        Assert.Equal(
            "Failed to send\nSystem.InvalidOperationException: boom\n   at App.Worker.Run() in C:\\src\\Worker.cs:line 60\n   at App.Program.Main()",
            entry.Message);
        Assert.Equal("Failed to send", entry.FirstLine);
        Assert.Equal(3, entry.ExtraLines);
        Assert.False(entry.Truncated);
    }

    [Fact]
    public void Lines_before_the_first_header_form_an_entry_with_unknown_level_and_time()
    {
        var assembler = Create();

        var entries = Feed(assembler, "   at Leftover.Frame()", "   at Another.Frame()", Header("INF", "real", 1));

        var entry = Assert.Single(entries);
        Assert.Equal(EntryLevel.Unknown, entry.Level);
        Assert.Null(entry.Timestamp);
        Assert.Equal("   at Leftover.Frame()\n   at Another.Frame()", entry.Message);
    }

    [Fact]
    public void The_last_entry_is_emitted_after_the_idle_timeout_and_not_before()
    {
        var assembler = Create(new EntryOptions { IdleTimeout = TimeSpan.FromMilliseconds(500) });
        Feed(assembler, Header("ERR", "last one"), "   at Frame()");

        Assert.Null(assembler.FlushIfIdle(T0 + TimeSpan.FromMilliseconds(400)));

        var entry = assembler.FlushIfIdle(T0 + TimeSpan.FromMilliseconds(500));
        Assert.NotNull(entry);
        Assert.Equal("last one\n   at Frame()", entry.Message);
        Assert.Null(assembler.FlushIfIdle(T0 + TimeSpan.FromSeconds(10)));
    }

    [Fact]
    public void A_new_line_resets_the_idle_clock()
    {
        var assembler = Create(new EntryOptions { IdleTimeout = TimeSpan.FromMilliseconds(500) });
        assembler.Add(Header("ERR", "slow writer"), T0);
        assembler.Add("   at Frame()", T0 + TimeSpan.FromMilliseconds(400));

        Assert.Null(assembler.FlushIfIdle(T0 + TimeSpan.FromMilliseconds(600)));
        Assert.NotNull(assembler.FlushIfIdle(T0 + TimeSpan.FromMilliseconds(900)));
    }

    [Fact]
    public void An_oversized_entry_is_truncated_keeping_the_header_and_flagged()
    {
        var assembler = Create(new EntryOptions { MaxEntryLength = 40 });

        var entries = Feed(assembler,
            Header("ERR", "header kept"),
            new string('x', 100),
            "   at AlsoCut()",
            Header("INF", "next", 1));

        var entry = Assert.Single(entries);
        Assert.True(entry.Truncated);
        Assert.StartsWith("header kept\nxxxx", entry.Message);
        Assert.Equal(40, entry.Message.Length);
    }

    [Fact]
    public void Very_long_single_lines_and_emoji_survive_intact()
    {
        var assembler = Create();
        var long_ = "▶️ " + new string('é', 3800);

        var entries = Feed(assembler, Header("INF", long_), Header("INF", "next", 1));

        Assert.Equal(long_, Assert.Single(entries).Message);
    }

    [Fact]
    public void Entries_do_not_mix_between_sources()
    {
        var a = new EntryAssembler(1, SerilogTextParser.Instance, new EntryOptions());
        var b = new EntryAssembler(2, SerilogTextParser.Instance, new EntryOptions());

        a.Add(Header("ERR", "from a"), T0);
        b.Add(Header("INF", "from b"), T0);
        a.Add("   at A.Frame()", T0);

        Assert.Equal("from a\n   at A.Frame()", a.FlushIfIdle(T0 + TimeSpan.FromSeconds(5))!.Message);
        Assert.Equal("from b", b.FlushIfIdle(T0 + TimeSpan.FromSeconds(5))!.Message);
    }

    [Fact]
    public void Every_anonymized_fixture_is_grouped_without_losing_a_line()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "fixtures", "logs");
        var files = Directory.GetFiles(root, "*.log", SearchOption.AllDirectories);
        Assert.NotEmpty(files);

        foreach (var file in files)
        {
            var lines = File.ReadAllLines(file);
            var assembler = Create();
            var entries = Feed(assembler, lines);
            entries.Add(assembler.FlushIfIdle(T0 + TimeSpan.FromHours(1))!);

            var headers = lines.Count(l => SerilogTextParser.Instance.TryParseHeader(l, out _));
            Assert.Equal(headers, entries.Count(e => e.Level != EntryLevel.Unknown || e.Timestamp is not null));
            Assert.Equal(lines.Length, entries.Sum(e => 1 + e.ExtraLines));
        }
    }
}
