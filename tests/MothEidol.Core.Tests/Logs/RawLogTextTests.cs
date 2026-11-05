using MothEidol.Core.Logs;

namespace MothEidol.Core.Tests.Logs;

public class RawLogTextTests
{
    private static readonly DateTimeOffset At = new(2026, 11, 5, 10, 30, 15, 123, TimeSpan.Zero);

    private static LogEntry Entry(string message, EntryLevel level = EntryLevel.Information) =>
        new(1, At, level, message, false, At);

    [Fact]
    public void Build_OneLinePerEntryInOrder()
    {
        var result = RawLogText.Build([Entry("first"), Entry("second", EntryLevel.Error)]);

        Assert.Equal("2026-11-05 10:30:15.123 INF first\n2026-11-05 10:30:15.123 ERR second", result.Text);
        Assert.False(result.Truncated);
    }

    [Fact]
    public void Build_KeepsContinuationLinesUnchanged()
    {
        var result = RawLogText.Build([Entry("boom\r\n   at A()\r\n   at B()", EntryLevel.Error), Entry("next")]);

        Assert.Equal(
            "2026-11-05 10:30:15.123 ERR boom\n   at A()\n   at B()\n2026-11-05 10:30:15.123 INF next",
            result.Text);
    }

    [Fact]
    public void Build_OnlyFormatsTheGivenEntries()
    {
        var all = new[] { Entry("info"), Entry("bad", EntryLevel.Error) };
        var result = RawLogText.Build(all.Where(e => e.Level >= EntryLevel.Error));

        Assert.DoesNotContain("info", result.Text);
        Assert.Contains("bad", result.Text);
    }

    [Fact]
    public void Build_WithoutTimestampStartsWithBlankTime()
    {
        var result = RawLogText.Build([new LogEntry(1, null, EntryLevel.Unknown, "plain", false, At)]);
        Assert.Equal("     plain", result.Text);
    }

    [Fact]
    public void Build_EmptyIsEmpty()
    {
        var result = RawLogText.Build([]);
        Assert.Equal("", result.Text);
        Assert.False(result.Truncated);
    }

    [Fact]
    public void Build_KeepsTheLastLinesWhenOverTheCap()
    {
        var entries = Enumerable.Range(1, 10).Select(i => Entry($"line {i}")).ToList();
        var result = RawLogText.Build(entries, maxLines: 4);

        Assert.True(result.Truncated);
        var lines = result.Text.Split('\n');
        Assert.Equal(4, lines.Length);
        Assert.EndsWith("line 7", lines[0]);
        Assert.EndsWith("line 10", lines[3]);
    }

    [Fact]
    public void Build_CapCountsContinuationLines()
    {
        var result = RawLogText.Build([Entry("a\nb\nc"), Entry("d")], maxLines: 3);

        Assert.True(result.Truncated);
        Assert.Equal("b\nc\n2026-11-05 10:30:15.123 INF d", result.Text);
    }

    [Fact]
    public void Build_ExactlyAtTheCapIsNotTruncated()
    {
        var result = RawLogText.Build([Entry("a"), Entry("b")], maxLines: 2);
        Assert.False(result.Truncated);
    }
}
