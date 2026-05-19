namespace CentralBillingService.Domain.Interfaces;

public interface IDomainEventHandler<EventType> where EventType : IDomainEvent
{
    Task Handle(EventType data, CancellationToken cancellationToken);
}
