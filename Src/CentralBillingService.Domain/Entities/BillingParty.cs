namespace CentralBillingService.Domain.Entities;

/// <summary>
/// Representa a una parte en la factura: puede ser el emisor o el receptor.
///
/// En nuestro sistema, el emisor siempre es el autónomo (tú),
/// pero cada web o proyecto puede presentarse con datos distintos:
/// distinto nombre comercial, logo, serie de factura, etc.
///
/// El receptor es el cliente al que va dirigida la factura.
///
/// Esta entidad no tiene identidad propia en el dominio — es un value
/// snapshot que queda grabado en la factura en el momento de emisión.
/// Si el cliente cambia de dirección, las facturas antiguas no cambian.
/// </summary>
public sealed class BillingParty
{
    /// <summary>Nombre fiscal completo (razón social o nombre + apellidos)</summary>
    public string LegalName { get; }

    /// <summary>Nombre comercial o de marca — opcional, para mostrar en cabecera</summary>
    public string? TradeName { get; }

    public TaxId TaxId { get; }
    public PostalAddress Address { get; }

    /// <summary>Email de contacto fiscal</summary>
    public string Email { get; }

    /// <summary>Teléfono — opcional, requerido por algunos formatos de factura</summary>
    public string? Phone { get; }

    /// <summary>Web — opcional, se muestra en el pie del documento</summary>
    public string? Website { get; }

    /// <summary>
    /// Identificador del destinatario en el sistema emisor (BusinessSource).
    /// Solo relevante para el receptor. Permite filtrar facturas por cliente
    /// sin necesidad de conocer su NIF.
    /// </summary>
    public string? ExternalId { get; }

    private BillingParty(
        string legalName,
        string? tradeName,
        TaxId taxId,
        PostalAddress address,
        string email,
        string? phone,
        string? website,
        string? externalId)
    {
        LegalName = legalName;
        TradeName = tradeName;
        TaxId = taxId;
        Address = address;
        Email = email;
        Phone = phone;
        Website = website;
        ExternalId = externalId;
    }

    public static BillingParty Create(
        string legalName,
        TaxId taxId,
        PostalAddress address,
        string email,
        string? tradeName = null,
        string? phone = null,
        string? website = null,
        string? externalId = null)
    {
        if (string.IsNullOrWhiteSpace(legalName))
            throw new DomainException("El nombre fiscal (LegalName) es obligatorio.");

        return new BillingParty(
            legalName.Trim(),
            tradeName?.Trim(),
            taxId,
            address,
            email.Trim().ToLowerInvariant(),
            phone?.Trim(),
            website?.Trim(),
            externalId?.Trim());
    }

    /// <summary>
    /// El nombre que se muestra en cabecera:
    /// el nombre comercial si existe, el fiscal si no.
    /// </summary>
    public string DisplayName => string.IsNullOrWhiteSpace(TradeName) ? LegalName : TradeName;

    public override string ToString() => $"{DisplayName} ({TaxId})";
}
