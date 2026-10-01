namespace CentralBillingService.Reports.Builders;

/// <summary>Column titles of the lines table; a null title leaves that column out of the header.</summary>
internal sealed record InvoiceTableHeaders(
    string Description, string? Quantity, string? UnitPrice, string? TaxRate, string? TaxableBase, string Total)
{
    public static readonly InvoiceTableHeaders Products = new("Descripción", "Cant.", "P.U. (€)", "IGIC %", "Base €", "Total €");

    public static readonly InvoiceTableHeaders Subscriptions = new("Suscripción", null, null, null, null, "Importe €");
}
