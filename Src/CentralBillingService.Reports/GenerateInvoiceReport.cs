namespace CentralBillingService.Reports;

public static class GenerateInvoiceReport
{
    public static async Task<ReportViewModel> BuildAsync(Invoice invoice, string logoUrl = "")
    {
        var setup = InvoiceReportSetupBuilder.Build(invoice.IsInOriginCurrency);
        var data = await InvoiceDataBuilder.BuildAsync(invoice, logoUrl);
        return new ReportViewModel(setup, data);
    }
}
