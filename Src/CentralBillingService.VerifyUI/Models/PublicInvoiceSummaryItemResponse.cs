namespace CentralBillingService.VerifyUI.Models;

public sealed class PublicInvoiceSummaryItemResponse
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = "";
    public string BillingSource { get; set; } = "";
    public string Status { get; set; } = "";
    public string RecipientName { get; set; } = "";
    public string RecipientTaxId { get; set; } = "";
    public DateOnly IssueDate { get; set; }
    public MoneyAmountResponse TotalEur { get; set; } = new();
    public MoneyAmountResponse TotalInOriginCurrency { get; set; } = new();
    public bool HasCurrencyConversion { get; set; }
    public bool IsRectificative { get; set; }
    public string? OriginalInvoiceNumber { get; set; }
    public string? RectifiedByNumber { get; set; }
    public bool HasTamper { get; set; }
}

public sealed class MoneyAmountResponse
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EUR";
}
