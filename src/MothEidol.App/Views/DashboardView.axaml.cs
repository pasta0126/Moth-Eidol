using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avalonia.Threading;
using MothEidol.Core.Alerts;
using MothEidol.Core.Logs;
using MothEidol.Core.Projects;

namespace MothEidol.App.Views;

/// <summary>The home screen: one status tile per project and a tile to create another.</summary>
public partial class DashboardView : UserControl
{
    private const double DragStartDistance = 8;
    private static readonly DataFormat<string> TileFormat = DataFormat.CreateStringApplicationFormat("moth-eidol.project-tile");

    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(2) };
    private ProjectService? _projects;
    private IEventStore? _events;
    private Action<Project, bool>? _openProject;
    private Action? _newProject;
    private long? _draggingId;

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
        // New events change the tiles; rebuilding them mid-drag would drop the tile being dragged.
        _timer.Tick += (_, _) => { if (IsEffectivelyVisible && _draggingId is null) Refresh(); };
        _timer.Start();
        Refresh();
    }

    public void Refresh()
    {
        if (_projects is null || _events is null || _draggingId is not null) return;

        var tiles = _projects.Projects
            .Select(p => TileModel.For(
                p, _projects.StatusOf(p.Id), _events.Pending(p.Id),
                _events.ProjectSeries(p.Id, DateTimeOffset.UtcNow, 48)))
            .Append(TileModel.NewProject())
            .ToList();

        Tiles.ItemsSource = tiles;
        HintText.IsVisible = tiles.Count == 1;
    }

    private void OnTileAttached(object? sender, Avalonia.VisualTreeAttachmentEventArgs e)
    {
        if (sender is not Control tile) return;

        ClickGesture.Attach(tile, OnTileClicked);
        if (tile.DataContext is TileModel { IsProject: true }) AttachReorder(tile);
    }

    /// <summary>
    /// Lets a project tile be dragged onto another to take its place. The drag only starts once the pointer has
    /// moved past the threshold, so a plain click still opens the project.
    /// </summary>
    private void AttachReorder(Control tile)
    {
        Point? pressedAt = null;
        PointerPressedEventArgs? pressedEvent = null;
        DragDrop.SetAllowDrop(tile, true);

        tile.AddHandler(InputElement.PointerPressedEvent, (_, e) =>
        {
            var onButton = (e.Source as Visual)?.FindAncestorOfType<Button>(includeSelf: true) is not null;
            var left = !onButton && e.GetCurrentPoint(tile).Properties.IsLeftButtonPressed;
            pressedAt = left ? e.GetPosition(tile) : null;
            pressedEvent = left ? e : null;
        }, RoutingStrategies.Bubble, handledEventsToo: true);

        tile.AddHandler(InputElement.PointerReleasedEvent, (_, _) => pressedAt = null,
            RoutingStrategies.Bubble, handledEventsToo: true);

        tile.AddHandler(InputElement.PointerMovedEvent, async (_, e) =>
        {
            if (pressedAt is not { } start || pressedEvent is not { } trigger || _draggingId is not null || tile.DataContext is not TileModel model) return;
            var position = e.GetPosition(tile);
            if (Math.Abs(position.X - start.X) <= DragStartDistance && Math.Abs(position.Y - start.Y) <= DragStartDistance)
                return;

            pressedAt = null;
            _draggingId = model.ProjectId;
            tile.Opacity = 0.6;
            try
            {
                var transfer = new DataTransfer();
                transfer.Add(DataTransferItem.Create(TileFormat, model.ProjectId.ToString()));
                await DragDrop.DoDragDropAsync(trigger, transfer, DragDropEffects.Move);
            }
            finally
            {
                tile.Opacity = 1;
                _draggingId = null;
                Refresh();
            }
        }, RoutingStrategies.Bubble, handledEventsToo: true);

        tile.AddHandler(DragDrop.DragOverEvent, (_, e) =>
            e.DragEffects = _draggingId is not null ? DragDropEffects.Move : DragDropEffects.None);

        tile.AddHandler(DragDrop.DropEvent, (_, e) =>
        {
            if (_draggingId is not { } moved || _projects is null || tile.DataContext is not TileModel target) return;
            e.Handled = true;
            if (moved == target.ProjectId) return;

            var ids = _projects.Projects.Select(p => p.Id).ToList();
            ids.Remove(moved);
            var index = ids.IndexOf(target.ProjectId);
            // Moving forward lands after the target (it takes its place), moving back lands before it.
            var wasBefore = _projects.Projects.ToList().FindIndex(p => p.Id == moved) <
                            _projects.Projects.ToList().FindIndex(p => p.Id == target.ProjectId);
            ids.Insert(wasBefore ? index + 1 : index, moved);
            _projects.Reorder(ids);
        });
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
