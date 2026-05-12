using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO.Compression;

namespace EstimateApp.Services;

public class UpdateService
{
    private const string VersionUrl = "https://raw.githubusercontent.com/viskunets/Iris/master/version.txt";
    private const string DownloadUrl = "https://github.com/viskunets/Iris/releases/latest/download/Iris.zip";

    public string CurrentVersion => "4.5.2";

    public async Task<(bool canUpdate, string newVersion)> CheckForUpdatesAsync()
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(5);
        var latestVersionStr = (await client.GetStringAsync(VersionUrl)).Trim();

        if (Version.TryParse(latestVersionStr, out var latest) && 
            Version.TryParse(CurrentVersion, out var current))
        {
            return (latest > current, latestVersionStr);
        }
        return (false, CurrentVersion);
    }

    public async Task DownloadAndInstallUpdateAsync(Action<string> logAction)
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "IrisUpdate");
        string zipPath = Path.Combine(Path.GetTempPath(), "update.zip");
        string currentDir = AppDomain.CurrentDomain.BaseDirectory;

        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        Directory.CreateDirectory(tempDir);

        logAction("Завантаження оновлення...");
        using (var client = new HttpClient())
        {
            var data = await client.GetByteArrayAsync(DownloadUrl);
            await File.WriteAllBytesAsync(zipPath, data);
        }

        logAction("Розпакування...");
        ZipFile.ExtractToDirectory(zipPath, tempDir);

        logAction("Підготовка до перезапуску...");
        string batchContent = $@"
@echo off
timeout /t 2 /nobreak > nul
xcopy /y /s /e ""{tempDir}\*"" ""{currentDir}""
start """" ""{Path.Combine(currentDir, "Iris.exe")}""
del ""%~f0""
";
        string batchPath = Path.Combine(Path.GetTempPath(), "update_iris.bat");
        await File.WriteAllTextAsync(batchPath, batchContent);

        Process.Start(new ProcessStartInfo
        {
            FileName = batchPath,
            CreateNoWindow = true,
            UseShellExecute = true
        });

        Environment.Exit(0);
    }
}
