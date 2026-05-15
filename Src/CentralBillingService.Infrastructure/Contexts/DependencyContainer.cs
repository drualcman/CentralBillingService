namespace Microsoft.Extensions.DependencyInjection;

internal static partial class DependencyContainer
{
    public static IServiceCollection AddIso9001Databases(this IServiceCollection services)
    {
        services.AddScoped<IWritableAuditLogDataContext, AuditLogCommandDataContext>();
        services.AddScoped<IQueryableAuditLogDataContext, AuditLogQueryDataContext>();
        services.AddScoped<IQueryableIncidentReportDataContext, IncidentReportQueryDataContext>();
        services.AddScoped<IWritableIncidentReportDataContext, IncidentReportCommandDataContext>();
        return services;
    }
}