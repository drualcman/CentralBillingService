namespace CentralBillingService.Domain.Interfaces;

public interface IDomainEventHub<EventType> where EventType : IDomainEvent
{
    ValueTask Raise(EventType eventInstance, CancellationToken cancellationToken);
}
