namespace CentralBillingService.Domain.Services;

/// <summary>
/// Domain service that orchestrates rectificative invoice creation.
///
/// Flow:
///   1. Validate the original invoice can be rectified
///   2. Obtain the exchange rate at the current moment (not the original's rate)
///   3. Build lines according to the rectification type
///   4. Create and issue the rectificative using the pre-reserved number
///   5. Mark the original as rectified
///
/// The original and the rectificative are returned together —
/// the use case must persist both in the same transaction.
/// </summary>
public sealed class RectifyInvoiceService
{
    private readonly BillingSourceRegistry _registry;
    private readonly IExchangeRateProvider _exchangeRateProvider;
    private readonly IInvoiceHasher _hasher;

    public RectifyInvoiceService(
        BillingSourceRegistry registry,
        IExchangeRateProvider exchangeRateProvider,
        IInvoiceHasher hasher)
    {
        _registry = registry;
        _exchangeRateProvider = exchangeRateProvider;
        _hasher = hasher;
    }

    /// <param name="request">Rectification data from the caller.</param>
    /// <param name="originalInvoice">The invoice to rectify — already loaded by the use case.</param>
    /// <param name="reservedNumber">
    /// Sequence number already reserved atomically by the use case via the repository.
    /// </param>
    /// <param name="previousHash">
    /// Hash of the last issued invoice in the rectificative BillingSource+Serie+Year chain.
    /// Null if this is the first rectificative in that chain.
    /// </param>
    public async Task<RectifyInvoice> ExecuteAsync(
        RectifyInvoiceRequest request,
        Invoice originalInvoice,
        int reservedNumber,
        string? previousHash,
        CancellationToken cancellationToken = default)
    {
        _registry.GetConfig(request.BillingSource, request.Secret);

        var issueDate = request.IssueDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var rectNumber = InvoiceNumber.Create(request.RectificativeSerie, issueDate.Year, reservedNumber);

        List<InvoiceLine> lines;
        ExchangeRate primaryRate;

        if (request.RectificationType == RectificationType.Substitution)
        {
            lines = BuildSubstitutionLines(originalInvoice.Lines);
            // Keep original invoice's primary rate for the rectificative
            var origCurrency = originalInvoice.AppliedExchangeRate.From;
            primaryRate = origCurrency == Currency.EUR
                ? ExchangeRate.Identity(DateTimeOffset.UtcNow)
                : await _exchangeRateProvider.GetRateAsync(origCurrency, Currency.EUR, cancellationToken);
        }
        else
        {
            // Difference: each line may specify its own currency
            var defaultCurrency = originalInvoice.AppliedExchangeRate.From.Code;
            (lines, primaryRate) = await BuildDifferenceLinesAsync(
                request.Lines!, defaultCurrency, originalInvoice.Recipient.Address.CountryCode, cancellationToken);
        }

        var rectificative = RectificativeInvoice.Create(
            number: rectNumber,
            billingSource: request.BillingSource,
            originalInvoice: originalInvoice,
            rectificationReason: request.Reason,
            rectificationType: request.RectificationType,
            lines: lines,
            appliedExchangeRate: primaryRate,
            hasher: _hasher,
            issueDate: issueDate,
            previousHash: previousHash,
            notes: request.Notes,
            paymentMethod: request.PaymentMethod,
            paymentReference: request.PaymentReference,
            transactionData: request.TransactionData);

        rectificative.Issue();
        originalInvoice.MarkAsRectifiedBy(rectNumber);

        return new RectifyInvoice(originalInvoice, rectificative);
    }

