namespace CentralBillingService.Application.DTOs;

public sealed class VerifyInvoiceQuery
{
    public required string BillingSource { get; init; }
    public required string Secret { get; init; }
    public required string InvoiceNumber { get; init; }

    /// <summary>
    /// The hash extracted from the QR code on the customer's document.
    /// Used to confirm the document in hand is the original issued invoice.
    /// </summary>
    public required string ProvidedHash { get; init; }
}
