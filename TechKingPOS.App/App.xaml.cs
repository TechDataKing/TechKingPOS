using System;
using System.Reflection;
using System.Windows;
using TechKingPOS.App.Data;
using TechKingPOS.App.Services;
using TechKingPOS.App.Security;

namespace TechKingPOS.App
{
    public partial class App : Application
    {
protected override async void OnStartup(StartupEventArgs e)
{
    // Global crash handlers...
    AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
    {
        if (ex.ExceptionObject is Exception exception)
            LogFatal("APPDOMAIN", exception);

        MessageBox.Show(
            ex.ExceptionObject.ToString(),
            "❌ Application Crash",
            MessageBoxButton.OK,
            MessageBoxImage.Error
        );
    };

    DispatcherUnhandledException += (s, ex) =>
    {
        LogFatal("DISPATCHER", ex.Exception);

        MessageBox.Show(
            ex.Exception.ToString(),
            "❌ UI Crash",
            MessageBoxButton.OK,
            MessageBoxImage.Error
        );

        ex.Handled = true;
    };

    base.OnStartup(e);
    ShutdownMode = ShutdownMode.OnExplicitShutdown;

    // Load services
    try
    {
        BranchService.Load();
        SettingsCache.Load();
    }
    catch (Exception ex)
    {
        LogFatal("STARTUP", ex);

        MessageBox.Show(
            "Critical startup error:\n" + ex.Message,
            "Startup Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error
        );

        Shutdown();
        return;
    }

    // 🔄 UPDATE CHECK (async-friendly)
    try
    {
        var updateResult = await UpdateService.CheckForUpdateAsync();

        if (updateResult.IsUpdateAvailable)
        {
            var updateWindow = new UpdateWindow(
                UpdateService.GetCurrentVersion(),
                updateResult.LatestVersion
            );

            var userAccepted = updateWindow.ShowDialog();

            if (userAccepted == true)
            {
                // UpdateWindow handles download + restart, so exit
                return;
            }
        }
    }
    catch (Exception ex)
    {
        LogFatal("UPDATE", ex);
    }

    // NORMAL APP STARTUP
    var settings = SettingsCache.Current;

    if (settings.RequireLogin)
        MainWindow = new LoginWindow();
    else
    {
        SessionService.LoginAsGuest();
        MainWindow = new MainWindow();
    }

    MainWindow.Show();
    ShutdownMode = ShutdownMode.OnMainWindowClose;
}

        private static void LogFatal(string source, Exception ex)
{
    try
    {
        var dir = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TechKingPOS",
            "crash"
        );

        System.IO.Directory.CreateDirectory(dir);

        var file = System.IO.Path.Combine(
            dir,
            $"{source}_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
        );

        System.IO.File.WriteAllText(file, ex.ToString());
    }
    catch
    {
        // never throw from logger
    }
}

    }
}
