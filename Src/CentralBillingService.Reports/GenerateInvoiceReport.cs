namespace CentralBillingService.Reports;

public static class GenerateInvoiceReport
{
    public static async Task<ReportViewModel> BuildAsync(Invoice invoice, string logoUrl = "")
    {
        // Use wide rows when the invoice OR any individual line has a non-EUR currency.
        // An invoice where all lines share the same origin currency sets IsInOriginCurrency=true,
        // but a mixed-currency invoice may leave IsInOriginCurrency=false while lines still need
        // the extra sub-row space for their origin values.
        var hasOriginCurrency = invoice.IsInOriginCurrency || invoice.Lines.Any(l => l.HasCurrencyConversion);
        var setup = InvoiceReportSetupBuilder.Build(hasOriginCurrency);
        var data = await InvoiceDataBuilder.BuildAsync(invoice, logoUrl);
        return new ReportViewModel(setup, data);
    }
}
