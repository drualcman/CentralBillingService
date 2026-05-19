namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods to wire up all layers into the DI container.
/// Called once from the Azure Function startup (or from a test fixture).
///
/// Usage in Azure Function Program.cs:
///   builder.Services.AddBillingDomain(builder.Configuration);
///   builder.Services.AddBillingInfrastructure(builder.Configuration);
///   builder.Services.AddBillingApplication();
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers infrastructure implementations.
    /// Swap these out for production implementations without touching domain or application.
    /// </summary>
    public static IServiceCollection AddBillingInfrastructure(
        this IServiceCollection services)
    {
        //Iso 9001 Databases
        services.AddIso9001Databases();
        services.AddAuditLogCoreServices();
        services.AddIncidentReportCoreServices();
        services.AddReportingPresenterPdfServices();
        services.AddScoped<IIso9001, Iso9001Service>();

        // Hashing
        services.AddScoped<IInvoiceHasher, Sha256InvoiceHasher>();

        // Verification URL provider — points to the Blazor verify UI by default.
        // Swap for SpanishAeatVerificationUrlProvider when VeriFactu submission is live.
        services.AddSingleton<IInvoiceVerificationUrlProvider>(sp =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<CbsOptions>>().Value;
            return new SystemInvoiceVerificationUrlProvider(opts.VerifyUiBaseUrl);
        });

        // QR code generation
        services.AddSingleton<IQrCodeGenerator, CentralBillingService.Infrastructure.QrCode.QrCodeGenerator>();

        // Blob storage for QR code images
        services.AddSingleton<IBlobStorageService, CentralBillingService.Infrastructure.BlobStorage.AzureBlobStorageService>();

        // QR code job queue — sends generation jobs to Azure Storage Queue
        services.AddScoped<IJobQueue, AzureQueueJobQueue>();

        // Exchange rate
        services.AddHttpClient<ICurrencyConvertion, CurrencyConvertion>(
            client => client.BaseAddress = new Uri(new("https://open.er-api.com/v6/latest/"))
            );

        services.AddScoped<IExchangeRateProvider, ExchangeRateProviderAdapter>();

        // Repository and number provider factory — the factory selects the correct
        // IInvoiceNumberProvider per BillingSource based on NumberProviderConfig.Type.
        // Swap DatabaseInvoiceNumberProvider for an API-based one by setting Type = "ExternalApi".
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IInvoiceNumberProviderFactory, InvoiceNumberProviderFactory>();
        services.AddScoped<IInvoiceNumberProviderStrategy, DatabaseNumberProviderStrategy>();
        services.AddScoped<IInvoiceNumberProviderStrategy, ExternalApiNumberProviderStrategy>();
        services.AddScoped<IInvoiceEventDispatcher, InvoiceEventDispatcher>();

        // Result dispatch for queue-triggered invoice creation
        services.AddScoped<IInvoiceResultQueuePublisher, InvoiceResultQueuePublisher>();
        services.AddHttpClient<IInvoiceResultCallbackNotifier, InvoiceResultCallbackNotifier>();

        return services;
    }
}
