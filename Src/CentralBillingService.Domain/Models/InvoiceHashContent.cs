namespace CentralBillingService.Domain.Models;

/// <summary>
/// Fields that feed into the VeriFactu hash computation.
/// Immutable by design — represents a fixed point in time.
///
/// Covers ALL data that must not be altered after invoice creation:
/// issuer identity, recipient identity and address, invoice lines
/// (description, quantities, prices, tax rates), payment data, and chain link.
/// </summary>
public sealed class InvoiceHashContent
{
    // ── Issuer ─────────────────────────────────────────────────────────────

    /// <summary>Issuer tax ID — IDEmisorFactura</summary>
    public string IssuerTaxId { get; init; } = string.Empty;

    /// <summary>Extended — issuer identity and address (not in VeriFactu spec, part of our audit trail)</summary>
    public string IssuerLegalName { get; init; } = string.Empty;
    public string IssuerAddressLine1 { get; init; } = string.Empty;
    public string IssuerCity { get; init; } = string.Empty;
    public string IssuerPostalCode { get; init; } = string.Empty;
    public string IssuerCountryCode { get; init; } = string.Empty;

    // ── Invoice identity ───────────────────────────────────────────────────

    /// <summary>Invoice number e.g. "FOTO2026-0001" — NumSerieFactura</summary>
    public string InvoiceNumber { get; init; } = string.Empty;

    /// <summary>Issue date in YYYY-MM-DD format — FechaExpedicionFactura</summary>
    public string IssueDate { get; init; } = string.Empty;

    /// <summary>
    /// Document type — TipoFactura.
    /// "F" for standard invoices, "R" for rectificative invoices.
    /// </summary>
    public string InvoiceType { get; init; } = "F";

    /// <summary>UTC creation timestamp in ISO 8601 format — FechaHoraHuella</summary>
    public string CreatedAt { get; init; } = string.Empty;

    /// <summary>Billing source — extended field for our own audit trail</summary>
    public string BillingSource { get; init; } = string.Empty;

    // ── Recipient ──────────────────────────────────────────────────────────

    public string RecipientTaxId { get; init; } = string.Empty;
    public string RecipientLegalName { get; init; } = string.Empty;
    public string RecipientAddressLine1 { get; init; } = string.Empty;
    public string RecipientCity { get; init; } = string.Empty;
    public string RecipientPostalCode { get; init; } = string.Empty;
    public string RecipientCountryCode { get; init; } = string.Empty;

    /// <summary>External ID of the recipient in the billing source's own system.</summary>
    public string RecipientExternalId { get; init; } = string.Empty;

    // ── Totals (EUR) ───────────────────────────────────────────────────────

    /// <summary>Total VAT amount in EUR with dot as decimal separator — CuotaTotal</summary>
    public string TotalTaxAmountEur { get; init; } = string.Empty;

    /// <summary>Total invoice amount in EUR with dot as decimal separator — ImporteTotal</summary>
    public string TotalAmountEur { get; init; } = string.Empty;

    // ── Payment ────────────────────────────────────────────────────────────

    public string PaymentReference { get; init; } = string.Empty;
    public string PaymentMethod { get; init; } = string.Empty;
    public string TransactionData { get; init; } = string.Empty;

    // ── Rectificative reference (empty for regular invoices) ───────────────

    /// <summary>Number of the original invoice being rectified. Empty for standard invoices.</summary>
    public string OriginalInvoiceNumber { get; init; } = string.Empty;

    // ── Lines ──────────────────────────────────────────────────────────────

    /// <summary>
    /// One entry per invoice line, ordered by LineNumber.
    /// Any change to description, quantity, price, or tax rate will invalidate the hash.
    /// </summary>
    public IReadOnlyList<InvoiceLineHashContent> Lines { get; init; } = [];
}
