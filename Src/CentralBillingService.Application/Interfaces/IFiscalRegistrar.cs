namespace CentralBillingService.Application.Interfaces;

/// <summary>
/// Port for reporting an invoice to an external fiscal authority (e.g. the Spanish AEAT
/// via VeriFactu, or an equivalent body in another jurisdiction).
///
/// Defined in Application so use cases depend on the abstraction, never on a concrete
/// authority. The whole reporting logic (record mapping, tax breakdown, fingerprint,
/// chaining, transport, persistence of submission state) lives inside the concrete
/// adapter — "we just hand it our Invoice". Selected per BillingSource by
/// <see cref="IFiscalRegistrarFactory"/> based on RegistrarConfig.Type.
///
/// Two phases:
///  - <see cref="StampAsync"/> runs synchronously during invoice creation and produces
///    what must be printed on the invoice (fingerprint + QR content).
///  - <see cref="SubmitAsync"/> runs asynchronously in a background worker and sends the
///    already-stamped record to the authority.
/// </summary>
public interface IFiscalRegistrar
{
    /// <summary>Discriminator matching RegistrarConfig.Type (e.g. "None", "VeriFactu").</summary>
    string RegistrarType { get; }

    /// <summary>
    /// Computes the authority-conformant fingerprint and QR content for the invoice,
    /// advancing the fiscal chain as needed. Called synchronously at creation, before
    /// persistence, so the stamp can be printed. Returns <see cref="FiscalStampResult.None"/>
    /// for registrars that do not stamp.
    /// </summary>
    Task<FiscalStampResult> StampAsync(Invoice invoice, CancellationToken cancellationToken = default);

    /// <summary>
    /// Submits the invoice's fiscal record to the authority and reports the outcome.
    /// Called asynchronously by the background worker after the invoice is persisted.
    /// </summary>
    Task<FiscalSubmissionOutcome> SubmitAsync(Invoice invoice, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invoice numbers still pending submission in the invoice's fiscal chain, up to and including
    /// the invoice itself, in chain order. Chained authorities require records to be reported in
    /// the order they were stamped, so the worker submits these one by one before the invoice.
    /// Empty when nothing is pending (or the registrar does not chain).
    /// </summary>
    Task<IReadOnlyList<string>> GetPendingSubmissionsUpToAsync(Invoice invoice, CancellationToken cancellationToken = default);

    /// <summary>
    /// The most recent invoice number of the billing source still pending submission and stamped
    /// before <paramref name="stampedBefore"/> (i.e. its queue message was lost or exhausted its
    /// retries). Re-submitting it also drains every pending predecessor. Null when none is stalled.
    /// </summary>
    Task<string?> GetLatestStalledSubmissionAsync(string billingSource, DateTimeOffset stampedBefore, CancellationToken cancellationToken = default);
}
