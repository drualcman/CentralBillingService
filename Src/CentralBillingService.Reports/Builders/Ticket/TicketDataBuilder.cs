namespace CentralBillingService.Reports.Builders.Ticket;

using Columns = TicketReportLayout.Columns;

/// <summary>
/// Data of the 80 mm ticket. Same legal content as the A4 invoice (fiscal QR + legend, issuer, number,
/// date, recipient when identified, tax totals, verification hash, rectification notice) in one column.
/// </summary>
internal static class TicketDataBuilder
{
    private const decimal SimplifiedInvoiceMaxTotalEur = 400m;

    public static async Task<List<ColumnData>> BuildAsync(InvoiceReportContent content)
    {
        Invoice invoice = content.Invoice;
        List<ColumnData> data = new List<ColumnData>();
        await AddHeaderAsync(data, content);
        AddLines(data, invoice);
        AddFooter(data, invoice, content.RectificationNotice);
        return data;
    }

    private static async Task AddHeaderAsync(List<ColumnData> data, InvoiceReportContent content)
    {
        Invoice invoice = content.Invoice;
        BillingParty issuer = invoice.Issuer;

        if (content.HasTamper)
            data.Add(ReportCells.Header(Columns.TamperWarning, "FACTURA MODIFICADA — integridad comprometida"));

        byte[] qrImage = await FiscalQr.LoadImageAsync(invoice);
        if (qrImage.Length > 0)
            data.Add(ReportCells.Header(Columns.Qr, qrImage));
        if (qrImage.Length > 0 && FiscalQr.IsPrinted(invoice))
            data.Add(ReportCells.Header(Columns.QrLegend, FiscalQr.LegendText));

        data.Add(ReportCells.Header(Columns.IssuerName, issuer.TradeName ?? issuer.LegalName));
        if (issuer.TradeName is not null)
            data.Add(ReportCells.Header(Columns.IssuerLegalName, issuer.LegalName));
        data.Add(ReportCells.Header(Columns.IssuerTaxId, $"NIF: {issuer.TaxId.Value}"));
        data.Add(ReportCells.Header(Columns.IssuerAddress, issuer.Address.ToSingleLine()));
        data.Add(ReportCells.Header(Columns.HeaderSeparator, " "));
        data.Add(ReportCells.Header(Columns.Title, Title(invoice, content.RectificationNotice)));
        data.Add(ReportCells.Header(Columns.Number, $"Nº {invoice.Number.Value}"));
        data.Add(ReportCells.Header(Columns.Date, invoice.IssueDate.ToString("dd/MM/yyyy")));
        if (HasTaxId(invoice.Recipient))
        {
            data.Add(ReportCells.Header(Columns.RecipientName, $"Cliente: {invoice.Recipient.DisplayName}"));
            data.Add(ReportCells.Header(Columns.RecipientTaxId, $"NIF: {invoice.Recipient.TaxId.Value}"));
        }
        data.Add(ReportCells.Header(Columns.LinesSeparator, " "));
    }

    private static void AddLines(List<ColumnData> data, Invoice invoice)
    {
        int row = 1;
        foreach (InvoiceLine line in invoice.Lines)
        {
            data.Add(ReportCells.Body(row, Columns.LineDescription, line.Description));
            data.Add(ReportCells.Body(row, Columns.LineTotal, ReportCells.Amount(line.TotalEur.Amount)));
            data.Add(ReportCells.Body(row, Columns.LineDetail, LineDetail(line)));
            row++;
        }
    }

    private static void AddFooter(List<ColumnData> data, Invoice invoice, string? rectificationNotice)
    {
        data.AddRange(new[]
        {
            ReportCells.Footer(Columns.TotalsSeparator, " "),
            ReportCells.Footer(Columns.BaseLabel, "Base imponible"),
            ReportCells.Footer(Columns.BaseValue, ReportCells.Amount(invoice.TaxableBaseEur.Amount)),
            ReportCells.Footer(Columns.TaxLabel, "IGIC total"),
            ReportCells.Footer(Columns.TaxValue, ReportCells.Amount(invoice.TotalTaxAmountEur.Amount)),
            ReportCells.Footer(Columns.TotalLabel, "TOTAL €"),
            ReportCells.Footer(Columns.TotalValue, ReportCells.Amount(invoice.TotalEur.Amount)),
            ReportCells.Footer(Columns.PaymentSeparator, " "),
            ReportCells.Footer(Columns.Payment, $"Pago: {invoice.PaymentMethod}"),
            ReportCells.Footer(Columns.Notes, invoice.Notes ?? string.Empty),
            ReportCells.Footer(Columns.Origin, $"Origen: {invoice.BillingSource}"),
            ReportCells.Footer(Columns.Hash, invoice.Hash),
        });

        if (!string.IsNullOrEmpty(invoice.PaymentReference))
            data.Add(ReportCells.Footer(Columns.PaymentReference, $"Ref. {invoice.PaymentReference}"));
        if (invoice.IsInOriginCurrency)
            data.Add(ReportCells.Footer(Columns.ExchangeRate,
                $"Tipo de cambio: 1 {invoice.AppliedExchangeRate.From} = {invoice.AppliedExchangeRate.Rate:F4} EUR"));
        if (!string.IsNullOrWhiteSpace(rectificationNotice))
            data.Add(ReportCells.Footer(Columns.RectificationNotice, rectificationNotice));
    }

    private static string Title(Invoice invoice, string? rectificationNotice)
    {
        string title = "FACTURA";
        if (!string.IsNullOrWhiteSpace(rectificationNotice))
        {
            title = "FACTURA RECTIFICATIVA";
        }
        else if (!HasTaxId(invoice.Recipient) && Math.Abs(invoice.TotalEur.Amount) <= SimplifiedInvoiceMaxTotalEur)
        {
            title = "FACTURA SIMPLIFICADA";
        }

        return title;
    }

    private static string LineDetail(InvoiceLine line)
    {
        string detail = $"{line.Quantity} × {ReportCells.Amount(line.UnitPriceEur.Amount)} · IGIC {line.TaxRate.Percentage}%";
        if (line.HasCurrencyConversion)
            detail += $" · {ReportCells.OriginAmount(line.UnitPriceOrigin.Amount, line.UnitPriceOrigin.Currency.Code)} c/u";
        return detail;
    }

    private static bool HasTaxId(BillingParty party) =>
        !string.IsNullOrWhiteSpace(party.TaxId.Value) && !party.TaxId.IsNotProvided;
}
