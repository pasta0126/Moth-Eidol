using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using MothEidol.Core.Alerts;
using MothEidol.Core.Logs;
using MothEidol.Core.Projects;

namespace MothEidol.App.Views;

/// <summary>The home screen: one status tile per project and a tile to create another.</summary>
public partial class DashboardView : UserControl
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(2) };
    private ProjectService? _projects;
    private IEventStore? _events;
    private Action<Project, bool>? _openProject;
    private Action? _newProject;

    public DashboardView()
    {
        InitializeComponent();
    }

    public void Initialize(
        ProjectService projects, TailingService tailing, IEventStore events,
        Action<Project, bool> openProject, Action openSettings, Action newProject)
    {
        _projects = projects;
        _events = events;
        _openProject = openProject;
        _newProject = newProject;

        SettingsButton.Click += (_, _) => openSettings();

        projects.Changed += (_, _) => Dispatcher.UIThread.Post(Refresh);
        tailing.StatusChanged += (_, _) => Dispatcher.UIThread.Post(Refresh);
        _timer.Tick += (_, _) => { if (IsEffectivelyVisible) Refresh(); }; // New events change the tiles.
        _timer.Start();
        Refresh();
    }

    public void Refresh()
    {
        if (_projects is null || _events is null) return;

        var tiles = _projects.Projects
            .Select(p => TileModel.For(
                p, _projects.StatusOf(p.Id), _projects.SourcesOf(p.Id).Count, _events.Pending(p.Id),
                _events.ProjectSeries(p.Id, DateTimeOffset.UtcNow, 48)))
            .Append(TileModel.NewProject())
            .ToList();

        Tiles.ItemsSource = tiles;
        HintText.IsVisible = tiles.Count == 1;
    }

    private void OnTileAttached(object? sender, Avalonia.VisualTreeAttachmentEventArgs e)
    {
        if (sender is Control tile) ClickGesture.Attach(tile, OnTileClicked);
    }

    private void OnTileClicked(Control sender)
    {
        if (_projects is null || sender is not { DataContext: TileModel tile }) return;

        if (tile.IsNew)
        {
            _newProject?.Invoke();
        }
        else if (_projects.Get(tile.ProjectId) is { } project)
        {
            _openProject?.Invoke(project, false);
        }
    }
}
