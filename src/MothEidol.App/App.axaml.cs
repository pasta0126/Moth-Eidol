using System;
using System.Diagnostics;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using MothEidol.Core;
using MothEidol.Core.Alerts;
using MothEidol.Core.Logs;
using MothEidol.Core.Notifications;
using MothEidol.Core.Projects;
using MothEidol.App.Platform;
using MothEidol.Core.Platform;
using MothEidol.Core.Settings;
using MothEidol.Persistence;

namespace MothEidol.App;

public partial class App : Application
{
    private IClassicDesktopStyleApplicationLifetime? _desktop;
    private MainWindow? _mainWindow;
    private SingleInstanceGuard? _instanceGuard;
    private TailingService? _tailing;
    private EntryPipeline? _entries;
    private ProjectTails? _tails;
    private AlertPipeline? _alerts;
    private NotificationDispatcher? _notifications;
    private UnreadCounter? _unread;
    private ToastPresenter? _toasts;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _desktop = desktop;
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            _instanceGuard = new SingleInstanceGuard($"MothEidol.{Environment.UserName}");
            if (!_instanceGuard.TryAcquire())
            {
                desktop.Shutdown();
                base.OnFrameworkInitializationCompleted();
                return;
            }

            _instanceGuard.ActivationRequested += () => Dispatcher.UIThread.Post(ShowMainWindow);
            desktop.Exit += (_, _) => _instanceGuard.Dispose();

            var version = AppVersion.FromAssembly(Assembly.GetExecutingAssembly());
            var autostart = AutostartFactory.Create(
                Environment.ProcessPath,
#if WINDOWS10_0_19041_0_OR_GREATER
                new Platform.WindowsStartupTaskGateway());
#else
                null);
#endif
            var storage = OpenStorage();
            var settings = storage.Settings;
            ApplyTheme(settings.Theme);
            ReconcileAutostart(settings, autostart);

            ProjectService? projects = null;
            _tailing = new TailingService(
                storage.Sources,
                storage.Positions,
                new TailOptions { PollInterval = TimeSpan.FromMilliseconds(settings.PollIntervalMs) },
                projectEnabled: id => projects?.IsEnabled(id) ?? true);
            projects = new ProjectService(storage.Projects, _tailing, storage.Events);
            _entries = new EntryPipeline(_tailing.Lines);
            _entries.Start(); // Subscribes before the sources start, so no line is missed.
            _alerts = new AlertPipeline(
                _entries.Entries, projects, storage.Events,
                new AlertOptions { Retention = TimeSpan.FromDays(settings.EventRetentionDays) });
            _alerts.Start();

            // Alerts reach the user through independent channels, each of which can be switched off.
            _unread = new UnreadCounter();
            var toastStack = new ToastStack();
            _notifications = new NotificationDispatcher(n => settings.IsChannelEnabled(n.Id, n.EnabledByDefault));
            var windows = OperatingSystem.IsWindows();
            var toastChannel = new ToastNotifier(toastStack, projects, enabledByDefault: NotificationDefaults.ToastEnabledByDefault(windows));
            _notifications.Register(toastChannel);
            _notifications.Register(new TrayNotifier(_unread));
#if WINDOWS10_0_19041_0_OR_GREATER
            // Windows: the native notification carries the sound itself, so the sound channel is not registered.
            Platform.WindowsNotifier.RegisterIdentity(ExtractNotificationIcon());
            _notifications.Register(new Platform.WindowsNotifier(
                projects,
                () => settings.IsAlertSoundEnabled(NotificationDefaults.SoundEnabledByDefault(true)),
                () => Dispatcher.UIThread.Post(ShowMainWindow),
                toastChannel.Show));
#else
            _notifications.Register(new SoundNotifier(new SystemAudioPlayer()));
