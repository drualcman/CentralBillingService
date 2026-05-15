namespace CentralBillingService.Client.Models;

public sealed class VerifyInvoiceResult
{
    public required string InvoiceNumber { get; init; }
    public required bool IsValid { get; init; }
    public required string Hash { get; init; }
    public required bool DocumentHashMatches { get; init; }
    public required bool IntegrityVerified { get; init; }
    public string? Message { get; init; }
}
