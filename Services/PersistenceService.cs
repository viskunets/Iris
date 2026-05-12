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
        // Зберігаємо в AppData/Roaming/EstimateApp - це стандартне місце для даних програм
        _folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EstimateApp");
        _filePath = Path.Combine(_folderPath, "data.json");
        
        if (!Directory.Exists(_folderPath)) Directory.CreateDirectory(_folderPath);
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
