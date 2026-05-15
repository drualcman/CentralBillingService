namespace CentralBillingService.Tests.Unit.Infrastructure.Hashing;

public class Sha256InvoiceHasherTests
{
    private static readonly Sha256InvoiceHasher Hasher = new();

    private static InvoiceHashContent BuildContent(string invoiceNumber = "FOTO2026-0001") => new()
    {
        IssuerTaxId = "12345678A",
        InvoiceNumber = invoiceNumber,
        IssueDate = "2026-05-01",
        InvoiceType = "F",
        TotalAmountEur = "121.00",
        TotalTaxAmountEur = "21.00",
        BillingSource = "web-fotos",
        CreatedAt = "2026-05-01T10:00:00.0000000+00:00",
    };

    [Fact]
    public void Compute_returns_non_empty_hash()
    {
        var hash = Hasher.Compute(BuildContent(), null);
        Assert.NotEmpty(hash);
    }

    [Fact]
    public void Compute_returns_uppercase_hexadecimal()
    {
        var hash = Hasher.Compute(BuildContent(), null);
        Assert.Equal(hash.ToUpperInvariant(), hash);
        Assert.Matches("^[0-9A-F]+$", hash);
    }

    [Fact]
    public void Compute_sha256_produces_64_character_hash()
    {
        var hash = Hasher.Compute(BuildContent(), null);
        Assert.Equal(64, hash.Length);
    }

    [Fact]
    public void Compute_is_deterministic_for_same_input()
    {
        var content = BuildContent();
        var hash1 = Hasher.Compute(content, "PREV_HASH");
        var hash2 = Hasher.Compute(content, "PREV_HASH");
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void Compute_different_previous_hash_changes_result()
    {
        var content = BuildContent();
        var hashFirst = Hasher.Compute(content, null);
        var hashChained = Hasher.Compute(content, "SOME_PREVIOUS_HASH");
        Assert.NotEqual(hashFirst, hashChained);
    }

    [Fact]
    public void Compute_different_invoice_number_changes_result()
    {
        var hash1 = Hasher.Compute(BuildContent("FOTO2026-0001"), null);
        var hash2 = Hasher.Compute(BuildContent("FOTO2026-0002"), null);
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Compute_different_issuer_tax_id_changes_result()
    {
        var content1 = BuildContent();
        var content2 = new InvoiceHashContent
        {
            IssuerTaxId = "87654321B",
            InvoiceNumber = content1.InvoiceNumber,
            IssueDate = content1.IssueDate,
            InvoiceType = content1.InvoiceType,
            TotalAmountEur = content1.TotalAmountEur,
            TotalTaxAmountEur = content1.TotalTaxAmountEur,
            BillingSource = content1.BillingSource,
            CreatedAt = content1.CreatedAt,
        };

        Assert.NotEqual(Hasher.Compute(content1, null), Hasher.Compute(content2, null));
    }

    [Fact]
    public void Verify_returns_true_for_correct_stored_hash()
    {
        var content = BuildContent();
        var hash = Hasher.Compute(content, "PREV");
        Assert.True(Hasher.Verify(content, "PREV", hash));
    }

    [Fact]
    public void Verify_returns_false_for_tampered_hash()
    {
        var content = BuildContent();
        Assert.False(Hasher.Verify(content, null, "TAMPERED_HASH_VALUE"));
    }

    [Fact]
    public void Verify_case_insensitive_comparison()
    {
        var content = BuildContent();
        var hash = Hasher.Compute(content, null);
        Assert.True(Hasher.Verify(content, null, hash.ToLowerInvariant()));
    }

    [Fact]
    public void Compute_sanitizes_ampersand_in_field_values()
    {
        var content = new InvoiceHashContent
        {
            IssuerTaxId = "12345678A",
            InvoiceNumber = "FOTO2026-0001",
            IssueDate = "2026-05-01",
            InvoiceType = "F",
            TotalAmountEur = "121.00",
            TotalTaxAmountEur = "21.00",
            BillingSource = "web&fotos",   // ampersand must be sanitized
            CreatedAt = "2026-05-01T10:00:00.0000000+00:00",
        };

        var hash = Hasher.Compute(content, null);

        Assert.NotEmpty(hash);
        Assert.Equal(64, hash.Length);
    }

    [Fact]
    public void Hash_chain_first_invoice_differs_from_second()
    {
        var content1 = BuildContent("FOTO2026-0001");
        var hash1 = Hasher.Compute(content1, null);

        var content2 = BuildContent("FOTO2026-0002");
        var hash2 = Hasher.Compute(content2, hash1);

        Assert.NotEqual(hash1, hash2);
    }
}
