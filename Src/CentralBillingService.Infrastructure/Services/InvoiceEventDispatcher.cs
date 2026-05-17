namespace CentralBillingService.Infrastructure.Services;

internal class InvoiceEventDispatcher(IQrCodeJobQueue qrJobQueue) : IInvoiceEventDispatcher
{
    public async Task InvoiceCreatedAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        // 8. Enqueue QR image generation (best-effort — never rolls back the invoice)
        await EnqueueQrCodeSafelyAsync(CreateQrCommand(invoice), cancellationToken);
    }
    public async Task InvoiceRectifiedAsync(RectificativeInvoice rectificative, Invoice? updatedOriginal, CancellationToken cancellationToken = default)
    {
        // 8. Enqueue QR image generation (best-effort — never rolls back the invoice)
        await EnqueueQrCodeSafelyAsync(CreateQrCommand(rectificative), cancellationToken);
    }

    private async Task EnqueueQrCodeSafelyAsync(GenerateInvoiceQrCommand qrCommand, CancellationToken cancellationToken)
    {
        try
        {
            await qrJobQueue.EnqueueAsync(qrCommand, cancellationToken);
        }
        catch (Exception ex)
        {
            // Non-critical — the invoice is persisted with its pre-computed QR URL.
            // The QR image will be absent until the job is re-queued or retried.
            // TODO: inject ILogger<CreateInvoiceUseCase> and log ex here.
            _ = ex;
        }
    }

    private static GenerateInvoiceQrCommand CreateQrCommand(Invoice invoice) => new GenerateInvoiceQrCommand(
                    invoice.Number.Value,
                    invoice.BillingSource,
                    invoice.Hash,
                    invoice.IssueDate,
                    invoice.TotalEur.Amount,
                    invoice.Issuer.TaxId.Value);

    private static GenerateInvoiceQrCommand CreateQrCommand(RectificativeInvoice invoice) => new GenerateInvoiceQrCommand(
                    invoice.Number.Value,
                    invoice.BillingSource,
                    invoice.Hash,
                    invoice.IssueDate,
                    invoice.TotalEur.Amount,
                    invoice.Issuer.TaxId.Value);
}
