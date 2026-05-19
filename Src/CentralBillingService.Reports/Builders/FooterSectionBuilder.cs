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
                Background = InvoiceReportLayout.TableHeaderColor
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.TotalSeparator)
        });
    }

    private static void BuildTotals(Section footer)
    {
        footer.AddColumn(new ColumnSetup
        {
            Format = new Format((double)RightColW, 6)
            {
                Position = new Kernel(6, RightLabelX),
                TextAlignment = TextAlignment.Right
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.SubtotalLabel)
        });
        footer.AddColumn(new ColumnSetup
        {
            Format = new Format((double)RightColW, 6)
            {
                Position = new Kernel(6, RightValueX),
                TextAlignment = TextAlignment.Right
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.SubtotalValue)
        });

        footer.AddColumn(new ColumnSetup
        {
            Format = new Format((double)RightColW, 6)
            {
                Position = new Kernel(13, RightLabelX),
                TextAlignment = TextAlignment.Right
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.TaxLabel)
        });
        footer.AddColumn(new ColumnSetup
        {
            Format = new Format((double)RightColW, 6)
            {
                Position = new Kernel(13, RightValueX),
                TextAlignment = TextAlignment.Right
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.TaxValue)
        });

        footer.AddColumn(new ColumnSetup
        {
            Format = new Format((double)InvoiceReportLayout.ContentWidth, 0.5)
            {
                Position = new Kernel(20, InvoiceReportLayout.Margin),
                Background = InvoiceReportLayout.SeparatorColor
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.TotalSeparatorBottom)
        });

        var totalFont = new Font(new Shade(16), new FontStyle(700));
        footer.AddColumn(new ColumnSetup
        {
            Format = new Format((double)RightColW, 10)
            {
                Position = new Kernel(22, RightLabelX),
                TextAlignment = TextAlignment.Right,
                FontDetails = totalFont
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.TotalLabel)
        });
        footer.AddColumn(new ColumnSetup
        {
            Format = new Format((double)RightColW, 10)
            {
                Position = new Kernel(22, RightValueX),
                TextAlignment = TextAlignment.Right,
                FontDetails = totalFont
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.TotalFooterValue)
        });
    }

    private static void BuildPaymentInfo(Section footer)
    {
        footer.AddColumn(new ColumnSetup
        {
            Format = new Format(50, 5) { Position = new Kernel(6, InvoiceReportLayout.Margin) },
            DataColumn = new Item(InvoiceReportLayout.Columns.PaymentMethodLabel)
        });
        footer.AddColumn(new ColumnSetup
        {
            Format = new Format((double)LeftColW, 5) { Position = new Kernel(12, InvoiceReportLayout.Margin) },
            DataColumn = new Item(InvoiceReportLayout.Columns.PaymentMethodValue)
        });
    }

    private static void BuildExchangeRateRow(Section footer)
    {
        footer.AddColumn(new ColumnSetup
        {
            Format = new Format((double)InvoiceReportLayout.ContentWidth, 5)
            {
                Position = new Kernel(18, InvoiceReportLayout.Margin),
                FontDetails = new Font(new Shade(8, InvoiceReportLayout.GrayText))
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.ExchangeRateRow)
        });
    }

    private static void BuildNotesRow(Section footer)
    {
        footer.AddColumn(new ColumnSetup
        {
            Format = new Format((double)InvoiceReportLayout.ContentWidth, 7)
            {
                Position = new Kernel(33, InvoiceReportLayout.Margin),
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
        decimal valueX = InvoiceReportLayout.Margin + VerifLabelW;
        // Leave 25mm on the right for the QR code (3mm gap before it)
        const decimal QrSize = 22m;
        const decimal QrX = 168m;   // 190 (content right edge) - 22 = 168
        double valueW = (double)(QrX - valueX - 3m); // 3mm gap before QR

        footer.AddColumn(new ColumnSetup
        {
            Format = new Format((double)InvoiceReportLayout.ContentWidth, 0.5)
            {
                Position = new Kernel(41, InvoiceReportLayout.Margin),
                Background = InvoiceReportLayout.SeparatorColor
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.VerificationSeparator)
        });

        footer.AddColumn(new ColumnSetup
        {
            Format = new Format((double)InvoiceReportLayout.ContentWidth, 5)
            {
                Position = new Kernel(43, InvoiceReportLayout.Margin),
                Background = InvoiceReportLayout.VerificationBgColor,
                FontDetails = smallGrayFont
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.VerificationTitle)
        });

        footer.AddColumn(new ColumnSetup
        {
            Format = new Format((double)VerifLabelW, 5)
            {
                Position = new Kernel(49, InvoiceReportLayout.Margin),
                FontDetails = smallGrayFont
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.BillingSourceLabel)
        });
        footer.AddColumn(new ColumnSetup
        {
            Format = new Format(valueW, 5)
            {
                Position = new Kernel(49, valueX),
                FontDetails = smallBoldFont
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.BillingSourceValue)
        });

        footer.AddColumn(new ColumnSetup
        {
            Format = new Format((double)VerifLabelW, 5)
            {
                Position = new Kernel(55, InvoiceReportLayout.Margin),
                FontDetails = smallGrayFont
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.HashLabel)
        });
        footer.AddColumn(new ColumnSetup
        {
            Format = new Format(valueW, 6)
            {
                Position = new Kernel(55, valueX),
                FontDetails = hashFont
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.HashValue)
        });

        // QR code — right side of verification block (only rendered when ColumnData is provided)
        footer.AddColumn(new ColumnSetup
        {
            Format = new Format((double)QrSize, (double)QrSize)
            {
                Position = new Kernel(43, QrX)
            },
            DataColumn = new Item(InvoiceReportLayout.Columns.QrCode)
        });
    }
}
