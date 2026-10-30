using System.Text.RegularExpressions;

namespace MothEidol.LogSim;

/// <summary>One log entry as text: the header line plus its continuation lines (stack traces).</summary>
public sealed record LogEntryText(string? Level, IReadOnlyList<string> Lines);

public static class LogFormat
{
    public static readonly Regex Header = new(
        @"^(?<date>\d{4}-\d{2}-\d{2}) (?<time>\d{2}:\d{2}:\d{2}\.\d{3}) (?<offset>[+-]\d{2}:\d{2}) \[(?<level>[A-Z]{3})\] (?<message>.*)$",
        RegexOptions.Compiled);

    public static string[] SplitLines(string text)
    {
        var lines = text.Split('\n').Select(l => l.TrimEnd('\r')).ToList();
        if (lines.Count > 0 && lines[^1].Length == 0) lines.RemoveAt(lines.Count - 1);
        return lines.ToArray();
    }

    /// <summary>Groups lines into entries: a header line starts one, any other line continues the current one.</summary>
    public static List<LogEntryText> Group(IEnumerable<string> lines)
    {
        var entries = new List<LogEntryText>();
        string? level = null;
        List<string>? current = null;

        void Flush()
        {
            if (current is not null) entries.Add(new LogEntryText(level, current));
        }

        foreach (var line in lines)
        {
            var match = Header.Match(line);
            if (match.Success)
            {
                Flush();
                level = match.Groups["level"].Value;
                current = [line];
            }
            else
            {
                if (current is null) { level = null; current = []; }
                current.Add(line);
            }
        }
        Flush();
        return entries;
    }

    public static bool IsAlerting(string? level) => level is null or "WRN" or "ERR" or "FTL";
}
