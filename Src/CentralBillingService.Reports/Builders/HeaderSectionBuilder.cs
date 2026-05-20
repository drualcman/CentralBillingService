namespace CentralBillingService.Reports.Builders;

internal static class HeaderSectionBuilder
{
    private const decimal IssuerX = 45m;
    private const decimal IssuerW = 155m;
    private const decimal MetaLabelX = 140m;
    private const decimal MetaValueX = 170m;
    private const decimal MetaW = 30m;

    public static void Build(Section header)
    {
        BuildTamperBanner(header);
        BuildLogo(header);
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
            Format = new Format(30, 30) { Position = new Kernel(10, 10) },
            DataColumn = new Item(InvoiceReportLayout.Columns.CompanyLogo)
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
            Format = new Format((double)MetaW, 5)
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
            Format = new Format((double)MetaW, 5)
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
            Format = new Format(80, 5)
            {
                Position = new Kernel(67, 15m),
                FontDetails = new Font(new Shade(10), new FontStyle(700))
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.RecipientName)
        });
        header.AddColumn(new ColumnSetup
        {
            Format = new Format(95, 5) { Position = new Kernel(73, 15m) },
            DataColumn = new Item(InvoiceReportLayout.Columns.RecipientAddress)
        });

        header.AddColumn(new ColumnSetup
        {
            Format = new Format(80, 5) { Position = new Kernel(61, 110m) },
            DataColumn = new Item(InvoiceReportLayout.Columns.RecipientTaxIdLabel)
        });
        header.AddColumn(new ColumnSetup
        {
            Format = new Format(80, 5) { Position = new Kernel(67, 110m) },
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
