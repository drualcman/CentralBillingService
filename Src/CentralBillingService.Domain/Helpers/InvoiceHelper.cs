namespace CentralBillingService.Domain.Helpers;

public static class InvoiceHelper
{
    public static string GetInvoiceFileName(string billingSource, string invoiceNumber) =>
        $"{billingSource}/{invoiceNumber}.pdf";

    public static string GetQrFileName(string billingSource, string invoiceNumber) =>
        $"{billingSource}/{invoiceNumber}.png";
}
