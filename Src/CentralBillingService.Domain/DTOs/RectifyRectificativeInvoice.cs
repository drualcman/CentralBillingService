namespace CentralBillingService.Domain.DTOs;

public sealed class RectifyRectificativeInvoice
{
    /// <summary>La factura rectificativa original, ahora en estado Rectified</summary>
    public RectificativeInvoice UpdatedOriginal { get; }

    /// <summary>La nueva factura rectificativa, en estado Issued</summary>
    public RectificativeInvoice Rectificative { get; }

    public RectifyRectificativeInvoice(RectificativeInvoice updatedOriginal, RectificativeInvoice rectificative)
    {
        UpdatedOriginal = updatedOriginal;
        Rectificative = rectificative;
    }
}
