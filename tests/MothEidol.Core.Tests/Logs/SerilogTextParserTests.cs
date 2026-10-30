using MothEidol.Core.Logs;

namespace MothEidol.Core.Tests.Logs;

public class SerilogTextParserTests
{
    private static readonly SerilogTextParser Parser = SerilogTextParser.Instance;

    [Fact]
    public void A_valid_header_gives_timestamp_level_and_message()
    {
        var ok = Parser.TryParseHeader("2026-10-28 10:49:09.258 +01:00 [INF] Concurrency limiter initialized with 5 slots", out var header);

        Assert.True(ok);
        Assert.Equal(new DateTimeOffset(2026, 10, 28, 10, 49, 9, 258, TimeSpan.FromHours(1)), header.Timestamp);
        Assert.Equal(EntryLevel.Information, header.Level);
        Assert.Equal("Concurrency limiter initialized with 5 slots", header.Message);
    }

    [Theory]
    [InlineData("VRB", EntryLevel.Verbose)]
    [InlineData("DBG", EntryLevel.Debug)]
    [InlineData("INF", EntryLevel.Information)]
    [InlineData("WRN", EntryLevel.Warning)]
    [InlineData("ERR", EntryLevel.Error)]
    [InlineData("FTL", EntryLevel.Fatal)]
    public void Every_serilog_level_token_is_mapped(string token, EntryLevel expected)
    {
        Assert.True(Parser.TryParseHeader($"2026-01-01 00:00:00.000 +00:00 [{token}] x", out var header));
        Assert.Equal(expected, header.Level);
    }

    [Fact]
    public void An_unknown_level_token_still_gives_an_entry_with_unknown_level_and_all_the_text()
    {
        Assert.True(Parser.TryParseHeader("2026-01-01 00:00:00.000 +00:00 [XYZ] something odd", out var header));

        Assert.Equal(EntryLevel.Unknown, header.Level);
        Assert.Equal("something odd", header.Message);
    }

    [Fact]
    public void An_unparsable_timestamp_keeps_the_entry_without_a_time()
    {
        Assert.True(Parser.TryParseHeader("2026-13-45 00:00:00.000 +00:00 [ERR] bad date", out var header));

        Assert.Null(header.Timestamp);
        Assert.Equal(EntryLevel.Error, header.Level);
    }

    [Fact]
    public void Negative_offsets_and_emoji_messages_are_handled()
    {
        Assert.True(Parser.TryParseHeader("2026-01-01 08:00:00.123 -05:00 [WRN] ⚠️ 🔍 ñandú", out var header));

        Assert.Equal(TimeSpan.FromHours(-5), header.Timestamp!.Value.Offset);
        Assert.Equal("⚠️ 🔍 ñandú", header.Message);
    }

    [Fact]
    public void A_header_with_an_empty_message_is_accepted()
    {
        Assert.True(Parser.TryParseHeader("2026-01-01 00:00:00.000 +00:00 [INF]", out var header));
        Assert.Equal("", header.Message);
    }

    [Theory]
    [InlineData("   at System.Foo.Bar() in C:\\x.cs:line 5")]
    [InlineData("System.IO.IOException: boom")]
    [InlineData("2026-01-01 text that is not a header")]
    [InlineData("")]
    public void Anything_else_is_not_a_header(string line)
    {
        Assert.False(Parser.TryParseHeader(line, out _));
    }
}
