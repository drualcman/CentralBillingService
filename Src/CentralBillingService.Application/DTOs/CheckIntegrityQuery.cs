namespace CentralBillingService.Application.DTOs;

public sealed class CheckIntegrityQuery
{
    public required string BillingSource { get; init; }
    public required string Secret { get; init; }
    public required string InvoiceNumber { get; init; }
}
