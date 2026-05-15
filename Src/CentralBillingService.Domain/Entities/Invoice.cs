namespace CentralBillingService.Domain.Entities;

/// <summary>
/// Agregado raíz del sistema de facturación.
///
/// Una factura es inmutable una vez emitida (estado Issued).
/// Para "corregir" una factura emitida se crea una factura rectificativa
/// (RectificativeInvoice) que referencia a esta — nunca se modifica aquí.
///
/// Contiene:
/// - Quién emite (el emisor con sus datos fiscales de esa web)
/// - A quién va dirigida (el cliente)
/// - Qué se factura (líneas)
/// - En qué divisa pidió el cliente y a qué cambio se convirtió a EUR
/// - El hash VeriFactu encadenado con la factura anterior
/// </summary>
public sealed class Invoice
{
    // ── Identidad ──────────────────────────────────────────────────────────

    public Guid Id { get; }
    public InvoiceNumber Number { get; }

    /// <summary>
    /// Identifica qué web o proyecto generó esta factura.
    /// Ej: "web-fotos", "web-cripto", "web-tv", "web-cms", "proyecto-directo"
    /// </summary>
    public string BillingSource { get; }

    // ── Partes ─────────────────────────────────────────────────────────────

    public BillingParty Issuer { get; }
    public BillingParty Recipient { get; }

    // ── Fechas ─────────────────────────────────────────────────────────────

    /// <summary>Fecha de expedición (la que aparece en la factura)</summary>
    public DateOnly IssueDate { get; }

    /// <summary>
    /// Fecha de devengo si difiere de la expedición.
    /// En servicios de suscripción puede ser el período facturado.
    /// </summary>
    public DateOnly? ValueDate { get; }

    /// <summary>Timestamp exacto de creación — siempre UTC</summary>
    public DateTimeOffset CreatedAt { get; }

    // ── Líneas ─────────────────────────────────────────────────────────────

    private readonly List<InvoiceLine> _lines;
    public IReadOnlyList<InvoiceLine> Lines => _lines.AsReadOnly();

    // ── Divisa y cambio ────────────────────────────────────────────────────

    /// <summary>
    /// Tipo de cambio aplicado en el momento de creación.
    /// Si el cliente pagó en EUR, es el tipo identidad (1:1).
    /// Este valor es inmutable — refleja la tasa real del momento de facturación.
    /// </summary>
    public ExchangeRate AppliedExchangeRate { get; }

    /// <summary>Total que el cliente verá en su divisa original</summary>
    public Money TotalInOriginCurrency { get; }

    // ── Totales en EUR ─────────────────────────────────────────────────────

    public Money TaxableBaseEur { get; }
    public Money TotalTaxAmountEur { get; }
    public Money TotalEur { get; }

    // ── VeriFactu ──────────────────────────────────────────────────────────

    /// <summary>Hash de esta factura (encadenado con el anterior)</summary>
    public string Hash { get; }

    /// <summary>Hash de la factura anterior en la misma serie (null si es la primera)</summary>
    public string? PreviousHash { get; }

    // ── Estado ─────────────────────────────────────────────────────────────

    public InvoiceStatus Status { get; private set; }

    /// <summary>
    /// Si esta factura ha sido rectificada, aquí va el número de la rectificativa.
    /// La factura original no cambia — solo se registra que existe una corrección.
    /// </summary>
    public InvoiceNumber? RectifiedBy { get; private set; }

    // ── Notas ──────────────────────────────────────────────────────────────

    public string? Notes { get; }

    public string? PaymentMethod { get; init; }

    public string PaymentReference { get; init; }

    public string? TransactionData { get; init; }

    public bool HasTamper { get; private set; }

    // ── Constructor privado ────────────────────────────────────────────────

