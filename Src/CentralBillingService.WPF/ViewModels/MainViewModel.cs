namespace CentralBillingService.WPF.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly LocalMasterDataStore _masterDataStore;
    private readonly AppSettingsService _appSettingsService;

    [ObservableProperty]
    ObservableCollection<BillingSourceSummary> billingSources = [];

    [ObservableProperty]
    BillingSourceSummary? selectedBillingSource;

    [ObservableProperty]
    object? currentView;

    public Visibility EmptyStateVisible =>
        CurrentView is null ? Visibility.Visible : Visibility.Collapsed;

    public MainViewModel(IServiceScopeFactory scopeFactory, IConfiguration config,
        LocalMasterDataStore masterDataStore, AppSettingsService appSettingsService)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _masterDataStore = masterDataStore;
        _appSettingsService = appSettingsService;

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
        CurrentView = new MasterDataViewModel(_masterDataStore);
        OnPropertyChanged(nameof(EmptyStateVisible));
    }
}
