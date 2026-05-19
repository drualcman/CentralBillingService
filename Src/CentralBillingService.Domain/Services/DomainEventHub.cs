namespace CentralBillingService.Domain.Services;

internal sealed class DomainEventHub<EventType>(IEnumerable<IDomainEventHandler<EventType>> eventHandlers, ILogger<DomainEventHub<EventType>> logger) :
    IDomainEventHub<EventType> where EventType : IDomainEvent
{
    public async ValueTask Raise(EventType eventInstance, CancellationToken cancellationToken)
    {
        List<Task> tasks = new List<Task>();
        foreach (var handler in eventHandlers)
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await handler.Handle(eventInstance, cancellationToken);
                }
                catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
                {
                    // Cancelación explícita (usuario/proceso canceló)
                    logger.LogInformation("Operación cancelada explícitamente: {Message}", ex.Message);
                }
                catch (OperationCanceledException ex)
                {
                    // Timeout u otra cancelación interna
                    logger.LogInformation("Timeout o cancelación interna: {Message}", ex.Message);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, JsonSerializer.Serialize(eventInstance));
                }
            }));
        }
        await Task.WhenAll(tasks);
    }
}
