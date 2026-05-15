using CentralBillingService.Domain.ValueObjects;

namespace CentralBillingService.Persistence.SqlServer.Mappers;

/// <summary>
/// Maps between EF entities and domain entities.
/// One direction: domain → EF (for writes).
/// Other direction: EF → domain (for reads).
/// No business logic here — pure structural translation.
/// </summary>
internal static class InvoiceMapper
{
    // ── Domain → EF ────────────────────────────────────────────────────────

    internal static InvoiceEntity ToEntity(Invoice invoice) => new()
    {
        Id = invoice.Id,
        InvoiceNumber = invoice.Number.Value,
        BillingSource = invoice.BillingSource,
        Serie = invoice.Number.Serie,
        SequenceNumber = invoice.Number.Number,
        Year = invoice.Number.Year,
        Status = invoice.Status.ToString(),
        InvoiceType = "F",

        IssuerLegalName = invoice.Issuer.LegalName,
        IssuerTradeName = invoice.Issuer.TradeName,
        IssuerTaxIdValue = invoice.Issuer.TaxId.Value,
        IssuerTaxIdCountryCode = invoice.Issuer.TaxId.CountryCode,
        IssuerEmail = invoice.Issuer.Email,
        IssuerPhone = invoice.Issuer.Phone,
        IssuerWebsite = invoice.Issuer.Website,
        IssuerAddressLine1 = invoice.Issuer.Address.Line1,
        IssuerAddressLine2 = invoice.Issuer.Address.Line2,
        IssuerCity = invoice.Issuer.Address.City,
        IssuerProvince = invoice.Issuer.Address.Province,
        IssuerPostalCode = invoice.Issuer.Address.PostalCode,
        IssuerAddressCountryCode = invoice.Issuer.Address.CountryCode,

        RecipientLegalName = invoice.Recipient.LegalName,
        RecipientTradeName = invoice.Recipient.TradeName,
        RecipientTaxIdValue = invoice.Recipient.TaxId.Value,
        RecipientTaxIdCountryCode = invoice.Recipient.TaxId.CountryCode,
        RecipientEmail = invoice.Recipient.Email,
        RecipientPhone = invoice.Recipient.Phone,
        RecipientWebsite = invoice.Recipient.Website,
        RecipientAddressLine1 = invoice.Recipient.Address.Line1,
        RecipientAddressLine2 = invoice.Recipient.Address.Line2,
        RecipientCity = invoice.Recipient.Address.City,
        RecipientProvince = invoice.Recipient.Address.Province,
        RecipientPostalCode = invoice.Recipient.Address.PostalCode,
        RecipientAddressCountryCode = invoice.Recipient.Address.CountryCode,
        RecipientExternalId = invoice.Recipient.ExternalId,

        IssueDate = invoice.IssueDate,
        ValueDate = invoice.ValueDate,
        CreatedAt = invoice.CreatedAt,

        TaxableBaseEur = invoice.TaxableBaseEur.Amount,
        TotalTaxAmountEur = invoice.TotalTaxAmountEur.Amount,
        TotalEur = invoice.TotalEur.Amount,
        TotalOriginAmount = invoice.TotalInOriginCurrency.Amount,
        OriginCurrencyCode = invoice.TotalInOriginCurrency.Currency.Code,

        ExchangeRateFrom = invoice.AppliedExchangeRate.From.Code,
        ExchangeRateTo = invoice.AppliedExchangeRate.To.Code,
        ExchangeRateValue = invoice.AppliedExchangeRate.Rate,
        ExchangeRateFetchedAt = invoice.AppliedExchangeRate.FetchedAt,

        Hash = invoice.Hash,
        PreviousHash = invoice.PreviousHash,
        RectifiedByNumber = invoice.RectifiedBy?.Value,
        Notes = invoice.Notes,
        PaymentReference = invoice.PaymentReference,
        PaymentMethod = invoice.PaymentMethod,
        TransactionData = invoice.TransactionData,
        QrCodeBlobUrl = invoice.QrCodeBlobUrl,

        Lines = invoice.Lines.Select(l => ToLineEntity(l, invoice.Id)).ToList(),
    };

