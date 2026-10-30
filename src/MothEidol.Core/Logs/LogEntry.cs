namespace MothEidol.Core.Logs;

/// <summary>Severity of an entry. Declared in increasing order, so a filter can compare levels.</summary>
public enum EntryLevel
{
    Unknown = 0,
    Verbose,
    Debug,
    Information,
    Warning,
    Error,
    Fatal,
}

/// <summary>The parsed header line of an entry.</summary>
public readonly record struct EntryHeader(DateTimeOffset? Timestamp, EntryLevel Level, string Message);

/// <summary>
/// One log entry: the header line plus the continuation lines that follow it (a stack trace stays attached
/// to its error). <see cref="Message"/> holds the text after the header prefix and then the continuation
/// lines, separated by line feeds.
/// </summary>
public sealed record LogEntry(
    long SourceId,
    DateTimeOffset? Timestamp,
    EntryLevel Level,
    string Message,
    bool Truncated,
    DateTimeOffset ReadAt)
{
    public string FirstLine
    {
        get
        {
            var end = Message.IndexOf('\n');
            return end < 0 ? Message : Message[..end];
        }
    }

    /// <summary>Number of continuation lines after the first.</summary>
    public int ExtraLines => Message.Count(c => c == '\n');
}

public sealed record EntryOptions
{
    /// <summary>An entry with no further line for this long is emitted, so the last entry of a burst is not held back.</summary>
    public TimeSpan IdleTimeout { get; init; } = TimeSpan.FromMilliseconds(500);

    /// <summary>Maximum characters kept per entry; the header is always kept and the rest is cut.</summary>
    public int MaxEntryLength { get; init; } = 64 * 1024;
}

/// <summary>Recognizes the header line that starts an entry. Replaceable to support other formats.</summary>
public interface ILogEntryParser
{
    bool TryParseHeader(string line, out EntryHeader header);
}
