namespace CentralBillingService.Tests.Helpers;

public sealed class FakeExchangeRateProvider : IExchangeRateProvider
{
    private static readonly Dictionary<string, decimal> DefaultRates = new()
    {
        ["USD"] = 0.92m,
        ["PHP"] = 0.016m,
        ["AUD"] = 0.60m,
        ["GBP"] = 1.17m,
        ["MXN"] = 0.047m,
    };

    private readonly Dictionary<string, decimal> _rates;

    public FakeExchangeRateProvider(Dictionary<string, decimal>? rates = null)
        => _rates = rates ?? DefaultRates;

    public Task<ExchangeRate> GetRateAsync(Currency from, Currency to, CancellationToken ct = default)
    {
        if (!_rates.TryGetValue(from.Code, out var rate))
            throw new ExchangeRateUnavailableException(from, to);

        return Task.FromResult(ExchangeRate.Create(from, to, rate, DateTimeOffset.UtcNow));
    }

    public bool Supports(Currency from, Currency to) => _rates.ContainsKey(from.Code);
}
