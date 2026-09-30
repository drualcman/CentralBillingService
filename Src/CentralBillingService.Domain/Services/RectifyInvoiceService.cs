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

        (List<InvoiceLine> lines, ExchangeRate primaryRate) = await BuildRectificativeLinesAsync(
            request, originalInvoice.Lines, originalInvoice.AppliedExchangeRate.From,
            originalInvoice.Recipient.Address.CountryCode, cancellationToken);

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

        (List<InvoiceLine> lines, ExchangeRate primaryRate) = await BuildRectificativeLinesAsync(
            request, originalRectificative.Lines, originalRectificative.AppliedExchangeRate.From,
            originalRectificative.Recipient.Address.CountryCode, cancellationToken);

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

    /// <summary>
    /// Substitution: the lines the caller sends are the corrected invoice as it should have been; with
    /// none, the rectified document's lines are copied. Difference: the caller's (signed) delta lines.
    /// </summary>
    private async Task<(List<InvoiceLine> Lines, ExchangeRate PrimaryRate)> BuildRectificativeLinesAsync(
        RectifyInvoiceRequest request,
        IReadOnlyList<InvoiceLine> rectifiedLines,
        Currency rectifiedCurrency,
        string recipientCountryCode,
        CancellationToken cancellationToken)
    {
        bool copiesRectifiedLines = request.RectificationType == RectificationType.Substitution
            && (request.Lines is null || request.Lines.Count == 0);
        (List<InvoiceLine> Lines, ExchangeRate PrimaryRate) result;

        if (copiesRectifiedLines)
        {
            ExchangeRate primaryRate = rectifiedCurrency == Currency.EUR
                ? ExchangeRate.Identity(DateTimeOffset.UtcNow)
                : await _exchangeRateProvider.GetRateAsync(rectifiedCurrency, Currency.EUR, cancellationToken);
            result = (BuildSubstitutionLines(rectifiedLines), primaryRate);
        }
        else
        {
            result = await BuildDifferenceLinesAsync(
                request.Lines!, rectifiedCurrency.Code, recipientCountryCode, cancellationToken);
        }

        return result;
    }

    private static List<InvoiceLine> BuildSubstitutionLines(IReadOnlyList<InvoiceLine> lines) =>
        lines
            .Select((l, i) => l.HasCurrencyConversion
                ? InvoiceLine.CreateWithConversion(
                    i + 1, l.Description, l.Quantity,
                    l.UnitPriceOrigin, l.UnitPriceEur, l.TaxRate, l.ProductType)
                : InvoiceLine.CreateInEur(
                    i + 1, l.Description, l.Quantity,
                    l.UnitPriceEur, l.TaxRate, l.ProductType))
            .ToList();

    /// <summary>
    /// Builds the caller's lines (substitution or difference) with per-line currency support.
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
                    Money.Of(data.UnitPrice, Currency.EUR), taxRate, data.ProductType);
            }
            else
            {
                var rate = rateCache[lineCurrency];
                var unitPriceOrigin = Money.Of(data.UnitPrice, lineCurrency);
                var unitPriceEur = rate.Apply(unitPriceOrigin);
                line = InvoiceLine.CreateWithConversion(
                    i + 1, data.Description, data.Quantity,
                    unitPriceOrigin, unitPriceEur, taxRate, data.ProductType);
            }
            lines.Add(line);
        }

        var primaryRate = uniqueNonEur.Count == 1
            ? rateCache[uniqueNonEur[0]]
            : ExchangeRate.Identity(DateTimeOffset.UtcNow);

        return (lines, primaryRate);
    }
}
