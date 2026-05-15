namespace CentralBillingService.Application.Interfaces;

public interface IInvoiceResultQueuePublisher
{
    Task PublishAsync(
        InvoiceResult result,
        ResultQueueConfig config,
        CancellationToken cancellationToken = default);
}
