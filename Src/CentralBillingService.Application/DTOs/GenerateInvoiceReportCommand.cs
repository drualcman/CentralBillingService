namespace CentralBillingService.Application.DTOs;

public sealed record GenerateInvoiceReportCommand(
    string InvoiceNumber,
    string BillingSource);