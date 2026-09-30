namespace CentralBillingService.Application.UseCases;

/// <summary>
/// Submits an already-created, stamped invoice to its billing source's fiscal authority
/// (e.g. AEAT VeriFactu). Triggered asynchronously from the fiscal-submission queue.
///
/// Authority-agnostic: it resolves the source's <see cref="IFiscalRegistrar"/> and delegates.
/// The concrete registrar owns the record mapping, transport and persistence of the
/// submission state — this use case only orchestrates and traces.
/// </summary>
public sealed class SubmitFiscalRecordUseCase
{
    private readonly IInvoiceRepository _repository;
    private readonly BillingSourceRegistry _registry;
    private readonly IFiscalRegistrarFactory _registrarFactory;
    private readonly IIso9001 _iso9001;

    public SubmitFiscalRecordUseCase(
        IInvoiceRepository repository,
        BillingSourceRegistry registry,
        IFiscalRegistrarFactory registrarFactory,
        IIso9001 iso9001)
    {
        _repository = repository;
        _registry = registry;
        _registrarFactory = registrarFactory;
        _iso9001 = iso9001;
    }

    public async Task<FiscalSubmissionOutcome> ExecuteAsync(
        SubmitFiscalRecordCommand command,
        CancellationToken cancellationToken = default)
    {
        await _iso9001.Register(command.InvoiceNumber, this, "Submitting invoice to fiscal authority", command);

        var config = _registry.GetConfig(command.BillingSource);
        var registrar = _registrarFactory.GetFor(config);

        Invoice invoice = await LoadInvoiceAsync(command.BillingSource, command.InvoiceNumber, cancellationToken);

        // Chained authorities require records in stamping order: first report every still-pending
        // predecessor of this invoice's chain, oldest first. A failure stops the drain and is rethrown,
        // so the message is retried and a later record is never reported before an earlier one.
        IReadOnlyList<string> pendingPredecessors = await registrar.GetPendingSubmissionsUpToAsync(invoice, cancellationToken);
        foreach (string predecessorNumber in pendingPredecessors.Where(number => number != command.InvoiceNumber))
        {
            Invoice predecessor = await LoadInvoiceAsync(command.BillingSource, predecessorNumber, cancellationToken);
            FiscalSubmissionOutcome predecessorOutcome = await registrar.SubmitAsync(predecessor, cancellationToken);
            await _iso9001.Register(predecessorNumber, this,
                $"Fiscal submission result (drained before {command.InvoiceNumber}): {predecessorOutcome.State}", predecessorOutcome);
        }

        FiscalSubmissionOutcome outcome = await registrar.SubmitAsync(invoice, cancellationToken);

        await _iso9001.Register(command.InvoiceNumber, this,
            $"Fiscal submission result: {outcome.State}", outcome);

        return outcome;
    }

    private async Task<Invoice> LoadInvoiceAsync(string billingSource, string invoiceNumber, CancellationToken cancellationToken) =>
        await _repository.FindByNumberAsync(billingSource, invoiceNumber, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Invoice '{invoiceNumber}' not found for billing source '{billingSource}'.");
}
