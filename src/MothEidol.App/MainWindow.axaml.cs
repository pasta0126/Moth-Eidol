using Avalonia.Controls;
using MothEidol.Core.Platform;

namespace MothEidol.App;

public partial class MainWindow : Window
{
    private readonly IAutostart? _autostart;
    private bool _allowClose;

    // Required by the Avalonia XAML loader / designer.
    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(string version, IAutostart autostart) : this()
    {
        _autostart = autostart;
        Title = $"Moth-Eidol v{version}";
        VersionText.Text = $"Moth-Eidol v{version}";

        if (autostart.IsSupported)
        {
            AutostartCheck.IsChecked = autostart.IsEnabled;
            AutostartCheck.IsCheckedChanged += OnAutostartChanged;
        }
        else
        {
            AutostartCheck.IsEnabled = false;
            AutostartNote.Text = autostart.UnsupportedReason;
            AutostartNote.IsVisible = true;
        }
    }

    /// <summary>Lets the next close actually close the window (used when quitting).</summary>
    public void AllowClose() => _allowClose = true;

    public void ShowFromTray()
    {
        if (!IsVisible)
        {
            Show();
        }

        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Activate();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        var shuttingDown = e.CloseReason is WindowCloseReason.ApplicationShutdown
            or WindowCloseReason.OSShutdown;

        if (!_allowClose && !shuttingDown)
        {
            e.Cancel = true;
            Hide();
        }

        base.OnClosing(e);
    }

    private void OnAutostartChanged(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_autostart is null)
        {
            return;
        }

        try
        {
            _autostart.SetEnabled(AutostartCheck.IsChecked == true);
        }
        catch (Exception ex)
        {
            AutostartCheck.IsChecked = _autostart.IsEnabled;
            AutostartNote.Text = $"Could not change the setting: {ex.Message}";
            AutostartNote.IsVisible = true;
        }
    }
}
