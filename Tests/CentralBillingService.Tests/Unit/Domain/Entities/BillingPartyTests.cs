namespace CentralBillingService.Tests.Unit.Domain.Entities;

public class BillingPartyTests
{
    private static readonly TaxId DefaultTaxId = TaxId.Create("12345678A", "ES");
    private static readonly PostalAddress DefaultAddress =
        PostalAddress.Create("Calle 1", "Madrid", "28001", "ES");

    [Fact]
    public void Create_stores_all_required_fields()
    {
        var party = BillingParty.Create("Empresa SL", DefaultTaxId, DefaultAddress, "empresa@test.com");

        Assert.Equal("Empresa SL", party.LegalName);
        Assert.Equal("empresa@test.com", party.Email);
        Assert.Null(party.TradeName);
        Assert.Null(party.Phone);
        Assert.Null(party.Website);
    }

    [Fact]
    public void Create_stores_optional_fields()
    {
        var party = BillingParty.Create(
            "Empresa SL",
            DefaultTaxId,
            DefaultAddress,
            "empresa@test.com",
            tradeName: "Marca Comercial",
            phone: "+34 600000000",
            website: "https://empresa.com");

        Assert.Equal("Marca Comercial", party.TradeName);
        Assert.Equal("+34 600000000", party.Phone);
        Assert.Equal("https://empresa.com", party.Website);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_empty_legal_name_throws(string name)
    {
        Assert.Throws<DomainException>(() =>
            BillingParty.Create(name, DefaultTaxId, DefaultAddress, "test@test.com"));
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_invalid_email_throws(string email)
    {
        Assert.Throws<DomainException>(() =>
            BillingParty.Create("Empresa SL", DefaultTaxId, DefaultAddress, email));
    }

    [Fact]
    public void DisplayName_returns_trade_name_when_present()
    {
        var party = BillingParty.Create(
            "Empresa Legal SL", DefaultTaxId, DefaultAddress, "t@t.com",
            tradeName: "Marca Comercial");

        Assert.Equal("Marca Comercial", party.DisplayName);
    }

    [Fact]
    public void DisplayName_falls_back_to_legal_name_when_no_trade_name()
    {
        var party = BillingParty.Create("Empresa Legal SL", DefaultTaxId, DefaultAddress, "t@t.com");
        Assert.Equal("Empresa Legal SL", party.DisplayName);
    }

    [Fact]
    public void Create_normalizes_email_to_lowercase()
    {
        var party = BillingParty.Create("Empresa SL", DefaultTaxId, DefaultAddress, "TEST@EMPRESA.COM");
        Assert.Equal("test@empresa.com", party.Email);
    }

    [Fact]
    public void Create_trims_legal_name()
    {
        var party = BillingParty.Create("  Empresa SL  ", DefaultTaxId, DefaultAddress, "t@t.com");
        Assert.Equal("Empresa SL", party.LegalName);
    }
}
