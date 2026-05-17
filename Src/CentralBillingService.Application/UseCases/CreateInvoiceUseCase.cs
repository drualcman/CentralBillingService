namespace CentralBillingService.Application.UseCases;

/// <summary>
/// Orchestrates the full invoice creation flow:
///
///   1. Validate the command
///   2. Reserve the next sequence number atomically (via IInvoiceNumberProviderFactory)
///   3. Get the previous hash for the VeriFactu chain
///   4. Map the command to a domain request
///   5. Delegate to the domain service (business rules, hash computation)
///   6. Compute QR blob URL deterministically and attach to invoice
///   7. Persist the resulting invoice (with QR URL already set)
///   8. Enqueue QR image generation (async, best-effort — never rolls back)
///   9. Dispatch events (PDF generation, email, VeriFactu notification, etc.)
///  10. Return the result DTO to the caller
///
/// This class knows about persistence and events.
/// It does NOT contain business rules — those live in the domain.
/// It does NOT know about HTTP, Azure Functions, or any transport.
/// </summary>
public sealed class CreateInvoiceUseCase : ICreateInvoiceUseCase
{
    private readonly CreateInvoiceService _domainService;
    private readonly BillingSourceRegistry _registry;
    private readonly IInvoiceRepository _repository;
    private readonly IInvoiceEventDispatcher _eventDispatcher;
    private readonly IInvoiceNumberProviderFactory _numberProviderFactory;
    private readonly IBlobStorageService _blobStorage;

    public CreateInvoiceUseCase(
        CreateInvoiceService domainService,
        BillingSourceRegistry registry,
        IInvoiceRepository repository,
        IInvoiceEventDispatcher eventDispatcher,
        IInvoiceNumberProviderFactory numberProviderFactory,
        IBlobStorageService blobStorage)
    {
        _domainService = domainService;
        _registry = registry;
        _repository = repository;
        _eventDispatcher = eventDispatcher;
        _numberProviderFactory = numberProviderFactory;
        _blobStorage = blobStorage;
    }

    public async Task<InvoiceResult> ExecuteAsync(
        CreateInvoiceCommand command,
        CancellationToken cancellationToken = default)
    {
        // 1. Application-level validation
        Validate(command);

        // 2. Validate that the billing source exists and get config
        var config = _registry.GetConfig(command.BillingSource, command.Secret);
        var numberProvider = _numberProviderFactory.GetFor(config);

        var issueDate = command.IssueDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        // 3. Reserve the next sequence number — durable and atomic.
        //    This also implicitly locks the BillingSource+Serie+Year slot
        //    so concurrent requests cannot get the same number.
        var reservedNumber = await numberProvider.ReserveNextNumberAsync(
            command.BillingSource,
            command.Serie,
            issueDate.Year,
            cancellationToken);

        // 4. Get the previous hash for the chain — same key: BillingSource+Serie+Year
        var previousHash = await _repository.GetLastHashAsync(
            command.BillingSource,
            command.Serie,
            issueDate.Year,
            cancellationToken);

        // 5. Map command → domain request
        var domainRequest = MapToDomainRequest(command);

        // 6. Domain service: exchange rate, hash computation, immutability rules
        var invoice = await _domainService.ExecuteAsync(
            domainRequest, reservedNumber, previousHash, cancellationToken);

        // Compute the QR blob URL deterministically from the invoice number and attach it
        // before persisting — the URL is stable regardless of when the image is generated.
        var blobName = $"qr/{invoice.BillingSource}/{invoice.Number.Value}.png";
        invoice.AttachQrCode(_blobStorage.GetBlobUrl(blobName));

        // 7. Persist — the invoice is stored with its QR URL already set
        await _repository.SaveAsync(invoice, cancellationToken);
        // 9. Dispatch events — failures here do NOT roll back the invoice
        await DispatchSafelyAsync(invoice, cancellationToken);

        // 10. Return DTO — no domain types escape this layer
        return InvoiceResultMapper.ToResult(invoice);
    }

    // ── Private ────────────────────────────────────────────────────────────

    private static void Validate(CreateInvoiceCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.BillingSource))
            throw new ArgumentException("BillingSource is required.", nameof(command));

        if (string.IsNullOrWhiteSpace(command.Serie))
            throw new ArgumentException("Serie is required.", nameof(command));

        if (command.Lines is null || command.Lines.Count == 0)
            throw new ArgumentException("At least one invoice line is required.", nameof(command));

        if (command.Recipient is null)
            throw new ArgumentException("Recipient data is required.", nameof(command));

        // can't have 2 invoices with same payment reference
        var payment = command.PaymentReference;
    }

    private static CreateInvoiceRequest MapToDomainRequest(CreateInvoiceCommand cmd) => new()
    {
        Secret = cmd.Secret,
        BillingSource = cmd.BillingSource,
        Serie = cmd.Serie,
        OriginCurrencyCode = cmd.OriginCurrencyCode,
        IssueDate = cmd.IssueDate,
        ValueDate = cmd.ValueDate,
        Notes = cmd.Notes,
        Recipient = new RecipientData
        {
            LegalName = cmd.Recipient.LegalName,
            TradeName = cmd.Recipient.TradeName,
            TaxIdValue = cmd.Recipient.TaxIdValue,
            TaxIdCountryCode = cmd.Recipient.TaxIdCountryCode,
            Email = cmd.Recipient.Email,
            Phone = cmd.Recipient.Phone,
            Website = cmd.Recipient.Website,
            AddressLine1 = cmd.Recipient.AddressLine1,
            AddressLine2 = cmd.Recipient.AddressLine2,
            City = cmd.Recipient.City,
            Province = cmd.Recipient.Province,
            PostalCode = cmd.Recipient.PostalCode,
            AddressCountryCode = cmd.Recipient.AddressCountryCode,
            ExternalId = cmd.Recipient.ExternalId,
        },
        Lines = [.. cmd.Lines.Select(l => new InvoiceLineData
        {
            Description = l.Description,
            Quantity = l.Quantity,
            UnitPrice = l.UnitPrice,
            TaxRatePercentage = l.TaxRatePercentage,
        })],
        PaymentMethod = cmd.PaymentMethod,
        PaymentReference = cmd.PaymentReference,
        TransactionData = cmd.TransactionData
    };

    private async Task DispatchSafelyAsync(Invoice invoice, CancellationToken cancellationToken)
    {
        try
        {
            await _eventDispatcher.InvoiceCreatedAsync(invoice, cancellationToken);
        }
        catch (Exception ex)
        {
            // Log and continue — the invoice is already persisted.
            // A failed notification must never undo a legally issued invoice.
            // TODO: inject ILogger<CreateInvoiceUseCase> and log ex here.
            _ = ex;
        }
    }
}
