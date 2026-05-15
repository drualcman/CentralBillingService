namespace CentralBillingService.Application.UseCases;

/// <summary>
/// Returns a paged, filtered list of invoice summaries.
/// Read-only — no state changes.
///
/// A single query against the unified Invoices table returns both standard
/// and rectificative invoices with correct combined pagination.
/// Use GetInvoiceUseCase for the complete picture of a single invoice.
/// </summary>
public sealed class ListInvoicesUseCase
{
    private readonly IInvoiceRepository _repository;
    private readonly BillingSourceRegistry _registry;
    private readonly IInvoiceHasher _hasher;

    public ListInvoicesUseCase(IInvoiceRepository repository,
        BillingSourceRegistry registry,
        IInvoiceHasher hasher)
    {
        _repository = repository;
        _registry = registry;
        _hasher = hasher;
    }

    public async Task<InvoiceListResult> ExecuteAsync(
        ListInvoicesQuery query,
        CancellationToken cancellationToken = default)
    {
        Validate(query);

        _registry.GetConfig(query.BillingSource, query.Secret);

        var filter = MapToFilter(query);

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

    // ── Private ────────────────────────────────────────────────────────────

    private static void Validate(ListInvoicesQuery query)
    {
        if (query.Page < 1)
            throw new ArgumentException("Page must be 1 or greater.", nameof(query));

        if (query.PageSize is < 1 or > 100)
            throw new ArgumentException("PageSize must be between 1 and 100.", nameof(query));

        if (query.IssuedFrom.HasValue && query.IssuedTo.HasValue &&
            query.IssuedFrom > query.IssuedTo)
            throw new ArgumentException(
                "IssuedFrom cannot be later than IssuedTo.", nameof(query));
    }

    private static InvoiceFilter MapToFilter(ListInvoicesQuery query) => new()
    {
        BillingSource = query.BillingSource,
        Serie = query.Serie,
        Year = query.Year,
        IssuedFrom = query.IssuedFrom,
        IssuedTo = query.IssuedTo,
        RecipientTaxId = query.RecipientTaxId,
        RecipientExternalId = query.RecipientExternalId,
        Status = query.Status,
        Page = query.Page,
        PageSize = query.PageSize,
    };

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
