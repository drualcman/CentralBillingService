namespace CentralBillingService.Infrastructure.NumberProviders;

public interface IInvoiceNumberProviderStrategy
{
    string ProviderType { get; }
    IInvoiceNumberProvider Create(NumberProviderConfig config);
}
