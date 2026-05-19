namespace CentralBillingService.Application.UseCases;

public class GenerateInvoiceReportUseCase(IInvoiceRepository repository,
        IInvoiceHasher hasher,
        ILogger<GenerateInvoiceReportUseCase> logger,
        BillingSourceRegistry registry,
        GetInvoiceUseCase invoiceUseCase)
{
    public async Task<ReportViewModel> GenerateInvoiceViewModel(GenerateInvoiceReportCommand command,
        CancellationToken cancellationToken)
    {
        var config = registry.GetConfig(command.BillingSource);

        var invoiceQuery = await invoiceUseCase.ExecuteAsync(new GetInvoiceQuery
        {
            BillingSource = command.BillingSource,
            InvoiceNumber = command.InvoiceNumber,
            Secret = config.Secret
        });

        Invoice? invoice;
        if (invoiceQuery.IsRectificative)
        {
            var rectificative = await repository.FindRectificativeByNumberAsync(command.BillingSource, invoiceQuery.InvoiceNumber, cancellationToken);
            if (rectificative is not null)
                invoice = Invoice.Reconstitute(rectificative.Id, rectificative.Number, rectificative.BillingSource, rectificative.Issuer,
                    rectificative.Recipient, rectificative.IssueDate, null, rectificative.CreatedAt, rectificative.Lines.ToList(),
                    rectificative.AppliedExchangeRate, rectificative.Hash, rectificative.PreviousHash, rectificative.Status,
                    rectificative.PaymentReference, null, rectificative.Notes, rectificative.TransactionData, rectificative.PaymentMethod,
                    rectificative.QrCodeBlobUrl);
            else
                invoice = null;
        }
        else
            invoice = await repository.FindByIdAsync(command.BillingSource, invoiceQuery.Id, cancellationToken);

        if (invoice is null)
        {
            logger.LogError("Invoice {InvoiceNumber} not found for report generation.", command.InvoiceNumber);
            throw new NotFoundException($"No invoice found for '{command.InvoiceNumber}'.");
        }

        invoice.VerifyIntegrity(hasher);

        if (invoice.HasTamper)
            logger.LogWarning(
                "DATA INTEGRITY WARNING: Invoice {InvoiceNumber} has been tampered with. Report will show warning banner.",
                command.InvoiceNumber);

        var logoUrl = string.IsNullOrWhiteSpace(config.Issuer.LogoUrl) ? "https://drualcman.blob.core.windows.net/content/SergiLogo.png" : config.Issuer.LogoUrl;

        return await GenerateInvoiceReport.BuildAsync(invoice, logoUrl);
    }
}
