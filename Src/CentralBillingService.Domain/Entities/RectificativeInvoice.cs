namespace CentralBillingService.Domain.Entities;

/// <summary>
/// Factura rectificativa: corrige total o parcialmente una factura ya emitida.
///
/// En España (Art. 15 RD 1619/2012) una factura rectificativa debe:
/// - Referenciar explícitamente la factura original
/// - Indicar el motivo de la rectificación
/// - Poder rectificar por diferencia (solo el delta) o por sustitución (importe completo negativo)
///
/// Esta entidad es también inmutable una vez emitida.
/// Tiene su propio número correlativo (serie REC u otra definida por el emisor).
/// Entra también en la cadena de hash VeriFactu.
/// </summary>
public sealed class RectificativeInvoice
{
    // ── Identidad ──────────────────────────────────────────────────────────

    public Guid Id { get; }
    public InvoiceNumber Number { get; }
    public string BillingSource { get; }

    // ── Referencia a la original ───────────────────────────────────────────

    /// <summary>Número de la factura que se está rectificando</summary>
    public InvoiceNumber OriginalInvoiceNumber { get; }

    /// <summary>Fecha de expedición de la factura original</summary>
    public DateOnly OriginalIssueDate { get; }

    /// <summary>Motivo de la rectificación — obligatorio y debe ser descriptivo</summary>
    public string RectificationReason { get; }

    /// <summary>Tipo de rectificación aplicada</summary>
    public RectificationType RectificationType { get; }

    // ── Partes — se copian de la original ─────────────────────────────────

    public BillingParty Issuer { get; }
    public BillingParty Recipient { get; }

    // ── Fechas ─────────────────────────────────────────────────────────────

    public DateOnly IssueDate { get; }
    public DateTimeOffset CreatedAt { get; }

    // ── Líneas ─────────────────────────────────────────────────────────────

    /// <summary>
    /// En rectificación por SUSTITUCIÓN: las líneas con importes negativos
    /// que anulan la factura original.
    /// En rectificación por DIFERENCIA: solo las líneas que cambian, con el delta.
    /// </summary>
    private readonly List<InvoiceLine> _lines;
    public IReadOnlyList<InvoiceLine> Lines => _lines.AsReadOnly();

    // ── Divisa y cambio ────────────────────────────────────────────────────

    /// <summary>
    /// Se usa el tipo de cambio del momento de la rectificativa,
    /// no el de la factura original — cada documento es independiente.
    /// </summary>
    public ExchangeRate AppliedExchangeRate { get; }

    // ── Totales ────────────────────────────────────────────────────────────

    /// <summary>
    /// En sustitución será negativo (anula la original).
    /// En diferencia puede ser positivo o negativo según el ajuste.
    /// </summary>
    public Money TaxableBaseEur { get; }
    public Money TotalTaxAmountEur { get; }
    public Money TotalEur { get; }
    public Money TotalInOriginCurrency { get; }

    // ── VeriFactu ──────────────────────────────────────────────────────────

    public string Hash { get; }
    public string? PreviousHash { get; }

    // ── Estado ─────────────────────────────────────────────────────────────

    public InvoiceStatus Status { get; private set; }

    /// <summary>
    /// Si esta factura rectificativa ha sido a su vez rectificada,
    /// aquí queda el número de la nueva rectificativa.
    /// </summary>
    public InvoiceNumber? RectifiedBy { get; private set; }

    public string? Notes { get; }

    public string? PaymentMethod { get; init; }

    public string PaymentReference { get; init; }

    public string? TransactionData { get; init; }
    public bool HasTamper { get; private set; }

    // ── Constructor privado ────────────────────────────────────────────────

