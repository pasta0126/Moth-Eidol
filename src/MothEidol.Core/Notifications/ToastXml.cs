using System.Xml.Linq;
using MothEidol.Core.Logs;

namespace MothEidol.Core.Notifications;

/// <summary>Builds the payload of a native (Windows) toast from the readable text of an alert.</summary>
public static class ToastXml
{
    public static string Build(AlertText text, bool silent)
    {
        var lines = new List<XElement>
        {
            new("text", $"{text.ProjectName} · {LevelWord(text.Level)}"),
            new("text", text.Headline),
        };
        if (!string.IsNullOrWhiteSpace(text.Detail)) lines.Add(new XElement("text", text.Detail));

        var toast = new XElement("toast",
            new XElement("visual",
                new XElement("binding", new XAttribute("template", "ToastGeneric"), lines)));
        // Without the element the OS plays its default notification sound.
        if (silent) toast.Add(new XElement("audio", new XAttribute("silent", "true")));

        return toast.ToString(SaveOptions.DisableFormatting);
    }

    private static string LevelWord(EntryLevel level) => level switch
    {
        EntryLevel.Fatal => "Fatal",
        EntryLevel.Error => "Error",
        EntryLevel.Warning => "Warning",
        EntryLevel.Information => "Info",
        _ => "Event",
    };
}
