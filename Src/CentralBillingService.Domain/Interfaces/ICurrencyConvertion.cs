namespace CentralBillingService.Domain.Interfaces;

public interface ICurrencyConvertion
{
    Task<decimal> GetRate(Currency origin, Currency destination);
    Task<Money> ConvertToCurrency(Money origin, Currency destination);
}
