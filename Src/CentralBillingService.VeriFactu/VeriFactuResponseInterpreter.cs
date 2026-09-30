using VeriFactu.Xml.Factu.Fault;
using VeriFactu.Xml.Factu.Respuesta;
using VeriFactu.Xml.Soap;

namespace CentralBillingService.VeriFactu;

/// <summary>
/// Translates the AEAT SOAP answer to a RegFactuSistemaFacturacion submission into a
/// <see cref="FiscalSubmissionOutcome"/>, using only the library's public response types.
/// The per-record state (EstadoRegistro of our line) decides the outcome; a SOAP fault or an
/// unexpected body is a rejection carrying the AEAT code and description.
/// </summary>
public static class VeriFactuResponseInterpreter
{
    private const string RecordAccepted = "Correcto";
    private const string RecordAcceptedWithErrors = "AceptadoConErrores";
    private const string DuplicateRecordErrorCode = "3000";
    private const string DuplicateOriginalAccepted = "Correcta";
    private const string DuplicateOriginalAcceptedWithErrors = "AceptadaConErrores";

    /// <summary>
    /// AEAT 3000 "Registro de facturación duplicado": the record was ALREADY registered (e.g. a retry after
    /// the answer was lost). Idempotent — the outcome is the state of the original registration.
    /// </summary>
    private static bool IsDuplicateOfAlreadyRegisteredRecord(RespuestaLinea? recordAnswer) =>
        string.Equals(recordAnswer?.CodigoErrorRegistro, DuplicateRecordErrorCode, StringComparison.Ordinal)
        && recordAnswer?.RegistroDuplicado is not null;

    private static FiscalSubmissionState GetStateOfAlreadyRegisteredRecord(string? originalState)
    {
        FiscalSubmissionState state = FiscalSubmissionState.Rejected;
        if (string.Equals(originalState, DuplicateOriginalAccepted, StringComparison.OrdinalIgnoreCase))
        {
            state = FiscalSubmissionState.Accepted;
        }
        else if (string.Equals(originalState, DuplicateOriginalAcceptedWithErrors, StringComparison.OrdinalIgnoreCase))
        {
            state = FiscalSubmissionState.AcceptedWithErrors;
        }

        return state;
    }

    public static FiscalSubmissionOutcome Interpret(Envelope responseEnvelope, string huella)
    {
        object? responseBody = responseEnvelope?.Body?.Registro;
        FiscalSubmissionOutcome outcome;

        if (responseBody is RespuestaRegFactuSistemaFacturacion aeatAnswer)
        {
            outcome = InterpretAnswer(aeatAnswer, huella);
        }
        else if (responseBody is Fault soapFault)
        {
            outcome = new FiscalSubmissionOutcome(
                FiscalSubmissionState.Rejected, null, huella, soapFault.faultcode, soapFault.faultstring);
        }
        else
        {
            outcome = new FiscalSubmissionOutcome(
                FiscalSubmissionState.Rejected, null, huella, null, "Unexpected AEAT response body.");
        }

        return outcome;
    }

    private static FiscalSubmissionOutcome InterpretAnswer(RespuestaRegFactuSistemaFacturacion aeatAnswer, string huella)
    {
        RespuestaLinea? recordAnswer = aeatAnswer.RespuestaLinea?.FirstOrDefault();
        string? recordState = recordAnswer?.EstadoRegistro ?? aeatAnswer.EstadoEnvio;

        FiscalSubmissionState state = FiscalSubmissionState.Rejected;
        if (string.Equals(recordState, RecordAccepted, StringComparison.OrdinalIgnoreCase))
        {
            state = FiscalSubmissionState.Accepted;
        }
        else if (string.Equals(recordState, RecordAcceptedWithErrors, StringComparison.OrdinalIgnoreCase))
        {
            state = FiscalSubmissionState.AcceptedWithErrors;
        }
        else if (IsDuplicateOfAlreadyRegisteredRecord(recordAnswer))
        {
            state = GetStateOfAlreadyRegisteredRecord(recordAnswer!.RegistroDuplicado.EstadoRegistroDuplicado);
        }

        return new FiscalSubmissionOutcome(
            state,
            aeatAnswer.CSV,
            huella,
            recordAnswer?.CodigoErrorRegistro,
            recordAnswer?.DescripcionErrorRegistro);
    }
}
