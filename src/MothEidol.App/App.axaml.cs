using System;
using System.Diagnostics;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using MothEidol.Core;
using MothEidol.Core.Logs;
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
            var autostart = AutostartFactory.Create(Environment.ProcessPath);
            var storage = OpenStorage();
            var settings = storage.Settings;
            ReconcileAutostart(settings, autostart);

            ProjectService? projects = null;
            _tailing = new TailingService(
                storage.Sources,
                storage.Positions,
                new TailOptions { PollInterval = TimeSpan.FromMilliseconds(settings.PollIntervalMs) },
                projectEnabled: id => projects?.IsEnabled(id) ?? true);
            projects = new ProjectService(storage.Projects, _tailing);
            _entries = new EntryPipeline(_tailing.Lines);
            _entries.Start(); // Subscribes before the sources start, so no line is missed.
            _tails = new ProjectTails(_entries.Entries, projects.ProjectOfSource, settings.ViewerMaxLines);
            _tailing.Start();
            desktop.Exit += (_, _) => StopTailing();

            _mainWindow = new MainWindow(version, autostart, settings, storage.Notice, _tailing, projects, _tails);
            desktop.MainWindow = _mainWindow;

            var trayIcons = TrayIcon.GetIcons(this);
            if (trayIcons is { Count: > 0 })
            {
                trayIcons[0].ToolTipText = $"Moth-Eidol v{version}";
            }

            if (OperatingSystem.IsMacOS())
            {
                MacDockIcon.TrySet(new Uri("avares://MothEidol.App/Assets/app-icon.png"));
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private sealed record Storage(
        AppSettings Settings,
        ILogSourceRepository Sources,
        IReadPositionStore Positions,
        IProjectRepository Projects,
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
            notice);

        static Storage InMemory(string notice) => new(
            new AppSettings(new InMemorySettingsRepository()),
            new InMemoryLogSourceRepository(),
            new InMemoryReadPositionStore(),
            new InMemoryProjectRepository(),
            notice);
    }

    /// <summary>Stops every source and saves its read position, so nothing is repeated on the next start.</summary>
    private void StopTailing()
    {
        try
        {
            _tailing?.DisposeAsync().AsTask().Wait(TimeSpan.FromSeconds(3));
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

    private void OnQuitClicked(object? sender, EventArgs e)
    {
        _mainWindow?.AllowClose();
        StopTailing();
        _desktop?.Shutdown();
    }
}
