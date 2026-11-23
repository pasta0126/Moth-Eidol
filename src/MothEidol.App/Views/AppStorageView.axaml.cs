using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using MothEidol.Core.Alerts;
using MothEidol.Core.Projects;

namespace MothEidol.App.Views;

/// <summary>
/// The application-wide storage section: the whole database, every stored project with its load and the actions
/// to clear or remove it, the leftovers of removed projects and on-demand compaction.
/// </summary>
public partial class AppStorageView : UserControl
{
    private AppStorageService? _storage;
    private Action<Project>? _openProject;
    private AppStorageOverview? _overview;
    private bool _busy;
    private long? _confirming; // The project whose row shows a confirmation.
    private bool _confirmingRemove; // Remove the project (true) or clear its events (false).

    public AppStorageView()
    {
        InitializeComponent();
        RefreshButton.Click += async (_, _) =>
        {
            ResultText.Text = ErrorText.Text = "";
            await MeasureAsync();
        };
        CompactButton.Click += async (_, _) => await RunAsync(s => s.CompactAsync());
        LeftoversButton.Click += (_, _) => AskLeftovers();
        LeftoversCancel.Click += (_, _) => LeftoversConfirm.IsVisible = false;
        LeftoversYes.Click += async (_, _) =>
        {
            LeftoversConfirm.IsVisible = false;
            await RunAsync(s => s.RemoveLeftoversAsync());
        };
        // The rows take their brushes from code, so they are rebuilt with the new theme's.
        ThemeBrushes.Changed += () => Dispatcher.UIThread.Post(BuildProjects);
        ShowOverview();
    }

    public void Initialize(AppStorageService storage, Action<Project> openProject)
    {
        _storage = storage;
        _openProject = openProject;
    }

    /// <summary>Measures everything again (when the section is shown or the user refreshes it).</summary>
    public async Task MeasureAsync()
    {
        if (_storage is null) return;
        RefreshButton.IsEnabled = false;
        try
        {
            _overview = await _storage.OverviewAsync();
        }
        catch (Exception ex)
        {
            ErrorText.Text = $"Could not measure the storage: {ex.Message}";
        }
        finally
        {
            RefreshButton.IsEnabled = true;
        }
        ShowOverview();
    }

    private void ShowOverview()
    {
        var overview = _overview;
        FolderText.Text = overview is null ? "…" : overview.DataFolder ?? "Nothing is stored on disk in this session.";
        FileText.Text = overview is null
            ? "…"
            : overview.File.WalBytes > 0
                ? $"{ByteSize.Format(overview.File.TotalBytes)} (including {ByteSize.Format(overview.File.WalBytes)} of write-ahead log)"
                : ByteSize.Format(overview.File.TotalBytes);
        ReclaimableText.Text = overview is null ? "…" : ByteSize.Format(overview.ReclaimableBytes);
        TotalText.Text = overview is null
            ? "…"
            : $"{ByteSize.Format(overview.TotalApproxBytes)} (approx.) in {overview.Projects.Count:N0} project{(overview.Projects.Count == 1 ? "" : "s")}";
        LeftoversText.Text = overview is null ? "…" : LeftoversDescription(overview);
        if (overview is null || !overview.HasLeftovers) LeftoversConfirm.IsVisible = false;
        BuildProjects();
        UpdateActions();
    }

    private static string LeftoversDescription(AppStorageOverview overview)
    {
        if (!overview.HasLeftovers) return "Nothing is left over.";
        var lines = new List<string>();
        var rows = overview.Leftovers;
        if (rows.Total > 0)
        {
            var parts = new List<string>();
            if (rows.Sources > 0) parts.Add($"{rows.Sources:N0} sources");
            if (rows.ReadPositions > 0) parts.Add($"{rows.ReadPositions:N0} read positions");
            if (rows.Groups > 0) parts.Add($"{rows.Groups:N0} event groups");
            if (rows.Occurrences > 0) parts.Add($"{rows.Occurrences:N0} occurrences");
            if (rows.HourlyCounts > 0) parts.Add($"{rows.HourlyCounts:N0} hourly counts");
            lines.Add($"Rows of removed projects: {string.Join(", ", parts)}.");
        }
        foreach (var backup in overview.CorruptBackups)
            lines.Add($"Corrupt database copy: {backup.Name} ({ByteSize.Format(backup.Bytes)}).");
        return string.Join(Environment.NewLine, lines);
    }

    private void UpdateActions()
    {
        var ready = _overview is not null && !_busy;
        CompactButton.IsEnabled = ready && _overview!.ReclaimableBytes > 0;
        LeftoversButton.IsEnabled = ready && _overview!.HasLeftovers;
        LeftoversYes.IsEnabled = !_busy;
        RefreshButton.IsEnabled = !_busy;
        BusyText.IsVisible = _busy;
    }

    private void BuildProjects()
    {
        ProjectsPanel.Children.Clear();
        if (_overview is null) return;
        if (_overview.Projects.Count == 0)
        {
            ProjectsPanel.Children.Add(Text("There are no projects.", "MothTextMutedBrush"));
            return;
        }
        foreach (var row in _overview.Projects) ProjectsPanel.Children.Add(ProjectRow(row));
    }

