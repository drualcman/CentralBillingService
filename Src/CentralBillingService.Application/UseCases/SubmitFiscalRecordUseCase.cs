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

        var invoice = await _repository.FindByNumberAsync(
                command.BillingSource, command.InvoiceNumber, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Invoice '{command.InvoiceNumber}' not found for billing source '{command.BillingSource}'.");

        var outcome = await registrar.SubmitAsync(invoice, cancellationToken);

        await _iso9001.Register(command.InvoiceNumber, this,
            $"Fiscal submission result: {outcome.State}", outcome);

        return outcome;
    }
}
