namespace CentralBillingService.Reports.Builders;

internal static class InvoiceReportLayout
{
    public const decimal PageWidth = 210m;
    public const decimal Margin = 10m;
    public const decimal ContentWidth = 190m;

    // Alturas de secciones — Header+Body+Footer = 297 (A4 sin márgenes de sección)
    public const decimal HeaderHeight = 95m;   // reducido de 110 para eliminar hueco bajo cabecera de tabla
    public const decimal FooterHeight = 80m;   // ampliado de 65 para margen QR y sub-valores de moneda original
    public const decimal BodyHeight = 122m;    // 297 - 95 - 80
    public const decimal BodyRowHeight = 12m;
    public const decimal BodyRowHeightWithOrigin = 18m;

    // Posiciones X de columnas de la tabla (desde el borde izquierdo de la página)
    public const decimal ColDescX = 10m;
    public const decimal ColQtyX = 95m;
    public const decimal ColPriceX = 110m;
    public const decimal ColTaxX = 135m;
    public const decimal ColBaseX = 150m;
    public const decimal ColTotalX = 175m;

    // Anchos de columnas de la tabla
    public const decimal ColDescW = 85m;
    public const decimal ColQtyW = 15m;
    public const decimal ColPriceW = 25m;
    public const decimal ColTaxW = 15m;
    public const decimal ColBaseW = 25m;
    public const decimal ColTotalW = 25m;

    // Colores
    public const string TableHeaderColor = "#2C3E50";
    public const string InfoBoxColor = "#F4F6F7";
    public const string VerificationBgColor = "#F8F9FA";
    public const string SeparatorColor = "#CCCCCC";
    public const string WhiteText = "White";
    public const string GrayText = "#888888";
    public const string TamperWarningColor = "#C0392B";

    public static class Columns
    {
        // Header — alerta de integridad
        public const string TamperWarning = "TamperWarning";

        // Header — logo
        public const string CompanyLogo = "CompanyLogo";

        // Header — emisor
        public const string IssuerName = "IssuerName";
        public const string IssuerLegalName = "IssuerLegalName";
        public const string IssuerAddress = "IssuerAddress";
        public const string IssuerTaxId = "IssuerTaxId";

        // Header — metadatos factura
        public const string InvoiceTitle = "InvoiceTitle";
        public const string InvoiceNumberLabel = "InvoiceNumberLabel";
        public const string InvoiceNumberValue = "InvoiceNumberValue";
        public const string IssuedDateLabel = "IssuedDateLabel";
        public const string IssuedDateValue = "IssuedDateValue";

        // Header — receptor
        public const string InfoBox = "InfoBox";
        public const string RecipientLabel = "RecipientLabel";
        public const string RecipientName = "RecipientName";
        public const string RecipientAddress = "RecipientAddress";
        public const string RecipientTaxIdLabel = "RecipientTaxIdLabel";
        public const string RecipientTaxIdValue = "RecipientTaxIdValue";

        // Header — cabeceras tabla
        public const string TableHeaderBg = "TableHeaderBg";
        public const string DescriptionHeader = "DescriptionHeader";
        public const string QtyHeader = "QtyHeader";
        public const string UnitPriceHeader = "UnitPriceHeader";
        public const string TaxRateHeader = "TaxRateHeader";
        public const string TaxableBaseHeader = "TaxableBaseHeader";
        public const string TotalHeader = "TotalHeader";

        // Body — valores por línea (grupo "Detail")
        public const string DescriptionValue = "DescriptionValue";
        public const string QtyValue = "QtyValue";
        public const string UnitPriceValue = "UnitPriceValue";
        public const string UnitPriceOriginValue = "UnitPriceOriginValue";
        public const string TaxRateValue = "TaxRateValue";
        public const string TaxableBaseValue = "TaxableBaseValue";
        public const string TaxableBaseOriginValue = "TaxableBaseOriginValue";
        public const string TotalValue = "TotalValue";
        public const string TotalOriginValue = "TotalOriginValue";

        // Footer — totales
        public const string TotalSeparator = "TotalSeparator";
        public const string SubtotalLabel = "SubtotalLabel";
        public const string SubtotalValue = "SubtotalValue";
        public const string SubtotalOriginValue = "SubtotalOriginValue";
        public const string TaxLabel = "TaxLabel";
        public const string TaxValue = "TaxValue";
        public const string TaxOriginValue = "TaxOriginValue";
        public const string TotalSeparatorBottom = "TotalSeparatorBottom";
        public const string TotalLabel = "TotalLabel";
        public const string TotalFooterValue = "TotalFooterValue";
        public const string TotalOriginFooterValue = "TotalOriginFooterValue";

        // Footer — info pago y cambio
        public const string PaymentMethodLabel = "PaymentMethodLabel";
        public const string PaymentMethodValue = "PaymentMethodValue";
        public const string ExchangeRateRow = "ExchangeRateRow";

        // Footer — notas
        public const string NotesValue = "NotesValue";

        // Footer — verificación VeriFactu
        public const string VerificationSeparator = "VerificationSeparator";
        public const string VerificationTitle = "VerificationTitle";
        public const string BillingSourceLabel = "BillingSourceLabel";
        public const string BillingSourceValue = "BillingSourceValue";
        public const string HashLabel = "HashLabel";
        public const string HashValue = "HashValue";
        public const string QrCode = "QrCode";
    }
}
