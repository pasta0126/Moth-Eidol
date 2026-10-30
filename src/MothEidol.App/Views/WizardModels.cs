using Avalonia.Media;
using MothEidol.Core.Logs;
using MothEidol.Core.Projects;

namespace MothEidol.App.Views;

/// <summary>One recognized entry of the location check, shown as a line with its detected level.</summary>
public sealed class SampleRow(LogEntry entry)
{
    public string Time { get; } = entry.Timestamp?.ToLocalTime().ToString("HH:mm:ss") ?? "--:--:--";

    public string Level { get; } = entry.Level switch
    {
        EntryLevel.Verbose => "VRB",
        EntryLevel.Debug => "DBG",
        EntryLevel.Information => "INF",
        EntryLevel.Warning => "WRN",
        EntryLevel.Error => "ERR",
        EntryLevel.Fatal => "FTL",
        _ => "???",
    };

    public string Message { get; } = entry.ExtraLines > 0 ? $"{entry.FirstLine}  (+{entry.ExtraLines} lines)" : entry.FirstLine;

    public IBrush? LevelBrush { get; } = ThemeBrushes.Get(entry.Level switch
    {
        EntryLevel.Information => "MothInfoBrush",
        EntryLevel.Warning => "MothWarningBrush",
        EntryLevel.Error or EntryLevel.Fatal => "MothErrorBrush",
        _ => "MothTextMutedBrush",
    });
}

/// <summary>A source already added in the wizard, with the button to take it out again.</summary>
public sealed class WizardSourceRow(SourceDraft source)
{
    public SourceDraft Source { get; } = source;

    public string Name => Source.DisplayName;

    public string Kind => Source.Kind == SourceKind.File ? "File" : "Folder";
}
