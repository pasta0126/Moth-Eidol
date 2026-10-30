using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using MothEidol.Core.Logs;
using MothEidol.Core.Projects;

namespace MothEidol.App.Views;

/// <summary>
/// Guides the creation of a project: name, log location (checked against the real files), alerts and a
/// review. Nothing is saved until the last step; cancelling leaves no trace.
/// </summary>
public partial class ProjectWizardView : UserControl
{
    private static readonly string[] StepTitles = ["Name", "Log location", "Alerts", "Review"];
    private static readonly TimeSpan CheckDelay = TimeSpan.FromMilliseconds(400);
    private static readonly TimeSpan CheckTimeout = TimeSpan.FromSeconds(10);

    private readonly ObservableCollection<WizardSourceRow> _sources = [];
    private readonly ObservableCollection<SampleRow> _sample = [];
    private readonly ObservableCollection<string> _files = [];
    private ProjectDraft _draft = new();
    private ProjectService? _projects;
    private Action<Project>? _created;
    private Action? _cancelled;
    private CancellationTokenSource? _check;
    private LocationInspection? _inspection;
    private int _step;
    private bool _creating;

    public ProjectWizardView()
    {
        InitializeComponent();
        SourceList.ItemsSource = _sources;
        SampleList.ItemsSource = _sample;
        FileList.ItemsSource = _files;

        CancelButton.Click += (_, _) => _cancelled?.Invoke();
        BackButton.Click += (_, _) => GoTo(_step - 1);
        NextButton.Click += OnNextClicked;
        NameBox.KeyDown += (_, e) => { if (e.Key == Key.Enter) OnNextClicked(null, e); };

        FolderRadio.IsCheckedChanged += (_, _) => { PatternBox.IsVisible = FolderRadio.IsChecked == true; ScheduleCheck(); };
        PathBox.TextChanged += (_, _) => ScheduleCheck();
        PatternBox.TextChanged += (_, _) => ScheduleCheck();
        BrowseButton.Click += OnBrowseClicked;
        AddButton.Click += OnAddClicked;

        ModeCombo.SelectionChanged += (_, _) => UpdateModeHelp();
    }

    public void Initialize(ProjectService projects, Action<Project> created, Action cancelled)
    {
        _projects = projects;
        _created = created;
        _cancelled = cancelled;
    }

    /// <summary>Starts a fresh wizard with default values.</summary>
    public void Start()
    {
        _check?.Cancel();
        _draft = new ProjectDraft();
        _sources.Clear();
        _creating = false;
        NameBox.Text = "";
        PathBox.Text = "";
        PatternBox.Text = "*.log";
        FolderRadio.IsChecked = true;
        WarningsCheck.IsChecked = _draft.Alerts.WarningAlerts;
        ModeCombo.SelectedIndex = 0;
        MinutesBox.Value = _draft.Alerts.Minutes;
        UpdateModeHelp();
        ClearCheck();
        AddError.Text = "";
        GoTo(0);
        NameBox.Focus();
    }

    private string? ExistingNameError() =>
        _projects is null ? null : _draft.ValidateName(_projects.Projects.Select(p => p.Name));

    private void GoTo(int step)
    {
        _step = Math.Clamp(step, 0, StepTitles.Length - 1);
        NameStep.IsVisible = _step == 0;
        LocationStep.IsVisible = _step == 1;
        AlertsStep.IsVisible = _step == 2;
        ReviewStep.IsVisible = _step == 3;

        StepText.Text = $"Step {_step + 1} of {StepTitles.Length} · {StepTitles[_step]}";
        BackButton.IsVisible = _step > 0;
        NextButton.Content = _step == StepTitles.Length - 1 ? "Create project" : "Next";
        NextButton.IsEnabled = !_creating;
        ErrorText.Text = "";

        if (_step == 3) FillReview();
    }

    private async void OnNextClicked(object? sender, RoutedEventArgs e)
    {
        if (_creating) return;

        switch (_step)
        {
            case 0:
                _draft.Name = NameBox.Text ?? "";
                if (ExistingNameError() is { } nameError) { ErrorText.Text = nameError; return; }
                GoTo(1);
                break;

            case 1:
                if (_draft.ValidateSources() is { } sourcesError)
                {
                    ErrorText.Text = (PathBox.Text ?? "").Trim().Length > 0
                        ? "Press \"Add this source\" to include the location you entered."
                        : sourcesError;
                    return;
                }
                GoTo(2);
                break;

            case 2:
                ReadAlerts();
                if (_draft.ValidateAlerts() is { } alertsError) { ErrorText.Text = alertsError; return; }
                GoTo(3);
                break;

            default:
                await CreateAsync();
                break;
        }
    }

    private async Task CreateAsync()
    {
        if (_projects is null) return;

        _creating = true;
        NextButton.IsEnabled = false;
        ErrorText.Text = "";
        try
        {
            var project = await _projects.CreateWithSourcesAsync(_draft);
            _created?.Invoke(project);
        }
        catch (ArgumentException ex) when (ExistingNameError() is not null)
        {
            // The name was taken in the meantime: nothing was saved, so go back and let the user change it.
            _creating = false;
            GoTo(0);
            ErrorText.Text = ex.Message;
        }
        catch (Exception ex)
        {
            _creating = false;
            NextButton.IsEnabled = true;
            ErrorText.Text = $"Could not create the project: {ex.Message}";
        }
    }

    private void ReadAlerts()
    {
        var mode = ModeCombo.SelectedIndex == 1 ? FrequencyMode.Summary : FrequencyMode.Immediate;
        var minutes = (int)(MinutesBox.Value ?? AlertSettings.Default.Minutes);
        _draft.Alerts = new AlertSettings(WarningsCheck.IsChecked == true, mode, minutes);
    }

