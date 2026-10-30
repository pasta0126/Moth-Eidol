using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using MothEidol.Core.Logs;
using MothEidol.Core.Projects;

namespace MothEidol.App.Views;

/// <summary>The home screen: one status tile per project and a tile to create another.</summary>
public partial class DashboardView : UserControl
{
    private ProjectService? _projects;
    private Action<Project, bool>? _openProject;

    public DashboardView()
    {
        InitializeComponent();
    }

    public void Initialize(
        ProjectService projects, TailingService tailing, Action<Project, bool> openProject, Action openSettings)
    {
        _projects = projects;
        _openProject = openProject;

        SettingsButton.Click += (_, _) => openSettings();
        CreateButton.Click += (_, _) => CreateProject();
        CancelButton.Click += (_, _) => HideCreate();
        NameBox.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter) CreateProject();
            else if (e.Key == Key.Escape) HideCreate();
        };

        projects.Changed += (_, _) => Dispatcher.UIThread.Post(Refresh);
        tailing.StatusChanged += (_, _) => Dispatcher.UIThread.Post(Refresh);
        Refresh();
    }

    public void Refresh()
    {
        if (_projects is null) return;

        var tiles = _projects.Projects
            .Select(p => TileModel.For(p, _projects.StatusOf(p.Id), _projects.SourcesOf(p.Id).Count))
            .Append(TileModel.NewProject())
            .ToList();

        Tiles.ItemsSource = tiles;
        HintText.IsVisible = tiles.Count == 1;
    }

    private void OnTileClicked(object? sender, RoutedEventArgs e)
    {
        if (_projects is null || sender is not Button { DataContext: TileModel tile }) return;

        if (tile.IsNew)
        {
            CreatePanel.IsVisible = true;
            NameBox.Focus();
        }
        else if (_projects.Get(tile.ProjectId) is { } project)
        {
            _openProject?.Invoke(project, false);
        }
    }

    private void CreateProject()
    {
        if (_projects is null) return;

        try
        {
            var project = _projects.Create(NameBox.Text ?? "");
            HideCreate();
            _openProject?.Invoke(project, true); // Its parameters are set on its own screen.
        }
        catch (ArgumentException ex)
        {
            ErrorText.Text = ex.Message;
        }
    }

    private void HideCreate()
    {
        CreatePanel.IsVisible = false;
        NameBox.Text = "";
        ErrorText.Text = "";
    }
}
