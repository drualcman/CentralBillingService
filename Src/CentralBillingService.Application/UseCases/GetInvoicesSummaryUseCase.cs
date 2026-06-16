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
        BuildPeriodic(data, periodsPerYear: 4, monthsPerPeriod: 3, prefix: "Q");

    private static IReadOnlyList<InvoicesPeriodSummaryResult> BuildFourMonthly(
        IReadOnlyList<InvoiceSummaryDataPoint> data) =>
        BuildPeriodic(data, periodsPerYear: 3, monthsPerPeriod: 4, prefix: "T");

    /// <summary>
    /// Builds one entry per period for every year that has data, filling periods
    /// with no invoices as zero so an elapsed fiscal period without activity still
    /// appears. Periods that have not started yet (future periods of the current
    /// year) are omitted, since this reflects real billing, not a forecast.
    /// </summary>
    private static IReadOnlyList<InvoicesPeriodSummaryResult> BuildPeriodic(
        IReadOnlyList<InvoiceSummaryDataPoint> data,
        int periodsPerYear,
        int monthsPerPeriod,
        string prefix)
    {
        var byPeriod = data
            .GroupBy(x => (x.Year, Period: (x.Month - 1) / monthsPerPeriod + 1))
            .ToDictionary(g => g.Key, g => g.ToList());

        var years = data.Select(x => x.Year).Distinct();

        var today = DateTime.UtcNow;
        var currentPeriod = (today.Month - 1) / monthsPerPeriod + 1;

        var result = new List<InvoicesPeriodSummaryResult>();
        foreach (var year in years)
        {
            // For the current year only show periods up to the one in progress;
            // earlier years are always complete. Never hide a period that actually
            // has invoices, regardless of the calendar.
            var calendarLast = year >= today.Year ? currentPeriod : periodsPerYear;
            var dataLast = byPeriod.Keys.Where(k => k.Year == year)
                .Select(k => k.Period).DefaultIfEmpty(0).Max();
            var lastPeriod = Math.Max(calendarLast, dataLast);

            for (int period = 1; period <= lastPeriod; period++)
            {
                byPeriod.TryGetValue((year, period), out var points);
                result.Add(new InvoicesPeriodSummaryResult
                {
                    Year = year,
                    Period = period,
                    Label = $"{prefix}{period} {year}",
                    TotalEur = points?.Sum(x => x.TotalEur) ?? 0m,
                    TaxableBaseEur = points?.Sum(x => x.TaxableBaseEur) ?? 0m,
                    TotalTaxAmountEur = points?.Sum(x => x.TotalTaxAmountEur) ?? 0m,
                    InvoiceCount = points?.Count ?? 0,
                });
            }
        }

        return result
            .OrderByDescending(x => x.Year).ThenByDescending(x => x.Period)
            .ToList();
    }
}
