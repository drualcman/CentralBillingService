namespace CentralBillingService.Infrastructure.Contexts;

internal sealed class AuditLogQueryDataContext(IIso9001Context context)
    : IQueryableAuditLogDataContext
{
    public IQueryable<AuditLogReadModel> AuditLogs => context.AuditLogsQuery.Select(entity => new AuditLogReadModel
    {
        LogId = entity.Id.ToString(),
        EntityId = entity.EntityId,
        CompanyId = entity.CompanyId,
        Action = entity.Action,
        PerformedBy = entity.PerformedBy,
        Timestamp = entity.Timestamp,
        CreatedAt = entity.CreatedAt,
        Details = entity.Details
    });

    public Task<IEnumerable<AuditLogReadModel>> ToListAsync(
           Expression<Func<AuditLogReadModel, bool>> filter = null,
           Func<IQueryable<AuditLogReadModel>, IOrderedQueryable<AuditLogReadModel>> orderBy = null)
    {
        IQueryable<AuditLogReadModel> query = AuditLogs;

        if (filter != null)
        {
            query = query.Where(filter);
        }

        if (orderBy != null)
        {
            query = orderBy(query);
        }

        IEnumerable<AuditLogReadModel> result = query.ToList();
        return Task.FromResult(result);
    }
}
