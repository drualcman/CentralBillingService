namespace CentralBillingService.Domain.DTOs;

public sealed class RectifyInvoice
{
    /// <summary>La factura original, ahora en estado Rectified</summary>
    public Invoice UpdatedOriginal { get; }

    /// <summary>La nueva factura rectificativa, en estado Issued</summary>
    public RectificativeInvoice Rectificative { get; }

    public RectifyInvoice(Invoice updatedOriginal, RectificativeInvoice rectificative)
    {
        UpdatedOriginal = updatedOriginal;
        Rectificative = rectificative;
    }
}
