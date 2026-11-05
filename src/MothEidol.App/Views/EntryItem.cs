using System.ComponentModel;
using Avalonia.Media;
using MothEidol.Core.Logs;

namespace MothEidol.App.Views;

/// <summary>An entry as shown in the viewer: colored by level, with long multi-line entries collapsible.</summary>
public sealed class EntryItem(LogEntry entry) : INotifyPropertyChanged
{
    private bool _expanded;

    public event PropertyChangedEventHandler? PropertyChanged;

    public LogEntry Entry { get; } = entry;

    public string Time => Entry.Timestamp?.ToString("yyyy-MM-dd HH:mm:ss.fff") ?? "";

    public string LevelText => RawLogText.LevelCode(Entry.Level);

    /// <summary>Semantic colors from the theme: info, warning and error; everything else is muted.</summary>
    public IBrush? LevelBrush => ThemeBrushes.Get(Entry.Level switch
    {
        EntryLevel.Information => "MothInfoBrush",
        EntryLevel.Warning => "MothWarningBrush",
        EntryLevel.Error or EntryLevel.Fatal => "MothErrorBrush",
        _ => "MothTextMutedBrush",
    });

    public bool IsMultiLine => Entry.ExtraLines > 0;

    public bool Expanded
    {
        get => _expanded;
        set
        {
            if (_expanded == value) return;
            _expanded = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Expanded)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text)));
        }
    }

    public string Text => !IsMultiLine || _expanded
        ? Entry.Message
        : $"{Entry.FirstLine}   [+{Entry.ExtraLines} lines, click to expand]";
}
