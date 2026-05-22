namespace CentralBillingService.WPF.Models;

public class NoteRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Content { get; set; } = "";

    public override string ToString() => Name;
}
