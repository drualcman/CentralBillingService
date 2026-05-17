namespace CentralBillingService.Application.DTOs;

public sealed class VerifyInvoiceQuery
{
    public required string BillingSource { get; init; }
    public required string InvoiceNumber { get; init; }

    /// <summary>
    /// The hash extracted from the QR code on the customer's document.
    /// Used to confirm the document in hand is the original issued invoice.
    /// </summary>
    public required string ProvidedHash { get; init; }

    /// <summary>
    /// Recipient NIF as encoded in the QR URL — validated against the stored value.
    /// </summary>
    public string? ProvidedRecipientTaxId { get; init; }

    /// <summary>
    /// Issue date as encoded in the QR URL — validated against the stored value.
    /// </summary>
    public DateOnly? ProvidedIssueDate { get; init; }

    /// <summary>
    /// Total amount as encoded in the QR URL — validated against the stored value.
    /// </summary>
    public decimal? ProvidedTotalEur { get; init; }
}
