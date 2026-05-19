namespace CentralBillingService.Reports.Builders;

internal static class InvoiceDataBuilder
{
    private static readonly CultureInfo EsEs = new("es-ES");

    public static async Task<List<ColumnData>> BuildAsync(Invoice invoice, string logoUrl)
    {
        var data = new List<ColumnData>();
        await AddHeaderDataAsync(data, invoice, logoUrl);
        AddBodyData(data, invoice);
        await AddFooterDataAsync(data, invoice);
        return data;
    }

    private static async Task AddHeaderDataAsync(List<ColumnData> data, Invoice invoice, string logoUrl)
    {
        var issuer = invoice.Issuer;
        var recipient = invoice.Recipient;

        if (invoice.HasTamper)
            data.Add(CreateData(SectionType.Header, InvoiceReportLayout.Columns.TamperWarning,
                "FACTURA MODIFICADA — La integridad de este documento ha sido comprometida"));

        if (!string.IsNullOrEmpty(logoUrl))
        {
            byte[] logo = await DownloadUrlHelper.GetBytes(logoUrl);
            data.Add(CreateData(SectionType.Header, InvoiceReportLayout.Columns.CompanyLogo, logo));
        }

        // Trade name is the prominent name; legal name shown smaller below when they differ
        var tradeName = issuer.TradeName;
        data.Add(CreateData(SectionType.Header, InvoiceReportLayout.Columns.IssuerName,
            tradeName ?? issuer.LegalName));
        if (tradeName is not null)
            data.Add(CreateData(SectionType.Header, InvoiceReportLayout.Columns.IssuerLegalName,
                issuer.LegalName));

        data.AddRange(new[]
        {
            CreateData(SectionType.Header, InvoiceReportLayout.Columns.IssuerAddress, issuer.Address.ToSingleLine()),
            CreateData(SectionType.Header, InvoiceReportLayout.Columns.IssuerTaxId, $"NIF: {issuer.TaxId.Value}"),
            CreateData(SectionType.Header, InvoiceReportLayout.Columns.InvoiceTitle, "FACTURA"),
            CreateData(SectionType.Header, InvoiceReportLayout.Columns.InvoiceNumberLabel, "Nº Factura:"),
            CreateData(SectionType.Header, InvoiceReportLayout.Columns.InvoiceNumberValue, invoice.Number.Value),
            CreateData(SectionType.Header, InvoiceReportLayout.Columns.IssuedDateLabel, "Fecha:"),
            CreateData(SectionType.Header, InvoiceReportLayout.Columns.IssuedDateValue, invoice.IssueDate.ToString("dd/MM/yyyy")),
            CreateData(SectionType.Header, InvoiceReportLayout.Columns.InfoBox, " "),
            CreateData(SectionType.Header, InvoiceReportLayout.Columns.RecipientLabel, "Cliente:"),
            CreateData(SectionType.Header, InvoiceReportLayout.Columns.RecipientName, recipient.DisplayName),
            CreateData(SectionType.Header, InvoiceReportLayout.Columns.RecipientAddress, recipient.Address.ToSingleLine()),
            CreateData(SectionType.Header, InvoiceReportLayout.Columns.RecipientTaxIdLabel, "NIF/CIF:"),
            CreateData(SectionType.Header, InvoiceReportLayout.Columns.RecipientTaxIdValue, recipient.TaxId.Value),
            CreateData(SectionType.Header, InvoiceReportLayout.Columns.TableHeaderBg, " "),
            CreateData(SectionType.Header, InvoiceReportLayout.Columns.DescriptionHeader, "Descripción"),
            CreateData(SectionType.Header, InvoiceReportLayout.Columns.QtyHeader, "Cant."),
            CreateData(SectionType.Header, InvoiceReportLayout.Columns.UnitPriceHeader, "P.U. (€)"),
            CreateData(SectionType.Header, InvoiceReportLayout.Columns.TaxRateHeader, "IVA %"),
            CreateData(SectionType.Header, InvoiceReportLayout.Columns.TaxableBaseHeader, "Base €"),
            CreateData(SectionType.Header, InvoiceReportLayout.Columns.TotalHeader, "Total €"),
        });
    }

