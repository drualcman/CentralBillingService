namespace CentralBillingService.WPF.ViewModels;

/// <summary>
/// Rectifies an issued invoice (or rectificative) in one of two ways:
///  - Substitution: the lines start as a copy of the original and are edited into the corrected invoice.
///  - Difference: only the signed delta — cancel a whole line or n units of it (negative quantity)
///    and/or add new positive lines.
/// </summary>
public partial class RectifyInvoiceViewModel : ObservableObject
{
    public const string SubstitutionType = "Substitution";
    public const string DifferenceType = "Difference";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Action _onRectified;
    private readonly Action _onCancel;

    public BillingSourceSummary BillingSource { get; }
    public string OriginalInvoiceNumber { get; }
    public InvoiceLinesEditor LinesEditor { get; }

    [ObservableProperty] ObservableCollection<SeriesRecord>  availableSeries   = [];
    [ObservableProperty] ObservableCollection<ProductRecord> availableProducts = [];
    [ObservableProperty] ObservableCollection<NoteRecord>    availableNotes    = [];
    [ObservableProperty] NoteRecord? selectedNoteTemplate;

    [ObservableProperty] string rectificativeSerie = string.Empty;
    [ObservableProperty] DateTime issueDate = DateTime.Today;
    [ObservableProperty] string reason = string.Empty;
    [ObservableProperty] string selectedRectificationType = SubstitutionType;
    [ObservableProperty] string paymentReference = string.Empty;
    [ObservableProperty] string? paymentMethod;
    [ObservableProperty] string? transactionData;
    [ObservableProperty] string? notes;
    [ObservableProperty] bool isSaving;
    [ObservableProperty] string? errorMessage;
    [ObservableProperty] bool success;
    [ObservableProperty] InvoiceResult? originalInvoice;
    [ObservableProperty] bool isLoadingOriginal;

    public static string[] RectificationTypes { get; } = [SubstitutionType, DifferenceType];
    public bool IsDifference => SelectedRectificationType == DifferenceType;
    public bool IsSubstitution => !IsDifference;

    public string LinesSectionTitle => IsDifference
        ? "LÍNEAS DE DIFERENCIA — cantidad negativa para anular (toda o parte de) una línea, positiva para añadir"
        : "FACTURA CORREGIDA — edita las líneas tal y como deberían haber quedado";

    public string DefaultCurrencyCode => OriginalInvoice?.AppliedExchangeRate.FromCurrency ?? "EUR";

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
        LinesEditor = new InvoiceLinesEditor(scopeFactory, () => DefaultCurrencyCode);

