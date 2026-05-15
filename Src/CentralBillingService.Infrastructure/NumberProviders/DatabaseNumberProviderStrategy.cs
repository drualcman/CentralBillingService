namespace CentralBillingService.Infrastructure.NumberProviders;

public sealed class DatabaseNumberProviderStrategy : IInvoiceNumberProviderStrategy
{
    private readonly IInvoiceWriteContext _write;

    public DatabaseNumberProviderStrategy(IInvoiceWriteContext write) => _write = write;

    public string ProviderType => "Database";

    public IInvoiceNumberProvider Create(NumberProviderConfig config) =>
        new DatabaseInvoiceNumberProvider(_write);
}
