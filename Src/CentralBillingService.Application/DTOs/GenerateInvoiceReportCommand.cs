namespace CentralBillingService.Application.DTOs;

/// <param name="LayoutOverride">Prints with this model instead of the one stored with the invoice
/// (e.g. to compare models in the admin). Nothing is persisted; null uses the stored layout.</param>
public sealed record GenerateInvoiceReportCommand(
    string InvoiceNumber,
    string BillingSource,
    string? LayoutOverride = null);
