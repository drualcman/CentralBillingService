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
    private readonly IFiscalRegistrarFactory _fiscalRegistrarFactory;
    private readonly IJobQueue _jobQueue;
    private readonly IIso9001 _iso9001;

    public RectifyInvoiceUseCase(
        RectifyInvoiceService domainService,
        BillingSourceRegistry registry,
        IInvoiceRepository repository,
        IInvoiceEventDispatcher eventDispatcher,
        IInvoiceHasher hasher,
        IInvoiceNumberProviderFactory numberProviderFactory,
        IBlobStorageService blobStorage,
        IFiscalRegistrarFactory fiscalRegistrarFactory,
        IJobQueue jobQueue,
        IIso9001 iso9001)
    {
        _domainService = domainService;
        _registry = registry;
        _repository = repository;
        _eventDispatcher = eventDispatcher;
        _hasher = hasher;
        _numberProviderFactory = numberProviderFactory;
        _blobStorage = blobStorage;
        _fiscalRegistrarFactory = fiscalRegistrarFactory;
        _jobQueue = jobQueue;
        _iso9001 = iso9001;
    }

    public async Task<RectifyInvoiceResult> ExecuteAsync(
        RectifyInvoiceCommand command,
        CancellationToken cancellationToken = default)
    {
        Validate(command);

        string reference = command.OriginalInvoiceNumber;

        await _iso9001.Register(reference, this, "Received rectify invoice command", command);

        try
        {
            var config = _registry.GetConfig(command.BillingSource, command.Secret);
            var numberProvider = _numberProviderFactory.GetFor(config);

            var issueDate = command.IssueDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var year = issueDate.Year;
            await FiscalSerieGuard.EnsureSerieIsNotSharedAsync(
                _repository, config, command.RectificativeSerie, year, cancellationToken);
            await RectificativeSerieGuard.EnsureSerieIsNotUsedByOrdinaryInvoicesAsync(
                _repository, command.BillingSource, command.RectificativeSerie, cancellationToken);
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

                var result = new RectifyInvoiceResult
                {
                    UpdatedOriginal = InvoiceResultMapper.ToResult(domainResult.UpdatedOriginal),
                    Rectificative = RectificativeInvoiceResultMapper.ToResult(domainResult.Rectificative),
                };

                await _iso9001.Register(domainResult.Rectificative.Number.Value, this, "Invoice rectified and persisted", result);

                await StampAndEnqueueFiscalAsync(
                    config, domainResult.Rectificative, RectifiedInvoiceAmounts.From(originalInvoice), cancellationToken);

                await DispatchSafelyAsync(domainResult.Rectificative, cancellationToken);

                return result;
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

            var result2 = new RectifyInvoiceResult
            {
                UpdatedOriginal = InvoiceResultMapper.ToResult(domainResult2.UpdatedOriginal),
                Rectificative = RectificativeInvoiceResultMapper.ToResult(domainResult2.Rectificative),
            };

            await _iso9001.Register(domainResult2.Rectificative.Number.Value, this, "Invoice rectified and persisted", result2);

            await StampAndEnqueueFiscalAsync(
                config, domainResult2.Rectificative, RectifiedInvoiceAmounts.From(originalRectificative), cancellationToken);

            await DispatchSafelyAsync(domainResult2.Rectificative, cancellationToken);

            return result2;
        }
        catch (Exception ex)
        {
            await _iso9001.Error(reference, this, ex);
            throw;
        }
    }

    // ── Private ────────────────────────────────────────────────────────────

    /// <summary>
    /// Runs the billing source's fiscal registrar on the rectificative: stamps it (fingerprint + QR
    /// content, persisted by a targeted update) and enqueues the async authority submission. No-op for
    /// sources with Registrar.Type = "None". Best-effort: a failure is traced but never undoes the
    /// already numbered and persisted rectificative; the recovery job retries stalled submissions.
    /// </summary>
    private async Task StampAndEnqueueFiscalAsync(
        BillingSourceConfig config, RectificativeInvoice rectificative, RectifiedInvoiceAmounts rectifiedAmounts,
        CancellationToken cancellationToken)
    {
        bool reportsToFiscalAuthority = config.Registrar is not null && !config.Registrar.IsNone;

        if (reportsToFiscalAuthority)
        {
            try
            {
                IFiscalRegistrar registrar = _fiscalRegistrarFactory.GetFor(config);
                FiscalStampResult stamp = await registrar.StampRectificativeAsync(rectificative, rectifiedAmounts, cancellationToken);

                if (stamp.HasStamp)
                {
                    rectificative.AttachFiscalStamp(stamp.Huella!);
                    if (!string.IsNullOrWhiteSpace(stamp.QrContent))
                        rectificative.AttachFiscalQr(stamp.QrContent!);
                    await _repository.UpdateFiscalStampAsync(
                        rectificative.Id, stamp.Huella!, stamp.QrContent, cancellationToken);
                }

                await _jobQueue.EnqueueFiscalSubmissionAsync(
                    new SubmitFiscalRecordCommand(rectificative.Number.Value, rectificative.BillingSource), cancellationToken);
            }
            catch (Exception ex)
            {
                await _iso9001.Error(rectificative.Number.Value, this, ex);
            }
        }
    }

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
        IssueDate = cmd.IssueDate,
        Lines = cmd.Lines?.Select(l => new InvoiceLineData
        {
            Description = l.Description,
            Quantity = l.Quantity,
            UnitPrice = l.UnitPrice,
            TaxRatePercentage = l.TaxRatePercentage,
            CurrencyCode = l.CurrencyCode,
            ProductType = l.ProductType,
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
            await _iso9001.Error(rectificative.Number.Value, this, ex);
        }
    }
}
