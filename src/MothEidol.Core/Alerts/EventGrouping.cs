using System.Text.RegularExpressions;
using MothEidol.Core.Logs;

namespace MothEidol.Core.Alerts;

/// <summary>
/// Decides which occurrences are "the same event": same level, same message once ids and other
/// variable values are masked and, when there is a stack trace, the same exception type.
/// </summary>
public static partial class EventGrouping
{
    private const int MaxKeyLength = 600;
    private const int MaxTitleLength = 300;

    [GeneratedRegex(@"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}")]
    private static partial Regex Guid();

    [GeneratedRegex(@"\d{4}-\d{2}-\d{2}T[\d:.]+(?:[+-]\d{2}:\d{2}|Z)?")]
    private static partial Regex IsoTimestamp();

    [GeneratedRegex("\"[^\"\\r\\n]*\"")]
    private static partial Regex Quoted();

    [GeneratedRegex(@"[\w.+-]+@[\w-]+(?:\.[\w-]+)+")]
    private static partial Regex Email();

    // Any token containing a digit (ids, tax numbers like 12345678Z, versions) is a value, not message text.
    [GeneratedRegex(@"\w*\d\w*")]
    private static partial Regex Digits();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    [GeneratedRegex(@"^\s*(?<type>[A-Za-z_][\w.`+<>]*(?:Exception|Error))\b", RegexOptions.Multiline)]
    private static partial Regex ExceptionLine();

    /// <summary>Masks GUIDs, timestamps, quoted values, emails and tokens with digits so repeated messages look identical.</summary>
    public static string Normalize(string line)
    {
        line = Guid().Replace(line, "<guid>");
        line = IsoTimestamp().Replace(line, "<ts>");
        line = Quoted().Replace(line, "<str>");
        line = Email().Replace(line, "<email>");
        line = Digits().Replace(line, "<n>");
        return Whitespace().Replace(line, " ").Trim();
    }

    /// <summary>The type of the first exception named after the first line of the message, if any.</summary>
    public static string? ExceptionType(string message)
    {
        var firstBreak = message.IndexOf('\n');
        if (firstBreak < 0) return null;

        var match = ExceptionLine().Match(message, firstBreak + 1);
        return match.Success ? match.Groups["type"].Value : null;
    }

    public static string Key(EntryLevel level, string message)
    {
        var first = message.Split('\n', 2)[0];
        var key = $"{level}|{Normalize(first)}|{ExceptionType(message)}";
        return key.Length > MaxKeyLength ? key[..MaxKeyLength] : key;
    }

    /// <summary>What the user reads as the event's name: the first line of the latest occurrence.</summary>
    public static string Title(string message)
    {
        var first = message.Split('\n', 2)[0].Trim();
        return first.Length > MaxTitleLength ? first[..MaxTitleLength] + "…" : first;
    }
}
