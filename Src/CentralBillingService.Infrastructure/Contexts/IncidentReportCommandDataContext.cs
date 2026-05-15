namespace CentralBillingService.Infrastructure.Contexts;

internal sealed class IncidentReportCommandDataContext(IIso9001Context context)
    : IWritableIncidentReportDataContext
{

    public Task AddAsync(IncidentReport incidentReport) => context.AddAsync(incidentReport);

    public Task SaveChangesAsync() => context.SaveChangesAsync();
}
