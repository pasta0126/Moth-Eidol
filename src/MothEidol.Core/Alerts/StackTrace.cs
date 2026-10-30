using System.Text.RegularExpressions;

namespace MothEidol.Core.Alerts;

/// <summary>One "at ..." line of a stack trace.</summary>
/// <param name="IsApplication">True for the monitored application's own code, false for framework and library frames.</param>
public sealed record StackFrame(string Method, string? File, int? Line, bool IsApplication);

/// <summary>An exception named in a stack trace with the frames listed under it.</summary>
public sealed record ParsedException(string Type, string Message, IReadOnlyList<StackFrame> Frames)
{
    /// <summary>The type without its namespace, for headings ("InvalidOperationException").</summary>
    public string ShortType => Type[(Type.LastIndexOf('.') + 1)..];
}

/// <summary>
/// The exceptions of an entry in order of appearance (the outer one first, then inner ones) and any lines that
/// were not recognized, kept as plain text.
/// </summary>
public sealed record StackTrace(IReadOnlyList<ParsedException> Exceptions, IReadOnlyList<string> OtherLines)
{
    public bool HasException => Exceptions.Count > 0;
}

/// <summary>Reads the .NET stack trace format found after the first line of an error entry.</summary>
public static partial class StackTraceParser
{
    private static readonly string[] FrameworkPrefixes = ["System.", "Microsoft.", "Windows.", "Newtonsoft.", "Dapper.", "Polly."];

    [GeneratedRegex(@"^\s*(?:--->\s*)?(?<type>[A-Za-z_][\w.`+<>]*(?:Exception|Error))(?:\s*\((?:0x)?[0-9A-Fa-f]+\))?(?::\s*(?<message>.*))?$")]
    private static partial Regex ExceptionLine();

    [GeneratedRegex(@"^\s*at\s+(?<method>.+?)(?:\s+in\s+(?<file>.+?):line\s+(?<line>\d+))?\s*$")]
    private static partial Regex FrameLine();

    [GeneratedRegex(@"^\s*---\s+End of .*---\s*$")]
    private static partial Regex Separator();

    /// <summary>Parses an entry message; its first line is the log text itself and is not part of the trace.</summary>
    public static StackTrace Parse(string message)
    {
        var exceptions = new List<(string Type, string Message, List<StackFrame> Frames)>();
        var other = new List<string>();

        var lines = message.Split('\n');
        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');
            if (line.Trim().Length == 0 || Separator().IsMatch(line)) continue;

            var frame = FrameLine().Match(line);
            if (frame.Success && exceptions.Count > 0)
            {
                var file = frame.Groups["file"].Success ? frame.Groups["file"].Value : null;
                int? lineNumber = frame.Groups["line"].Success ? int.Parse(frame.Groups["line"].Value) : null;
                var method = frame.Groups["method"].Value.Trim();
                exceptions[^1].Frames.Add(new StackFrame(method, file, lineNumber, IsApplication(method, file)));
                continue;
            }

            var exception = ExceptionLine().Match(line);
            if (exception.Success)
            {
                exceptions.Add((exception.Groups["type"].Value, exception.Groups["message"].Value.Trim(), []));
                continue;
            }

            if (exceptions.Count > 0 && exceptions[^1].Frames.Count == 0)
            {
                // A continuation of the exception message (multi-line messages).
                var last = exceptions[^1];
                exceptions[^1] = (last.Type, (last.Message + "\n" + line.Trim()).Trim(), last.Frames);
            }
            else
            {
                other.Add(line);
            }
        }

        return new StackTrace([.. exceptions.Select(e => new ParsedException(e.Type, e.Message, e.Frames))], other);
    }

    /// <summary>
    /// Framework frames carry no source location (no symbols) and live in well-known namespaces;
    /// anything with a file and line, or outside those namespaces, is the application's own code.
    /// </summary>
    private static bool IsApplication(string method, string? file) =>
        file is not null || !FrameworkPrefixes.Any(p => method.StartsWith(p, StringComparison.Ordinal));
}
