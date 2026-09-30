namespace CentralBillingService.Reports;

public static class GenerateInvoiceReport
{
    /// <param name="hasTamper">Outcome of the document's own integrity check (an invoice and a rectificative
    /// hash different content), which prints the tamper warning banner.</param>
    /// <param name="rectificationNotice">For a rectificative: its condition, the rectified invoice and the reason
    /// (mandatory on a corrective invoice, art. 15 RD 1619/2012). Null for an ordinary invoice.</param>
    public static async Task<ReportViewModel> BuildAsync(Invoice invoice, bool hasTamper, string logoUrl = "", string? rectificationNotice = null)
    {
        // Use wide rows when the invoice OR any individual line has a non-EUR currency.
        // An invoice where all lines share the same origin currency sets IsInOriginCurrency=true,
        // but a mixed-currency invoice may leave IsInOriginCurrency=false while lines still need
        // the extra sub-row space for their origin values.
        var hasOriginCurrency = invoice.IsInOriginCurrency || invoice.Lines.Any(l => l.HasCurrencyConversion);
        var setup = InvoiceReportSetupBuilder.Build(hasOriginCurrency);
        var data = await InvoiceDataBuilder.BuildAsync(invoice, hasTamper, logoUrl, rectificationNotice);
        return new ReportViewModel(setup, data);
    }
}
