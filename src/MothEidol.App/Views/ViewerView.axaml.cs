using System.Collections.Concurrent;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Threading;
using MothEidol.Core.Logs;
using MothEidol.Core.Settings;

namespace MothEidol.App.Views;

public partial class ViewerView : UserControl
{
    private readonly AvaloniaList<string> _lines = [];
    private readonly ConcurrentDictionary<long, LineViewBuffer> _buffers = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(150) };
    private AppSettings? _settings;
    private ScrollViewer? _scroll;
    private SourceRow? _selected;
    private long _position;
    private bool _scrolledAway;

    public ViewerView()
    {
        InitializeComponent();
        LinesList.ItemsSource = _lines;
        LinesList.TemplateApplied += OnListTemplateApplied;
        SourceCombo.SelectionChanged += OnSourceChanged;
        ClearButton.Click += OnClearClicked;
        PauseToggle.IsCheckedChanged += (_, _) => UpdateScrollState();
        MaxLinesBox.ValueChanged += OnMaxLinesChanged;
        _timer.Tick += (_, _) => Sync();
        UpdateScrollState();
    }

    public void Initialize(TailingService service, SourceListModel model, AppSettings settings)
    {
        _settings = settings;
        MaxLinesBox.Value = settings.ViewerMaxLines;
        SourceCombo.ItemsSource = model.Rows;
        if (model.Rows.Count > 0) SourceCombo.SelectedIndex = 0;
        UpdateHint(model);
        model.Rows.CollectionChanged += (_, _) => UpdateHint(model);

        // Lines are collected for every source even while another one is shown, so switching loses nothing.
        var subscription = service.Lines.Subscribe();
        _ = Task.Run(async () =>
        {
            await foreach (var line in subscription.Reader.ReadAllAsync())
            {
                BufferFor(line.SourceId).Add(line.Text);
            }
        });

        _timer.Start();
    }

    private LineViewBuffer BufferFor(long sourceId) =>
        _buffers.GetOrAdd(sourceId, _ => new LineViewBuffer(_settings?.ViewerMaxLines ?? 5000));

    private void UpdateHint(SourceListModel model)
    {
        HintText.Text = model.Rows.Count == 0
            ? "No sources yet. Add one in the Sources tab to see its lines here."
            : "New lines appear as they are written. Existing content is not shown.";
        if (SourceCombo.SelectedItem is null && model.Rows.Count > 0) SourceCombo.SelectedIndex = 0;
    }

    private void OnSourceChanged(object? sender, SelectionChangedEventArgs e)
    {
        _selected = SourceCombo.SelectedItem as SourceRow;
        _position = 0;
        _lines.Clear();
        _scrolledAway = false;
        UpdateScrollState();
        Sync();
    }

    private void OnClearClicked(object? sender, RoutedEventArgs e)
    {
        if (_selected is null) return;
        var buffer = BufferFor(_selected.Id);
        buffer.Clear();
        _position = buffer.NextSequence;
        _lines.Clear();
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
        var (lines, next) = buffer.ReadSince(_position);
        _position = next;
        if (lines.Count == 0) return;

        _lines.AddRange(lines);
        TrimView(buffer.MaxLines);

        if (PauseToggle.IsChecked != true && !_scrolledAway)
        {
            Dispatcher.UIThread.Post(() => _scroll?.ScrollToEnd(), DispatcherPriority.Background);
        }
    }

    /// <summary>Drops the oldest lines from the view only; monitoring is unaffected.</summary>
    private void TrimView(int max)
    {
        if (_lines.Count > max) _lines.RemoveRange(0, _lines.Count - max);
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
