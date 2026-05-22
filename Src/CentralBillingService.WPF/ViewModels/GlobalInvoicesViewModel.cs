using CentralBillingService.Domain.Services;

namespace CentralBillingService.WPF.ViewModels;

public partial class GlobalInvoicesViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly BillingSourceRegistry _registry;
    private readonly Action<object> _navigate;

    public BillingSourceEntry[] AllBillingSources { get; }

    [ObservableProperty] ObservableCollection<InvoiceSummaryResult> allInvoices = [];
    [ObservableProperty] InvoiceSummaryResult? selectedInvoice;
    [ObservableProperty] bool isLoading;
    [ObservableProperty] string? errorMessage;

    [ObservableProperty] string? filterBillingSource;
    [ObservableProperty] string? filterStatus;
    [ObservableProperty] DateTime? filterIssuedFrom;
    [ObservableProperty] DateTime? filterIssuedTo;

    [ObservableProperty] int currentPage = 1;
    [ObservableProperty] int totalPages = 1;
    [ObservableProperty] int totalCount;
    private const int PageSize = 25;

    public bool CanGoPrev => CurrentPage > 1;
    public bool CanGoNext => CurrentPage < TotalPages;
    public int AllInvoicesCount => AllInvoices.Count;

    partial void OnAllInvoicesChanged(ObservableCollection<InvoiceSummaryResult> value) =>
        OnPropertyChanged(nameof(AllInvoicesCount));

    public static string[] StatusOptions { get; } = ["", "Issued", "Rectified", "Cancelled", "Draft"];

    public GlobalInvoicesViewModel(
        IServiceScopeFactory scopeFactory,
        BillingSourceRegistry registry,
        Action<object> navigate)
    {
        _scopeFactory = scopeFactory;
        _registry = registry;
        _navigate = navigate;
        AllBillingSources =
        [
            new BillingSourceEntry("", ""),
            .. registry.GetAll()
                .Select(c => new BillingSourceEntry(
                    c.BillingSource,
                    !string.IsNullOrWhiteSpace(c.Issuer.TradeName) ? c.Issuer.TradeName! : c.Issuer.LegalName))
                .OrderBy(e => e.DisplayName)
        ];
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var useCase = scope.ServiceProvider.GetRequiredService<PublicListInvoicesUseCase>();

            var result = await useCase.ExecuteAsync(new ListInvoicesQuery
            {
                BillingSource = string.IsNullOrEmpty(FilterBillingSource) ? null : FilterBillingSource,
                Status = string.IsNullOrEmpty(FilterStatus) ? null : FilterStatus,
                IssuedFrom = FilterIssuedFrom.HasValue ? DateOnly.FromDateTime(FilterIssuedFrom.Value) : null,
                IssuedTo = FilterIssuedTo.HasValue ? DateOnly.FromDateTime(FilterIssuedTo.Value) : null,
                Page = CurrentPage,
                PageSize = PageSize,
            });

            var merged = result.Items
                .Concat(result.RectificativeItems)
                .OrderByDescending(x => x.IssueDate)
                .ThenByDescending(x => x.InvoiceNumber)
                .ToList();

            AllInvoices = new ObservableCollection<InvoiceSummaryResult>(merged);
            TotalPages = result.TotalPages == 0 ? 1 : result.TotalPages;
            TotalCount = result.TotalCount;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(CanGoPrev));
            OnPropertyChanged(nameof(CanGoNext));
        }
    }

    [RelayCommand]
    async Task Refresh() => await LoadAsync();

    [RelayCommand]
    async Task ApplyFilters()
    {
        CurrentPage = 1;
        await LoadAsync();
    }

    [RelayCommand]
    void ClearFilters()
    {
        FilterBillingSource = null;
        FilterStatus = null;
        FilterIssuedFrom = null;
        FilterIssuedTo = null;
        CurrentPage = 1;
        _ = LoadAsync();
    }

    [RelayCommand]
    async Task PreviousPage()
    {
        if (!CanGoPrev) return;
        CurrentPage--;
        await LoadAsync();
    }

    [RelayCommand]
    async Task NextPage()
    {
        if (!CanGoNext) return;
        CurrentPage++;
        await LoadAsync();
    }

    [RelayCommand]
    void PreviewInvoice(InvoiceSummaryResult? invoice)
    {
        if (invoice is null) return;
        var vm = new InvoicePreviewViewModel(_scopeFactory,
            invoice.InvoiceNumber, invoice.BillingSource,
            goBack: () => _navigate(this));
        _navigate(vm);
        _ = vm.LoadAsync();
    }
}