    private Invoice(
        Guid id,
        InvoiceNumber number,
        string billingSource,
        BillingParty issuer,
        BillingParty recipient,
        DateOnly issueDate,
        DateOnly? valueDate,
        DateTimeOffset createdAt,
        List<InvoiceLine> lines,
        ExchangeRate appliedExchangeRate,
        string hash,
        string? previousHash,
        string? notes,
        string paymentReference,
        string? transactionData,
        string? paymentMethod)
    {
        Id = id;
        Number = number;
        BillingSource = billingSource;
        Issuer = issuer;
        Recipient = recipient;
        IssueDate = issueDate;
        ValueDate = valueDate;
        CreatedAt = createdAt;
        _lines = lines;
        AppliedExchangeRate = appliedExchangeRate;
        Hash = hash;
        PreviousHash = previousHash;
        Notes = notes;
        Status = InvoiceStatus.Draft;

        // Calcular totales consolidando las líneas
        TaxableBaseEur = lines.Aggregate(Money.Zero(Currency.EUR), (acc, l) => acc.Add(l.TaxableBaseEur));
        TotalTaxAmountEur = lines.Aggregate(Money.Zero(Currency.EUR), (acc, l) => acc.Add(l.TaxAmountEur));
        TotalEur = TaxableBaseEur.Add(TotalTaxAmountEur);

        // Total en divisa origen: suma de totales origen de cada línea
        var originCurrency = appliedExchangeRate.From;
        TotalInOriginCurrency = lines.Aggregate(
            Money.Zero(originCurrency),
            (acc, l) => acc.Add(l.TotalOrigin));

        PaymentReference = paymentReference;
        TransactionData = transactionData;
        PaymentMethod = paymentMethod;
    }

    // ── Factory method ─────────────────────────────────────────────────────

    /// <summary>
    /// Crea una factura.
    /// El hash se calcula en este momento con el hasher proporcionado.
    /// </summary>
    public static Invoice Create(
        InvoiceNumber number,
        string billingSource,
        BillingParty issuer,
        BillingParty recipient,
        DateOnly issueDate,
        List<InvoiceLine> lines,
        ExchangeRate appliedExchangeRate,
        IInvoiceHasher hasher,
        string paymentReference,
        string? previousHash = null,
        DateOnly? valueDate = null,
        string? notes = null,
        string? transactionData = null,
        string? paymentMethod = null)
    {
        if (string.IsNullOrWhiteSpace(billingSource))
            throw new DomainException("Source is mandatory: it identifies which website generates the invoice.");

        if (string.IsNullOrWhiteSpace(paymentReference))
            throw new DomainException("PaymentReference is mandatory: provide a register of payment action.");

        if (lines == null || lines.Count == 0)
            throw new DomainException("An invoice must have at least one line.");

        var createdAt = DateTimeOffset.UtcNow;

        // Construir primero la instancia sin hash para poder calcular el contenido
        var invoice = new Invoice(
            Guid.NewGuid(),
            number,
            billingSource.Trim().ToLowerInvariant(),
            issuer,
            recipient,
            issueDate,
            valueDate,
            createdAt,
            lines,
            appliedExchangeRate,
            hash: string.Empty, // temporal
            previousHash,
            notes,
            paymentReference,
            transactionData,
            paymentMethod);

        // Calcular el hash ahora que tenemos todos los datos
        var hashContent = invoice.BuildHashContent();
        var computedHash = hasher.Compute(hashContent, previousHash);

        // Devolver una instancia definitiva con el hash real
        return new Invoice(
            invoice.Id,
            number,
            invoice.BillingSource,
            issuer,
            recipient,
            issueDate,
            valueDate,
            createdAt,
            lines,
            appliedExchangeRate,
            computedHash,
            previousHash,
            notes,
            paymentReference,
            transactionData,
            paymentMethod);
    }



