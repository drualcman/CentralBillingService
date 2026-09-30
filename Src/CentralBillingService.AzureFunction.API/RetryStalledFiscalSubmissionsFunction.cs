namespace CentralBillingService.AzureFunction.API;

/// <summary>
/// Timer-triggered safety net for fiscal submissions (e.g. AEAT VeriFactu): every 15 minutes
/// re-enqueues the records still pending whose queue message was lost or ended in the poison
/// queue, so no stamped invoice is left unreported.
/// </summary>
public sealed class RetryStalledFiscalSubmissionsFunction
{
    private const string EveryFifteenMinutes = "0 */15 * * * *";

    private readonly RetryStalledFiscalSubmissionsUseCase _useCase;
    private readonly ILogger<RetryStalledFiscalSubmissionsFunction> _logger;

    public RetryStalledFiscalSubmissionsFunction(
        RetryStalledFiscalSubmissionsUseCase useCase,
        ILogger<RetryStalledFiscalSubmissionsFunction> logger)
    {
        _useCase = useCase;
        _logger = logger;
    }

    [Function(nameof(RetryStalledFiscalSubmissionsFunction))]
    public async Task Run(
        [TimerTrigger(EveryFifteenMinutes)] TimerInfo timer,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<string> reEnqueuedInvoiceNumbers = await _useCase.ExecuteAsync(cancellationToken);

        if (reEnqueuedInvoiceNumbers.Count > 0)
        {
            _logger.LogWarning(
                "Re-enqueued stalled fiscal submissions: {InvoiceNumbers}.",
                string.Join(", ", reEnqueuedInvoiceNumbers));
        }
    }
}
