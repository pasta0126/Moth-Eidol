using MothEidol.Core.Alerts;
using MothEidol.Core.Logs;

namespace MothEidol.Core.Tests.Alerts;

public class CopyTextTests
{
    private static LogEntry Entry(string message) => new(1, null, EntryLevel.Information, message, false, DateTimeOffset.UtcNow);

    [Fact]
    public void EntryCopyText_KeepsOrderAndContinuationLines()
    {
        var text = EntryCopyText.Build([Entry("first\r\n   at A()"), Entry("second\n")]);
        Assert.Equal("first\r\n   at A()\nsecond", text);
    }

    [Fact]
    public void EntryCopyText_EmptyIsEmpty() => Assert.Equal("", EntryCopyText.Build([]));

    [Fact]
    public void OccurrenceCopyText_ContextThenOccurrence()
    {
        var at = DateTimeOffset.UtcNow;
        var occurrence = new EventOccurrence(1, 1, 1, at, "boom\n   at B()",
            [new ContextEntry(at, EntryLevel.Information, "before 1"), new ContextEntry(at, EntryLevel.Warning, "before 2")]);
        Assert.Equal("before 1\nbefore 2\nboom\n   at B()", OccurrenceCopyText.Build(occurrence));
    }

    [Fact]
    public void OccurrenceCopyText_WithoutContext()
    {
        var occurrence = new EventOccurrence(1, 1, 1, DateTimeOffset.UtcNow, "boom");
        Assert.Equal("boom", OccurrenceCopyText.Build(occurrence));
    }

    [Fact]
    public void OccurrenceCopyText_IsWhatTheRawDetailAndTheCopyButtonShow()
    {
        // The Raw detail and the per-occurrence Copy button both use this builder, so they cannot disagree.
        var at = DateTimeOffset.UtcNow;
        var occurrence = new EventOccurrence(1, 1, 1, at, "boom\r\n   at B()\r\n",
            [new ContextEntry(at, EntryLevel.Information, "before\r\n")]);

        var text = OccurrenceCopyText.Build(occurrence);

        Assert.Equal("before\nboom\r\n   at B()", text);
        Assert.Equal(text, OccurrenceCopyText.Build(occurrence));
    }
}
