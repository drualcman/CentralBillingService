namespace CentralBillingService.Application.DTOs;

/// <summary>
/// Payload sent to the QR generation queue.
/// Contains all data needed to build the verification URL and upload the QR image
/// without requiring a database read inside the background job.
/// </summary>
public sealed record GenerateInvoiceQrCommand(
    string InvoiceNumber,
    string BillingSource,
    string Hash,
    DateOnly IssueDate,
    decimal TotalEurAmount,
    string IssuerTaxId);
