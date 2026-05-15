using CentralBillingService.WPF.Services;

namespace CentralBillingService.WPF.ViewModels;

public partial class InvoicesViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly LocalMasterDataStore _masterDataStore;
    private readonly Action<object> _navigate;

    public BillingSourceSummary BillingSource { get; }

    [ObservableProperty] ObservableCollection<InvoiceSummaryResult> allInvoices = [];
    [ObservableProperty] InvoiceSummaryResult? selectedInvoice;

    public bool CanRectifySelected => SelectedInvoice is not null && SelectedInvoice.Status != "Rectified";

    partial void OnSelectedInvoiceChanged(InvoiceSummaryResult? value) =>
        OnPropertyChanged(nameof(CanRectifySelected));
    public int AllInvoicesCount => AllInvoices.Count;

    partial void OnAllInvoicesChanged(ObservableCollection<InvoiceSummaryResult> value) =>
        OnPropertyChanged(nameof(AllInvoicesCount));

    [ObservableProperty] bool isLoading;
    [ObservableProperty] string? errorMessage;

    // Filters
    [ObservableProperty] string? filterSerie;
    [ObservableProperty] int? filterYear;
    [ObservableProperty] string? filterStatus;
    [ObservableProperty] DateTime? filterIssuedFrom;
    [ObservableProperty] DateTime? filterIssuedTo;
    [ObservableProperty] string? filterRecipientExternalId;

    // Pagination
    [ObservableProperty] int currentPage = 1;
    [ObservableProperty] int totalPages = 1;
    [ObservableProperty] int totalCount;
    [ObservableProperty] int pageSize = 25;

    public bool CanGoPrev => CurrentPage > 1;
    public bool CanGoNext => CurrentPage < TotalPages;

    public static string[] StatusOptions { get; } =
        ["", "Issued", "Rectified", "Cancelled", "Draft"];

    public InvoicesViewModel(
        IServiceScopeFactory scopeFactory,
        BillingSourceSummary billingSource,
        LocalMasterDataStore masterDataStore,
        Action<object> navigate)
    {
        _scopeFactory = scopeFactory;
        _masterDataStore = masterDataStore;
        BillingSource = billingSource;
        _navigate = navigate;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var useCase = scope.ServiceProvider.GetRequiredService<ListInvoicesUseCase>();

            var result = await useCase.ExecuteAsync(new ListInvoicesQuery
            {
                BillingSource = BillingSource.Name,
                Secret = BillingSource.Secret,
                Serie = string.IsNullOrWhiteSpace(FilterSerie) ? null : FilterSerie,
                Year = FilterYear,
                Status = string.IsNullOrWhiteSpace(FilterStatus) ? null : FilterStatus,
                IssuedFrom = FilterIssuedFrom.HasValue
                    ? DateOnly.FromDateTime(FilterIssuedFrom.Value) : null,
                IssuedTo = FilterIssuedTo.HasValue
                    ? DateOnly.FromDateTime(FilterIssuedTo.Value) : null,
                RecipientExternalId = string.IsNullOrWhiteSpace(FilterRecipientExternalId) ? null : FilterRecipientExternalId,
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
        FilterSerie = null;
        FilterYear = null;
        FilterStatus = null;
        FilterIssuedFrom = null;
        FilterIssuedTo = null;
        FilterRecipientExternalId = null;
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
    void ViewDetail(InvoiceSummaryResult? invoice)
    {
        if (invoice is null) return;
        var vm = new InvoiceDetailViewModel(_scopeFactory, BillingSource, invoice.InvoiceNumber,
            goBack: () => _navigate(this));
        _navigate(vm);
        _ = vm.LoadAsync();
    }

    [RelayCommand]
    void CreateInvoice()
    {
        var vm = new CreateInvoiceViewModel(_scopeFactory, BillingSource, _masterDataStore,
            onCreated: () =>
            {
                _navigate(this);
                _ = LoadAsync();
            },
            onCancel: () => _navigate(this));
        _navigate(vm);
    }

    [RelayCommand]
    void Rectify(InvoiceSummaryResult? invoice)
    {
        if (invoice is null) return;
        var vm = new RectifyInvoiceViewModel(_scopeFactory, BillingSource, _masterDataStore,
            invoice.InvoiceNumber,
            onRectified: () =>
            {
                _navigate(this);
                _ = LoadAsync();
            },
            onCancel: () => _navigate(this));
        _navigate(vm);
        _ = vm.LoadAsync();
    }

    [RelayCommand]
    void Verify(InvoiceSummaryResult? invoice)
    {
        if (invoice is null) return;
        var vm = new VerifyInvoiceViewModel(_scopeFactory, BillingSource, invoice.InvoiceNumber,
            onBack: () => _navigate(this));
        _navigate(vm);
        _ = vm.VerifyAsync();
    }
}
