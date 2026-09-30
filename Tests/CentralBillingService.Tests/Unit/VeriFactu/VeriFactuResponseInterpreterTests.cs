using CentralBillingService.Application.Models;
using CentralBillingService.VeriFactu;
using VeriFactu.Xml.Factu.Fault;
using VeriFactu.Xml.Factu.Respuesta;
using VeriFactu.Xml.Soap;

namespace CentralBillingService.Tests.Unit.VeriFactu;

public class VeriFactuResponseInterpreterTests
{
    private const string StampedHuella = "1E5751E5230FC99C8DEBABBE3D5F7373087F499D78F3DCA3B618B83220FA5D60";

    [Fact]
    public void Accepted_record_returns_accepted_with_csv()
    {
        Envelope envelope = BuildAnswerEnvelope("Correcto", "Correcto", "A-CSV-123", null, null);

        FiscalSubmissionOutcome outcome = VeriFactuResponseInterpreter.Interpret(envelope, StampedHuella);

        Assert.Equal(FiscalSubmissionState.Accepted, outcome.State);
        Assert.Equal("A-CSV-123", outcome.Csv);
    }

    [Fact]
    public void Record_accepted_with_errors_returns_accepted_with_errors_and_the_aeat_code()
    {
        Envelope envelope = BuildAnswerEnvelope("ParcialmenteCorrecto", "AceptadoConErrores", "A-CSV-456", "2000", "Aviso");

        FiscalSubmissionOutcome outcome = VeriFactuResponseInterpreter.Interpret(envelope, StampedHuella);

        Assert.Equal(FiscalSubmissionState.AcceptedWithErrors, outcome.State);
        Assert.Equal("2000", outcome.ErrorCode);
    }

    [Fact]
    public void Incorrect_record_returns_rejected_with_the_aeat_error()
    {
        Envelope envelope = BuildAnswerEnvelope("Incorrecto", "Incorrecto", null, "4102", "El XML no cumple el esquema");

        FiscalSubmissionOutcome outcome = VeriFactuResponseInterpreter.Interpret(envelope, StampedHuella);

        Assert.Equal(FiscalSubmissionState.Rejected, outcome.State);
        Assert.Equal("4102", outcome.ErrorCode);
        Assert.Equal("El XML no cumple el esquema", outcome.ErrorDescription);
    }

    [Fact]
    public void Duplicate_of_an_already_accepted_record_is_treated_as_accepted()
    {
        Envelope envelope = BuildAnswerEnvelope("Incorrecto", "Incorrecto", null, "3000", "Registro de facturación duplicado.");
        RespuestaRegFactuSistemaFacturacion answer = (RespuestaRegFactuSistemaFacturacion)envelope.Body.Registro;
        answer.RespuestaLinea[0].RegistroDuplicado = new RegistroDuplicado { EstadoRegistroDuplicado = "Correcta" };

        FiscalSubmissionOutcome outcome = VeriFactuResponseInterpreter.Interpret(envelope, StampedHuella);

        Assert.Equal(FiscalSubmissionState.Accepted, outcome.State);
    }

    [Fact]
    public void Soap_fault_returns_rejected_with_fault_code_and_text()
    {
        Envelope envelope = new Envelope
        {
            Body = new Body { Registro = new Fault { faultcode = "env:Client", faultstring = "Certificado no válido" } }
        };

        FiscalSubmissionOutcome outcome = VeriFactuResponseInterpreter.Interpret(envelope, StampedHuella);

        Assert.Equal(FiscalSubmissionState.Rejected, outcome.State);
        Assert.Equal("env:Client", outcome.ErrorCode);
        Assert.Equal("Certificado no válido", outcome.ErrorDescription);
    }

    private static Envelope BuildAnswerEnvelope(
        string sendState, string recordState, string? csv, string? errorCode, string? errorDescription)
    {
        RespuestaRegFactuSistemaFacturacion answer = new RespuestaRegFactuSistemaFacturacion
        {
            EstadoEnvio = sendState,
            CSV = csv,
            RespuestaLinea = new List<RespuestaLinea>
            {
                new RespuestaLinea
                {
                    EstadoRegistro = recordState,
                    CodigoErrorRegistro = errorCode,
                    DescripcionErrorRegistro = errorDescription,
                }
            }
        };

        return new Envelope { Body = new Body { Registro = answer } };
    }
}
