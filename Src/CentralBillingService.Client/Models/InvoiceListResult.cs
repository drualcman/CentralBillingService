namespace CentralBillingService.Client.Models;

/// <summary>
/// Paged list of invoice summaries.
/// Contains both standard invoices (Items) and rectificative invoices (RectificativeItems)
/// from the same paginated query. TotalCount covers both types.
/// </summary>
public sealed class InvoiceListResult
{
    public required IReadOnlyList<InvoiceSummaryResult> Items { get; init; }
    public required IReadOnlyList<InvoiceSummaryResult> RectificativeItems { get; init; }
    public required int TotalCount { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int TotalPages { get; init; }
    public required bool HasNextPage { get; init; }
}
