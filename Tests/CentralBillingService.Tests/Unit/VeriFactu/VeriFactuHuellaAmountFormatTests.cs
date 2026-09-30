using CentralBillingService.VeriFactu.Mapping;
using VeriFactu.Xml.Factu.Alta; // RegistroAlta

namespace CentralBillingService.Tests.Unit.VeriFactu;

/// <summary>
/// The AEAT computes the huella with amounts always written with two decimals ("15.00", "0.00").
/// A C# decimal keeps its scale (15m prints "15"), so round amounts with 0 % tax produced a huella
/// the AEAT rejected (error 2000). Amounts handed to the library must carry two decimals.
/// </summary>
public class VeriFactuHuellaAmountFormatTests
{
    private readonly InvoiceToVeriFactuMapper _mapper = new();

    private static Invoice DomesticInvoiceOf(decimal unitPrice, TaxRate taxRate) => InvoiceBuilder.BuildIssued(
        recipient: BillingParty.Create(
            "Cliente Nacional", TaxId.Create("44714088H", "ES"),
            PostalAddress.Create("Calle Mayor 1", "Las Palmas de Gran Canaria", "35001", "ES"), "cliente@test.com"),
        lines: [InvoiceBuilder.DefaultLine(1, unitPrice: unitPrice, taxRate: taxRate)]);

    [Fact]
    public void Round_amount_with_zero_tax_is_hashed_with_two_decimals()
    {
        RegistroAlta registro = _mapper.Map(DomesticInvoiceOf(15m, TaxRate.Zero)).GetRegistroAlta();

        Assert.Equal("0.00", registro.CuotaTotal);
        Assert.Equal("15.00", registro.ImporteTotal);
    }

    [Fact]
    public void Round_amount_with_tax_is_hashed_with_two_decimals()
    {
        RegistroAlta registro = _mapper.Map(DomesticInvoiceOf(100m, TaxRate.General)).GetRegistroAlta();

        Assert.Equal("21.00", registro.CuotaTotal);
        Assert.Equal("121.00", registro.ImporteTotal);
    }

    [Fact]
    public void Huella_matches_the_aeat_computation_for_a_round_zero_tax_invoice()
    {
        // Real case SUA2026-0043: the AEAT answered error 2000 with this exact hash input.
        const string previousHuella = "83A968BCF0BDD02943A38537760966E5E76D8289072A20508FD513594A1E003C";
        const string generatedAt = "2026-09-30T15:44:40+08:00";
        const string aeatHashInput =
            "IDEmisorFactura=12345678A&NumSerieFactura=TEST2026-0001&FechaExpedicionFactura=15-01-2026&TipoFactura=F1" +
            "&CuotaTotal=0.00&ImporteTotal=15.00&Huella=" + previousHuella + "&FechaHoraHusoGenRegistro=" + generatedAt;
        RegistroAlta registro = _mapper.Map(DomesticInvoiceOf(15m, TaxRate.Zero)).GetRegistroAlta();
        registro.Encadenamiento = new global::VeriFactu.Xml.Factu.Encadenamiento
        {
            RegistroAnterior = new global::VeriFactu.Xml.Factu.RegistroAnterior
            {
                IDEmisorFactura = "12345678A", NumSerieFactura = "TEST2026-0000",
                FechaExpedicionFactura = "15-01-2026", Huella = previousHuella,
            }
        };
        registro.FechaHoraHusoGenRegistro = generatedAt;

        string expectedHuella = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(aeatHashInput)));

        Assert.Equal(expectedHuella, registro.GetHashOutput());
    }

    [Fact]
    public void Round_foreign_amount_is_hashed_with_two_decimals()
    {
        BillingParty foreignRecipient = BillingParty.Create(
            "Foreign Buyer LLC", TaxId.Create("US-123", "US"),
            PostalAddress.Create("1 Fifth Ave", "New York", "10001", "US"), "buyer@foreign.com");
        Invoice invoice = InvoiceBuilder.BuildIssued(
            recipient: foreignRecipient,
            lines: [InvoiceBuilder.DefaultLine(1, unitPrice: 200m, taxRate: TaxRate.Zero)]);

        RegistroAlta registro = _mapper.Map(invoice).GetRegistroAlta();

        Assert.Equal("0.00", registro.CuotaTotal);
        Assert.Equal("200.00", registro.ImporteTotal);
    }
}