#endif
            _notifications.Start(_alerts.Deliveries);

            _tails = new ProjectTails(_entries.Entries, projects.ProjectOfSource, settings.ViewerMaxLines);
            _tailing.Start();
            desktop.Exit += (_, _) => StopTailing();

            _mainWindow = new MainWindow(version, autostart, settings, storage.Notice, _tailing, projects, _tails, storage.Events, _notifications, _unread);
            desktop.MainWindow = _mainWindow;

            var trayIcons = TrayIcon.GetIcons(this);
            if (trayIcons is { Count: > 0 })
            {
                _ = new TrayPresenter(trayIcons[0], _unread);
            }

            _toasts = new ToastPresenter(toastStack, _mainWindow);

            if (OperatingSystem.IsMacOS())
            {
                MacDockIcon.TrySet(new Uri("avares://MothEidol.App/Assets/app-icon.png"));
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>Switches the whole application to a theme right away: Moth is the Dark variant, Vesper the Light one.</summary>
    public static void ApplyTheme(AppTheme theme)
    {
        if (Current is { } app)
        {
            app.RequestedThemeVariant = theme == AppTheme.Vesper
                ? Avalonia.Styling.ThemeVariant.Light
                : Avalonia.Styling.ThemeVariant.Dark;
            Dispatcher.UIThread.Post(Views.ThemeBrushes.NotifyChanged, DispatcherPriority.Background);
        }
    }

    private sealed record Storage(
        AppSettings Settings,
        ILogSourceRepository Sources,
        IReadPositionStore Positions,
        IProjectRepository Projects,
        IEventStore Events,
        string? Notice);

    private static Storage OpenStorage()
    {
        try
        {
            var path = DataDirectory.DatabasePath(DataDirectory.Resolve());
            var database = SqliteDatabase.Open(path);

            return database.Status switch
            {
                DatabaseStatus.Ready => Persistent(database, null),
                DatabaseStatus.RecoveredFromCorruption => Persistent(database,
                    $"The local database was corrupt. A backup was kept at {database.BackupPath} and a new one was created."),
                _ => InMemory(
                    $"The local database was created by a newer version (schema {database.FoundVersion}). It was not modified and settings and sources will not be saved in this session."),
            };
        }
        catch (Exception ex)
        {
            return InMemory($"Local storage is unavailable ({ex.Message}). Settings and sources will not be saved in this session.");
        }

        static Storage Persistent(SqliteDatabase database, string? notice) => new(
            new AppSettings(new SqliteSettingsRepository(database)),
            new SqliteLogSourceRepository(database),
            new SqliteReadPositionStore(database),
            new SqliteProjectRepository(database),
            new SqliteEventStore(database),
            notice);

        static Storage InMemory(string notice) => new(
            new AppSettings(new InMemorySettingsRepository()),
            new InMemoryLogSourceRepository(),
            new InMemoryReadPositionStore(),
            new InMemoryProjectRepository(),
            new InMemoryEventStore(),
            notice);
    }

    /// <summary>Stops every source and saves its read position, so nothing is repeated on the next start.</summary>
    private void StopTailing()
    {
        try
        {
            _tailing?.DisposeAsync().AsTask().Wait(TimeSpan.FromSeconds(3));
            _notifications?.DisposeAsync().AsTask().Wait(TimeSpan.FromSeconds(2));
            _alerts?.DisposeAsync().AsTask().Wait(TimeSpan.FromSeconds(2));
            _tails?.Dispose();
            _entries?.DisposeAsync().AsTask().Wait(TimeSpan.FromSeconds(2));
        }
        catch (Exception)
        {
            // Best effort on the way out.
        }
    }

    private static void ReconcileAutostart(AppSettings settings, IAutostart autostart)
    {
        if (!autostart.IsSupported)
        {
            return;
        }

        try
        {
            if (autostart.IsEnabled != settings.StartWithSession)
            {
                autostart.SetEnabled(settings.StartWithSession);
            }
        }
        catch (Exception)
        {
            // Best effort: the window shows the real state and reports errors on change.
        }
    }

#if WINDOWS10_0_19041_0_OR_GREATER
    /// <summary>Copies the app icon next to the data so the notification identity can point at a file.</summary>
    private static string? ExtractNotificationIcon()
    {
        try
        {
            var path = Path.Combine(DataDirectory.Resolve(), "notification-icon.png");
            if (!File.Exists(path))
            {
                using var source = Avalonia.Platform.AssetLoader.Open(new Uri("avares://MothEidol.App/Assets/app-icon.png"));
                using var target = File.Create(path);
                source.CopyTo(target);
            }
            return path;
        }
        catch (Exception)
        {
            return null;
        }
    }
#endif

    private void ShowMainWindow()
    {
        if (_mainWindow is null)
        {
            return;
        }

        _mainWindow.ShowFromTray();
    }

    private void OnTrayClicked(object? sender, EventArgs e) => ShowMainWindow();

    private void OnOpenClicked(object? sender, EventArgs e) => ShowMainWindow();

    private void OnAcknowledgeClicked(object? sender, EventArgs e) => _unread?.Acknowledge();

    private void OnQuitClicked(object? sender, EventArgs e)
    {
        _mainWindow?.AllowClose();
        StopTailing();
        _desktop?.Shutdown();
    }
}
