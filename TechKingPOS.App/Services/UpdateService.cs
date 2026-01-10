using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace TechKingPOS.App.Services
{
    public class UpdateCheckResult
    {
        public bool IsUpdateAvailable { get; set; }
        public string CurrentVersion { get; set; } = "";
        public string LatestVersion { get; set; } = "";
        public string InstallerUrl { get; set; } = "";
        public string InstallerFileName { get; set; } = "";
    }

    public static class UpdateService
    {
        private const string GitHubLatestReleaseUrl =
            "https://api.github.com/repos/TechDataKing/TechKingPOS/releases/latest";

        public static string GetCurrentVersion()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return version == null ? "0.0.0" : $"{version.Major}.{version.Minor}.{version.Build}";
        }

        public static async Task<UpdateCheckResult> CheckForUpdateAsync()
        {
            var result = new UpdateCheckResult();
            result.CurrentVersion = GetCurrentVersion();

            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("TechKingPOS-Updater");

            var json = await client.GetStringAsync(GitHubLatestReleaseUrl);

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var tag = root.GetProperty("tag_name").GetString() ?? "";
            var latestVersion = tag.StartsWith("v") ? tag.Substring(1) : tag;

            result.LatestVersion = latestVersion;

            if (!IsNewerVersion(result.CurrentVersion, latestVersion))
            {
                result.IsUpdateAvailable = false;
                return result;
            }

            var expectedInstallerName = $"TechKingPOS_{latestVersion}.exe";

            var assets = root.GetProperty("assets").EnumerateArray();
            var installerAsset = assets.FirstOrDefault(a =>
                a.GetProperty("name").GetString()?.Equals(expectedInstallerName, StringComparison.OrdinalIgnoreCase) == true
            );

            if (installerAsset.ValueKind == JsonValueKind.Undefined)
                throw new Exception("Installer not found in GitHub release.");

            result.IsUpdateAvailable = true;
            result.InstallerFileName = expectedInstallerName;
            result.InstallerUrl = installerAsset.GetProperty("browser_download_url").GetString()!;

            return result;
        }

        private static bool IsNewerVersion(string current, string latest)
        {
            return Version.Parse(latest) > Version.Parse(current);
        }

        public static string GetUpdateFolder()
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TechKingPOS",
                "Updates"
            );

            Directory.CreateDirectory(path);
            return path;
        }
        public static async Task<string> DownloadInstallerAsync(
    string downloadUrl,
    string fileName,
    Action<int, string> progressCallback)
{
    var folder = GetUpdateFolder();
    var filePath = Path.Combine(folder, fileName);

    using var client = new HttpClient();
    client.DefaultRequestHeaders.UserAgent.ParseAdd("TechKingPOS-Updater");

    using var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
    response.EnsureSuccessStatusCode();

    var totalBytes = response.Content.Headers.ContentLength ?? -1L;
    var canReportProgress = totalBytes != -1;

    using var stream = await response.Content.ReadAsStreamAsync();
    using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);

    var buffer = new byte[81920];
    long totalRead = 0;
    int read;

    while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
    {
        await fs.WriteAsync(buffer, 0, read);
        totalRead += read;

        if (canReportProgress)
        {
            var percent = (int)((totalRead * 100) / totalBytes);
            progressCallback(percent, "Downloading update...");
        }
    }

    return filePath;
}

    }
}
