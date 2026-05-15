namespace CentralBillingService.Application.UseCases;

/// <summary>
/// Returns a single invoice by number or ID.
/// Throws InvoiceNotFoundException if it does not exist.
/// Read-only — no state changes.
/// </summary>
public sealed class GetInvoiceUseCase
{
    private readonly IInvoiceRepository _repository;
    private readonly BillingSourceRegistry _registry;
    private readonly IInvoiceHasher _hasher;

    public GetInvoiceUseCase(IInvoiceRepository repository,
        BillingSourceRegistry registry,
        IInvoiceHasher hasher)
    {
        _repository = repository;
        _registry = registry;
        _hasher = hasher;
    }

    public async Task<InvoiceResult> ExecuteAsync(
        GetInvoiceQuery query,
        CancellationToken cancellationToken = default)
    {
        Validate(query);

        // 2. Validate that the billing source exists before doing anything else
        _registry.GetConfig(query.BillingSource, query.Secret);

        var invoice = query.Id.HasValue
            ? await _repository.FindByIdAsync(query.BillingSource!, query.Id.Value, cancellationToken)
            : await _repository.FindByNumberAsync(query.BillingSource!, query.InvoiceNumber!, cancellationToken);

        if (invoice is not null)
        {
            invoice.VerifyIntegrity(_hasher);
            return InvoiceResultMapper.ToResult(invoice);
        }

        // If not found as a regular invoice and a number was supplied, try rectificatives.
        if (!query.Id.HasValue)
        {
            var rectificative = await _repository.FindRectificativeByNumberAsync(
                query.BillingSource!, query.InvoiceNumber!, cancellationToken);

            if (rectificative is not null)
            {
                rectificative.VerifyIntegrity(_hasher);
                return InvoiceResultMapper.ToResult(rectificative);
            }
        }

        var identifier = query.Id.HasValue ? query.Id.Value.ToString() : query.InvoiceNumber!;
        throw new InvoiceNotFoundException(identifier);
    }

    private static void Validate(GetInvoiceQuery query)
    {
        if (string.IsNullOrWhiteSpace(query.BillingSource))
            throw new ArgumentException("BillingSource is required.", nameof(query.BillingSource));

        if (!query.Id.HasValue && string.IsNullOrWhiteSpace(query.InvoiceNumber))
            throw new ArgumentException(
                "Either Id or InvoiceNumber must be provided.", nameof(query));
    }
}
