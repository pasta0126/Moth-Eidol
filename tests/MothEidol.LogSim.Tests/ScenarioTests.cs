using System.Text;

namespace MothEidol.LogSim.Tests;

public class ScenarioTests
{
    private static int _n;

    private static EntrySource Source() => at =>
        [$"{at:yyyy-MM-dd HH:mm:ss.fff} {at:zzz} [INF] entry {Interlocked.Increment(ref _n)}"];

    private static string[] Lines(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return LogFormat.SplitLines(reader.ReadToEnd());
    }

    [Fact]
    public async Task Append_writes_count_crlf_terminated_entries()
    {
        using var temp = new TempDir();
        var path = Path.Combine(temp.Path, "app.log");

        await Scenarios.AppendAsync(path, Source(), 5, TimeSpan.Zero);

        Assert.Equal(5, Lines(path).Length);
        Assert.EndsWith("\r\n", File.ReadAllText(path));
    }

    [Fact]
    public async Task Partial_leaves_the_line_unterminated_until_completed()
    {
        using var temp = new TempDir();
        var path = Path.Combine(temp.Path, "app.log");
        var seenWhilePending = "";

        var task = Scenarios.PartialAsync(path, Source(), TimeSpan.FromMilliseconds(400));
        await Task.Delay(150);
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            seenWhilePending = new StreamReader(stream).ReadToEnd();
        await task;

        Assert.NotEmpty(seenWhilePending);
        Assert.DoesNotContain('\n', seenWhilePending);
        Assert.EndsWith("\r\n", File.ReadAllText(path));
    }

    [Fact]
    public async Task Rollover_continues_in_the_next_days_file_and_leaves_the_first_untouched()
    {
        using var temp = new TempDir();

        await Scenarios.RolloverAsync(temp.Path, "App", new DateOnly(2026, 1, 1), Source(), 3, TimeSpan.Zero);

        var first = Lines(Path.Combine(temp.Path, "App20260101.log"));
        var second = Lines(Path.Combine(temp.Path, "App20260102.log"));
        Assert.Equal(3, first.Length);
        Assert.Equal(3, second.Length);
        Assert.StartsWith("2026-01-01 23:59", first[0]);
        Assert.StartsWith("2026-01-02 00:00", second[0]);
    }

    [Fact]
    public async Task Truncate_empties_the_file_in_place_and_keeps_writing()
    {
        using var temp = new TempDir();
        var path = Path.Combine(temp.Path, "app.log");

        await Scenarios.TruncateAsync(path, Source(), 4, TimeSpan.Zero);

        Assert.Equal(4, Lines(path).Length);
    }

    [Fact]
    public async Task Rotate_renames_the_old_file_and_starts_a_new_one_at_the_same_path()
    {
        using var temp = new TempDir();
        var path = Path.Combine(temp.Path, "app.log");

        await Scenarios.RotateAsync(path, Source(), 3, TimeSpan.Zero);

        Assert.Equal(3, Lines(path + ".1").Length);
        Assert.Equal(3, Lines(path).Length);
    }

    [Fact]
    public async Task Disappear_deletes_the_file_and_recreates_it_after_the_gap()
    {
        using var temp = new TempDir();
        var path = Path.Combine(temp.Path, "app.log");

        var task = Scenarios.DisappearAsync(path, Source(), 3, TimeSpan.Zero, TimeSpan.FromMilliseconds(400));
        await Task.Delay(150);
        var existedDuringGap = File.Exists(path);
        await task;

        Assert.False(existedDuringGap);
        Assert.Equal(3, Lines(path).Length);
    }

    [Fact]
    public void Writer_does_not_lock_the_file_against_readers_or_deletion()
    {
        using var temp = new TempDir();
        var path = Path.Combine(temp.Path, "app.log");

        using var writer = new LogFileWriter(path);
        writer.WriteLines(["one"]);

        Assert.Single(Lines(path));
        File.Delete(path);
        Assert.False(File.Exists(path));
    }
}
