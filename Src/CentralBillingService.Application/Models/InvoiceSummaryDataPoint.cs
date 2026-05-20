namespace CentralBillingService.Application.Models;

/// <summary>
/// Lightweight projection used to compute period-based billing summaries.
/// Not a domain entity — only used for read-side aggregation.
/// </summary>
public sealed record InvoiceSummaryDataPoint(
    string BillingSource,
    int Year,
    int Month,
    decimal TotalEur,
    decimal TaxableBaseEur,
    decimal TotalTaxAmountEur);
