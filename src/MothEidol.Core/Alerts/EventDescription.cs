using System.Text.RegularExpressions;

namespace MothEidol.Core.Alerts;

/// <param name="Title">What the event is, in words a person understands.</param>
/// <param name="Location">Where it happens (for example <c>Worker.ExecuteAsync</c>); null when unknown.</param>
public sealed record EventDescription(string Title, string? Location);

public static partial class EventDescriber
{
    private const int MaxTitleLength = 300;

    [GeneratedRegex(@"^<(?<name>[^>]+)>d__\d+$")]
    private static partial Regex AsyncStateMachine();

    /// <summary>
    /// The exception type and message as the title when the entry has an exception, the first line of the
    /// entry otherwise; and the first application frame of the first exception as the location.
    /// </summary>
    public static EventDescription Describe(string message)
    {
        var trace = StackTraceParser.Parse(message);
        if (!trace.HasException) return new EventDescription(EventGrouping.Title(message), null);

        var exception = trace.Exceptions[0];
        var firstMessageLine = exception.Message.Split('\n', 2)[0].Trim();
        var title = firstMessageLine.Length == 0 ? exception.ShortType : $"{exception.ShortType}: {firstMessageLine}";
        if (title.Length > MaxTitleLength) title = title[..MaxTitleLength] + "…";

        var frame = exception.Frames.FirstOrDefault(f => f.IsApplication);
        return new EventDescription(title, frame is null ? null : Location(frame.Method));
    }

    /// <summary>"Ns.Class.Method(args)" becomes "Class.Method"; async state machines become the awaited method.</summary>
    public static string Location(string method)
    {
        var open = method.IndexOf('(');
        var name = (open >= 0 ? method[..open] : method).Trim();

        // Generic arguments such as Run[T] or Run`1 are noise for a person.
        name = Regex.Replace(name, @"\[[^\]]*\]|`\d+", "");
        var parts = name.Split('.');
        if (parts.Length == 1) return name;

        if (parts[^1] == "MoveNext" && parts.Length >= 3 && AsyncStateMachine().Match(parts[^2]) is { Success: true } state)
            return $"{parts[^3]}.{state.Groups["name"].Value}";

        return $"{parts[^2]}.{parts[^1]}";
    }
}
