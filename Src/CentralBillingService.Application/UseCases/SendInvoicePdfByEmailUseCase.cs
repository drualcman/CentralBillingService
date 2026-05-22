namespace CentralBillingService.Application.UseCases;

public sealed class SendInvoicePdfByEmailUseCase
{
    private readonly IInvoiceRepository _repository;
    private readonly BillingSourceRegistry _registry;
    private readonly IBlobStorageService _blobStorage;
    private readonly IMailService _mailService;
    private readonly IIso9001 _iso9001;

    public SendInvoicePdfByEmailUseCase(
        IInvoiceRepository repository,
        BillingSourceRegistry registry,
        IBlobStorageService blobStorage,
        IMailService mailService,
        IIso9001 iso9001)
    {
        _repository = repository;
        _registry = registry;
        _blobStorage = blobStorage;
        _mailService = mailService;
        _iso9001 = iso9001;
    }

    public async Task<SendInvoicePdfByEmailResult> ExecuteAsync(
        SendInvoicePdfByEmailQuery query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.BillingSource))
            throw new ArgumentException("BillingSource is required.", nameof(query.BillingSource));
        if (string.IsNullOrWhiteSpace(query.InvoiceNumber))
            throw new ArgumentException("InvoiceNumber is required.", nameof(query.InvoiceNumber));

        await _iso9001.Register(query.InvoiceNumber, this, "Send invoice PDF by email requested", query);

        _registry.GetConfig(query.BillingSource);

        var invoice = await _repository.FindByNumberAsync(
            query.BillingSource, query.InvoiceNumber, cancellationToken);

        if (invoice is not null)
            return await SendAsync(query, invoice.Recipient, cancellationToken);

        var rectificative = await _repository.FindRectificativeByNumberAsync(
            query.BillingSource, query.InvoiceNumber, cancellationToken);

        if (rectificative is not null)
            return await SendAsync(query, rectificative.Recipient, cancellationToken);

        throw new InvoiceNotFoundException(query.InvoiceNumber);
    }

    private async Task<SendInvoicePdfByEmailResult> SendAsync(
        SendInvoicePdfByEmailQuery query,
        BillingParty recipient,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(recipient.Email))
        {
            await _iso9001.Register(query.InvoiceNumber, this,
                $"PDF send skipped — recipient '{recipient.DisplayName}' has no email");
            return SendInvoicePdfByEmailResult.NoEmail(recipient.DisplayName);
        }

        var blobName = InvoiceHelper.GetInvoiceFileName(query.BillingSource, query.InvoiceNumber);
        var pdfBytes = await _blobStorage.DownloadInvoiceAsync(blobName, cancellationToken);

        if (pdfBytes is null)
        {
            await _iso9001.Register(query.InvoiceNumber, this,
                $"PDF blob not found for invoice '{query.InvoiceNumber}'");
            return SendInvoicePdfByEmailResult.PdfNotFound(query.InvoiceNumber);
        }

        var content = $@"
            <p>Estimado/a <strong>[contact]</strong>,</p>
            <p>Le enviamos adjunto su factura <strong>{query.InvoiceNumber}</strong> en formato PDF.</p>
            <p>Si tiene alguna duda o consulta sobre este documento, no dude en ponerse en contacto con nosotros.</p>
            <p>Muchas gracias por su confianza.</p>
            <hr style=""border:none;border-top:1px solid #e0e0e0;margin:20px 0;"" />
            <p>Dear <strong>[contact]</strong>,</p>
            <p>Please find your invoice <strong>{query.InvoiceNumber}</strong> attached as a PDF.</p>
            <p>Should you have any questions regarding this document, please do not hesitate to contact us.</p>
            <p>Thank you for your trust.</p>
            ";

        var email = new Email(
            subject: $"Factura {query.InvoiceNumber} · Invoice {query.InvoiceNumber}",
            content: content);
        email.AddAddressee(recipient.Email);
        email.AddAttach(new Attachment($"{query.InvoiceNumber}.pdf", pdfBytes));

        await _mailService.Send(email, cancellationToken);

        await _iso9001.Register(query.InvoiceNumber, this, $"Invoice PDF queued for delivery to {recipient.Email}");

        return SendInvoicePdfByEmailResult.Sent(recipient.Email);
    }
}
