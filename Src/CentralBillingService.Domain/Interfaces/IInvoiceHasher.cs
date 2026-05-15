namespace CentralBillingService.Domain.Interfaces;

/// <summary>
/// Contract for computing the VeriFactu chained hash.
/// Each invoice hash incorporates the previous invoice's hash in the same
/// BillingSource+Serie+Year chain, making retroactive tampering detectable.
/// </summary>
public interface IInvoiceHasher
{
    /// <summary>
    /// Computes the hash for the given invoice content, chained with the previous hash.
    /// </summary>
    /// <param name="content">Canonical fields of the invoice to hash.</param>
    /// <param name="previousHash">
    /// Hash of the previous invoice in the same chain.
    /// Null or empty if this is the first invoice in the chain.
    /// </param>
    string Compute(InvoiceHashContent content, string? previousHash);

    /// <summary>
    /// Verifies that the stored hash matches the recomputed one.
    /// Used for audit and reconciliation.
    /// </summary>
    bool Verify(InvoiceHashContent content, string? previousHash, string storedHash);
}