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

        LaunchInstaller(installerPath);
    }
    catch (Exception ex)
    {
        progressWindow.SetStatus("Update failed.");
        MessageBox.Show(ex.Message, "Update Error", MessageBoxButton.OK, MessageBoxImage.Error);
        progressWindow.AllowClose();
        progressWindow.Close();
    }
}


private void RelaunchApplication(UpdateProgressWindow progressWindow)
{
    progressWindow.Dispatcher.Invoke(() =>
    {
        progressWindow.AllowClose();
        progressWindow.Close();
    });

    // Start new instance
    System.Diagnostics.Process.Start(
        Environment.ProcessPath!
    );

    // Kill old instance
    Application.Current.Dispatcher.Invoke(() =>
    {
        Application.Current.Shutdown();
    });
}

private void LaunchInstaller(string installerPath)
{
    var psi = new System.Diagnostics.ProcessStartInfo
    {
        FileName = installerPath,
        Arguments = "/VERYSILENT /NORESTART",
        UseShellExecute = true
    };

    System.Diagnostics.Process.Start(psi);

    Application.Current.Dispatcher.Invoke(() =>
    {
        Application.Current.Shutdown();
    });
}


        private void SkipUpdate_Click(object sender, RoutedEventArgs e)
        {
            UserAcceptedUpdate = false;
            DialogResult = false;
            Close();
        }
    }
}
