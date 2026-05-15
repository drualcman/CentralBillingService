namespace CentralBillingService.Infrastructure.Contexts;

internal sealed class IncidentReportQueryDataContext(IIso9001Context context)
    : IQueryableIncidentReportDataContext
{
    IQueryable<IncidentReportReadModel> IncidentReportsQuery => context.IncidentReportsQuery.Select(entity => new IncidentReportReadModel
    {
        Id = entity.Id.ToString(),
        EntityId = entity.EntityId,
        CompanyId = entity.CompanyId,
        ReportedAt = entity.ReportedAt,
        UserId = entity.UserId,
        Description = entity.Description,
        AffectedProcess = entity.AffectedProcess,
        Severity = entity.Severity,
        Data = entity.Data
    });

    public Task<IEnumerable<IncidentReportReadModel>> ToListAsync(
        Expression<Func<IncidentReportReadModel, bool>> filter = null,
        Func<IQueryable<IncidentReportReadModel>, IOrderedQueryable<IncidentReportReadModel>> orderBy = null)
    {
        IQueryable<IncidentReportReadModel> query =
            IncidentReportsQuery
                   .Select(entity => new IncidentReportReadModel
                   {
                       Id = entity.Id.ToString(),
                       EntityId = entity.EntityId,
                       CompanyId = entity.CompanyId,
                       ReportedAt = entity.ReportedAt,
                       UserId = entity.UserId,
                       Description = entity.Description,
                       AffectedProcess = entity.AffectedProcess,
                       Severity = entity.Severity,
                       Data = entity.Data
                   });

        if (filter != null)
        {
            query = query.Where(filter);
        }

        if (orderBy != null)
        {
            query = orderBy(query);
        }

        IEnumerable<IncidentReportReadModel> result = query.ToList();
        return Task.FromResult(result);
    }
}
