namespace CentralBillingService.AzureFunction.API.Requests;

internal sealed class CreateInvoiceRequest
{
    public required string Serie { get; init; }
    public required RecipientDto Recipient { get; init; }
    public required IReadOnlyList<InvoiceLineDto> Lines { get; init; }
    public required string OriginCurrencyCode { get; init; }
    public DateOnly? IssueDate { get; init; }
    public DateOnly? ValueDate { get; init; }
    public string? Notes { get; init; }
    public required string PaymentMethod { get; init; }
    public required string PaymentReference { get; init; }
    public string? TransactionData { get; init; }
}
