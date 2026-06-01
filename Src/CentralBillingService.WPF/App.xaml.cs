using CentralBillingService.Persistence.SqlServer.Admin;
using Iso9001Client;

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
                      .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
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
                    opts => cfg.GetSection(CbsOptions.SectionKey).Bind(opts),
                    mail => cfg.GetSection(EmailOptions.SectionKey).Bind(mail));

                services.AddBillingApplication();
                services.AddBillingInfrastructure(
                    iso9001 => cfg.GetSection(Iso9001ClientOptions.SectionKey).Bind(iso9001)
                    );

                services.AddSqlServerPersistence(opts =>
                    cfg.GetSection(DatabaseOptions.SectionKey).Bind(opts));

                services.AddSingleton<IConfiguration>(cfg);

                // Master data (local JSON store)
                services.AddSingleton<LocalMasterDataStore>();
                services.AddSingleton<AppSettingsService>();

                // Admin service — WPF only, not registered in the Azure Function
                services.AddSingleton<ISequenceAdminService, SequenceAdminService>();

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

        await _host.Services.ApplyMigrationsAsync();

        var window = _host.Services.GetRequiredService<MainWindow>();
        window.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        using (_host)
            await _host.StopAsync(TimeSpan.FromSeconds(5));

        base.OnExit(e);
    }
}
