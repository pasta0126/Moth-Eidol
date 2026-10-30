namespace MothEidol.LogSim;

/// <summary>Produces the next entry (header plus continuation lines) stamped at the given time.</summary>
public delegate IReadOnlyList<string> EntrySource(DateTimeOffset at);

/// <summary>Live-writing scenarios used to exercise tailing: they write the way a running application would.</summary>
public static class Scenarios
{
    public static async Task AppendAsync(string path, EntrySource source, int count, TimeSpan interval, CancellationToken ct = default)
    {
        using var writer = new LogFileWriter(path);
        await WriteAsync(writer, source, count, interval, DateTimeOffset.Now, ct);
    }

    /// <summary>Writes a header without its line break, waits, then completes the line.</summary>
    public static async Task PartialAsync(string path, EntrySource source, TimeSpan delay, CancellationToken ct = default)
    {
        using var writer = new LogFileWriter(path);
        var lines = source(DateTimeOffset.Now);
        var first = lines[0];
        var split = Math.Max(1, first.Length / 2);
        writer.WriteRaw(first[..split]);
        await Pause(delay, ct);
        writer.WriteRaw(first[split..] + "\r\n");
        writer.WriteLines(lines.Skip(1));
    }

    /// <summary>Writes to <c>{prefix}{yyyyMMdd}.log</c>, then continues in the next day's file, leaving the first untouched.</summary>
    public static async Task RolloverAsync(
        string folder, string prefix, DateOnly firstDay, EntrySource source, int countPerDay, TimeSpan interval, CancellationToken ct = default)
    {
        for (var day = 0; day < 2; day++)
        {
            var date = firstDay.AddDays(day);
            var path = Path.Combine(folder, $"{prefix}{date:yyyyMMdd}.log");
            var start = new DateTimeOffset(date.ToDateTime(new TimeOnly(day == 0 ? 23 : 0, day == 0 ? 59 : 0, 0)), DateTimeOffset.Now.Offset);
            using var writer = new LogFileWriter(path);
            await WriteAsync(writer, source, countPerDay, interval, start, ct);
        }
    }

    /// <summary>Empties the file in place, as a copy-truncate rotation does, and keeps writing.</summary>
    public static async Task TruncateAsync(string path, EntrySource source, int count, TimeSpan interval, CancellationToken ct = default)
    {
        using var writer = new LogFileWriter(path);
        await WriteAsync(writer, source, count, interval, DateTimeOffset.Now, ct);
        writer.Truncate();
        await WriteAsync(writer, source, count, interval, DateTimeOffset.Now, ct);
    }

    /// <summary>Renames the file to <c>{path}.1</c> and continues in a new file at the same path.</summary>
    public static async Task RotateAsync(string path, EntrySource source, int count, TimeSpan interval, CancellationToken ct = default)
    {
        using (var writer = new LogFileWriter(path))
            await WriteAsync(writer, source, count, interval, DateTimeOffset.Now, ct);
        File.Move(path, path + ".1", overwrite: true);
        using (var writer = new LogFileWriter(path))
            await WriteAsync(writer, source, count, interval, DateTimeOffset.Now, ct);
    }

    /// <summary>Deletes the file, waits, then recreates it and continues.</summary>
    public static async Task DisappearAsync(
        string path, EntrySource source, int count, TimeSpan interval, TimeSpan gap, CancellationToken ct = default)
    {
        using (var writer = new LogFileWriter(path))
        {
            await WriteAsync(writer, source, count, interval, DateTimeOffset.Now, ct);
            File.Delete(path);
            await Pause(gap, ct);
        }
        if (File.Exists(path)) File.Delete(path);
        using (var writer = new LogFileWriter(path))
            await WriteAsync(writer, source, count, interval, DateTimeOffset.Now, ct);
    }

    private static async Task WriteAsync(
        LogFileWriter writer, EntrySource source, int count, TimeSpan interval, DateTimeOffset start, CancellationToken ct)
    {
        for (var i = 0; i < count; i++)
        {
            writer.WriteLines(source(start + TimeSpan.FromMilliseconds(i * 10)));
            await Pause(interval, ct);
        }
    }

    private static Task Pause(TimeSpan time, CancellationToken ct) =>
        time > TimeSpan.Zero ? Task.Delay(time, ct) : Task.CompletedTask;
}
