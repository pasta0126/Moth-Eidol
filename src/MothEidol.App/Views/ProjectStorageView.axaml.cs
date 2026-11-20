using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using MothEidol.Core.Alerts;
using MothEidol.Core.Projects;

namespace MothEidol.App.Views;

/// <summary>What a project keeps in the local database, its storage warning and the manual cleanups.</summary>
public partial class ProjectStorageView : UserControl
{
    private ProjectService? _projects;
    private long _projectId;
    private ProjectStorageUsage _usage = ProjectStorageUsage.Empty;
    private int? _confirmingKeepDays; // null with the panel open: confirming "clear all".
    private bool _busy;
    private int _measure; // Ignores a measure that finishes after another project was opened.

    public ProjectStorageView()
    {
        InitializeComponent();
        RefreshButton.Click += async (_, _) => await MeasureAsync();
        ApplyThresholdButton.Click += (_, _) => ApplyThreshold();
        ThresholdBox.KeyDown += (_, e) => { if (e.Key == Key.Enter) ApplyThreshold(); };
        ThresholdBox.TextChanged += (_, _) => ThresholdError.Text = "";
        DaysBox.TextChanged += (_, _) => UpdateActions();
        ClearButton.Click += (_, _) => AskClearAll();
        KeepButton.Click += (_, _) => AskKeepLastDays();
        CancelButton.Click += (_, _) => ConfirmPanel.IsVisible = false;
        ConfirmButton.Click += OnConfirmClicked;
    }

    public void Initialize(ProjectService projects) => _projects = projects;

    /// <summary>Shows a project's storage; the figures are measured when the section is shown.</summary>
    public void Bind(Project project)
    {
        _projectId = project.Id;
        _usage = ProjectStorageUsage.Empty;
        _measure++;
        ThresholdBox.Text = project.StorageWarningMb.ToString();
        DaysBox.Text = ProjectService.DefaultKeepDays.ToString();
        ThresholdError.Text = DaysError.Text = ResultText.Text = CleanupError.Text = "";
        ConfirmPanel.IsVisible = false;
        ShowUsage(ProjectStorageUsage.Empty, null);
    }

    /// <summary>Measures the project again (when the section is opened or the user refreshes it).</summary>
    public async Task MeasureAsync()
    {
        if (_projects is null) return;
        var measure = ++_measure;
        RefreshButton.IsEnabled = false;
        try
        {
            var usage = await _projects.UsageOfAsync(_projectId);
            var file = await _projects.DatabaseFileAsync();
            if (measure != _measure) return;
            _usage = usage;
            ShowUsage(usage, file);
        }
        catch (Exception ex)
        {
            CleanupError.Text = $"Could not measure the storage: {ex.Message}";
        }
        finally
        {
            RefreshButton.IsEnabled = true;
        }
    }

    private void ShowUsage(ProjectStorageUsage usage, DatabaseFileUsage? file)
    {
        var now = DateTimeOffset.Now;
        GroupsText.Text = usage.PendingGroups > 0
            ? $"{usage.Groups:N0} ({usage.PendingGroups:N0} pending)"
            : $"{usage.Groups:N0}";
        OccurrencesText.Text = $"{usage.Occurrences:N0}";
        BucketsText.Text = $"{usage.HourlyCounts:N0}";
        SizeText.Text = $"{ByteSize.Format(usage.ApproxBytes)} (approx.)";
        OldestText.Text = When(usage.Oldest, now);
        NewestText.Text = When(usage.Newest, now);
        FileText.Text = file is null
            ? "…"
            : file.WalBytes > 0
                ? $"{ByteSize.Format(file.TotalBytes)} (including {ByteSize.Format(file.WalBytes)} of write-ahead log)"
                : ByteSize.Format(file.TotalBytes);
        ShowWarning();
        UpdateActions();
    }

    private static string When(DateTimeOffset? at, DateTimeOffset now) =>
        at is { } value ? $"{RelativeTime.Format(value, now)} ({value.ToLocalTime():yyyy-MM-dd HH:mm})" : "—";

