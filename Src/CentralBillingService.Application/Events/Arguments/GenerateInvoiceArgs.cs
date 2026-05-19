namespace CentralBillingService.Application.Events.Arguments;

public class GenerateInvoiceArgs(
    string invoiceNumber,
    string billingSource,
    string hash,
    DateOnly issueDate,
    decimal totalEurAmount,
    string recipientTaxId) : IDomainEvent
{
    public string InvoiceNumber => invoiceNumber;
    public string BillingSource => billingSource;
    public string Hash => hash;
    public DateOnly IssueDate => issueDate;
    public decimal TotalEurAmount => totalEurAmount;
    public string RecipientTaxId => recipientTaxId;
}