    internal static InvoiceEntity ToEntity(RectificativeInvoice invoice) => new()
    {
        Id = invoice.Id,
        InvoiceNumber = invoice.Number.Value,
        BillingSource = invoice.BillingSource,
        Serie = invoice.Number.Serie,
        SequenceNumber = invoice.Number.Number,
        Year = invoice.Number.Year,
        Status = invoice.Status.ToString(),
        InvoiceType = "R",

        OriginalInvoiceNumber = invoice.OriginalInvoiceNumber.Value,
        OriginalIssueDate = invoice.OriginalIssueDate,
        RectificationReason = invoice.RectificationReason,
        RectificationType = invoice.RectificationType.ToString(),

        IssuerLegalName = invoice.Issuer.LegalName,
        IssuerTradeName = invoice.Issuer.TradeName,
        IssuerTaxIdValue = invoice.Issuer.TaxId.Value,
        IssuerTaxIdCountryCode = invoice.Issuer.TaxId.CountryCode,
        IssuerEmail = invoice.Issuer.Email,
        IssuerPhone = invoice.Issuer.Phone,
        IssuerWebsite = invoice.Issuer.Website,
        IssuerAddressLine1 = invoice.Issuer.Address.Line1,
        IssuerAddressLine2 = invoice.Issuer.Address.Line2,
        IssuerCity = invoice.Issuer.Address.City,
        IssuerProvince = invoice.Issuer.Address.Province,
        IssuerPostalCode = invoice.Issuer.Address.PostalCode,
        IssuerAddressCountryCode = invoice.Issuer.Address.CountryCode,

        RecipientLegalName = invoice.Recipient.LegalName,
        RecipientTradeName = invoice.Recipient.TradeName,
        RecipientTaxIdValue = invoice.Recipient.TaxId.Value,
        RecipientTaxIdCountryCode = invoice.Recipient.TaxId.CountryCode,
        RecipientEmail = invoice.Recipient.Email,
        RecipientPhone = invoice.Recipient.Phone,
        RecipientWebsite = invoice.Recipient.Website,
        RecipientAddressLine1 = invoice.Recipient.Address.Line1,
        RecipientAddressLine2 = invoice.Recipient.Address.Line2,
        RecipientCity = invoice.Recipient.Address.City,
        RecipientProvince = invoice.Recipient.Address.Province,
        RecipientPostalCode = invoice.Recipient.Address.PostalCode,
        RecipientAddressCountryCode = invoice.Recipient.Address.CountryCode,
        RecipientExternalId = invoice.Recipient.ExternalId,

        IssueDate = invoice.IssueDate,
        CreatedAt = invoice.CreatedAt,

        TaxableBaseEur = invoice.TaxableBaseEur.Amount,
        TotalTaxAmountEur = invoice.TotalTaxAmountEur.Amount,
        TotalEur = invoice.TotalEur.Amount,
        TotalOriginAmount = invoice.TotalInOriginCurrency.Amount,
        OriginCurrencyCode = invoice.TotalInOriginCurrency.Currency.Code,

        ExchangeRateFrom = invoice.AppliedExchangeRate.From.Code,
        ExchangeRateTo = invoice.AppliedExchangeRate.To.Code,
        ExchangeRateValue = invoice.AppliedExchangeRate.Rate,
        ExchangeRateFetchedAt = invoice.AppliedExchangeRate.FetchedAt,

        Hash = invoice.Hash,
        PreviousHash = invoice.PreviousHash,
        RectifiedByNumber = invoice.RectifiedBy?.Value,
        Notes = invoice.Notes,
        PaymentReference = invoice.PaymentReference,
        PaymentMethod = invoice.PaymentMethod,
        TransactionData = invoice.TransactionData,

        Lines = invoice.Lines.Select(l => ToLineEntity(l, invoice.Id)).ToList(),
    };

    private static InvoiceLineEntity ToLineEntity(InvoiceLine line, Guid invoiceId) => new()
    {
        Id = Guid.NewGuid(),
        InvoiceId = invoiceId,
        LineNumber = line.LineNumber,
        Description = line.Description,
        Quantity = line.Quantity,
        TaxRatePercentage = line.TaxRate.Percentage,
        UnitPriceEur = line.UnitPriceEur.Amount,
        TaxableBaseEur = line.TaxableBaseEur.Amount,
        TaxAmountEur = line.TaxAmountEur.Amount,
        TotalEur = line.TotalEur.Amount,
        UnitPriceOrigin = line.UnitPriceOrigin.Amount,
        TotalOrigin = line.TotalOrigin.Amount,
        OriginCurrencyCode = line.UnitPriceOrigin.Currency.Code,
        HasCurrencyConversion = line.HasCurrencyConversion,
    };

    // ── EF → Domain ────────────────────────────────────────────────────────

    internal static Invoice ToDomain(InvoiceEntity e)
    {
        var issuer = ToParty(e.IssuerLegalName, e.IssuerTradeName, e.IssuerTaxIdValue,
            e.IssuerTaxIdCountryCode, e.IssuerEmail, e.IssuerPhone, e.IssuerWebsite,
            e.IssuerAddressLine1, e.IssuerAddressLine2, e.IssuerCity,
            e.IssuerProvince, e.IssuerPostalCode, e.IssuerAddressCountryCode, null);

        var recipient = ToParty(e.RecipientLegalName, e.RecipientTradeName, e.RecipientTaxIdValue,
            e.RecipientTaxIdCountryCode, e.RecipientEmail, e.RecipientPhone, e.RecipientWebsite,
            e.RecipientAddressLine1, e.RecipientAddressLine2, e.RecipientCity,
            e.RecipientProvince, e.RecipientPostalCode, e.RecipientAddressCountryCode, e.RecipientExternalId);

        var exchangeRate = ExchangeRate.Create(
            Currency.From(e.ExchangeRateFrom),
            Currency.From(e.ExchangeRateTo),
            e.ExchangeRateValue,
            e.ExchangeRateFetchedAt);

        var lines = e.Lines
            .OrderBy(l => l.LineNumber)
            .Select(l => ToLineDomain(l, e.OriginCurrencyCode))
            .ToList();

        return Invoice.Reconstitute(
            id: e.Id,
            number: InvoiceNumber.Create(e.Serie, e.Year, e.SequenceNumber),
            billingSource: e.BillingSource,
            issuer: issuer,
            recipient: recipient,
            issueDate: e.IssueDate,
            valueDate: e.ValueDate,
            createdAt: e.CreatedAt,
            lines: lines,
            appliedExchangeRate: exchangeRate,
            hash: e.Hash,
            previousHash: e.PreviousHash,
            status: Enum.Parse<InvoiceStatus>(e.Status),
            paymentReference: e.PaymentReference,
            rectifiedBy: e.RectifiedByNumber is not null
                                    ? InvoiceNumber.CreateFromFormatted(e.RectifiedByNumber)
                                    : null,
            notes: e.Notes,
            transactionData: e.TransactionData,
            paymentMethod: e.PaymentMethod,
            qrCodeBlobUrl: e.QrCodeBlobUrl);
    }

