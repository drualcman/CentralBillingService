namespace CentralBillingService.Tests.Unit.Domain.ValueObjects;

public class ExchangeRateTests
{
    [Fact]
    public void Create_stores_all_fields()
    {
        var fetchedAt = DateTimeOffset.UtcNow;
        var rate = ExchangeRate.Create(Currency.USD, Currency.EUR, 0.92m, fetchedAt);

        Assert.Equal(Currency.USD, rate.From);
        Assert.Equal(Currency.EUR, rate.To);
        Assert.Equal(0.92m, rate.Rate);
        Assert.Equal(fetchedAt, rate.FetchedAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.5)]
    [InlineData(-1)]
    public void Create_non_positive_rate_throws(double rate)
    {
        Assert.Throws<DomainException>(() =>
            ExchangeRate.Create(Currency.USD, Currency.EUR, (decimal)rate, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Identity_returns_eur_to_eur_with_rate_1()
    {
        var rate = ExchangeRate.Identity(DateTimeOffset.UtcNow);

        Assert.Equal(Currency.EUR, rate.From);
        Assert.Equal(Currency.EUR, rate.To);
        Assert.Equal(1m, rate.Rate);
        Assert.True(rate.IsIdentity);
    }

    [Fact]
    public void IsIdentity_false_for_real_exchange_rate()
    {
        var rate = ExchangeRate.Create(Currency.USD, Currency.EUR, 0.92m, DateTimeOffset.UtcNow);
        Assert.False(rate.IsIdentity);
    }

    [Fact]
    public void Apply_converts_origin_amount_to_destination_currency()
    {
        var rate = ExchangeRate.Create(Currency.USD, Currency.EUR, 0.92m, DateTimeOffset.UtcNow);
        var usd = Money.Of(100m, Currency.USD);

        var result = rate.Apply(usd);

        Assert.Equal(Currency.EUR, result.Currency);
        Assert.Equal(92m, result.Amount);
    }

    [Fact]
    public void Apply_with_fractional_rate_rounds_to_currency_decimals()
    {
        var rate = ExchangeRate.Create(Currency.PHP, Currency.EUR, 0.016m, DateTimeOffset.UtcNow);
        var php = Money.Of(100m, Currency.PHP);

        var result = rate.Apply(php);

        Assert.Equal(Currency.EUR, result.Currency);
        Assert.Equal(1.60m, result.Amount);
    }

    [Fact]
    public void Apply_wrong_currency_throws()
    {
        var rate = ExchangeRate.Create(Currency.USD, Currency.EUR, 0.92m, DateTimeOffset.UtcNow);
        var eur = Money.Of(100m, Currency.EUR);

        Assert.Throws<DomainException>(() => rate.Apply(eur));
    }

    [Fact]
    public void Apply_identity_rate_returns_same_eur_amount()
    {
        var rate = ExchangeRate.Identity(DateTimeOffset.UtcNow);
        var eur = Money.Of(150m, Currency.EUR);

        var result = rate.Apply(eur);

        Assert.Equal(Money.Of(150m, Currency.EUR), result);
    }
}
