namespace Microsoft.Extensions.DependencyInjection;

public static partial class DependencyContainer
{
    public static IServiceCollection AddApplicationEvents(this IServiceCollection services)
    {
        services.AddScoped<IDomainEventHandler<GenerateInvoiceArgs>, GenerateInvoiceQrHandler>();

        return services;
    }
}
