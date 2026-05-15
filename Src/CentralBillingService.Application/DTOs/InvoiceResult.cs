namespace CentralBillingService.Application.DTOs;

/// <summary>
/// Result returned to the caller after a successful invoice creation.
/// Contains everything needed to display, store, or send the invoice.
/// No domain types exposed — all primitives and nested DTOs.
/// </summary>
public sealed class InvoiceResult
{
    public required Guid Id { get; init; }
    public required string InvoiceNumber { get; init; }
    public required string BillingSource { get; init; }
    public required string Status { get; init; }

    public required PartyResult Issuer { get; init; }
    public required PartyResult Recipient { get; init; }

    public required DateOnly IssueDate { get; init; }
    public DateOnly? ValueDate { get; init; }

    public required IReadOnlyList<InvoiceLineResult> Lines { get; init; }

    // Totals in EUR (functional currency)
    public required MoneyResult TaxableBaseEur { get; init; }
    public required MoneyResult TotalTaxAmountEur { get; init; }
    public required MoneyResult TotalEur { get; init; }

    // Total in the origin currency (what the client sees)
    public required MoneyResult TotalInOriginCurrency { get; init; }

    // Exchange rate snapshot — immutably recorded at invoice creation time
    public required ExchangeRateResult AppliedExchangeRate { get; init; }

    // VeriFactu chain
    public required string Hash { get; init; }
    public string? PreviousHash { get; init; }

    public string? Notes { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public bool HasTamper { get; init; }

    /// <summary>True when this result was loaded from a rectificative invoice.</summary>
    public bool IsRectificative { get; init; }

    /// <summary>For rectificative invoices: the number of the invoice being corrected.</summary>
    public string? OriginalInvoiceNumber { get; init; }

    /// <summary>For rectificative invoices: the stated reason for rectification.</summary>
    public string? RectificationReason { get; init; }

    /// <summary>
    /// Public URL of the QR code image in blob storage.
    /// Null if the QR has not been generated yet (best-effort post-creation step).
    /// </summary>
    public string? QrCodeBlobUrl { get; init; }
}