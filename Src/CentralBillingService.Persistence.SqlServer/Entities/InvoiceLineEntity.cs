namespace CentralBillingService.Persistence.SqlServer.Entities;

/// <summary>
/// EF Core entity representing invoice lines.
/// Unified FK to the Invoices table (covers both standard and rectificative invoices).
/// </summary>
public sealed class InvoiceLineEntity
{
    public Guid Id { get; set; }

    // ── Parent reference ───────────────────────────────────────────────────
    public Guid? InvoiceId { get; set; }

    // ── Line data ──────────────────────────────────────────────────────────
    public int LineNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int TaxRatePercentage { get; set; }

    // ── Amounts in EUR ─────────────────────────────────────────────────────
    public decimal UnitPriceEur { get; set; }
    public decimal TaxableBaseEur { get; set; }
    public decimal TaxAmountEur { get; set; }
    public decimal TotalEur { get; set; }

    // ── Amounts in origin currency ─────────────────────────────────────────
    public decimal UnitPriceOrigin { get; set; }
    public decimal TotalOrigin { get; set; }
    public string OriginCurrencyCode { get; set; } = string.Empty;
    public bool HasCurrencyConversion { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────
    public InvoiceEntity? Invoice { get; set; }
}
