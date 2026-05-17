namespace CentralBillingService.Application.UseCases;

/// <summary>
/// Generates a QR code PNG for an invoice and uploads it to blob storage.
/// Triggered asynchronously via a queue message after the invoice has been persisted.
/// No database write is needed — the blob URL was already stored deterministically
/// at invoice creation time.
/// </summary>
public sealed class GenerateInvoiceQrUseCase
{
    private readonly IQrCodeGenerator _qrGenerator;
    private readonly IBlobStorageService _blobStorage;
    private readonly IInvoiceVerificationUrlProvider _urlProvider;

    public GenerateInvoiceQrUseCase(
        IQrCodeGenerator qrGenerator,
        IBlobStorageService blobStorage,
        IInvoiceVerificationUrlProvider urlProvider)
    {
        _qrGenerator = qrGenerator;
        _blobStorage = blobStorage;
        _urlProvider = urlProvider;
    }

    public async Task ExecuteAsync(GenerateInvoiceQrCommand command, CancellationToken cancellationToken = default)
    {
        var verificationUrl = _urlProvider.GetVerificationUrl(
            command.BillingSource,
            command.InvoiceNumber,
            command.Hash,
            command.IssueDate,
            command.TotalEurAmount,
            command.RecipientTaxId);

        var pngBytes = await _qrGenerator.GenerateAsync(verificationUrl, cancellationToken);

        var blobName = $"qr/{command.BillingSource}/{command.InvoiceNumber}.png";
        await _blobStorage.UploadAsync(blobName, pngBytes, "image/png", cancellationToken);
    }
}
