using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EstimateApp.Models;
using EstimateApp.Services;
using MaterialDesignThemes.Wpf;

namespace EstimateApp.ViewModels;

public class SavedCalculation
{
    public string Name { get; set; } = "Без назви";
    public DateTime Date { get; set; } = DateTime.Now;
    public ObservableCollection<EstimateItem> Items { get; set; } = new();
    public decimal GrandTotal { get; set; }
}

public partial class MainViewModel : ObservableObject
{
    private readonly ExportService _exportService = new();
    private readonly UpdateService _updateService = new();
    private readonly PersistenceService _persistenceService = new();
    
    [ObservableProperty] private string _calculationName = "Новий розрахунок";
    [ObservableProperty] private string _appVersion = "v4.5.1";
    [ObservableProperty] private decimal _grandTotal;
    [ObservableProperty] private decimal _vatRatePercent = 20;
    [ObservableProperty] private bool _isDarkTheme = false;
    [ObservableProperty] private int _selectedTabIndex = 0;
    [ObservableProperty] private bool _isMenuOpen = false;
    [ObservableProperty] private string _searchText = string.Empty;

    // Для діалогів
    [ObservableProperty] private string _newProjectName = string.Empty;
    [ObservableProperty] private bool _isDialogOpen = false;
    [ObservableProperty] private int _dialogState = 0; 

    public SnackbarMessageQueue MessageQueue { get; } = new(TimeSpan.FromSeconds(3));

    [ObservableProperty] private string _headerColor = "#2196F3";
    [ObservableProperty] private string _footerColor = "#2196F3";

    // Параметри вікна
    [ObservableProperty] private double _winWidth;
    [ObservableProperty] private double _winHeight;
    [ObservableProperty] private double _winTop;
    [ObservableProperty] private double _winLeft;
    [ObservableProperty] private WindowState _winState;

    public ObservableCollection<EstimateItem> Catalog { get; } = new();
    public System.ComponentModel.ICollectionView CatalogView { get; }
    public ObservableCollection<SavedCalculation> History { get; } = new();

    public MainViewModel()
    {
        CatalogView = System.Windows.Data.CollectionViewSource.GetDefaultView(Catalog);
        CatalogView.Filter = o => {
            if (string.IsNullOrWhiteSpace(SearchText)) return true;
            var item = (EstimateItem)o;
            return item.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) || item.Category.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
        };

        // ЗАВАНТАЖЕННЯ ДАНИХ
        var data = _persistenceService.LoadData();
        HeaderColor = data.HeaderColor;
        FooterColor = data.FooterColor;
        VatRatePercent = data.VatRatePercent;
        IsDarkTheme = data.IsDarkTheme;
        
        WinWidth = data.WindowWidth;
        WinHeight = data.WindowHeight;
        WinTop = data.WindowTop;
        WinLeft = data.WindowLeft;
        WinState = data.IsMaximized ? WindowState.Maximized : WindowState.Normal;

        foreach (var item in data.Catalog) AddToCatalogInternal(item);
        foreach (var calc in data.History) History.Add(calc);

        // Якщо база порожня, додаємо демо-дані
        if (!Catalog.Any())
        {
            AddToCatalogInternal(new EstimateItem { Name = "Мікшерний пульт YAMAHA CL5", Price = 5000, Category = "Звук" });
            AddToCatalogInternal(new EstimateItem { Name = "Акустична система d&b VIO", Price = 2000, Category = "Звук" });
        }

        Catalog.CollectionChanged += (s, e) => SaveAll();
        History.CollectionChanged += (s, e) => SaveAll();
        
