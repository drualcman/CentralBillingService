namespace CentralBillingService.Tests.Unit.Domain.ValueObjects;

public class TaxRateTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(10)]
    [InlineData(21)]
    public void Of_returns_correct_percentage_for_spanish_rates(int percentage)
    {
        var rate = TaxRate.Of(percentage);
        Assert.Equal(percentage, rate.Percentage);
    }

    [Fact]
    public void Of_returns_same_instance_for_predefined_rates()
    {
        Assert.Same(TaxRate.Zero, TaxRate.Of(0));
        Assert.Same(TaxRate.SuperReduced, TaxRate.Of(4));
        Assert.Same(TaxRate.Reduced, TaxRate.Of(10));
        Assert.Same(TaxRate.General, TaxRate.Of(21));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(-100)]
    public void Of_out_of_range_throws(int percentage)
    {
        Assert.Throws<DomainException>(() => TaxRate.Of(percentage));
    }

    [Fact]
    public void Factor_general_is_021()
    {
        Assert.Equal(0.21m, TaxRate.General.Factor);
    }

    [Fact]
    public void Factor_reduced_is_010()
    {
        Assert.Equal(0.10m, TaxRate.Reduced.Factor);
    }

    [Fact]
    public void Factor_zero_is_0()
    {
        Assert.Equal(0m, TaxRate.Zero.Factor);
    }

    [Fact]
    public void CalculateTaxOn_general_rate_computes_21_percent()
    {
        var taxableBase = Money.Of(100m, Currency.EUR);
        var tax = TaxRate.General.CalculateTaxOn(taxableBase);
        Assert.Equal(Money.Of(21m, Currency.EUR), tax);
    }

    [Fact]
    public void CalculateTaxOn_zero_rate_returns_zero()
    {
        var taxableBase = Money.Of(100m, Currency.EUR);
        Assert.Equal(Money.Zero(Currency.EUR), TaxRate.Zero.CalculateTaxOn(taxableBase));
    }

    [Fact]
    public void ApplyTo_returns_base_plus_tax()
    {
        var base_ = Money.Of(100m, Currency.EUR);
        var total = TaxRate.General.ApplyTo(base_);
        Assert.Equal(Money.Of(121m, Currency.EUR), total);
    }

    [Fact]
    public void Custom_percentage_creates_new_instance()
    {
        var custom = TaxRate.Of(8);
        Assert.Equal(8, custom.Percentage);
        Assert.Equal(0.08m, custom.Factor);
    }

    [Fact]
    public void Equality_same_percentage_are_equal()
    {
        Assert.Equal(TaxRate.Of(21), TaxRate.General);
    }

    [Fact]
    public void ToString_includes_percentage_symbol()
    {
        Assert.Equal("21%", TaxRate.General.ToString());
    }
}
