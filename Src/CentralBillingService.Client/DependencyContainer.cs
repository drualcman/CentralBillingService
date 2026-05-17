namespace Microsoft.Extensions.DependencyInjection;

public static partial class DependencyContainer
{
    public static IServiceCollection AddCbsServices(this IServiceCollection services,
           Action<CbsOptions> options)
    {
        CbsOptions values = new CbsOptions();
        options(values);
        ConfigureOptionsHelper.ConfigureOptions(services, options, CbsOptions.SectionKey);

        if (string.IsNullOrWhiteSpace(values.Uri))
            throw new ArgumentException("CbsOptions.Uri must be configured.", nameof(options));

        services.AddTransient<AuthorizationHandler>();
        services.AddHttpClient<ICbsService, CbsHttpClient>(client => client.BaseAddress = new Uri(values.Uri))
            .AddHttpMessageHandler<AuthorizationHandler>();
        return services;
    }
}
