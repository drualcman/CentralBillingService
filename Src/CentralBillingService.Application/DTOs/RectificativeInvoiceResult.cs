namespace CentralBillingService.Application.DTOs;

/// <summary>
/// Result DTO for a rectificative invoice.
/// Mirrors InvoiceResult but includes rectification-specific fields.
/// </summary>
public sealed class RectificativeInvoiceResult
{
    public required Guid Id { get; init; }
    public required string InvoiceNumber { get; init; }
    public required string BillingSource { get; init; }
    public required string Status { get; init; }

    // Reference to the original
    public required string OriginalInvoiceNumber { get; init; }
    public required DateOnly OriginalIssueDate { get; init; }
    public required string RectificationReason { get; init; }
    public required string RectificationType { get; init; }

    public required PartyResult Issuer { get; init; }
    public required PartyResult Recipient { get; init; }

    public required DateOnly IssueDate { get; init; }
    public required IReadOnlyList<InvoiceLineResult> Lines { get; init; }

    public required MoneyResult TaxableBaseEur { get; init; }
    public required MoneyResult TotalTaxAmountEur { get; init; }
    public required MoneyResult TotalEur { get; init; }
    public required MoneyResult TotalInOriginCurrency { get; init; }

    public required ExchangeRateResult AppliedExchangeRate { get; init; }

    public required string Hash { get; init; }
    public string? PreviousHash { get; init; }

    public string? Notes { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public bool HasTamper { get; init; }
}
