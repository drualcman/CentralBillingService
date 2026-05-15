namespace CentralBillingService.Tests.Unit.Domain.ValueObjects;

public class InvoiceNumberTests
{
    [Fact]
    public void Create_builds_correct_formatted_value()
    {
        var number = InvoiceNumber.Create("FOTO", 2026, 1);
        Assert.Equal("FOTO2026-0001", number.Value);
    }

    [Fact]
    public void Create_normalizes_serie_to_uppercase()
    {
        var number = InvoiceNumber.Create("foto", 2026, 5);
        Assert.Equal("FOTO", number.Serie);
    }

    [Fact]
    public void Create_pads_number_to_four_digits()
    {
        var number = InvoiceNumber.Create("F", 2026, 3);
        Assert.Equal("F2026-0003", number.Value);
    }

    [Fact]
    public void Create_large_number_shows_full_digits()
    {
        var number = InvoiceNumber.Create("F", 2026, 1234);
        Assert.Equal("F2026-1234", number.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_empty_serie_throws(string serie)
    {
        Assert.Throws<DomainException>(() => InvoiceNumber.Create(serie, 2026, 1));
    }

    [Fact]
    public void Create_year_before_2026_throws()
    {
        Assert.Throws<DomainException>(() => InvoiceNumber.Create("F", 2025, 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_non_positive_number_throws(int number)
    {
        Assert.Throws<DomainException>(() => InvoiceNumber.Create("F", 2026, number));
    }

    [Fact]
    public void CreateFromFormatted_parses_correctly()
    {
        var number = InvoiceNumber.CreateFromFormatted("FOTO2026-0003");
        Assert.Equal("FOTO", number.Serie);
        Assert.Equal(2026, number.Year);
        Assert.Equal(3, number.Number);
    }

    [Fact]
    public void CreateFromFormatted_single_char_serie()
    {
        var number = InvoiceNumber.CreateFromFormatted("F2026-0001");
        Assert.Equal("F", number.Serie);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ABCD")]
    [InlineData("X-0001")]
    public void CreateFromFormatted_invalid_format_throws(string formatted)
    {
        Assert.Throws<DomainException>(() => InvoiceNumber.CreateFromFormatted(formatted));
    }

    [Fact]
    public void Equality_same_value_are_equal()
    {
        var a = InvoiceNumber.Create("F", 2026, 1);
        var b = InvoiceNumber.Create("f", 2026, 1);
        Assert.Equal(a, b);
    }

    [Fact]
    public void Equality_different_number_not_equal()
    {
        var a = InvoiceNumber.Create("F", 2026, 1);
        var b = InvoiceNumber.Create("F", 2026, 2);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void ToString_returns_formatted_value()
    {
        var number = InvoiceNumber.Create("TEST", 2026, 7);
        Assert.Equal("TEST2026-0007", number.ToString());
    }
}
