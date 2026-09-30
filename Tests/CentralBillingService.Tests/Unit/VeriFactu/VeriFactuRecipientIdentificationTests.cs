using CentralBillingService.VeriFactu.Mapping;
using VeriFactu.Xml.Factu;      // IDType
using VeriFactu.Xml.Factu.Alta; // TipoFactura
using VfInvoice = VeriFactu.Business.Invoice;
using VfInvoiceEntry = VeriFactu.Business.InvoiceEntry;

namespace CentralBillingService.Tests.Unit.VeriFactu;

/// <summary>
/// The AEAT only allows a full invoice (F1) with an identified recipient. A recipient without tax id
/// becomes a simplified invoice (F2) up to 400 €, or an F1 identified by another reference above it.
/// </summary>
public class VeriFactuRecipientIdentificationTests
{
    private readonly InvoiceToVeriFactuMapper _mapper = new();

    private static BillingParty ForeignRecipientWithoutTaxId(
        string? externalId = null, string? phone = null, string email = "") => BillingParty.Create(
        "Juan Dela Cruz",
        TaxId.Create(string.Empty, "PH"),
        PostalAddress.Create("1 Ayala Ave", "Makati", "1226", "PH"),
        email,
        phone: phone,
        externalId: externalId);

    private static List<InvoiceLine> LinesTotalling(decimal unitPrice) =>
        new List<InvoiceLine> { InvoiceBuilder.DefaultLine(1, unitPrice: unitPrice, taxRate: TaxRate.Zero) };

    [Fact]
    public void Recipient_with_tax_id_is_a_full_invoice_identified_by_it()
    {
        Invoice invoice = InvoiceBuilder.BuildIssued();

        VfInvoice vf = _mapper.Map(invoice);

        Assert.Equal(TipoFactura.F1, vf.InvoiceType);
        Assert.Equal(invoice.Recipient.TaxId.Value, vf.BuyerID);
    }

    [Fact]
    public void Recipient_without_tax_id_up_to_400_eur_is_a_simplified_invoice_without_buyer()
    {
        Invoice invoice = InvoiceBuilder.BuildIssued(recipient: ForeignRecipientWithoutTaxId(), lines: LinesTotalling(400m));

        VfInvoice vf = _mapper.Map(invoice);

        Assert.Equal(TipoFactura.F2, vf.InvoiceType);
        Assert.Null(vf.BuyerID);
    }

    [Fact]
    public void Spanish_recipient_without_nif_up_to_400_eur_is_a_simplified_invoice()
    {
        BillingParty spanishConsumerWithoutNif = BillingParty.Create(
            "Consumidor Final",
            TaxId.NotProvided("ES"),
            PostalAddress.Create("Calle Mayor 1", "Las Palmas de Gran Canaria", "35001", "ES"),
            string.Empty);
        Invoice invoice = InvoiceBuilder.BuildIssued(recipient: spanishConsumerWithoutNif, lines: LinesTotalling(100m));

        VfInvoice vf = _mapper.Map(invoice);

        Assert.Equal(TipoFactura.F2, vf.InvoiceType);
        Assert.Null(vf.BuyerID);
    }

    [Fact]
    public void Spanish_recipient_without_nif_above_400_eur_cannot_be_registered()
    {
        BillingParty spanishConsumerWithoutNif = BillingParty.Create(
            "Consumidor Final",
            TaxId.Create(string.Empty, "ES"),
            PostalAddress.Create("Calle Mayor 1", "Las Palmas de Gran Canaria", "35001", "ES"),
            string.Empty);
        Invoice invoice = InvoiceBuilder.BuildIssued(recipient: spanishConsumerWithoutNif, lines: LinesTotalling(500m));

        Assert.Throws<InvalidOperationException>(() => _mapper.Map(invoice));
    }

    [Fact]
    public void Foreign_recipient_without_tax_id_above_400_eur_is_identified_by_external_id()
    {
        Invoice invoice = InvoiceBuilder.BuildIssued(
            recipient: ForeignRecipientWithoutTaxId(externalId: "USR-1001", phone: "+639159915081"),
            lines: LinesTotalling(500m));

        VfInvoice vf = _mapper.Map(invoice);

        Assert.Equal(TipoFactura.F1, vf.InvoiceType);
        Assert.Equal("USR-1001", vf.BuyerID);
        Assert.Equal("PH", vf.BuyerCountryID);
        Assert.Equal(IDType.OTRO_DOC_PROBATORIO, vf.BuyerIDType);
    }

    [Fact]
    public void Without_external_id_the_phone_identifies_the_foreign_recipient()
    {
        Invoice invoice = InvoiceBuilder.BuildIssued(
            recipient: ForeignRecipientWithoutTaxId(phone: "+639159915081", email: "juan.delacruz.long.email@example.com"),
            lines: LinesTotalling(500m));

        VfInvoice vf = _mapper.Map(invoice);

        Assert.Equal("+639159915081", vf.BuyerID);
    }

    [Theory]
    [InlineData(50)]  // F2 simplified, no recipient
    [InlineData(500)] // F1 identified by IDOtro "other supporting document"
    public void Library_validation_accepts_a_foreign_recipient_without_tax_id(int unitPrice)
    {
        Invoice invoice = InvoiceBuilder.BuildIssued(
            recipient: ForeignRecipientWithoutTaxId(externalId: "USR-1001"),
            lines: LinesTotalling(unitPrice));

        Exception? validationError = Record.Exception(() => new VfInvoiceEntry(_mapper.Map(invoice)));

        Assert.Null(validationError);
    }

    [Fact]
    public void Email_longer_than_20_characters_cannot_identify_the_recipient()
    {
        Invoice invoice = InvoiceBuilder.BuildIssued(
            recipient: ForeignRecipientWithoutTaxId(email: "juan.delacruz.long.email@example.com"),
            lines: LinesTotalling(500m));

        Assert.Throws<InvalidOperationException>(() => _mapper.Map(invoice));
    }
}
