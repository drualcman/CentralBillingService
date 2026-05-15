namespace CentralBillingService.Application.DTOs;

/// <summary>
/// Result of an invoice verification.
///
/// DocumentHashMatches: the hash from the customer's QR equals the stored hash.
/// IntegrityVerified: the stored hash equals the hash recomputed from DB fields.
/// IsValid: both checks passed.
/// </summary>
public sealed class VerifyInvoiceResult
{
    public required string InvoiceNumber { get; init; }
    public required bool IsValid { get; init; }
    public required string Hash { get; init; }
    public required bool DocumentHashMatches { get; init; }
    public required bool IntegrityVerified { get; init; }
    public string? Message { get; init; }
}
