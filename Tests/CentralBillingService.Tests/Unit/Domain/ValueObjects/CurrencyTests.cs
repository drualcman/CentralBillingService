namespace CentralBillingService.Tests.Unit.Domain.ValueObjects;

public class CurrencyTests
{
    [Theory]
    [InlineData("EUR")]
    [InlineData("USD")]
    [InlineData("PHP")]
    [InlineData("JPY")]
    [InlineData("GBP")]
    public void From_known_code_returns_currency(string code)
    {
        var currency = Currency.From(code);
        Assert.Equal(code, currency.Code);
    }

    [Theory]
    [InlineData("eur", "EUR")]
    [InlineData("usd", "USD")]
    [InlineData("  EUR  ", "EUR")]
    public void From_is_case_insensitive_and_trims(string input, string expectedCode)
    {
        var currency = Currency.From(input);
        Assert.Equal(expectedCode, currency.Code);
    }

    [Fact]
    public void From_unknown_code_throws_argument_exception()
    {
        Assert.Throws<ArgumentException>(() => Currency.From("XYZ"));
    }

    [Fact]
    public void From_null_throws_argument_null_exception()
    {
        Assert.Throws<ArgumentNullException>(() => Currency.From(null!));
    }

    [Fact]
    public void IsSupported_returns_true_for_known_codes()
    {
        Assert.True(Currency.IsSupported("EUR"));
        Assert.True(Currency.IsSupported("usd"));
        Assert.True(Currency.IsSupported("  JPY  "));
    }

    [Theory]
    [InlineData("XYZ")]
    [InlineData("")]
    [InlineData("   ")]
    public void IsSupported_returns_false_for_unknown_codes(string code)
    {
        Assert.False(Currency.IsSupported(code));
    }

    [Fact]
    public void IsSupported_returns_false_for_null()
    {
        Assert.False(Currency.IsSupported(null!));
    }

    [Fact]
    public void Equality_same_code_are_equal()
    {
        var eur1 = Currency.From("EUR");
        var eur2 = Currency.EUR;
        Assert.Equal(eur1, eur2);
        Assert.True(eur1 == eur2);
    }

    [Fact]
    public void Equality_different_codes_not_equal()
    {
        Assert.True(Currency.EUR != Currency.USD);
    }

    [Fact]
    public void Jpy_has_zero_decimal_places()
    {
        Assert.Equal(0, Currency.JPY.DecimalPlaces);
    }

    [Fact]
    public void Kwd_has_three_decimal_places()
    {
        Assert.Equal(3, Currency.KWD.DecimalPlaces);
    }

    [Fact]
    public void Eur_has_standard_properties()
    {
        Assert.Equal("EUR", Currency.EUR.Code);
        Assert.Equal(2, Currency.EUR.DecimalPlaces);
        Assert.Equal("€", Currency.EUR.Symbol);
    }

    [Fact]
    public void All_returns_read_only_dictionary_with_currencies()
    {
        Assert.NotEmpty(Currency.All);
        Assert.True(Currency.All.ContainsKey("EUR"));
    }
}
