using MothEidol.Core.Logs;

namespace MothEidol.Core.Tests.Logs;

public class ViewBufferTests
{
    [Fact]
    public void Keeps_only_the_most_recent_lines_up_to_the_limit()
    {
        var buffer = new ViewBuffer<string>(3);
        foreach (var i in Enumerable.Range(1, 5)) buffer.Add($"l{i}");

        Assert.Equal(["l3", "l4", "l5"], buffer.ReadSince(0).Lines);
    }

    [Fact]
    public void ReadSince_returns_only_what_is_new_and_the_next_position()
    {
        var buffer = new ViewBuffer<string>(10);
        buffer.Add("a");
        buffer.Add("b");
        var (first, next) = buffer.ReadSince(0);
        buffer.Add("c");

        var (second, afterSecond) = buffer.ReadSince(next);

        Assert.Equal(["a", "b"], first);
        Assert.Equal(["c"], second);
        Assert.Equal(3, afterSecond);
        Assert.Empty(buffer.ReadSince(afterSecond).Lines);
    }

    [Fact]
    public void A_reader_that_fell_behind_gets_the_retained_lines_only()
    {
        var buffer = new ViewBuffer<string>(2);
        foreach (var i in Enumerable.Range(1, 10)) buffer.Add($"l{i}");

        Assert.Equal(["l9", "l10"], buffer.ReadSince(3).Lines);
    }

    [Fact]
    public void Lowering_the_limit_discards_the_oldest_lines()
    {
        var buffer = new ViewBuffer<string>(5);
        foreach (var i in Enumerable.Range(1, 5)) buffer.Add($"l{i}");

        buffer.MaxLines = 2;

        Assert.Equal(["l4", "l5"], buffer.ReadSince(0).Lines);
    }

    [Fact]
    public void Clear_empties_the_buffer_without_repeating_old_lines_to_readers()
    {
        var buffer = new ViewBuffer<string>(5);
        buffer.Add("a");
        var (_, next) = buffer.ReadSince(0);

        buffer.Clear();
        buffer.Add("b");

        Assert.Equal(["b"], buffer.ReadSince(next).Lines);
    }
}
