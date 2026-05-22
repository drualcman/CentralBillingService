namespace CentralBillingService.Application.DTOs;

public sealed record SendInvoicePdfByEmailQuery
{
    public required string BillingSource { get; init; }
    public required string InvoiceNumber { get; init; }
}
