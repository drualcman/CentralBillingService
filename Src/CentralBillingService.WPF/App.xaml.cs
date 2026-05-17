using CentralBillingService.Domain.Entities;
using CentralBillingService.Domain.Models;
using CentralBillingService.Domain.ValueObjects;
using CentralBillingService.Persistence.SqlServer.Options;
using CentralBillingService.WPF.Services;

namespace CentralBillingService.WPF;

public partial class App : System.Windows.Application
{
    private IHost _host = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((_, config) =>
            {
                config.SetBasePath(AppContext.BaseDirectory)
                      .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
#if DEBUG
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets<App>()
#endif
            ;
            })
            .ConfigureServices((context, services) =>
            {
                var cfg = context.Configuration;

                // BillingParty has a private constructor so the config binder cannot
                // instantiate it automatically. We build each BillingSourceConfig manually.
                services.AddBillingDomain(
                    opts => cfg.GetSection(CbsOptions.SectionKey).Bind(opts));

                services.AddBillingApplication();
                services.AddBillingInfrastructure();

                services.AddSqlServerPersistence(opts =>
                    cfg.GetSection(DatabaseOptions.SectionKey).Bind(opts));

                services.AddSingleton<IConfiguration>(cfg);

                // Master data (local JSON store)
                services.AddSingleton<LocalMasterDataStore>();
                services.AddSingleton<AppSettingsService>();

                // ViewModels
                services.AddSingleton<MainViewModel>();
                services.AddTransient<InvoicesViewModel>();
                services.AddTransient<InvoiceDetailViewModel>();
                services.AddTransient<CreateInvoiceViewModel>();
                services.AddTransient<RectifyInvoiceViewModel>();
                services.AddTransient<VerifyInvoiceViewModel>();

                // Main window
                services.AddSingleton<MainWindow>();
            })
            .Build();

        await _host.StartAsync();

        var window = _host.Services.GetRequiredService<MainWindow>();
        window.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        using (_host)
            await _host.StopAsync(TimeSpan.FromSeconds(5));

        base.OnExit(e);
    }

    private static BillingSourceConfig BuildBillingSourceConfig(IConfigurationSection s)
    {
        var i = s.GetSection("Issuer");
        return new BillingSourceConfig
        {
            BillingSource = s["BillingSource"] ?? "",
            Secret = s["Secret"] ?? "",
            Issuer = new IssuerConfig
            {
                LegalName = i["LegalName"] ?? "",
                TradeName = i["TradeName"],
                TaxIdValue = i["TaxIdValue"] ?? "",
                TaxIdCountryCode = i["TaxIdCountryCode"] ?? "ES",
                Email = i["Email"] ?? "",
                Phone = i["Phone"],
                Website = i["Website"],
                AddressLine1 = i["AddressLine1"] ?? "",
                AddressLine2 = i["AddressLine2"],
                City = i["City"] ?? "",
                Province = i["Province"],
                PostalCode = i["PostalCode"] ?? "",
                AddressCountryCode = i["AddressCountryCode"] ?? "ES",
            },
        };
    }
}
