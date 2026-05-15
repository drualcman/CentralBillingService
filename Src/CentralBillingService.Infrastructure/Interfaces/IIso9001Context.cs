namespace CentralBillingService.Infrastructure.Interfaces;

internal interface IIso9001Context
{
    IQueryable<AuditLogEntity> AuditLogsQuery { get; }
    IQueryable<IncidentReportEntity> IncidentReportsQuery { get; }
    Task AddAsync(AuditLog auditLog);
    Task AddAsync(IncidentReport incidentReport);
    Task SaveChangesAsync();
}