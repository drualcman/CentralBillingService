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
    private readonly IIso9001 _iso9001;

    public GenerateInvoiceQrUseCase(
        IQrCodeGenerator qrGenerator,
        IBlobStorageService blobStorage,
        IInvoiceVerificationUrlProvider urlProvider,
        IJobQueue jobQueue,
        ILogger<GenerateInvoiceQrUseCase> logger,
        IIso9001 iso9001)
    {
        _qrGenerator = qrGenerator;
        _blobStorage = blobStorage;
        _urlProvider = urlProvider;
        _jobQueue = jobQueue;
        _logger = logger;
        _iso9001 = iso9001;
    }

    public async Task ExecuteAsync(GenerateInvoiceQrCommand command, CancellationToken cancellationToken = default)
    {
        await _iso9001.Register(command.InvoiceNumber, this, "Generating QR code", command);

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

        await _iso9001.Register(command.InvoiceNumber, this, "QR code generated and uploaded");

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
            await _iso9001.Error(data.InvoiceNumber, this, ex);
        }
    }

    private static GenerateInvoiceReportCommand CreateCommand(GenerateInvoiceQrCommand invoice) =>
        new(invoice.InvoiceNumber, invoice.BillingSource);
}
