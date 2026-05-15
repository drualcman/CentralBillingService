namespace CentralBillingService.Infrastructure.Hashing;

/// <summary>
/// Computes the chained SHA-256 hash required by the VeriFactu specification
/// (Real Decreto 1007/2023, Anexo II).
///
/// VeriFactu hash chain rules:
/// - Each invoice hash must incorporate the previous invoice's hash in the same series.
/// - The canonical string is built from a fixed set of fields in a defined order,
///   separated by the "&amp;" character, with no trailing separator.
/// - The result is expressed as uppercase hexadecimal.
/// - If the invoice is the first in its series, the previous hash field is empty.
///
/// Core VeriFactu fields (in spec order):
///   IDEmisorFactura &amp; NumSerieFactura &amp; FechaExpedicionFactura &amp;
///   TipoFactura &amp; CuotaTotal &amp; ImporteTotal &amp; FechaHoraHuella
///
/// Extended fields (our audit trail, appended after VeriFactu fields):
///   BillingSource, recipient identity and address, payment data,
///   original invoice reference (rectificatives only),
///   one group of fields per invoice line (ordered by line number),
///   and finally the previous hash (Huella) to form the chain.
/// </summary>
public sealed class Sha256InvoiceHasher : IInvoiceHasher
{
    private const char Separator = '&';

    public string Compute(InvoiceHashContent content, string? previousHash)
    {
        var canonical = BuildCanonicalString(content, previousHash ?? string.Empty);
        return ComputeSha256Hex(canonical);
    }

    public bool Verify(InvoiceHashContent content, string? previousHash, string storedHash)
    {
        var recomputed = Compute(content, previousHash);
        return string.Equals(recomputed, storedHash, StringComparison.OrdinalIgnoreCase);
    }

    // ── Private ────────────────────────────────────────────────────────────

    private static string BuildCanonicalString(InvoiceHashContent content, string previousHash)
    {
        var segments = new List<string>
        {
            // VeriFactu-mandated fields first, in spec order
            Sanitize(content.IssuerTaxId),             // IDEmisorFactura
            Sanitize(content.InvoiceNumber),           // NumSerieFactura
            Sanitize(content.IssueDate),               // FechaExpedicionFactura
            Sanitize(content.InvoiceType),             // TipoFactura: "F" or "R"
            Sanitize(content.TotalTaxAmountEur),       // CuotaTotal
            Sanitize(content.TotalAmountEur),          // ImporteTotal
            Sanitize(content.CreatedAt),               // FechaHoraHuella

            // Extended — billing context
            Sanitize(content.BillingSource),

            // Extended — issuer identity and address
            Sanitize(content.IssuerLegalName),
            Sanitize(content.IssuerAddressLine1),
            Sanitize(content.IssuerCity),
            Sanitize(content.IssuerPostalCode),
            Sanitize(content.IssuerCountryCode),

            Sanitize(content.PaymentReference),

            // Extended — recipient identity and address
            Sanitize(content.RecipientTaxId),
            Sanitize(content.RecipientLegalName),
            Sanitize(content.RecipientAddressLine1),
            Sanitize(content.RecipientCity),
            Sanitize(content.RecipientPostalCode),
            Sanitize(content.RecipientCountryCode),
            Sanitize(content.RecipientExternalId),

            // Extended — payment data
            Sanitize(content.PaymentMethod),
            Sanitize(content.TransactionData),

            // Extended — rectificative reference (empty for standard invoices)
            Sanitize(content.OriginalInvoiceNumber),
        };

        // Extended — one group per invoice line, ordered by line number
        foreach (var line in content.Lines.OrderBy(l => l.LineNumber))
        {
            segments.Add(Sanitize(line.LineNumber));
            segments.Add(Sanitize(line.Description));
            segments.Add(Sanitize(line.Quantity));
            segments.Add(Sanitize(line.UnitPriceEur));
            segments.Add(Sanitize(line.TaxRatePercentage));
            segments.Add(Sanitize(line.TotalEur));
        }

        // Huella — always last to close the chain
        segments.Add(Sanitize(previousHash));

        return string.Join(Separator, segments);
    }

    private static string ComputeSha256Hex(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash); // uppercase, no dashes
    }

    /// <summary>
    /// Removes characters that could alter the canonical string structure.
    /// The '&amp;' separator must never appear inside a field value.
    /// </summary>
    private static string Sanitize(string? value) =>
        (value ?? string.Empty).Trim().Replace("&", string.Empty);
}
