namespace CentralBillingService.Infrastructure.Entities;

internal sealed class IncidentReportEntity
{

    public Guid Id { get; set; }
    public string CompanyId { get; set; } = "CentralBillingService";
    public string EntityId { get; set; }
    public DateTime ReportedAt { get; set; }
    public string UserId { get; set; }
    public string Description { get; set; }
    public string AffectedProcess { get; set; }
    public string Severity { get; set; }
    public string Data { get; set; }
}
