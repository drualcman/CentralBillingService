using System.Security.Cryptography.X509Certificates;
using VeriFactu.Net;               // Wsd (certificate)
using VeriFactu.Xml.Soap;          // Envelope
using VeriFactu.Xml.Factu.Alta;    // RegistroAlta, TipoFactura
using DomainInvoice = CentralBillingService.Domain.Entities.Invoice;
using VfInvoice = VeriFactu.Business.Invoice;
using VfInvoiceEntry = VeriFactu.Business.InvoiceEntry;

namespace CentralBillingService.VeriFactu;

/// <summary>
/// <see cref="IFiscalRegistrar"/> that reports CBS invoices and rectificatives to the Spanish AEAT via
/// VeriFactu, wrapping the mdiago <c>VeriFactu</c> library. Selected for billing sources with
/// Registrar.Type = "VeriFactu".
///
/// Chain of custody: the AEAT "encadenamiento" (per issuer NIF and billing source — each source is an
/// independent SIF) is owned by OUR database (<see cref="IVeriFactuStore"/>), not the library's file
/// blockchain, so it is robust in a stateless/multi-instance host. Invoices and rectificatives share the
/// same chain: both are handled as a <see cref="FiscalDocument"/>.
///
/// The library's Settings.Current (certificate, endpoint, SistemaInformatico, QR host) is process-wide
/// static, so every library call applies THIS source's settings inside <see cref="VeriFactuSettingsConfigurator.SyncRoot"/>.
/// </summary>
public sealed class VeriFactuFiscalRegistrar : IFiscalRegistrar
{
    public const string TypeName = "VeriFactu";

    private readonly IVeriFactuStore _store;
    private readonly InvoiceToVeriFactuMapper _mapper;
    private readonly VeriFactuOptions _options;
    private readonly ILogger<VeriFactuFiscalRegistrar> _logger;

    /// <summary>What the registrar needs from an invoice or a rectificative to stamp and submit it.</summary>
    private sealed record FiscalDocument(string IssuerNif, string BillingSource, string Number, DateOnly IssueDate, Func<VfInvoice> BuildLibraryInvoice);

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

    public Task<FiscalStampResult> StampAsync(DomainInvoice invoice, CancellationToken cancellationToken = default) =>
        StampDocumentAsync(ToDocument(invoice), cancellationToken);

    public async Task<FiscalStampResult> StampRectificativeAsync(RectificativeInvoice rectificative, RectifiedInvoiceAmounts rectifiedAmounts, CancellationToken cancellationToken = default) =>
        await StampDocumentAsync(await ToDocumentAsync(rectificative, rectifiedAmounts, cancellationToken), cancellationToken);

    public Task<FiscalSubmissionOutcome> SubmitAsync(DomainInvoice invoice, CancellationToken cancellationToken = default) =>
        SubmitDocumentAsync(ToDocument(invoice), cancellationToken);

    public async Task<FiscalSubmissionOutcome> SubmitRectificativeAsync(RectificativeInvoice rectificative, RectifiedInvoiceAmounts rectifiedAmounts, CancellationToken cancellationToken = default) =>
        await SubmitDocumentAsync(await ToDocumentAsync(rectificative, rectifiedAmounts, cancellationToken), cancellationToken);

    public async Task<IReadOnlyList<string>> GetPendingSubmissionsUpToAsync(string billingSource, string invoiceNumber, CancellationToken cancellationToken = default)
    {
        VeriFactuSubmissionEntity? submission = await _store.GetSubmissionAsync(billingSource, invoiceNumber, cancellationToken);
        IReadOnlyList<string> pendingInvoiceNumbers = submission is null
            ? Array.Empty<string>()
            : await _store.GetPendingInChainUpToAsync(submission.IssuerNif, submission.BillingSource, submission.ChainSequence, cancellationToken);

        return pendingInvoiceNumbers;
    }

    public Task<string?> GetLatestStalledSubmissionAsync(string billingSource, DateTimeOffset stampedBefore, CancellationToken cancellationToken = default) =>
        _store.GetLatestPendingStampedBeforeAsync(billingSource, stampedBefore, cancellationToken);

    private FiscalDocument ToDocument(DomainInvoice invoice) => new FiscalDocument(
        invoice.Issuer.TaxId.Value, invoice.BillingSource, invoice.Number.Value, invoice.IssueDate,
        () => _mapper.Map(invoice));

    private async Task<FiscalDocument> ToDocumentAsync(RectificativeInvoice rectificative, RectifiedInvoiceAmounts rectifiedAmounts, CancellationToken cancellationToken)
    {
        bool rectifiesSimplifiedInvoice = await RectifiesSimplifiedInvoiceAsync(rectificative, rectifiedAmounts, cancellationToken);
        return new FiscalDocument(
            rectificative.Issuer.TaxId.Value, rectificative.BillingSource, rectificative.Number.Value, rectificative.IssueDate,
            () => _mapper.MapRectificative(rectificative, rectifiedAmounts, rectifiesSimplifiedInvoice));
    }

