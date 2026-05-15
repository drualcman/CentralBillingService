namespace CentralBillingService.Domain.Interfaces;

public interface IInvoiceNumberGenerator
{
    Task<string> GenerateAsync(
        string BillingSource,
        CancellationToken cancellationToken);
}
