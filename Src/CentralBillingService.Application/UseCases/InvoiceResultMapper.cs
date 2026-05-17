namespace CentralBillingService.Application.UseCases;

/// <summary>
/// Maps domain entities to application-layer DTOs.
/// Kept internal to the Application layer — callers never touch domain types.
/// Static methods only: no state, no dependencies.
/// </summary>
internal static class InvoiceResultMapper
{
    internal static InvoiceResult ToResult(Invoice invoice) => new()
    {
        Id = invoice.Id,
        InvoiceNumber = invoice.Number.Value,
        BillingSource = invoice.BillingSource,
        Status = invoice.Status.ToString(),
        Issuer = ToPartyResult(invoice.Issuer),
        Recipient = ToPartyResult(invoice.Recipient),
        IssueDate = invoice.IssueDate,
        ValueDate = invoice.ValueDate,
        Lines = invoice.Lines.Select(ToLineResult).ToList(),
        TaxableBaseEur = ToMoneyResult(invoice.TaxableBaseEur),
        TotalTaxAmountEur = ToMoneyResult(invoice.TotalTaxAmountEur),
        TotalEur = ToMoneyResult(invoice.TotalEur),
        TotalInOriginCurrency = ToMoneyResult(invoice.TotalInOriginCurrency),
        AppliedExchangeRate = ToExchangeRateResult(invoice.AppliedExchangeRate),
        Hash = invoice.Hash,
        PreviousHash = invoice.PreviousHash,
        Notes = invoice.Notes,
        CreatedAt = invoice.CreatedAt,
        PaymentMethod = invoice.PaymentMethod,
        PaymentReference = invoice.PaymentReference,
        HasTamper = invoice.HasTamper,
        QrCodeBlobUrl = invoice.QrCodeBlobUrl,
    };

    internal static InvoiceResult ToResult(RectificativeInvoice invoice) => new()
    {
        Id = invoice.Id,
        InvoiceNumber = invoice.Number.Value,
        BillingSource = invoice.BillingSource,
        Status = invoice.Status.ToString(),
        Issuer = ToPartyResult(invoice.Issuer),
        Recipient = ToPartyResult(invoice.Recipient),
        IssueDate = invoice.IssueDate,
        ValueDate = null,
        Lines = invoice.Lines.Select(ToLineResult).ToList(),
        TaxableBaseEur = ToMoneyResult(invoice.TaxableBaseEur),
        TotalTaxAmountEur = ToMoneyResult(invoice.TotalTaxAmountEur),
        TotalEur = ToMoneyResult(invoice.TotalEur),
        TotalInOriginCurrency = ToMoneyResult(invoice.TotalInOriginCurrency),
        AppliedExchangeRate = ToExchangeRateResult(invoice.AppliedExchangeRate),
        Hash = invoice.Hash,
        PreviousHash = invoice.PreviousHash,
        Notes = invoice.Notes,
        CreatedAt = invoice.CreatedAt,
        PaymentMethod = invoice.PaymentMethod,
        PaymentReference = invoice.PaymentReference,
        HasTamper = invoice.HasTamper,
        IsRectificative = true,
        OriginalInvoiceNumber = invoice.OriginalInvoiceNumber.Value,
        RectificationReason = invoice.RectificationReason,
    };

    internal static InvoiceLineResult ToLineResult(InvoiceLine line) => new()
    {
        LineNumber = line.LineNumber,
        Description = line.Description,
        Quantity = line.Quantity,
        UnitPriceEur = ToMoneyResult(line.UnitPriceEur),
        UnitPriceOrigin = ToMoneyResult(line.UnitPriceOrigin),
        TaxableBaseEur = ToMoneyResult(line.TaxableBaseEur),
        TaxAmountEur = ToMoneyResult(line.TaxAmountEur),
        TotalEur = ToMoneyResult(line.TotalEur),
        TotalOrigin = ToMoneyResult(line.TotalOrigin),
        TaxRatePercentage = line.TaxRate.Percentage,
        HasCurrencyConversion = line.HasCurrencyConversion,
    };

    internal static MoneyResult ToMoneyResult(Money money) => new()
    {
        Amount = money.Amount,
        CurrencyCode = money.Currency.Code,
        Formatted = money.Format(),
    };

    internal static ExchangeRateResult ToExchangeRateResult(ExchangeRate rate) => new()
    {
        FromCurrency = rate.From.Code,
        ToCurrency = rate.To.Code,
        Rate = rate.Rate,
        FetchedAt = rate.FetchedAt,
        IsIdentity = rate.IsIdentity,
    };

    internal static PartyResult ToPartyResult(BillingParty party) => new()
    {
        LegalName = party.LegalName,
        TradeName = party.TradeName,
        DisplayName = party.DisplayName,
        TaxIdValue = party.TaxId.Value,
        TaxIdCountryCode = party.TaxId.CountryCode,
        Email = party.Email,
        Phone = party.Phone,
        Website = party.Website,
        AddressLine1 = party.Address.Line1,
        AddressLine2 = party.Address.Line2,
        City = party.Address.City,
        Province = party.Address.Province,
        PostalCode = party.Address.PostalCode,
        AddressCountryCode = party.Address.CountryCode,
        ExternalId = party.ExternalId,
    };
}
