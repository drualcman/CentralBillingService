namespace CentralBillingService.Persistence.SqlServer.Contetxs;

internal static class ContextConfigurations
{
    // ── Invoice (unified — covers both standard and rectificative) ─────────

    public static void ConfigureInvoice(ModelBuilder mb)
    {
        mb.Entity<InvoiceEntity>(e =>
        {
            e.ToTable("Invoices");
            e.HasKey(x => x.Id);

            e.Property(x => x.InvoiceNumber).HasMaxLength(30).IsRequired();
            e.Property(x => x.BillingSource).HasMaxLength(50).IsRequired();
            e.Property(x => x.Serie).HasMaxLength(25).IsRequired();
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();
            e.Property(x => x.InvoiceType).HasMaxLength(1).IsRequired().HasDefaultValue("F");

            // Rectificative-specific (nullable for standard invoices)
            e.Property(x => x.OriginalInvoiceNumber).HasMaxLength(30);
            e.Property(x => x.RectificationReason).HasMaxLength(500);
            e.Property(x => x.RectificationType).HasMaxLength(20);

            // Issuer
            e.Property(x => x.IssuerLegalName).HasMaxLength(200).IsRequired();
            e.Property(x => x.IssuerTradeName).HasMaxLength(200);
            e.Property(x => x.IssuerTaxIdValue).HasMaxLength(20).IsRequired();
            e.Property(x => x.IssuerTaxIdCountryCode).HasMaxLength(2).IsRequired();
            e.Property(x => x.IssuerEmail).HasMaxLength(200).IsRequired();
            e.Property(x => x.IssuerPhone).HasMaxLength(30);
            e.Property(x => x.IssuerWebsite).HasMaxLength(200);
            e.Property(x => x.IssuerAddressLine1).HasMaxLength(200).IsRequired();
            e.Property(x => x.IssuerAddressLine2).HasMaxLength(200);
            e.Property(x => x.IssuerCity).HasMaxLength(100).IsRequired();
            e.Property(x => x.IssuerProvince).HasMaxLength(100);
            e.Property(x => x.IssuerPostalCode).HasMaxLength(10).IsRequired();
            e.Property(x => x.IssuerAddressCountryCode).HasMaxLength(2).IsRequired();

            // Recipient
            e.Property(x => x.RecipientLegalName).HasMaxLength(200).IsRequired();
            e.Property(x => x.RecipientTradeName).HasMaxLength(200);
            e.Property(x => x.RecipientTaxIdValue).HasMaxLength(20).IsRequired();
            e.Property(x => x.RecipientTaxIdCountryCode).HasMaxLength(2).IsRequired();
            e.Property(x => x.RecipientEmail).HasMaxLength(200).IsRequired();
            e.Property(x => x.RecipientPhone).HasMaxLength(30);
            e.Property(x => x.RecipientWebsite).HasMaxLength(200);
            e.Property(x => x.RecipientAddressLine1).HasMaxLength(200).IsRequired();
            e.Property(x => x.RecipientAddressLine2).HasMaxLength(200);
            e.Property(x => x.RecipientCity).HasMaxLength(100).IsRequired();
            e.Property(x => x.RecipientProvince).HasMaxLength(100);
            e.Property(x => x.RecipientPostalCode).HasMaxLength(10).IsRequired();
            e.Property(x => x.RecipientAddressCountryCode).HasMaxLength(2).IsRequired();
            e.Property(x => x.RecipientExternalId).HasMaxLength(100);

            // Amounts — 18 digits total, 4 decimal places for precision
            e.Property(x => x.TaxableBaseEur).HasColumnType("decimal(18,4)");
            e.Property(x => x.TotalTaxAmountEur).HasColumnType("decimal(18,4)");
            e.Property(x => x.TotalEur).HasColumnType("decimal(18,4)");
            e.Property(x => x.TotalOriginAmount).HasColumnType("decimal(18,4)");
            e.Property(x => x.OriginCurrencyCode).HasMaxLength(3).IsRequired();

            // Exchange rate
            e.Property(x => x.ExchangeRateFrom).HasMaxLength(3).IsRequired();
            e.Property(x => x.ExchangeRateTo).HasMaxLength(3).IsRequired();
            e.Property(x => x.ExchangeRateValue).HasColumnType("decimal(18,8)");

            // VeriFactu — SHA-256 hex is always 64 chars
            e.Property(x => x.Hash).HasMaxLength(64).IsRequired();
            e.Property(x => x.PreviousHash).HasMaxLength(64);

            e.Property(x => x.RectifiedByNumber).HasMaxLength(30);
            e.Property(x => x.Notes).HasMaxLength(1000);
            e.Property(x => x.QrCodeBlobUrl).HasMaxLength(500);

            // Indexes
            e.HasIndex(x => x.InvoiceNumber).IsUnique();
            e.HasIndex(x => new { x.BillingSource, x.Serie, x.Year, x.SequenceNumber }).IsUnique();

            // Idempotency backstop: a standard invoice's payment reference is unique per billing
            // source, so a retried/concurrent payment webhook cannot create a duplicate invoice.
            // Filtered to InvoiceType = 'F' — rectificatives ("R") carry their own references.
            e.HasIndex(x => new { x.BillingSource, x.PaymentReference })
             .IsUnique()
             .HasFilter("[InvoiceType] = 'F'");
            e.HasIndex(x => new { x.BillingSource, x.RecipientTaxIdValue });
            e.HasIndex(x => new { x.BillingSource, x.RecipientExternalId });
            e.HasIndex(x => x.IssueDate);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.InvoiceType);
            e.HasIndex(x => x.OriginalInvoiceNumber);

            // Lines
            e.HasMany(x => x.Lines)
             .WithOne(x => x.Invoice)
             .HasForeignKey(x => x.InvoiceId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }

    // ── InvoiceLine ────────────────────────────────────────────────────────

    public static void ConfigureInvoiceLine(ModelBuilder mb)
    {
        mb.Entity<InvoiceLineEntity>(e =>
        {
            e.ToTable("InvoiceLines");
            e.HasKey(x => x.Id);

            e.Property(x => x.Description).HasMaxLength(500).IsRequired();
            e.Property(x => x.OriginCurrencyCode).HasMaxLength(3).IsRequired();

            e.Property(x => x.UnitPriceEur).HasColumnType("decimal(18,4)");
            e.Property(x => x.TaxableBaseEur).HasColumnType("decimal(18,4)");
            e.Property(x => x.TaxAmountEur).HasColumnType("decimal(18,4)");
            e.Property(x => x.TotalEur).HasColumnType("decimal(18,4)");
            e.Property(x => x.UnitPriceOrigin).HasColumnType("decimal(18,4)");
            e.Property(x => x.TotalOrigin).HasColumnType("decimal(18,4)");

            e.HasIndex(x => new { x.InvoiceId, x.LineNumber });
        });
    }

    // ── InvoiceSequence ────────────────────────────────────────────────────

    public static void ConfigureInvoiceSequence(ModelBuilder mb)
    {
        mb.Entity<InvoiceSequenceEntity>(e =>
        {
            e.ToTable("InvoiceSequences");
            e.HasKey(x => x.Id);

            e.Property(x => x.BillingSource).HasMaxLength(50).IsRequired();
            e.Property(x => x.Serie).HasMaxLength(25).IsRequired();
            e.Property(x => x.LastHash).HasMaxLength(64);

            // Optimistic concurrency — SQL Server rowversion
            e.Property(x => x.RowVersion)
             .IsRowVersion()
             .IsConcurrencyToken();

            // Natural unique key
            e.HasIndex(x => new { x.BillingSource, x.Serie, x.Year }).IsUnique();
        });
    }
}
