namespace CentralBillingService.Domain.ValueObjects;

/// <summary>
/// Printed models an invoice can be rendered with. Pure presentation: the fiscal content (and therefore
/// the hash and the tax-authority record) is the same whichever layout is chosen. The chosen layout is
/// stored with the invoice so it is always regenerated the same way.
/// </summary>
public static class InvoiceLayoutNames
{
    public const string Invoice = "Invoice";
    public const string Ticket = "Ticket";
    public const string Subscription = "Subscription";

    public static IReadOnlyList<string> All { get; } = [Invoice, Ticket, Subscription];

    /// <summary>The requested layout when it exists, else the source's default when it exists, else <see cref="Invoice"/>.</summary>
    public static string Resolve(string? requestedLayout, string? defaultLayout)
    {
        string resolvedLayout = Invoice;
        string? knownRequested = FindKnown(requestedLayout);
        string? knownDefault = FindKnown(defaultLayout);

        if (knownRequested is not null)
        {
            resolvedLayout = knownRequested;
        }
        else if (knownDefault is not null)
        {
            resolvedLayout = knownDefault;
        }

        return resolvedLayout;
    }

    private static string? FindKnown(string? layout) =>
        string.IsNullOrWhiteSpace(layout)
            ? null
            : All.FirstOrDefault(known => known.Equals(layout.Trim(), StringComparison.OrdinalIgnoreCase));
}
