namespace CentralBillingService.Domain.Services;

/// <summary>
/// Domain service that orchestrates invoice creation.
///
/// Responsibilities:
///   1. Resolve the issuer and validate the billing source
///   2. Obtain the exchange rate if the origin currency is not EUR
///   3. Build invoice lines with amounts in both currencies
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

        // 2. Resolve origin currency
        var originCurrency = Currency.From(request.OriginCurrencyCode);

        // 3. Obtain exchange rate (or identity if already EUR)
        var exchangeRate = await ResolveExchangeRateAsync(originCurrency, cancellationToken);

        // 4. Build recipient
        var recipient = BuildRecipient(request.Recipient);

        // 5. Build invoice number from the pre-reserved sequence number
        var issueDate = request.IssueDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var invoiceNumber = InvoiceNumber.Create(request.Serie, issueDate.Year, reservedNumber);

        // 6. Build lines with currency conversion if needed
        var lines = BuildLines(request.Lines, originCurrency, exchangeRate);

        // 7. Create and immediately issue the invoice
        var invoice = Invoice.Create(
            number: invoiceNumber,
            billingSource: request.BillingSource,
            issuer: sourceConfig.Issuer.ToBillingParty(),
            recipient: recipient,
            issueDate: issueDate,
            lines: lines,
            appliedExchangeRate: exchangeRate,
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

    private async Task<ExchangeRate> ResolveExchangeRateAsync(
        Currency originCurrency,
        CancellationToken cancellationToken)
    {
        if (originCurrency == Currency.EUR)
            return ExchangeRate.Identity(DateTimeOffset.UtcNow);

        if (!_exchangeRateProvider.Supports(originCurrency, Currency.EUR))
            throw new DomainException(
                $"Currency '{originCurrency.Code}' is not supported by the exchange rate provider.");

        return await _exchangeRateProvider.GetRateAsync(
            originCurrency, Currency.EUR, cancellationToken);
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

    private static List<InvoiceLine> BuildLines(
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
