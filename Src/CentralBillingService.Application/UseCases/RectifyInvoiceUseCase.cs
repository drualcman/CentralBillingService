namespace CentralBillingService.Application.UseCases;

/// <summary>
/// Orchestrates the full invoice rectification flow:
///
///   1. Validate the command
///   2. Load the original invoice — must exist and be Issued or Rectified
///   3. Reserve the next sequence number for the rectificative serie
///   4. Get the previous hash for the rectificative serie chain
///   5. Map command to domain request
///   6. Delegate to the domain service
///   7. Persist both invoices atomically
///   8. Dispatch events
///   9. Return result DTOs
///
/// The original invoice and the rectificative are always persisted together.
/// If persistence fails, neither is saved.
/// </summary>
public sealed class RectifyInvoiceUseCase
{
    private readonly RectifyInvoiceService _domainService;
    private readonly BillingSourceRegistry _registry;
    private readonly IInvoiceRepository _repository;
    private readonly IInvoiceEventDispatcher _eventDispatcher;
    private readonly IInvoiceHasher _hasher;
    private readonly IInvoiceNumberProviderFactory _numberProviderFactory;
    private readonly IBlobStorageService _blobStorage;

    public RectifyInvoiceUseCase(
        RectifyInvoiceService domainService,
        BillingSourceRegistry registry,
        IInvoiceRepository repository,
        IInvoiceEventDispatcher eventDispatcher,
        IInvoiceHasher hasher,
        IInvoiceNumberProviderFactory numberProviderFactory,
        IBlobStorageService blobStorage)
    {
        _domainService = domainService;
        _registry = registry;
        _repository = repository;
        _eventDispatcher = eventDispatcher;
        _hasher = hasher;
        _numberProviderFactory = numberProviderFactory;
        _blobStorage = blobStorage;
    }

    public async Task<RectifyInvoiceResult> ExecuteAsync(
        RectifyInvoiceCommand command,
        CancellationToken cancellationToken = default)
    {
        Validate(command);
        var config = _registry.GetConfig(command.BillingSource, command.Secret);
        var numberProvider = _numberProviderFactory.GetFor(config);

        var year = DateOnly.FromDateTime(DateTime.UtcNow).Year;
        var domainRequest = MapToDomainRequest(command);

        // Intentar cargar la factura original; si no existe, buscar entre las rectificativas
        var originalInvoice = await _repository.FindByNumberAsync(
            command.BillingSource, command.OriginalInvoiceNumber, cancellationToken);

        if (originalInvoice is not null)
        {
            if (!originalInvoice.VerifyIntegrity(_hasher))
                throw new InvoiceTamperingDetectedException(originalInvoice.Number.Value, originalInvoice.Hash);

            var reservedNumber = await numberProvider.ReserveNextNumberAsync(
                originalInvoice.BillingSource, command.RectificativeSerie, year, cancellationToken);
            var previousHash = await _repository.GetLastHashAsync(
                originalInvoice.BillingSource, command.RectificativeSerie, year, cancellationToken);

            var domainResult = await _domainService.ExecuteAsync(
                domainRequest, originalInvoice, reservedNumber, previousHash, cancellationToken);

            // Compute the QR blob URL deterministically from the invoice number and attach it
            // before persisting — the URL is stable regardless of when the image is generated.
            domainResult.Rectificative.AttachQrCode(
                _blobStorage.GetQrUrl(
                    InvoiceHelper.GetQrFileName(domainResult.Rectificative.BillingSource, domainResult.Rectificative.Number.Value)));

            await _repository.SaveRectificativeAsync(
                domainResult.Rectificative, domainResult.UpdatedOriginal, cancellationToken);

            await DispatchSafelyAsync(
                domainResult.Rectificative, cancellationToken);

            return new RectifyInvoiceResult
            {
                UpdatedOriginal = InvoiceResultMapper.ToResult(domainResult.UpdatedOriginal),
                Rectificative = RectificativeInvoiceResultMapper.ToResult(domainResult.Rectificative),
            };
        }

        // La factura original es una rectificativa
        var originalRectificative = await _repository.FindRectificativeByNumberAsync(
            command.BillingSource, command.OriginalInvoiceNumber, cancellationToken);

        if (originalRectificative is null)
            throw new InvoiceNotFoundException(command.OriginalInvoiceNumber);

        if (!originalRectificative.VerifyIntegrity(_hasher))
            throw new InvoiceTamperingDetectedException(originalRectificative.Number.Value, originalRectificative.Hash);

        var reservedNumber2 = await numberProvider.ReserveNextNumberAsync(
            originalRectificative.BillingSource, command.RectificativeSerie, year, cancellationToken);
        var previousHash2 = await _repository.GetLastHashAsync(
            originalRectificative.BillingSource, command.RectificativeSerie, year, cancellationToken);

        var domainResult2 = await _domainService.ExecuteFromRectificativeAsync(
            domainRequest, originalRectificative, reservedNumber2, previousHash2, cancellationToken);

        domainResult2.Rectificative.AttachQrCode(_blobStorage
            .GetQrUrl(InvoiceHelper.GetQrFileName(domainResult2.Rectificative.BillingSource, domainResult2.Rectificative.Number.Value)));

        await _repository.SaveRectificativeFromRectificativeAsync(
            domainResult2.Rectificative, domainResult2.UpdatedOriginal, cancellationToken);

        await DispatchSafelyAsync(
            domainResult2.Rectificative, cancellationToken);

        return new RectifyInvoiceResult
        {
            UpdatedOriginal = InvoiceResultMapper.ToResult(domainResult2.UpdatedOriginal),
            Rectificative = RectificativeInvoiceResultMapper.ToResult(domainResult2.Rectificative),
        };
    }

