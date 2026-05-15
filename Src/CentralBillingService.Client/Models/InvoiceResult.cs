namespace CentralBillingService.Client.Models;

/// <summary>
/// Result returned to the caller after a successful invoice creation.
/// Contains everything needed to display, store, or send the invoice.
/// No domain types exposed — all primitives and nested DTOs.
/// </summary>
public sealed class InvoiceResult
{
    public required Guid Id { get; init; }
    public required string InvoiceNumber { get; init; }
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
    public required string PaymentMethod { get; init; }
    public required string PaymentReference { get; init; }
}