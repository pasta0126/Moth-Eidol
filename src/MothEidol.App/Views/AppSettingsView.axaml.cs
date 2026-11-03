using Avalonia.Controls;
using Avalonia.Threading;
using MothEidol.Core.Notifications;
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

    public void Initialize(IAutostart autostart, AppSettings settings, NotificationDispatcher dispatcher, Action back)
    {
        _autostart = autostart;
        _settings = settings;
        BackButton.Click += (_, _) => back();
        BuildChannels(settings, dispatcher);

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

    /// <summary>One switch per channel, built from the registered ones so new channels appear without changes here.</summary>
    private void BuildChannels(AppSettings settings, NotificationDispatcher dispatcher)
    {
        var problems = new Dictionary<string, SelectableTextBlock>();

        foreach (var notifier in dispatcher.Notifiers)
        {
            var check = new CheckBox
            {
                Content = notifier.DisplayName,
                IsChecked = settings.IsChannelEnabled(notifier.Id, notifier.EnabledByDefault),
                Foreground = ThemeBrushes.Get("MothTextBrush"),
            };
            check.Click += (_, _) => settings.SetChannelEnabled(notifier.Id, check.IsChecked == true);

            var problem = new SelectableTextBlock
            {
                IsVisible = false,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                Margin = new Avalonia.Thickness(28, 0, 0, 0),
                Foreground = ThemeBrushes.Get("MothErrorBrush"),
            };
            problems[notifier.Id] = problem;

            ChannelsPanel.Children.Add(check);
            ChannelsPanel.Children.Add(problem);
        }

        // A channel that fails is reported here instead of failing silently.
        dispatcher.ChannelFailed += (_, e) => Dispatcher.UIThread.Post(() =>
        {
            if (!problems.TryGetValue(e.ChannelId, out var text)) return;
            text.Text = $"Last problem: {e.Error.Message}";
            text.IsVisible = true;
        });
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
