namespace CentralBillingService.Application.UseCases;

/// <summary>
/// Returns a paged, filtered list of invoices for the public verification portal.
/// Does NOT require billing source authentication — for use by the VerifyUI only.
/// When BillingSource is null, returns invoices across all sources.
/// </summary>
public sealed class PublicListInvoicesUseCase
{
    private readonly IInvoiceRepository _repository;
    private readonly IInvoiceHasher _hasher;

    public PublicListInvoicesUseCase(IInvoiceRepository repository, IInvoiceHasher hasher)
    {
        _repository = repository;
        _hasher = hasher;
    }

    public async Task<InvoiceListResult> ExecuteAsync(
        ListInvoicesQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.Page < 1)
            throw new ArgumentException("Page must be 1 or greater.", nameof(query));

        if (query.PageSize is < 1 or > 100)
            throw new ArgumentException("PageSize must be between 1 and 100.", nameof(query));

        if (query.IssuedFrom.HasValue && query.IssuedTo.HasValue && query.IssuedFrom > query.IssuedTo)
            throw new ArgumentException("IssuedFrom cannot be later than IssuedTo.", nameof(query));

        var filter = new InvoiceFilter
        {
            BillingSource = string.IsNullOrWhiteSpace(query.BillingSource) ? null : query.BillingSource,
            Serie = query.Serie,
            Year = query.Year,
            IssuedFrom = query.IssuedFrom,
            IssuedTo = query.IssuedTo,
            RecipientTaxId = query.RecipientTaxId,
            Status = query.Status,
            Page = query.Page,
            PageSize = query.PageSize,
        };

        var paged = await _repository.ListAsync(filter, cancellationToken);

        foreach (var item in paged.Items)
            item.VerifyIntegrity(_hasher);

        foreach (var item in paged.Rectificatives)
            item.VerifyIntegrity(_hasher);

        return new InvoiceListResult
        {
            Items = paged.Items.Select(ToSummary).ToList(),
            RectificativeItems = paged.Rectificatives.Select(ToRectificativeSummary).ToList(),
            TotalCount = paged.TotalCount,
            Page = paged.Page,
            PageSize = paged.PageSize,
            TotalPages = paged.TotalPages,
            HasNextPage = paged.HasNextPage,
        };
    }

    private static InvoiceSummaryResult ToSummary(Invoice invoice) => new()
    {
        Id = invoice.Id,
        InvoiceNumber = invoice.Number.Value,
        BillingSource = invoice.BillingSource,
        Status = invoice.Status.ToString(),
        RecipientName = invoice.Recipient.DisplayName,
        RecipientTaxId = invoice.Recipient.TaxId.Value,
        RecipientExternalId = invoice.Recipient.ExternalId,
        IssueDate = invoice.IssueDate,
        TotalEur = InvoiceResultMapper.ToMoneyResult(invoice.TotalEur),
        TotalInOriginCurrency = InvoiceResultMapper.ToMoneyResult(invoice.TotalInOriginCurrency),
        HasCurrencyConversion = invoice.IsInOriginCurrency,
        RectifiedByNumber = invoice.RectifiedBy?.Value,
        HasTamper = invoice.HasTamper,
    };

    private static InvoiceSummaryResult ToRectificativeSummary(RectificativeInvoice invoice) => new()
    {
        Id = invoice.Id,
        InvoiceNumber = invoice.Number.Value,
        BillingSource = invoice.BillingSource,
        Status = invoice.Status.ToString(),
        RecipientName = invoice.Recipient.DisplayName,
        RecipientTaxId = invoice.Recipient.TaxId.Value,
        RecipientExternalId = invoice.Recipient.ExternalId,
        IssueDate = invoice.IssueDate,
        TotalEur = InvoiceResultMapper.ToMoneyResult(invoice.TotalEur),
        TotalInOriginCurrency = InvoiceResultMapper.ToMoneyResult(invoice.TotalInOriginCurrency),
        HasCurrencyConversion = !invoice.AppliedExchangeRate.IsIdentity,
        IsRectificative = true,
        OriginalInvoiceNumber = invoice.OriginalInvoiceNumber.Value,
        RectifiedByNumber = invoice.RectifiedBy?.Value,
        HasTamper = invoice.HasTamper,
    };
}
