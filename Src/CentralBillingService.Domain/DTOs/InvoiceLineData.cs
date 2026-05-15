namespace CentralBillingService.Domain.DTOs;

public sealed class InvoiceLineData
{
    public required string Description { get; init; }
    public required int Quantity { get; init; }

    /// <summary>
    /// Precio unitario en la divisa origen del request (OriginCurrencyCode).
    /// El servicio convierte a EUR usando el tipo de cambio del momento.
    /// </summary>
    public required decimal UnitPrice { get; init; }

    /// <summary>Porcentaje de IVA aplicable: 0, 4, 10 o 21</summary>
    public required int TaxRatePercentage { get; init; }
}
