using CentralBillingService.VeriFactu.Mapping;
using VeriFactu.Xml.Factu;      // Impuesto
using VeriFactu.Xml.Factu.Alta; // CalificacionOperacion

namespace CentralBillingService.Tests.Unit.VeriFactu;

/// <summary>
/// Maps our domain invoice to the mdiago Business.Invoice. Verifies the fiscal inference that
/// lives inside the adapter: IVA vs IGIC by issuer region, and the desglose grouping by rate.
/// </summary>
public class InvoiceToVeriFactuMapperTests
{
    private readonly InvoiceToVeriFactuMapper _mapper = new();

    private static BillingParty IssuerWithPostal(string postalCode) => BillingParty.Create(
        legalName: "Test Autónomo SA",
        taxId: TaxId.Create("12345678A", "ES"),
        address: PostalAddress.Create("Calle Test 1", "Ciudad", postalCode, "ES"),
        email: "issuer@test.com");

    [Theory]
    [InlineData("35001")] // Las Palmas
    [InlineData("38001")] // Santa Cruz de Tenerife
    public void Canary_issuer_maps_to_IGIC(string postalCode)
    {
        var invoice = InvoiceBuilder.BuildIssued(issuer: IssuerWithPostal(postalCode));

        var vf = _mapper.Map(invoice);

        Assert.All(vf.TaxItems, t => Assert.Equal(Impuesto.IGIC, t.Tax));
    }

    [Theory]
    [InlineData("28001")] // Madrid
    [InlineData("08001")] // Barcelona
    public void Mainland_issuer_maps_to_IVA(string postalCode)
    {
        var invoice = InvoiceBuilder.BuildIssued(issuer: IssuerWithPostal(postalCode));

        var vf = _mapper.Map(invoice);

        Assert.All(vf.TaxItems, t => Assert.Equal(Impuesto.IVA, t.Tax));
    }

    [Fact]
    public void Lines_are_grouped_into_one_breakdown_row_per_rate()
    {
        var lines = new List<InvoiceLine>
        {
            InvoiceBuilder.DefaultLine(1, unitPrice: 100m, taxRate: TaxRate.General), // 21%
            InvoiceBuilder.DefaultLine(2, unitPrice: 50m,  taxRate: TaxRate.General), // 21%
            InvoiceBuilder.DefaultLine(3, unitPrice: 10m,  taxRate: TaxRate.Reduced), // 10%
        };
        var invoice = InvoiceBuilder.BuildIssued(issuer: IssuerWithPostal("28001"), lines: lines);

        var vf = _mapper.Map(invoice);

        Assert.Equal(2, vf.TaxItems.Count);
        var general = vf.TaxItems.Single(t => t.TaxRate == 21);
        Assert.Equal(150m, general.TaxBase);   // 100 + 50
        Assert.Equal(31.5m, general.TaxAmount); // 21% of 150
    }

    [Fact]
    public void Domestic_recipient_is_subject_S1()
    {
        var invoice = InvoiceBuilder.BuildIssued(issuer: IssuerWithPostal("28001")); // recipient ES by default

        var vf = _mapper.Map(invoice);

        Assert.All(vf.TaxItems, t => Assert.Equal(CalificacionOperacion.S1, t.TaxType));
    }

    [Fact]
    public void Foreign_recipient_is_not_subject_N2_in_eur()
    {
        var recipient = BillingParty.Create(
            "Foreign Buyer LLC",
            TaxId.Create("US-123", "US"),
            PostalAddress.Create("1 Fifth Ave", "New York", "10001", "US"),
            "buyer@foreign.com");
        var invoice = InvoiceBuilder.BuildIssued(
            issuer: IssuerWithPostal("35001"),
            recipient: recipient,
            lines: [InvoiceBuilder.DefaultLine(1, unitPrice: 200m, taxRate: TaxRate.Zero)]);

        var vf = _mapper.Map(invoice);

        var item = Assert.Single(vf.TaxItems);
        Assert.Equal(CalificacionOperacion.N2, item.TaxType); // no sujeta por localización
        Assert.Equal(200m, item.TaxBase);                     // base en EUR
        Assert.Equal(0m, item.TaxAmount);                     // sin cuota
    }

    private static Invoice ForeignInvoice(string country, ProductType productType)
    {
        var recipient = BillingParty.Create(
            "Foreign Buyer", TaxId.Create($"{country}-123", country),
            PostalAddress.Create("1 Main St", "City", "00000", country), "buyer@foreign.com");
        var line = InvoiceLine.CreateInEur(1, "Item", 1, Money.Of(500m, Currency.EUR), TaxRate.Zero, productType);
        return InvoiceBuilder.BuildIssued(issuer: IssuerWithPostal("35001"), recipient: recipient, lines: [line]);
    }

    [Fact]
    public void Foreign_good_outside_eu_is_exempt_export_E2()
    {
        var vf = _mapper.Map(ForeignInvoice("US", ProductType.Good));

        var item = Assert.Single(vf.TaxItems);
        Assert.Equal(CausaExencion.E2, item.TaxException); // exportación
        Assert.Equal(500m, item.TaxBase);
        Assert.Equal(0m, item.TaxAmount);
    }

    [Fact]
    public void Foreign_good_inside_eu_is_exempt_intracommunity_E5()
    {
        var vf = _mapper.Map(ForeignInvoice("DE", ProductType.Good));

        var item = Assert.Single(vf.TaxItems);
        Assert.Equal(CausaExencion.E5, item.TaxException); // entrega intracomunitaria
    }

    [Fact]
    public void Foreign_service_stays_not_subject_N2()
    {
        var vf = _mapper.Map(ForeignInvoice("US", ProductType.Service));

        var item = Assert.Single(vf.TaxItems);
        Assert.Equal(CalificacionOperacion.N2, item.TaxType);
    }

    [Fact]
    public void Maps_seller_and_buyer_identity()
    {
        var invoice = InvoiceBuilder.BuildIssued(issuer: IssuerWithPostal("28001"));

        var vf = _mapper.Map(invoice);

        Assert.Equal("12345678A", vf.SellerID);
        Assert.Equal("Test Autónomo SA", vf.SellerName);
        Assert.Equal("B12345678", vf.BuyerID);
    }

    [Fact]
    public void Domestic_buyer_has_no_country_or_id_type()
    {
        var vf = _mapper.Map(InvoiceBuilder.BuildIssued(issuer: IssuerWithPostal("28001"))); // recipient ES

        Assert.Null(vf.BuyerCountryID);
    }

    [Fact]
    public void Foreign_buyer_gets_country_and_id_type()
    {
        var eu = _mapper.Map(ForeignInvoice("DE", ProductType.Service));
        Assert.Equal("DE", eu.BuyerCountryID);
        Assert.Equal(IDType.NIF_IVA, eu.BuyerIDType);

        var nonEu = _mapper.Map(ForeignInvoice("US", ProductType.Service));
        Assert.Equal("US", nonEu.BuyerCountryID);
        Assert.Equal(IDType.OTRO_DOC_PROBATORIO, nonEu.BuyerIDType);
    }
}
