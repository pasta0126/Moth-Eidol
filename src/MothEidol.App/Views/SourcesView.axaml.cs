using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MothEidol.Core.Logs;

namespace MothEidol.App.Views;

public partial class SourcesView : UserControl
{
    private TailingService? _service;
    private SourceListModel? _model;

    public SourcesView()
    {
        InitializeComponent();
        FileRadio.IsCheckedChanged += (_, _) => PatternBox.IsVisible = FolderRadio.IsChecked == true;
        FolderRadio.IsCheckedChanged += (_, _) => PatternBox.IsVisible = FolderRadio.IsChecked == true;
        BrowseButton.Click += OnBrowseClicked;
        AddButton.Click += OnAddClicked;
    }

    public void Initialize(TailingService service, SourceListModel model)
    {
        _service = service;
        _model = model;
        SourceList.ItemsSource = model.Rows;
        model.Rows.CollectionChanged += (_, _) => UpdateEmptyState();
        UpdateEmptyState();
    }

    private void UpdateEmptyState() => EmptyText.IsVisible = _model?.Rows.Count == 0;

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
        if (_service is null || _model is null) return;

        try
        {
            var kind = FolderRadio.IsChecked == true ? SourceKind.Folder : SourceKind.File;
            _service.Add(kind, PathBox.Text ?? "", PatternBox.Text);
            _model.Refresh();
            PathBox.Text = "";
            PatternBox.Text = "";
            ErrorText.Text = "";
        }
        catch (ArgumentException ex)
        {
            ErrorText.Text = ex.Message;
        }
        catch (Exception ex)
        {
            ErrorText.Text = $"Could not add the source: {ex.Message}";
        }
    }

    private async void OnEnabledClicked(object? sender, RoutedEventArgs e)
    {
        if (_service is null || _model is null || sender is not CheckBox { DataContext: SourceRow row } box) return;

        try
        {
            await _service.SetEnabledAsync(row.Id, box.IsChecked == true);
        }
        catch (Exception ex)
        {
            ErrorText.Text = $"Could not change the source: {ex.Message}";
        }
        _model.Refresh();
    }

    private async void OnRemoveClicked(object? sender, RoutedEventArgs e)
    {
        if (_service is null || _model is null || sender is not Button { DataContext: SourceRow row }) return;

        try
        {
            await _service.RemoveAsync(row.Id);
        }
        catch (Exception ex)
        {
            ErrorText.Text = $"Could not remove the source: {ex.Message}";
        }
        _model.Refresh();
    }
}
