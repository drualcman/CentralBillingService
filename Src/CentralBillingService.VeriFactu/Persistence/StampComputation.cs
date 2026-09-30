namespace CentralBillingService.VeriFactu.Persistence;

/// <summary>
/// What the registrar computes for one record while holding the chain lock. Besides the huella,
/// it carries the exact <see cref="FechaHoraGenRegistro"/> used in the hash input so the submission
/// can reproduce the byte-identical RegistroAlta later and register OUR huella at the AEAT.
/// </summary>
public sealed record StampComputation(string Huella, string FechaHoraGenRegistro);
