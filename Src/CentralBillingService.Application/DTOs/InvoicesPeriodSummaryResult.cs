namespace CentralBillingService.Application.DTOs;

public sealed class InvoicesPeriodSummaryResult
{
    public required int Year { get; init; }

    /// <summary>1-based period number within the year. Null for annual entries.</summary>
    public required int? Period { get; init; }

    /// <summary>Human-readable label: "2025", "Q1 2025", "T1 2025".</summary>
    public required string Label { get; init; }

    public required decimal TotalEur { get; init; }
    public required decimal TaxableBaseEur { get; init; }
    public required decimal TotalTaxAmountEur { get; init; }
    public required int InvoiceCount { get; init; }
}
