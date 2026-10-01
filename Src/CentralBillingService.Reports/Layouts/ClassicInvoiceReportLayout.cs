namespace CentralBillingService.Reports.Layouts;

/// <summary>The classic A4 invoice: one table with quantity, unit price, tax rate, base and total per line.</summary>
internal sealed class ClassicInvoiceReportLayout : IInvoiceReportLayout
{
    public string Name => InvoiceLayoutNames.Invoice;

    public async Task<ReportViewModel> BuildAsync(InvoiceReportContent content)
    {
        List<ColumnData> data = await InvoiceDataBuilder.BuildAsync(content, InvoiceTableHeaders.Products, AddRows);
        return new ReportViewModel(A4ReportSetup.For(content.Invoice), data);
    }

    private static void AddRows(List<ColumnData> data, Invoice invoice)
    {
        if (invoice.Lines.Count == 0)
            LineRowsDataBuilder.AddEmptyRow(data);
        else
            LineRowsDataBuilder.AddProductRows(data, invoice.Lines, firstRow: 1);
    }
}
