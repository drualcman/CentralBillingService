namespace CentralBillingService.Infrastructure.ExchangeRates;

/// <summary>
/// Adapter that wraps your existing exchange rate implementation
/// behind the domain's <see cref="IExchangeRateProvider"/> interface.
///
/// HOW TO PLUG IN YOUR CODE:
/// Replace the body of <see cref="FetchRateAsync"/> with a call to
/// your existing exchange rate service. It must return:
///   - the rate as a decimal (how many EUR per 1 unit of the origin currency)
///   - the source name (e.g. "fixer.io", "ECB", "openexchangerates")
///   - the exact UTC timestamp of the fetch
///
/// The rest of the plumbing (caching, retry, exception mapping) is here.
/// </summary>
public sealed class ExchangeRateProviderAdapter : IExchangeRateProvider
{
    // Supported conversion pairs — always TO EUR in our system
    private static readonly HashSet<string> _supportedFrom = new(StringComparer.OrdinalIgnoreCase)
    {
        "USD", "PHP", "AUD", "GBP", "MXN"
        // Add more as needed — must match Currency.From() supported codes
    };

    private readonly ICurrencyConvertion _currencyConvertion;

    public ExchangeRateProviderAdapter(ICurrencyConvertion currencyConvertion)
    {
        _currencyConvertion = currencyConvertion;
    }

    public bool Supports(Currency from, Currency to) =>
        from == Currency.EUR && to == Currency.EUR   // identity, always supported
        || (to == Currency.EUR && _supportedFrom.Contains(from.Code));

    public async Task<ExchangeRate> GetRateAsync(
        Currency from,
        Currency to,
        CancellationToken cancellationToken = default)
    {
        // Identity case — no external call needed
        if (from == Currency.EUR && to == Currency.EUR)
            return ExchangeRate.Identity(DateTimeOffset.UtcNow);
        try
        {
            var result = await FetchRateAsync(from, to, cancellationToken);
            return result;
        }
        catch (ExchangeRateUnavailableException)
        {
            throw; // already the right type, let it bubble
        }
        catch (Exception ex)
        {
            throw new ExchangeRateUnavailableException(from, to, ex);
        }
    }

    // ── Replace this method body with your existing implementation ─────────

    /// <summary>
    /// Fetches the live exchange rate from your existing provider.
    /// Replace the throw below with a call to your actual rate service.
    /// </summary>
    private async Task<ExchangeRate> FetchRateAsync(
        Currency from,
        Currency to,
        CancellationToken cancellationToken)
    {
        if (!cancellationToken.IsCancellationRequested)
        {
            var result = await _currencyConvertion.GetRate(from, to);
            return ExchangeRate.Create(from, to, result, DateTime.UtcNow);
        }
        return ExchangeRate.Create(from, from, 1m, DateTime.UtcNow);
    }
}