        Application.Current.Dispatcher.InvokeAsync(() => ToggleTheme());
    }

    private void AddToCatalogInternal(EstimateItem item)
    {
        item.IsSelected = false; // Завжди знімаємо вибір при старті
        item.PropertyChanged += (s, args) => { 
            if (args.PropertyName == nameof(EstimateItem.IsSelected) || args.PropertyName == nameof(EstimateItem.Total)) UpdateGrandTotal(); 
            SaveAll(); 
        };
        Catalog.Add(item);
    }

    private void SaveAll()
    {
        var data = new AppData
        {
            Catalog = Catalog.ToList(),
            History = History.ToList(),
            HeaderColor = HeaderColor,
            FooterColor = FooterColor,
            VatRatePercent = VatRatePercent,
            IsDarkTheme = IsDarkTheme,
            WindowWidth = WinWidth,
            WindowHeight = WinHeight,
            WindowTop = WinTop,
            WindowLeft = WinLeft,
            IsMaximized = WinState == WindowState.Maximized
        };
        _persistenceService.SaveData(data);
    }

    [RelayCommand]
    private void StartNewProjectFlow()
    {
        bool hasSelected = Catalog.Any(i => i.IsSelected);
        if (hasSelected) DialogState = 1;
        else { NewProjectName = $"Проект від {DateTime.Now:dd.MM.yyyy}"; DialogState = 0; }
        IsDialogOpen = true;
    }

    [RelayCommand]
    private void SaveAndContinue() { SaveEstimate(); SkipSaveAndContinue(); }

    [RelayCommand]
    private void SkipSaveAndContinue() { NewProjectName = $"Проект від {DateTime.Now:dd.MM.yyyy}"; DialogState = 0; }

    [RelayCommand]
    private void ConfirmNewProject()
    {
        CalculationName = NewProjectName;
        foreach (var item in Catalog) item.IsSelected = false;
        IsDialogOpen = false;
        SelectedTabIndex = 0;
        MessageQueue.Enqueue($"Створено: {CalculationName}");
        SaveAll();
    }

    [RelayCommand] private void AddToCatalog() => AddToCatalogInternal(new EstimateItem { Name = "Нова позиція", Price = 0, Category = "Загальне" });
    [RelayCommand] private void RemoveFromCatalog(EstimateItem item) { if (item != null) Catalog.Remove(item); }
    [RelayCommand] private void ResetSearch() => SearchText = string.Empty;
    partial void OnSearchTextChanged(string value) => CatalogView.Refresh();

    [RelayCommand]
    private void SaveEstimate()
    {
        var selectedItems = Catalog.Where(i => i.IsSelected).ToList();
        if (!selectedItems.Any()) { MessageQueue.Enqueue("Помилка: Не вибрано товарів!"); return; }
        var saved = new SavedCalculation { Name = CalculationName, Date = DateTime.Now, GrandTotal = GrandTotal, Items = new ObservableCollection<EstimateItem>(selectedItems.Select(i => new EstimateItem { Name = i.Name, Price = i.Price, Factor = i.Factor, Category = i.Category, IncludeVat = i.IncludeVat, IsSelected = true })) };
        History.Insert(0, saved);
        MessageQueue.Enqueue($"✅ Проект '{CalculationName}' збережено!");
        SaveAll();
    }

    [RelayCommand]
    private void ExportPdf()
    {
        var selectedItems = Catalog.Where(i => i.IsSelected).ToList();
        if (!selectedItems.Any()) return;
        var sfd = new Microsoft.Win32.SaveFileDialog { Filter = "PDF Files (*.pdf)|*.pdf", FileName = $"{CalculationName}.pdf" };
        if (sfd.ShowDialog() == true) _exportService.GeneratePdf(sfd.FileName, selectedItems, GrandTotal);
    }

    [RelayCommand]
    private void ExportExcel()
    {
        var selectedItems = Catalog.Where(i => i.IsSelected).ToList();
        if (!selectedItems.Any()) return;
        var sfd = new Microsoft.Win32.SaveFileDialog { Filter = "Excel Files (*.xlsx)|*.xlsx", FileName = $"{CalculationName}.xlsx" };
        if (sfd.ShowDialog() == true) _exportService.ExportToExcel(sfd.FileName, selectedItems, GrandTotal);
    }

    [RelayCommand]
    private void LoadCalculation(SavedCalculation calc)
    {
        CalculationName = calc.Name;
        foreach (var item in Catalog) item.IsSelected = false;
        foreach (var savedItem in calc.Items)
        {
            var existing = Catalog.FirstOrDefault(i => i.Name == savedItem.Name);
            if (existing != null) { existing.IsSelected = true; existing.Price = savedItem.Price; existing.Factor = savedItem.Factor; existing.IncludeVat = savedItem.IncludeVat; }
            else { savedItem.IsSelected = true; AddToCatalogInternal(savedItem); }
        }
        SelectedTabIndex = 0;
        MessageQueue.Enqueue($"Завантажено: {CalculationName}");
    }

    [RelayCommand] private void ExportHistoryPdf(SavedCalculation calc) => _exportService.GeneratePdf("temp.pdf", calc.Items, calc.GrandTotal);
    [RelayCommand] private void RemoveHistory(SavedCalculation calc) { if (calc != null) History.Remove(calc); MessageQueue.Enqueue("Запис видалено"); }

    [RelayCommand]
    private async Task CheckForUpdates()
    {
        MessageQueue.Enqueue("Перевірка оновлень...");
        try 
        {
            var (canUpdate, newVersion) = await _updateService.CheckForUpdatesAsync();
            if (canUpdate)
            {
                var result = MessageBox.Show($"Знайдено нову версію {newVersion}! Встановити оновлення зараз?", "Автооновлення", MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (result == MessageBoxResult.Yes) 
                {
                    await _updateService.DownloadAndInstallUpdateAsync(msg => MessageQueue.Enqueue(msg));
                }
            }
            else MessageQueue.Enqueue("У вас найновіша версія!");
        }
        catch (Exception ex)
        {
            MessageQueue.Enqueue($"Помилка оновлення: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        var paletteHelper = new PaletteHelper();
        Theme theme = paletteHelper.GetTheme();
        theme.SetBaseTheme(IsDarkTheme ? BaseTheme.Dark : BaseTheme.Light);
        paletteHelper.SetTheme(theme);
    }

    private void UpdateGrandTotal() => GrandTotal = Catalog.Where(i => i.IsSelected).Sum(i => i.Total);
    partial void OnIsDarkThemeChanged(bool value) { ToggleTheme(); SaveAll(); }
    partial void OnHeaderColorChanged(string value) => SaveAll();
    partial void OnFooterColorChanged(string value) => SaveAll();
    partial void OnVatRatePercentChanged(decimal value) => SaveAll();
    partial void OnSelectedTabIndexChanged(int value) => IsMenuOpen = false;

    partial void OnWinWidthChanged(double value) => SaveAll();
    partial void OnWinHeightChanged(double value) => SaveAll();
    partial void OnWinTopChanged(double value) => SaveAll();
    partial void OnWinLeftChanged(double value) => SaveAll();
    partial void OnWinStateChanged(WindowState value) => SaveAll();
}
