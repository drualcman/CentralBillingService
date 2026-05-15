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

        var originCurrency = originalInvoice.AppliedExchangeRate.From;
        var exchangeRate = originCurrency == Currency.EUR
            ? ExchangeRate.Identity(DateTimeOffset.UtcNow)
            : await _exchangeRateProvider.GetRateAsync(originCurrency, Currency.EUR, cancellationToken);

        var year = DateOnly.FromDateTime(DateTime.UtcNow).Year;
        var rectNumber = InvoiceNumber.Create(request.RectificativeSerie, year, reservedNumber);

        var lines = request.RectificationType == RectificationType.Substitution
            ? BuildSubstitutionLines(originalInvoice.Lines)
            : BuildDifferenceLines(request.Lines!, originCurrency, exchangeRate);

        var rectificative = RectificativeInvoice.Create(
            number: rectNumber,
            billingSource: request.BillingSource,
            originalInvoice: originalInvoice,
            rectificationReason: request.Reason,
            rectificationType: request.RectificationType,
            lines: lines,
            appliedExchangeRate: exchangeRate,
            hasher: _hasher,
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

        var originCurrency = originalRectificative.AppliedExchangeRate.From;
        var exchangeRate = originCurrency == Currency.EUR
            ? ExchangeRate.Identity(DateTimeOffset.UtcNow)
            : await _exchangeRateProvider.GetRateAsync(originCurrency, Currency.EUR, cancellationToken);

        var year = DateOnly.FromDateTime(DateTime.UtcNow).Year;
        var rectNumber = InvoiceNumber.Create(request.RectificativeSerie, year, reservedNumber);

        var lines = request.RectificationType == RectificationType.Substitution
            ? BuildSubstitutionLines(originalRectificative.Lines)
            : BuildDifferenceLines(request.Lines!, originCurrency, exchangeRate);

        var rectificative = RectificativeInvoice.CreateFromRectificative(
            number: rectNumber,
            billingSource: request.BillingSource,
            originalRectificative: originalRectificative,
            rectificationReason: request.Reason,
            rectificationType: request.RectificationType,
            lines: lines,
            appliedExchangeRate: exchangeRate,
            hasher: _hasher,
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
    /// Difference: uses only the lines provided by the caller (the delta).
    /// </summary>
    private static List<InvoiceLine> BuildDifferenceLines(
        IReadOnlyList<InvoiceLineData> lineData,
        Currency originCurrency,
        ExchangeRate exchangeRate)
    {
        var lines = new List<InvoiceLine>(lineData.Count);

        for (int i = 0; i < lineData.Count; i++)
        {
            var data = lineData[i];
            var taxRate = TaxRate.Of(data.TaxRatePercentage);
            var lineNum = i + 1;

            InvoiceLine line;

            if (originCurrency == Currency.EUR)
            {
                line = InvoiceLine.CreateInEur(
                    lineNum, data.Description, data.Quantity,
                    Money.Of(data.UnitPrice, Currency.EUR), taxRate);
            }
            else
            {
                var unitPriceOrigin = Money.Of(data.UnitPrice, originCurrency);
                var unitPriceEur = exchangeRate.Apply(unitPriceOrigin);
                line = InvoiceLine.CreateWithConversion(
                    lineNum, data.Description, data.Quantity,
                    unitPriceOrigin, unitPriceEur, taxRate);
            }

            lines.Add(line);
        }

        return lines;
    }
}
