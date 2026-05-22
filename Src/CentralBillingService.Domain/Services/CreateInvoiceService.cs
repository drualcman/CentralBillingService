namespace CentralBillingService.Domain.Services;

/// <summary>
/// Domain service that orchestrates invoice creation.
///
/// Responsibilities:
///   1. Resolve the issuer and validate the billing source
///   2. Obtain the exchange rate for each unique non-EUR currency used across lines
///   3. Build invoice lines with amounts in both currencies (per-line currency support)
///   4. Create and issue the invoice using the pre-reserved number
///   5. Compute the VeriFactu hash chain
///
/// What this service does NOT do:
///   - Reserve sequence numbers (done by the use case via the repository)
///   - Persist anything (done by the use case via the repository)
///   - Send emails or generate PDFs (done by event handlers)
/// </summary>
public sealed class CreateInvoiceService
{
    private readonly BillingSourceRegistry _registry;
    private readonly IExchangeRateProvider _exchangeRateProvider;
    private readonly IInvoiceHasher _hasher;

    public CreateInvoiceService(
        BillingSourceRegistry registry,
        IExchangeRateProvider exchangeRateProvider,
        IInvoiceHasher hasher)
    {
        _registry = registry;
        _exchangeRateProvider = exchangeRateProvider;
        _hasher = hasher;
    }

    /// <summary>
    /// Creates and issues an invoice.
    /// </summary>
    /// <param name="request">Invoice data from the caller.</param>
    /// <param name="reservedNumber">
    /// The sequence number already reserved atomically by the use case
    /// via the repository. The domain uses it directly — no further
    /// reservation is needed.
    /// </param>
    /// <param name="previousHash">
    /// Hash of the last issued invoice in the same BillingSource+Serie+Year chain.
    /// Null if this is the first invoice in the chain.
    /// </param>
    public async Task<Invoice> ExecuteAsync(
        CreateInvoiceRequest request,
        int reservedNumber,
        string? previousHash,
        CancellationToken cancellationToken = default)
    {
        // 1. Resolve issuer from registry
        var sourceConfig = _registry.GetConfig(request.BillingSource, request.Secret);

        // 2. Build lines with per-line currency conversion
        var defaultCurrency = request.OriginCurrencyCode ?? "EUR";
        var (lines, primaryRate) = await BuildLinesAsync(request.Lines, defaultCurrency, request.Recipient.AddressCountryCode, cancellationToken);

        // 3. Build recipient
        var recipient = BuildRecipient(request.Recipient);

        // 4. Build invoice number from the pre-reserved sequence number
        var issueDate = request.IssueDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var invoiceNumber = InvoiceNumber.Create(
            request.Serie, issueDate.Year, reservedNumber,
            request.InvoiceNumberClientPrefix, request.InvoiceNumberClientSuffix);

        // 5. Create and immediately issue the invoice
        var invoice = Invoice.Create(
            number: invoiceNumber,
            billingSource: request.BillingSource,
            issuer: sourceConfig.Issuer.ToBillingParty(),
            recipient: recipient,
            issueDate: issueDate,
            lines: lines,
            appliedExchangeRate: primaryRate,
            hasher: _hasher,
            previousHash: previousHash,
            valueDate: request.ValueDate,
            notes: request.Notes,
            paymentReference: request.PaymentReference,
            transactionData: request.TransactionData,
            paymentMethod: request.PaymentMethod);

        invoice.Issue();

        return invoice;
    }

    // ── Private ────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds invoice lines with per-line currency conversion.
    /// Returns the lines and the "primary" exchange rate to store on the invoice
    /// (the single non-EUR rate when all lines share one currency, otherwise identity).
    /// </summary>
    private async Task<(List<InvoiceLine> Lines, ExchangeRate PrimaryRate)> BuildLinesAsync(
        IReadOnlyList<InvoiceLineData> lineData,
        string defaultCurrencyCode,
        string recipientCountryCode,
        CancellationToken cancellationToken)
    {
        // Resolve currency per line (fall back to invoice default)
        var lineCurrencies = lineData
            .Select(l => Currency.From(l.CurrencyCode ?? defaultCurrencyCode))
            .ToList();

        // Fetch exchange rates for each unique non-EUR currency (one API call per currency)
        var uniqueNonEur = lineCurrencies.Where(c => c != Currency.EUR).Distinct().ToList();
        var rateCache = new Dictionary<Currency, ExchangeRate>(uniqueNonEur.Count);
        foreach (var cur in uniqueNonEur)
        {
            if (!_exchangeRateProvider.Supports(cur, Currency.EUR))
                throw new DomainException(
                    $"Currency '{cur.Code}' is not supported by the exchange rate provider.");
            rateCache[cur] = await _exchangeRateProvider.GetRateAsync(cur, Currency.EUR, cancellationToken);
        }

        // Build lines
        var lines = new List<InvoiceLine>(lineData.Count);
        for (int i = 0; i < lineData.Count; i++)
        {
            var data = lineData[i];
            var lineCurrency = lineCurrencies[i];
            // International transactions (non-EUR currency or non-ES recipient) carry no Spanish VAT
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

        // Primary rate: the single non-EUR rate when uniform; identity for EUR-only or mixed
        var primaryRate = uniqueNonEur.Count == 1
            ? rateCache[uniqueNonEur[0]]
            : ExchangeRate.Identity(DateTimeOffset.UtcNow);

        return (lines, primaryRate);
    }

    private static BillingParty BuildRecipient(RecipientData data)
    {
        var taxId = TaxId.Create(data.TaxIdValue, data.TaxIdCountryCode);

        var address = PostalAddress.Create(
            line1: data.AddressLine1,
            city: data.City,
            postalCode: data.PostalCode,
            countryCode: data.AddressCountryCode,
            line2: data.AddressLine2,
            province: data.Province);

        return BillingParty.Create(
            legalName: data.LegalName,
            taxId: taxId,
            address: address,
            email: data.Email,
            tradeName: data.TradeName,
            phone: data.Phone,
            website: data.Website,
            externalId: data.ExternalId);
    }
}
