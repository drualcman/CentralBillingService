namespace CentralBillingService.Client.Interfaces;

public interface ICbsService
{
    Task<InvoiceCreateResult> CreateInvoiceAsync(CreateInvoiceCommand invoiceData);
    Task<RectifyInvoiceResult> RectifyInvoiceAsync(string invoiceNumber, RectifyInvoiceCommand invoiceData);
    Task<InvoiceResult> GetInvoiceAsync(string invoiceNumber);
    Task<InvoiceListResult> GetInvoicesAsync(GetInvoicesQuery? filter = null);
    Task<VerifyInvoiceResult> VerifyInvoiceAsync(string invoiceNumber, string documentHash);
    Task<ReportViewModel> GetInvoiceReportAsync(string invoiceNumber);
    Task<string> GetInvoicePdfAsync(string invoiceNumber);
}
