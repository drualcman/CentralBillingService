using CentralBillingService.VeriFactu.Mapping;
using VeriFactu.Xml.Factu;      // Encadenamiento, RegistroAnterior
using VeriFactu.Xml.Factu.Alta; // RegistroAlta

namespace CentralBillingService.Tests.Unit.VeriFactu;

/// <summary>
/// Verifies the core assumption of our DB-owned chain of custody: the official AEAT huella
/// (computed by the library's GetHashOutput) actually depends on the Encadenamiento WE set on the
/// RegistroAlta — not on the library's internal file blockchain. If it does, our SqlVeriFactuStore
/// (which feeds the previous link into the compute callback) drives the huella correctly.
/// </summary>
public class VeriFactuHuellaChainingTests
{
    private readonly InvoiceToVeriFactuMapper _mapper = new();

    private RegistroAlta BuildRegistro()
    {
        // Foreign recipient (export): exercises the buyer country/ID-type path and avoids any
        // dependence on Spanish NIF checksum validation inside the library.
        var recipient = BillingParty.Create(
            "Foreign Buyer LLC", TaxId.Create("US-123", "US"),
            PostalAddress.Create("1 Fifth Ave", "New York", "10001", "US"), "buyer@foreign.com");
        var invoice = InvoiceBuilder.BuildIssued(
            issuer: BillingParty.Create(
                "Test Autónomo SA", TaxId.Create("12345678A", "ES"),
                PostalAddress.Create("Calle Test 1", "Madrid", "28001", "ES"), "issuer@test.com"),
            recipient: recipient);
        var registro = _mapper.Map(invoice).GetRegistroAlta();
        registro.FechaHoraHusoGenRegistro = "2026-07-26T10:00:00+02:00"; // fixed so only chaining varies
        return registro;
    }

    private static void SetPrevious(RegistroAlta r, string huella) =>
        r.Encadenamiento = new Encadenamiento
        {
            RegistroAnterior = new RegistroAnterior
            {
                IDEmisorFactura = "12345678A",
                NumSerieFactura = "A/1",
                FechaExpedicionFactura = "25-07-2026",
                Huella = huella,
            }
        };

    [Fact]
    public void Huella_depends_on_the_previous_link_we_set()
    {
        var r1 = BuildRegistro();
        SetPrevious(r1, "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA1");
        var hashA = r1.GetHashOutput();

        var r2 = BuildRegistro();
        SetPrevious(r2, "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB2");
        var hashB = r2.GetHashOutput();

        // Different previous huella → different output huella ⇒ our Encadenamiento feeds the hash.
        Assert.NotEqual(hashA, hashB);
        Assert.False(string.IsNullOrWhiteSpace(hashA));
    }

    [Fact]
    public void First_record_differs_from_a_chained_record()
    {
        var first = BuildRegistro();
        first.Encadenamiento = new Encadenamiento { PrimerRegistro = "S" };
        var firstHash = first.GetHashOutput();

        var chained = BuildRegistro();
        SetPrevious(chained, "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA1");
        var chainedHash = chained.GetHashOutput();

        Assert.NotEqual(firstHash, chainedHash);
    }

    [Fact]
    public void Same_previous_link_is_deterministic()
    {
        var r1 = BuildRegistro();
        SetPrevious(r1, "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA1");
        var r2 = BuildRegistro();
        SetPrevious(r2, "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA1");

        Assert.Equal(r1.GetHashOutput(), r2.GetHashOutput());
    }
}
