namespace CentralBillingService.Application.DTOs;

public sealed class InvoiceLineDto
{
    public required string Description { get; init; }
    public required int Quantity { get; init; }

    /// <summary>Unit price in the origin currency.</summary>
    public required decimal UnitPrice { get; init; }

    /// <summary>VAT percentage: 0, 4, 10 or 21.</summary>
    public required int TaxRatePercentage { get; init; }
}
