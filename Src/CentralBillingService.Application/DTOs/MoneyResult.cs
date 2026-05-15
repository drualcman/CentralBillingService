namespace CentralBillingService.Application.DTOs;

public sealed class MoneyResult
{
    public required decimal Amount { get; init; }
    public required string CurrencyCode { get; init; }
    public required string Formatted { get; init; }
}