    private Control ProjectRow(StoredProject row)
    {
        var usage = row.Usage;
        var now = DateTimeOffset.Now;

        var title = new WrapPanel { Orientation = Orientation.Horizontal, ItemSpacing = 8 };
        title.Children.Add(new SelectableTextBlock
        {
            Text = row.Project.Name, FontWeight = FontWeight.SemiBold, Foreground = ThemeBrushes.Get("MothTextBrush"),
        });
        title.Children.Add(Text(row.StateText, "MothTextMutedBrush"));
        if (row.IsCleanupCandidate)
            title.Children.Add(Text("Not monitored, still holds events", "MothWarningBrush", FontWeight.SemiBold));

        var pending = usage.PendingGroups > 0 ? $" ({usage.PendingGroups:N0} pending)" : "";
        var newest = usage.Newest is { } at ? $"last event {RelativeTime.Format(at, now)}" : "no events";
        var figures = Text(
            $"{usage.Groups:N0} event groups{pending} · {usage.Occurrences:N0} occurrences · " +
            $"{ByteSize.Format(usage.ApproxBytes)} (approx.) · keeps {row.Project.RetentionDays} days · {newest}",
            "MothTextMutedBrush");
        figures.TextWrapping = TextWrapping.Wrap;

        var open = new Button { Content = "Open" };
        open.Click += (_, _) => _openProject?.Invoke(row.Project);
        var clear = new Button { Content = "Clear events", IsEnabled = !usage.IsEmpty && !_busy };
        clear.Click += (_, _) => Ask(row.Project.Id, remove: false);
        var remove = new Button { Content = "Remove project", IsEnabled = !_busy };
        remove.Click += (_, _) => Ask(row.Project.Id, remove: true);
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        actions.Children.Add(open);
        actions.Children.Add(clear);
        actions.Children.Add(remove);

        var content = new StackPanel { Spacing = 6 };
        content.Children.Add(title);
        content.Children.Add(figures);
        content.Children.Add(actions);
        if (_confirming == row.Project.Id) content.Children.Add(Confirmation(row));

        return new Border
        {
            Padding = new Thickness(12),
            CornerRadius = new CornerRadius(4),
            BorderThickness = new Thickness(1),
            Background = ThemeBrushes.Get("MothSurfaceBrush"),
            BorderBrush = ThemeBrushes.Get(row.IsCleanupCandidate ? "MothWarningBrush" : "MothBorderBrush"),
            Child = content,
        };
    }

    private Control Confirmation(StoredProject row)
    {
        var usage = row.Usage;
        var pending = usage.PendingGroups > 0 ? $", {usage.PendingGroups:N0} of them pending," : "";
        var message = _confirmingRemove
            ? $"This removes the project \"{row.Project.Name}\" with its {row.Sources:N0} source{(row.Sources == 1 ? "" : "s")} " +
              $"and its {usage.Groups:N0} event groups{pending} and stops monitoring it. It cannot be undone."
            : $"This removes all {usage.Groups:N0} event groups of \"{row.Project.Name}\"{pending} with their " +
              $"{usage.Occurrences:N0} occurrences. The project, its sources and its settings are kept. It cannot be undone.";

        var yes = new Button { Content = _confirmingRemove ? "Yes, remove it" : "Yes, remove them", IsEnabled = !_busy };
        yes.Click += async (_, _) =>
        {
            var id = row.Project.Id;
            var removeProject = _confirmingRemove;
            _confirming = null;
            await RunAsync(s => removeProject ? s.RemoveProjectAsync(id) : s.ClearProjectAsync(id));
        };
        var cancel = new Button { Content = "Cancel" };
        cancel.Click += (_, _) =>
        {
            _confirming = null;
            BuildProjects();
        };

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        buttons.Children.Add(yes);
        buttons.Children.Add(cancel);
        var panel = new StackPanel { Spacing = 8, Margin = new Thickness(0, 4, 0, 0) };
        var text = Text(message, "MothTextBrush");
        text.TextWrapping = TextWrapping.Wrap;
        panel.Children.Add(text);
        panel.Children.Add(buttons);
        return panel;
    }

    private void Ask(long projectId, bool remove)
    {
        _confirming = projectId;
        _confirmingRemove = remove;
        ResultText.Text = ErrorText.Text = "";
        BuildProjects();
    }

    private void AskLeftovers()
    {
        if (_overview is null) return;
        var rows = _overview.Leftovers.Total;
        var files = _overview.CorruptBackups.Count;
        var parts = new List<string>();
        if (rows > 0) parts.Add($"{rows:N0} rows of removed projects");
        if (files > 0) parts.Add($"{files:N0} copies of a corrupt database");
        LeftoversConfirmText.Text = $"This removes {string.Join(" and ", parts)}. It cannot be undone.";
        ResultText.Text = ErrorText.Text = "";
        LeftoversConfirm.IsVisible = true;
    }

    private async Task RunAsync(Func<AppStorageService, Task<AppStorageResult>> action)
    {
        if (_storage is null || _busy) return;
        _busy = true;
        ResultText.Text = ErrorText.Text = "";
        BuildProjects();
        UpdateActions();
        try
        {
            var result = await action(_storage);
            ResultText.Text = result.Summary;
            if (result.Failures.Count > 0)
                ErrorText.Text = "Could not delete: " + string.Join("; ", result.Failures);
        }
        catch (Exception ex)
        {
            ErrorText.Text = $"Could not complete the action: {ex.Message}";
        }
        finally
        {
            _busy = false;
        }
        await MeasureAsync();
    }

    private static SelectableTextBlock Text(string text, string brush, FontWeight? weight = null) => new()
    {
        Text = text,
        Foreground = ThemeBrushes.Get(brush),
        FontWeight = weight ?? FontWeight.Normal,
        VerticalAlignment = VerticalAlignment.Center,
    };
}
