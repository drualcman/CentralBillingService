namespace CentralBillingService.Domain.Models;

/// <summary>
/// Fields from a single invoice line that feed into the VeriFactu hash computation.
/// Changing any of these values after invoice creation will invalidate the stored hash.
/// </summary>
public sealed class InvoiceLineHashContent
{
    public string LineNumber { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Quantity { get; init; } = string.Empty;
    public string UnitPriceEur { get; init; } = string.Empty;
    public string TaxRatePercentage { get; init; } = string.Empty;
    public string TotalEur { get; init; } = string.Empty;
}
