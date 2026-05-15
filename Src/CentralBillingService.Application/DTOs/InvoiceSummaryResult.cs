namespace CentralBillingService.Application.DTOs;

/// <summary>
/// Lightweight invoice summary for list views.
/// Full detail is available via GetInvoiceUseCase.
/// </summary>
public sealed class InvoiceSummaryResult
{
    public required Guid Id { get; init; }
    public required string InvoiceNumber { get; init; }
    public required string BillingSource { get; init; }
    public required string Status { get; init; }
    public required string RecipientName { get; init; }
    public required string RecipientTaxId { get; init; }
    public string? RecipientExternalId { get; init; }
    public required DateOnly IssueDate { get; init; }
    public required MoneyResult TotalEur { get; init; }
    public required MoneyResult TotalInOriginCurrency { get; init; }

    /// <summary>
    /// True if the invoice was originally requested in a non-EUR currency.
    /// Useful to quickly identify converted invoices in list views.
    /// </summary>
    public required bool HasCurrencyConversion { get; init; }

    /// <summary>Non-null if this invoice has been rectified.</summary>
    public string? RectifiedByNumber { get; init; }

    /// <summary>True when the stored hash does not match the recomputed hash — indicates DB-level tampering.</summary>
    public bool HasTamper { get; init; }

    /// <summary>True for rectificative invoices (InvoiceType "R").</summary>
    public bool IsRectificative { get; init; }

    /// <summary>For rectificative invoices: the number of the invoice being corrected.</summary>
    public string? OriginalInvoiceNumber { get; init; }
}
