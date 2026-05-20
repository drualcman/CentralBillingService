using CentralBillingService.Domain.Services;

namespace CentralBillingService.WPF.ViewModels;

public partial class BillingSummaryViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;

    public string[] AllBillingSources { get; }

    [ObservableProperty] string? selectedBillingSource;
    [ObservableProperty] InvoicesSummaryResult? summary;
    [ObservableProperty] bool isLoading;
    [ObservableProperty] string? errorMessage;

    partial void OnSelectedBillingSourceChanged(string? value) => _ = LoadAsync();

    public BillingSummaryViewModel(IServiceScopeFactory scopeFactory, BillingSourceRegistry registry)
    {
        _scopeFactory = scopeFactory;
        AllBillingSources = ["", .. registry.GetAll().Select(c => c.BillingSource).Order()];
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        Summary = null;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var useCase = scope.ServiceProvider.GetRequiredService<GetInvoicesSummaryUseCase>();
            Summary = await useCase.ExecuteAsync(
                string.IsNullOrEmpty(SelectedBillingSource) ? null : SelectedBillingSource);
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
}
