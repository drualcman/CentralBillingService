namespace CentralBillingService.Domain.Exceptions;

public class NotFoundException : DomainException, ISerializable
{
    public NotFoundException()
    {
    }

    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
