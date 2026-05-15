namespace CentralBillingService.Domain.Interfaces;

/// <summary>
/// Contrato para obtener el tipo de cambio actual entre dos divisas.
///
/// El dominio solo define qué necesita — la implementación concreta
/// usará tu código existente (Fixer.io, ECB, OpenExchangeRates, etc.)
/// y vivirá en infraestructura.
///
/// Importante: el proveedor debe devolver siempre un <see cref="ExchangeRate"/>
/// con la fuente y el timestamp exacto de obtención, porque ese dato
/// queda grabado de forma inmutable en la factura.
/// </summary>
public interface IExchangeRateProvider
{
    /// <summary>
    /// Obtiene el tipo de cambio para convertir <paramref name="from"/>
    /// a <paramref name="to"/> en este momento.
    /// </summary>
    /// <param name="from">Divisa de origen (ej: USD, PHP, AUD)</param>
    /// <param name="to">Divisa de destino — en nuestro sistema siempre EUR</param>
    /// <param name="cancellationToken"></param>
    /// <returns>
    /// El tipo de cambio con tasa, fuente y timestamp.
    /// Nunca devuelve null — lanza excepción si no puede obtenerlo.
    /// </returns>
    /// <exception cref="ExchangeRateUnavailableException">
    /// Si el proveedor no puede obtener el tipo de cambio en este momento.
    /// </exception>
    Task<ExchangeRate> GetRateAsync(
        Currency from,
        Currency to,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica si el proveedor soporta la conversión entre estas dos divisas.
    /// </summary>
    bool Supports(Currency from, Currency to);
}
