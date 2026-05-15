namespace CentralBillingService.Infrastructure.NumberProviders;

/// <summary>
/// Placeholder for government-issued invoice number providers (e.g. Mexico SAT CFDI).
/// Implement this class when an external API authority is required for a billing source.
/// </summary>
public sealed class ExternalApiInvoiceNumberProvider : IInvoiceNumberProvider
{
    private readonly NumberProviderConfig _config;

    public ExternalApiInvoiceNumberProvider(NumberProviderConfig config)
    {
        _config = config;
    }

    public Task<int> ReserveNextNumberAsync(
        string billingSource, string serie, int year,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException(
            $"ExternalApi number provider is not yet implemented. " +
            $"Configure a concrete implementation for billing source '{billingSource}'.");
}
