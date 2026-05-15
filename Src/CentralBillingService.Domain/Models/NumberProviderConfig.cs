namespace CentralBillingService.Domain.Models;

public sealed class NumberProviderConfig
{
    /// <summary>"Database" (default) or "ExternalApi".</summary>
    public string Type { get; init; } = "Database";

    public string? BaseUrl { get; init; }
    public string? ApiKey { get; init; }
}
