using Avalonia.Controls;
using Avalonia.Input.Platform;
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
    private readonly IUrlOpener _urlOpener = new SystemUrlOpener();

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
        BuildSound(settings);
        BuildTheme(settings);
        BuildAbout();

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

    /// <summary>The theme applies as soon as it is chosen and is remembered for the next start.</summary>
    private void BuildTheme(AppSettings settings)
    {
        ThemeCombo.SelectedIndex = settings.Theme == AppTheme.Vesper ? 1 : 0;
        ThemeCombo.SelectionChanged += (_, _) =>
        {
            var theme = ThemeCombo.SelectedIndex == 1 ? AppTheme.Vesper : AppTheme.Moth;
            if (theme == settings.Theme) return;
            settings.Theme = theme;
            App.ApplyTheme(theme);
        };
    }

    /// <summary>One switch per channel, built from the registered ones so new channels appear without changes here.</summary>
    private void BuildChannels(AppSettings settings, NotificationDispatcher dispatcher)
    {
        var problems = new Dictionary<string, SelectableTextBlock>();

        // The sound channel is not a switch of its own: the "Alert sound" setting below drives it.
        foreach (var notifier in dispatcher.Notifiers.Where(n => n.Id != ChannelIds.Sound))
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

    /// <summary>"Alert sound": with sound or silent, applied from the next alert without a restart.</summary>
    private void BuildSound(AppSettings settings)
    {
        var defaultOn = NotificationDefaults.SoundEnabledByDefault(OperatingSystem.IsWindows());
        SoundCombo.SelectedIndex = settings.IsAlertSoundEnabled(defaultOn) ? 0 : 1;
        SoundCombo.SelectionChanged += (_, _) => settings.SetAlertSoundEnabled(SoundCombo.SelectedIndex == 0);
    }

    /// <summary>The discreet About block: plain text links that only open the default browser.</summary>
    private void BuildAbout()
    {
        ToolTip.SetTip(CoffeeLink, SupportLinks.BuyMeACoffee);
        CoffeeLink.Click += async (_, _) => await OpenAsync(SupportLinks.BuyMeACoffee);
        AddLink("Suggestions & bugs", SupportLinks.SuggestionsAndBugs);
        AddLink("GitHub", SupportLinks.GitHub);
    }

    private void AddLink(string text, string url)
    {
        var link = new Button
        {
            Content = text,
            Padding = new Avalonia.Thickness(0),
            FontSize = 12,
            Background = Avalonia.Media.Brushes.Transparent,
            BorderThickness = new Avalonia.Thickness(0),
            Foreground = ThemeBrushes.Get("MothAccentBrush"),
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
        };
        ToolTip.SetTip(link, url);
        link.Click += async (_, _) => await OpenAsync(url);
        AboutLinks.Children.Add(link);
    }

    /// <summary>Opens the address; when the browser cannot be started it says so and copies the address instead of failing.</summary>
    private async Task OpenAsync(string url)
    {
        if (_urlOpener.TryOpen(url, out var error))
        {
            AboutNote.IsVisible = false;
            return;
        }

        var copied = false;
        try
        {
            if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
            {
                await clipboard.SetTextAsync(url);
                copied = true;
            }
        }
        catch (Exception)
        {
            // Copying is a courtesy; the address is shown below anyway.
        }

        AboutNote.Text = copied
            ? $"{error} The address was copied: {url}"
            : $"{error} Address: {url}";
        AboutNote.IsVisible = true;
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
