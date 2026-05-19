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
    /// Registers domain services. No infrastructure dependencies.
    /// </summary>
    public static IServiceCollection AddBillingDomain(
        this IServiceCollection services,
        Action<CbsOptions> options)
    {
        // Build the registry once from config — singleton for the lifetime of the app    
        ConfigureOptionsHelper.ConfigureOptions(services, options, CbsOptions.SectionKey);
        services.AddSingleton<BillingSourceRegistry>();
        services.AddScoped<CreateInvoiceService>();
        services.AddScoped<RectifyInvoiceService>();
        services.AddScoped(typeof(IDomainEventHub<>), typeof(DomainEventHub<>));

        return services;
    }
}
