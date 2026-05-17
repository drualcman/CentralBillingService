namespace CentralBillingService.Tests.Unit.Domain.Services;

public class BillingSourceRegistryTests
{
    private static BillingSourceRegistry BuildRegistry(
        string billingSource = "web-fotos",
        string secret = "secret123")
    {
        var options = Options.Create(new CbsOptions
        {
            BillingSources = [new BillingSourceConfig
            {
                BillingSource = billingSource,
                Secret = secret,
                Issuer = IssuerConfig.From(InvoiceBuilder.DefaultIssuer())
            }]
        });
        return new BillingSourceRegistry(options);
    }

    [Fact]
    public void GetConfig_returns_config_for_valid_source_and_secret()
    {
        var registry = BuildRegistry();
        var config = registry.GetConfig("web-fotos", "secret123");

        Assert.NotNull(config);
        Assert.Equal("web-fotos", config.BillingSource);
    }

    [Fact]
    public void GetConfig_is_case_insensitive_for_billing_source()
    {
        var registry = BuildRegistry();
        var config = registry.GetConfig("WEB-FOTOS", "secret123");
        Assert.NotNull(config);
    }

    [Fact]
    public void GetConfig_wrong_secret_throws()
    {
        var registry = BuildRegistry();
        Assert.Throws<DomainException>(() => registry.GetConfig("web-fotos", "wrong-secret"));
    }

    [Fact]
    public void GetConfig_unknown_billing_source_throws()
    {
        var registry = BuildRegistry();
        Assert.Throws<DomainException>(() => registry.GetConfig("unknown-source", "secret123"));
    }

    [Fact]
    public void GetConfig_null_billing_source_throws()
    {
        var registry = BuildRegistry();
        Assert.Throws<ArgumentNullException>(() => registry.GetConfig(null!, "secret123"));
    }

    [Fact]
    public void IsRegistered_returns_true_for_known_source()
    {
        var registry = BuildRegistry();
        Assert.True(registry.IsRegistered("web-fotos"));
    }

    [Fact]
    public void IsRegistered_is_case_insensitive()
    {
        var registry = BuildRegistry();
        Assert.True(registry.IsRegistered("WEB-FOTOS"));
    }

    [Fact]
    public void IsRegistered_returns_false_for_unknown_source()
    {
        var registry = BuildRegistry();
        Assert.False(registry.IsRegistered("unknown"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void IsRegistered_returns_false_for_empty_source(string source)
    {
        var registry = BuildRegistry();
        Assert.False(registry.IsRegistered(source));
    }

    [Fact]
    public void IsRegistered_false_for_null()
    {
        var registry = BuildRegistry();
        Assert.False(registry.IsRegistered(null!));
    }

    [Fact]
    public void Registry_supports_multiple_billing_sources()
    {
        var options = Options.Create(new CbsOptions
        {
            BillingSources =
            [
                new BillingSourceConfig
                {
                    BillingSource = "web-fotos",
                    Secret = "secret1",
                    Issuer = IssuerConfig.From(InvoiceBuilder.DefaultIssuer())
                },
                new BillingSourceConfig
                {
                    BillingSource = "web-cripto",
                    Secret = "secret2",
                    Issuer = IssuerConfig.From(InvoiceBuilder.DefaultIssuer())
                }
            ]
        });
        var registry = new BillingSourceRegistry(options);

        Assert.True(registry.IsRegistered("web-fotos"));
        Assert.True(registry.IsRegistered("web-cripto"));
        Assert.NotNull(registry.GetConfig("web-fotos", "secret1"));
        Assert.NotNull(registry.GetConfig("web-cripto", "secret2"));
    }
}
