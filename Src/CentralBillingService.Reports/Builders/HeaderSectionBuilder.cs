namespace CentralBillingService.Reports.Builders;

internal static class HeaderSectionBuilder
{
    private const decimal IssuerX = 45m;
    private const decimal IssuerW = 155m;
    private const decimal MetaLabelX = 70m;
    private const decimal MetaValueX = 110m;
    private const decimal MetaW = 20m;
    private const decimal MetaValueW = 90m;
    private const decimal FiscalQrX = InvoiceReportLayout.Margin;   // AEAT: top-left, before the invoice content
    private const decimal FiscalQrY = 7m;
    private const decimal LogoSize = 30m;
    private const decimal LogoY = 10m;
    private const decimal LogoBesideFiscalQrX = 45m;
    private const decimal FiscalQrSize = 30m;   // AEAT: between 30x30 and 40x40 mm
    private const decimal FiscalQrLegendHeight = 4m;
    private const decimal IssuerBesideFiscalQrX = LogoBesideFiscalQrX + LogoSize + 5m;

    public static void Build(Section header, bool printsFiscalQr)
    {
        BuildTamperBanner(header);
        BuildLogo(header);
        BuildFiscalQr(header);
        if (printsFiscalQr)
            BuildIssuerInfoBesideFiscalQr(header);
        else
            BuildIssuerInfo(header);
        BuildInvoiceMetadata(header);
        BuildRecipientBlock(header);
        BuildTableHeader(header);
    }

    private static void BuildTamperBanner(Section header)
    {
        header.AddColumn(new ColumnSetup
        {
            Format = new Format((double)InvoiceReportLayout.ContentWidth, 7)
            {
                Position = new Kernel(2, InvoiceReportLayout.Margin),
                Background = InvoiceReportLayout.TamperWarningColor,
                TextAlignment = TextAlignment.Center,
                FontDetails = new Font(new Shade(9, InvoiceReportLayout.WhiteText), new FontStyle(700))
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.TamperWarning)
        });
    }

    private static void BuildLogo(Section header)
    {
        header.AddColumn(new ColumnSetup
        {
            Format = new Format((double)LogoSize, (double)LogoSize) { Position = new Kernel(LogoY, InvoiceReportLayout.Margin) },
            DataColumn = new Item(InvoiceReportLayout.Columns.CompanyLogo)
        });
        header.AddColumn(new ColumnSetup
        {
            Format = new Format((double)LogoSize, (double)LogoSize) { Position = new Kernel(LogoY, LogoBesideFiscalQrX) },
            DataColumn = new Item(InvoiceReportLayout.Columns.CompanyLogoBesideFiscalQr)
        });
    }

    private static void BuildFiscalQr(Section header)
    {
        header.AddColumn(new ColumnSetup
        {
            Format = new Format((double)FiscalQrSize, (double)FiscalQrSize)
            {
                Position = new Kernel(FiscalQrY, FiscalQrX)
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.FiscalQrCode)
        });
        header.AddColumn(new ColumnSetup
        {
            Format = new Format((double)FiscalQrSize, (double)FiscalQrLegendHeight)
            {
                Position = new Kernel(FiscalQrY + FiscalQrSize, FiscalQrX),
                TextAlignment = TextAlignment.Center,
                FontDetails = new Font(new Shade(9), new FontStyle(700))
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.FiscalQrLegend)
        });
    }

    private static void BuildIssuerInfo(Section header)
    {
        // Trade name — prominent
        header.AddColumn(new ColumnSetup
        {
            Format = new Format((double)IssuerW, 8)
            {
                Position = new Kernel(10, IssuerX),
                TextAlignment = TextAlignment.Right,
                FontDetails = new Font(new Shade(14), new FontStyle(700))
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.IssuerName)
        });

        // Legal name — smaller, below trade name (only emitted when trade name differs)
        header.AddColumn(new ColumnSetup
        {
            Format = new Format((double)IssuerW, 5)
            {
                Position = new Kernel(19, IssuerX),
                TextAlignment = TextAlignment.Right,
                FontDetails = new Font(new Shade(9, InvoiceReportLayout.GrayText))
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.IssuerLegalName)
        });

        header.AddColumn(new ColumnSetup
        {
            Format = new Format((double)IssuerW, 5)
            {
                Position = new Kernel(25, IssuerX),
                TextAlignment = TextAlignment.Right
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.IssuerAddress)
        });

        header.AddColumn(new ColumnSetup
        {
            Format = new Format((double)IssuerW, 5)
            {
                Position = new Kernel(31, IssuerX),
                TextAlignment = TextAlignment.Right
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.IssuerTaxId)
        });
    }

    // QR + logo take the left 75 mm: the issuer gets the narrower right side, tax id before the address
    // so a long address can wrap onto a second line without running into anything.
    private static void BuildIssuerInfoBesideFiscalQr(Section header)
    {
        const decimal width = InvoiceReportLayout.PageWidth - InvoiceReportLayout.Margin - IssuerBesideFiscalQrX;

        header.AddColumn(IssuerCell(InvoiceReportLayout.Columns.IssuerName, 10, 8, width, new Font(new Shade(14), new FontStyle(700))));
        header.AddColumn(IssuerCell(InvoiceReportLayout.Columns.IssuerLegalName, 19, 5, width, new Font(new Shade(9, InvoiceReportLayout.GrayText))));
        header.AddColumn(IssuerCell(InvoiceReportLayout.Columns.IssuerTaxId, 25, 5, width, new Font(new Shade(10))));
        header.AddColumn(IssuerCell(InvoiceReportLayout.Columns.IssuerAddress, 31, 10, width, new Font(new Shade(10))));
    }

