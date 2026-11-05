using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Threading;
using MothEidol.Core.Alerts;
using MothEidol.Core.Logs;
using MothEidol.Core.Projects;

namespace MothEidol.App.Views;

/// <summary>The events of a project: grouped, pending first, with a detail view of each group's occurrences.</summary>
public partial class EventsView : UserControl
{
    private const int MaxListedGroups = 200;

    private readonly ObservableCollection<EventGroupRow> _rows = [];
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(1500) };
    private ProjectService? _projects;
    private IEventStore? _store;
    private long _projectId;
    private long? _openGroup;
    private long _shownOccurrence;
    private bool _loadingDetail;

    private WordWrapState? _wrap;

    public EventsView()
    {
        InitializeComponent();
        GroupList.ItemsSource = _rows;
        FilterCombo.SelectionChanged += (_, _) => Refresh();
        MarkAllButton.Click += OnMarkAllClicked;
        DetailBackButton.Click += (_, _) => CloseDetail();
        DetailSeenButton.Click += OnDetailSeenClicked;
        CopyButton.Click += OnCopyClicked;
        CopyAllItem.Click += async (_, _) =>
        {
            if (OccurrenceList.SelectedItem is OccurrenceRow row && TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
                await clipboard.SetTextAsync(OccurrenceCopyText.Build(row.Occurrence));
        };
        DetailRawToggle.IsCheckedChanged += (_, _) => ShowSelectedOccurrence();
        RangeCombo.SelectionChanged += (_, _) => UpdateTimeline();
        OccurrenceList.SelectionChanged += OnOccurrenceSelected;
        _timer.Tick += (_, _) => { if (IsEffectivelyVisible) Refresh(); };
    }

    public void Initialize(ProjectService projects, IEventStore store, WordWrapState wrap)
    {
        _wrap = wrap;
        DetailWrapCheck.IsCheckedChanged += (_, _) => wrap.Enabled = DetailWrapCheck.IsChecked == true;
        wrap.Changed += (_, _) => { ApplyWrap(); ShowSelectedOccurrence(); };
        ApplyWrap();
        _projects = projects;
        _store = store;
        _timer.Start();
    }

    /// <summary>Shows the events of a project, starting from its list.</summary>
    public void Bind(Project project)
    {
        if (_projectId != project.Id) _rows.Clear();
        _projectId = project.Id;
        CloseDetail();
    }

    private bool PendingOnly => FilterCombo.SelectedIndex == 0;

    private void Refresh()
    {
        if (_store is null || _projectId == 0) return;

        var now = DateTimeOffset.UtcNow;
        var all = _store.Groups(_projectId);
        var shown = EventGroupOrder.ForDisplay(PendingOnly ? all.Where(g => g.IsPending) : all).Take(MaxListedGroups).ToList();
        Reconcile(shown, now);

        var pending = all.Where(g => g.IsPending).ToList();
        SummaryText.Text = pending.Count == 0
            ? $"{all.Count} event group(s), nothing pending"
            : $"{pending.Count} group(s) pending · {pending.Sum(g => g.PendingCount):N0} occurrences";
        MarkAllButton.IsEnabled = pending.Count > 0;
        EmptyText.Text = shown.Count > 0 ? ""
            : all.Count == 0 ? "No events yet. Errors (and warnings, if enabled in the settings) will show up here as they are logged."
            : "Nothing pending. Choose \"All events\" to see the ones you already reviewed.";

        if (_openGroup is { } open) ShowDetail(open, refreshOnly: true);
    }

    /// <summary>Updates the list in place (moving rows instead of rebuilding) so scrolling is not disturbed.</summary>
    private void Reconcile(IReadOnlyList<EventGroup> target, DateTimeOffset now)
    {
        for (var i = 0; i < target.Count; i++)
        {
            var group = target[i];
            var series = _store!.GroupSeries(group.Id, now, 24);
            var current = _rows.FirstOrDefault(r => r.Id == group.Id);
            if (current is null)
            {
                _rows.Insert(i, new EventGroupRow(group, series, now));
                continue;
            }

            current.Update(group, series, now);
            var from = _rows.IndexOf(current);
            if (from != i) _rows.Move(from, i);
        }
        while (_rows.Count > target.Count) _rows.RemoveAt(_rows.Count - 1);
    }

    private void OnGroupRowAttached(object? sender, Avalonia.VisualTreeAttachmentEventArgs e)
    {
        if (sender is Control row) ClickGesture.Attach(row, OpenGroupRow);
    }

    private void OnOccurrenceRowAttached(object? sender, Avalonia.VisualTreeAttachmentEventArgs e)
    {
        if (sender is Control row)
            ClickGesture.Attach(row, c => { if (c.DataContext is OccurrenceRow o) OccurrenceList.SelectedItem = o; });
    }

    private void OpenGroupRow(Control control)
    {
        if (control.DataContext is not EventGroupRow row) return;

        GroupList.SelectedItem = null;
        _openGroup = row.Id;
        CopiedText.Text = "";
        DetailRawToggle.IsChecked = false;
        _shownOccurrence = 0;
        ShowDetail(row.Id, refreshOnly: false);
    }

    private void ShowDetail(long groupId, bool refreshOnly)
    {
        if (_store is null || _projects is null) return;

        var group = _store.Groups(_projectId).FirstOrDefault(g => g.Id == groupId);
        if (group is null)
        {
            CloseDetail();
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var row = new EventGroupRow(group, [], now);
        DetailLevel.Text = row.LevelText;
        DetailLevel.Foreground = row.LevelBrush;
        DetailTitle.Text = group.Title;
        DetailLocation.Text = row.HasLocation ? $"in {group.Location}" : "";
        DetailLocation.IsVisible = row.HasLocation;
        NewBadge.IsVisible = group.IsNew(now);
        RegressedBadge.IsVisible = group.IsRegressed;
        DetailMeta.Text = $"{group.Count:N0} occurrence(s) · first seen {RelativeTime.Format(group.FirstSeen, now)} · " +
                          $"last seen {RelativeTime.Format(group.LastSeen, now)} · {row.PendingText}";
        ToolTip.SetTip(DetailMeta, $"First {group.FirstSeen.ToLocalTime():yyyy-MM-dd HH:mm:ss}\nLast {group.LastSeen.ToLocalTime():yyyy-MM-dd HH:mm:ss}");
        DetailSeenButton.IsEnabled = group.IsPending;
        UpdateTimeline(group, now);

        var occurrences = _store.Occurrences(groupId);
        DetailNote.Text = occurrences.Count < group.Count
            ? $"The {occurrences.Count} most recent of {group.Count:N0} occurrences, newest first."
            : "Occurrences, newest first.";

        // On a periodic refresh keep what the user is reading unless a newer occurrence arrived.
        var newest = occurrences.Count > 0 ? occurrences[0].Id : 0;
        if (!(refreshOnly && newest == _shownOccurrence))
        {
            var sources = _projects.SourcesOf(_projectId).ToDictionary(s => s.Id, s => Path.GetFileName(s.DisplayName));
            _loadingDetail = true;
            var rows = occurrences
                .Select(o => new OccurrenceRow(o, sources.GetValueOrDefault(o.SourceId, $"source {o.SourceId}"), now))
                .ToList();
            OccurrenceList.ItemsSource = rows;
            _loadingDetail = false;
            OccurrenceList.SelectedIndex = rows.Count > 0 ? 0 : -1;
            _shownOccurrence = newest;
            if (rows.Count == 0) OccurrenceDetail.Content = null;
        }

        ListPanel.IsVisible = false;
        DetailPanel.IsVisible = true;
    }

    private void OnOccurrenceSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (_loadingDetail || OccurrenceList.SelectedItem is not OccurrenceRow row) return;
        ShowOccurrence(row);
    }

    private void ShowSelectedOccurrence()
    {
        if (OccurrenceList.SelectedItem is OccurrenceRow row) ShowOccurrence(row);
        else DetailRawBox.Text = "";
    }

    /// <summary>Shows an occurrence styled, or as the same plain text "Copy all" produces when Raw is on.</summary>
    private void ShowOccurrence(OccurrenceRow row)
    {
        var raw = DetailRawToggle.IsChecked == true;
        DetailScroll.IsVisible = !raw;
        DetailRawBox.IsVisible = raw;
        if (raw)
        {
            DetailRawBox.Text = OccurrenceCopyText.Build(row.Occurrence);
            OccurrenceDetail.Content = null;
        }
        else
        {
            OccurrenceDetail.Content = OccurrenceDetailBuilder.Build(row.Occurrence, _wrap?.Enabled ?? true);
        }
    }

    /// <summary>Copies one occurrence without selecting it, so the displayed detail does not change.</summary>
    private async void OnOccurrenceCopyClicked(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if ((sender as Control)?.DataContext is not OccurrenceRow row) return;

        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is null)
        {
            CopiedText.Text = "The clipboard is not available.";
            return;
        }

        await clipboard.SetTextAsync(OccurrenceCopyText.Build(row.Occurrence));
        CopiedText.Text = $"Occurrence from {row.Relative} copied to the clipboard.";
    }

    private void ApplyWrap()
    {
        var wrap = _wrap?.Enabled ?? true;
        DetailWrapCheck.IsChecked = wrap;
        DetailScroll.HorizontalScrollBarVisibility = wrap ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto;
        DetailRawBox.TextWrapping = wrap ? Avalonia.Media.TextWrapping.Wrap : Avalonia.Media.TextWrapping.NoWrap;
        ScrollViewer.SetHorizontalScrollBarVisibility(
            DetailRawBox, wrap ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto);
    }

    private void UpdateTimeline()
    {
        if (_store is null || _openGroup is not { } id) return;
        var group = _store.Groups(_projectId).FirstOrDefault(g => g.Id == id);
        if (group is not null) UpdateTimeline(group, DateTimeOffset.UtcNow);
    }

    /// <summary>Hourly bars for the last 24 hours, or one bar per day for the last 7 days.</summary>
    private void UpdateTimeline(EventGroup group, DateTimeOffset now)
    {
        if (_store is null) return;

        IReadOnlyList<int> values;
        if (RangeCombo.SelectedIndex == 1)
        {
            values = TimeSeries.ByDay(_store.GroupSeries(group.Id, now, 24 * 7), now, 7);
            AxisStart.Text = "7 days ago";
            AxisEnd.Text = "today";
            PeakText.Text = $"peak {values.Max():N0} per day";
        }
        else
        {
            values = _store.GroupSeries(group.Id, now, 24);
            AxisStart.Text = "24 h ago";
            AxisEnd.Text = "now";
            PeakText.Text = $"peak {values.Max():N0} per hour";
        }

        Timeline.Values = values;
        Timeline.Brush = ThemeBrushes.Get(group.Level >= EntryLevel.Error ? "MothErrorBrush" : "MothWarningBrush");
    }

    private void CloseDetail()
    {
        _openGroup = null;
        DetailPanel.IsVisible = false;
        ListPanel.IsVisible = true;
        CopiedText.Text = "";
        Refresh();
    }

    private void OnDetailSeenClicked(object? sender, RoutedEventArgs e)
    {
        if (_store is null || _openGroup is not { } id) return;
        _store.MarkSeen(id, DateTimeOffset.UtcNow);
        CloseDetail();
    }

    private async void OnCopyClicked(object? sender, RoutedEventArgs e)
    {
        if (_store is null || _projects is null || _openGroup is not { } id) return;

        var group = _store.Groups(_projectId).FirstOrDefault(g => g.Id == id);
        var projectName = _projects.Get(_projectId)?.Name ?? "";
        if (group is null) return;

        var text = EventSummaryText.Build(projectName, group, _store.Occurrences(id).FirstOrDefault());
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is null)
        {
            CopiedText.Text = "The clipboard is not available.";
            return;
        }

        await clipboard.SetTextAsync(text);
        CopiedText.Text = "Summary copied to the clipboard.";
    }

    private void OnMarkAllClicked(object? sender, RoutedEventArgs e)
    {
        if (_store is null) return;
        _store.MarkAllSeen(_projectId, DateTimeOffset.UtcNow);
        Refresh();
    }
}
