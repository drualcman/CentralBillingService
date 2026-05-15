namespace CentralBillingService.Infrastructure.Entities;

internal sealed class AuditLogEntity
{
    public Guid Id { get; set; }
    public string EntityId { get; set; }
    public string CompanyId { get; set; } = "CentralBillingService";
    public string Action { get; set; }
    public string PerformedBy { get; set; }
    public DateTime Timestamp { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Details { get; set; }
    public string Data { get; set; }
}
