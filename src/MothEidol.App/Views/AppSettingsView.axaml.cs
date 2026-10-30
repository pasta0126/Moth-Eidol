using Avalonia.Controls;
using MothEidol.Core.Platform;
using MothEidol.Core.Settings;

namespace MothEidol.App.Views;

/// <summary>Application-wide settings (as opposed to the settings of one project).</summary>
public partial class AppSettingsView : UserControl
{
    private IAutostart? _autostart;
    private AppSettings? _settings;

    public AppSettingsView()
    {
        InitializeComponent();
    }

    public void Initialize(IAutostart autostart, AppSettings settings, Action back)
    {
        _autostart = autostart;
        _settings = settings;
        BackButton.Click += (_, _) => back();

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

    private void OnAutostartChanged(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_autostart is null) return;

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
