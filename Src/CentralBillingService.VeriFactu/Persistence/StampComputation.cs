namespace CentralBillingService.VeriFactu.Persistence;

/// <summary>
/// What the registrar computes for one record while holding the chain lock. Besides the huella,
/// it carries the exact <see cref="FechaHoraGenRegistro"/> used in the hash input so the submission
/// can reproduce the byte-identical RegistroAlta later and register OUR huella at the AEAT, and the
/// record type (F1/F2/R1/R5), itself part of the huella and needed to classify later rectificatives.
/// </summary>
public sealed record StampComputation(string Huella, string FechaHoraGenRegistro, string InvoiceType);
