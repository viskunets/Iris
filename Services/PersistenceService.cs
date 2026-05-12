using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using EstimateApp.Models;
using EstimateApp.ViewModels;

namespace EstimateApp.Services;

public class AppData
{
    public List<EstimateItem> Catalog { get; set; } = new();
    public List<SavedCalculation> History { get; set; } = new();
    public string HeaderColor { get; set; } = "#2196F3";
    public string FooterColor { get; set; } = "#2196F3";
    public decimal VatRatePercent { get; set; } = 20;
    public bool IsDarkTheme { get; set; }
    public double WindowWidth { get; set; } = 1200;
    public double WindowHeight { get; set; } = 800;
    public double WindowTop { get; set; } = 100;
    public double WindowLeft { get; set; } = 100;
    public bool IsMaximized { get; set; }

    // НОВІ ПОЛЯ ДЛЯ ЕКСПОРТУ
    public string CompanyWebsite { get; set; } = "";
    public string AuthorName { get; set; } = "";
    public string AuthorPhone { get; set; } = "";
    public string AuthorEmail { get; set; } = "";
    public string HeaderImagePath { get; set; } = "maxeffectshow.jpg";
    public bool AutoDate { get; set; } = true;
}

public class PersistenceService
{
    private readonly string _folderPath;
    private readonly string _filePath;

    public PersistenceService()
    {
        // Переносимо дані в LocalAppData/Iris для надійності при оновленнях
        _folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Iris");
        _filePath = Path.Combine(_folderPath, "data.json");
        
        if (!Directory.Exists(_folderPath)) Directory.CreateDirectory(_folderPath);

        // МІГРАЦІЯ: Якщо є старий файл у папці з програмою або в Roaming, переносимо його
        MigrateOldData();
    }

    private void MigrateOldData()
    {
        try
        {
            string appDirFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data.json");
            string roamingFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EstimateApp", "data.json");

            if (File.Exists(appDirFile) && !File.Exists(_filePath))
            {
                File.Copy(appDirFile, _filePath, true);
            }
            else if (File.Exists(roamingFile) && !File.Exists(_filePath))
            {
                File.Copy(roamingFile, _filePath, true);
            }
        }
        catch { /* Ігноруємо помилки міграції */ }
    }

    public void SaveData(AppData data)
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(data, options);
            File.WriteAllText(_filePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Помилка збереження: {ex.Message}");
        }
    }

    public AppData LoadData()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<AppData>(json) ?? new AppData();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Помилка завантаження: {ex.Message}");
        }
        return new AppData();
    }
}
