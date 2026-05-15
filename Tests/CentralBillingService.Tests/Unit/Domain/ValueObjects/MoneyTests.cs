namespace CentralBillingService.Tests.Unit.Domain.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Of_creates_money_with_correct_amount_and_currency()
    {
        var money = Money.Of(99.50m, Currency.EUR);
        Assert.Equal(99.50m, money.Amount);
        Assert.Equal(Currency.EUR, money.Currency);
    }

    [Fact]
    public void Of_rounds_to_currency_decimal_places()
    {
        var money = Money.Of(99.999m, Currency.EUR);
        Assert.Equal(100.00m, money.Amount);
    }

    [Fact]
    public void Of_from_string_currency_code_works()
    {
        var money = Money.Of(50m, "USD");
        Assert.Equal(Currency.USD, money.Currency);
    }

    [Fact]
    public void Zero_creates_zero_amount()
    {
        var zero = Money.Zero(Currency.EUR);
        Assert.Equal(0m, zero.Amount);
        Assert.True(zero.IsZero);
    }

    [Fact]
    public void Add_same_currency_returns_sum()
    {
        var a = Money.Of(50m, Currency.EUR);
        var b = Money.Of(30.50m, Currency.EUR);
        Assert.Equal(Money.Of(80.50m, Currency.EUR), a.Add(b));
    }

    [Fact]
    public void Add_different_currencies_throws()
    {
        var eur = Money.Of(50m, Currency.EUR);
        var usd = Money.Of(30m, Currency.USD);
        Assert.Throws<DomainException>(() => eur.Add(usd));
    }

    [Fact]
    public void Subtract_same_currency_returns_difference()
    {
        var a = Money.Of(100m, Currency.EUR);
        var b = Money.Of(30m, Currency.EUR);
        Assert.Equal(Money.Of(70m, Currency.EUR), a.Subtract(b));
    }

    [Fact]
    public void Subtract_different_currencies_throws()
    {
        var eur = Money.Of(50m, Currency.EUR);
        var usd = Money.Of(30m, Currency.USD);
        Assert.Throws<DomainException>(() => eur.Subtract(usd));
    }

    [Fact]
    public void Multiply_by_int_returns_scaled_amount()
    {
        var price = Money.Of(25m, Currency.EUR);
        Assert.Equal(Money.Of(75m, Currency.EUR), price.Multiply(3));
    }

    [Fact]
    public void Multiply_by_decimal_returns_scaled_amount()
    {
        var base_ = Money.Of(100m, Currency.EUR);
        Assert.Equal(Money.Of(21m, Currency.EUR), base_.Multiply(0.21m));
    }

    [Fact]
    public void IsZero_false_when_amount_is_positive()
    {
        Assert.False(Money.Of(0.01m, Currency.EUR).IsZero);
    }

    [Fact]
    public void IsGreaterThan_returns_true_when_larger()
    {
        var big = Money.Of(100m, Currency.EUR);
        var small = Money.Of(50m, Currency.EUR);
        Assert.True(big.IsGreaterThan(small));
        Assert.False(small.IsGreaterThan(big));
    }

    [Fact]
    public void Equality_same_amount_and_currency_are_equal()
    {
        var a = Money.Of(100m, Currency.EUR);
        var b = Money.Of(100m, Currency.EUR);
        Assert.Equal(a, b);
    }

    [Fact]
    public void Equality_different_currency_not_equal()
    {
        Assert.NotEqual(Money.Of(100m, Currency.EUR), Money.Of(100m, Currency.USD));
    }

    [Fact]
    public void Jpy_rounds_to_zero_decimal_places()
    {
        var jpy = Money.Of(1234.7m, Currency.JPY);
        Assert.Equal(1235m, jpy.Amount);
    }

    [Fact]
    public void Kwd_rounds_to_three_decimal_places()
    {
        var kwd = Money.Of(1.2345m, Currency.KWD);
        Assert.Equal(1.235m, kwd.Amount);
    }

    [Fact]
    public void ToString_includes_amount_and_code()
    {
        var money = Money.Of(99.50m, Currency.EUR);
        Assert.Contains("99.50", money.ToString());
        Assert.Contains("EUR", money.ToString());
    }
}
