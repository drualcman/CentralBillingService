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
    private readonly IJobQueue _jobQueue;
    private readonly ILogger<GenerateInvoiceQrUseCase> _logger;

    public GenerateInvoiceQrUseCase(
        IQrCodeGenerator qrGenerator,
        IBlobStorageService blobStorage,
        IInvoiceVerificationUrlProvider urlProvider,
        IJobQueue jobQueue,
        ILogger<GenerateInvoiceQrUseCase> logger)
    {
        _qrGenerator = qrGenerator;
        _blobStorage = blobStorage;
        _urlProvider = urlProvider;
        _jobQueue = jobQueue;
        _logger = logger;
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

        await _blobStorage.UploadQrAsync(
            InvoiceHelper.GetQrFileName(command.BillingSource, command.InvoiceNumber), pngBytes, cancellationToken);
        await CreatePdf(command, cancellationToken);
    }

    private async Task CreatePdf(GenerateInvoiceQrCommand data, CancellationToken cancellationToken)
    {
        try
        {
            await _jobQueue.EnqueuePdfAsync(CreateCommand(data), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex.Message);
        }
    }

    private static GenerateInvoiceReportCommand CreateCommand(GenerateInvoiceQrCommand invoice) =>
        new(invoice.InvoiceNumber, invoice.BillingSource);
}
