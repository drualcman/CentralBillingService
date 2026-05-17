namespace CentralBillingService.Application.UseCases;

/// <summary>
/// Maps RectificativeInvoice domain entities to application DTOs.
/// Kept internal — callers never touch domain types.
/// </summary>
internal static class RectificativeInvoiceResultMapper
{
    internal static RectificativeInvoiceResult ToResult(RectificativeInvoice invoice) => new()
    {
        Id = invoice.Id,
        InvoiceNumber = invoice.Number.Value,
        BillingSource = invoice.BillingSource,
        Status = invoice.Status.ToString(),
        OriginalInvoiceNumber = invoice.OriginalInvoiceNumber.Value,
        OriginalIssueDate = invoice.OriginalIssueDate,
        RectificationReason = invoice.RectificationReason,
        RectificationType = invoice.RectificationType.ToString(),
        Issuer = InvoiceResultMapper.ToPartyResult(invoice.Issuer),
        Recipient = InvoiceResultMapper.ToPartyResult(invoice.Recipient),
        IssueDate = invoice.IssueDate,
        Lines = invoice.Lines.Select(InvoiceResultMapper.ToLineResult).ToList(),
        TaxableBaseEur = InvoiceResultMapper.ToMoneyResult(invoice.TaxableBaseEur),
        TotalTaxAmountEur = InvoiceResultMapper.ToMoneyResult(invoice.TotalTaxAmountEur),
        TotalEur = InvoiceResultMapper.ToMoneyResult(invoice.TotalEur),
        TotalInOriginCurrency = InvoiceResultMapper.ToMoneyResult(invoice.TotalInOriginCurrency),
        AppliedExchangeRate = InvoiceResultMapper.ToExchangeRateResult(invoice.AppliedExchangeRate),
        Hash = invoice.Hash,
        PreviousHash = invoice.PreviousHash,
        Notes = invoice.Notes,
        CreatedAt = invoice.CreatedAt,
        HasTamper = invoice.HasTamper,
        QrCodeBlobUrl = invoice.QrCodeBlobUrl,
    };
}
