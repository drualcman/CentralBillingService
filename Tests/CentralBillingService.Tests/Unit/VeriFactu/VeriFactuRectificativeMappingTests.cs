using CentralBillingService.VeriFactu.Mapping;
using VeriFactu.Xml.Factu.Alta; // TipoFactura, TipoRectificativa, RegistroAlta
using VfInvoice = VeriFactu.Business.Invoice;
using VfInvoiceEntry = VeriFactu.Business.InvoiceEntry;

namespace CentralBillingService.Tests.Unit.VeriFactu;

/// <summary>
/// Rectificatives are R1 (or R5 when rectifying a simplified invoice), reference the rectified invoice,
/// and are "S" (substitution, with ImporteRectificacion) or "I" (differences, signed amounts).
/// </summary>
public class VeriFactuRectificativeMappingTests
{
    private readonly InvoiceToVeriFactuMapper _mapper = new();

    private static BillingParty SpanishRecipient() => BillingParty.Create(
        "Cliente Nacional", TaxId.Create("44714088H", "ES"),
        PostalAddress.Create("Calle Mayor 1", "Las Palmas de Gran Canaria", "35001", "ES"), "cliente@test.com");

    private static BillingParty ForeignRecipientWithoutTaxId() => BillingParty.Create(
        "Juan Dela Cruz", TaxId.Create(string.Empty, "PH"),
        PostalAddress.Create("1 Ayala Ave", "Makati", "1226", "PH"), string.Empty);

    private static RectificativeInvoice Rectify(BillingParty recipient, RectificationType type, decimal lineUnitPrice, TaxRate taxRate)
    {
        Invoice original = InvoiceBuilder.BuildIssued(
            serie: "SUA", number: 40, recipient: recipient,
            lines: [InvoiceBuilder.DefaultLine(1, unitPrice: 15m, taxRate: taxRate)]);

        return RectificativeInvoice.Create(
            number: InvoiceNumber.Create("RSUA", 2026, 1),
            billingSource: "web-test",
            originalInvoice: original,
            rectificationReason: "Devolución del importe cobrado por error",
            rectificationType: type,
            lines: [InvoiceBuilder.DefaultLine(1, unitPrice: lineUnitPrice, taxRate: taxRate)],
            appliedExchangeRate: ExchangeRate.Identity(DateTimeOffset.UtcNow),
            hasher: new FakeInvoiceHasher(),
            issueDate: new DateOnly(2026, 1, 20),
            paymentReference: "PAY-REC-1");
    }

    private static readonly RectifiedInvoiceAmounts RectifiedFifteenEurosAtGeneralRate = new RectifiedInvoiceAmounts(15m, 3.15m);

    [Fact]
    public void Substitution_is_an_R1_S_with_the_rectified_amounts_and_reference()
    {
        RectificativeInvoice rectificative = Rectify(SpanishRecipient(), RectificationType.Substitution, 15m, TaxRate.General);

        VfInvoice vf = _mapper.MapRectificative(rectificative, RectifiedFifteenEurosAtGeneralRate, rectifiesSimplifiedInvoice: false);
        RegistroAlta registro = vf.GetRegistroAlta();

        Assert.Equal(TipoFactura.R1, registro.TipoFactura);
        Assert.Equal(TipoRectificativa.S, registro.TipoRectificativa);
        Assert.Equal("SUA2026-0040", Assert.Single(registro.FacturasRectificadas).NumSerieFactura);
        Assert.Equal("15.00", registro.ImporteRectificacion.BaseRectificada);
        Assert.Equal("3.15", registro.ImporteRectificacion.CuotaRectificada);
    }

    [Fact]
    public void Edited_substitution_declares_the_corrected_amounts_and_the_rectified_invoice_amounts_apart()
    {
        RectificativeInvoice rectificative = Rectify(SpanishRecipient(), RectificationType.Substitution, 10m, TaxRate.General);

        RegistroAlta registro = _mapper.MapRectificative(rectificative, RectifiedFifteenEurosAtGeneralRate, rectifiesSimplifiedInvoice: false).GetRegistroAlta();

        Assert.Equal("12.10", registro.ImporteTotal);
        Assert.Equal("15.00", registro.ImporteRectificacion.BaseRectificada);
        Assert.Equal("3.15", registro.ImporteRectificacion.CuotaRectificada);
    }

    [Fact]
    public void Difference_is_an_R1_I_with_signed_amounts_hashed_with_two_decimals()
    {
        RectificativeInvoice rectificative = Rectify(SpanishRecipient(), RectificationType.Difference, -15m, TaxRate.Zero);

        RegistroAlta registro = _mapper.MapRectificative(rectificative, RectifiedFifteenEurosAtGeneralRate, rectifiesSimplifiedInvoice: false).GetRegistroAlta();

        Assert.Equal(TipoFactura.R1, registro.TipoFactura);
        Assert.Equal(TipoRectificativa.I, registro.TipoRectificativa);
        Assert.Equal("-15.00", registro.ImporteTotal);
        Assert.Null(registro.ImporteRectificacion);
    }

    [Fact]
    public void Rectificative_of_a_simplified_invoice_is_an_R5_without_recipient()
    {
        RectificativeInvoice rectificative = Rectify(ForeignRecipientWithoutTaxId(), RectificationType.Difference, -15m, TaxRate.Zero);

        VfInvoice vf = _mapper.MapRectificative(rectificative, RectifiedFifteenEurosAtGeneralRate, rectifiesSimplifiedInvoice: true);

        Assert.Equal(TipoFactura.R5, vf.InvoiceType);
        Assert.Null(vf.BuyerID);
    }

    [Fact]
    public void Unknown_rectified_type_falls_back_to_the_simplified_invoice_rule_on_the_rectified_amounts()
    {
        RectificativeInvoice withoutTaxId = Rectify(ForeignRecipientWithoutTaxId(), RectificationType.Difference, -1m, TaxRate.Zero);
        RectificativeInvoice withTaxId = Rectify(SpanishRecipient(), RectificationType.Substitution, 15m, TaxRate.Zero);

        Assert.True(InvoiceToVeriFactuMapper.IsLikelyRectifyingSimplifiedInvoice(withoutTaxId, new RectifiedInvoiceAmounts(15m, 0m)));
        Assert.False(InvoiceToVeriFactuMapper.IsLikelyRectifyingSimplifiedInvoice(withoutTaxId, new RectifiedInvoiceAmounts(500m, 0m)));
        Assert.False(InvoiceToVeriFactuMapper.IsLikelyRectifyingSimplifiedInvoice(withTaxId, new RectifiedInvoiceAmounts(15m, 0m)));
    }

    [Theory]
    [InlineData(RectificationType.Substitution, 15, false)]
    [InlineData(RectificationType.Difference, -15, false)]
    [InlineData(RectificationType.Difference, -15, true)]
    public void Library_validation_accepts_the_rectificative(RectificationType type, int unitPrice, bool rectifiesSimplifiedInvoice)
    {
        BillingParty recipient = rectifiesSimplifiedInvoice ? ForeignRecipientWithoutTaxId() : SpanishRecipient();
        RectificativeInvoice rectificative = Rectify(recipient, type, unitPrice, TaxRate.General);

        Exception? validationError = Record.Exception(() =>
            new VfInvoiceEntry(_mapper.MapRectificative(rectificative, RectifiedFifteenEurosAtGeneralRate, rectifiesSimplifiedInvoice)));

        Assert.Null(validationError);
    }
}
