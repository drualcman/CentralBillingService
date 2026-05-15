namespace CentralBillingService.Application.DTOs;

public sealed class ExchangeRateResult
{
    public required string FromCurrency { get; init; }
    public required string ToCurrency { get; init; }
    public required decimal Rate { get; init; }
    public required DateTimeOffset FetchedAt { get; init; }
    public required bool IsIdentity { get; init; }
}
