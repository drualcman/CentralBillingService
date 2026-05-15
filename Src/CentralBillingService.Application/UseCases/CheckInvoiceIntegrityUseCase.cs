namespace CentralBillingService.Application.UseCases;

/// <summary>
/// Admin-side integrity check: verifies that the stored invoice has not been modified
/// since it was issued by recomputing its SHA-256 hash and comparing with the stored one.
///
/// Does not require the customer's QR hash — intended for back-office tools (e.g. WPF admin).
/// For customer-facing QR verification use VerifyInvoiceIntegrityUseCase instead.
/// </summary>
public sealed class CheckInvoiceIntegrityUseCase
{
    private readonly IInvoiceRepository _repository;
    private readonly BillingSourceRegistry _registry;
    private readonly IInvoiceHasher _hasher;

    public CheckInvoiceIntegrityUseCase(
        IInvoiceRepository repository,
        BillingSourceRegistry registry,
        IInvoiceHasher hasher)
    {
        _repository = repository;
        _registry = registry;
        _hasher = hasher;
    }

    public async Task<CheckIntegrityResult> ExecuteAsync(
        CheckIntegrityQuery query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.BillingSource))
            throw new ArgumentException("BillingSource is required.", nameof(query.BillingSource));
        if (string.IsNullOrWhiteSpace(query.InvoiceNumber))
            throw new ArgumentException("InvoiceNumber is required.", nameof(query.InvoiceNumber));

        _registry.GetConfig(query.BillingSource, query.Secret);

        var invoice = await _repository.FindByNumberAsync(
            query.BillingSource, query.InvoiceNumber, cancellationToken);

        if (invoice is not null)
        {
            bool isValid = invoice.VerifyIntegrity(_hasher);
            return new CheckIntegrityResult
            {
                InvoiceNumber = query.InvoiceNumber,
                IsValid = isValid,
                Hash = invoice.Hash,
                Message = isValid
                    ? "Integrity verified: invoice data matches the stored hash."
                    : $"Integrity check failed for '{query.InvoiceNumber}': stored hash does not match recomputed hash. Possible tampering.",
            };
        }

        var rectificative = await _repository.FindRectificativeByNumberAsync(
            query.BillingSource, query.InvoiceNumber, cancellationToken);

        if (rectificative is not null)
        {
            bool isValid = rectificative.VerifyIntegrity(_hasher);
            return new CheckIntegrityResult
            {
                InvoiceNumber = query.InvoiceNumber,
                IsValid = isValid,
                Hash = rectificative.Hash,
                Message = isValid
                    ? "Integrity verified: invoice data matches the stored hash."
                    : $"Integrity check failed for '{query.InvoiceNumber}': stored hash does not match recomputed hash. Possible tampering.",
            };
        }

        throw new InvoiceNotFoundException(query.InvoiceNumber);
    }
}
