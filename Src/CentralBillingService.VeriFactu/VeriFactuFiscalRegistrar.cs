using System.Security.Cryptography.X509Certificates;
using VeriFactu.Business.Operations; // InvoiceActionMessage.SendXmlBytes (public transport)
using VeriFactu.Net;               // Wsd (certificate + transport)
using VeriFactu.Xml.Soap;          // Envelope
using VeriFactu.Xml.Factu;         // Encadenamiento, RegistroAnterior
using VeriFactu.Xml.Factu.Alta;    // RegistroAlta
using DomainInvoice = CentralBillingService.Domain.Entities.Invoice;
using VfInvoiceEntry = VeriFactu.Business.InvoiceEntry;

namespace CentralBillingService.VeriFactu;

/// <summary>
/// <see cref="IFiscalRegistrar"/> that reports CBS invoices to the Spanish AEAT via VeriFactu,
/// wrapping the mdiago <c>VeriFactu</c> library. Selected for billing sources with
/// Registrar.Type = "VeriFactu".
///
/// Chain of custody: the AEAT "encadenamiento" (per issuer NIF and billing source — each source is an
/// independent SIF) is owned by OUR database
/// (<see cref="IVeriFactuStore"/>) — not the library's file blockchain — so it is robust in a
/// stateless/multi-instance host. <see cref="StampAsync"/> reserves the previous link under a
/// per-chain lock, sets it on the RegistroAlta, and computes the official huella from it.
///
/// VERIFY AGAINST AEAT TEST ENV (needs certificate + Portal de Pruebas):
///  - that <c>GetHashOutput()</c> honours the manually-set Encadenamiento (huella chaining),
///  - the exact SOAP transport/endpoint selection in <see cref="SubmitAsync"/>, and
///  - the full desglose calificación (exenta/ISP/no sujeta) once modelled in the domain.
/// </summary>
public sealed class VeriFactuFiscalRegistrar : IFiscalRegistrar
{
    public const string TypeName = "VeriFactu";

    private readonly IVeriFactuStore _store;
    private readonly InvoiceToVeriFactuMapper _mapper;
    private readonly VeriFactuOptions _options;
    private readonly ILogger<VeriFactuFiscalRegistrar> _logger;

    /// <summary>Built per billing source by <see cref="VeriFactuFiscalRegistrarBuilder"/>, so the
    /// <paramref name="options"/> carry that source's own certificate/endpoint.</summary>
    public VeriFactuFiscalRegistrar(
        IVeriFactuStore store,
        InvoiceToVeriFactuMapper mapper,
        VeriFactuOptions options,
        ILogger<VeriFactuFiscalRegistrar> logger)
    {
        _store = store;
        _mapper = mapper;
        _options = options;
        _logger = logger;
    }

    public string RegistrarType => TypeName;

    public async Task<FiscalStampResult> StampAsync(DomainInvoice invoice, CancellationToken cancellationToken = default)
    {
        var nif = invoice.Issuer.TaxId.Value;
        var numSerie = invoice.Number.Value;
        var fecha = invoice.IssueDate.ToString("dd-MM-yyyy");

        // The record (SistemaInformatico) and the QR URL (test vs production validate host) are
        // built from the library's static Settings.Current, so apply THIS source's settings first.
        var huella = await _store.StampAsync(nif, invoice.BillingSource, numSerie, numSerie, fecha, previous =>
        {
            lock (VeriFactuSettingsConfigurator.SyncRoot)
            {
                VeriFactuSettingsConfigurator.Apply(_options);
                return ComputeStamp(invoice, nif, previous);
            }
        }, cancellationToken);

        // The QR only depends on NIF/number/date/total, not on the chain, so it is rebuilt here —
        // also covering the idempotent re-stamp path, where the store skips the compute callback.
        string qrUrl;
        lock (VeriFactuSettingsConfigurator.SyncRoot)
        {
            VeriFactuSettingsConfigurator.Apply(_options);
            qrUrl = _mapper.Map(invoice).GetRegistroAlta().GetUrlValidate();
        }

        return new FiscalStampResult(huella, qrUrl);
    }

    private StampComputation ComputeStamp(DomainInvoice invoice, string nif, ChainLink previous)
    {
        var registro = _mapper.Map(invoice).GetRegistroAlta();

        registro.Encadenamiento = previous.IsFirst
            ? new Encadenamiento { PrimerRegistro = "S" }
            : new Encadenamiento
            {
                RegistroAnterior = new RegistroAnterior
                {
                    IDEmisorFactura = nif,
                    NumSerieFactura = previous.NumSerie,
                    FechaExpedicionFactura = previous.FechaExpedicion,
                    Huella = previous.Huella,
                }
            };

        var fechaHoraGen = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz");
        registro.FechaHoraHusoGenRegistro = fechaHoraGen;

        var computedHuella = registro.GetHashOutput();
        registro.Huella = computedHuella;

        return new StampComputation(computedHuella, fechaHoraGen);
    }

    public Task<FiscalSubmissionOutcome> SubmitAsync(DomainInvoice invoice, CancellationToken cancellationToken = default) =>
        _store.RunExclusiveOnChainAsync(
            invoice.Issuer.TaxId.Value, invoice.BillingSource,
            () => SubmitHoldingChainLockAsync(invoice, cancellationToken),
            cancellationToken);

