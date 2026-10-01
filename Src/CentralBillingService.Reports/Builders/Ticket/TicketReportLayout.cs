namespace CentralBillingService.Reports.Builders.Ticket;

/// <summary>Geometry (mm) and column names of the 80 mm thermal-roll ticket.</summary>
internal static class TicketReportLayout
{
    public const decimal PageWidth = 80m;
    public const decimal Margin = 4m;
    public const decimal ContentWidth = 72m;
    public const decimal HeaderHeight = 89m;
    public const decimal BodyRowHeight = 10m;
    public const decimal FooterHeight = 64m;
    public const decimal FiscalQrSize = 30m;   // AEAT: between 30x30 and 40x40 mm
    public const string SeparatorColor = "#999999";
    public const string GrayText = "#666666";

    /// <summary>The roll is cut after the content: the page is exactly as long as the ticket.</summary>
    public static decimal BodyHeight(int lineCount) => Math.Max(lineCount, 1) * BodyRowHeight + 2m;

    public static class Columns
    {
        public const string TamperWarning = "TicketTamperWarning";
        public const string Qr = "TicketQr";
        public const string QrLegend = "TicketQrLegend";
        public const string IssuerName = "TicketIssuerName";
        public const string IssuerLegalName = "TicketIssuerLegalName";
        public const string IssuerTaxId = "TicketIssuerTaxId";
        public const string IssuerAddress = "TicketIssuerAddress";
        public const string HeaderSeparator = "TicketHeaderSeparator";
        public const string Title = "TicketTitle";
        public const string Number = "TicketNumber";
        public const string Date = "TicketDate";
        public const string RecipientName = "TicketRecipientName";
        public const string RecipientTaxId = "TicketRecipientTaxId";
        public const string LinesSeparator = "TicketLinesSeparator";

        public const string LineDescription = "TicketLineDescription";
        public const string LineTotal = "TicketLineTotal";
        public const string LineDetail = "TicketLineDetail";

        public const string TotalsSeparator = "TicketTotalsSeparator";
        public const string BaseLabel = "TicketBaseLabel";
        public const string BaseValue = "TicketBaseValue";
        public const string TaxLabel = "TicketTaxLabel";
        public const string TaxValue = "TicketTaxValue";
        public const string TotalLabel = "TicketTotalLabel";
        public const string TotalValue = "TicketTotalValue";
        public const string PaymentSeparator = "TicketPaymentSeparator";
        public const string Payment = "TicketPayment";
        public const string ExchangeRate = "TicketExchangeRate";
        public const string Notes = "TicketNotes";
        public const string Origin = "TicketOrigin";
        public const string Hash = "TicketHash";
        public const string RectificationNotice = "TicketRectificationNotice";
    }
}
