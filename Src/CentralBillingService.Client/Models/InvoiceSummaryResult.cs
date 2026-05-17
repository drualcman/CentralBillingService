namespace CentralBillingService.Client.Models;

/// <summary>
/// Lightweight invoice summary for list views.
/// Full detail is available via GetInvoiceUseCase.
/// </summary>
public sealed class InvoiceSummaryResult
{
    public required Guid Id { get; init; }
    public required string InvoiceNumber { get; init; }
    public required string Status { get; init; }
    public required string RecipientName { get; init; }
    public required string RecipientTaxId { get; init; }
    public string? RecipientExternalId { get; init; }
    public required DateOnly IssueDate { get; init; }
    public required MoneyResult TotalEur { get; init; }
    public required MoneyResult TotalInOriginCurrency { get; init; }
    public required bool HasCurrencyConversion { get; init; }
    public string? RectifiedByNumber { get; init; }
    public bool HasTamper { get; init; }
    public bool IsRectificative { get; init; }
    public string? OriginalInvoiceNumber { get; init; }
}
