namespace CentralBillingService.Domain.ValueObjects;

/// <summary>
/// Número de factura: serie + año + prefijo cliente (opcional) + número correlativo + sufijo cliente (opcional).
/// Formato: {Serie}{Año}{ClientNumberPrefix}-{Número:D4}{ClientNumberSuffix}
/// Ejemplos:
///   F2026-0001                          (sin prefix/suffix)
///   GLUONSERGI-20260501-0001            (serie con guion, prefix=0501)
///   GLUONSERGI-20260501-0001A           (serie con guion, prefix=0501, suffix=A)
///
/// ClientNumberPrefix y ClientNumberSuffix son opcionales y los envía el caller.
/// El año y el número correlativo los genera siempre el sistema.
/// </summary>
public sealed class InvoiceNumber
{
    public string Serie { get; }
    public int Year { get; }
    public int Number { get; }
    public string? ClientNumberPrefix { get; }
    public string? ClientNumberSuffix { get; }

    // Only set when created via CreateFromFormatted — avoids re-parsing ambiguous strings.
    private readonly string? _rawFormattedValue;

    private InvoiceNumber(string serie, int year, int number,
        string? clientNumberPrefix, string? clientNumberSuffix)
    {
        Serie = serie;
        Year = year;
        Number = number;
        ClientNumberPrefix = clientNumberPrefix;
        ClientNumberSuffix = clientNumberSuffix;
    }

    private InvoiceNumber(string rawFormattedValue)
    {
        _rawFormattedValue = rawFormattedValue;
        Serie = string.Empty;
        Year = 0;
        Number = 0;
    }

    public static InvoiceNumber Create(string serie, int year, int number,
        string? clientNumberPrefix = null, string? clientNumberSuffix = null)
    {
        if (string.IsNullOrWhiteSpace(serie))
            throw new DomainException("The invoice number series cannot be empty.");

        var normalizedSerie = serie.Trim().ToUpperInvariant();

        if (year < 2026 || year > DateTime.Today.Year)
            throw new DomainException($"Invalid invoice year: {year}.");

        if (number <= 0)
            throw new DomainException($"The sequential number must be positive. Received: {number}.");

        var normalizedPrefix = string.IsNullOrWhiteSpace(clientNumberPrefix)
            ? null : clientNumberPrefix.Trim();
        var normalizedSuffix = string.IsNullOrWhiteSpace(clientNumberSuffix)
            ? null : clientNumberSuffix.Trim();

        return new InvoiceNumber(normalizedSerie, year, number, normalizedPrefix, normalizedSuffix);
    }

    /// <summary>
    /// Wraps a formatted invoice number string for use as a reference (e.g. RectifiedByNumber).
    /// Only .Value is reliable on the result — Serie/Year/Number are not parsed.
    /// Used when rehydrating reference numbers from persistence.
    /// </summary>
    public static InvoiceNumber CreateFromFormatted(string formatted)
    {
        if (string.IsNullOrWhiteSpace(formatted))
            throw new DomainException("Formatted invoice number cannot be empty.");

        return new InvoiceNumber(formatted.Trim());
    }

    /// <summary>
    /// Canonical representation: what appears on the invoice and in VeriFactu.
    /// Ej: F2026-0001, GLUONSERGI-20260501-0001A
    /// </summary>
    public string Value => _rawFormattedValue
        ?? $"{Serie}{Year}{ClientNumberPrefix ?? string.Empty}-{Number:D4}{ClientNumberSuffix ?? string.Empty}";

    public override string ToString() => Value;

    public override bool Equals(object? obj) =>
        obj is InvoiceNumber other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();
}
