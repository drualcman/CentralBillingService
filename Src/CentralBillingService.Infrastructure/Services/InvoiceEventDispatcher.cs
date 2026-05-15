namespace CentralBillingService.Infrastructure.Services;

internal class InvoiceEventDispatcher : IInvoiceEventDispatcher
{
    public Task InvoiceCreatedAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
    public Task InvoiceRectifiedAsync(RectificativeInvoice rectificative, Invoice updatedOriginal, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
