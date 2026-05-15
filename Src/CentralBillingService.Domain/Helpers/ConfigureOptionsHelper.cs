namespace CentralBillingService.Domain.Helpers;

public static class ConfigureOptionsHelper
{
    public static void ConfigureOptions<TOptions>(
        IServiceCollection services,
        Action<TOptions> configureOptions,
        string sectionKey) where TOptions : class
    {
        if (configureOptions is not null)
        {
            services.Configure(configureOptions);
        }
        else
        {
            services
                .AddOptions<TOptions>()
                .BindConfiguration(sectionKey);
        }
    }
}
