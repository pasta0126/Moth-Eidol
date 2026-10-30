using Avalonia.Controls;
using MothEidol.App.Views;
using MothEidol.Core.Alerts;
using MothEidol.Core.Logs;
using MothEidol.Core.Notifications;
using MothEidol.Core.Platform;
using MothEidol.Core.Projects;
using MothEidol.Core.Settings;

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
        UnreadCounter unread) : this()
    {
        if (!string.IsNullOrEmpty(notice))
        {
            NoticeText.Text = notice;
            NoticeBanner.IsVisible = true;
        }

        Dashboard.Initialize(projects, tailing, events, OpenProject, () => Show(SettingsScreen), StartWizard);
        WizardScreen.Initialize(projects, project => OpenProject(project, false), ShowDashboard);
        ProjectScreen.Initialize(projects, tailing, tails, events, settings, ShowDashboard);
        SettingsScreen.Initialize(autostart, settings, notifications, ShowDashboard);

        // Opening the application is how alerts get acknowledged: the tray counter returns to normal.
        Activated += (_, _) => unread.Acknowledge();

        // The version stays visible on every screen: the footer lives outside the screens.
        Title = $"Moth-Eidol v{version}";
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
