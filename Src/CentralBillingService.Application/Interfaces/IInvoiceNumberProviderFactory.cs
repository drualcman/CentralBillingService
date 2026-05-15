namespace CentralBillingService.Application.Interfaces;

public interface IInvoiceNumberProviderFactory
{
    IInvoiceNumberProvider GetFor(BillingSourceConfig config);
}
