namespace CentralBillingService.Application.UseCases;

/// <summary>
/// Returns billing totals grouped by annual, quarterly, and four-monthly periods.
/// Aggregates all billing sources when billingSource is null.
/// </summary>
public sealed class GetInvoicesSummaryUseCase
{
    private readonly IInvoiceRepository _repository;

    public GetInvoicesSummaryUseCase(IInvoiceRepository repository)
    {
        _repository = repository;
    }

    public async Task<InvoicesSummaryResult> ExecuteAsync(
        string? billingSource,
        CancellationToken cancellationToken = default)
    {
        var source = string.IsNullOrWhiteSpace(billingSource) ? null : billingSource;

        var data = await _repository.GetSummaryDataAsync(source, cancellationToken);

        return new InvoicesSummaryResult
        {
            BillingSource = source,
            Annual = BuildAnnual(data),
            Quarterly = BuildQuarterly(data),
            FourMonthly = BuildFourMonthly(data),
        };
    }

    private static IReadOnlyList<InvoicesPeriodSummaryResult> BuildAnnual(
        IReadOnlyList<InvoiceSummaryDataPoint> data) =>
        data.GroupBy(x => x.Year)
            .Select(g => new InvoicesPeriodSummaryResult
            {
                Year = g.Key,
                Period = null,
                Label = g.Key.ToString(),
                TotalEur = g.Sum(x => x.TotalEur),
                TaxableBaseEur = g.Sum(x => x.TaxableBaseEur),
                TotalTaxAmountEur = g.Sum(x => x.TotalTaxAmountEur),
                InvoiceCount = g.Count(),
            })
            .OrderByDescending(x => x.Year)
            .ToList();

    private static IReadOnlyList<InvoicesPeriodSummaryResult> BuildQuarterly(
        IReadOnlyList<InvoiceSummaryDataPoint> data) =>
        data.GroupBy(x => new { x.Year, Quarter = (x.Month - 1) / 3 + 1 })
            .Select(g => new InvoicesPeriodSummaryResult
            {
                Year = g.Key.Year,
                Period = g.Key.Quarter,
                Label = $"Q{g.Key.Quarter} {g.Key.Year}",
                TotalEur = g.Sum(x => x.TotalEur),
                TaxableBaseEur = g.Sum(x => x.TaxableBaseEur),
                TotalTaxAmountEur = g.Sum(x => x.TotalTaxAmountEur),
                InvoiceCount = g.Count(),
            })
            .OrderByDescending(x => x.Year).ThenByDescending(x => x.Period)
            .ToList();

    private static IReadOnlyList<InvoicesPeriodSummaryResult> BuildFourMonthly(
        IReadOnlyList<InvoiceSummaryDataPoint> data) =>
        data.GroupBy(x => new { x.Year, Cuatrimestre = (x.Month - 1) / 4 + 1 })
            .Select(g => new InvoicesPeriodSummaryResult
            {
                Year = g.Key.Year,
                Period = g.Key.Cuatrimestre,
                Label = $"T{g.Key.Cuatrimestre} {g.Key.Year}",
                TotalEur = g.Sum(x => x.TotalEur),
                TaxableBaseEur = g.Sum(x => x.TaxableBaseEur),
                TotalTaxAmountEur = g.Sum(x => x.TotalTaxAmountEur),
                InvoiceCount = g.Count(),
            })
            .OrderByDescending(x => x.Year).ThenByDescending(x => x.Period)
            .ToList();
}
