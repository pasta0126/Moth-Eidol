using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using MothEidol.Core.Logs;
using MothEidol.Core.Settings;

namespace MothEidol.App.Views;

/// <summary>The live tail of one project: the entries of all its sources, optionally limited to one.</summary>
public partial class TailView : UserControl
{
    /// <summary>One choice of the source filter; a null id means every source of the project.</summary>
    private sealed record SourceChoice(long? SourceId, string Label)
    {
        public override string ToString() => Label;
    }

    private readonly AvaloniaList<EntryItem> _items = [];
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(150) };
    private ProjectTails? _tails;
    private AppSettings? _settings;
    private ViewBuffer<LogEntry>? _buffer;
    private ScrollViewer? _scroll;
    private long _position;
    private bool _scrolledAway;
    private bool _loading;

    public TailView()
    {
        InitializeComponent();
        LinesList.ItemsSource = _items;
        LinesList.TemplateApplied += OnListTemplateApplied;
        SourceCombo.SelectionChanged += (_, _) => { if (!_loading) ResetView(); };
        LevelCombo.SelectionChanged += (_, _) => ResetView();
        ClearButton.Click += OnClearClicked;
        PauseToggle.IsCheckedChanged += (_, _) => UpdateScrollState();
        MaxLinesBox.ValueChanged += OnMaxLinesChanged;
        _timer.Tick += (_, _) => Sync();
        UpdateScrollState();
    }

    public void Initialize(ProjectTails tails, AppSettings settings)
    {
        _tails = tails;
        _settings = settings;
        MaxLinesBox.Value = settings.ViewerMaxLines;
        _timer.Start();
    }

    /// <summary>Shows the tail of a project; call again when its sources change.</summary>
    public void Bind(long projectId, IReadOnlyList<LogSource> sources)
    {
        if (_tails is null) return;

        var changedProject = _buffer != _tails.For(projectId);
        _buffer = _tails.For(projectId);

        var previous = (SourceCombo.SelectedItem as SourceChoice)?.SourceId;
        var choices = new List<SourceChoice> { new(null, "All sources") };
        choices.AddRange(sources.Select(s => new SourceChoice(s.Id, s.DisplayName)));

        _loading = true;
        SourceCombo.ItemsSource = choices;
        SourceCombo.SelectedItem = changedProject ? choices[0] : choices.FirstOrDefault(c => c.SourceId == previous) ?? choices[0];
        _loading = false;

        HintText.Text = sources.Count == 0
            ? "This project has no sources yet. Add one in its Settings."
            : "New entries appear as they are written. Click a multi-line entry to expand its stack trace.";

        if (changedProject) _scrolledAway = false;
        ResetView();
    }

    /// <summary>Rebuilds the view from the retained entries (project, source or filter changed).</summary>
    private void ResetView()
    {
        _position = 0;
        _items.Clear();
        UpdateScrollState();
        Sync();
    }

    private EntryLevel MinimumLevel => LevelCombo.SelectedIndex switch
    {
        1 => EntryLevel.Warning,
        2 => EntryLevel.Error,
        _ => EntryLevel.Unknown,
    };

    private void OnClearClicked(object? sender, RoutedEventArgs e)
    {
        if (_buffer is null) return;
        _buffer.Clear();
        _position = _buffer.NextSequence;
        _items.Clear();
    }

    private void OnMaxLinesChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_settings is null || _tails is null || e.NewValue is not { } value) return;

        var max = (int)value;
        _settings.ViewerMaxLines = max;
        _tails.MaxLines = max;
        TrimView(max);
    }

    private void Sync()
    {
        if (_buffer is null || !IsEffectivelyVisible) return;

        var (entries, next) = _buffer.ReadSince(_position);
        _position = next;

        // The filters only change what is displayed: every entry stays monitored and buffered.
        var minimum = MinimumLevel;
        var source = (SourceCombo.SelectedItem as SourceChoice)?.SourceId;
        var shown = entries
            .Where(e => e.Level >= minimum && (source is null || e.SourceId == source))
            .Select(e => new EntryItem(e))
            .ToList();
        if (shown.Count == 0) return;

        _items.AddRange(shown);
        TrimView(_buffer.MaxLines);

        if (PauseToggle.IsChecked != true && !_scrolledAway)
        {
            Dispatcher.UIThread.Post(() => _scroll?.ScrollToEnd(), DispatcherPriority.Background);
        }
    }

    /// <summary>Drops the oldest entries from the view only; monitoring is unaffected.</summary>
    private void TrimView(int max)
    {
        if (_items.Count > max) _items.RemoveRange(0, _items.Count - max);
    }

    private void OnEntryTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Control { DataContext: EntryItem { IsMultiLine: true } item })
        {
            item.Expanded = !item.Expanded;
        }
    }

    private void OnListTemplateApplied(object? sender, TemplateAppliedEventArgs e)
    {
        _scroll = e.NameScope.Find<ScrollViewer>("PART_ScrollViewer");
        if (_scroll is not null) _scroll.ScrollChanged += OnScrollChanged;
    }

    /// <summary>Scrolling up suspends auto-scroll; coming back to the end resumes it.</summary>
    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (_scroll is null || e.OffsetDelta.Y == 0) return;

        var atEnd = _scroll.Offset.Y + _scroll.Viewport.Height >= _scroll.Extent.Height - 4;
        _scrolledAway = !atEnd;
        UpdateScrollState();
    }

    private void UpdateScrollState() =>
        ScrollStateText.Text = PauseToggle.IsChecked == true ? "Auto-scroll paused"
            : _scrolledAway ? "Scrolled up (not following)"
            : "Following";
}