    /// <summary>
    /// Reconstitutes an Invoice from persisted data.
    /// Bypasses creation validations and hash computation —
    /// the data is already valid and the hash is already stored.
    /// Only called by the persistence mapper.
    /// </summary>
    public static Invoice Reconstitute(
        Guid id,
        InvoiceNumber number,
        string billingSource,
        BillingParty issuer,
        BillingParty recipient,
        DateOnly issueDate,
        DateOnly? valueDate,
        DateTimeOffset createdAt,
        List<InvoiceLine> lines,
        ExchangeRate appliedExchangeRate,
        string hash,
        string? previousHash,
        InvoiceStatus status,
        string paymentReference,
        InvoiceNumber? rectifiedBy,
        string? notes,
        string? transactionData = null,
        string? paymentMethod = null)
    {
        var invoice = new Invoice(
            id, number, billingSource, issuer, recipient,
            issueDate, valueDate, createdAt, lines,
            appliedExchangeRate, hash, previousHash, notes,
            paymentReference, transactionData, paymentMethod);

        invoice.Status = status;
        invoice.RectifiedBy = rectifiedBy;
        return invoice;
    }

    // ── Transiciones de estado ─────────────────────────────────────────────

    /// <summary>
    /// Emite la factura: pasa a Issued y ya no puede modificarse.
    /// A partir de este momento es inmutable.
    /// </summary>
    public void Issue()
    {
        if (Status != InvoiceStatus.Draft)
            throw new DomainException($"Only one invoice can be issued in Draft status. Current status: {Status}.");

        Status = InvoiceStatus.Issued;
    }

    /// <summary>
    /// Marca la factura como rectificada por otra.
    /// La factura original permanece intacta — solo se registra la referencia.
    /// </summary>
    public void MarkAsRectifiedBy(InvoiceNumber rectificativeNumber)
    {
        if (Status != InvoiceStatus.Issued)
            throw new DomainException("An invoice can only be corrected in the Issued state.");

        RectifiedBy = rectificativeNumber;
        Status = InvoiceStatus.Rectified;
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    public bool IsInOriginCurrency => !AppliedExchangeRate.IsIdentity;

    public InvoiceHashContent BuildHashContent() => new()
    {
        IssuerTaxId = Issuer.TaxId.Value,
        InvoiceNumber = Number.Value,
        IssueDate = IssueDate.ToString("yyyy-MM-dd"),
        InvoiceType = "F",
        TotalTaxAmountEur = TotalTaxAmountEur.Amount.ToString("F2"),
        TotalAmountEur = TotalEur.Amount.ToString("F2"),
        CreatedAt = CreatedAt.ToString("o"),
        BillingSource = BillingSource,
        IssuerLegalName = Issuer.LegalName,
        IssuerAddressLine1 = Issuer.Address.Line1,
        IssuerCity = Issuer.Address.City,
        IssuerPostalCode = Issuer.Address.PostalCode,
        IssuerCountryCode = Issuer.Address.CountryCode,
        PaymentReference = PaymentReference,
        RecipientTaxId = Recipient.TaxId.Value,
        RecipientLegalName = Recipient.LegalName,
        RecipientAddressLine1 = Recipient.Address.Line1,
        RecipientCity = Recipient.Address.City,
        RecipientPostalCode = Recipient.Address.PostalCode,
        RecipientCountryCode = Recipient.Address.CountryCode,
        RecipientExternalId = Recipient.ExternalId ?? string.Empty,
        PaymentMethod = PaymentMethod ?? string.Empty,
        TransactionData = TransactionData ?? string.Empty,
        Lines = Lines
            .OrderBy(l => l.LineNumber)
            .Select(l => new InvoiceLineHashContent
            {
                LineNumber = l.LineNumber.ToString(),
                Description = l.Description,
                Quantity = l.Quantity.ToString(),
                UnitPriceEur = l.UnitPriceEur.Amount.ToString("F2"),
                TaxRatePercentage = l.TaxRate.Percentage.ToString("F2"),
                TotalEur = l.TotalEur.Amount.ToString("F2"),
            })
            .ToList(),
    };

    /// <summary>
    /// Verifies that the stored hash matches a fresh recomputation.
    /// Returns false if any field was modified after the invoice was created.
    /// </summary>
    public bool VerifyIntegrity(IInvoiceHasher hasher)
    {
        var isValid = hasher.Verify(BuildHashContent(), PreviousHash, Hash);
        HasTamper = !isValid;
        return isValid;
    }

    public override string ToString() =>
        $"{Number} | {Recipient.DisplayName} | {TotalEur} | {Status}";
}