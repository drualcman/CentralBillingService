namespace CentralBillingService.Client.Models;

public sealed class InvoiceCreateReult
{
    public required string InvoiceNumber { get; init; }
    public required string Status { get; init; }
    public required string Hash { get; init; }
}