    private async Task<FiscalSubmissionOutcome> SubmitHoldingChainLockAsync(DomainInvoice invoice, CancellationToken cancellationToken)
    {
        var submission = await _store.GetSubmissionAsync(invoice.BillingSource, invoice.Number.Value, cancellationToken);

        if (submission is null)
            throw new InvalidOperationException(
                $"No stamped submission found for invoice '{invoice.Number.Value}' — StampAsync must run before SubmitAsync.");

        // Idempotent: a redelivered message (or a drain that already covered this record) must not
        // re-send a record the AEAT has already answered.
        if (submission.State != (int)FiscalSubmissionState.Pending)
            return new FiscalSubmissionOutcome(
                (FiscalSubmissionState)submission.State, submission.Csv, submission.Huella,
                submission.ErrorCode, submission.ErrorDescription);

        FiscalSubmissionOutcome outcome;
        try
        {
            // We must register the SAME huella we stamped and printed on the QR — NOT the one the
            // library would recompute from its own file blockchain. The high-level InvoiceEntry.Save()
            // does Post() which "recalcula Xml con la info Blockchain actualizada" (overwriting our
            // chain) and writes to disk (unreliable in serverless). So instead we inject OUR chain of
            // custody (from our DB) onto the RegistroAlta and send via the LOW-LEVEL transport, which
            // does not touch the blockchain.
            //
            // The library's Settings.Current is process-wide static, so we apply THIS source's
            // certificate/endpoint/SIF and build + send inside the shared lock. The entry must be
            // built AFTER Apply: its RegistroAlta takes the SistemaInformatico from Settings.Current.
            Envelope responseEnvelope;
            lock (VeriFactuSettingsConfigurator.SyncRoot)
            {
                VeriFactuSettingsConfigurator.Apply(_options);
                VfInvoiceEntry entry = new VfInvoiceEntry(_mapper.Map(invoice));
                ApplyStoredChain((RegistroAlta)entry.Registro, invoice.Issuer.TaxId.Value, submission);
                X509Certificate2 certificate = Wsd.GetCertificate(); // from the Settings we just applied
                responseEnvelope = SendWithoutBlockchain(entry, certificate);
            }

            outcome = VeriFactuResponseInterpreter.Interpret(responseEnvelope, submission.Huella);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VeriFactu submission failed for invoice {InvoiceNumber}.", invoice.Number.Value);
            throw; // transient — let the worker retry
        }

        submission.State = (int)outcome.State;
        submission.Csv = outcome.Csv ?? submission.Csv; // a duplicate (3000) answer carries no CSV: keep the original
        submission.ErrorCode = outcome.ErrorCode;
        submission.ErrorDescription = outcome.ErrorDescription;
        submission.SentUtc = DateTimeOffset.UtcNow;
        submission.RetryCount++;
        await _store.UpdateSubmissionAsync(submission, cancellationToken);

        return outcome;
    }

    public async Task<IReadOnlyList<string>> GetPendingSubmissionsUpToAsync(DomainInvoice invoice, CancellationToken cancellationToken = default)
    {
        VeriFactuSubmissionEntity? submission = await _store.GetSubmissionAsync(invoice.BillingSource, invoice.Number.Value, cancellationToken);
        IReadOnlyList<string> pendingInvoiceNumbers = submission is null
            ? Array.Empty<string>()
            : await _store.GetPendingInChainUpToAsync(submission.IssuerNif, submission.BillingSource, submission.ChainSequence, cancellationToken);

        return pendingInvoiceNumbers;
    }

    public Task<string?> GetLatestStalledSubmissionAsync(string billingSource, DateTimeOffset stampedBefore, CancellationToken cancellationToken = default) =>
        _store.GetLatestPendingStampedBeforeAsync(billingSource, stampedBefore, cancellationToken);

    /// <summary>Reproduces the exact record that produced our stored huella (our DB chain of custody).</summary>
    private static void ApplyStoredChain(RegistroAlta registro, string issuerNif, VeriFactuSubmissionEntity submission)
    {
        registro.Encadenamiento = string.IsNullOrEmpty(submission.PreviousHuella)
            ? new Encadenamiento { PrimerRegistro = "S" }
            : new Encadenamiento
            {
                RegistroAnterior = new RegistroAnterior
                {
                    IDEmisorFactura = issuerNif,
                    NumSerieFactura = submission.PreviousNumSerie,
                    FechaExpedicionFactura = submission.PreviousFechaExpedicion,
                    Huella = submission.PreviousHuella,
                }
            };
        registro.FechaHoraHusoGenRegistro = submission.FechaHoraGenRegistro;
        registro.Huella = submission.Huella;
    }

    /// <summary>
    /// Sends the (already chained + stamped) record to the AEAT WITHOUT the library's file
    /// blockchain. <c>InvoiceEntry.Save()</c> runs Post first, which recomputes the huella from its
    /// own disk chain (overwriting ours). So we use the library's PUBLIC low-level pieces instead:
    /// <c>GetXml()</c> builds the SOAP envelope from <c>entry.Registro</c> (OUR chain),
    /// <c>SendXmlBytes</c> posts it with mutual-TLS to the configured endpoint, and
    /// <c>GetResponseEnvelope</c> parses the AEAT answer. Nothing is written to disk.
    /// </summary>
    private static Envelope SendWithoutBlockchain(VfInvoiceEntry entry, X509Certificate2 certificate)
    {
        byte[] envelopeXml = entry.GetXml();
        string response = InvoiceActionMessage.SendXmlBytes(envelopeXml, certificate: certificate);

        if (string.IsNullOrWhiteSpace(response) || response.TrimStart().StartsWith("<!DOCTYPE html", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"The AEAT did not return a SOAP XML response: {response}");

        return entry.GetResponseEnvelope(response);
    }
}