    private void ShowWarning()
    {
        if (_projects is null) return;
        var warning = _projects.StorageWarningOf(_projectId);
        OverPanel.IsVisible = warning.IsOver;
        OverText.Text = warning.SizeBytes is { } size
            ? $"This project uses about {ByteSize.Format(size)}, over its {warning.ThresholdMb:N0} MB limit. Clean up its history below."
            : "";
    }

    private void UpdateActions()
    {
        var hasEvents = !_usage.IsEmpty;
        var days = ValidDays();
        DaysError.Text = days is null
            ? $"Enter a whole number of days between {ProjectService.MinKeepDays} and {ProjectService.MaxKeepDays}."
            : "";
        KeepButton.IsEnabled = hasEvents && days is not null && !_busy;
        ClearButton.IsEnabled = hasEvents && !_busy;
        DaysBox.IsEnabled = ThresholdBox.IsEnabled = !_busy;
    }

    private int? ValidDays() =>
        int.TryParse(DaysBox.Text?.Trim(), out var days) && days is >= ProjectService.MinKeepDays and <= ProjectService.MaxKeepDays
            ? days
            : null;

    private void ApplyThreshold()
    {
        if (_projects is null) return;
        if (!int.TryParse(ThresholdBox.Text?.Trim(), out var megabytes)
            || megabytes is < Project.MinStorageWarningMb or > Project.MaxStorageWarningMb)
        {
            ThresholdError.Text =
                $"Enter a whole number of MB between {Project.MinStorageWarningMb} and {Project.MaxStorageWarningMb:N0}.";
            return;
        }

        try
        {
            _projects.SetStorageWarning(_projectId, megabytes);
            ThresholdError.Text = "";
            ShowWarning();
        }
        catch (Exception ex)
        {
            ThresholdError.Text = ex.Message;
        }
    }

    private void AskClearAll()
    {
        _confirmingKeepDays = null;
        var pending = _usage.PendingGroups > 0 ? $", {_usage.PendingGroups:N0} of them pending," : "";
        ConfirmText.Text =
            $"This removes all {_usage.Groups:N0} event groups of this project{pending} with their {_usage.Occurrences:N0} " +
            "occurrences and hourly counts. It cannot be undone.";
        ResultText.Text = CleanupError.Text = "";
        ConfirmPanel.IsVisible = true;
    }

    private void AskKeepLastDays()
    {
        if (_projects is null || ValidDays() is not { } days) return;
        _confirmingKeepDays = days;
        var (groups, pending) = _projects.KeepLastDaysPreview(_projectId, days);
        var removed = groups == 0
            ? "No event group is removed as a whole"
            : pending > 0
                ? $"{groups:N0} event groups with no activity since then are removed, {pending:N0} of them pending"
                : $"{groups:N0} event groups with no activity since then are removed";
        ConfirmText.Text =
            $"This removes the history older than {days} days. {removed}; the others keep their totals and lose only " +
            "their older occurrences and hourly counts. It cannot be undone.";
        ResultText.Text = CleanupError.Text = "";
        ConfirmPanel.IsVisible = true;
    }

    private async void OnConfirmClicked(object? sender, RoutedEventArgs e)
    {
        if (_projects is null || _busy) return;
        ConfirmPanel.IsVisible = false;
        _busy = true;
        BusyText.IsVisible = true;
        UpdateActions();
        try
        {
            var result = _confirmingKeepDays is { } days
                ? await _projects.KeepLastDaysAsync(_projectId, days)
                : await _projects.ClearEventsAsync(_projectId);
            await MeasureAsync();
            var file = await _projects.DatabaseFileAsync();
            ResultText.Text =
                $"Removed {result.Groups:N0} event groups and {result.Occurrences:N0} occurrences. " +
                $"The database file is now {ByteSize.Format(file.TotalBytes)}.";
        }
        catch (Exception ex)
        {
            CleanupError.Text = $"Could not clean up: {ex.Message}";
        }
        finally
        {
            _busy = false;
            BusyText.IsVisible = false;
            UpdateActions();
        }
    }
}
