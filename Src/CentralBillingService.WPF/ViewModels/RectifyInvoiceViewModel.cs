using CentralBillingService.Domain.Interfaces;
using CentralBillingService.Domain.ValueObjects;

namespace CentralBillingService.WPF.ViewModels;

public partial class RectifyInvoiceViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Action _onRectified;
    private readonly Action _onCancel;

    public BillingSourceSummary BillingSource { get; }
    public string OriginalInvoiceNumber { get; }

    // Master data
    [ObservableProperty] ObservableCollection<SeriesRecord> availableSeries = [];
    [ObservableProperty] ObservableCollection<ProductRecord> availableProducts = [];

    [ObservableProperty] string rectificativeSerie = "R";
    [ObservableProperty] string reason = string.Empty;
    [ObservableProperty] string selectedRectificationType = "Substitution";
    [ObservableProperty] string paymentReference = string.Empty;
    [ObservableProperty] string? paymentMethod;
    [ObservableProperty] string? transactionData;
    [ObservableProperty] string? notes;
    [ObservableProperty] ObservableCollection<InvoiceLineItem> differenceLines = [];
    [ObservableProperty] bool isSaving;
    [ObservableProperty] string? errorMessage;
    [ObservableProperty] bool success;
    [ObservableProperty] InvoiceResult? originalInvoice;
    [ObservableProperty] bool isLoadingOriginal;

    public static string[] RectificationTypes { get; } = ["Substitution", "Difference"];
    public bool IsDifference => SelectedRectificationType == "Difference";

    partial void OnSelectedRectificationTypeChanged(string value) =>
        OnPropertyChanged(nameof(IsDifference));

    // Default currency for new difference lines — derived from original invoice's primary currency
    public string DefaultCurrencyCode => OriginalInvoice?.AppliedExchangeRate.FromCurrency ?? "EUR";

    // Live totals for difference lines (raw sums — actual EUR amounts computed at rectification time)
    public decimal TotalsSubtotal => DifferenceLines.Sum(l => l.Quantity * l.UnitPrice);
    public decimal TotalsTax => DifferenceLines.Sum(l => l.Quantity * l.UnitPrice * l.TaxRate / 100m);
    public decimal TotalsTotal => TotalsSubtotal + TotalsTax;

    public string TotalsSubtotalFormatted => $"{TotalsSubtotal:N2}";
    public string TotalsTaxFormatted      => $"{TotalsTax:N2}";
    public string TotalsTotalFormatted    => $"{TotalsTotal:N2}";

    public RectifyInvoiceViewModel(
        IServiceScopeFactory scopeFactory,
        BillingSourceSummary billingSource,
        LocalMasterDataStore masterDataStore,
        string originalInvoiceNumber,
        Action onRectified,
        Action onCancel)
    {
        _scopeFactory = scopeFactory;
        _onRectified = onRectified;
        _onCancel = onCancel;
        BillingSource = billingSource;
        OriginalInvoiceNumber = originalInvoiceNumber;

        AvailableSeries = new ObservableCollection<SeriesRecord>(masterDataStore.LoadSeries());
        AvailableProducts = new ObservableCollection<ProductRecord>(masterDataStore.LoadProducts());

        DifferenceLines.CollectionChanged += OnDiffLinesCollectionChanged;
    }

    private void OnDiffLinesCollectionChanged(object? sender,
        System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
            foreach (InvoiceLineItem item in e.NewItems)
                item.PropertyChanged += OnDiffLineChanged;
        if (e.OldItems is not null)
            foreach (InvoiceLineItem item in e.OldItems)
                item.PropertyChanged -= OnDiffLineChanged;
        RefreshTotals();
    }

    private void OnDiffLineChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        RefreshTotals();
        if (sender is InvoiceLineItem line && e.PropertyName == nameof(InvoiceLineItem.CurrencyCode))
            _ = FetchRateHintAsync(line);
    }

    private async Task FetchRateHintAsync(InvoiceLineItem line)
    {
        if (line.CurrencyCode == "EUR")
        {
            line.ExchangeRateHint = null;
            return;
        }
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var provider = scope.ServiceProvider.GetRequiredService<IExchangeRateProvider>();
            var currency = Currency.From(line.CurrencyCode);
            if (!provider.Supports(currency, Currency.EUR))
            {
                line.ExchangeRateHint = "Divisa no soportada por el proveedor de cambio";
                return;
            }
            var rate = await provider.GetRateAsync(currency, Currency.EUR);
            line.ExchangeRateHint = $"1 {line.CurrencyCode} ≈ {rate.Rate:G5} EUR";
        }
        catch
        {
            line.ExchangeRateHint = "Cambio no disponible (se calculará al emitir)";
        }
    }

    partial void OnOriginalInvoiceChanged(InvoiceResult? value) => RefreshTotals();

    private void RefreshTotals()
    {
        OnPropertyChanged(nameof(TotalsSubtotal));
        OnPropertyChanged(nameof(TotalsTax));
        OnPropertyChanged(nameof(TotalsTotal));
        OnPropertyChanged(nameof(TotalsSubtotalFormatted));
        OnPropertyChanged(nameof(TotalsTaxFormatted));
        OnPropertyChanged(nameof(TotalsTotalFormatted));
        OnPropertyChanged(nameof(DefaultCurrencyCode));
    }

    public async Task LoadAsync()
    {
        IsLoadingOriginal = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var useCase = scope.ServiceProvider.GetRequiredService<GetInvoiceUseCase>();
            OriginalInvoice = await useCase.ExecuteAsync(new GetInvoiceQuery
            {
                BillingSource = BillingSource.Name,
                Secret = BillingSource.Secret,
                InvoiceNumber = OriginalInvoiceNumber,
            });
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoadingOriginal = false;
        }
    }

    [RelayCommand]
    void AddLine()
    {
        var line = new InvoiceLineItem { CurrencyCode = DefaultCurrencyCode };
        DifferenceLines.Add(line);
    }

    [RelayCommand]
    void RemoveLine(InvoiceLineItem line) => DifferenceLines.Remove(line);

    [RelayCommand]
    void CopyOriginalLine(InvoiceLineResult line)
    {
        DifferenceLines.Add(new InvoiceLineItem
        {
            Description  = line.Description,
            Quantity     = -line.Quantity,
            UnitPrice    = line.UnitPriceOrigin.Amount,
            TaxRate      = line.TaxRatePercentage,
            CurrencyCode = line.UnitPriceOrigin.CurrencyCode,
        });
    }

    [RelayCommand]
    async Task Save()
    {
        ErrorMessage = null;
        if (!Validate())
            return;

        IsSaving = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var useCase = scope.ServiceProvider.GetRequiredService<RectifyInvoiceUseCase>();

            var type = SelectedRectificationType == "Substitution"
                ? RectificationType.Substitution
                : RectificationType.Difference;

            await useCase.ExecuteAsync(new RectifyInvoiceCommand
            {
                BillingSource = BillingSource.Name,
                Secret = BillingSource.Secret,
                OriginalInvoiceNumber = OriginalInvoiceNumber,
                RectificativeSerie = RectificativeSerie.Trim().ToUpperInvariant(),
                Reason = Reason.Trim(),
                RectificationType = type,
                PaymentReference = PaymentReference.Trim(),
                PaymentMethod = string.IsNullOrWhiteSpace(PaymentMethod) ? null : PaymentMethod.Trim(),
                TransactionData = string.IsNullOrWhiteSpace(TransactionData) ? null : TransactionData.Trim(),
                Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
                Lines = type == RectificationType.Difference
                    ? DifferenceLines.Select(l => new InvoiceLineDto
                    {
                        Description = l.Description.Trim(),
                        Quantity = l.Quantity,
                        UnitPrice = l.UnitPrice,
                        TaxRatePercentage = l.TaxRate,
                        CurrencyCode = l.CurrencyCode,
                    }).ToList()
                    : null,
            });

            Success = true;
            await Task.Delay(1200);
            _onRectified();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    void Cancel() => _onCancel();

    private bool Validate()
    {
        if (string.IsNullOrWhiteSpace(RectificativeSerie))
        { ErrorMessage = "La serie rectificativa es obligatoria."; return false; }
        if (string.IsNullOrWhiteSpace(Reason) || Reason.Trim().Length < 10)
        { ErrorMessage = "El motivo debe tener al menos 10 caracteres."; return false; }
        if (string.IsNullOrWhiteSpace(PaymentReference))
        { ErrorMessage = "La referencia de pago es obligatoria."; return false; }
        if (IsDifference && DifferenceLines.Count == 0)
        { ErrorMessage = "Añade al menos una línea para una rectificación por diferencia."; return false; }
        return true;
    }
}
