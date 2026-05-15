namespace CentralBillingService.Infrastructure.NumberProviders;

public sealed class ExternalApiNumberProviderStrategy : IInvoiceNumberProviderStrategy
{
    public string ProviderType => "ExternalApi";

    public IInvoiceNumberProvider Create(NumberProviderConfig config) =>
        new ExternalApiInvoiceNumberProvider(config);
}
