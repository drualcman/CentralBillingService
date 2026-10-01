namespace CentralBillingService.Reports.Builders;

/// <summary>
/// Data of the A4 models: they share header, totals footer and legal blocks, and differ only in the
/// lines table (its column titles and how each line becomes body rows).
/// </summary>
internal static class InvoiceDataBuilder
{
    public const string TamperWarningText = "FACTURA MODIFICADA — La integridad de este documento ha sido comprometida";

    public static async Task<List<ColumnData>> BuildAsync(
        InvoiceReportContent content, InvoiceTableHeaders tableHeaders, Action<List<ColumnData>, Invoice> addBodyRows)
    {
        Invoice invoice = content.Invoice;
        List<ColumnData> data = new List<ColumnData>();
        await AddHeaderDataAsync(data, invoice, content.HasTamper, content.LogoUrl);
        AddTableHeaders(data, tableHeaders);
        addBodyRows(data, invoice);
        AddFooterData(data, invoice);
        await AddQrCodeDataAsync(data, invoice);
        if (!string.IsNullOrWhiteSpace(content.RectificationNotice))
            data.Add(ReportCells.Footer(InvoiceReportLayout.Columns.RectificationNotice, content.RectificationNotice));
        return data;
    }

    private static async Task AddHeaderDataAsync(List<ColumnData> data, Invoice invoice, bool hasTamper, string logoUrl)
    {
        BillingParty issuer = invoice.Issuer;
        BillingParty recipient = invoice.Recipient;

        if (hasTamper)
            data.Add(ReportCells.Header(InvoiceReportLayout.Columns.TamperWarning, TamperWarningText));

        if (!string.IsNullOrEmpty(logoUrl))
        {
            byte[] logo = await DownloadUrlHelper.GetBytes(logoUrl);
            string logoColumn = FiscalQr.IsPrinted(invoice)
                ? InvoiceReportLayout.Columns.CompanyLogoBesideFiscalQr
                : InvoiceReportLayout.Columns.CompanyLogo;
            data.Add(ReportCells.Header(logoColumn, logo));
        }

        // Trade name is the prominent name; legal name shown smaller below when they differ
        data.Add(ReportCells.Header(InvoiceReportLayout.Columns.IssuerName, issuer.TradeName ?? issuer.LegalName));
        if (issuer.TradeName is not null)
            data.Add(ReportCells.Header(InvoiceReportLayout.Columns.IssuerLegalName, issuer.LegalName));

        data.AddRange(new[]
        {
            ReportCells.Header(InvoiceReportLayout.Columns.IssuerAddress, issuer.Address.ToSingleLine()),
            ReportCells.Header(InvoiceReportLayout.Columns.IssuerTaxId, $"NIF: {issuer.TaxId.Value}"),
            ReportCells.Header(InvoiceReportLayout.Columns.InvoiceTitle, "FACTURA"),
            ReportCells.Header(InvoiceReportLayout.Columns.InvoiceNumberLabel, "Nº Factura:"),
            ReportCells.Header(InvoiceReportLayout.Columns.InvoiceNumberValue, invoice.Number.Value),
            ReportCells.Header(InvoiceReportLayout.Columns.IssuedDateLabel, "Fecha:"),
            ReportCells.Header(InvoiceReportLayout.Columns.IssuedDateValue, invoice.IssueDate.ToString("dd/MM/yyyy")),
            ReportCells.Header(InvoiceReportLayout.Columns.InfoBox, " "),
            ReportCells.Header(InvoiceReportLayout.Columns.RecipientLabel, "Cliente:"),
            ReportCells.Header(InvoiceReportLayout.Columns.RecipientName, recipient.DisplayName),
            ReportCells.Header(InvoiceReportLayout.Columns.RecipientAddress, recipient.Address.ToSingleLine()),
            ReportCells.Header(InvoiceReportLayout.Columns.RecipientTaxIdLabel, "NIF/CIF/VAT-ID:"),
            ReportCells.Header(InvoiceReportLayout.Columns.RecipientTaxIdValue, recipient.TaxId.Value),
            ReportCells.Header(InvoiceReportLayout.Columns.TableHeaderBg, " "),
        });
    }

    private static void AddTableHeaders(List<ColumnData> data, InvoiceTableHeaders headers)
    {
        AddHeaderIfPresent(data, InvoiceReportLayout.Columns.DescriptionHeader, headers.Description);
        AddHeaderIfPresent(data, InvoiceReportLayout.Columns.QtyHeader, headers.Quantity);
        AddHeaderIfPresent(data, InvoiceReportLayout.Columns.UnitPriceHeader, headers.UnitPrice);
        AddHeaderIfPresent(data, InvoiceReportLayout.Columns.TaxRateHeader, headers.TaxRate);
        AddHeaderIfPresent(data, InvoiceReportLayout.Columns.TaxableBaseHeader, headers.TaxableBase);
        AddHeaderIfPresent(data, InvoiceReportLayout.Columns.TotalHeader, headers.Total);
    }

