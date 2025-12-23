using System;
using System.Windows;
//using OfficeOpenXml;

namespace TechKingPOS.App
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // ✅ EPPlus 8+ REQUIRED LICENSE SETUP
            //ExcelPackage.License = EPPlusLicense.NonCommercial;
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

            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var login = new LoginWindow();
            MainWindow = login;
            login.Show();
        }
    }
}
