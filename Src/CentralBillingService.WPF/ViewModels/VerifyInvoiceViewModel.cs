namespace CentralBillingService.WPF.ViewModels;

public partial class VerifyInvoiceViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Action _onBack;

    public BillingSourceSummary BillingSource { get; }
    public string InvoiceNumber { get; }

    [ObservableProperty] CheckIntegrityResult? result;
    [ObservableProperty] bool isLoading;
    [ObservableProperty] string? errorMessage;

    public VerifyInvoiceViewModel(
        IServiceScopeFactory scopeFactory,
        BillingSourceSummary billingSource,
        string invoiceNumber,
        Action onBack)
    {
        _scopeFactory = scopeFactory;
        _onBack = onBack;
        BillingSource = billingSource;
        InvoiceNumber = invoiceNumber;
    }

    public async Task VerifyAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        Result = null;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var useCase = scope.ServiceProvider.GetRequiredService<CheckInvoiceIntegrityUseCase>();

            Result = await useCase.ExecuteAsync(new CheckIntegrityQuery
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
    void GoBack() => _onBack();

    [RelayCommand]
    async Task Retry() => await VerifyAsync();
}
