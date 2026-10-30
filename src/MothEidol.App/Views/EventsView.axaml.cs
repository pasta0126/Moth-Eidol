using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using MothEidol.Core.Alerts;
using MothEidol.Core.Projects;

namespace MothEidol.App.Views;

/// <summary>The events of a project: grouped, pending first, with a detail view of each group's occurrences.</summary>
public partial class EventsView : UserControl
{
    private readonly ObservableCollection<EventGroupRow> _rows = [];
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(1500) };
    private ProjectService? _projects;
    private IEventStore? _store;
    private long _projectId;
    private long? _openGroup;

    public EventsView()
    {
        InitializeComponent();
        GroupList.ItemsSource = _rows;
        FilterCombo.SelectionChanged += (_, _) => Refresh();
        MarkAllButton.Click += OnMarkAllClicked;
        GroupList.SelectionChanged += OnGroupSelected;
        DetailBackButton.Click += (_, _) => CloseDetail();
        DetailSeenButton.Click += OnDetailSeenClicked;
        _timer.Tick += (_, _) => { if (IsEffectivelyVisible) Refresh(); };
    }

    public void Initialize(ProjectService projects, IEventStore store)
    {
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
        Refresh();
    }

    private bool PendingOnly => FilterCombo.SelectedIndex == 0;

    private void Refresh()
    {
        if (_store is null || _projectId == 0) return;

        var all = _store.Groups(_projectId);
        var shown = EventGroupOrder.ForDisplay(PendingOnly ? all.Where(g => g.IsPending) : all);
        Reconcile(shown);

        var pending = all.Where(g => g.IsPending).ToList();
        SummaryText.Text = pending.Count == 0
            ? $"{all.Count} event group(s), nothing pending"
            : $"{pending.Count} group(s) pending · {pending.Sum(g => g.PendingCount):N0} occurrences";
        MarkAllButton.IsEnabled = pending.Count > 0;
        EmptyText.Text = shown.Count > 0 ? ""
            : all.Count == 0 ? "No events yet. Errors (and warnings, if enabled in the settings) will show up here as they are logged."
            : "Nothing pending. Choose \"All events\" to see the ones you already reviewed.";

        if (_openGroup is { } open) ShowDetail(open, keepScroll: true);
    }

    /// <summary>Updates the list in place (moving rows instead of rebuilding) so scrolling is not disturbed.</summary>
    private void Reconcile(IReadOnlyList<EventGroup> target)
    {
        for (var i = 0; i < target.Count; i++)
        {
            var group = target[i];
            var current = _rows.FirstOrDefault(r => r.Id == group.Id);
            if (current is null)
            {
                _rows.Insert(i, new EventGroupRow(group));
                continue;
            }

            current.Update(group);
            var from = _rows.IndexOf(current);
            if (from != i) _rows.Move(from, i);
        }
        while (_rows.Count > target.Count) _rows.RemoveAt(_rows.Count - 1);
    }

    private void OnGroupSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (GroupList.SelectedItem is not EventGroupRow row) return;

        GroupList.SelectedItem = null;
        _openGroup = row.Id;
        ShowDetail(row.Id, keepScroll: false);
    }

    private void ShowDetail(long groupId, bool keepScroll)
    {
        if (_store is null || _projects is null) return;

        var group = _store.Groups(_projectId).FirstOrDefault(g => g.Id == groupId);
        if (group is null)
        {
            CloseDetail();
            return;
        }

        var row = new EventGroupRow(group);
        DetailTitle.Text = $"{row.LevelText}  {group.Title}";
        DetailMeta.Text = $"{group.Count:N0} occurrence(s) · first {group.FirstSeen.ToLocalTime():yyyy-MM-dd HH:mm:ss} · last {row.LastSeenText} · {row.PendingText}";
        DetailSeenButton.IsEnabled = group.IsPending;

        var occurrences = _store.Occurrences(groupId);
        DetailNote.Text = occurrences.Count < group.Count
            ? $"Showing the {occurrences.Count} most recent of {group.Count:N0} occurrences, newest first."
            : "Occurrences, newest first.";

        if (keepScroll && OccurrenceList.ItemCount == occurrences.Count && occurrences.Count > 0 && FirstMatches(occurrences[0])) return;

        var sources = _projects.SourcesOf(_projectId).ToDictionary(s => s.Id, s => Path.GetFileName(s.DisplayName));
        OccurrenceList.ItemsSource = occurrences
            .Select(o => new OccurrenceRow(o, sources.GetValueOrDefault(o.SourceId, $"source {o.SourceId}")))
            .ToList();

        ListPanel.IsVisible = false;
        DetailPanel.IsVisible = true;
    }

    private bool FirstMatches(EventOccurrence newest) =>
        OccurrenceList.ItemsSource is IEnumerable<OccurrenceRow> rows &&
        rows.FirstOrDefault()?.Time == newest.At.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.fff");

    private void CloseDetail()
    {
        _openGroup = null;
        DetailPanel.IsVisible = false;
        ListPanel.IsVisible = true;
        Refresh();
    }

    private void OnDetailSeenClicked(object? sender, RoutedEventArgs e)
    {
        if (_store is null || _openGroup is not { } id) return;
        _store.MarkSeen(id, DateTimeOffset.UtcNow);
        CloseDetail();
    }

    private void OnMarkAllClicked(object? sender, RoutedEventArgs e)
    {
        if (_store is null) return;
        _store.MarkAllSeen(_projectId, DateTimeOffset.UtcNow);
        Refresh();
    }
}
