namespace CentralBillingService.AzureFunction.API;

/// <summary>
/// Queue-triggered function that generates a QR code PNG for an invoice
/// and uploads it to blob storage.
///
/// The invoice's QrCodeBlobUrl was already stored at creation time using the
/// deterministic blob name — this function just materialises the image.
/// Transient failures are retried automatically by the Azure Functions runtime.
/// </summary>
public sealed class GenerateInvoiceQrFunction
{
    private readonly GenerateInvoiceQrUseCase _useCase;
    private readonly ILogger<GenerateInvoiceQrFunction> _logger;

    public GenerateInvoiceQrFunction(
        GenerateInvoiceQrUseCase useCase,
        ILogger<GenerateInvoiceQrFunction> logger)
    {
        _useCase = useCase;
        _logger = logger;
    }

    [Function(nameof(GenerateInvoiceQrFunction))]
    public async Task Run(
        [QueueTrigger("%QrCodeQueueName%", Connection = "InvoiceCreateQueueStorage")]
        string message,
        CancellationToken cancellationToken)
    {
        GenerateInvoiceQrCommand command;

        try
        {
            var deserialized = JsonSerializer.Deserialize<GenerateInvoiceQrCommand>(message, JsonOptions.Default);
            command = deserialized ?? throw new JsonException("Message deserialized to null.");
        }
        catch (Exception ex)
        {
            // Malformed message — retrying will never succeed, discard it.
            _logger.LogError(ex,
                "Poison QR message: could not deserialize GenerateInvoiceQrCommand. Message: {Message}", message);
            return;
        }

        try
        {
            await _useCase.ExecuteAsync(command, cancellationToken);

            _logger.LogInformation(
                "QR code generated for invoice {InvoiceNumber} (billing source {BillingSource}).",
                command.InvoiceNumber, command.BillingSource);
        }
        catch (Exception ex)
        {
            // Transient failure — rethrow so Azure Functions retries with backoff
            // and routes to the poison queue after max retries.
            _logger.LogError(ex,
                "Error generating QR for invoice {InvoiceNumber}. Message will be retried.",
                command.InvoiceNumber);
            throw;
        }
    }
}
