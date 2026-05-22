namespace CentralBillingService.WPF.ViewModels;

public partial class InvoiceDetailViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Action _goBack;

    public BillingSourceSummary BillingSource { get; }
    public string InvoiceNumber { get; }

    [ObservableProperty] InvoiceResult? invoice;
    [ObservableProperty] bool isLoading;
    [ObservableProperty] string? errorMessage;
    [ObservableProperty] string? emailSuccessMessage;
    [ObservableProperty] string? emailErrorMessage;

    public InvoiceDetailViewModel(
        IServiceScopeFactory scopeFactory,
        BillingSourceSummary billingSource,
        string invoiceNumber,
        Action goBack)
    {
        _scopeFactory = scopeFactory;
        _goBack = goBack;
        BillingSource = billingSource;
        InvoiceNumber = invoiceNumber;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var useCase = scope.ServiceProvider.GetRequiredService<GetInvoiceUseCase>();

            Invoice = await useCase.ExecuteAsync(new GetInvoiceQuery
            {
                BillingSource = BillingSource.Name,
                Secret = BillingSource.Secret,
                InvoiceNumber = InvoiceNumber,
            });
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    void GoBack() => _goBack();

    [RelayCommand]
    async Task SendPdfByEmail()
    {
        EmailSuccessMessage = null;
        EmailErrorMessage = null;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var useCase = scope.ServiceProvider.GetRequiredService<SendInvoicePdfByEmailUseCase>();

            var result = await useCase.ExecuteAsync(new SendInvoicePdfByEmailQuery
            {
                BillingSource = BillingSource.Name,
                InvoiceNumber = InvoiceNumber,
            });

            if (result.Success)
                EmailSuccessMessage = result.Message;
            else
                EmailErrorMessage = result.Message;
        }
        catch (Exception ex)
        {
            EmailErrorMessage = ex.Message;
        }
    }
}
