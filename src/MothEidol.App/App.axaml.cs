using System;
using System.Diagnostics;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using MothEidol.Core;
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
            var (settings, notice) = OpenSettings();
            ReconcileAutostart(settings, autostart);
            _mainWindow = new MainWindow(version, autostart, settings, notice);
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

    private static (AppSettings Settings, string? Notice) OpenSettings()
    {
        try
        {
            var path = DataDirectory.DatabasePath(DataDirectory.Resolve());
            var database = SqliteDatabase.Open(path);

            return database.Status switch
            {
                DatabaseStatus.Ready =>
                    (new AppSettings(new SqliteSettingsRepository(database)), null),
                DatabaseStatus.RecoveredFromCorruption =>
                    (new AppSettings(new SqliteSettingsRepository(database)),
                     $"The local database was corrupt. A backup was kept at {database.BackupPath} and a new one was created."),
                _ =>
                    (new AppSettings(new InMemorySettingsRepository()),
                     $"The local database was created by a newer version (schema {database.FoundVersion}). It was not modified and settings will not be saved in this session."),
            };
        }
        catch (Exception ex)
        {
            return (new AppSettings(new InMemorySettingsRepository()),
                    $"Local storage is unavailable ({ex.Message}). Settings will not be saved in this session.");
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
        _desktop?.Shutdown();
    }
}