    private static ColumnSetup IssuerCell(string column, decimal y, double height, decimal width, Font font) =>
        new ColumnSetup
        {
            Format = new Format((double)width, height)
            {
                Position = new Kernel(y, IssuerBesideFiscalQrX),
                TextAlignment = TextAlignment.Right,
                FontDetails = font
            },
            DataColumn = new Item(column)
        };

    private static void BuildInvoiceMetadata(Section header)
    {
        header.AddColumn(new ColumnSetup
        {
            Format = new Format(80, 10)
            {
                Position = new Kernel(42, InvoiceReportLayout.Margin),
                FontDetails = new Font(new Shade(18), new FontStyle(700))
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.InvoiceTitle)
        });

        header.AddColumn(new ColumnSetup
        {
            Format = new Format((double)MetaW, 5)
            {
                Position = new Kernel(42, MetaLabelX),
                TextAlignment = TextAlignment.Right
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.InvoiceNumberLabel)
        });
        header.AddColumn(new ColumnSetup
        {
            Format = new Format((double)MetaValueW, 5)
            {
                Position = new Kernel(42, MetaValueX),
                TextAlignment = TextAlignment.Right,
                FontDetails = new Font(new Shade(10), new FontStyle(700))
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.InvoiceNumberValue)
        });

        header.AddColumn(new ColumnSetup
        {
            Format = new Format((double)MetaW, 5)
            {
                Position = new Kernel(48, MetaLabelX),
                TextAlignment = TextAlignment.Right
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.IssuedDateLabel)
        });
        header.AddColumn(new ColumnSetup
        {
            Format = new Format((double)MetaValueW, 5)
            {
                Position = new Kernel(48, MetaValueX),
                TextAlignment = TextAlignment.Right
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.IssuedDateValue)
        });
    }

    private static void BuildRecipientBlock(Section header)
    {
        // InfoBox background removed — background colors cause rendering issues in PDF
        header.AddColumn(new ColumnSetup
        {
            Format = new Format((double)InvoiceReportLayout.ContentWidth, 22)
            {
                Position = new Kernel(58, InvoiceReportLayout.Margin)
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.InfoBox)
        });

        header.AddColumn(new ColumnSetup
        {
            Format = new Format(80, 5) { Position = new Kernel(61, 15m) },
            DataColumn = new Item(InvoiceReportLayout.Columns.RecipientLabel)
        });
        header.AddColumn(new ColumnSetup
        {
            Format = new Format(135, 5)
            {
                Position = new Kernel(67, 15m),
                FontDetails = new Font(new Shade(10), new FontStyle(700))
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.RecipientName)
        });
        header.AddColumn(new ColumnSetup
        {
            Format = new Format(135, 5) { Position = new Kernel(73, 15m) },
            DataColumn = new Item(InvoiceReportLayout.Columns.RecipientAddress)
        });

        header.AddColumn(new ColumnSetup
        {
            Format = new Format(45, 5) { Position = new Kernel(61, 155m) },
            DataColumn = new Item(InvoiceReportLayout.Columns.RecipientTaxIdLabel)
        });
        header.AddColumn(new ColumnSetup
        {
            Format = new Format(45, 5) { Position = new Kernel(67, 155m) },
            DataColumn = new Item(InvoiceReportLayout.Columns.RecipientTaxIdValue)
        });
    }

    private static void BuildTableHeader(Section header)
    {
        // Dark bold font replacing white-on-dark — background colors cause rendering issues in PDF
        var headerFont = new Font(new Shade(9, InvoiceReportLayout.TableHeaderColor), new FontStyle(700));

        // Background cell kept for spacing; background removed
        header.AddColumn(new ColumnSetup
        {
            Format = new Format((double)InvoiceReportLayout.ContentWidth, 8)
            {
                Position = new Kernel(83, InvoiceReportLayout.Margin)
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.TableHeaderBg)
        });

        header.AddColumn(new ColumnSetup
        {
            Format = new Format((double)InvoiceReportLayout.ColDescW, 6)
            {
                Position = new Kernel(85, InvoiceReportLayout.ColDescX + 3),
                FontDetails = headerFont
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.DescriptionHeader)
        });

        header.AddColumn(new ColumnSetup
        {
            Format = new Format((double)InvoiceReportLayout.ColQtyW, 6)
            {
                Position = new Kernel(85, InvoiceReportLayout.ColQtyX),
                TextAlignment = TextAlignment.Center,
                FontDetails = headerFont
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.QtyHeader)
        });

        header.AddColumn(new ColumnSetup
        {
            Format = new Format((double)InvoiceReportLayout.ColPriceW, 6)
            {
                Position = new Kernel(85, InvoiceReportLayout.ColPriceX),
                TextAlignment = TextAlignment.Right,
                FontDetails = headerFont
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.UnitPriceHeader)
        });

        header.AddColumn(new ColumnSetup
        {
            Format = new Format((double)InvoiceReportLayout.ColTaxW, 6)
            {
                Position = new Kernel(85, InvoiceReportLayout.ColTaxX),
                TextAlignment = TextAlignment.Center,
                FontDetails = headerFont
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.TaxRateHeader)
        });

        header.AddColumn(new ColumnSetup
        {
            Format = new Format((double)InvoiceReportLayout.ColBaseW, 6)
            {
                Position = new Kernel(85, InvoiceReportLayout.ColBaseX),
                TextAlignment = TextAlignment.Right,
                FontDetails = headerFont
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.TaxableBaseHeader)
        });

        header.AddColumn(new ColumnSetup
        {
            Format = new Format((double)InvoiceReportLayout.ColTotalW, 6)
            {
                Position = new Kernel(85, InvoiceReportLayout.ColTotalX),
                TextAlignment = TextAlignment.Right,
                FontDetails = headerFont
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.TotalHeader)
        });
    }
}
