using DigitalDoor.Reporting.Entities.Models;
using DigitalDoor.Reporting.Entities.ValueObjects;

namespace CentralBillingService.Reports.Builders;

internal static class FooterSectionBuilder
{
    private const decimal RightLabelX = 120m;
    private const decimal RightValueX = 155m;
    private const decimal RightColW = 35m;
    private const decimal VerifLabelW = 40m;
    private const decimal QrSize = 22m;
    private const decimal QrStartY = 26m;                                                 // QR arranca en la línea del TOTAL
    private const decimal SideX = InvoiceReportLayout.Margin + QrSize + 3m;              // 35mm: info de pago a la derecha del QR

    public static void Build(Section footer)
    {
        BuildTopSeparator(footer);
        BuildTotals(footer);
        BuildQrCode(footer);
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

    // QR en el lado izquierdo de la zona del TOTAL, encima del bloque VERIFICACIÓN
    private static void BuildQrCode(Section footer)
    {
        footer.AddColumn(new ColumnSetup
        {
            Format = new Format((double)QrSize, (double)QrSize)
            {
                Position = new Kernel(QrStartY, InvoiceReportLayout.Margin)
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.QrCode)
        });
    }

    private static void BuildPaymentInfo(Section footer)
    {
        double leftW = (double)(RightLabelX - InvoiceReportLayout.Margin - 5m); // 105mm
        footer.AddColumn(new ColumnSetup
        {
            Format = new Format(40, 5) { Position = new Kernel(5, InvoiceReportLayout.Margin) },
            DataColumn = new Item(InvoiceReportLayout.Columns.PaymentMethodLabel)
        });
        footer.AddColumn(new ColumnSetup
        {
            Format = new Format(leftW, 6) { Position = new Kernel(10, InvoiceReportLayout.Margin) },
            DataColumn = new Item(InvoiceReportLayout.Columns.PaymentMethodValue)
        });
    }

    private static void BuildExchangeRateRow(Section footer)
    {
        double leftW = (double)(RightLabelX - InvoiceReportLayout.Margin - 5m); // 105mm
        footer.AddColumn(new ColumnSetup
        {
            Format = new Format(leftW, 5)
            {
                Position = new Kernel(16, InvoiceReportLayout.Margin),
                FontDetails = new Font(new Shade(8, InvoiceReportLayout.GrayText))
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.ExchangeRateRow)
        });
    }

    private static void BuildNotesRow(Section footer)
    {
        footer.AddColumn(new ColumnSetup
        {
            Format = new Format(85, 5)
            {
                Position = new Kernel(44, SideX),
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
        decimal valueX = InvoiceReportLayout.Margin + VerifLabelW;               // 50mm
        double valueW = (double)(InvoiceReportLayout.ContentWidth - VerifLabelW); // 150mm

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
            Format = new Format((double)InvoiceReportLayout.ContentWidth, 5)
            {
                Position = new Kernel(51, InvoiceReportLayout.Margin),
                FontDetails = smallGrayFont
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.VerificationTitle)
        });

        footer.AddColumn(new ColumnSetup
        {
            Format = new Format((double)VerifLabelW, 5)
            {
                Position = new Kernel(57, InvoiceReportLayout.Margin),
                FontDetails = smallGrayFont
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.BillingSourceLabel)
        });
        footer.AddColumn(new ColumnSetup
        {
            Format = new Format(valueW, 5)
            {
                Position = new Kernel(57, valueX),
                FontDetails = smallBoldFont
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.BillingSourceValue)
        });

        footer.AddColumn(new ColumnSetup
        {
            Format = new Format((double)VerifLabelW, 5)
            {
                Position = new Kernel(63, InvoiceReportLayout.Margin),
                FontDetails = smallGrayFont
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.HashLabel)
        });
        footer.AddColumn(new ColumnSetup
        {
            Format = new Format(valueW, 6)
            {
                Position = new Kernel(63, valueX),
                FontDetails = hashFont
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.HashValue)
        });
    }
}
