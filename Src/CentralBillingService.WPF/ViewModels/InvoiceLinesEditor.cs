using System.Collections.Specialized;
using System.ComponentModel;
using CentralBillingService.Domain.Interfaces;

namespace CentralBillingService.WPF.ViewModels;

/// <summary>
/// Editable invoice lines with live totals and an exchange-rate hint per foreign-currency line.
/// Quantities may be negative (to cancel all or part of a line) and the tax rate may be any value.
/// </summary>
public partial class InvoiceLinesEditor : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Func<string> _defaultCurrencyCode;

    public ObservableCollection<InvoiceLineItem> Lines { get; } = [];

    public decimal TotalsSubtotal => Lines.Sum(line => line.Quantity * line.UnitPrice);
    public decimal TotalsTax => Lines.Sum(line => line.Quantity * line.UnitPrice * line.TaxRate / 100m);
    public decimal TotalsTotal => TotalsSubtotal + TotalsTax;

    public string TotalsSubtotalFormatted => $"{TotalsSubtotal:N2}";
    public string TotalsTaxFormatted => $"{TotalsTax:N2}";
    public string TotalsTotalFormatted => $"{TotalsTotal:N2}";

    public InvoiceLinesEditor(IServiceScopeFactory scopeFactory, Func<string> defaultCurrencyCode)
    {
        _scopeFactory = scopeFactory;
        _defaultCurrencyCode = defaultCurrencyCode;
        Lines.CollectionChanged += OnLinesCollectionChanged;
    }

    public void ReplaceLines(IEnumerable<InvoiceLineItem> newLines)
    {
        Lines.Clear();
        foreach (InvoiceLineItem line in newLines)
        {
            Lines.Add(line);
        }
    }

    [RelayCommand]
    void AddLine() => Lines.Add(new InvoiceLineItem { CurrencyCode = _defaultCurrencyCode() });

    [RelayCommand]
    void RemoveLine(InvoiceLineItem line) => Lines.Remove(line);

    private void OnLinesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs eventArgs)
    {
        if (eventArgs.NewItems is not null)
        {
            foreach (InvoiceLineItem item in eventArgs.NewItems)
            {
                item.PropertyChanged += OnLineChanged;
            }
        }
        if (eventArgs.OldItems is not null)
        {
            foreach (InvoiceLineItem item in eventArgs.OldItems)
            {
                item.PropertyChanged -= OnLineChanged;
            }
        }
        RefreshTotals();
    }

    private void OnLineChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        RefreshTotals();
        if (sender is InvoiceLineItem line && eventArgs.PropertyName == nameof(InvoiceLineItem.CurrencyCode))
        {
            _ = FetchRateHintAsync(line);
            if (line.CurrencyCode != "EUR")
            {
                line.TaxRate = 0;
            }
        }
    }

    private async Task FetchRateHintAsync(InvoiceLineItem line)
    {
        string? hint = null;
        if (line.CurrencyCode != "EUR")
        {
            try
            {
                using IServiceScope scope = _scopeFactory.CreateScope();
                IExchangeRateProvider provider = scope.ServiceProvider.GetRequiredService<IExchangeRateProvider>();
                Currency currency = Currency.From(line.CurrencyCode);
                if (provider.Supports(currency, Currency.EUR))
                {
                    ExchangeRate rate = await provider.GetRateAsync(currency, Currency.EUR);
                    hint = $"1 {line.CurrencyCode} ≈ {rate.Rate:G5} EUR";
                }
                else
                {
                    hint = "Divisa no soportada por el proveedor de cambio";
                }
            }
            catch
            {
                hint = "Cambio no disponible (se calculará al emitir)";
            }
        }
        line.ExchangeRateHint = hint;
    }

    public void RefreshTotals()
    {
        OnPropertyChanged(nameof(TotalsSubtotal));
        OnPropertyChanged(nameof(TotalsTax));
        OnPropertyChanged(nameof(TotalsTotal));
        OnPropertyChanged(nameof(TotalsSubtotalFormatted));
        OnPropertyChanged(nameof(TotalsTaxFormatted));
        OnPropertyChanged(nameof(TotalsTotalFormatted));
    }
}
