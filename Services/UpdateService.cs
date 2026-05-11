using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;

namespace EstimateApp.Services;

public class UpdateService
{
    // Сюди ви потім пропишете посилання на ваш файл з версією
    private const string VersionUrl = "https://your-server.com/estimate_app/version.txt";
    private const string DownloadUrl = "https://your-server.com/estimate_app/EstimateApp.exe";

    public string CurrentVersion => "4.5.0";

    public async Task<(bool canUpdate, string newVersion)> CheckForUpdatesAsync()
    {
        try
        {
            using var client = new HttpClient();
            // Встановлюємо таймаут, щоб програма не зависла, якщо немає інтернету
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
            // Якщо сервера немає або інтернету - просто кажемо, що оновлень немає
        }
        return (false, CurrentVersion);
    }

    public void OpenDownloadPage()
    {
        // Відкриваємо посилання на завантаження в браузері
        Process.Start(new ProcessStartInfo
        {
            FileName = DownloadUrl,
            UseShellExecute = true
        });
    }
}
