using System.Text;

namespace MothEidol.Core.Logs;

/// <summary>
/// Turns the lines of one source into entries: a header line starts an entry and every following line that
/// is not a header is appended to it. Timeless (the caller passes the clock), so it is easy to test.
/// </summary>
public sealed class EntryAssembler(long sourceId, ILogEntryParser parser, EntryOptions options)
{
    private EntryHeader _header;
    private bool _hasHeader;
    private StringBuilder? _text;
    private bool _truncated;
    private DateTimeOffset _startedAt;
    private DateTimeOffset _lastLineAt;

    /// <summary>Adds a line; returns the entry it completed, if any (the previous one when a new header arrives).</summary>
    public LogEntry? Add(string line, DateTimeOffset now)
    {
        if (parser.TryParseHeader(line, out var header))
        {
            var completed = Complete();
            Start(header, header.Message, now);
            return completed;
        }

        if (_text is null)
        {
            // Lines before any header (for example after resuming in the middle of a stack trace).
            Start(new EntryHeader(null, EntryLevel.Unknown, line), line, now);
        }
        else
        {
            Append(line);
        }

        _lastLineAt = now;
        return null;
    }

    /// <summary>Emits the open entry if no line has arrived for the idle timeout.</summary>
    public LogEntry? FlushIfIdle(DateTimeOffset now) =>
        _text is not null && now - _lastLineAt >= options.IdleTimeout ? Complete() : null;

    private void Start(EntryHeader header, string firstText, DateTimeOffset now)
    {
        _header = header;
        _hasHeader = true;
        _text = new StringBuilder();
        _truncated = false;
        _startedAt = now;
        _lastLineAt = now;
        AppendRaw(firstText);
    }

    private void Append(string line)
    {
        if (_truncated) return;
        AppendRaw("\n" + line);
    }

    private void AppendRaw(string text)
    {
        var room = options.MaxEntryLength - _text!.Length;
        if (text.Length <= room)
        {
            _text.Append(text);
            return;
        }

        if (room > 0) _text.Append(text, 0, room);
        _truncated = true;
    }

    private LogEntry? Complete()
    {
        if (_text is null || !_hasHeader) return null;

        var entry = new LogEntry(sourceId, _header.Timestamp, _header.Level, _text.ToString(), _truncated, _startedAt);
        _text = null;
        _hasHeader = false;
        return entry;
    }
}
