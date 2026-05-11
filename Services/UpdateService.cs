using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Diagnostics;

namespace EstimateApp.Services;

public class UpdateService
{
    // Реальні посилання на ваш GitHub (viskunets/Iris)
    private const string VersionUrl = "https://raw.githubusercontent.com/viskunets/Iris/master/version.txt";
    private const string DownloadUrl = "https://github.com/viskunets/Iris/releases/latest";

    public string CurrentVersion => "4.5.1";

    public async Task<(bool canUpdate, string newVersion)> CheckForUpdatesAsync()
    {
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            
            var latestVersionStr = await client.GetStringAsync(VersionUrl);
            latestVersionStr = latestVersionStr.Trim();

            if (Version.TryParse(latestVersionStr, out var latest) && 
                Version.TryParse(CurrentVersion, out var current))
            {
                return (latest > current, latestVersionStr);
            }
        }
        catch
        {
            // Помилка зазвичай означає відсутність інтернету або приватний репозиторій
        }
        return (false, CurrentVersion);
    }

    public void OpenDownloadPage()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = DownloadUrl,
            UseShellExecute = true
        });
    }
}
