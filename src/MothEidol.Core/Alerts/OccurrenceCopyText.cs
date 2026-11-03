namespace MothEidol.Core.Alerts;

/// <summary>The clipboard text of an event detail: the context entries followed by the occurrence text.</summary>
public static class OccurrenceCopyText
{
    public static string Build(EventOccurrence occurrence)
    {
        var lines = new List<string>();
        foreach (var entry in occurrence.Context ?? [])
            lines.Add(entry.Text.TrimEnd('\r', '\n'));
        lines.Add(occurrence.Message.TrimEnd('\r', '\n'));
        return string.Join('\n', lines);
    }
}
