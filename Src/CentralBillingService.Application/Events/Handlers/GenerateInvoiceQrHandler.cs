namespace CentralBillingService.Application.Events.Handlers;

internal class GenerateInvoiceQrHandler(IJobQueue jobQueue, ILogger<GenerateInvoiceQrHandler> logger) : IDomainEventHandler<GenerateInvoiceArgs>
{
    public async Task Handle(GenerateInvoiceArgs data, CancellationToken cancellationToken)
    {
        try
        {
            await jobQueue.EnqueueQrAsync(CreateQrCommand(data), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex.Message);
        }
    }
    private static GenerateInvoiceQrCommand CreateQrCommand(GenerateInvoiceArgs invoice) => new GenerateInvoiceQrCommand(
                    invoice.InvoiceNumber,
                    invoice.BillingSource,
                    invoice.Hash,
                    invoice.IssueDate,
                    invoice.TotalEurAmount,
                    invoice.RecipientTaxId);
}