    private static void AddHeaderIfPresent(List<ColumnData> data, string column, string? title)
    {
        if (!string.IsNullOrEmpty(title))
            data.Add(ReportCells.Header(column, title));
    }

    private static void AddFooterData(List<ColumnData> data, Invoice invoice)
    {
        string exchangeRateInfo = invoice.IsInOriginCurrency
            ? $"Tipo de cambio: 1 {invoice.AppliedExchangeRate.From} = {invoice.AppliedExchangeRate.Rate:F4} EUR"
            : string.Empty;
        string paymentReference = string.IsNullOrEmpty(invoice.PaymentReference) ? string.Empty : $"Ref.: {invoice.PaymentReference}";

        data.AddRange(new[]
        {
            ReportCells.Footer(InvoiceReportLayout.Columns.TotalSeparator, " "),
            ReportCells.Footer(InvoiceReportLayout.Columns.SubtotalLabel, "Base imponible:"),
            ReportCells.Footer(InvoiceReportLayout.Columns.SubtotalValue, ReportCells.Amount(invoice.TaxableBaseEur.Amount)),
            ReportCells.Footer(InvoiceReportLayout.Columns.TaxLabel, "IGIC total:"),
            ReportCells.Footer(InvoiceReportLayout.Columns.TaxValue, ReportCells.Amount(invoice.TotalTaxAmountEur.Amount)),
            ReportCells.Footer(InvoiceReportLayout.Columns.TotalSeparatorBottom, " "),
            ReportCells.Footer(InvoiceReportLayout.Columns.TotalLabel, "TOTAL"),
            ReportCells.Footer(InvoiceReportLayout.Columns.TotalFooterValue, ReportCells.Amount(invoice.TotalEur.Amount)),
            ReportCells.Footer(InvoiceReportLayout.Columns.PaymentMethodLabel, "Forma de pago:"),
            ReportCells.Footer(InvoiceReportLayout.Columns.PaymentMethodValue, invoice.PaymentMethod ?? string.Empty),
            ReportCells.Footer(InvoiceReportLayout.Columns.PaymentReference, paymentReference),
            ReportCells.Footer(InvoiceReportLayout.Columns.ExchangeRateRow, exchangeRateInfo),
            ReportCells.Footer(InvoiceReportLayout.Columns.NotesValue, invoice.Notes ?? string.Empty),
            ReportCells.Footer(InvoiceReportLayout.Columns.VerificationSeparator, " "),
            ReportCells.Footer(InvoiceReportLayout.Columns.VerificationTitle, "VERIFICACIÓN"),
            ReportCells.Footer(InvoiceReportLayout.Columns.BillingSourceLabel, "Código de origen:"),
            ReportCells.Footer(InvoiceReportLayout.Columns.BillingSourceValue, invoice.BillingSource),
            ReportCells.Footer(InvoiceReportLayout.Columns.HashLabel, "Hash SHA-256:"),
            ReportCells.Footer(InvoiceReportLayout.Columns.HashValue, invoice.Hash),
        });

        if (invoice.IsInOriginCurrency)
        {
            string currency = invoice.AppliedExchangeRate.From.Code;
            decimal subtotalOrigin = invoice.TotalInOriginCurrency.Amount;
            decimal taxOrigin = invoice.Lines.Sum(line => line.TotalOrigin.Amount * line.TaxRate.Percentage / 100m);

            data.Add(ReportCells.Footer(InvoiceReportLayout.Columns.SubtotalOriginValue, ReportCells.OriginAmount(subtotalOrigin, currency)));
            data.Add(ReportCells.Footer(InvoiceReportLayout.Columns.TaxOriginValue, ReportCells.OriginAmount(taxOrigin, currency)));
            data.Add(ReportCells.Footer(InvoiceReportLayout.Columns.TotalOriginFooterValue,
                ReportCells.OriginAmount(subtotalOrigin + taxOrigin, currency)));
        }
    }

    /// <summary>
    /// A fiscally-registered invoice (VeriFactu) prints the AEAT QR at the top of the first page with
    /// the mandatory "VERI*FACTU" legend below it; any other invoice keeps the system QR in the footer.
    /// </summary>
    private static async Task AddQrCodeDataAsync(List<ColumnData> data, Invoice invoice)
    {
        byte[] qrBytes = await FiscalQr.LoadImageAsync(invoice);
        if (qrBytes.Length > 0 && FiscalQr.IsPrinted(invoice))
        {
            data.Add(ReportCells.Header(InvoiceReportLayout.Columns.FiscalQrCode, qrBytes));
            data.Add(ReportCells.Header(InvoiceReportLayout.Columns.FiscalQrLegend, FiscalQr.LegendText));
        }
        else if (qrBytes.Length > 0)
        {
            data.Add(ReportCells.Footer(InvoiceReportLayout.Columns.QrCode, qrBytes));
        }
    }
}
