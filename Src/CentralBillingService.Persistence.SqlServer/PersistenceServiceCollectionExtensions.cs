namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registers the SQL Server persistence layer.
/// Called from the Azure Function (or any host) after AddBillingInfrastructure().
///
/// Usage in Program.cs:
///   builder.Services.AddSqlServerPersistence(builder.Configuration);
///
/// Connection string in appsettings.json / Azure Key Vault:
///   "ConnectionStrings": {
///     "BillingDb": "Server=...;Database=CentralBilling;..."
///   }
/// </summary>
public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddSqlServerPersistence(
        this IServiceCollection services,
           Action<DatabaseOptions> options)
    {
        ConfigureOptionsHelper.ConfigureOptions(services, options, DatabaseOptions.SectionKey);

        //services.AddDbContext<IIso9001Context, Iso9001Context>(ServiceLifetime.Scoped);

        services.AddScoped<IIso9001Context>(provider =>
            provider.GetRequiredService<Iso9001Context>());

        services.AddDbContext<IInvoiceReadContext, SqlInvoiceReadContext>(ServiceLifetime.Scoped);
        services.AddDbContext<IInvoiceWriteContext, SqlInvoiceWriteContext>(ServiceLifetime.Scoped);

        return services;
    }
}
