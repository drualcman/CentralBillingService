namespace CentralBillingService.Domain.Exceptions;

/// <summary>
/// Se lanza cuando el proveedor de tipos de cambio no puede
/// devolver una tasa en este momento (servicio caído, divisa no soportada,
/// timeout de red, etc.).
///
/// Es una excepción de infraestructura con semántica de dominio:
/// el dominio la conoce porque afecta directamente a si se puede
/// crear una factura o no.
/// </summary>
public sealed class ExchangeRateUnavailableException : DomainException
{
    public Currency From { get; }
    public Currency To { get; }

    public ExchangeRateUnavailableException(Currency from, Currency to)
        : base($"No se pudo obtener el tipo de cambio {from.Code}→{to.Code}.")
    {
        From = from;
        To = to;
    }

    public ExchangeRateUnavailableException(Currency from, Currency to, Exception inner)
        : base($"No se pudo obtener el tipo de cambio {from.Code}→{to.Code}: {inner.Message}", inner)
    {
        From = from;
        To = to;
    }
}
