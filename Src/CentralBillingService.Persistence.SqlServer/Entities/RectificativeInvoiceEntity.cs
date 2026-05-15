namespace CentralBillingService.Persistence.SqlServer.Entities;

/// <summary>
/// EF Core entity representing the RectificativeInvoices table.
/// Separate table from Invoices — keeps each type clean and makes it
/// trivial to query how many rectificatives exist (ideally zero).
/// </summary>
public sealed class RectificativeInvoiceEntity
{
    // ── Identity ───────────────────────────────────────────────────────────
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string BillingSource { get; set; } = string.Empty;
    public string Serie { get; set; } = string.Empty;
    public int SequenceNumber { get; set; }
    public int Year { get; set; }
    public string Status { get; set; } = string.Empty;

    // ── Reference to original ──────────────────────────────────────────────
    public string OriginalInvoiceNumber { get; set; } = string.Empty;
    public DateOnly OriginalIssueDate { get; set; }
    public string RectificationReason { get; set; } = string.Empty;
    public string RectificationType { get; set; } = string.Empty;

    // ── Issuer (snapshot — same as original but stored for immutability) ───
    public string IssuerLegalName { get; set; } = string.Empty;
    public string? IssuerTradeName { get; set; }
    public string IssuerTaxIdValue { get; set; } = string.Empty;
    public string IssuerTaxIdCountryCode { get; set; } = string.Empty;
    public string IssuerEmail { get; set; } = string.Empty;
    public string? IssuerPhone { get; set; }
    public string? IssuerWebsite { get; set; }
    public string IssuerAddressLine1 { get; set; } = string.Empty;
    public string? IssuerAddressLine2 { get; set; }
    public string IssuerCity { get; set; } = string.Empty;
    public string? IssuerProvince { get; set; }
    public string IssuerPostalCode { get; set; } = string.Empty;
    public string IssuerAddressCountryCode { get; set; } = string.Empty;

    // ── Recipient (snapshot) ───────────────────────────────────────────────
    public string RecipientLegalName { get; set; } = string.Empty;
    public string? RecipientTradeName { get; set; }
    public string RecipientTaxIdValue { get; set; } = string.Empty;
    public string RecipientTaxIdCountryCode { get; set; } = string.Empty;
    public string RecipientEmail { get; set; } = string.Empty;
    public string? RecipientPhone { get; set; }
    public string? RecipientWebsite { get; set; }
    public string RecipientAddressLine1 { get; set; } = string.Empty;
    public string? RecipientAddressLine2 { get; set; }
    public string RecipientCity { get; set; } = string.Empty;
    public string? RecipientProvince { get; set; }
    public string RecipientPostalCode { get; set; } = string.Empty;
    public string RecipientAddressCountryCode { get; set; } = string.Empty;

    // ── Dates ──────────────────────────────────────────────────────────────
    public DateOnly IssueDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    // ── Totals in EUR ──────────────────────────────────────────────────────
    public decimal TaxableBaseEur { get; set; }
    public decimal TotalTaxAmountEur { get; set; }
    public decimal TotalEur { get; set; }

    // ── Origin currency totals ─────────────────────────────────────────────
    public decimal TotalOriginAmount { get; set; }
    public string OriginCurrencyCode { get; set; } = string.Empty;

    // ── Exchange rate snapshot ─────────────────────────────────────────────
    public string ExchangeRateFrom { get; set; } = string.Empty;
    public string ExchangeRateTo { get; set; } = string.Empty;
    public decimal ExchangeRateValue { get; set; }
    public DateTimeOffset ExchangeRateFetchedAt { get; set; }

    // ── VeriFactu chain ───────────────────────────────────────────────────
    public string Hash { get; set; } = string.Empty;
    public string? PreviousHash { get; set; }

    /// <summary>Número de la nueva rectificativa si esta también fue rectificada.</summary>
    public string? RectifiedByNumber { get; set; }

    public string? Notes { get; set; }

    // ── Payment ────────────────────────────────────────────────────────
    public string? PaymentMethod { get; init; }

    public string PaymentReference { get; init; }

    public string? TransactionData { get; init; }

    // ── Navigation ────────────────────────────────────────────────────────
    public ICollection<InvoiceLineEntity> Lines { get; set; } = [];
}
