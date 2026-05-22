namespace CentralBillingService.Domain.Models;

public record Addressee
{
    public string DisplayName { get; set; }

    public string Adressee { get; set; }
    public Addressee()
    {
        DisplayName = string.Empty;
        Adressee = string.Empty;
    }
    public Addressee(string addressee) : this() => Adressee = addressee;

    public Addressee(string addressee, string displayName) : this(addressee) => DisplayName = displayName;

    public bool HasAddress => !string.IsNullOrEmpty(Adressee);
}
