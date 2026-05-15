namespace CentralBillingService.Application.Models;

/// <summary>
/// Paged result for invoice listing.
/// A single query against the unified Invoices table returns both standard
/// and rectificative invoices — TotalCount covers both types combined.
/// </summary>
public sealed class InvoicePagedResult
{
    public required IReadOnlyList<Invoice> Items { get; init; }
    public required IReadOnlyList<RectificativeInvoice> Rectificatives { get; init; }
    public required int TotalCount { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
}
