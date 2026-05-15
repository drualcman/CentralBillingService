namespace CentralBillingService.Domain.Interfaces;

public interface IIso9001
{
    Task Error<T, TData>(string reference, T action, string description, TData data);
    Task Error<T>(string reference, T action, Exception ex);
    Task Error<T>(string reference, T action, string description);
    Task Register<T, TData>(string reference, T action, string description, TData data);
    Task Register<T>(string reference, T action, string description);
}