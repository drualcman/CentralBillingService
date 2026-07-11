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
    private readonly IIso9001 _iso9001;

    public CreateInvoiceUseCase(
        CreateInvoiceService domainService,
        BillingSourceRegistry registry,
        IInvoiceRepository repository,
        IInvoiceEventDispatcher eventDispatcher,
        IInvoiceNumberProviderFactory numberProviderFactory,
        IBlobStorageService blobStorage,
        IIso9001 iso9001)
    {
        _domainService = domainService;
        _registry = registry;
        _repository = repository;
        _eventDispatcher = eventDispatcher;
        _numberProviderFactory = numberProviderFactory;
        _blobStorage = blobStorage;
        _iso9001 = iso9001;
    }

    public async Task<InvoiceResult> ExecuteAsync(
        CreateInvoiceCommand command,
        CancellationToken cancellationToken = default)
    {
        // 1. Application-level validation
        Validate(command);

        string reference = GetReference(command);

        await _iso9001.Register(reference, this, "Received create invoice command", command);

        try
        {
            // 2. Validate that the billing source exists and get config
            var config = _registry.GetConfig(command.BillingSource, command.Secret);
            var numberProvider = _numberProviderFactory.GetFor(config);

            // 2b. Idempotency: a retried payment webhook (PayPal/Stripe can redeliver,
            //     and the queue can retry) must not create a second invoice for the same
            //     payment. If one already exists for this BillingSource + PaymentReference,
            //     return it instead of reserving a new number and issuing a duplicate.
            var existing = await _repository.FindByPaymentReferenceAsync(
                command.BillingSource, command.PaymentReference, cancellationToken);
            if (existing is not null)
            {
                var existingResult = InvoiceResultMapper.ToResult(existing);
                await _iso9001.Register(reference, this,
                    "Invoice already exists for this payment reference — returning existing (idempotent)",
                    existingResult);
                return existingResult;
            }

            var issueDate = command.IssueDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var domainRequest = MapToDomainRequest(command);

            // Builds the fully-hashed invoice from the reserved number and previous chain hash,
            // and attaches the deterministic QR URL before persistence (the URL is stable
            // regardless of when the image is generated). Runs the domain: exchange rate, hash
            // computation, immutability rules.
            async Task<Invoice> BuildInvoiceAsync(int reservedNumber, string? previousHash, CancellationToken ct)
            {
                var built = await _domainService.ExecuteAsync(domainRequest, reservedNumber, previousHash, ct);
                built.AttachQrCode(_blobStorage.GetQrUrl(
                    InvoiceHelper.GetQrFileName(built.BillingSource, built.Number.Value)));
                return built;
            }

            Invoice invoice;
            try
            {
                if (numberProvider.ReservesFromLocalDatabase)
                {
                    // 3+7. Local numbering (Spain / VeriFactu): reserve the number AND persist the
                    //      invoice in a single transaction. If the caller's HTTP client aborts (the
                    //      request is cancelled), the whole thing rolls back and no number is burned —
                    //      no gap in the correlative numbering.
                    invoice = await _repository.CreateAtomicAsync(
                        command.BillingSource, command.Serie, issueDate.Year, BuildInvoiceAsync, cancellationToken);
                }
                else
                {
                    // External authority issues the number first (it cannot be rolled back locally),
                    // then we build and persist the invoice.
                    var reservedNumber = await numberProvider.ReserveNextNumberAsync(
                        command.BillingSource, command.Serie, issueDate.Year, cancellationToken);
                    var previousHash = await _repository.GetLastHashAsync(
                        command.BillingSource, command.Serie, issueDate.Year, cancellationToken);
                    invoice = await BuildInvoiceAsync(reservedNumber, previousHash, cancellationToken);
                    await _repository.SaveAsync(invoice, cancellationToken);
                }
            }
            catch (DuplicatePaymentReferenceException)
            {
                // Lost a concurrent race after the idempotency pre-check: another request persisted
                // the invoice for this payment reference first. Re-read it and return it.
                var raced = await _repository.FindByPaymentReferenceAsync(
                    command.BillingSource, command.PaymentReference, cancellationToken);
                if (raced is null) throw;

                var racedResult = InvoiceResultMapper.ToResult(raced);
                await _iso9001.Register(reference, this,
                    "Concurrent create collided on payment reference — returning existing (idempotent)",
                    racedResult);
                return racedResult;
            }

            var result = InvoiceResultMapper.ToResult(invoice);

            await _iso9001.Register(reference, this, "Invoice created and persisted", result);

            // 9. Dispatch events — failures here do NOT roll back the invoice
            await DispatchSafelyAsync(invoice, cancellationToken);

            // 10. Return DTO — no domain types escape this layer
            return result;
        }
        catch (Exception ex)
        {
            await _iso9001.Error(reference, this, ex);
            throw;
        }
    }

    // ── Private ────────────────────────────────────────────────────────────

    private static string GetReference(CreateInvoiceCommand command) =>
        !string.IsNullOrWhiteSpace(command.Recipient?.Email) ? command.Recipient.Email :
        !string.IsNullOrWhiteSpace(command.Recipient?.TaxIdValue) ? command.Recipient.TaxIdValue :
        Guid.NewGuid().ToString();

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

        if (string.IsNullOrWhiteSpace(command.PaymentReference))
            throw new ArgumentException("PaymentReference is required.", nameof(command));
    }

    private static CreateInvoiceRequest MapToDomainRequest(CreateInvoiceCommand cmd) => new()
    {
        Secret = cmd.Secret,
        BillingSource = cmd.BillingSource,
        Serie = cmd.Serie,
        InvoiceNumberClientPrefix = cmd.InvoiceNumberClientPrefix,
        InvoiceNumberClientSuffix = cmd.InvoiceNumberClientSuffix,
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
            CurrencyCode = l.CurrencyCode,
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
            await _iso9001.Error(invoice.Number.Value, this, ex);
        }
    }
}
