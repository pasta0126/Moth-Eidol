using Avalonia.Controls;
using Avalonia.Threading;
using MothEidol.Core.Logs;
using MothEidol.Core.Projects;
using MothEidol.Core.Settings;

namespace MothEidol.App.Views;

/// <summary>The screen of one project, with its live tail and its settings.</summary>
public partial class ProjectView : UserControl
{
    private ProjectService? _projects;
    private long _projectId;
    private string _sourceKey = "";

    public ProjectView()
    {
        InitializeComponent();
    }

    public void Initialize(
        ProjectService projects, TailingService tailing, ProjectTails tails, AppSettings settings, Action back)
    {
        _projects = projects;
        BackButton.Click += (_, _) => back();
        Tail.Initialize(tails, settings);
        SettingsSection.Initialize(projects, tailing, back);

        projects.Changed += (_, _) => Dispatcher.UIThread.Post(() => Refresh());
        tailing.StatusChanged += (_, _) => Dispatcher.UIThread.Post(() => Refresh());
    }

    /// <summary>Opens a project, on its settings when it was just created and on its tail otherwise.</summary>
    public void Open(Project project, bool settingsFirst)
    {
        _projectId = project.Id;
        SettingsSection.Bind(project);
        Sections.SelectedIndex = settingsFirst ? 1 : 0;
        Refresh(force: true);
    }

    private void Refresh(bool force = false)
    {
        if (_projects?.Get(_projectId) is not { } project) return;

        TitleText.Text = project.Name;
        var status = _projects.StatusOf(project.Id);
        var tile = TileModel.For(project, status, _projects.SourcesOf(project.Id).Count);
        StatusText.Text = status.Health == ProjectHealth.Degraded ? $"{tile.StatusText} · {tile.Detail}" : tile.StatusText;
        StatusDot.Fill = tile.StatusBrush;

        // Rebinding rebuilds the tail, so only do it when the project or its sources actually changed.
        var sources = _projects.SourcesOf(project.Id);
        var key = string.Join('|', sources.Select(s => $"{s.Id}:{s.DisplayName}"));
        if (force || key != _sourceKey)
        {
            _sourceKey = key;
            Tail.Bind(project.Id, sources);
        }
    }
}
