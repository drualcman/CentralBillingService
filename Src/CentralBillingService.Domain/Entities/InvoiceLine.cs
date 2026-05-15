namespace CentralBillingService.Domain.Entities;

/// <summary>
/// Una línea de la factura: qué se factura, a qué precio y con qué impuesto.
///
/// Todos los importes de cálculo son en EUR (la divisa funcional del sistema).
/// Si la factura viene en otra divisa, los importes en origen se guardan
/// también aquí para que la línea sea legible en ambos contextos.
/// </summary>
public sealed class InvoiceLine
{
    public int LineNumber { get; }
    public string Description { get; }
    public int Quantity { get; }

    // --- Importes en EUR (divisa funcional) ---

    /// <summary>Precio unitario en EUR</summary>
    public Money UnitPriceEur { get; }

    /// <summary>Base imponible en EUR: UnitPriceEur × Quantity</summary>
    public Money TaxableBaseEur { get; }

    /// <summary>Cuota de IVA en EUR</summary>
    public Money TaxAmountEur { get; }

    /// <summary>Total de línea en EUR: TaxableBaseEur + TaxAmountEur</summary>
    public Money TotalEur { get; }

    // --- Importes en divisa origen (para mostrar al cliente) ---

    /// <summary>Precio unitario en la divisa en que el cliente pagó/verá la factura</summary>
    public Money UnitPriceOrigin { get; }

    /// <summary>Total de línea en divisa origen</summary>
    public Money TotalOrigin { get; }

    public TaxRate TaxRate { get; }

    private InvoiceLine(
        int lineNumber,
        string description,
        int quantity,
        Money unitPriceEur,
        Money unitPriceOrigin,
        TaxRate taxRate)
    {
        LineNumber = lineNumber;
        Description = description;
        Quantity = quantity;
        UnitPriceEur = unitPriceEur;
        UnitPriceOrigin = unitPriceOrigin;
        TaxRate = taxRate;

        TaxableBaseEur = unitPriceEur.Multiply(quantity);
        TaxAmountEur = taxRate.CalculateTaxOn(TaxableBaseEur);
        TotalEur = TaxableBaseEur.Add(TaxAmountEur);
        TotalOrigin = unitPriceOrigin.Multiply(quantity);
    }

    /// <summary>
    /// Crea una línea cuando el precio ya viene en EUR.
    /// UnitPriceOrigin = UnitPriceEur (divisa EUR).
    /// </summary>
    public static InvoiceLine CreateInEur(
        int lineNumber,
        string description,
        int quantity,
        Money unitPriceEur,
        TaxRate taxRate)
    {
        ValidateCommon(lineNumber, description, quantity, unitPriceEur);

        return new InvoiceLine(
            lineNumber, description, quantity,
            unitPriceEur, unitPriceEur, taxRate);
    }

    /// <summary>
    /// Crea una línea cuando el precio viene en divisa extranjera.
    /// Se proporciona el unitPrice en origen y el unitPrice ya convertido a EUR
    /// (la conversión la realiza el servicio de dominio, no esta entidad).
    /// </summary>
    public static InvoiceLine CreateWithConversion(
        int lineNumber,
        string description,
        int quantity,
        Money unitPriceOrigin,
        Money unitPriceEur,
        TaxRate taxRate)
    {
        if (unitPriceOrigin.Currency == Currency.EUR)
            throw new DomainException(
                "Usa CreateInEur cuando el precio origen ya está en EUR.");

        ValidateCommon(lineNumber, description, quantity, unitPriceOrigin);

        return new InvoiceLine(
            lineNumber, description, quantity,
            unitPriceEur, unitPriceOrigin, taxRate);
    }

    public bool HasCurrencyConversion => UnitPriceOrigin.Currency != Currency.EUR;

    private static void ValidateCommon(int lineNumber, string description, int quantity, Money unitPrice)
    {
        if (lineNumber <= 0)
            throw new DomainException($"El número de línea debe ser positivo. Recibido: {lineNumber}.");

        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("La descripción de la línea es obligatoria.");
    }

    public override string ToString() =>
        $"[{LineNumber}] {Description} × {Quantity} = {TotalEur}";
}
