using System;
using System.Diagnostics;
using System.Reflection;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using MothEidol.Core;
using MothEidol.Core.Platform;

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
            desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnExplicitShutdown;

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
            _mainWindow = new MainWindow(version, autostart);
            desktop.MainWindow = _mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
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
