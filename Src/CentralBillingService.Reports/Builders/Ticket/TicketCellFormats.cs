namespace CentralBillingService.Reports.Builders.Ticket;

/// <summary>Builds the positioned cells of the ticket: text, right-aligned amounts and separators.</summary>
internal static class TicketCellFormats
{
    public static ColumnSetup Text(string column, decimal y, decimal height, double fontSize,
        bool bold = false, string color = "black", TextAlignment alignment = TextAlignment.Left,
        decimal x = TicketReportLayout.Margin, decimal width = TicketReportLayout.ContentWidth, string group = "") =>
        new ColumnSetup
        {
            Format = new Format((double)width, (double)height)
            {
                Position = new Kernel(y, x),
                TextAlignment = alignment,
                FontDetails = bold
                    ? new Font(new Shade(fontSize, color), new FontStyle(700))
                    : new Font(new Shade(fontSize, color))
            },
            DataColumn = string.IsNullOrEmpty(group) ? new Item(column) : new Item(group, column)
        };

    public static ColumnSetup Separator(string column, decimal y) =>
        new ColumnSetup
        {
            Format = new Format((double)TicketReportLayout.ContentWidth, 0.3)
            {
                Position = new Kernel(y, TicketReportLayout.Margin),
                Background = TicketReportLayout.SeparatorColor
            },
            DataColumn = new Item(column)
        };

    public static ColumnSetup Image(string column, decimal y, decimal x, decimal size) =>
        new ColumnSetup
        {
            Format = new Format((double)size, (double)size) { Position = new Kernel(y, x) },
            DataColumn = new Item(column)
        };
}
