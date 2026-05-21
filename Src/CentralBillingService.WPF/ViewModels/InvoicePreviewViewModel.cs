using DigitalDoor.Reporting.Entities.Interfaces;

namespace CentralBillingService.WPF.ViewModels;

public partial class InvoicePreviewViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Action _goBack;

    public string InvoiceNumber { get; }
    public string BillingSourceName { get; }

    [ObservableProperty] bool isLoading = true;
    [ObservableProperty] string? errorMessage;
    [ObservableProperty] string? pdfTempPath;

    public InvoicePreviewViewModel(
        IServiceScopeFactory scopeFactory,
        string invoiceNumber,
        string billingSourceName,
        Action goBack)
    {
        _scopeFactory = scopeFactory;
        InvoiceNumber = invoiceNumber;
        BillingSourceName = billingSourceName;
        _goBack = goBack;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        PdfTempPath = null;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var reportUseCase = scope.ServiceProvider.GetRequiredService<GenerateInvoiceReportUseCase>();
            var pdfGenerator = scope.ServiceProvider.GetRequiredService<IReportAsBytes>();

            var reportModel = await reportUseCase.GenerateInvoiceViewModel(
                new GenerateInvoiceReportCommand(InvoiceNumber, BillingSourceName),
                CancellationToken.None);

            var pdfBytes = await pdfGenerator.GenerateReport(reportModel);
            var tempPath = Path.Combine(Path.GetTempPath(), $"cbs_invoice_{Guid.NewGuid():N}.pdf");
            await File.WriteAllBytesAsync(tempPath, pdfBytes);
            PdfTempPath = tempPath;
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
}