    // ── Private ────────────────────────────────────────────────────────────

    private static void Validate(RectifyInvoiceCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.OriginalInvoiceNumber))
            throw new ArgumentException("OriginalInvoiceNumber is required.", nameof(command));

        if (string.IsNullOrWhiteSpace(command.RectificativeSerie))
            throw new ArgumentException("RectificativeSerie is required.", nameof(command));

        if (string.IsNullOrWhiteSpace(command.Reason) || command.Reason.Trim().Length < 10)
            throw new ArgumentException(
                "Reason must be descriptive (minimum 10 characters).", nameof(command));

        if (command.RectificationType == RectificationType.Difference &&
            (command.Lines is null || command.Lines.Count == 0))
            throw new ArgumentException(
                "Lines are required for Difference rectification type.", nameof(command));
    }

    private static RectifyInvoiceRequest MapToDomainRequest(RectifyInvoiceCommand cmd) => new()
    {
        BillingSource = cmd.BillingSource,
        Secret = cmd.Secret,
        Reason = cmd.Reason,
        RectificativeSerie = cmd.RectificativeSerie,
        RectificationType = cmd.RectificationType == RectificationType.Substitution
            ? RectificationType.Substitution
            : RectificationType.Difference,
        Lines = cmd.Lines?.Select(l => new InvoiceLineData
        {
            Description = l.Description,
            Quantity = l.Quantity,
            UnitPrice = l.UnitPrice,
            TaxRatePercentage = l.TaxRatePercentage,
        }).ToList(),
        Notes = cmd.Notes,
        PaymentMethod = cmd.PaymentMethod,
        PaymentReference = cmd.PaymentReference,
        TransactionData = cmd.TransactionData
    };

    private async Task DispatchSafelyAsync(
        RectificativeInvoice rectificative,
        CancellationToken cancellationToken)
    {
        try
        {
            await _eventDispatcher.InvoiceRectifiedAsync(
                rectificative, cancellationToken);
        }
        catch (Exception ex)
        {
            _ = ex;
        }
    }
}
