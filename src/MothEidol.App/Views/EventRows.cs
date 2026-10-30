using System.ComponentModel;
using Avalonia.Media;
using MothEidol.Core.Alerts;
using MothEidol.Core.Logs;

namespace MothEidol.App.Views;

/// <summary>An event group as listed: level, title, how many times and how much is still pending.</summary>
public sealed class EventGroupRow(EventGroup group) : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public EventGroup Group { get; private set; } = group;

    public long Id => Group.Id;

    public string LevelText => Group.Level switch
    {
        EntryLevel.Fatal => "FTL",
        EntryLevel.Error => "ERR",
        EntryLevel.Warning => "WRN",
        _ => "···",
    };

    public IBrush? LevelBrush => ThemeBrushes.Get(Group.Level >= EntryLevel.Error ? "MothErrorBrush" : "MothWarningBrush");

    public string Title => Group.Title;

    public string CountText => $"×{Group.Count:N0}";

    public string PendingText => Group.IsPending ? $"{Group.PendingCount:N0} pending" : "seen";

    public IBrush? PendingBrush => ThemeBrushes.Get(Group.IsPending ? "MothPrimaryBrush" : "MothTextMutedBrush");

    public FontWeight TitleWeight => Group.IsPending ? FontWeight.SemiBold : FontWeight.Normal;

    public string LastSeenText => Group.LastSeen.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");

    public void Update(EventGroup group)
    {
        Group = group;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }
}

/// <summary>One recorded occurrence of a group, with its full text.</summary>
public sealed class OccurrenceRow(EventOccurrence occurrence, string sourceName)
{
    public string Time { get; } = occurrence.At.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.fff");

    public string Source { get; } = sourceName;

    public string Message { get; } = occurrence.Message;
}
