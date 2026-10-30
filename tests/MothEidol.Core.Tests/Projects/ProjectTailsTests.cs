using MothEidol.Core.Logs;

namespace MothEidol.Core.Tests.Projects;

public class ProjectTailsTests
{
    private static LogEntry Entry(long sourceId, string text) =>
        new(sourceId, null, EntryLevel.Information, text, false, DateTimeOffset.Now);

    private static async Task WaitForAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline) throw new TimeoutException();
            await Task.Delay(10);
        }
    }

    [Fact]
    public async Task Entries_are_collected_per_project_through_their_source()
    {
        var bus = new EntryBus();
        var map = new Dictionary<long, long> { [1] = 10, [2] = 10, [3] = 20 };
        using var tails = new ProjectTails(bus, id => map.TryGetValue(id, out var p) ? p : null, 100);

        await bus.PublishAsync([Entry(1, "a"), Entry(3, "x"), Entry(2, "b")], CancellationToken.None);

        await WaitForAsync(() => tails.For(10).NextSequence == 2 && tails.For(20).NextSequence == 1);
        Assert.Equal(["a", "b"], tails.For(10).ReadSince(0).Lines.Select(e => e.Message));
        Assert.Equal(["x"], tails.For(20).ReadSince(0).Lines.Select(e => e.Message));
        Assert.Equal([1L, 2L], tails.For(10).ReadSince(0).Lines.Select(e => e.SourceId));
    }

    [Fact]
    public async Task Entries_of_sources_without_a_project_are_ignored()
    {
        var bus = new EntryBus();
        using var tails = new ProjectTails(bus, _ => null, 100);

        await bus.PublishAsync([Entry(1, "orphan")], CancellationToken.None);
        await Task.Delay(100);

        Assert.Equal(0, tails.For(10).NextSequence);
    }

    [Fact]
    public async Task The_line_limit_applies_to_every_project_buffer()
    {
        var bus = new EntryBus();
        using var tails = new ProjectTails(bus, _ => 10, maxLines: 3);

        await bus.PublishAsync([.. Enumerable.Range(1, 6).Select(i => Entry(1, $"e{i}"))], CancellationToken.None);
        await WaitForAsync(() => tails.For(10).NextSequence == 6);
        Assert.Equal(["e4", "e5", "e6"], tails.For(10).ReadSince(0).Lines.Select(e => e.Message));

        tails.MaxLines = 2;

        Assert.Equal(["e5", "e6"], tails.For(10).ReadSince(0).Lines.Select(e => e.Message));
    }
}
