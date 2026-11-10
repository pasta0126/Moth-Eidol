using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using MothEidol.Core.Logs;
using MothEidol.Core.Projects;

namespace MothEidol.App.Views;

/// <summary>Where a project's parameters are edited: name, sources, alert settings and removal.</summary>
public partial class ProjectSettingsView : UserControl
{
    private readonly ObservableCollection<SourceRow> _rows = [];
    private readonly LogLocationOpener _opener = new(new MothEidol.Core.Platform.SystemFileManager());
    private ProjectService? _projects;
    private TailingService? _tailing;
    private Action? _removed;
    private long _projectId;
    private bool _loading;
    private string? _accent;
    private readonly List<(Border Swatch, string Hex)> _swatches = [];

    public ProjectSettingsView()
    {
        InitializeComponent();
        SourceList.ItemsSource = _rows;
        FileRadio.IsCheckedChanged += (_, _) => PatternBox.IsVisible = FolderRadio.IsChecked == true;
        FolderRadio.IsCheckedChanged += (_, _) => PatternBox.IsVisible = FolderRadio.IsChecked == true;
        BrowseButton.Click += OnBrowseClicked;
        AddButton.Click += OnAddClicked;
        RenameButton.Click += OnRenameClicked;
        BuildSwatches();
        ApplyAccentButton.Click += (_, _) => ApplyAccent(HexBox.Text);
        DefaultAccentButton.Click += (_, _) => ApplyAccent(null);
        HexBox.KeyDown += (_, e) => { if (e.Key == Key.Enter) ApplyAccent(HexBox.Text); };
        EnabledCheck.Click += OnEnabledClicked;
        WarningsCheck.Click += (_, _) => SaveAlerts();
        ModeCombo.SelectionChanged += (_, _) => { UpdateMinutesLabel(); SaveAlerts(); };
        MinutesBox.ValueChanged += (_, _) => SaveAlerts();
        RemoveButton.Click += (_, _) => ConfirmPanel.IsVisible = true;
        CancelRemoveButton.Click += (_, _) => ConfirmPanel.IsVisible = false;
        ConfirmRemoveButton.Click += OnConfirmRemoveClicked;
    }

    public void Initialize(ProjectService projects, TailingService tailing, Action removed)
    {
        _projects = projects;
        _tailing = tailing;
        _removed = removed;
        projects.Changed += (_, _) => Dispatcher.UIThread.Post(RefreshRows);
        tailing.StatusChanged += (_, _) => Dispatcher.UIThread.Post(RefreshRows);
    }

    /// <summary>Shows the settings of a project.</summary>
    public void Bind(Project project)
    {
        _projectId = project.Id;
        _loading = true;
        NameBox.Text = project.Name;
        EnabledCheck.IsChecked = project.Enabled;
        WarningsCheck.IsChecked = project.Alerts.WarningAlerts;
        ModeCombo.SelectedIndex = project.Alerts.Mode == FrequencyMode.Immediate ? 0 : 1;
        MinutesBox.Value = project.Alerts.Minutes;
        UpdateMinutesLabel();
        ShowAccent(project.AccentColor);
        _loading = false;

        GeneralError.Text = SourceError.Text = AlertError.Text = AccentError.Text = "";
        ConfirmPanel.IsVisible = false;
        _rows.Clear();
        RefreshRows();
    }

    private void RefreshRows()
    {
        if (_projects is null || _tailing is null) return;

        var sources = _projects.SourcesOf(_projectId);
        for (var i = _rows.Count - 1; i >= 0; i--)
        {
            if (sources.All(s => s.Id != _rows[i].Id)) _rows.RemoveAt(i);
        }
        foreach (var source in sources)
        {
            var (status, detail) = _tailing.GetStatus(source.Id);
            var row = _rows.FirstOrDefault(r => r.Id == source.Id);
            if (row is null) _rows.Add(new SourceRow(source, status, detail));
            else row.Update(source, status, detail);
        }
        NoSourcesText.IsVisible = _rows.Count == 0;
    }

    private void BuildSwatches()
    {
        foreach (var hex in AccentColor.Palette)
        {
            var swatch = new Border
            {
                Width = 28, Height = 28, Margin = new Avalonia.Thickness(0, 0, 6, 6), CornerRadius = new Avalonia.CornerRadius(4),
                Background = new SolidColorBrush(Color.Parse(hex)), BorderThickness = new Avalonia.Thickness(2),
                Cursor = new Cursor(StandardCursorType.Hand),
            };
            ToolTip.SetTip(swatch, hex);
            swatch.Tapped += (_, _) => ApplyAccent(hex);
            _swatches.Add((swatch, hex));
            SwatchPanel.Children.Add(swatch);
        }
    }

    /// <summary>Shows which accent is in use: the selected swatch, the preview and the hex field.</summary>
    private void ShowAccent(string? hex)
    {
        _accent = hex;
        HexBox.Text = hex ?? "";
        AccentPreview.Background = hex is not null && Color.TryParse(hex, out var color)
            ? new SolidColorBrush(color)
            : ThemeBrushes.Get("MothPrimaryBrush");
        var selected = ThemeBrushes.Get("MothTextBrush");
        foreach (var (swatch, swatchHex) in _swatches)
            swatch.BorderBrush = swatchHex == hex ? selected : Brushes.Transparent;
    }

