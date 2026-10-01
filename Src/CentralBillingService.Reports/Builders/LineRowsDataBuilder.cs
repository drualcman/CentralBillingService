namespace CentralBillingService.Reports.Builders;

/// <summary>
/// Body rows of the A4 models. A product row fills every column; a subscription row only a wide
/// description and the line total (tax included).
/// </summary>
internal static class LineRowsDataBuilder
{
    public static void AddProductRows(List<ColumnData> data, IEnumerable<InvoiceLine> lines, int firstRow)
    {
        int row = firstRow;
        foreach (InvoiceLine line in lines)
        {
            AddProductRow(data, row, line);
            row++;
        }
    }

    public static void AddSubscriptionRows(List<ColumnData> data, IEnumerable<InvoiceLine> lines, int firstRow)
    {
        int row = firstRow;
        foreach (InvoiceLine line in lines)
        {
            AddSubscriptionRow(data, row, line);
            row++;
        }
    }

    public static void AddEmptyRow(List<ColumnData> data) =>
        data.Add(ReportCells.Body(1, InvoiceReportLayout.Columns.DescriptionValue, "Sin líneas"));

    private static void AddProductRow(List<ColumnData> data, int row, InvoiceLine line)
    {
        data.Add(ReportCells.Body(row, InvoiceReportLayout.Columns.DescriptionValue, line.Description));
        data.Add(ReportCells.Body(row, InvoiceReportLayout.Columns.QtyValue, line.Quantity.ToString()));
        data.Add(ReportCells.Body(row, InvoiceReportLayout.Columns.UnitPriceValue, ReportCells.Amount(line.UnitPriceEur.Amount)));
        data.Add(ReportCells.Body(row, InvoiceReportLayout.Columns.TaxRateValue, $"{line.TaxRate.Percentage}%"));
        data.Add(ReportCells.Body(row, InvoiceReportLayout.Columns.TaxableBaseValue, ReportCells.Amount(line.TaxableBaseEur.Amount)));
        data.Add(ReportCells.Body(row, InvoiceReportLayout.Columns.TotalValue, ReportCells.Amount(line.TotalEur.Amount)));

        if (line.HasCurrencyConversion)
        {
            string currency = line.UnitPriceOrigin.Currency.Code;
            data.Add(ReportCells.Body(row, InvoiceReportLayout.Columns.UnitPriceOriginValue,
                ReportCells.OriginAmount(line.UnitPriceOrigin.Amount, currency)));
            data.Add(ReportCells.Body(row, InvoiceReportLayout.Columns.TaxableBaseOriginValue,
                ReportCells.OriginAmount(line.TotalOrigin.Amount, currency)));
            data.Add(ReportCells.Body(row, InvoiceReportLayout.Columns.TotalOriginValue,
                ReportCells.OriginAmount(OriginTotalWithTax(line), currency)));
        }
    }

    private static void AddSubscriptionRow(List<ColumnData> data, int row, InvoiceLine line)
    {
        data.Add(ReportCells.Body(row, InvoiceReportLayout.Columns.SubscriptionDescriptionValue, line.Description));
        data.Add(ReportCells.Body(row, InvoiceReportLayout.Columns.TotalValue, ReportCells.Amount(line.TotalEur.Amount)));

        if (line.HasCurrencyConversion)
        {
            data.Add(ReportCells.Body(row, InvoiceReportLayout.Columns.TotalOriginValue,
                ReportCells.OriginAmount(OriginTotalWithTax(line), line.UnitPriceOrigin.Currency.Code)));
        }
    }

    private static decimal OriginTotalWithTax(InvoiceLine line) =>
        line.TotalOrigin.Amount + line.TotalOrigin.Amount * line.TaxRate.Percentage / 100m;
}