    internal static RectificativeInvoice ToRectificativeDomain(InvoiceEntity e)
    {
        var issuer = ToParty(e.IssuerLegalName, e.IssuerTradeName, e.IssuerTaxIdValue,
            e.IssuerTaxIdCountryCode, e.IssuerEmail, e.IssuerPhone, e.IssuerWebsite,
            e.IssuerAddressLine1, e.IssuerAddressLine2, e.IssuerCity,
            e.IssuerProvince, e.IssuerPostalCode, e.IssuerAddressCountryCode, null);

        var recipient = ToParty(e.RecipientLegalName, e.RecipientTradeName, e.RecipientTaxIdValue,
            e.RecipientTaxIdCountryCode, e.RecipientEmail, e.RecipientPhone, e.RecipientWebsite,
            e.RecipientAddressLine1, e.RecipientAddressLine2, e.RecipientCity,
            e.RecipientProvince, e.RecipientPostalCode, e.RecipientAddressCountryCode, e.RecipientExternalId);

        var exchangeRate = ExchangeRate.Create(
            Currency.From(e.ExchangeRateFrom),
            Currency.From(e.ExchangeRateTo),
            e.ExchangeRateValue,
            e.ExchangeRateFetchedAt);

        var lines = e.Lines
            .OrderBy(l => l.LineNumber)
            .Select(l => ToLineDomain(l, e.OriginCurrencyCode))
            .ToList();

        return RectificativeInvoice.Reconstitute(
            id: e.Id,
            number: InvoiceNumber.Create(e.Serie, e.Year, e.SequenceNumber),
            billingSource: e.BillingSource,
            originalNumber: InvoiceNumber.CreateFromFormatted(e.OriginalInvoiceNumber!),
            originalIssueDate: e.OriginalIssueDate!.Value,
            rectificationReason: e.RectificationReason!,
            rectificationType: Enum.Parse<RectificationType>(e.RectificationType!),
            issuer: issuer,
            recipient: recipient,
            issueDate: e.IssueDate,
            createdAt: e.CreatedAt,
            lines: lines,
            appliedExchangeRate: exchangeRate,
            hash: e.Hash,
            previousHash: e.PreviousHash,
            status: Enum.Parse<InvoiceStatus>(e.Status),
            paymentReference: e.PaymentReference,
            notes: e.Notes,
            rectifiedBy: e.RectifiedByNumber is not null
                ? InvoiceNumber.CreateFromFormatted(e.RectifiedByNumber)
                : null,
            transactionData: e.TransactionData,
            paymentMethod: e.PaymentMethod);
    }

    private static InvoiceLine ToLineDomain(InvoiceLineEntity l, string invoiceCurrencyCode)
    {
        var taxRate = TaxRate.Of(l.TaxRatePercentage);
        var unitPriceEur = Money.Of(l.UnitPriceEur, Currency.EUR);

        if (!l.HasCurrencyConversion)
            return InvoiceLine.CreateInEur(l.LineNumber, l.Description, l.Quantity, unitPriceEur, taxRate);

        var originCurrency = Currency.From(l.OriginCurrencyCode);
        var unitPriceOrigin = Money.Of(l.UnitPriceOrigin, originCurrency);
        return InvoiceLine.CreateWithConversion(
            l.LineNumber, l.Description, l.Quantity, unitPriceOrigin, unitPriceEur, taxRate);
    }

    private static BillingParty ToParty(
        string legalName, string? tradeName,
        string taxIdValue, string taxIdCountryCode,
        string email, string? phone, string? website,
        string line1, string? line2, string city,
        string? province, string postalCode, string countryCode,
        string? externalId)
    {
        var taxId = TaxId.Create(taxIdValue, taxIdCountryCode);
        var address = PostalAddress.Create(line1, city, postalCode, countryCode, line2, province);
        return BillingParty.Create(legalName, taxId, address, email, tradeName, phone, website, externalId);
    }
}
