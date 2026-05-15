namespace CentralBillingService.WPF.Models;

public sealed class BillingSourceSummary
{
    public required string Name { get; init; }
    public required string Secret { get; init; }
    public required string DisplayName { get; init; }
}