    private void UpdateModeHelp()
    {
        var summary = ModeCombo.SelectedIndex == 1;
        MinutesLabel.Text = summary ? "Interval (minutes)" : "Cooldown (minutes)";
        ModeHelp.Text = summary
            ? "You get one message per interval summarizing what happened, instead of one per error."
            : "The first error alerts you right away; further ones are grouped and reported once the cooldown ends.";
    }

    private void FillReview()
    {
        ReviewName.Text = _draft.Name.Trim();
        ReviewSources.Text = string.Join("\n", _draft.Sources.Select(s =>
            $"{(s.Kind == SourceKind.File ? "File" : "Folder")}: {s.DisplayName}"));
        var a = _draft.Alerts;
        ReviewAlerts.Text =
            $"Errors and above: always. Warnings: {(a.WarningAlerts ? "yes" : "no")}.\n" +
            (a.Mode == FrequencyMode.Summary
                ? $"One summary every {a.Minutes} min."
                : $"Immediate, with a {a.Minutes} min cooldown.");
    }

    // --- Location -------------------------------------------------------------------------------------------

    private SourceDraft CurrentInput() => new(
        FolderRadio.IsChecked == true ? SourceKind.Folder : SourceKind.File,
        PathBox.Text ?? "",
        PatternBox.Text);

    private void ClearCheck()
    {
        _inspection = null;
        AddButton.IsEnabled = false;
        CheckText.Text = "Enter a path to check it.";
        CheckText.Foreground = ThemeBrushes.Get("MothTextMutedBrush");
        WarningText.Text = "";
        FilesPanel.IsVisible = SamplePanel.IsVisible = false;
        _files.Clear();
        _sample.Clear();
    }

    /// <summary>Checks the location shortly after the user stops typing; a newer edit discards an older result.</summary>
    private async void ScheduleCheck()
    {
        _check?.Cancel();
        _inspection = null;
        AddButton.IsEnabled = false;
        AddError.Text = "";

        var input = CurrentInput().Normalized();
        if (input.Path.Length == 0)
        {
            ClearCheck();
            return;
        }

        var cancellation = _check = new CancellationTokenSource();
        var token = cancellation.Token;
        try
        {
            await Task.Delay(CheckDelay, token);
            CheckText.Text = "Checking...";
            CheckText.Foreground = ThemeBrushes.Get("MothTextMutedBrush");

            LocationInspection result;
            try
            {
                result = await Task.Run(() => LocationInspector.Inspect(input.Kind, input.Path, input.Pattern), token)
                    .WaitAsync(CheckTimeout, token);
            }
            catch (TimeoutException)
            {
                // A slow share is not proof that the location is wrong: let the user add it anyway.
                if (token.IsCancellationRequested) return;
                ShowTimeout();
                return;
            }

            if (token.IsCancellationRequested) return;
            ShowInspection(result);
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer edit.
        }
    }

    private void ShowTimeout()
    {
        _inspection = new LocationInspection(InspectionOutcome.Ok, null, [], 0, [], []);
        CheckText.Text = "The location did not answer within 10 seconds.";
        CheckText.Foreground = ThemeBrushes.Get("MothWarningBrush");
        WarningText.Text = "It may be a slow network share. You can add it anyway; the app keeps retrying until it can read it.";
        FilesPanel.IsVisible = SamplePanel.IsVisible = false;
        AddButton.IsEnabled = true;
    }

    private void ShowInspection(LocationInspection result)
    {
        _inspection = result;
        _files.Clear();
        _sample.Clear();

        if (!result.CanAdd)
        {
            CheckText.Text = result.Error;
            CheckText.Foreground = ThemeBrushes.Get("MothErrorBrush");
            WarningText.Text = "";
            FilesPanel.IsVisible = SamplePanel.IsVisible = false;
            AddButton.IsEnabled = false;
            return;
        }

        CheckText.Text = result.TotalFiles switch
        {
            0 => "The location can be read, but nothing matches yet.",
            1 => "The location can be read: 1 file matches.",
            var n => $"The location can be read: {n} files match; the newest one is followed.",
        };
        CheckText.Foreground = ThemeBrushes.Get(result.Warnings.Count > 0 ? "MothWarningBrush" : "MothInfoBrush");
        WarningText.Text = string.Join("\n", result.Warnings);

        foreach (var file in result.Files)
        {
            _files.Add($"{(file == result.Newest ? "▶ " : "   ")}{file.Name} · {FormatSize(file.Length)} · {file.Modified.ToLocalTime():yyyy-MM-dd HH:mm}");
        }
        FilesTitle.Text = result.TotalFiles > result.Files.Count
            ? $"Matching files (newest {result.Files.Count} of {result.TotalFiles})"
            : "Matching files";
        FilesPanel.IsVisible = result.Files.Count > 0;

        foreach (var entry in result.Sample) _sample.Add(new SampleRow(entry));
        SamplePanel.IsVisible = result.Sample.Count > 0;

        AddButton.IsEnabled = true;
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:0.#} KB",
        _ => $"{bytes / 1024.0 / 1024.0:0.#} MB",
    };

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
        if (_inspection is not { CanAdd: true }) return;

        var error = _draft.AddSource(CurrentInput());
        if (error is not null)
        {
            AddError.Text = error;
            return;
        }

        _sources.Add(new WizardSourceRow(_draft.Sources[^1]));
        NoSourcesText.IsVisible = false;
        PathBox.Text = "";
        AddError.Text = "";
        ErrorText.Text = "";
        ClearCheck();
    }

    private void OnSourceRemoveClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: WizardSourceRow row }) return;

        _draft.Sources.Remove(row.Source);
        _sources.Remove(row);
        NoSourcesText.IsVisible = _sources.Count == 0;
    }
}
