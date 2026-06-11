namespace CentralBillingService.AzureFunction.API;

public sealed class ProcessQueuedCreateInvoiceFunction
{
    private readonly ProcessQueuedCreateInvoiceUseCase _useCase;
    private readonly ILogger<ProcessQueuedCreateInvoiceFunction> _logger;

    public ProcessQueuedCreateInvoiceFunction(
        ProcessQueuedCreateInvoiceUseCase useCase,
        ILogger<ProcessQueuedCreateInvoiceFunction> logger)
    {
        _useCase = useCase;
        _logger = logger;
    }

    [Function(nameof(ProcessQueuedCreateInvoiceFunction))]
    public async Task Run(
        [QueueTrigger("%CreateInvoiceQueueName%", Connection = "InvoiceCreateQueueStorage")]
        string message,
        CancellationToken cancellationToken)
    {
        CreateInvoiceCommand command;

        try
        {
            var deserialized = QueueMessageSerializer.Deserialize<CreateInvoiceCommand>(message, JsonOptions.Default);
            command = deserialized ?? throw new JsonException("Message deserialized to null.");
        }
        catch (Exception ex)
        {
            // Malformed message — retrying will never succeed, do not rethrow.
            _logger.LogError(ex,
                "Poison message received: could not deserialize CreateInvoiceCommand. Message: {Message}", message);
            return;
        }

        try
        {
            var result = await _useCase.ExecuteAsync(command, cancellationToken);

            _logger.LogInformation(
                "Invoice {InvoiceNumber} created from queue for billing source {BillingSource}.",
                result.InvoiceNumber, result.BillingSource);
        }
        catch (ArgumentException ex)
        {
            // Invalid command data — structural problem in the message, retrying will never succeed.
            _logger.LogError(ex,
                "Validation failure processing queued invoice for billing source {BillingSource}. Message discarded.",
                command.BillingSource);
        }
        catch (Exception ex)
        {
            // Transient failure (DB unavailable, network error, etc.) — rethrow so Azure
            // Functions retries with backoff and eventually routes to the poison queue.
            _logger.LogError(ex,
                "Transient error processing queued invoice for billing source {BillingSource}. Message will be retried.",
                command.BillingSource);
            throw;
        }
    }
}
