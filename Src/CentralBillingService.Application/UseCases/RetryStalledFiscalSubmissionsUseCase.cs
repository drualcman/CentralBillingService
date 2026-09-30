namespace CentralBillingService.Application.UseCases;

/// <summary>
/// Recovers fiscal submissions whose queue message was lost or exhausted its retries (poison):
/// for every billing source with a fiscal registrar, re-enqueues its most recent submission still
/// pending after a grace period. Submitting that record drains every pending predecessor of its
/// chain in order (see <see cref="SubmitFiscalRecordUseCase"/>), so one message per source suffices.
/// Run periodically by a timer.
/// </summary>
public sealed class RetryStalledFiscalSubmissionsUseCase
{
    /// <summary>Records younger than this are still in their normal queue flow and are left alone.</summary>
    public static readonly TimeSpan StalledAfter = TimeSpan.FromMinutes(10);

    private readonly BillingSourceRegistry _registry;
    private readonly IFiscalRegistrarFactory _registrarFactory;
    private readonly IJobQueue _jobQueue;
    private readonly TimeProvider _timeProvider;

    public RetryStalledFiscalSubmissionsUseCase(
        BillingSourceRegistry registry,
        IFiscalRegistrarFactory registrarFactory,
        IJobQueue jobQueue,
        TimeProvider timeProvider)
    {
        _registry = registry;
        _registrarFactory = registrarFactory;
        _jobQueue = jobQueue;
        _timeProvider = timeProvider;
    }

    /// <summary>Returns the invoice numbers re-enqueued (one per billing source with stalled records).</summary>
    public async Task<IReadOnlyList<string>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        DateTimeOffset stampedBefore = _timeProvider.GetUtcNow() - StalledAfter;
        List<string> reEnqueuedInvoiceNumbers = new List<string>();

        IEnumerable<BillingSourceConfig> fiscalSources = _registry.GetAll()
            .Where(config => config.Registrar is not null && !config.Registrar.IsNone);

        foreach (BillingSourceConfig config in fiscalSources)
        {
            IFiscalRegistrar registrar = _registrarFactory.GetFor(config);
            string? stalledInvoiceNumber = await registrar.GetLatestStalledSubmissionAsync(
                config.BillingSource, stampedBefore, cancellationToken);

            if (stalledInvoiceNumber is not null)
            {
                await _jobQueue.EnqueueFiscalSubmissionAsync(
                    new SubmitFiscalRecordCommand(stalledInvoiceNumber, config.BillingSource), cancellationToken);
                reEnqueuedInvoiceNumbers.Add(stalledInvoiceNumber);
            }
        }

        return reEnqueuedInvoiceNumbers;
    }
}
