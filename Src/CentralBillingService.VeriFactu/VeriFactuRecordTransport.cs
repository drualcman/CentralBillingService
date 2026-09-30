using System.Security.Cryptography.X509Certificates;
using VeriFactu.Business.Operations; // InvoiceActionMessage.SendXmlBytes (public transport)
using VeriFactu.Xml.Factu;           // Encadenamiento, RegistroAnterior
using VeriFactu.Xml.Factu.Alta;      // RegistroAlta
using VeriFactu.Xml.Soap;            // Envelope
using VfInvoiceEntry = VeriFactu.Business.InvoiceEntry;

namespace CentralBillingService.VeriFactu;

/// <summary>
/// Chains a record with OUR chain of custody and sends it to the AEAT without the library's file
/// blockchain. <c>InvoiceEntry.Save()</c> runs Post first, which recomputes the huella from its own
/// disk chain (overwriting ours). So we use the library's PUBLIC low-level pieces instead:
/// <c>GetXml()</c> builds the SOAP envelope from <c>entry.Registro</c> (OUR chain),
/// <c>SendXmlBytes</c> posts it with mutual-TLS to the configured endpoint, and
/// <c>GetResponseEnvelope</c> parses the AEAT answer. Nothing is written to disk.
/// </summary>
internal static class VeriFactuRecordTransport
{
    public static Encadenamiento BuildChainLink(string issuerNif, string? previousNumSerie, string? previousFechaExpedicion, string? previousHuella) =>
        string.IsNullOrEmpty(previousHuella)
            ? new Encadenamiento { PrimerRegistro = "S" }
            : new Encadenamiento
            {
                RegistroAnterior = new RegistroAnterior
                {
                    IDEmisorFactura = issuerNif,
                    NumSerieFactura = previousNumSerie,
                    FechaExpedicionFactura = previousFechaExpedicion,
                    Huella = previousHuella,
                }
            };

    /// <summary>Reproduces the exact record that produced our stored huella (our DB chain of custody).</summary>
    public static void ApplyStoredChain(RegistroAlta registro, string issuerNif, VeriFactuSubmissionEntity submission)
    {
        registro.Encadenamiento = BuildChainLink(
            issuerNif, submission.PreviousNumSerie, submission.PreviousFechaExpedicion, submission.PreviousHuella);
        registro.FechaHoraHusoGenRegistro = submission.FechaHoraGenRegistro;
        registro.Huella = submission.Huella;
    }

    public static Envelope Send(VfInvoiceEntry entry, X509Certificate2 certificate)
    {
        byte[] envelopeXml = entry.GetXml();
        string response = InvoiceActionMessage.SendXmlBytes(envelopeXml, certificate: certificate);

        if (string.IsNullOrWhiteSpace(response) || response.TrimStart().StartsWith("<!DOCTYPE html", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"The AEAT did not return a SOAP XML response: {response}");

        return entry.GetResponseEnvelope(response);
    }
}
