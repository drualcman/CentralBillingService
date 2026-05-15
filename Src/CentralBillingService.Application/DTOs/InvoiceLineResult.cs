namespace CentralBillingService.Application.DTOs;

public sealed class InvoiceLineResult
{
    public required int LineNumber { get; init; }
    public required string Description { get; init; }
    public required int Quantity { get; init; }
    public required MoneyResult UnitPriceEur { get; init; }
    public required MoneyResult UnitPriceOrigin { get; init; }
    public required MoneyResult TaxableBaseEur { get; init; }
    public required MoneyResult TaxAmountEur { get; init; }
    public required MoneyResult TotalEur { get; init; }
    public required MoneyResult TotalOrigin { get; init; }
    public required int TaxRatePercentage { get; init; }
    public required bool HasCurrencyConversion { get; init; }
}
