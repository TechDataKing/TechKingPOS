using System;
using System.Windows;

namespace TechKingPOS.App
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
            {
                MessageBox.Show(
                    ex.ExceptionObject.ToString(),
                    "❌ Unhandled Domain Crash"
                );
            };

            DispatcherUnhandledException += (s, ex) =>
            {
                MessageBox.Show(
                    ex.Exception.ToString(),
                    "❌ Dispatcher Crash"
                );
                ex.Handled = true;
            };

            base.OnStartup(e);
        }
    }
}