    /// <summary>
    /// Rectifica una factura rectificativa existente.
    /// Útil cuando se cometió un error al emitir una rectificativa.
    /// </summary>
    public async Task<RectifyRectificativeInvoice> ExecuteFromRectificativeAsync(
        RectifyInvoiceRequest request,
        RectificativeInvoice originalRectificative,
        int reservedNumber,
        string? previousHash,
        CancellationToken cancellationToken = default)
    {
        _registry.GetConfig(request.BillingSource, request.Secret);

        var issueDate = request.IssueDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var rectNumber = InvoiceNumber.Create(request.RectificativeSerie, issueDate.Year, reservedNumber);

        List<InvoiceLine> lines;
        ExchangeRate primaryRate;

        if (request.RectificationType == RectificationType.Substitution)
        {
            lines = BuildSubstitutionLines(originalRectificative.Lines);
            var origCurrency = originalRectificative.AppliedExchangeRate.From;
            primaryRate = origCurrency == Currency.EUR
                ? ExchangeRate.Identity(DateTimeOffset.UtcNow)
                : await _exchangeRateProvider.GetRateAsync(origCurrency, Currency.EUR, cancellationToken);
        }
        else
        {
            var defaultCurrency = originalRectificative.AppliedExchangeRate.From.Code;
            (lines, primaryRate) = await BuildDifferenceLinesAsync(
                request.Lines!, defaultCurrency, originalRectificative.Recipient.Address.CountryCode, cancellationToken);
        }

        var rectificative = RectificativeInvoice.CreateFromRectificative(
            number: rectNumber,
            billingSource: request.BillingSource,
            originalRectificative: originalRectificative,
            rectificationReason: request.Reason,
            rectificationType: request.RectificationType,
            lines: lines,
            appliedExchangeRate: primaryRate,
            hasher: _hasher,
            issueDate: issueDate,
            previousHash: previousHash,
            notes: request.Notes,
            paymentMethod: request.PaymentMethod,
            paymentReference: request.PaymentReference,
            transactionData: request.TransactionData);

        rectificative.Issue();
        originalRectificative.MarkAsRectifiedBy(rectNumber);

        return new RectifyRectificativeInvoice(originalRectificative, rectificative);
    }

    // ── Private ────────────────────────────────────────────────────────────

    private static List<InvoiceLine> BuildSubstitutionLines(IReadOnlyList<InvoiceLine> lines) =>
        lines
            .Select((l, i) => l.HasCurrencyConversion
                ? InvoiceLine.CreateWithConversion(
                    i + 1, l.Description, l.Quantity,
                    l.UnitPriceOrigin, l.UnitPriceEur, l.TaxRate)
                : InvoiceLine.CreateInEur(
                    i + 1, l.Description, l.Quantity,
                    l.UnitPriceEur, l.TaxRate))
            .ToList();

    /// <summary>
    /// Difference: builds the delta lines with per-line currency support.
    /// Returns the lines and the primary exchange rate for the rectificative invoice.
    /// </summary>
    private async Task<(List<InvoiceLine> Lines, ExchangeRate PrimaryRate)> BuildDifferenceLinesAsync(
        IReadOnlyList<InvoiceLineData> lineData,
        string defaultCurrencyCode,
        string recipientCountryCode,
        CancellationToken cancellationToken)
    {
        var lineCurrencies = lineData
            .Select(l => Currency.From(l.CurrencyCode ?? defaultCurrencyCode))
            .ToList();

        var uniqueNonEur = lineCurrencies.Where(c => c != Currency.EUR).Distinct().ToList();
        var rateCache = new Dictionary<Currency, ExchangeRate>(uniqueNonEur.Count);
        foreach (var cur in uniqueNonEur)
        {
            if (!_exchangeRateProvider.Supports(cur, Currency.EUR))
                throw new DomainException($"Currency '{cur.Code}' is not supported by the exchange rate provider.");
            rateCache[cur] = await _exchangeRateProvider.GetRateAsync(cur, Currency.EUR, cancellationToken);
        }

        var lines = new List<InvoiceLine>(lineData.Count);
        for (int i = 0; i < lineData.Count; i++)
        {
            var data = lineData[i];
            var lineCurrency = lineCurrencies[i];
            var taxRate = lineCurrency != Currency.EUR || recipientCountryCode.Trim().ToUpperInvariant() != "ES"
                ? TaxRate.Zero
                : TaxRate.Of(data.TaxRatePercentage);

            InvoiceLine line;
            if (lineCurrency == Currency.EUR)
            {
                line = InvoiceLine.CreateInEur(
                    i + 1, data.Description, data.Quantity,
                    Money.Of(data.UnitPrice, Currency.EUR), taxRate);
            }
            else
            {
                var rate = rateCache[lineCurrency];
                var unitPriceOrigin = Money.Of(data.UnitPrice, lineCurrency);
                var unitPriceEur = rate.Apply(unitPriceOrigin);
                line = InvoiceLine.CreateWithConversion(
                    i + 1, data.Description, data.Quantity,
                    unitPriceOrigin, unitPriceEur, taxRate);
            }
            lines.Add(line);
        }

        var primaryRate = uniqueNonEur.Count == 1
            ? rateCache[uniqueNonEur[0]]
            : ExchangeRate.Identity(DateTimeOffset.UtcNow);

        return (lines, primaryRate);
    }
}