    private RectificativeInvoice(
        Guid id,
        InvoiceNumber number,
        string billingSource,
        InvoiceNumber originalInvoiceNumber,
        DateOnly originalIssueDate,
        string rectificationReason,
        RectificationType rectificationType,
        BillingParty issuer,
        BillingParty recipient,
        DateOnly issueDate,
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
        OriginalInvoiceNumber = originalInvoiceNumber;
        OriginalIssueDate = originalIssueDate;
        RectificationReason = rectificationReason;
        RectificationType = rectificationType;
        Issuer = issuer;
        Recipient = recipient;
        IssueDate = issueDate;
        CreatedAt = createdAt;
        _lines = lines;
        AppliedExchangeRate = appliedExchangeRate;
        Hash = hash;
        PreviousHash = previousHash;
        Notes = notes;
        Status = InvoiceStatus.Draft;

        // Los totales se calculan igual que en Invoice —
        // el signo negativo lo llevan los importes de las líneas si procede
        TaxableBaseEur = lines.Aggregate(Money.Zero(Currency.EUR), (acc, l) => acc.Add(l.TaxableBaseEur));
        TotalTaxAmountEur = lines.Aggregate(Money.Zero(Currency.EUR), (acc, l) => acc.Add(l.TaxAmountEur));
        TotalEur = TaxableBaseEur.Add(TotalTaxAmountEur);

        var originCurrency = appliedExchangeRate.From;
        TotalInOriginCurrency = lines.Aggregate(
            Money.Zero(originCurrency),
            (acc, l) => acc.Add(l.TotalOrigin));

        PaymentReference = paymentReference;
        TransactionData = transactionData;
        PaymentMethod = paymentMethod;
    }

    // ── Factory method ─────────────────────────────────────────────────────

