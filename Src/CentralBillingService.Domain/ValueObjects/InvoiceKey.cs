namespace CentralBillingService.Domain.ValueObjects;

public record InvoiceKey
{
    public string Paltform { get; init; }
    public string Reference { get; init; }

    public InvoiceKey(string webOrigen, string referenciaExterna)
    {
        Paltform = webOrigen?.ToLowerInvariant().Trim()
            ?? throw new ArgumentNullException(nameof(webOrigen));

        Reference = referenciaExterna?.Trim()
            ?? throw new ArgumentNullException(nameof(referenciaExterna));

        if (string.IsNullOrWhiteSpace(Reference))
            throw new ArgumentException("Reference is mandatory for idempotence");
    }
}
