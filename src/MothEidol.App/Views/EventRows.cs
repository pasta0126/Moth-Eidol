using System.ComponentModel;
using Avalonia.Media;
using MothEidol.Core.Alerts;
using MothEidol.Core.Logs;

namespace MothEidol.App.Views;

/// <summary>An event group as listed: level, readable title and location, badges, trend and recency.</summary>
public sealed class EventGroupRow : INotifyPropertyChanged
{
    private EventGroup _group;
    private IReadOnlyList<int> _series;
    private DateTimeOffset _now;

    public EventGroupRow(EventGroup group, IReadOnlyList<int> series, DateTimeOffset now)
    {
        _group = group;
        _series = series;
        _now = now;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public EventGroup Group => _group;

    public long Id => _group.Id;

    public string LevelText => _group.Level switch
    {
        EntryLevel.Fatal => "FTL",
        EntryLevel.Error => "ERR",
        EntryLevel.Warning => "WRN",
        _ => "···",
    };

    public IBrush? LevelBrush => ThemeBrushes.Get(_group.Level >= EntryLevel.Error ? "MothErrorBrush" : "MothWarningBrush");

    public string Title => _group.Title;

    public string Location => _group.Location ?? "";

    public bool HasLocation => !string.IsNullOrEmpty(_group.Location);

    public bool IsNew => _group.IsNew(_now);

    public bool IsRegressed => _group.IsRegressed;

    public IReadOnlyList<int> Series => _series;

    public IBrush? SeriesBrush => LevelBrush;

    public string CountText => $"×{_group.Count:N0}";

    public string PendingText => _group.IsPending ? $"{_group.PendingCount:N0} pending" : "seen";

    public IBrush? PendingBrush => ThemeBrushes.Get(_group.IsPending ? "MothPrimaryBrush" : "MothTextMutedBrush");

    public FontWeight TitleWeight => _group.IsPending ? FontWeight.SemiBold : FontWeight.Normal;

    public string LastSeenText => RelativeTime.Format(_group.LastSeen, _now);

    public string LastSeenExact => $"Last seen {_group.LastSeen.ToLocalTime():yyyy-MM-dd HH:mm:ss}";

    public void Update(EventGroup group, IReadOnlyList<int> series, DateTimeOffset now)
    {
        _group = group;
        _series = series;
        _now = now;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }
}

/// <summary>One recorded occurrence of a group, as an item of the occurrence list.</summary>
public sealed class OccurrenceRow(EventOccurrence occurrence, string sourceName, DateTimeOffset now)
{
    public EventOccurrence Occurrence { get; } = occurrence;

    public string Relative { get; } = RelativeTime.Format(occurrence.At, now);

    public string Exact { get; } = occurrence.At.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.fff");

    public string Source { get; } = sourceName;
}
