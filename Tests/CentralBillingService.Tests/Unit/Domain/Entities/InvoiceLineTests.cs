namespace CentralBillingService.Tests.Unit.Domain.Entities;

public class InvoiceLineTests
{
    [Fact]
    public void CreateInEur_computes_totals_correctly()
    {
        var unitPrice = Money.Of(100m, Currency.EUR);
        var line = InvoiceLine.CreateInEur(1, "Servicio", 2, unitPrice, TaxRate.General);

        Assert.Equal(Money.Of(200m, Currency.EUR), line.TaxableBaseEur);   // 100 × 2
        Assert.Equal(Money.Of(42m, Currency.EUR), line.TaxAmountEur);      // 200 × 21%
        Assert.Equal(Money.Of(242m, Currency.EUR), line.TotalEur);         // 200 + 42
        Assert.Equal(Money.Of(200m, Currency.EUR), line.TotalOrigin);      // same as EUR
        Assert.False(line.HasCurrencyConversion);
    }

    [Fact]
    public void CreateInEur_with_zero_tax_no_tax_amount()
    {
        var unitPrice = Money.Of(100m, Currency.EUR);
        var line = InvoiceLine.CreateInEur(1, "Servicio", 1, unitPrice, TaxRate.Zero);

        Assert.Equal(Money.Zero(Currency.EUR), line.TaxAmountEur);
        Assert.Equal(Money.Of(100m, Currency.EUR), line.TotalEur);
    }

    [Fact]
    public void CreateInEur_single_unit_calculates_correctly()
    {
        var unitPrice = Money.Of(50m, Currency.EUR);
        var line = InvoiceLine.CreateInEur(1, "Servicio", 1, unitPrice, TaxRate.Reduced);

        Assert.Equal(Money.Of(50m, Currency.EUR), line.TaxableBaseEur);
        Assert.Equal(Money.Of(5m, Currency.EUR), line.TaxAmountEur);       // 50 × 10%
        Assert.Equal(Money.Of(55m, Currency.EUR), line.TotalEur);
    }

    [Fact]
    public void CreateWithConversion_stores_both_currency_amounts()
    {
        var unitOrigin = Money.Of(100m, Currency.USD);
        var unitEur = Money.Of(92m, Currency.EUR);
        var line = InvoiceLine.CreateWithConversion(1, "Servicio", 1, unitOrigin, unitEur, TaxRate.General);

        Assert.Equal(unitEur, line.UnitPriceEur);
        Assert.Equal(unitOrigin, line.UnitPriceOrigin);
        Assert.Equal(Money.Of(100m, Currency.USD), line.TotalOrigin);
        Assert.True(line.HasCurrencyConversion);
    }

    [Fact]
    public void CreateWithConversion_computes_eur_totals_from_eur_price()
    {
        var unitOrigin = Money.Of(200m, Currency.PHP);
        var unitEur = Money.Of(3.20m, Currency.EUR);
        var line = InvoiceLine.CreateWithConversion(1, "Foto", 5, unitOrigin, unitEur, TaxRate.General);

        Assert.Equal(Money.Of(16m, Currency.EUR), line.TaxableBaseEur);    // 3.20 × 5
        Assert.Equal(Money.Of(3.36m, Currency.EUR), line.TaxAmountEur);    // 16 × 21%
        Assert.Equal(Money.Of(19.36m, Currency.EUR), line.TotalEur);
        Assert.Equal(Money.Of(1000m, Currency.PHP), line.TotalOrigin);     // 200 × 5
    }

    [Fact]
    public void CreateWithConversion_eur_as_origin_throws()
    {
        var eur = Money.Of(100m, Currency.EUR);
        Assert.Throws<DomainException>(() =>
            InvoiceLine.CreateWithConversion(1, "Servicio", 1, eur, eur, TaxRate.General));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_invalid_line_number_throws(int lineNumber)
    {
        var price = Money.Of(100m, Currency.EUR);
        Assert.Throws<DomainException>(() =>
            InvoiceLine.CreateInEur(lineNumber, "Servicio", 1, price, TaxRate.General));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_empty_description_throws(string description)
    {
        var price = Money.Of(100m, Currency.EUR);
        Assert.Throws<DomainException>(() =>
            InvoiceLine.CreateInEur(1, description, 1, price, TaxRate.General));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_zero_or_negative_quantity_allowed(int quantity)
    {
        var price = Money.Of(100m, Currency.EUR);
        var line = InvoiceLine.CreateInEur(1, "Servicio", quantity, price, TaxRate.General);
        Assert.Equal(quantity, line.Quantity);
    }

    [Fact]
    public void Create_zero_unit_price_allowed()
    {
        var line = InvoiceLine.CreateInEur(1, "Regalo incluido", 1, Money.Zero(Currency.EUR), TaxRate.General);
        Assert.Equal(Money.Zero(Currency.EUR), line.TotalEur);
    }
}
