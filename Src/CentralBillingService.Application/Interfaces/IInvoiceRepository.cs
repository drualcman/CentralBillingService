namespace CentralBillingService.Application.Interfaces;

/// <summary>
/// Port for persisting and retrieving invoices.
/// Defined in Application so use cases can depend on it
/// without knowing the concrete storage technology.
/// Implemented in Infrastructure.
/// </summary>
public interface IInvoiceRepository
{
    // ── Reads ──────────────────────────────────────────────────────────────

    Task<Invoice?> FindByIdAsync(
        string billingSource,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Invoice?> FindByNumberAsync(
        string billingSource,
        string invoiceNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a standard invoice by its payment reference within a billing source.
    /// Used to make creation idempotent under retried payment webhooks.
    /// Returns null if no invoice has that payment reference yet.
    /// </summary>
    Task<Invoice?> FindByPaymentReferenceAsync(
        string billingSource,
        string paymentReference,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the hash of the last issued invoice for the given
    /// BillingSource + Serie + Year combination.
    /// Returns null if no invoice exists yet for that combination.
    /// </summary>
    /// <summary>
    /// True when another billing source of the same issuer (NIF) already issued invoices in this
    /// serie and year. The tax authority identifies an invoice by NIF + number, so two sources of the
    /// same NIF sharing a serie would produce duplicate invoice numbers.
    /// </summary>
    Task<bool> IsSerieUsedByAnotherBillingSourceAsync(
        string issuerTaxId, string serie, int year, string billingSource, CancellationToken cancellationToken = default);

    /// <summary>True when the billing source has issued ordinary (non-rectificative) invoices in this serie.</summary>
    Task<bool> IsSerieUsedByOrdinaryInvoicesAsync(
        string billingSource, string serie, CancellationToken cancellationToken = default);

    Task<string?> GetLastHashAsync(
        string billingSource,
        string serie,
        int year,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a paged, filtered list of invoices (both standard and rectificative).
    /// TotalCount covers all matching records regardless of type.
    /// Results are ordered by IssueDate DESC, SequenceNumber DESC.
    /// </summary>
    Task<InvoicePagedResult> ListAsync(
        InvoiceFilter filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a rectificative invoice by its number.
    /// Returns null if not found.
    /// </summary>
    Task<RectificativeInvoice?> FindRectificativeByNumberAsync(
        string billingSource,
        string invoiceNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns lightweight projections for period-based summary aggregation.
    /// Pass null billingSource to aggregate across all sources.
    /// </summary>
    Task<IReadOnlyList<InvoiceSummaryDataPoint>> GetSummaryDataAsync(
        string? billingSource,
        CancellationToken cancellationToken = default);


    // ── Writes ─────────────────────────────────────────────────────────────

    Task SaveAsync(
        Invoice invoice,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists the fiscal registrar outputs (the "huella" stamp and the authority QR content,
    /// e.g. the AEAT ValidarQR URL) onto an already-created invoice. Called after creation, once
    /// the external fiscal registrar has produced them. Targeted update — does not touch any other
    /// field. <paramref name="fiscalQrContent"/> is optional. No-op if the invoice does not exist.
    /// </summary>
    Task UpdateFiscalStampAsync(
        Guid id,
        string fiscalStamp,
        string? fiscalQrContent = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reserves the next sequence number and persists the resulting invoice atomically,
    /// in a single transaction. Use this for billing sources whose number authority is the
    /// local database (see <see cref="IInvoiceNumberProvider.ReservesFromLocalDatabase"/>):
    /// a cancellation or failure rolls back cleanly and never leaves a gap in the numbering.
    ///
    /// <paramref name="buildInvoice"/> receives the reserved number and the previous chain hash
    /// and returns the fully-built, hashed invoice (this is where the domain runs). It may be
    /// invoked more than once if the transaction is retried after a transient failure.
    /// </summary>
    Task<Invoice> CreateAtomicAsync(
        string billingSource,
        string serie,
        int year,
        Func<int, string?, CancellationToken, Task<Invoice>> buildInvoice,
        CancellationToken cancellationToken = default);

    Task SaveRectificativeAsync(
        RectificativeInvoice rectificative,
        Invoice updatedOriginal,
        CancellationToken cancellationToken = default);

    Task SaveRectificativeFromRectificativeAsync(
        RectificativeInvoice rectificative,
        RectificativeInvoice updatedOriginal,
        CancellationToken cancellationToken = default);

}
