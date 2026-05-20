namespace CentralBillingService.VerifyUI.Models;

public sealed class PublicInvoiceListResponse
{
    public List<PublicInvoiceSummaryItemResponse> Items { get; set; } = [];
    public List<PublicInvoiceSummaryItemResponse> RectificativeItems { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
}
