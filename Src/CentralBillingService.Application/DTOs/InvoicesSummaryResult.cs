namespace CentralBillingService.Application.DTOs;

public sealed class InvoicesSummaryResult
{
    /// <summary>The billing source used to filter, or null when aggregating all sources.</summary>
    public required string? BillingSource { get; init; }

    public required IReadOnlyList<InvoicesPeriodSummaryResult> Annual { get; init; }
    public required IReadOnlyList<InvoicesPeriodSummaryResult> Quarterly { get; init; }
    public required IReadOnlyList<InvoicesPeriodSummaryResult> FourMonthly { get; init; }
}
