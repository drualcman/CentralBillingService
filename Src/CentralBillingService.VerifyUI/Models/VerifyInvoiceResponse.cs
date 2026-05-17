namespace CentralBillingService.VerifyUI.Models;

public sealed record VerifyInvoiceResponse(
    string InvoiceNumber,
    bool IsValid,
    bool DocumentHashMatches,
    bool IntegrityVerified,
    string? Message,
    string? IssuerTaxId,
    string? IssuerName,
    DateOnly? IssueDate,
    decimal? TotalEur);
