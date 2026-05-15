namespace CentralBillingService.Application.Interfaces;

public interface ICreateInvoiceUseCase
{
    Task<InvoiceResult> ExecuteAsync(
        CreateInvoiceCommand command,
        CancellationToken cancellationToken = default);
}
