namespace MothEidol.Core.Logs;

/// <summary>The plain text of a Raw view: the text and whether the oldest lines were cut to respect the cap.</summary>
public readonly record struct RawLogResult(string Text, bool Truncated);

/// <summary>
/// Formats log entries as plain log lines (time, level, message) for the Raw view of the tail: one entry after
/// another in the given order, with the continuation lines unchanged. Only the last lines are kept when the
/// text would exceed the cap, so a text box stays responsive.
/// </summary>
public static class RawLogText
{
    public const int DefaultMaxLines = 20_000;

    /// <summary>Three-letter code of a level as shown in the log lines; blank when unknown.</summary>
    public static string LevelCode(EntryLevel level) => level switch
    {
        EntryLevel.Verbose => "VRB",
        EntryLevel.Debug => "DBG",
        EntryLevel.Information => "INF",
        EntryLevel.Warning => "WRN",
        EntryLevel.Error => "ERR",
        EntryLevel.Fatal => "FTL",
        _ => "   ",
    };

    public static RawLogResult Build(IEnumerable<LogEntry> entries, int maxLines = DefaultMaxLines)
    {
        var lines = new List<string>();
        foreach (var entry in entries)
        {
            var time = entry.Timestamp?.ToString("yyyy-MM-dd HH:mm:ss.fff") ?? "";
            var message = entry.Message.TrimEnd('\r', '\n').Replace("\r\n", "\n").Split('\n');
            lines.Add($"{time} {LevelCode(entry.Level)} {message[0]}");
            for (var i = 1; i < message.Length; i++) lines.Add(message[i]);
        }

        var truncated = lines.Count > maxLines;
        if (truncated) lines.RemoveRange(0, lines.Count - maxLines);
        return new RawLogResult(string.Join('\n', lines), truncated);
    }
}
