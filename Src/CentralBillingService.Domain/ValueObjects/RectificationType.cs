namespace CentralBillingService.Domain.ValueObjects;

public enum RectificationType
{
    /// <summary>
    /// Se emite con los importes completos de la factura original en negativo,
    /// anulándola totalmente, más una nueva factura correcta si procede.
    /// Es el método más común y el que mejor entienden los programas de contabilidad.
    /// </summary>
    Substitution,

    /// <summary>
    /// Se emite solo por la diferencia entre lo facturado y lo correcto.
    /// Más complejo de gestionar pero válido fiscalmente.
    /// </summary>
    Difference
}
