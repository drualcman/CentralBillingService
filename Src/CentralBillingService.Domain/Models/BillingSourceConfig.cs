namespace CentralBillingService.Domain.Models;

/// <summary>
/// Configuración de un origen de facturación concreto.
/// </summary>
public sealed class BillingSourceConfig
{
    /// <summary>Identificador del origen: "web-fotos", "web-cripto", etc.</summary>
    public string Secret { get; set; } = "";

    /// <summary>Identificador del origen: "web-fotos", "web-cripto", etc.</summary>
    public string BillingSource { get; set; } = "";

    /// <summary>
    /// Datos del emisor tal como aparecerán en las facturas de este origen.
    /// Mismo NIF siempre, pero puede variar el nombre comercial, web, teléfono, etc.
    /// </summary>
    public IssuerConfig Issuer { get; set; } = new();

    /// <summary>How invoice numbers are reserved for this source. Defaults to local DB.</summary>
    public NumberProviderConfig NumberProvider { get; set; } = new();

    /// <summary>If set, the result of queue-triggered invoice creation is published here.</summary>
    public ResultQueueConfig? ResultQueue { get; set; }

    /// <summary>If set, the result of queue-triggered invoice creation is POSTed to this URL.</summary>
    public CallbackConfig? Callback { get; set; }
}
