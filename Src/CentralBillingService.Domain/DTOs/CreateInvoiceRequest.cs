namespace CentralBillingService.Domain.DTOs;

/// <summary>
/// Datos de entrada para crear una factura.
/// Este es el contrato que cualquier web origen debe satisfacer
/// al llamar al servicio centralizado.
///
/// Los importes vienen en la divisa del cliente — el servicio
/// se encarga de convertir a EUR y registrar el cambio aplicado.
/// </summary>
public sealed class CreateInvoiceRequest
{
    /// <summary>
    /// Identifica qué web o proyecto genera la factura.
    /// Determina qué emisor (BillingParty) se usa y qué serie de factura.
    /// Ej: "web-fotos", "web-cripto", "web-tv", "web-cms", "proyecto-directo"
    /// </summary>
    public required string Secret { get; init; }

    /// <summary>
    /// Identifica qué web o proyecto genera la factura.
    /// Determina qué emisor (BillingParty) se usa y qué serie de factura.
    /// Ej: "web-fotos", "web-cripto", "web-tv", "web-cms", "proyecto-directo"
    /// </summary>
    public required string BillingSource { get; init; }

    /// <summary>
    /// Serie de factura para este origen.
    /// Ej: "F" para la serie general, "FOTO", "TV", "CRIPTO", "REC" para rectificativas.
    /// </summary>
    public required string Serie { get; init; }

    /// <summary>Datos fiscales del cliente receptor</summary>
    public required RecipientData Recipient { get; init; }

    /// <summary>Líneas de lo que se factura</summary>
    public required IReadOnlyList<InvoiceLineData> Lines { get; init; }

    /// <summary>
    /// Divisa en que el cliente ve los importes.
    /// Si es EUR, no se realiza conversión de divisas.
    /// </summary>
    public required string OriginCurrencyCode { get; init; }

    /// <summary>
    /// Fecha de expedición. Si no se indica, se usa la fecha UTC de hoy.
    /// </summary>
    public DateOnly? IssueDate { get; init; }

    /// <summary>
    /// Fecha de devengo si difiere de la expedición
    /// (ej: período de suscripción facturado).
    /// </summary>
    public DateOnly? ValueDate { get; init; }

    /// <summary>Notas o texto libre que aparece al pie de la factura</summary>
    public string? Notes { get; init; }

    public string? PaymentMethod { get; init; }
    public required string PaymentReference { get; init; }

    public string? TransactionData { get; init; }
}