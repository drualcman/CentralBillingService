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
        services.AddDbContext<IInvoiceReadContext, SqlInvoiceReadContext>(ServiceLifetime.Scoped);
        services.AddDbContext<IInvoiceWriteContext, SqlInvoiceWriteContext>(ServiceLifetime.Scoped);

        return services;
    }
}
