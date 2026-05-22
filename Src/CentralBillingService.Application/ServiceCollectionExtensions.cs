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
    /// Registers application use cases.
    /// </summary>
    public static IServiceCollection AddBillingApplication(
        this IServiceCollection services)
    {
        services.AddApplicationEvents();
        services.AddScoped<CreateInvoiceUseCase>();
        services.AddScoped<ICreateInvoiceUseCase>(sp => sp.GetRequiredService<CreateInvoiceUseCase>());
        services.AddScoped<GetInvoiceUseCase>();
        services.AddScoped<ListInvoicesUseCase>();
        services.AddScoped<RectifyInvoiceUseCase>();
        services.AddScoped<VerifyInvoiceIntegrityUseCase>();
        services.AddScoped<CheckInvoiceIntegrityUseCase>();
        services.AddScoped<ProcessQueuedCreateInvoiceUseCase>();
        services.AddScoped<GenerateInvoiceQrUseCase>();
        services.AddScoped<GenerateInvoiceReportUseCase>();
        services.AddScoped<GenerateInvoiceUseCase>();
        services.AddScoped<PublicListInvoicesUseCase>();
        services.AddScoped<GetInvoicesSummaryUseCase>();
        services.AddScoped<SendInvoicePdfByEmailUseCase>();

        return services;
    }
}
