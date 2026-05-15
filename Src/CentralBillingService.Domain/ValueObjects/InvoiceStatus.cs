namespace CentralBillingService.Domain.ValueObjects;

public enum InvoiceStatus
{
    Draft,      // Creada pero no emitida (se puede cancelar sin traza)
    Issued,     // Emitida y firmada — inmutable
    Rectified,  // Existe una factura rectificativa que la corrige
    Cancelled   // Anulada antes de ser emitida (nunca modifica Issued)
}
