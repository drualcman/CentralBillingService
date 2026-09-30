namespace CentralBillingService.AzureFunction.API;

/// <summary>
/// Queue-triggered function that submits an already-created, stamped invoice to its
/// billing source's fiscal authority (e.g. AEAT VeriFactu). Authority-agnostic — the
/// concrete <see cref="IFiscalRegistrar"/> selected per billing source owns the actual
/// reporting. Transient failures are retried by the Azure Functions runtime.
/// </summary>
public sealed class ProcessFiscalSubmissionFunction
{
    private readonly SubmitFiscalRecordUseCase _useCase;
    private readonly ILogger<ProcessFiscalSubmissionFunction> _logger;

    public ProcessFiscalSubmissionFunction(
        SubmitFiscalRecordUseCase useCase,
        ILogger<ProcessFiscalSubmissionFunction> logger)
    {
        _useCase = useCase;
        _logger = logger;
    }

    [Function(nameof(ProcessFiscalSubmissionFunction))]
    public async Task Run(
        [QueueTrigger("%FiscalSubmissionQueueName%", Connection = "InvoiceCreateQueueStorage")]
        string message,
        CancellationToken cancellationToken)
    {
        SubmitFiscalRecordCommand command;

        try
        {
            var deserialized = QueueMessageSerializer.Deserialize<SubmitFiscalRecordCommand>(message, JsonOptions.Default);
            command = deserialized ?? throw new JsonException("Message deserialized to null.");
        }
        catch (Exception ex)
        {
            // Malformed message — retrying will never succeed, discard it.
            _logger.LogError(ex,
                "Poison fiscal-submission message: could not deserialize SubmitFiscalRecordCommand. Message: {Message}", message);
            return;
        }

        try
        {
            var outcome = await _useCase.ExecuteAsync(command, cancellationToken);

            _logger.LogInformation(
                "Fiscal submission for invoice {InvoiceNumber} (billing source {BillingSource}) finished with state {State}.",
                command.InvoiceNumber, command.BillingSource, outcome.State);
        }
        catch (Exception ex)
        {
            // Transient failure (authority unavailable, network error, etc.) — rethrow so Azure
            // Functions retries with backoff and eventually routes to the poison queue.
            _logger.LogError(ex,
                "Error submitting invoice {InvoiceNumber} to the fiscal authority. Message will be retried.",
                command.InvoiceNumber);
            throw;
        }
    }
}
