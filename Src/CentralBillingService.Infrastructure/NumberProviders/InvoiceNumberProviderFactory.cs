namespace CentralBillingService.Infrastructure.NumberProviders;

public sealed class InvoiceNumberProviderFactory : IInvoiceNumberProviderFactory
{
    private readonly IReadOnlyDictionary<string, IInvoiceNumberProviderStrategy> _strategies;

    public InvoiceNumberProviderFactory(IEnumerable<IInvoiceNumberProviderStrategy> strategies)
    {
        _strategies = strategies.ToDictionary(s => s.ProviderType);
    }

    public IInvoiceNumberProvider GetFor(BillingSourceConfig config) =>
        _strategies.TryGetValue(config.NumberProvider.Type, out var strategy)
            ? strategy.Create(config.NumberProvider)
            : throw new InvalidOperationException(
                $"No number provider strategy registered for type '{config.NumberProvider.Type}'. " +
                $"Register an {nameof(IInvoiceNumberProviderStrategy)} with ProviderType = '{config.NumberProvider.Type}'.");
}
