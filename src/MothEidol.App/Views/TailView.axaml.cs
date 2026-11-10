using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Platform;
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
    private readonly LogLocationOpener _opener = new(new MothEidol.Core.Platform.SystemFileManager());
    private IReadOnlyList<LogSource> _sources = [];
    private WordWrapState? _wrap;
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
        ThemeBrushes.Changed += () => { foreach (var item in _items) item.RefreshTheme(); };
        LinesList.TemplateApplied += OnListTemplateApplied;
        SourceCombo.SelectionChanged += (_, _) => { if (!_loading) ResetView(); };
        LevelCombo.SelectionChanged += (_, _) => ResetView();
        ClearButton.Click += OnClearClicked;
        OpenFolderButton.Click += OnOpenFolderClicked;
        RawToggle.IsCheckedChanged += (_, _) => SetRaw(RawToggle.IsChecked == true);
        RawRefreshButton.Click += (_, _) => TakeSnapshot();
        CopySelectedItem.Click += (_, _) =>
            _ = CopyAsync((LinesList.SelectedItems ?? new List<object>()).OfType<EntryItem>().OrderBy(i => _items.IndexOf(i)));
        CopyVisibleItem.Click += (_, _) => _ = CopyAsync(_items);
        LinesList.KeyDown += OnListKeyDown;
        PauseToggle.IsCheckedChanged += (_, _) => UpdateScrollState();
        MaxLinesBox.ValueChanged += OnMaxLinesChanged;
        _timer.Tick += (_, _) => Sync();
        UpdateScrollState();
    }

    public void Initialize(ProjectTails tails, AppSettings settings, WordWrapState wrap)
    {
        _wrap = wrap;
        WrapCheck.IsCheckedChanged += (_, _) => wrap.Enabled = WrapCheck.IsChecked == true;
        wrap.Changed += (_, _) => ApplyWrap();
        ApplyWrap();
        _tails = tails;
        _settings = settings;
        MaxLinesBox.Value = settings.ViewerMaxLines;
        _timer.Start();
    }

    /// <summary>Shows the tail of a project; call again when its sources change.</summary>
    public void Bind(long projectId, IReadOnlyList<LogSource> sources)
    {
        if (_tails is null) return;

        _sources = sources;
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

        // In Raw the styled list is hidden: entries keep arriving into it, and the snapshot is not touched.
        if (!IsRaw && PauseToggle.IsChecked != true && !_scrolledAway)
        {
            Dispatcher.UIThread.Post(() => _scroll?.ScrollToEnd(), DispatcherPriority.Background);
        }
    }

    private bool IsRaw => RawToggle.IsChecked == true;

    /// <summary>Switches between the styled live list and the plain-text snapshot.</summary>
    private void SetRaw(bool raw)
    {
        RawBox.IsVisible = raw;
        RawRefreshButton.IsVisible = raw;
        LinesList.IsVisible = !raw;
        if (raw)
        {
            TakeSnapshot();
            return;
        }

        RawNote.IsVisible = false;
        RawBox.Text = "";
        UpdateScrollState();
        if (PauseToggle.IsChecked != true && !_scrolledAway)
            Dispatcher.UIThread.Post(() => _scroll?.ScrollToEnd(), DispatcherPriority.Background);
    }

    /// <summary>Captures the visible entries as plain text; it does not change while new entries arrive.</summary>
    private void TakeSnapshot()
    {
        var result = RawLogText.Build(_items.Select(i => i.Entry));
        RawBox.Text = result.Text;
        RawBox.CaretIndex = result.Text.Length;
        RawNote.IsVisible = true;
        RawNote.Text = result.Truncated
            ? $"Snapshot truncated to the last {RawLogText.DefaultMaxLines:N0} lines. Refresh to include new entries."
            : "Snapshot: new entries are not added until you press Refresh.";
    }

    /// <summary>Drops the oldest entries from the view only; monitoring is unaffected.</summary>
    private void TrimView(int max)
    {
        if (_items.Count > max) _items.RemoveRange(0, _items.Count - max);
    }

    /// <summary>Opens the folder of the selected source; with all sources, the shared folder or a choice of them.</summary>
    private async void OnOpenFolderClicked(object? sender, RoutedEventArgs e)
    {
        OpenMessage.Text = "";
        if ((SourceCombo.SelectedItem as SourceChoice)?.SourceId is { } id)
        {
            if (_sources.FirstOrDefault(s => s.Id == id) is { } source) await OpenAsync(() => _opener.OpenAsync(source));
            return;
        }

        var folders = LogLocationOpener.FoldersOf(_sources);
        if (folders.Count == 0)
        {
            OpenMessage.Text = "This project has no sources yet.";
        }
        else if (folders.Count == 1)
        {
            await OpenAsync(() => _opener.OpenFolderAsync(folders[0]));
        }
        else
        {
            var menu = new ContextMenu();
            foreach (var folder in folders)
            {
                var item = new MenuItem { Header = folder };
                item.Click += async (_, _) => await OpenAsync(() => _opener.OpenFolderAsync(folder));
                menu.Items.Add(item);
            }
            menu.Open(OpenFolderButton);
        }
    }

    private async Task OpenAsync(Func<Task<OpenResult>> open)
    {
        var result = await open();
        OpenMessage.Text = result.Message ?? "";
    }

    private void ApplyWrap()
    {
        var wrap = _wrap?.Enabled ?? true;
        WrapCheck.IsChecked = wrap;
        LinesList.Classes.Set("wrap", wrap);
        RawBox.TextWrapping = wrap ? Avalonia.Media.TextWrapping.Wrap : Avalonia.Media.TextWrapping.NoWrap;
        ScrollViewer.SetHorizontalScrollBarVisibility(
            RawBox, wrap ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto);
        ScrollViewer.SetHorizontalScrollBarVisibility(
            LinesList, wrap ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto);
    }

    private void OnEntryAttached(object? sender, Avalonia.VisualTreeAttachmentEventArgs e)
    {
        if (sender is Control row) ClickGesture.Attach(row, ToggleEntry);
    }

    private static void ToggleEntry(Control row)
    {
        if (row.DataContext is EntryItem { IsMultiLine: true } item) item.Expanded = !item.Expanded;
    }

    private async Task CopyAsync(IEnumerable<EntryItem> items)
    {
        var text = EntryCopyText.Build(items.Select(i => i.Entry));
        if (text.Length == 0) return;
        if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard) await clipboard.SetTextAsync(text);
    }

    private void OnListKeyDown(object? sender, KeyEventArgs e)
    {
        // Ctrl/Cmd+C copies the selected entries unless text inside a line is selected (that one copies itself).
        if (e.Key != Key.C || !e.KeyModifiers.HasFlag(KeyModifiers.Control) && !e.KeyModifiers.HasFlag(KeyModifiers.Meta)) return;
        if (LinesList.SelectedItems is not { Count: > 0 } selected) return;

        e.Handled = true;
        _ = CopyAsync(selected.OfType<EntryItem>().OrderBy(i => _items.IndexOf(i)));
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
        ScrollStateText.Text = IsRaw ? "Raw snapshot (auto-scroll paused)"
            : PauseToggle.IsChecked == true ? "Auto-scroll paused"
            : _scrolledAway ? "Scrolled up (not following)"
            : "Following";
}
