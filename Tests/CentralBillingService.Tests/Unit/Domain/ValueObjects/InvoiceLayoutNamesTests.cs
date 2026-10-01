namespace CentralBillingService.Tests.Unit.Domain.ValueObjects;

/// <summary>The requested layout wins when it exists; otherwise the source's default; otherwise "Invoice".</summary>
public class InvoiceLayoutNamesTests
{
    [Theory]
    [InlineData("Ticket", "Subscription", "Ticket")]
    [InlineData("ticket", null, "Ticket")]
    [InlineData(null, "Subscription", "Subscription")]
    [InlineData("DoesNotExist", "Subscription", "Subscription")]
    [InlineData("Mixed", null, "Invoice")]
    [InlineData("DoesNotExist", "AlsoUnknown", "Invoice")]
    [InlineData(null, null, "Invoice")]
    [InlineData("  ", "", "Invoice")]
    public void Resolve_falls_back_from_requested_to_default_to_invoice(string? requested, string? sourceDefault, string expected)
    {
        Assert.Equal(expected, InvoiceLayoutNames.Resolve(requested, sourceDefault));
    }

    [Fact]
    public void Layout_is_not_part_of_the_integrity_hash()
    {
        Invoice invoice = InvoiceBuilder.BuildIssued();
        string hashBefore = invoice.Hash;

        invoice.AssignLayout(InvoiceLayoutNames.Ticket);

        Assert.Equal(InvoiceLayoutNames.Ticket, invoice.Layout);
        Assert.True(invoice.VerifyIntegrity(new FakeInvoiceHasher()));
        Assert.Equal(hashBefore, invoice.Hash);
    }
}
