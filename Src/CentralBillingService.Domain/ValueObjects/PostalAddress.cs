namespace CentralBillingService.Domain.ValueObjects;

/// <summary>
/// Dirección postal estructurada.
/// Se almacena completa en la factura — es una fotografía del momento
/// de emisión, aunque el cliente cambie de dirección después.
/// </summary>
public sealed class PostalAddress
{
    public string Line1 { get; }          // Calle, número, piso
    public string? Line2 { get; }         // Complemento (urbanización, edificio...)
    public string City { get; }
    public string? Province { get; }      // Provincia / estado
    public string PostalCode { get; }
    public string CountryCode { get; }    // ISO 3166-1 alpha-2

    private PostalAddress(
        string line1,
        string? line2,
        string city,
        string? province,
        string postalCode,
        string countryCode)
    {
        Line1 = line1;
        Line2 = line2;
        City = city;
        Province = province;
        PostalCode = postalCode;
        CountryCode = countryCode;
    }

    public static PostalAddress Create(
        string line1,
        string city,
        string postalCode,
        string countryCode,
        string? line2 = null,
        string? province = null)
    {
        return new PostalAddress(
            line1.Trim(),
            line2?.Trim(),
            city.Trim(),
            province?.Trim(),
            postalCode.Trim(),
            countryCode.Trim().ToUpperInvariant());
    }

    /// <summary>Formato de una sola línea para mostrar en documentos</summary>
    public string ToSingleLine()
    {
        var parts = new List<string> { Line1 };
        if (!string.IsNullOrWhiteSpace(Line2))
            parts.Add(Line2);
        parts.Add($"{PostalCode} {City}");
        if (!string.IsNullOrWhiteSpace(Province))
            parts.Add(Province);
        parts.Add(CountryCode);
        return string.Join(", ", parts);
    }

    public override string ToString() => ToSingleLine();
}
