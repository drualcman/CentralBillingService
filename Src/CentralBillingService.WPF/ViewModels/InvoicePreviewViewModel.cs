using CentralBillingService.Domain.Services;
using System.Windows.Media.Imaging;

namespace CentralBillingService.WPF.ViewModels;

public partial class InvoicePreviewViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly BillingSourceRegistry _registry;
    private readonly Action _goBack;

    public string InvoiceNumber { get; }
    public string BillingSourceName { get; }

    [ObservableProperty] InvoiceResult? invoice;
    [ObservableProperty] bool isLoading = true;
    [ObservableProperty] string? errorMessage;
    [ObservableProperty] BitmapImage? logoImage;
    [ObservableProperty] BitmapImage? qrImage;

    public string ExchangeRateText => Invoice is null || Invoice.AppliedExchangeRate.IsIdentity ? "" :
        $"Tipo de cambio: 1 {Invoice.AppliedExchangeRate.FromCurrency} = {Invoice.AppliedExchangeRate.Rate:F4} EUR";

    public string IssuerAddress => Invoice is null ? "" :
        string.Join(", ", new[]
        {
            Invoice.Issuer.AddressLine1,
            Invoice.Issuer.AddressLine2,
            Invoice.Issuer.PostalCode,
            Invoice.Issuer.City,
            Invoice.Issuer.Province
        }.Where(s => !string.IsNullOrWhiteSpace(s)));

    public string RecipientAddress => Invoice is null ? "" :
        string.Join(", ", new[]
        {
            Invoice.Recipient.AddressLine1,
            Invoice.Recipient.AddressLine2,
            Invoice.Recipient.PostalCode,
            Invoice.Recipient.City,
            Invoice.Recipient.Province
        }.Where(s => !string.IsNullOrWhiteSpace(s)));

    public InvoicePreviewViewModel(
        IServiceScopeFactory scopeFactory,
        BillingSourceRegistry registry,
        string invoiceNumber,
        string billingSourceName,
        Action goBack)
    {
        _scopeFactory = scopeFactory;
        _registry = registry;
        InvoiceNumber = invoiceNumber;
        BillingSourceName = billingSourceName;
        _goBack = goBack;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var config = _registry.GetConfig(BillingSourceName);

            using var scope = _scopeFactory.CreateScope();
            var useCase = scope.ServiceProvider.GetRequiredService<GetInvoiceUseCase>();
            Invoice = await useCase.ExecuteAsync(new GetInvoiceQuery
            {
                BillingSource = BillingSourceName,
                InvoiceNumber = InvoiceNumber,
                Secret = config.Secret,
            });

            OnPropertyChanged(nameof(IssuerAddress));
            OnPropertyChanged(nameof(RecipientAddress));
            OnPropertyChanged(nameof(ExchangeRateText));

            if (!string.IsNullOrEmpty(config.Issuer.LogoUrl))
                LogoImage = await LoadImageAsync(config.Issuer.LogoUrl);

            if (!string.IsNullOrEmpty(Invoice?.QrCodeBlobUrl))
                QrImage = await LoadImageAsync(Invoice.QrCodeBlobUrl!);
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

    private static async Task<BitmapImage?> LoadImageAsync(string url)
    {
        try
        {
            using var client = new System.Net.Http.HttpClient();
            var bytes = await client.GetByteArrayAsync(url);
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.StreamSource = new System.IO.MemoryStream(bytes);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }
        catch
        {
            return null;
        }
    }
}
