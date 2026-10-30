using System.Globalization;
using System.Text.RegularExpressions;

namespace MothEidol.Core.Logs;

/// <summary>
/// The default Serilog file layout: <c>yyyy-MM-dd HH:mm:ss.fff +zz:zz [LVL] message</c>.
/// Any line that does not start with this shape is a continuation of the previous entry.
/// </summary>
public sealed partial class SerilogTextParser : ILogEntryParser
{
    public static SerilogTextParser Instance { get; } = new();

    [GeneratedRegex(@"^(?<stamp>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3} [+-]\d{2}:\d{2}) \[(?<level>[A-Za-z]{3,11})\] ?(?<message>.*)$", RegexOptions.Singleline)]
    private static partial Regex Header();

    public bool TryParseHeader(string line, out EntryHeader header)
    {
        var match = Header().Match(line);
        if (!match.Success)
        {
            header = default;
            return false;
        }

        DateTimeOffset? timestamp = DateTimeOffset.TryParseExact(
            match.Groups["stamp"].Value, "yyyy-MM-dd HH:mm:ss.fff zzz",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : null;

        header = new EntryHeader(timestamp, ParseLevel(match.Groups["level"].Value), match.Groups["message"].Value);
        return true;
    }

    private static EntryLevel ParseLevel(string token) => token.ToUpperInvariant() switch
    {
        "VRB" or "VERBOSE" => EntryLevel.Verbose,
        "DBG" or "DEBUG" => EntryLevel.Debug,
        "INF" or "INFORMATION" => EntryLevel.Information,
        "WRN" or "WARNING" => EntryLevel.Warning,
        "ERR" or "ERROR" => EntryLevel.Error,
        "FTL" or "FATAL" => EntryLevel.Fatal,
        _ => EntryLevel.Unknown,
    };
}
