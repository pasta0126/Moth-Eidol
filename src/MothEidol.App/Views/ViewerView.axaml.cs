using System.Collections.Concurrent;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using MothEidol.Core.Logs;
using MothEidol.Core.Settings;

namespace MothEidol.App.Views;

public partial class ViewerView : UserControl
{
    private readonly AvaloniaList<EntryItem> _items = [];
    private readonly ConcurrentDictionary<long, ViewBuffer<LogEntry>> _buffers = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(150) };
    private AppSettings? _settings;
    private ScrollViewer? _scroll;
    private SourceRow? _selected;
    private long _position;
    private bool _scrolledAway;

    public ViewerView()
    {
        InitializeComponent();
        LinesList.ItemsSource = _items;
        LinesList.TemplateApplied += OnListTemplateApplied;
        SourceCombo.SelectionChanged += OnSourceChanged;
        LevelCombo.SelectionChanged += (_, _) => ResetView();
        ClearButton.Click += OnClearClicked;
        PauseToggle.IsCheckedChanged += (_, _) => UpdateScrollState();
        MaxLinesBox.ValueChanged += OnMaxLinesChanged;
        _timer.Tick += (_, _) => Sync();
        UpdateScrollState();
    }

    public void Initialize(SourceListModel model, EntryBus entries, AppSettings settings)
    {
        _settings = settings;
        MaxLinesBox.Value = settings.ViewerMaxLines;
        SourceCombo.ItemsSource = model.Rows;
        if (model.Rows.Count > 0) SourceCombo.SelectedIndex = 0;
        UpdateHint(model);
        model.Rows.CollectionChanged += (_, _) => UpdateHint(model);

        // Entries are collected for every source even while another one is shown, so switching loses nothing.
        var subscription = entries.Subscribe();
        _ = Task.Run(async () =>
        {
            await foreach (var entry in subscription.Reader.ReadAllAsync())
            {
                BufferFor(entry.SourceId).Add(entry);
            }
        });

        _timer.Start();
    }

    private ViewBuffer<LogEntry> BufferFor(long sourceId) =>
        _buffers.GetOrAdd(sourceId, _ => new ViewBuffer<LogEntry>(_settings?.ViewerMaxLines ?? 5000));

    private void UpdateHint(SourceListModel model)
    {
        HintText.Text = model.Rows.Count == 0
            ? "No sources yet. Add one in the Sources tab to see its entries here."
            : "New entries appear as they are written. Click a multi-line entry to expand its stack trace.";
        if (SourceCombo.SelectedItem is null && model.Rows.Count > 0) SourceCombo.SelectedIndex = 0;
    }

    private void OnSourceChanged(object? sender, SelectionChangedEventArgs e)
    {
        _selected = SourceCombo.SelectedItem as SourceRow;
        _scrolledAway = false;
        ResetView();
    }

    /// <summary>Rebuilds the view from the retained entries (source or filter changed).</summary>
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
        if (_selected is null) return;
        var buffer = BufferFor(_selected.Id);
        buffer.Clear();
        _position = buffer.NextSequence;
        _items.Clear();
    }

    private void OnMaxLinesChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_settings is null || e.NewValue is not { } value) return;

        var max = (int)value;
        _settings.ViewerMaxLines = max;
        foreach (var buffer in _buffers.Values) buffer.MaxLines = max;
        TrimView(max);
    }

    private void Sync()
    {
        if (_selected is null || !IsEffectivelyVisible) return;

        var buffer = BufferFor(_selected.Id);
        var (entries, next) = buffer.ReadSince(_position);
        _position = next;

        // The filter only changes what is displayed: every entry stays monitored and buffered.
        var minimum = MinimumLevel;
        var shown = entries.Where(e => e.Level >= minimum).Select(e => new EntryItem(e)).ToList();
        if (shown.Count == 0) return;

        _items.AddRange(shown);
        TrimView(buffer.MaxLines);

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