    private static void AddBodyData(List<ColumnData> data, Invoice invoice)
    {
        if (invoice.Lines.Count == 0)
        {
            data.Add(CreateBodyData(1, InvoiceReportLayout.Columns.DescriptionValue, "Sin líneas"));
            data.Add(CreateBodyData(1, InvoiceReportLayout.Columns.QtyValue, "0"));
            data.Add(CreateBodyData(1, InvoiceReportLayout.Columns.UnitPriceValue, "0,00"));
            data.Add(CreateBodyData(1, InvoiceReportLayout.Columns.TaxRateValue, "0%"));
            data.Add(CreateBodyData(1, InvoiceReportLayout.Columns.TaxableBaseValue, "0,00"));
            data.Add(CreateBodyData(1, InvoiceReportLayout.Columns.TotalValue, "0,00"));
            return;
        }

        foreach (var line in invoice.Lines)
        {
            data.Add(CreateBodyData(line.LineNumber, InvoiceReportLayout.Columns.DescriptionValue, line.Description));
            data.Add(CreateBodyData(line.LineNumber, InvoiceReportLayout.Columns.QtyValue, line.Quantity.ToString()));
            data.Add(CreateBodyData(line.LineNumber, InvoiceReportLayout.Columns.UnitPriceValue, FormatAmount(line.UnitPriceEur.Amount)));
            data.Add(CreateBodyData(line.LineNumber, InvoiceReportLayout.Columns.TaxRateValue, $"{line.TaxRate.Percentage}%"));
            data.Add(CreateBodyData(line.LineNumber, InvoiceReportLayout.Columns.TaxableBaseValue, FormatAmount(line.TaxableBaseEur.Amount)));
            data.Add(CreateBodyData(line.LineNumber, InvoiceReportLayout.Columns.TotalValue, FormatAmount(line.TotalEur.Amount)));
        }
    }

    private static async Task AddFooterDataAsync(List<ColumnData> data, Invoice invoice)
    {
        string exchangeRateInfo = invoice.IsInOriginCurrency
            ? $"Tipo de cambio: 1 {invoice.AppliedExchangeRate.From} = {invoice.AppliedExchangeRate.Rate:F4} EUR" +
              $"  |  Total {invoice.AppliedExchangeRate.From}: {invoice.TotalInOriginCurrency.Amount.ToString("N2", EsEs)}"
            : string.Empty;

        string paymentInfo = string.IsNullOrEmpty(invoice.PaymentMethod)
            ? invoice.PaymentReference
            : $"{invoice.PaymentMethod}  ·  Ref.: {invoice.PaymentReference}";

        data.AddRange(new[]
        {
            CreateData(SectionType.Footer, InvoiceReportLayout.Columns.TotalSeparator, " "),
            CreateData(SectionType.Footer, InvoiceReportLayout.Columns.SubtotalLabel, "Base imponible:"),
            CreateData(SectionType.Footer, InvoiceReportLayout.Columns.SubtotalValue, FormatAmount(invoice.TaxableBaseEur.Amount)),
            CreateData(SectionType.Footer, InvoiceReportLayout.Columns.TaxLabel, "IVA total:"),
            CreateData(SectionType.Footer, InvoiceReportLayout.Columns.TaxValue, FormatAmount(invoice.TotalTaxAmountEur.Amount)),
            CreateData(SectionType.Footer, InvoiceReportLayout.Columns.TotalSeparatorBottom, " "),
            CreateData(SectionType.Footer, InvoiceReportLayout.Columns.TotalLabel, "TOTAL"),
            CreateData(SectionType.Footer, InvoiceReportLayout.Columns.TotalFooterValue, FormatAmount(invoice.TotalEur.Amount)),
            CreateData(SectionType.Footer, InvoiceReportLayout.Columns.PaymentMethodLabel, "Forma de pago:"),
            CreateData(SectionType.Footer, InvoiceReportLayout.Columns.PaymentMethodValue, paymentInfo),
            CreateData(SectionType.Footer, InvoiceReportLayout.Columns.ExchangeRateRow, exchangeRateInfo),
            CreateData(SectionType.Footer, InvoiceReportLayout.Columns.NotesValue, invoice.Notes ?? string.Empty),
            CreateData(SectionType.Footer, InvoiceReportLayout.Columns.VerificationSeparator, " "),
            CreateData(SectionType.Footer, InvoiceReportLayout.Columns.VerificationTitle, "VERIFICACIÓN"),
            CreateData(SectionType.Footer, InvoiceReportLayout.Columns.BillingSourceLabel, "Código de origen:"),
            CreateData(SectionType.Footer, InvoiceReportLayout.Columns.BillingSourceValue, invoice.BillingSource),
            CreateData(SectionType.Footer, InvoiceReportLayout.Columns.HashLabel, "Hash SHA-256:"),
            CreateData(SectionType.Footer, InvoiceReportLayout.Columns.HashValue, invoice.Hash),
        });

        if (!string.IsNullOrWhiteSpace(invoice.QrCodeBlobUrl))
        {
            byte[] qrBytes = await DownloadUrlHelper.GetBytes(invoice.QrCodeBlobUrl);
            if (qrBytes.Length > 0)
                data.Add(CreateData(SectionType.Footer, InvoiceReportLayout.Columns.QrCode, qrBytes));
        }
    }

    private static string FormatAmount(decimal amount) => amount.ToString("N2", EsEs);

    private static ColumnData CreateData(SectionType section, string col, object value)
        => new() { Section = section, Column = new Item(col), Value = value };

    private static ColumnData CreateBodyData(int row, string col, object value)
        => new() { Section = SectionType.Body, Column = new Item("Detail", col), Value = value, Row = row };
}
