namespace CentralBillingService.Application.UseCases;

/// <summary>
/// Verifies an invoice from two independent angles:
///
///   1. Document check: the hash from the customer's QR matches the stored hash,
///      confirming the document in hand is the original issued invoice.
///
///   2. Integrity check: recomputes the SHA-256 hash from the stored DB fields
///      and compares it against the stored hash, detecting any external DB tampering.
///
/// IsValid = true only when both checks pass.
///
/// When a country-specific system (e.g. VeriFactu) is implemented, add a third check
/// here without touching the existing logic.
/// </summary>
public sealed class VerifyInvoiceIntegrityUseCase
{
    private readonly IInvoiceRepository _repository;
    private readonly BillingSourceRegistry _registry;
    private readonly IInvoiceHasher _hasher;

    public VerifyInvoiceIntegrityUseCase(
        IInvoiceRepository repository,
        BillingSourceRegistry registry,
        IInvoiceHasher hasher)
    {
        _repository = repository;
        _registry = registry;
        _hasher = hasher;
    }

    public async Task<VerifyInvoiceResult> ExecuteAsync(
        VerifyInvoiceQuery query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.BillingSource))
            throw new ArgumentException("BillingSource is required.", nameof(query.BillingSource));
        if (string.IsNullOrWhiteSpace(query.InvoiceNumber))
            throw new ArgumentException("InvoiceNumber is required.", nameof(query.InvoiceNumber));
        if (string.IsNullOrWhiteSpace(query.ProvidedHash))
            throw new ArgumentException("ProvidedHash is required.", nameof(query.ProvidedHash));

        _registry.GetConfig(query.BillingSource);

        var invoice = await _repository.FindByNumberAsync(
            query.BillingSource, query.InvoiceNumber, cancellationToken);

        if (invoice is not null)
            return BuildResult(
                query.InvoiceNumber, invoice.Hash, invoice.VerifyIntegrity(_hasher), query.ProvidedHash,
                invoice.Issuer.TaxId.Value, invoice.Issuer.DisplayName, invoice.IssueDate, invoice.TotalEur.Amount);

        var rectificative = await _repository.FindRectificativeByNumberAsync(
            query.BillingSource, query.InvoiceNumber, cancellationToken);

        if (rectificative is not null)
            return BuildResult(
                query.InvoiceNumber, rectificative.Hash, rectificative.VerifyIntegrity(_hasher), query.ProvidedHash,
                rectificative.Issuer.TaxId.Value, rectificative.Issuer.DisplayName, rectificative.IssueDate, rectificative.TotalEur.Amount);

        throw new InvoiceNotFoundException(query.InvoiceNumber);
    }

    private static VerifyInvoiceResult BuildResult(
        string invoiceNumber,
        string storedHash,
        bool integrityVerified,
        string providedHash,
        string issuerTaxId,
        string issuerName,
        DateOnly issueDate,
        decimal totalEur)
    {
        bool documentHashMatches = string.Equals(providedHash, storedHash, StringComparison.OrdinalIgnoreCase);
        bool isValid = documentHashMatches && integrityVerified;

        string message = (documentHashMatches, integrityVerified) switch
        {
            (true, true) => "Invoice is authentic: document hash matches and integrity verified.",
            (true, false) => $"Integrity check failed for '{invoiceNumber}': stored hash does not match recomputed hash. Possible tampering.",
            (false, true) => "Document hash mismatch: the provided hash does not match the stored one. The document may not be the original issued invoice.",
            (false, false) => $"Both checks failed for '{invoiceNumber}': document hash mismatch and integrity check failed.",
        };

        return new VerifyInvoiceResult
        {
            InvoiceNumber = invoiceNumber,
            IsValid = isValid,
            Hash = storedHash,
            DocumentHashMatches = documentHashMatches,
            IntegrityVerified = integrityVerified,
            Message = message,
            IssuerTaxId = issuerTaxId,
            IssuerName = issuerName,
            IssueDate = issueDate,
            TotalEur = totalEur,
        };
    }
}
