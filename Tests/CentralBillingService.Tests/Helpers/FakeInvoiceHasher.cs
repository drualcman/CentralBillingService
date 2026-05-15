namespace CentralBillingService.Tests.Helpers;

public sealed class FakeInvoiceHasher : IInvoiceHasher
{
    public string Compute(InvoiceHashContent content, string? previousHash)
        => $"FAKE_{content.InvoiceNumber}_{previousHash ?? "FIRST"}";

    public bool Verify(InvoiceHashContent content, string? previousHash, string storedHash)
        => Compute(content, previousHash) == storedHash;
}
