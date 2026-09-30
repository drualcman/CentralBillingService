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

        // Chained authorities require records in stamping order: first report every still-pending
        // predecessor of this document's chain (invoices and rectificatives share it), oldest first.
        // A failure stops the drain and is rethrown, so the message is retried and a later record is
        // never reported before an earlier one.
        IReadOnlyList<string> pendingPredecessors = await registrar.GetPendingSubmissionsUpToAsync(
            command.BillingSource, command.InvoiceNumber, cancellationToken);
        foreach (string predecessorNumber in pendingPredecessors.Where(number => number != command.InvoiceNumber))
        {
            FiscalSubmissionOutcome predecessorOutcome = await SubmitDocumentAsync(
                registrar, command.BillingSource, predecessorNumber, cancellationToken);
            await _iso9001.Register(predecessorNumber, this,
                $"Fiscal submission result (drained before {command.InvoiceNumber}): {predecessorOutcome.State}", predecessorOutcome);
        }

        FiscalSubmissionOutcome outcome = await SubmitDocumentAsync(
            registrar, command.BillingSource, command.InvoiceNumber, cancellationToken);

        await _iso9001.Register(command.InvoiceNumber, this,
            $"Fiscal submission result: {outcome.State}", outcome);

        return outcome;
    }

    /// <summary>Submits the document with that number, whether it is an invoice or a rectificative.</summary>
    private async Task<FiscalSubmissionOutcome> SubmitDocumentAsync(
        IFiscalRegistrar registrar, string billingSource, string documentNumber, CancellationToken cancellationToken)
    {
        Invoice? invoice = await _repository.FindByNumberAsync(billingSource, documentNumber, cancellationToken);
        FiscalSubmissionOutcome outcome;

        if (invoice is not null)
        {
            outcome = await registrar.SubmitAsync(invoice, cancellationToken);
        }
        else
        {
            RectificativeInvoice rectificative = await _repository.FindRectificativeByNumberAsync(billingSource, documentNumber, cancellationToken)
                ?? throw new InvalidOperationException(
                    $"Invoice or rectificative '{documentNumber}' not found for billing source '{billingSource}'.");
            RectifiedInvoiceAmounts rectifiedAmounts = await LoadRectifiedAmountsAsync(rectificative, cancellationToken);
            outcome = await registrar.SubmitRectificativeAsync(rectificative, rectifiedAmounts, cancellationToken);
        }

        return outcome;
    }

    /// <summary>The rectified document may itself be an invoice or a rectificative.</summary>
    private async Task<RectifiedInvoiceAmounts> LoadRectifiedAmountsAsync(
        RectificativeInvoice rectificative, CancellationToken cancellationToken)
    {
        string rectifiedNumber = rectificative.OriginalInvoiceNumber.Value;
        Invoice? rectifiedInvoice = await _repository.FindByNumberAsync(rectificative.BillingSource, rectifiedNumber, cancellationToken);
        RectifiedInvoiceAmounts rectifiedAmounts;

        if (rectifiedInvoice is not null)
        {
            rectifiedAmounts = RectifiedInvoiceAmounts.From(rectifiedInvoice);
        }
        else
        {
            RectificativeInvoice rectifiedRectificative = await _repository.FindRectificativeByNumberAsync(rectificative.BillingSource, rectifiedNumber, cancellationToken)
                ?? throw new InvalidOperationException(
                    $"Rectified document '{rectifiedNumber}' of '{rectificative.Number.Value}' not found for billing source '{rectificative.BillingSource}'.");
            rectifiedAmounts = RectifiedInvoiceAmounts.From(rectifiedRectificative);
        }

        return rectifiedAmounts;
    }
}
