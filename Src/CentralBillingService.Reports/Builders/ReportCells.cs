namespace CentralBillingService.Reports.Builders;

/// <summary>Creates report cells and formats amounts the same way in every printed model.</summary>
internal static class ReportCells
{
    public const string DetailGroup = "Detail";

    private static readonly CultureInfo EsEs = new("es-ES");

    public static string Amount(decimal amount) => amount.ToString("N2", EsEs);

    public static string OriginAmount(decimal amount, string currencyCode) => $"{amount.ToString("N2", EsEs)} {currencyCode}";

    public static ColumnData Header(string column, object value) => Cell(SectionType.Header, column, value);

    public static ColumnData Footer(string column, object value) => Cell(SectionType.Footer, column, value);

    public static ColumnData Body(int row, string column, object value) =>
        new() { Section = SectionType.Body, Column = new Item(DetailGroup, column), Value = value, Row = row };

    private static ColumnData Cell(SectionType section, string column, object value) =>
        new() { Section = section, Column = new Item(column), Value = value };
}
