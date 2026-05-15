namespace CentralBillingService.Tests.Unit.Domain.ValueObjects;

public class TaxIdTests
{
    [Fact]
    public void Create_spanish_nif_detected_correctly()
    {
        // NIF: starts with non-X/Y/Z letter AND ends with letter (e.g. A1234567B)
        var taxId = TaxId.Create("A1234567B", "ES");

        Assert.Equal(TaxIdType.NIF, taxId.Type);
        Assert.Equal("ES", taxId.CountryCode);
        Assert.True(taxId.IsSpanish);
    }

    [Fact]
    public void Create_spanish_cif_detected_correctly()
    {
        // CIF: starts with letter AND ends with digit
        var taxId = TaxId.Create("B12345678", "ES");
        Assert.Equal(TaxIdType.CIF, taxId.Type);
    }

    [Fact]
    public void Create_spanish_nie_detected_correctly()
    {
        // NIE: starts with X/Y/Z AND ends with digit (if ends with letter → caught as NIF first)
        var taxId = TaxId.Create("X12345678", "ES");
        Assert.Equal(TaxIdType.NIE, taxId.Type);
    }

    [Fact]
    public void Create_eu_vat_detected_correctly()
    {
        var taxId = TaxId.Create("DE123456789", "DE");
        Assert.Equal(TaxIdType.EuVat, taxId.Type);
    }

    [Fact]
    public void Create_normalizes_value_to_uppercase()
    {
        var taxId = TaxId.Create("b12345678", "ES");
        Assert.Equal("B12345678", taxId.Value);
    }

    [Fact]
    public void Create_normalizes_country_to_uppercase()
    {
        var taxId = TaxId.Create("12345678A", "es");
        Assert.Equal("ES", taxId.CountryCode);
    }

    [Theory]
    [InlineData("", "ES")]
    [InlineData("   ", "ES")]
    public void Create_empty_value_throws(string value, string country)
    {
        Assert.Throws<DomainException>(() => TaxId.Create(value, country));
    }

    [Theory]
    [InlineData("12345678A", "E")]
    [InlineData("12345678A", "ESP")]
    [InlineData("12345678A", "")]
    [InlineData("12345678A", "   ")]
    public void Create_invalid_country_code_length_throws(string value, string country)
    {
        Assert.Throws<DomainException>(() => TaxId.Create(value, country));
    }

    [Fact]
    public void NotProvided_creates_placeholder_with_correct_type()
    {
        var taxId = TaxId.NotProvided("US");

        Assert.True(taxId.IsNotProvided);
        Assert.Equal(TaxIdType.NotProvided, taxId.Type);
        Assert.Equal("NO_ID", taxId.Value);
        Assert.Equal("US", taxId.CountryCode);
    }

    [Fact]
    public void IsSpanish_false_for_non_spanish_tax_id()
    {
        var taxId = TaxId.Create("DE123456789", "DE");
        Assert.False(taxId.IsSpanish);
    }

    [Fact]
    public void Equality_based_on_value_and_country()
    {
        var a = TaxId.Create("B12345678", "ES");
        var b = TaxId.Create("b12345678", "es");
        Assert.Equal(a, b);
    }

    [Fact]
    public void Equality_different_country_not_equal()
    {
        var a = TaxId.Create("12345678A", "ES");
        var b = TaxId.Create("12345678A", "PT");
        Assert.NotEqual(a, b);
    }
}