    /// <summary>
    /// A rectificative of a simplified invoice is an R5. The rectified record's own type (as stamped)
    /// decides; when it was never registered (issued before VeriFactu), the invoice rule is applied.
    /// </summary>
    private async Task<bool> RectifiesSimplifiedInvoiceAsync(RectificativeInvoice rectificative, RectifiedInvoiceAmounts rectifiedAmounts, CancellationToken cancellationToken)
    {
        VeriFactuSubmissionEntity? rectifiedSubmission = await _store.GetSubmissionAsync(
            rectificative.BillingSource, rectificative.OriginalInvoiceNumber.Value, cancellationToken);
        bool hasKnownType = !string.IsNullOrWhiteSpace(rectifiedSubmission?.InvoiceType);

        return hasKnownType
            ? rectifiedSubmission!.InvoiceType is nameof(TipoFactura.F2) or nameof(TipoFactura.R5)
            : InvoiceToVeriFactuMapper.IsLikelyRectifyingSimplifiedInvoice(rectificative, rectifiedAmounts);
    }

    private async Task<FiscalStampResult> StampDocumentAsync(FiscalDocument document, CancellationToken cancellationToken)
    {
        string fechaExpedicion = document.IssueDate.ToString("dd-MM-yyyy");

        string huella = await _store.StampAsync(document.IssuerNif, document.BillingSource, document.Number, document.Number, fechaExpedicion, previous =>
        {
            lock (VeriFactuSettingsConfigurator.SyncRoot)
            {
                VeriFactuSettingsConfigurator.Apply(_options);
                return ComputeStamp(document, previous);
            }
        }, cancellationToken);

        // The QR only depends on NIF/number/date/total, not on the chain, so it is rebuilt here —
        // also covering the idempotent re-stamp path, where the store skips the compute callback.
        string qrUrl;
        lock (VeriFactuSettingsConfigurator.SyncRoot)
        {
            VeriFactuSettingsConfigurator.Apply(_options);
            qrUrl = document.BuildLibraryInvoice().GetRegistroAlta().GetUrlValidate();
        }

        return new FiscalStampResult(huella, qrUrl);
    }

    private static StampComputation ComputeStamp(FiscalDocument document, ChainLink previous)
    {
        RegistroAlta registro = document.BuildLibraryInvoice().GetRegistroAlta();
        registro.Encadenamiento = VeriFactuRecordTransport.BuildChainLink(
            document.IssuerNif, previous.NumSerie, previous.FechaExpedicion, previous.Huella);

        string fechaHoraGen = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz");
        registro.FechaHoraHusoGenRegistro = fechaHoraGen;
        registro.Huella = registro.GetHashOutput();

        return new StampComputation(registro.Huella, fechaHoraGen, registro.TipoFactura.ToString());
    }

    private Task<FiscalSubmissionOutcome> SubmitDocumentAsync(FiscalDocument document, CancellationToken cancellationToken) =>
        _store.RunExclusiveOnChainAsync(
            document.IssuerNif, document.BillingSource,
            () => SubmitHoldingChainLockAsync(document, cancellationToken),
            cancellationToken);

    private async Task<FiscalSubmissionOutcome> SubmitHoldingChainLockAsync(FiscalDocument document, CancellationToken cancellationToken)
    {
        VeriFactuSubmissionEntity submission = await _store.GetSubmissionAsync(document.BillingSource, document.Number, cancellationToken)
            ?? throw new InvalidOperationException(
                $"No stamped submission found for '{document.Number}' — StampAsync must run before SubmitAsync.");

        // Idempotent: a redelivered message (or a drain that already covered this record) must not
        // re-send a record the AEAT has already answered.
        FiscalSubmissionOutcome outcome = new FiscalSubmissionOutcome(
            (FiscalSubmissionState)submission.State, submission.Csv, submission.Huella,
            submission.ErrorCode, submission.ErrorDescription);

        if (submission.State == (int)FiscalSubmissionState.Pending)
        {
            outcome = SendStampedRecord(document, submission);
            submission.State = (int)outcome.State;
            submission.Csv = outcome.Csv ?? submission.Csv; // a duplicate (3000) answer carries no CSV: keep the original
            submission.ErrorCode = outcome.ErrorCode;
            submission.ErrorDescription = outcome.ErrorDescription;
            submission.SentUtc = DateTimeOffset.UtcNow;
            submission.RetryCount++;
            await _store.UpdateSubmissionAsync(submission, cancellationToken);
        }

        return outcome;
    }

    /// <summary>
    /// Registers the SAME huella we stamped and printed on the QR: the record is rebuilt with OUR stored
    /// chain link and timestamp. The entry is built AFTER applying the settings, because its RegistroAlta
    /// takes the SistemaInformatico from Settings.Current.
    /// </summary>
    private FiscalSubmissionOutcome SendStampedRecord(FiscalDocument document, VeriFactuSubmissionEntity submission)
    {
        try
        {
            Envelope responseEnvelope;
            lock (VeriFactuSettingsConfigurator.SyncRoot)
            {
                VeriFactuSettingsConfigurator.Apply(_options);
                VfInvoiceEntry entry = new VfInvoiceEntry(document.BuildLibraryInvoice());
                VeriFactuRecordTransport.ApplyStoredChain((RegistroAlta)entry.Registro, document.IssuerNif, submission);
                X509Certificate2 certificate = Wsd.GetCertificate();
                responseEnvelope = VeriFactuRecordTransport.Send(entry, certificate);
            }

            return VeriFactuResponseInterpreter.Interpret(responseEnvelope, submission.Huella);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VeriFactu submission failed for {InvoiceNumber}.", document.Number);
            throw; // transient — let the worker retry
        }
    }
}
