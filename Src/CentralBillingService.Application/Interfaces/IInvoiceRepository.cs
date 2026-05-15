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
    /// Returns the hash of the last issued invoice for the given
    /// BillingSource + Serie + Year combination.
    /// Returns null if no invoice exists yet for that combination.
    /// </summary>
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


    // ── Writes ─────────────────────────────────────────────────────────────

    Task SaveAsync(
        Invoice invoice,
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
