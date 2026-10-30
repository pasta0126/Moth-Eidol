using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using MothEidol.Core.Logs;
using MothEidol.Core.Projects;

namespace MothEidol.App.Views;

/// <summary>Where a project's parameters are edited: name, sources, alert settings and removal.</summary>
public partial class ProjectSettingsView : UserControl
{
    private readonly ObservableCollection<SourceRow> _rows = [];
    private ProjectService? _projects;
    private TailingService? _tailing;
    private Action? _removed;
    private long _projectId;
    private bool _loading;

    public ProjectSettingsView()
    {
        InitializeComponent();
        SourceList.ItemsSource = _rows;
        FileRadio.IsCheckedChanged += (_, _) => PatternBox.IsVisible = FolderRadio.IsChecked == true;
        FolderRadio.IsCheckedChanged += (_, _) => PatternBox.IsVisible = FolderRadio.IsChecked == true;
        BrowseButton.Click += OnBrowseClicked;
        AddButton.Click += OnAddClicked;
        RenameButton.Click += OnRenameClicked;
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
        _loading = false;

        GeneralError.Text = SourceError.Text = AlertError.Text = "";
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
