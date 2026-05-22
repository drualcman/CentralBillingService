using CentralBillingService.Domain.Interfaces;
using CentralBillingService.Domain.ValueObjects;
using CentralBillingService.WPF.Services;

namespace CentralBillingService.WPF.ViewModels;

public partial class CreateInvoiceViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly LocalMasterDataStore _masterDataStore;
    private readonly Action _onCreated;
    private readonly Action _onCancel;

    public BillingSourceSummary BillingSource { get; }

    // Master data
    [ObservableProperty] ObservableCollection<ClientRecord>  availableClients  = [];
    [ObservableProperty] ObservableCollection<SeriesRecord>  availableSeries   = [];
    [ObservableProperty] ObservableCollection<ProductRecord> availableProducts = [];
    [ObservableProperty] ObservableCollection<NoteRecord>    availableNotes    = [];
    [ObservableProperty] NoteRecord? selectedNoteTemplate;
    [ObservableProperty] ClientRecord? selectedClient;

    // Invoice header
    [ObservableProperty] string serie = "";
    [ObservableProperty] string? invoiceNumberClientPrefix;
    [ObservableProperty] string? invoiceNumberClientSuffix;
    [ObservableProperty] DateTime issueDate = DateTime.Today;
    [ObservableProperty] DateTime? valueDate;
    [ObservableProperty] string? notes;
    [ObservableProperty] string paymentMethod = "TRANSFER";
    [ObservableProperty] string paymentReference = string.Empty;
    [ObservableProperty] string? transactionData;

    // Recipient
    [ObservableProperty] string recipientLegalName = string.Empty;
    [ObservableProperty] string? recipientTradeName;
    [ObservableProperty] string recipientTaxId = string.Empty;
    [ObservableProperty] string recipientTaxIdCountry = "ES";
    [ObservableProperty] string recipientEmail = string.Empty;
    [ObservableProperty] string? recipientPhone;
    [ObservableProperty] string recipientAddress = string.Empty;
    [ObservableProperty] string recipientCity = string.Empty;
    [ObservableProperty] string? recipientProvince;
    [ObservableProperty] string recipientPostalCode = string.Empty;
    [ObservableProperty] string recipientCountry = "ES";
    [ObservableProperty] string? recipientExternalId;

    // Lines
    [ObservableProperty] ObservableCollection<InvoiceLineItem> lines = [new InvoiceLineItem()];

    // State
    [ObservableProperty] bool isSaving;
    [ObservableProperty] string? errorMessage;
    [ObservableProperty] bool success;

    public static string[] PaymentMethods { get; } =
        ["TRANSFER", "CARD", "CASH", "PAYPAL", "CRYPTO", "OTHER"];

    [ObservableProperty] string? saveToMasterMessage;

    // Live totals (raw sums of unit prices × quantities; actual EUR amounts computed by backend)
    public decimal TotalsSubtotal => Lines.Sum(l => l.Quantity * l.UnitPrice);
    public decimal TotalsTax      => Lines.Sum(l => l.Quantity * l.UnitPrice * l.TaxRate / 100m);
    public decimal TotalsTotal    => TotalsSubtotal + TotalsTax;

    public string TotalsSubtotalFormatted => $"{TotalsSubtotal:N2}";
    public string TotalsTaxFormatted      => $"{TotalsTax:N2}";
    public string TotalsTotalFormatted    => $"{TotalsTotal:N2}";

    public CreateInvoiceViewModel(
        IServiceScopeFactory scopeFactory,
        BillingSourceSummary billingSource,
        LocalMasterDataStore masterDataStore,
        Action onCreated,
        Action onCancel)
    {
        _scopeFactory = scopeFactory;
        _masterDataStore = masterDataStore;
        _onCreated = onCreated;
        _onCancel = onCancel;
        BillingSource = billingSource;

        AvailableClients  = new ObservableCollection<ClientRecord>(masterDataStore.LoadClients());
        AvailableSeries   = new ObservableCollection<SeriesRecord>(masterDataStore.LoadSeries());
        AvailableProducts = new ObservableCollection<ProductRecord>(masterDataStore.LoadProducts());
        AvailableNotes    = new ObservableCollection<NoteRecord>(masterDataStore.LoadNotes());

        Lines.CollectionChanged += OnLinesCollectionChanged;
        foreach (var line in Lines) line.PropertyChanged += OnLineChanged;
    }

    private void OnLinesCollectionChanged(object? sender,
        System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
            foreach (InvoiceLineItem item in e.NewItems) item.PropertyChanged += OnLineChanged;
        if (e.OldItems is not null)
            foreach (InvoiceLineItem item in e.OldItems) item.PropertyChanged -= OnLineChanged;
        RefreshTotals();
    }

    private void OnLineChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        RefreshTotals();
        if (sender is InvoiceLineItem line && e.PropertyName == nameof(InvoiceLineItem.CurrencyCode))
        {
            _ = FetchRateHintAsync(line);
            if (line.CurrencyCode != "EUR")
                line.TaxRate = 0;
        }
    }

    partial void OnRecipientCountryChanged(string value)
    {
        if (value.Trim().ToUpperInvariant() != "ES")
            foreach (var line in Lines)
                line.TaxRate = 0;
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

    private void RefreshTotals()
    {
        OnPropertyChanged(nameof(TotalsSubtotal));
        OnPropertyChanged(nameof(TotalsTax));
        OnPropertyChanged(nameof(TotalsTotal));
        OnPropertyChanged(nameof(TotalsSubtotalFormatted));
        OnPropertyChanged(nameof(TotalsTaxFormatted));
        OnPropertyChanged(nameof(TotalsTotalFormatted));
    }

    partial void OnSelectedClientChanged(ClientRecord? value)
    {
        if (value is null) return;
        RecipientLegalName    = value.LegalName;
        RecipientTradeName    = value.TradeName;
        RecipientTaxId        = value.TaxId;
        RecipientTaxIdCountry = value.TaxIdCountry;
        RecipientEmail        = value.Email ?? "";
        RecipientPhone        = value.Phone;
        RecipientAddress      = value.Address ?? "";
        RecipientCity         = value.City ?? "";
        RecipientProvince     = value.Province;
        RecipientPostalCode   = value.PostalCode ?? "";
        RecipientCountry      = value.Country ?? "ES";
        RecipientExternalId   = value.ExternalId;
    }

    [RelayCommand]
    void AddLine() => Lines.Add(new InvoiceLineItem());

    [RelayCommand]
    void RemoveLine(InvoiceLineItem line) => Lines.Remove(line);

    [RelayCommand]
    async Task Save()
    {
        ErrorMessage = null;
        if (!Validate()) return;

        IsSaving = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var useCase = scope.ServiceProvider.GetRequiredService<CreateInvoiceUseCase>();

            await useCase.ExecuteAsync(new CreateInvoiceCommand
            {
                BillingSource = BillingSource.Name,
                Secret = BillingSource.Secret,
                Serie = Serie.Trim().ToUpperInvariant(),
                InvoiceNumberClientPrefix = string.IsNullOrWhiteSpace(InvoiceNumberClientPrefix) ? null : InvoiceNumberClientPrefix.Trim(),
                InvoiceNumberClientSuffix = string.IsNullOrWhiteSpace(InvoiceNumberClientSuffix) ? null : InvoiceNumberClientSuffix.Trim(),
                OriginCurrencyCode = null, // per-line currencies are used instead
                IssueDate = DateOnly.FromDateTime(IssueDate),
                ValueDate = ValueDate.HasValue ? DateOnly.FromDateTime(ValueDate.Value) : null,
                Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
                PaymentMethod = PaymentMethod,
                PaymentReference = PaymentReference.Trim(),
                TransactionData = string.IsNullOrWhiteSpace(TransactionData) ? null : TransactionData.Trim(),
                Recipient = new RecipientDto
                {
                    LegalName = RecipientLegalName.Trim(),
                    TradeName = string.IsNullOrWhiteSpace(RecipientTradeName) ? null : RecipientTradeName.Trim(),
                    TaxIdValue = RecipientTaxId.Trim().ToUpperInvariant(),
                    TaxIdCountryCode = RecipientTaxIdCountry.Trim().ToUpperInvariant(),
                    Email = RecipientEmail.Trim().ToLowerInvariant(),
                    Phone = string.IsNullOrWhiteSpace(RecipientPhone) ? null : RecipientPhone.Trim(),
                    AddressLine1 = RecipientAddress.Trim(),
                    City = RecipientCity.Trim(),
                    Province = string.IsNullOrWhiteSpace(RecipientProvince) ? null : RecipientProvince.Trim(),
                    PostalCode = RecipientPostalCode.Trim(),
                    AddressCountryCode = RecipientCountry.Trim().ToUpperInvariant(),
                    ExternalId = string.IsNullOrWhiteSpace(RecipientExternalId) ? null : RecipientExternalId.Trim(),
                },
                Lines = Lines.Select(l => new InvoiceLineDto
                {
                    Description = l.Description.Trim(),
                    Quantity = l.Quantity,
                    UnitPrice = l.UnitPrice,
                    TaxRatePercentage = l.TaxRate,
                    CurrencyCode = l.CurrencyCode,
                }).ToList(),
            });

            Success = true;
            await Task.Delay(1200);
            _onCreated();
        }
        catch (Exception ex)
        {
            ErrorMessage = GetDeepMessage(ex);
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    void SaveClientToMaster()
    {
        if (string.IsNullOrWhiteSpace(RecipientLegalName) || string.IsNullOrWhiteSpace(RecipientTaxId))
        { SaveToMasterMessage = "Completa al menos nombre fiscal y NIF antes de guardar."; return; }

        var clients = _masterDataStore.LoadClients();
        var existing = clients.FirstOrDefault(c =>
            c.TaxId.Equals(RecipientTaxId.Trim().ToUpperInvariant(), StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            existing.LegalName   = RecipientLegalName.Trim();
            existing.TradeName   = string.IsNullOrWhiteSpace(RecipientTradeName) ? null : RecipientTradeName.Trim();
            existing.TaxIdCountry = RecipientTaxIdCountry.Trim().ToUpperInvariant();
            existing.Email       = string.IsNullOrWhiteSpace(RecipientEmail) ? null : RecipientEmail.Trim();
            existing.Phone       = string.IsNullOrWhiteSpace(RecipientPhone) ? null : RecipientPhone.Trim();
            existing.Address     = string.IsNullOrWhiteSpace(RecipientAddress) ? null : RecipientAddress.Trim();
            existing.City        = string.IsNullOrWhiteSpace(RecipientCity) ? null : RecipientCity.Trim();
            existing.PostalCode  = string.IsNullOrWhiteSpace(RecipientPostalCode) ? null : RecipientPostalCode.Trim();
            existing.Country     = string.IsNullOrWhiteSpace(RecipientCountry) ? null : RecipientCountry.Trim().ToUpperInvariant();
            existing.ExternalId  = string.IsNullOrWhiteSpace(RecipientExternalId) ? null : RecipientExternalId.Trim();
            SaveToMasterMessage = $"Cliente '{existing.DisplayName}' actualizado en el maestro.";
        }
        else
        {
            var newClient = new ClientRecord
            {
                LegalName   = RecipientLegalName.Trim(),
                TradeName   = string.IsNullOrWhiteSpace(RecipientTradeName) ? null : RecipientTradeName.Trim(),
                TaxId        = RecipientTaxId.Trim().ToUpperInvariant(),
                TaxIdCountry = RecipientTaxIdCountry.Trim().ToUpperInvariant(),
                Email        = string.IsNullOrWhiteSpace(RecipientEmail) ? null : RecipientEmail.Trim(),
                Phone        = string.IsNullOrWhiteSpace(RecipientPhone) ? null : RecipientPhone.Trim(),
                Address      = string.IsNullOrWhiteSpace(RecipientAddress) ? null : RecipientAddress.Trim(),
                City         = string.IsNullOrWhiteSpace(RecipientCity) ? null : RecipientCity.Trim(),
                PostalCode   = string.IsNullOrWhiteSpace(RecipientPostalCode) ? null : RecipientPostalCode.Trim(),
                Country      = string.IsNullOrWhiteSpace(RecipientCountry) ? null : RecipientCountry.Trim().ToUpperInvariant(),
                ExternalId   = string.IsNullOrWhiteSpace(RecipientExternalId) ? null : RecipientExternalId.Trim(),
            };
            clients.Add(newClient);
            AvailableClients.Add(newClient);
            SaveToMasterMessage = $"Cliente '{newClient.DisplayName}' guardado en el maestro.";
        }

        _masterDataStore.SaveClients(clients);
    }

    [RelayCommand]
    void SaveProductToMaster(InvoiceLineItem line)
    {
        if (string.IsNullOrWhiteSpace(line.Description))
        { SaveToMasterMessage = "La línea no tiene descripción."; return; }

        var products = _masterDataStore.LoadProducts();
        var existing = products.FirstOrDefault(p =>
            p.Description.Equals(line.Description.Trim(), StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            existing.DefaultUnitPrice = line.UnitPrice;
            existing.DefaultTaxRate   = line.TaxRate;
            SaveToMasterMessage = $"Artículo '{existing.Description}' actualizado en el maestro.";
        }
        else
        {
            var newProduct = new ProductRecord
            {
                Description      = line.Description.Trim(),
                DefaultUnitPrice = line.UnitPrice,
                DefaultTaxRate   = line.TaxRate,
            };
            products.Add(newProduct);
            AvailableProducts.Add(newProduct);
            SaveToMasterMessage = $"Artículo '{newProduct.Description}' guardado en el maestro.";
        }

        _masterDataStore.SaveProducts(products);
    }

    partial void OnSelectedNoteTemplateChanged(NoteRecord? value)
    {
        if (value is null) return;
        Notes = value.Content;
        SelectedNoteTemplate = null;
    }

    [RelayCommand]
    void Cancel() => _onCancel();

    private static string GetDeepMessage(Exception ex)
    {
        var inner = ex;
        while (inner.InnerException != null) inner = inner.InnerException;
        return inner == ex ? ex.Message : $"{ex.Message}\n→ {inner.Message}";
    }

    private bool Validate()
    {
        if (string.IsNullOrWhiteSpace(Serie))
        { ErrorMessage = "La serie es obligatoria."; return false; }
        if (string.IsNullOrWhiteSpace(RecipientLegalName))
        { ErrorMessage = "El nombre fiscal del destinatario es obligatorio."; return false; }
        if (string.IsNullOrWhiteSpace(RecipientTaxId))
        { ErrorMessage = "El NIF del destinatario es obligatorio."; return false; }
        if (string.IsNullOrWhiteSpace(RecipientEmail))
        { ErrorMessage = "El email del destinatario es obligatorio."; return false; }
        if (string.IsNullOrWhiteSpace(RecipientAddress))
        { ErrorMessage = "La dirección del destinatario es obligatoria."; return false; }
        if (string.IsNullOrWhiteSpace(PaymentReference))
        { ErrorMessage = "La referencia de pago es obligatoria."; return false; }
        if (Lines.Count == 0)
        { ErrorMessage = "Añade al menos una línea."; return false; }
        if (Lines.Any(l => string.IsNullOrWhiteSpace(l.Description)))
        { ErrorMessage = "Todas las líneas deben tener descripción."; return false; }
        if (Lines.Any(l => l.Quantity == 0))
        { ErrorMessage = "La cantidad de cada línea no puede ser cero."; return false; }
        return true;
    }
}
