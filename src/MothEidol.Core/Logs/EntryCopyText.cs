
namespace MothEidol.Core.Logs;

/// <summary>The clipboard text of log entries: the full message of each (continuation lines included), in order.</summary>
public static class EntryCopyText
{
    public static string Build(IEnumerable<LogEntry> entries) =>
        string.Join('\n', entries.Select(e => e.Message.TrimEnd('\r', '\n')));
}
