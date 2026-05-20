using DigitalDoor.Reporting.Entities.Models;
using DigitalDoor.Reporting.Entities.ValueObjects;

namespace CentralBillingService.Reports.Builders;

internal static class FooterSectionBuilder
{
    private const decimal RightLabelX = 120m;
    private const decimal RightValueX = 155m;
    private const decimal RightColW = 35m;
    private const decimal LeftColW = 110m;
    private const decimal VerifLabelW = 40m;

    public static void Build(Section footer)
    {
        BuildTopSeparator(footer);
        BuildTotals(footer);
        BuildPaymentInfo(footer);
        BuildExchangeRateRow(footer);
        BuildNotesRow(footer);
        BuildVerificationSection(footer);
    }

    private static void BuildTopSeparator(Section footer)
    {
        footer.AddColumn(new ColumnSetup
        {
            Format = new Format((double)InvoiceReportLayout.ContentWidth, 1)
            {
                Position = new Kernel(2, InvoiceReportLayout.Margin),
                Background = InvoiceReportLayout.SeparatorColor
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.TotalSeparator)
        });
    }

    private static void BuildTotals(Section footer)
    {
        var originFont = new Font(new Shade(8, InvoiceReportLayout.GrayText));

        footer.AddColumn(new ColumnSetup
        {
            Format = new Format((double)RightColW, 5)
            {
                Position = new Kernel(5, RightLabelX),
                TextAlignment = TextAlignment.Right
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.SubtotalLabel)
        });
        footer.AddColumn(new ColumnSetup
        {
            Format = new Format((double)RightColW, 5)
            {
                Position = new Kernel(5, RightValueX),
                TextAlignment = TextAlignment.Right
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.SubtotalValue)
        });
        footer.AddColumn(new ColumnSetup
        {
            Format = new Format((double)RightColW, 4)
            {
                Position = new Kernel(10, RightValueX),
                TextAlignment = TextAlignment.Right,
                FontDetails = originFont
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.SubtotalOriginValue)
        });

        footer.AddColumn(new ColumnSetup
        {
            Format = new Format((double)RightColW, 5)
            {
                Position = new Kernel(15, RightLabelX),
                TextAlignment = TextAlignment.Right
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.TaxLabel)
        });
        footer.AddColumn(new ColumnSetup
        {
            Format = new Format((double)RightColW, 5)
            {
                Position = new Kernel(15, RightValueX),
                TextAlignment = TextAlignment.Right
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.TaxValue)
        });
        footer.AddColumn(new ColumnSetup
        {
            Format = new Format((double)RightColW, 4)
            {
                Position = new Kernel(20, RightValueX),
                TextAlignment = TextAlignment.Right,
                FontDetails = originFont
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.TaxOriginValue)
        });

        footer.AddColumn(new ColumnSetup
        {
            Format = new Format((double)InvoiceReportLayout.ContentWidth, 0.5)
            {
                Position = new Kernel(26, InvoiceReportLayout.Margin),
                Background = InvoiceReportLayout.SeparatorColor
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.TotalSeparatorBottom)
        });

        var totalFont = new Font(new Shade(16), new FontStyle(700));
        footer.AddColumn(new ColumnSetup
        {
            Format = new Format((double)RightColW, 10)
            {
                Position = new Kernel(28, RightLabelX),
                TextAlignment = TextAlignment.Right,
                FontDetails = totalFont
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.TotalLabel)
        });
        footer.AddColumn(new ColumnSetup
        {
            Format = new Format((double)RightColW, 10)
            {
                Position = new Kernel(28, RightValueX),
                TextAlignment = TextAlignment.Right,
                FontDetails = totalFont
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.TotalFooterValue)
        });
        footer.AddColumn(new ColumnSetup
        {
            Format = new Format((double)RightColW, 5)
            {
                Position = new Kernel(38, RightValueX),
                TextAlignment = TextAlignment.Right,
                FontDetails = new Font(new Shade(8, InvoiceReportLayout.GrayText), new FontStyle(700))
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.TotalOriginFooterValue)
        });
    }

