namespace CentralBillingService.Infrastructure.Services;

internal class InvoiceEventDispatcher(IDomainEventHandler<GenerateInvoiceArgs> eventHandler) : IInvoiceEventDispatcher
{
    public async Task InvoiceCreatedAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        // 8. Enqueue QR image generation (best-effort — never rolls back the invoice)
        await eventHandler.Handle(CreateQrCommand(invoice), cancellationToken);
    }
    public async Task InvoiceRectifiedAsync(RectificativeInvoice rectificative, CancellationToken cancellationToken = default)
    {
        // 8. Enqueue QR image generation (best-effort — never rolls back the invoice)
        await eventHandler.Handle(CreateQrCommand(rectificative), cancellationToken);
    }

    private static GenerateInvoiceArgs CreateQrCommand(Invoice invoice) => new GenerateInvoiceArgs(
                    invoice.Number.Value,
                    invoice.BillingSource,
                    invoice.Hash,
                    invoice.IssueDate,
                    invoice.TotalEur.Amount,
                    invoice.Issuer.TaxId.Value);

    private static GenerateInvoiceArgs CreateQrCommand(RectificativeInvoice invoice) => new GenerateInvoiceArgs(
                    invoice.Number.Value,
                    invoice.BillingSource,
                    invoice.Hash,
                    invoice.IssueDate,
                    invoice.TotalEur.Amount,
                    invoice.Issuer.TaxId.Value);
}
