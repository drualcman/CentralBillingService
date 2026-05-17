namespace CentralBillingService.Application.DTOs;

/// <summary>
/// Result of an invoice verification.
///
/// DocumentHashMatches: the hash from the customer's QR equals the stored hash.
/// IntegrityVerified: the stored hash equals the hash recomputed from DB fields.
/// QrDataConsistent: nif/fecha/importe in the QR URL match the stored invoice fields.
/// IsValid: all checks passed.
/// </summary>
public sealed class VerifyInvoiceResult
{
    public required string InvoiceNumber { get; init; }
    public required bool IsValid { get; init; }
    public required string Hash { get; init; }
    public required bool DocumentHashMatches { get; init; }
    public required bool IntegrityVerified { get; init; }
    public required bool QrDataConsistent { get; init; }
    public string? Message { get; init; }

    public required string IssuerTaxId { get; init; }
    public required string IssuerName { get; init; }
    public required string RecipientTaxId { get; init; }
    public required string RecipientName { get; init; }
    public required DateOnly IssueDate { get; init; }
    public required decimal TotalEur { get; init; }
}
