namespace CentralBillingService.Application.Interfaces;

public interface IInvoiceResultCallbackNotifier
{
    Task NotifyAsync(
        InvoiceResult result,
        CallbackConfig config,
        CancellationToken cancellationToken = default);
}
