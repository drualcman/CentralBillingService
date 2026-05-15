namespace CentralBillingService.Domain.ValueObjects;

/// <summary>
/// Tipo impositivo aplicado a una línea de factura.
/// En España los tipos habituales son 0%, 4% (superreducido),
/// 10% (reducido) y 21% (general).
/// Clase liviana — no necesita semántica de record.
/// </summary>
public sealed class TaxRate
{
    /// <summary>Porcentaje como entero: 21 significa 21%</summary>
    public int Percentage { get; }

    /// <summary>Factor multiplicador listo para operar: 0.21 para 21%</summary>
    public decimal Factor => Percentage / 100m;

    private TaxRate(int percentage) => Percentage = percentage;

    // Tipos españoles predefinidos
    public static readonly TaxRate Zero = new(0);
    public static readonly TaxRate SuperReduced = new(4);
    public static readonly TaxRate Reduced = new(10);
    public static readonly TaxRate General = new(21);

    public static TaxRate Of(int percentage)
    {
        if (percentage < 0 || percentage > 100)
            throw new DomainException($"Tipo impositivo inválido: {percentage}%. Debe estar entre 0 y 100.");

        return percentage switch
        {
            0 => Zero,
            4 => SuperReduced,
            10 => Reduced,
            21 => General,
            _ => new TaxRate(percentage) // permite tipos personalizados si fuera necesario
        };
    }

    /// <summary>Calcula el importe de impuesto sobre una base imponible</summary>
    public Money CalculateTaxOn(Money taxableBase) =>
        taxableBase.Multiply(Factor);

    /// <summary>Calcula la base imponible + impuesto</summary>
    public Money ApplyTo(Money taxableBase) =>
        taxableBase.Add(CalculateTaxOn(taxableBase));

    public override bool Equals(object? obj) =>
        obj is TaxRate other && Percentage == other.Percentage;

    public override int GetHashCode() => Percentage.GetHashCode();

    public override string ToString() => $"{Percentage}%";
}