    private static void BuildPaymentInfo(Section footer)
    {
        footer.AddColumn(new ColumnSetup
        {
            Format = new Format(50, 5) { Position = new Kernel(5, InvoiceReportLayout.Margin) },
            DataColumn = new Item(InvoiceReportLayout.Columns.PaymentMethodLabel)
        });
        footer.AddColumn(new ColumnSetup
        {
            Format = new Format((double)LeftColW, 5) { Position = new Kernel(11, InvoiceReportLayout.Margin) },
            DataColumn = new Item(InvoiceReportLayout.Columns.PaymentMethodValue)
        });
    }

    private static void BuildExchangeRateRow(Section footer)
    {
        footer.AddColumn(new ColumnSetup
        {
            Format = new Format((double)InvoiceReportLayout.ContentWidth, 5)
            {
                Position = new Kernel(17, InvoiceReportLayout.Margin),
                FontDetails = new Font(new Shade(8, InvoiceReportLayout.GrayText))
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.ExchangeRateRow)
        });
    }

    private static void BuildNotesRow(Section footer)
    {
        footer.AddColumn(new ColumnSetup
        {
            Format = new Format((double)InvoiceReportLayout.ContentWidth, 5)
            {
                Position = new Kernel(44, InvoiceReportLayout.Margin),
                FontDetails = new Font(new Shade(9, InvoiceReportLayout.GrayText))
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.NotesValue)
        });
    }

    private static void BuildVerificationSection(Section footer)
    {
        var smallGrayFont = new Font(new Shade(8, InvoiceReportLayout.GrayText));
        var smallBoldFont = new Font(new Shade(8), new FontStyle(700));
        var hashFont = new Font(new Shade(7, "#555555"));
        const decimal QrX = InvoiceReportLayout.Margin;       // QR en lado izquierdo
        const decimal QrSize = 22m;
        const decimal ContentX = QrX + QrSize + 3m;           // 35mm: tras QR + 3mm de margen
        const decimal ValueX = ContentX + VerifLabelW;        // 75mm
        double contentW = (double)(InvoiceReportLayout.ContentWidth - QrSize - 3m); // 165mm
        double valueW = (double)(InvoiceReportLayout.Margin + InvoiceReportLayout.ContentWidth - ValueX); // 125mm

        footer.AddColumn(new ColumnSetup
        {
            Format = new Format((double)InvoiceReportLayout.ContentWidth, 0.5)
            {
                Position = new Kernel(49, InvoiceReportLayout.Margin),
                Background = InvoiceReportLayout.SeparatorColor
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.VerificationSeparator)
        });

        footer.AddColumn(new ColumnSetup
        {
            Format = new Format(contentW, 5)
            {
                Position = new Kernel(51, ContentX),
                FontDetails = smallGrayFont
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.VerificationTitle)
        });

        footer.AddColumn(new ColumnSetup
        {
            Format = new Format((double)VerifLabelW, 5)
            {
                Position = new Kernel(57, ContentX),
                FontDetails = smallGrayFont
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.BillingSourceLabel)
        });
        footer.AddColumn(new ColumnSetup
        {
            Format = new Format(valueW, 5)
            {
                Position = new Kernel(57, ValueX),
                FontDetails = smallBoldFont
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.BillingSourceValue)
        });

        footer.AddColumn(new ColumnSetup
        {
            Format = new Format((double)VerifLabelW, 5)
            {
                Position = new Kernel(63, ContentX),
                FontDetails = smallGrayFont
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.HashLabel)
        });
        footer.AddColumn(new ColumnSetup
        {
            Format = new Format(valueW, 6)
            {
                Position = new Kernel(63, ValueX),
                FontDetails = hashFont
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.HashValue)
        });

        // QR code — lado izquierdo; fila 51 → termina en 73, margen de 7mm al borde del footer
        footer.AddColumn(new ColumnSetup
        {
            Format = new Format((double)QrSize, (double)QrSize)
            {
                Position = new Kernel(51, QrX)
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.QrCode)
        });
    }
}
