using Avalonia.Controls;
using MothEidol.Core.Platform;
using MothEidol.Core.Settings;

namespace MothEidol.App;

public partial class MainWindow : Window
{
    private readonly IAutostart? _autostart;
    private readonly AppSettings? _settings;
    private bool _allowClose;

    // Required by the Avalonia XAML loader / designer.
    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(string version, IAutostart autostart, AppSettings settings, string? notice) : this()
    {
        _autostart = autostart;
        _settings = settings;

        if (!string.IsNullOrEmpty(notice))
        {
            NoticeText.Text = notice;
            NoticeBanner.IsVisible = true;
        }

        Title = $"Moth-Eidol v{version}";
        VersionText.Text = $"Moth-Eidol v{version}";

        if (autostart.IsSupported)
        {
            AutostartCheck.IsChecked = settings.StartWithSession;
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
            var enabled = AutostartCheck.IsChecked == true;
            _autostart.SetEnabled(enabled);
            _settings!.StartWithSession = enabled;
        }
        catch (Exception ex)
        {
            AutostartCheck.IsChecked = _autostart.IsEnabled;
            AutostartNote.Text = $"Could not change the setting: {ex.Message}";
            AutostartNote.IsVisible = true;
        }
    }
}
