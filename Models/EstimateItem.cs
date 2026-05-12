using CommunityToolkit.Mvvm.ComponentModel;

namespace EstimateApp.Models;

public partial class EstimateItem : ObservableObject
{
    [ObservableProperty]
    private bool _isSelected = true;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private decimal _price;

    [ObservableProperty]
    private double _factor = 1.0;

    [ObservableProperty]
    private string _category = "Загальне";

    [ObservableProperty]
    private decimal _vatRate = 0.20m;

    [ObservableProperty]
    private bool _includeVat = true;

    [ObservableProperty]
    private System.DateTime _createdAt = System.DateTime.Now;

    public decimal VatAmount => IncludeVat ? Price * (decimal)Factor * VatRate : 0;
    public decimal Total => (Price * (decimal)Factor) + VatAmount;

    partial void OnIsSelectedChanged(bool value) => RefreshTotals();
    partial void OnPriceChanged(decimal value) => RefreshTotals();
    partial void OnFactorChanged(double value) => RefreshTotals();
    partial void OnIncludeVatChanged(bool value) => RefreshTotals();
    partial void OnVatRateChanged(decimal value) => RefreshTotals();

    private void RefreshTotals()
    {
        OnPropertyChanged(nameof(VatAmount));
        OnPropertyChanged(nameof(Total));
    }
}
