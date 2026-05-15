namespace CentralBillingService.Domain.ValueObjects;

/// <summary>
/// Representa un importe monetario: cantidad + divisa.
/// Inmutable. Aritmética solo entre importes de la misma divisa.
/// Usamos decimal para precisión financiera — nunca double.
/// </summary>
public sealed class Money
{
    public decimal Amount { get; }
    public Currency Currency { get; }

    private Money(decimal amount, Currency currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Of(decimal amount, Currency currency) =>
        new Money(Round(amount, currency), currency);

    public static Money Of(decimal amount, string currencyCode) =>
        Of(amount, Currency.From(currencyCode));

    public static Money Zero(Currency currency) => new(0m, currency);

    // --- Operaciones aritméticas ---

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Round(Amount + other.Amount, Currency), Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        var result = Amount - other.Amount;
        return new Money(Round(result, Currency), Currency);
    }

    public Money Multiply(decimal factor) =>
        new Money(Round(Amount * factor, Currency), Currency);

    public Money Multiply(int quantity) =>
        Multiply((decimal)quantity);

    // --- Comparaciones ---

    public bool IsZero => Amount == 0m;

    public bool IsGreaterThan(Money other)
    {
        EnsureSameCurrency(other);
        return Amount > other.Amount;
    }

    // --- Formato ---

    /// <summary>Muestra el importe con símbolo: "€ 99,00" o "$ 99.00"</summary>
    public string Format() =>
        $"{Currency.Symbol} {Amount.ToString($"F{Currency.DecimalPlaces}")}";

    public override string ToString() => $"{Amount.ToString($"F{Currency.DecimalPlaces}")} {Currency.Code}";

    // --- Privados ---

    private static decimal Round(decimal amount, Currency currency) =>
        Math.Round(amount, currency.DecimalPlaces, MidpointRounding.AwayFromZero);

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
            throw new DomainException(
                $"Transactions cannot be carried out in different currencies: {Currency.Code} vs {other.Currency.Code}");
    }

    public override bool Equals(object? obj) =>
        obj is Money other && Amount == other.Amount && Currency == other.Currency;

    public override int GetHashCode() => HashCode.Combine(Amount, Currency.Code);
}
