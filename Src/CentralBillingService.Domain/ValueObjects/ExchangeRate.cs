namespace CentralBillingService.Domain.ValueObjects;

/// <summary>
/// Representa el tipo de cambio aplicado en un momento concreto.
/// Es una fotografía inmutable: qué tasa, de qué fuente, en qué instante.
/// Este valor se almacena con la factura para garantizar la inmutabilidad
/// del cálculo aunque la tasa de mercado cambie después.
/// </summary>
public sealed class ExchangeRate
{
    /// <summary>Divisa de origen (ej: USD)</summary>
    public Currency From { get; }

    /// <summary>Divisa de destino — siempre EUR en nuestro sistema</summary>
    public Currency To { get; }

    /// <summary>
    /// Tasa de cambio: cuántas unidades de <see cref="To"/> equivalen a 1 unidad de <see cref="From"/>.
    /// Ej: si 1 USD = 0.92 EUR, la tasa es 0.92.
    /// </summary>
    public decimal Rate { get; }

    /// <summary>Momento exacto en que se obtuvo la tasa — siempre UTC</summary>
    public DateTimeOffset FetchedAt { get; }

    private ExchangeRate(
        Currency from,
        Currency to,
        decimal rate,
        DateTimeOffset fetchedAt)
    {
        From = from;
        To = to;
        Rate = rate;
        FetchedAt = fetchedAt;
    }

    public static ExchangeRate Create(
        Currency from,
        Currency to,
        decimal rate,
        DateTimeOffset fetchedAt)
    {
        if (rate <= 0)
            throw new DomainException($"The exchange rate must be positive. Received: {rate}");

        return new ExchangeRate(from, to, rate, fetchedAt);
    }

    /// <summary>
    /// Crea un tipo de cambio identidad para EUR→EUR (tasa 1:1).
    /// Útil cuando el origen ya es EUR y no hay conversión real.
    /// </summary>
    public static ExchangeRate Identity(DateTimeOffset fetchedAt) =>
        new(Currency.EUR, Currency.EUR, 1m, fetchedAt);

    public bool IsIdentity => From == Currency.EUR && To == Currency.EUR && Rate == 1m;

    /// <summary>
    /// Aplica el tipo de cambio a un importe en la divisa de origen
    /// y devuelve el equivalente en la divisa de destino.
    /// </summary>
    public Money Apply(Money originAmount)
    {
        if (originAmount.Currency != From)
            throw new DomainException(
                $"This exchange rate converts {From.Code}→{To.Code}, " +
                $"but the amount is in {originAmount.Currency.Code}.");

        var converted = originAmount.Amount * Rate;
        return Money.Of(converted, To);
    }

    public override string ToString() =>
        $"1 {From.Code} = {Rate} {To.Code} [{FetchedAt:u}]";
}
