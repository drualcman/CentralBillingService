namespace CentralBillingService.AzureFunction.API.Requests;

internal sealed class RectifyInvoiceRequest
{
    public required string RectificativeSerie { get; init; }
    public required string Reason { get; init; }
    public required RectificationType RectificationType { get; init; }
    public IReadOnlyList<InvoiceLineDto>? Lines { get; init; }
    public string? Notes { get; init; }
    public string? PaymentMethod { get; init; }
    public required string PaymentReference { get; init; }
    public string? TransactionData { get; init; }
}
