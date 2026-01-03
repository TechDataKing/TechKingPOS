using System;
using System.Windows;
using TechKingPOS.App.Data;    // ✅ correct namespace
using TechKingPOS.App.Models;
using TechKingPOS.App.Services;
using TechKingPOS.App.Security;

namespace TechKingPOS.App
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Global crash handlers
            AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
            {
                MessageBox.Show(
                    ex.ExceptionObject.ToString(),
                    "❌ Application Crash",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            };

            DispatcherUnhandledException += (s, ex) =>
            {
                MessageBox.Show(
                    ex.Exception.ToString(),
                    "❌ UI Crash",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                ex.Handled = true;
            };

            base.OnStartup(e);
            BranchService.Load();


            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // ✅ LOAD SETTINGS FIRST
            SettingsCache.Load();

            var settings = SettingsCache.Current;

            if (settings.RequireLogin)
            {
                MainWindow = new LoginWindow();
            }
            else
            {
                MainWindow = new MainWindow();
            }

            MainWindow.Show();
        }
    }
}
