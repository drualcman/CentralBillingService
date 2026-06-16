using CentralBillingService.Domain.Services;

namespace CentralBillingService.WPF.ViewModels;

public partial class BillingSummaryViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;

    public string[] AllBillingSources { get; }

    [ObservableProperty] string? selectedBillingSource;
    [ObservableProperty] InvoicesSummaryResult? summary;
    [ObservableProperty] bool isLoading;
    [ObservableProperty] string? errorMessage;

    [ObservableProperty] List<YearFilterOption> availableYears = [];
    [ObservableProperty] YearFilterOption? selectedYear;
    [ObservableProperty] IReadOnlyList<InvoicesPeriodSummaryResult>? filteredAnnual;
    [ObservableProperty] IReadOnlyList<InvoicesPeriodSummaryResult>? filteredQuarterly;
    [ObservableProperty] IReadOnlyList<InvoicesPeriodSummaryResult>? filteredFourMonthly;

    partial void OnSelectedBillingSourceChanged(string? value) => _ = LoadAsync();

    partial void OnSelectedYearChanged(YearFilterOption? value) => ApplyYearFilter();

    public BillingSummaryViewModel(IServiceScopeFactory scopeFactory, BillingSourceRegistry registry)
    {
        _scopeFactory = scopeFactory;
        AllBillingSources = ["", .. registry.GetAll().Select(c => c.BillingSource).Order()];
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        Summary = null;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var useCase = scope.ServiceProvider.GetRequiredService<GetInvoicesSummaryUseCase>();
            Summary = await useCase.ExecuteAsync(
                string.IsNullOrEmpty(SelectedBillingSource) ? null : SelectedBillingSource);

            RebuildYearOptions();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void RebuildYearOptions()
    {
        var years = Summary?.Annual.Select(a => a.Year).Distinct().OrderByDescending(y => y).ToList() ?? [];

        AvailableYears =
        [
            new YearFilterOption(null, "Todos los años"),
            .. years.Select(y => new YearFilterOption(y, y.ToString())),
        ];

        // Preserve the current selection if it still exists; otherwise default to the
        // current year when it has data, falling back to the most recent year.
        var previous = SelectedYear?.Year;
        var target = years.Contains(previous ?? -1) ? previous
            : years.Contains(DateTime.Now.Year) ? DateTime.Now.Year
            : years.Count > 0 ? years[0]
            : (int?)null;

        SelectedYear = AvailableYears.FirstOrDefault(o => o.Year == target) ?? AvailableYears[0];

        // Ensure the filter reflects the freshly loaded data even when the selected
        // year is value-equal to the previous one (record equality suppresses the change event).
        ApplyYearFilter();
    }

    private void ApplyYearFilter()
    {
        if (Summary is null)
        {
            FilteredAnnual = FilteredQuarterly = FilteredFourMonthly = null;
            return;
        }

        var year = SelectedYear?.Year;

        FilteredAnnual = year is null
            ? Summary.Annual
            : Summary.Annual.Where(x => x.Year == year).ToList();
        FilteredQuarterly = year is null
            ? Summary.Quarterly
            : Summary.Quarterly.Where(x => x.Year == year).ToList();
        FilteredFourMonthly = year is null
            ? Summary.FourMonthly
            : Summary.FourMonthly.Where(x => x.Year == year).ToList();
    }
}

public sealed record YearFilterOption(int? Year, string Display);
