namespace CentralBillingService.Tests.Unit.Domain.ValueObjects;

public class PostalAddressTests
{
    [Fact]
    public void Create_builds_address_with_required_fields()
    {
        var address = PostalAddress.Create("Calle Mayor 1", "Barcelona", "08001", "ES");

        Assert.Equal("Calle Mayor 1", address.Line1);
        Assert.Equal("Barcelona", address.City);
        Assert.Equal("08001", address.PostalCode);
        Assert.Equal("ES", address.CountryCode);
        Assert.Null(address.Line2);
        Assert.Null(address.Province);
    }

    [Fact]
    public void Create_stores_optional_fields()
    {
        var address = PostalAddress.Create(
            "Calle Mayor 1",
            "Barcelona",
            "08001",
            "ES",
            line2: "Piso 3",
            province: "Cataluña");

        Assert.Equal("Piso 3", address.Line2);
        Assert.Equal("Cataluña", address.Province);
    }

    [Theory]
    [InlineData("", "Barcelona", "08001", "ES")]
    [InlineData("   ", "Barcelona", "08001", "ES")]
    public void Create_empty_line1_throws(string line1, string city, string postal, string country)
    {
        Assert.Throws<DomainException>(() => PostalAddress.Create(line1, city, postal, country));
    }

    [Theory]
    [InlineData("Calle", "", "08001", "ES")]
    [InlineData("Calle", "   ", "08001", "ES")]
    public void Create_empty_city_throws(string line1, string city, string postal, string country)
    {
        Assert.Throws<DomainException>(() => PostalAddress.Create(line1, city, postal, country));
    }

    [Theory]
    [InlineData("Calle", "Barcelona", "", "ES")]
    [InlineData("Calle", "Barcelona", "   ", "ES")]
    public void Create_empty_postal_code_throws(string line1, string city, string postal, string country)
    {
        Assert.Throws<DomainException>(() => PostalAddress.Create(line1, city, postal, country));
    }

    [Fact]
    public void Create_invalid_country_code_length_throws()
    {
        Assert.Throws<DomainException>(() =>
            PostalAddress.Create("Calle", "Barcelona", "08001", "ESP"));
    }

    [Fact]
    public void Create_empty_country_code_throws()
    {
        Assert.Throws<DomainException>(() =>
            PostalAddress.Create("Calle", "Barcelona", "08001", ""));
    }

    [Fact]
    public void Create_normalizes_country_to_uppercase()
    {
        var address = PostalAddress.Create("Calle", "Barcelona", "08001", "es");
        Assert.Equal("ES", address.CountryCode);
    }

    [Fact]
    public void ToSingleLine_contains_all_main_parts()
    {
        var address = PostalAddress.Create("Calle Mayor 1", "Barcelona", "08001", "ES");
        var result = address.ToSingleLine();

        Assert.Contains("Calle Mayor 1", result);
        Assert.Contains("08001 Barcelona", result);
        Assert.Contains("ES", result);
    }

    [Fact]
    public void ToSingleLine_includes_line2_when_present()
    {
        var address = PostalAddress.Create("Calle Mayor 1", "Barcelona", "08001", "ES", line2: "1º B");
        Assert.Contains("1º B", address.ToSingleLine());
    }

    [Fact]
    public void ToSingleLine_includes_province_when_present()
    {
        var address = PostalAddress.Create("Calle Mayor 1", "Barcelona", "08001", "ES", province: "Cataluña");
        Assert.Contains("Cataluña", address.ToSingleLine());
    }
}
