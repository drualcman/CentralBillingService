namespace CentralBillingService.Domain.Exceptions;

/// <summary>
/// Excepción base para violaciones de reglas de negocio del dominio.
/// No es un error de sistema — es una regla que no se cumple.
/// Las capas superiores la capturan para devolver 400/422, no 500.
/// </summary>
public class DomainException : Exception
{
    public DomainException() : base() { }
    public DomainException(string message) : base(message) { }

    public DomainException(string message, Exception inner) : base(message, inner) { }
}
