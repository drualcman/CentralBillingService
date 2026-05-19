namespace CentralBillingService.Reports;

public static class GenerateInvoiceReport
{
    public static async Task<ReportViewModel> BuildAsync(Invoice invoice)
    {
        var setup = InvoiceReportSetupBuilder.Build();
        var data = await InvoiceDataBuilder.BuildAsync(invoice);
        return new ReportViewModel(setup, data);
    }
}
