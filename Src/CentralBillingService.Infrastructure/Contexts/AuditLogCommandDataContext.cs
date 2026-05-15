namespace CentralBillingService.Infrastructure.Contexts;

internal sealed class AuditLogCommandDataContext(IIso9001Context context)
    : IWritableAuditLogDataContext
{
    public Task AddAsync(AuditLog auditLog) => context.AddAsync(auditLog);

    public Task SaveChangesAsync() => context.SaveChangesAsync();
}

