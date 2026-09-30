namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registers the VeriFactu fiscal registrar. Call after AddBillingInfrastructure(); the
/// generic FiscalRegistrarFactory will select this registrar for billing sources whose
/// Registrar.Type = "VeriFactu". Everything VeriFactu-specific stays inside this project.
/// </summary>
public static class VeriFactuServiceCollectionExtensions
{
    /// <summary>
    /// Registers the VeriFactu registrar builder. There is no global VeriFactu configuration:
    /// each billing source carries its own certificate/endpoint in its <c>Registrar.Settings</c>,
    /// and the builder produces a registrar bound to that source. The EF context reuses the CbsDb
    /// connection for the huella chain + submission state.
    /// </summary>
    public static IServiceCollection AddVeriFactuRegistrar(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<VeriFactuDbContext>(o => o.UseSqlServer(connectionString));
        services.AddScoped<IVeriFactuStore, SqlVeriFactuStore>();
        services.AddSingleton<InvoiceToVeriFactuMapper>();
        services.AddScoped<IFiscalRegistrarBuilder, VeriFactuFiscalRegistrarBuilder>();
        return services;
    }
}
