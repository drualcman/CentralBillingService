namespace CentralBillingService.Application.Events.Arguments;

public class GenerateInvoiceArgs(
    string invoiceNumber,
    string billingSource,
    string hash,
    DateOnly issueDate,
    decimal totalEurAmount,
    string recipientTaxId,
    string? fiscalQrContent = null) : IDomainEvent
{
    public string InvoiceNumber => invoiceNumber;
    public string BillingSource => billingSource;
    public string Hash => hash;
    public DateOnly IssueDate => issueDate;
    public decimal TotalEurAmount => totalEurAmount;
    public string RecipientTaxId => recipientTaxId;

    /// <summary>Fiscal QR content (AEAT URL) when the source has a fiscal registrar; else null.</summary>
    public string? FiscalQrContent => fiscalQrContent;
}
