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
    private readonly IIso9001 _iso9001;

    public VerifyInvoiceIntegrityUseCase(
        IInvoiceRepository repository,
        BillingSourceRegistry registry,
        IInvoiceHasher hasher,
        IIso9001 iso9001)
    {
        _repository = repository;
        _registry = registry;
        _hasher = hasher;
        _iso9001 = iso9001;
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

        await _iso9001.Register(query.InvoiceNumber, this, "Verification requested", query);

        _registry.GetConfig(query.BillingSource);

        var invoice = await _repository.FindByNumberAsync(
            query.BillingSource, query.InvoiceNumber, cancellationToken);

        if (invoice is not null)
        {
            var result = BuildResult(
                query.InvoiceNumber, invoice.Hash, invoice.VerifyIntegrity(_hasher), query.ProvidedHash,
                invoice.Issuer.TaxId.Value, invoice.Issuer.DisplayName,
                invoice.Recipient.TaxId.Value, invoice.Recipient.DisplayName,
                invoice.IssueDate, invoice.TotalEur.Amount,
                query.ProvidedRecipientTaxId, query.ProvidedIssueDate, query.ProvidedTotalEur);

            await RegisterVerificationResult(result);
            return result;
        }

        var rectificative = await _repository.FindRectificativeByNumberAsync(
            query.BillingSource, query.InvoiceNumber, cancellationToken);

        if (rectificative is not null)
        {
            var result = BuildResult(
                query.InvoiceNumber, rectificative.Hash, rectificative.VerifyIntegrity(_hasher), query.ProvidedHash,
                rectificative.Issuer.TaxId.Value, rectificative.Issuer.DisplayName,
                rectificative.Recipient.TaxId.Value, rectificative.Recipient.DisplayName,
                rectificative.IssueDate, rectificative.TotalEur.Amount,
                query.ProvidedRecipientTaxId, query.ProvidedIssueDate, query.ProvidedTotalEur);

            await RegisterVerificationResult(result);
            return result;
        }

        throw new InvoiceNotFoundException(query.InvoiceNumber);
    }

    private async Task RegisterVerificationResult(VerifyInvoiceResult result)
    {
        if (result.IsValid)
            await _iso9001.Register(result.InvoiceNumber, this, result.Message);
        else
            await _iso9001.Error(result.InvoiceNumber, this, result.Message);
    }

    private static VerifyInvoiceResult BuildResult(
        string invoiceNumber,
        string storedHash,
        bool integrityVerified,
        string providedHash,
        string issuerTaxId,
        string issuerName,
        string recipientTaxId,
        string recipientName,
        DateOnly issueDate,
        decimal totalEur,
        string? providedRecipientTaxId,
        DateOnly? providedIssueDate,
        decimal? providedTotalEur)
    {
        bool documentHashMatches = string.Equals(providedHash, storedHash, StringComparison.OrdinalIgnoreCase);

        bool recipientTaxIdMatches = providedRecipientTaxId is null
            || string.Equals(providedRecipientTaxId.Trim(), recipientTaxId.Trim(), StringComparison.OrdinalIgnoreCase);
        bool issueDateMatches = providedIssueDate is null || providedIssueDate.Value == issueDate;
        bool amountMatches = providedTotalEur is null
            || Math.Round(providedTotalEur.Value, 2, MidpointRounding.AwayFromZero)
               == Math.Round(totalEur, 2, MidpointRounding.AwayFromZero);

        bool qrDataConsistent = recipientTaxIdMatches && issueDateMatches && amountMatches;

        bool isValid = documentHashMatches && integrityVerified && qrDataConsistent;

        var issues = new List<string>(5);
        if (!documentHashMatches)
            issues.Add("document hash mismatch");
        if (!integrityVerified)
            issues.Add("integrity check failed — possible tampering");
        if (!recipientTaxIdMatches)
            issues.Add("recipient tax ID mismatch");
        if (!issueDateMatches)
            issues.Add("issue date mismatch");
        if (!amountMatches)
            issues.Add("total amount mismatch");

        string message = issues.Count == 0
            ? "Invoice is authentic: document hash matches, integrity verified, and QR data consistent."
            : $"Verification failed for '{invoiceNumber}': {string.Join("; ", issues)}.";

        return new VerifyInvoiceResult
        {
            InvoiceNumber = invoiceNumber,
            IsValid = isValid,
            Hash = storedHash,
            DocumentHashMatches = documentHashMatches,
            IntegrityVerified = integrityVerified,
            QrDataConsistent = qrDataConsistent,
            Message = message,
            IssuerTaxId = issuerTaxId,
            IssuerName = issuerName,
            RecipientTaxId = recipientTaxId,
            RecipientName = recipientName,
            IssueDate = issueDate,
            TotalEur = totalEur,
        };
    }
}
