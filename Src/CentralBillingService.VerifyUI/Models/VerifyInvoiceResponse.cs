namespace CentralBillingService.VerifyUI.Models;

public sealed record VerifyInvoiceResponse(
    string InvoiceNumber,
    bool IsValid,
    bool DocumentHashMatches,
    bool IntegrityVerified,
    bool QrDataConsistent,
    string? Message,
    string? IssuerTaxId,
    string? IssuerName,
    string? RecipientTaxId,
    string? RecipientName,
    DateOnly? IssueDate,
    decimal? TotalEur);
