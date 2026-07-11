namespace CentralBillingService.Infrastructure.Interfaces;

/// <summary>
/// Read-side context: all queries the repository needs from the storage layer.
/// Implemented in the persistence project (e.g. Persistence.SqlServer, Persistence.CosmosDb).
///
/// Methods here are pure reads — no side effects, no state changes.
/// Implementations are free to use read replicas, caches, or optimized
/// read models without affecting the write side.
/// </summary>
public interface IInvoiceReadContext
{
    Task<Invoice?> FindByIdAsync(string billingSource, Guid id, CancellationToken cancellationToken = default);

    Task<Invoice?> FindByNumberAsync(string billingSource, string invoiceNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a standard invoice ("F") by its payment reference within a billing source.
    /// Used to make invoice creation idempotent: a retried payment webhook must not
    /// produce a second invoice for the same payment. Returns null if none exists.
    /// </summary>
    Task<Invoice?> FindByPaymentReferenceAsync(string billingSource, string paymentReference, CancellationToken cancellationToken = default);

    Task<string?> GetLastHashAsync(
        string billingSource,
        string serie,
        int year,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a paged, filtered list of both standard and rectificative invoices.
    /// TotalCount is the combined count. Results are ordered by IssueDate DESC, SequenceNumber DESC.
    /// </summary>
    Task<InvoicePagedResult> ListAsync(
        InvoiceFilter filter,
        CancellationToken cancellationToken = default);

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
}
