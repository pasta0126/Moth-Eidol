using Avalonia.Controls;
using MothEidol.App.Views;
using MothEidol.Core.Alerts;
using MothEidol.Core.Logs;
using MothEidol.Core.Notifications;
using MothEidol.Core.Platform;
using MothEidol.Core.Projects;
using MothEidol.Core.Settings;
using MothEidol.Core.Updates;

namespace MothEidol.App;

public partial class MainWindow : Window
{
    private bool _allowClose;

    // Required by the Avalonia XAML loader / designer.
    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(
        string version,
        IAutostart autostart,
        AppSettings settings,
        string? notice,
        TailingService tailing,
        ProjectService projects,
        ProjectTails tails,
        IEventStore events,
        NotificationDispatcher notifications,
        UnreadCounter unread,
        AppStorageService appStorage,
        UpdateCheckService? updates = null,
        Func<Task<bool>>? openStore = null) : this()
    {
        if (!string.IsNullOrEmpty(notice))
        {
            NoticeText.Text = notice;
            NoticeBanner.IsVisible = true;
        }

        Dashboard.Initialize(projects, tailing, events, OpenProject, ShowSettings, StartWizard);
        WizardScreen.Initialize(projects, project => OpenProject(project, false), ShowDashboard);
        ProjectScreen.Initialize(projects, tailing, tails, events, settings, ShowDashboard);
        SettingsScreen.Initialize(autostart, settings, notifications, appStorage, project => OpenProject(project, false), ShowDashboard);
        if (updates is not null && openStore is not null)
        {
            Dashboard.InitializeUpdates(updates, openStore);
            SettingsScreen.InitializeUpdates(updates, settings);
        }

        // Opening the application is how alerts get acknowledged: the tray counter returns to normal.
        Activated += (_, _) => unread.Acknowledge();

        // The version is shown only in the footer, which lives outside the screens; the title carries the name alone.
        Title = "Moth-Eidol";
        VersionText.Text = $"Moth-Eidol v{version}";
        ShowDashboard();
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

    /// <summary>Brings the window up on the dashboard, where the update banner is (click on the update notification).</summary>
    public void ShowUpdateNotice()
    {
        ShowFromTray();
        ShowDashboard();
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

    private void ShowDashboard()
    {
        Dashboard.Refresh();
        Show(Dashboard);
    }

    private void ShowSettings()
    {
        Show(SettingsScreen);
        _ = SettingsScreen.OnShownAsync();
    }

    private void StartWizard()
    {
        WizardScreen.Start();
        Show(WizardScreen);
    }

    private void OpenProject(Project project, bool settingsFirst)
    {
        ProjectScreen.Open(project, settingsFirst);
        Show(ProjectScreen);
    }

    private void Show(Control screen)
    {
        Dashboard.IsVisible = screen == Dashboard;
        ProjectScreen.IsVisible = screen == ProjectScreen;
        WizardScreen.IsVisible = screen == WizardScreen;
        SettingsScreen.IsVisible = screen == SettingsScreen;
    }
}
