namespace CentralBillingService.Domain.ValueObjects;

/// <summary>
/// Identificador fiscal: NIF español, CIF, VAT europeo u otros.
/// Almacena el valor tal como viene + el país emisor.
/// La validación de formato es básica — una validación exhaustiva
/// requeriría llamadas externas (VIES para VAT europeo) que no
/// pertenecen al dominio puro.
/// </summary>
public sealed class TaxId
{
    public string Value { get; }

    /// <summary>Código de país ISO 3166-1 alpha-2. Ej: "ES", "US", "PH"</summary>
    public string CountryCode { get; }

    public TaxIdType Type { get; }

    private TaxId(string value, string countryCode, TaxIdType type)
    {
        Value = value;
        CountryCode = countryCode;
        Type = type;
    }

    public static TaxId Create(string value, string countryCode)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("El identificador fiscal no puede estar vacío.");

        if (string.IsNullOrWhiteSpace(countryCode) || countryCode.Trim().Length != 2)
            throw new DomainException("El código de país debe ser ISO 3166-1 alpha-2 (2 letras).");

        var normalizedValue = value.Trim().ToUpperInvariant();
        var normalizedCountry = countryCode.Trim().ToUpperInvariant();

        var type = DetermineType(normalizedValue, normalizedCountry);

        return new TaxId(normalizedValue, normalizedCountry, type);
    }

    /// <summary>Para clientes extranjeros sin obligación de NIF español</summary>
    public static TaxId Foreign(string value, string countryCode) =>
        Create(value, countryCode);

    /// <summary>Cuando el cliente no proporciona NIF (B2C extranjero)</summary>
    public static TaxId NotProvided(string countryCode) =>
        new("NO_ID", countryCode.Trim().ToUpperInvariant(), TaxIdType.NotProvided);

    public bool IsSpanish => CountryCode == "ES";
    public bool IsNotProvided => Type == TaxIdType.NotProvided;

    private static TaxIdType DetermineType(string value, string country)
    {
        if (string.IsNullOrWhiteSpace(value))
            return TaxIdType.NotProvided;
        if (value == "NO_ID")
            return TaxIdType.NotProvided;

        if (country == "ES")
        {
            // NIF persona física: letra + 7 dígitos + letra control
            // CIF persona jurídica: letra + 7 dígitos + dígito/letra control
            // NIE extranjero residente: X/Y/Z + 7 dígitos + letra
            if (value.Length == 9)
            {
                if (char.IsLetter(value[0]) && char.IsLetter(value[8]))
                    return TaxIdType.NIF;
                if (value[0] is 'X' or 'Y' or 'Z')
                    return TaxIdType.NIE;
                if (char.IsLetter(value[0]))
                    return TaxIdType.CIF;
            }
            return TaxIdType.Unknown;
        }

        // VAT europeo: código país (2 letras) + identificador
        if (value.Length > 4 && char.IsLetter(value[0]) && char.IsLetter(value[1]))
            return TaxIdType.EuVat;

        return TaxIdType.Foreign;
    }

    public override string ToString() => $"{Value} ({CountryCode})";

    public override bool Equals(object? obj) =>
        obj is TaxId other && Value == other.Value && CountryCode == other.CountryCode;

    public override int GetHashCode() => HashCode.Combine(Value, CountryCode);
}

public enum TaxIdType
{
    NIF,          // Persona física española
    NIE,          // Extranjero residente en España
    CIF,          // Persona jurídica española
    EuVat,        // VAT europeo
    Foreign,      // Identificador fiscal extranjero genérico
    NotProvided,  // No proporcionado (B2C extranjero)
    Unknown
}
