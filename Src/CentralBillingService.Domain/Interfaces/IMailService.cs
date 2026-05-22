namespace CentralBillingService.Domain.Interfaces;

public interface IMailService
{
    ValueTask Send(Email email, CancellationToken token);
    ValueTask Send(string name, string email, string subject, string message, CancellationToken token);
}