        AvailableSeries   = new ObservableCollection<SeriesRecord>(RectificativeSeriesFrom(masterDataStore.LoadSeries()));
        AvailableProducts = new ObservableCollection<ProductRecord>(masterDataStore.LoadProducts());
        AvailableNotes    = new ObservableCollection<NoteRecord>(masterDataStore.LoadNotes());
    }

    /// <summary>Series flagged as rectificative; all of them while none is flagged yet.</summary>
    private static List<SeriesRecord> RectificativeSeriesFrom(List<SeriesRecord> allSeries)
    {
        List<SeriesRecord> rectificativeSeries = allSeries.Where(serie => serie.IsRectificative).ToList();
        return rectificativeSeries.Count > 0 ? rectificativeSeries : allSeries;
    }

    partial void OnSelectedRectificationTypeChanged(string value)
    {
        OnPropertyChanged(nameof(IsDifference));
        OnPropertyChanged(nameof(IsSubstitution));
        OnPropertyChanged(nameof(LinesSectionTitle));
        ResetLinesForSelectedType();
    }

    partial void OnOriginalInvoiceChanged(InvoiceResult? value) => ResetLinesForSelectedType();

    private void ResetLinesForSelectedType()
    {
        List<InvoiceLineItem> startingLines = IsSubstitution && OriginalInvoice is not null
            ? OriginalInvoice.Lines.Select(line => ToEditableLine(line, line.Quantity)).ToList()
            : [];
        LinesEditor.ReplaceLines(startingLines);
    }

    private static InvoiceLineItem ToEditableLine(InvoiceLineResult line, int quantity) => new InvoiceLineItem
    {
        Description  = line.Description,
        Quantity     = quantity,
        UnitPrice    = line.UnitPriceOrigin.Amount,
        TaxRate      = line.TaxRatePercentage,
        CurrencyCode = line.UnitPriceOrigin.CurrencyCode,
        ProductType  = line.ProductType,
    };

    public async Task LoadAsync()
    {
        IsLoadingOriginal = true;
        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            GetInvoiceUseCase useCase = scope.ServiceProvider.GetRequiredService<GetInvoiceUseCase>();
            OriginalInvoice = await useCase.ExecuteAsync(new GetInvoiceQuery
            {
                BillingSource = BillingSource.Name,
                Secret = BillingSource.Secret,
                InvoiceNumber = OriginalInvoiceNumber,
            });
        }
        catch (Exception exception)
        {
            ErrorMessage = RectifyInvoiceFormValidator.GetDeepMessage(exception);
        }
        finally
        {
            IsLoadingOriginal = false;
        }
    }

    /// <summary>Difference: adds the original line negated in full; lower the quantity to cancel only part of it.</summary>
    [RelayCommand]
    void CancelOriginalLine(InvoiceLineResult line) => LinesEditor.Lines.Add(ToEditableLine(line, -line.Quantity));

    [RelayCommand]
    void RestoreOriginalLines() => ResetLinesForSelectedType();

    [RelayCommand]
    async Task Save()
    {
        ErrorMessage = RectifyInvoiceFormValidator.Validate(this);
        if (ErrorMessage is null)
        {
            IsSaving = true;
            try
            {
                using IServiceScope scope = _scopeFactory.CreateScope();
                RectifyInvoiceUseCase useCase = scope.ServiceProvider.GetRequiredService<RectifyInvoiceUseCase>();
                await useCase.ExecuteAsync(BuildCommand());

                Success = true;
                await Task.Delay(1200);
                _onRectified();
            }
            catch (Exception exception)
            {
                ErrorMessage = RectifyInvoiceFormValidator.GetDeepMessage(exception);
            }
            finally
            {
                IsSaving = false;
            }
        }
    }

    private RectifyInvoiceCommand BuildCommand() => new RectifyInvoiceCommand
    {
        BillingSource = BillingSource.Name,
        Secret = BillingSource.Secret,
        OriginalInvoiceNumber = OriginalInvoiceNumber,
        RectificativeSerie = RectificativeSerie.Trim().ToUpperInvariant(),
        IssueDate = DateOnly.FromDateTime(IssueDate),
        Reason = Reason.Trim(),
        RectificationType = IsDifference ? RectificationType.Difference : RectificationType.Substitution,
        PaymentReference = PaymentReference.Trim(),
        PaymentMethod = string.IsNullOrWhiteSpace(PaymentMethod) ? null : PaymentMethod.Trim(),
        TransactionData = string.IsNullOrWhiteSpace(TransactionData) ? null : TransactionData.Trim(),
        Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
        Lines = LinesEditor.Lines.Select(line => new InvoiceLineDto
        {
            Description = line.Description.Trim(),
            Quantity = line.Quantity,
            UnitPrice = line.UnitPrice,
            TaxRatePercentage = line.TaxRate,
            CurrencyCode = line.CurrencyCode,
            ProductType = line.ProductType,
        }).ToList(),
    };

    partial void OnSelectedNoteTemplateChanged(NoteRecord? value)
    {
        if (value is not null)
        {
            Notes = value.Content;
            SelectedNoteTemplate = null;
        }
    }

    [RelayCommand]
    void Cancel() => _onCancel();
}