    /// <summary>Saves the accent right away (null returns to the default); an invalid value is reported and not saved.</summary>
    private void ApplyAccent(string? text)
    {
        if (_projects is null) return;
        try
        {
            _projects.SetAccent(_projectId, text);
            AccentError.Text = "";
            ShowAccent(_projects.Get(_projectId)?.AccentColor);
        }
        catch (ArgumentException ex)
        {
            AccentError.Text = ex.Message;
        }
        catch (Exception ex)
        {
            AccentError.Text = $"Could not change the color: {ex.Message}";
        }
    }

    private void UpdateMinutesLabel() =>
        MinutesLabel.Text = ModeCombo.SelectedIndex == 1 ? "Interval (minutes)" : "Cooldown (minutes)";

    private void OnRenameClicked(object? sender, RoutedEventArgs e)
    {
        if (_projects is null) return;
        try
        {
            _projects.Rename(_projectId, NameBox.Text ?? "");
            GeneralError.Text = "";
        }
        catch (ArgumentException ex)
        {
            GeneralError.Text = ex.Message;
        }
    }

    private async void OnEnabledClicked(object? sender, RoutedEventArgs e)
    {
        if (_projects is null) return;
        try
        {
            await _projects.SetEnabledAsync(_projectId, EnabledCheck.IsChecked == true);
        }
        catch (Exception ex)
        {
            GeneralError.Text = $"Could not change the project: {ex.Message}";
        }
    }

    /// <summary>Alert settings apply as soon as they change; there is no separate save step.</summary>
    private void SaveAlerts()
    {
        if (_loading || _projects is null) return;
        try
        {
            var mode = ModeCombo.SelectedIndex == 1 ? FrequencyMode.Summary : FrequencyMode.Immediate;
            var minutes = (int)(MinutesBox.Value ?? AlertSettings.Default.Minutes);
            _projects.UpdateAlerts(_projectId, new AlertSettings(WarningsCheck.IsChecked == true, mode, minutes));
            AlertError.Text = "";
        }
        catch (ArgumentException ex)
        {
            AlertError.Text = ex.Message;
        }
    }

    private async void OnBrowseClicked(object? sender, RoutedEventArgs e)
    {
        var storage = TopLevel.GetTopLevel(this)?.StorageProvider;
        if (storage is null) return;

        if (FolderRadio.IsChecked == true)
        {
            var folders = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "Select the log folder" });
            if (folders.Count > 0) PathBox.Text = folders[0].TryGetLocalPath();
        }
        else
        {
            var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions { Title = "Select the log file" });
            if (files.Count > 0) PathBox.Text = files[0].TryGetLocalPath();
        }
    }

    private void OnAddClicked(object? sender, RoutedEventArgs e)
    {
        if (_projects is null) return;
        try
        {
            var kind = FolderRadio.IsChecked == true ? SourceKind.Folder : SourceKind.File;
            _projects.AddSource(_projectId, kind, PathBox.Text ?? "", PatternBox.Text);
            PathBox.Text = PatternBox.Text = SourceError.Text = "";
        }
        catch (ArgumentException ex)
        {
            SourceError.Text = ex.Message;
        }
        catch (Exception ex)
        {
            SourceError.Text = $"Could not add the source: {ex.Message}";
        }
    }

    private async void OnSourceEnabledClicked(object? sender, RoutedEventArgs e)
    {
        if (_projects is null || sender is not CheckBox { DataContext: SourceRow row } box) return;
        try
        {
            await _projects.SetSourceEnabledAsync(row.Id, box.IsChecked == true);
        }
        catch (Exception ex)
        {
            SourceError.Text = $"Could not change the source: {ex.Message}";
        }
    }

    private async void OnSourceOpenClicked(object? sender, RoutedEventArgs e)
    {
        if (_projects is null || sender is not Button { DataContext: SourceRow row }) return;

        SourceError.Text = "";
        if (_projects.SourcesOf(_projectId).FirstOrDefault(s => s.Id == row.Id) is not { } source) return;
        var result = await _opener.OpenAsync(source);
        SourceError.Text = result.Message ?? "";
    }

    private async void OnSourceRemoveClicked(object? sender, RoutedEventArgs e)
    {
        if (_projects is null || sender is not Button { DataContext: SourceRow row }) return;
        try
        {
            await _projects.RemoveSourceAsync(row.Id);
        }
        catch (Exception ex)
        {
            SourceError.Text = $"Could not remove the source: {ex.Message}";
        }
    }

    private async void OnConfirmRemoveClicked(object? sender, RoutedEventArgs e)
    {
        if (_projects is null) return;
        try
        {
            await _projects.RemoveAsync(_projectId);
            _removed?.Invoke();
        }
        catch (Exception ex)
        {
            GeneralError.Text = $"Could not remove the project: {ex.Message}";
        }
    }
}
