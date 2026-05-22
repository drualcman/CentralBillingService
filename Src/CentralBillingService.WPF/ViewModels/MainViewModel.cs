using CentralBillingService.Domain.Services;
using CentralBillingService.Persistence.SqlServer.Admin;

namespace CentralBillingService.WPF.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly LocalMasterDataStore _masterDataStore;
    private readonly AppSettingsService _appSettingsService;
    private readonly BillingSourceRegistry _registry;
    private readonly ISequenceAdminService _sequenceAdminService;

    [ObservableProperty]
    ObservableCollection<BillingSourceSummary> billingSources = [];

    [ObservableProperty]
    BillingSourceSummary? selectedBillingSource;

    [ObservableProperty]
    object? currentView;

    public Visibility EmptyStateVisible =>
        CurrentView is null ? Visibility.Visible : Visibility.Collapsed;

    public MainViewModel(IServiceScopeFactory scopeFactory, IConfiguration config,
        LocalMasterDataStore masterDataStore, AppSettingsService appSettingsService,
        BillingSourceRegistry registry, ISequenceAdminService sequenceAdminService)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _masterDataStore = masterDataStore;
        _appSettingsService = appSettingsService;
        _registry = registry;
        _sequenceAdminService = sequenceAdminService;

        LoadBillingSources();
    }

    private void LoadBillingSources()
    {
        var sources = _config.GetSection("CbsOptions:BillingSources").GetChildren();
        foreach (var s in sources)
        {
            var name = s["BillingSource"] ?? "";
            if (string.IsNullOrEmpty(name))
                continue;
            BillingSources.Add(new BillingSourceSummary
            {
                Name = name,
                Secret = s["Secret"] ?? "",
                DisplayName = s["Issuer:TradeName"] ?? s["Issuer:LegalName"] ?? name,
            });
        }
    }

    // Refresh the sidebar list from the file (called after editing billing sources).
    // Secrets-only sources (not in appsettings.json) are kept from the original config.
    private void ReloadBillingSources()
    {
        BillingSources.Clear();

        var fromFile = _appSettingsService.LoadBillingSources();
        foreach (var s in fromFile)
            BillingSources.Add(new BillingSourceSummary
            {
                Name = s.Key,
                Secret = s.Secret,
                DisplayName = s.DisplayName,
            });

        var fileKeys = fromFile.Select(s => s.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var s in _config.GetSection("CbsOptions:BillingSources").GetChildren())
        {
            var name = s["BillingSource"] ?? "";
            if (string.IsNullOrEmpty(name) || fileKeys.Contains(name))
                continue;
            BillingSources.Add(new BillingSourceSummary
            {
                Name = name,
                Secret = s["Secret"] ?? "",
                DisplayName = s["Issuer:TradeName"] ?? s["Issuer:LegalName"] ?? name,
            });
        }
    }

    partial void OnSelectedBillingSourceChanged(BillingSourceSummary? value)
    {
        if (value is null)
            return;
        var vm = new InvoicesViewModel(_scopeFactory, value, _masterDataStore, NavigateTo);
        CurrentView = vm;
        OnPropertyChanged(nameof(EmptyStateVisible));
        _ = vm.LoadAsync();
    }

    public void NavigateTo(object viewModel)
    {
        CurrentView = viewModel;
        OnPropertyChanged(nameof(EmptyStateVisible));
    }

    [RelayCommand]
    void OpenGlobalInvoices()
    {
        SelectedBillingSource = null;
        var vm = new GlobalInvoicesViewModel(_scopeFactory, _registry, NavigateTo);
        CurrentView = vm;
        OnPropertyChanged(nameof(EmptyStateVisible));
        _ = vm.LoadAsync();
    }

    [RelayCommand]
    void OpenBillingSummary()
    {
        SelectedBillingSource = null;
        var vm = new BillingSummaryViewModel(_scopeFactory, _registry);
        CurrentView = vm;
        OnPropertyChanged(nameof(EmptyStateVisible));
        _ = vm.LoadAsync();
    }

    [RelayCommand]
    void OpenBillingSources()
    {
        SelectedBillingSource = null;
        CurrentView = new BillingSourcesViewModel(_appSettingsService, ReloadBillingSources);
        OnPropertyChanged(nameof(EmptyStateVisible));
    }

    [RelayCommand]
    void OpenMasterData()
    {
        SelectedBillingSource = null;
        var billingSources = BillingSources.Select(s => s.Name).ToList();
        var sequenceAdmin = new SequenceAdminViewModel(_sequenceAdminService, billingSources);
        CurrentView = new MasterDataViewModel(_masterDataStore, sequenceAdmin);
        OnPropertyChanged(nameof(EmptyStateVisible));
    }
}
