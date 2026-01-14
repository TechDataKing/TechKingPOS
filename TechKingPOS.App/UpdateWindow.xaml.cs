using System.Windows;

namespace TechKingPOS.App
{
    public partial class UpdateWindow : Window
    {
        public string CurrentVersion { get; }
        public string AvailableVersion { get; }

        public bool UserAcceptedUpdate { get; private set; }

        public UpdateWindow(string currentVersion, string availableVersion)
        {
            InitializeComponent();

            CurrentVersion = currentVersion;
            AvailableVersion = availableVersion;

            DataContext = this;
        }

        private void UpdateNow_Click(object sender, RoutedEventArgs e)
        {
            var progressWindow = new UpdateProgressWindow();
            progressWindow.Show();

            Hide(); // hide the question dialog

            // Start update work in background
            Task.Run(() =>
            {
                RunUpdate(progressWindow);
            });
        }
private async void RunUpdate(UpdateProgressWindow progressWindow)
{
    try
    {
        progressWindow.SetStatus("Checking update...");
        progressWindow.SetProgress(5);

        var update = await Services.UpdateService.CheckForUpdateAsync();

        if (!update.IsUpdateAvailable)
        {
            progressWindow.SetStatus("No update available.");
            progressWindow.SetProgress(100);
            await Task.Delay(800);

            progressWindow.AllowClose();
            progressWindow.Close();
            Close();
            return;
        }

        var installerPath = await Services.UpdateService.DownloadInstallerAsync(
            update.InstallerUrl,
            update.InstallerFileName,
            (percent, status) =>
            {
                progressWindow.SetStatus(status);
                progressWindow.SetProgress(percent);
            });

        progressWindow.SetStatus("Installing update...");
        progressWindow.SetProgress(100);
        await Task.Delay(500);

        LaunchInstaller(installerPath, progressWindow);
    }
    catch (Exception ex)
    {
        progressWindow.SetStatus("Update failed.");
        MessageBox.Show(ex.Message, "Update Error", MessageBoxButton.OK, MessageBoxImage.Error);
        progressWindow.AllowClose();
        progressWindow.Close();
    }
}


private void RelaunchNewVersion(UpdateProgressWindow progressWindow)
{
    progressWindow.Dispatcher.Invoke(() =>
    {
        progressWindow.AllowClose();
        progressWindow.Close();
    });

    var exePath = System.IO.Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "TechKingPOS.App.exe"
    );

    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
    {
        FileName = exePath,
        UseShellExecute = true
    });

    Application.Current.Shutdown();
}


private async void LaunchInstaller(string installerPath, UpdateProgressWindow progressWindow)
{
    try
    {
        progressWindow.SetStatus("Installing update...");
        progressWindow.SetProgress(100);

        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = installerPath,
            Arguments = "/SILENT /NORESTART",
            UseShellExecute = true
        };

        var installerProcess = System.Diagnostics.Process.Start(psi);

        if (installerProcess != null)
        {
            await installerProcess.WaitForExitAsync();
        }

        progressWindow.SetStatus("Update complete. Launching application...");
        await Task.Delay(800);

        RelaunchNewVersion(progressWindow);
    }
    catch (Exception ex)
    {
        MessageBox.Show(ex.Message, "Installer Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}



        private void SkipUpdate_Click(object sender, RoutedEventArgs e)
        {
            UserAcceptedUpdate = false;
            DialogResult = false;
            Close();
        }
        
    }
}
