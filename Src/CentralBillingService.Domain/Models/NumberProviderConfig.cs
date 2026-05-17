namespace CentralBillingService.Domain.Models;

public sealed class NumberProviderConfig
{
    /// <summary>"Database" (default) or "ExternalApi".</summary>
    public string Type { get; set; } = "Database";

    public string? BaseUrl { get; set; }
    public string? ApiKey { get; set; }
}