    public static RectificativeInvoice Create(
        InvoiceNumber number,
        string billingSource,
        Invoice originalInvoice,
        string rectificationReason,
        RectificationType rectificationType,
        List<InvoiceLine> lines,
        ExchangeRate appliedExchangeRate,
        IInvoiceHasher hasher,
        string paymentReference,
        string? previousHash = null,
        string? notes = null,
        string? transactionData = null,
        string? paymentMethod = null)
    {
        if (lines == null || lines.Count == 0)
            throw new DomainException("A corrective invoice must have at least one line.");

        if (originalInvoice.Status != InvoiceStatus.Issued && originalInvoice.Status != InvoiceStatus.Rectified)
            throw new DomainException(
                $"Solo se puede rectificar una factura en estado Issued o Rectified. " +
                $"Estado actual: {originalInvoice.Status}.");

        var issueDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var createdAt = DateTimeOffset.UtcNow;

        // Construcción temporal para calcular el hash
        var temp = new RectificativeInvoice(
            Guid.NewGuid(),
            number,
            billingSource.Trim().ToLowerInvariant(),
            originalInvoice.Number,
            originalInvoice.IssueDate,
            rectificationReason.Trim(),
            rectificationType,
            originalInvoice.Issuer,
            originalInvoice.Recipient,
            issueDate,
            createdAt,
            lines,
            appliedExchangeRate,
            hash: string.Empty,
            previousHash,
            notes,
            paymentReference,
            transactionData,
            paymentMethod);

        var hashContent = temp.BuildHashContent();
        var computedHash = hasher.Compute(hashContent, previousHash);

        return new RectificativeInvoice(
            temp.Id,
            number,
            temp.BillingSource,
            originalInvoice.Number,
            originalInvoice.IssueDate,
            rectificationReason.Trim(),
            rectificationType,
            originalInvoice.Issuer,
            originalInvoice.Recipient,
            issueDate,
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
    /// Crea una rectificativa de una factura rectificativa existente.
    /// Permite corregir errores en rectificativas ya emitidas.
    /// </summary>
    public static RectificativeInvoice CreateFromRectificative(
        InvoiceNumber number,
        string billingSource,
        RectificativeInvoice originalRectificative,
        string rectificationReason,
        RectificationType rectificationType,
        List<InvoiceLine> lines,
        ExchangeRate appliedExchangeRate,
        IInvoiceHasher hasher,
        string paymentReference,
        string? previousHash = null,
        string? notes = null,
        string? transactionData = null,
        string? paymentMethod = null)
    {
        if (lines == null || lines.Count == 0)
            throw new DomainException("A corrective invoice must have at least one line.");

        if (originalRectificative.Status != InvoiceStatus.Issued)
            throw new DomainException(
                $"Solo se puede rectificar una factura rectificativa en estado Issued. " +
                $"Estado actual: {originalRectificative.Status}.");

        var issueDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var createdAt = DateTimeOffset.UtcNow;

        var temp = new RectificativeInvoice(
            Guid.NewGuid(),
            number,
            billingSource.Trim().ToLowerInvariant(),
            originalRectificative.Number,
            originalRectificative.IssueDate,
            rectificationReason.Trim(),
            rectificationType,
            originalRectificative.Issuer,
            originalRectificative.Recipient,
            issueDate,
            createdAt,
            lines,
            appliedExchangeRate,
            hash: string.Empty,
            previousHash,
            notes,
            paymentReference,
            transactionData,
            paymentMethod);

        var hashContent = temp.BuildHashContent();
        var computedHash = hasher.Compute(hashContent, previousHash);

        return new RectificativeInvoice(
            temp.Id,
            number,
            temp.BillingSource,
            originalRectificative.Number,
            originalRectificative.IssueDate,
            rectificationReason.Trim(),
            rectificationType,
            originalRectificative.Issuer,
            originalRectificative.Recipient,
            issueDate,
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
    /// Reconstitutes a RectificativeInvoice from persisted data.
    /// Bypasses creation validations and hash computation.
    /// Only called by the persistence mapper.
    /// </summary>
    public static RectificativeInvoice Reconstitute(
        Guid id,
        InvoiceNumber number,
        string billingSource,
        InvoiceNumber originalNumber,
        DateOnly originalIssueDate,
        string rectificationReason,
        RectificationType rectificationType,
        BillingParty issuer,
        BillingParty recipient,
        DateOnly issueDate,
        DateTimeOffset createdAt,
        List<InvoiceLine> lines,
        ExchangeRate appliedExchangeRate,
        string hash,
        string? previousHash,
        InvoiceStatus status,
        string paymentReference,
        string? notes,
        InvoiceNumber? rectifiedBy = null,
        string? transactionData = null,
        string? paymentMethod = null)
    {
        var invoice = new RectificativeInvoice(
            id, number, billingSource, originalNumber, originalIssueDate,
            rectificationReason, rectificationType, issuer, recipient,
            issueDate, createdAt, lines, appliedExchangeRate,
            hash, previousHash, notes,
            paymentReference, transactionData, paymentMethod);

        invoice.Status = status;
        invoice.RectifiedBy = rectifiedBy;
        return invoice;
    }

    // ── Transiciones de estado ─────────────────────────────────────────────

    /// <summary>
    /// Marca esta factura rectificativa como rectificada por otra.
    /// </summary>
    public void MarkAsRectifiedBy(InvoiceNumber rectificativeNumber)
    {
        if (Status != InvoiceStatus.Issued)
            throw new DomainException("Solo se puede rectificar una factura rectificativa en estado Issued.");

        RectifiedBy = rectificativeNumber;
        Status = InvoiceStatus.Rectified;
    }

    public void Issue()
    {
        if (Status != InvoiceStatus.Draft)
            throw new DomainException(
                $"Only one correction can be issued in Draft status. Current status: {Status}.");

        Status = InvoiceStatus.Issued;
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    public InvoiceHashContent BuildHashContent() => new()
    {
        IssuerTaxId = Issuer.TaxId.Value,
        InvoiceNumber = Number.Value,
        IssueDate = IssueDate.ToString("yyyy-MM-dd"),
        InvoiceType = "R",
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
        OriginalInvoiceNumber = OriginalInvoiceNumber.Value,
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
        $"{Number} [REC→{OriginalInvoiceNumber}] | {Recipient.DisplayName} | {TotalEur} | {Status}";
}