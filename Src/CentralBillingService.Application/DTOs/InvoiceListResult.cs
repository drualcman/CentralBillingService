namespace CentralBillingService.Application.DTOs;

/// <summary>
/// Paged list of invoice summaries.
/// Returns lightweight summaries rather than full InvoiceResult objects
/// to keep list responses fast and small.
/// </summary>
public sealed class InvoiceListResult
{
    public required IReadOnlyList<InvoiceSummaryResult> Items { get; init; }
    public required int TotalCount { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int TotalPages { get; init; }
    public required bool HasNextPage { get; init; }

    /// <summary>Rectificative invoices in the current page (ordered together with standard invoices).</summary>
    public required IReadOnlyList<InvoiceSummaryResult> RectificativeItems { get; init; }
}
