namespace CentralBillingService.VerifyUI.Models;

public sealed record VerifyInvoiceResponse(
    string InvoiceNumber,
    bool IsValid,
    string Hash,
    bool DocumentHashMatches,
    bool IntegrityVerified,
    string? Message);
