namespace CentralBillingService.Domain.ValueObjects;

/// <summary>
/// Número de factura: serie + número correlativo dentro del año.
/// Formato: {Serie}{Año}-{Número:D4}
/// Ejemplos: F2026-0001, REC2026-0001 (rectificativa), FOTO2026-0012
///
/// La serie permite distinguir facturas por origen o tipo.
/// El número correlativo es global dentro de la serie+año — lo genera
/// el secuenciador, nunca el dominio directamente.
/// </summary>
public sealed class InvoiceNumber
{
    public string Serie { get; }
    public int Year { get; }
    public int Number { get; }

    private InvoiceNumber(string serie, int year, int number)
    {
        Serie = serie;
        Year = year;
        Number = number;
    }

    public static InvoiceNumber Create(string serie, int year, int number)
    {
        if (string.IsNullOrWhiteSpace(serie))
            throw new DomainException("The invoice number series cannot be empty.");

        var normalizedSerie = serie.Trim().ToUpperInvariant();

        if (year < 2026 || year > DateTime.Today.Year)
            throw new DomainException($"Invalid invoice year: {year}.");

        if (number <= 0)
            throw new DomainException($"The sequential number must be positive. Received: {number}.");

        return new InvoiceNumber(normalizedSerie, year, number);
    }

    /// <summary>
    /// Parses a formatted invoice number string back into an InvoiceNumber.
    /// Expected format: {Serie}{Year}-{Number} e.g. "FOTO2026-0003"
    /// Used when rehydrating from persistence.
    /// </summary>
    public static InvoiceNumber CreateFromFormatted(string formatted)
    {
        if (string.IsNullOrWhiteSpace(formatted))
            throw new DomainException("Formatted invoice number cannot be empty.");

        var dashIndex = formatted.IndexOf('-');
        if (dashIndex < 5)
            throw new DomainException($"Invalid formatted invoice number: '{formatted}'.");

        var prefix = formatted[..dashIndex];        // e.g. "FOTO2026"
        var numStr = formatted[(dashIndex + 1)..];  // e.g. "0003"

        if (prefix.Length < 5)
            throw new DomainException($"Invalid formatted invoice number: '{formatted}'.");

        var yearStr = prefix[^4..];    // last 4 chars of prefix
        var serie = prefix[..^4];    // everything before the year

        if (!int.TryParse(yearStr, out var year) || !int.TryParse(numStr, out var number))
            throw new DomainException($"Invalid formatted invoice number: '{formatted}'.");

        return Create(serie, year, number);
    }

    /// <summary>
    /// Canonical representation: what appears on the invoice and in VeriFactu.
    /// Ej: F2026-0001
    /// </summary>
    public string Value => $"{Serie}{Year}-{Number:D4}";

    public override string ToString() => Value;

    public override bool Equals(object? obj) =>
        obj is InvoiceNumber other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();
}
