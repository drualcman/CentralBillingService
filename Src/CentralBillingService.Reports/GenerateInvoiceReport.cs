namespace CentralBillingService.Reports;

public static class GenerateInvoiceReport
{
    /// <summary>Renders the invoice with the printed model it was created with (<see cref="Invoice.Layout"/>).</summary>
    /// <param name="hasTamper">Outcome of the document's own integrity check (an invoice and a rectificative
    /// hash different content), which prints the tamper warning banner.</param>
    /// <param name="rectificationNotice">For a rectificative: its condition, the rectified invoice and the reason
    /// (mandatory on a corrective invoice, art. 15 RD 1619/2012). Null for an ordinary invoice.</param>
    public static Task<ReportViewModel> BuildAsync(Invoice invoice, bool hasTamper, string logoUrl = "", string? rectificationNotice = null) =>
        InvoiceReportLayouts.For(invoice.Layout)
            .BuildAsync(new InvoiceReportContent(invoice, hasTamper, logoUrl, rectificationNotice));
}
