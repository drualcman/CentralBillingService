# CentralBillingService.Domain

The heart of the system. Contains all business rules, entities, value objects, and the interfaces that define what CBS can do — without depending on any framework or infrastructure concern.

All other projects depend on this one. Nothing here depends on anything outside .NET itself.

---

## Contents

- [Value Objects](#value-objects)
- [Aggregates](#aggregates)
- [Domain Services](#domain-services)
- [Interfaces — Extension Points](#interfaces--extension-points)
- [Exceptions](#exceptions)
- [Configuration Models](#configuration-models)
- [DI Registration](#di-registration)

---

## Value Objects

Immutable types that carry meaning and enforce their own invariants. All comparisons are by value, not reference.

### `Money`
Represents an amount in a specific currency.

```csharp
var price = Money.Of(100.00m, Currency.EUR);
var tax   = TaxRate.General.CalculateTaxOn(price); // 21 EUR
var total = price.Add(tax);                        // 121 EUR
var half  = price.Multiply(0.5m);                  // 50 EUR
```

| Property / Method | Description |
|---|---|
| `Money.Of(amount, currency)` | Factory |
| `.Amount` | Decimal value |
| `.Currency` | Associated currency |
| `.Add(other)` | Returns new Money; currencies must match |
| `.Subtract(other)` | Returns new Money; currencies must match |
| `.Multiply(factor)` | Scales by decimal or int factor |

### `Currency`
154 ISO 4217 currencies as static fields.

```csharp
var eur = Currency.EUR;
var usd = Currency.USD;
var mxn = Currency.From("MXN"); // lookup by code string
bool ok = Currency.IsSupported("GBP");
```

| Property | Description |
|---|---|
| `.Code` | ISO 4217 code (e.g., `"EUR"`) |
| `.Name` | Full name (e.g., `"Euro"`) |
| `.Symbol` | Display symbol (e.g., `"€"`) |
| `.DecimalPlaces` | Standard decimal precision |

### `TaxRate`
Percentage-based tax with predefined values and calculation methods.

```csharp
var base_ = Money.Of(100m, Currency.EUR);

TaxRate.Zero.CalculateTaxOn(base_);      // 0 EUR
TaxRate.Reduced.CalculateTaxOn(base_);   // 10 EUR
TaxRate.General.CalculateTaxOn(base_);   // 21 EUR
TaxRate.Of(8m).ApplyTo(base_);           // 108 EUR (applies tax on top)
```

Predefined: `Zero` (0%), `SuperReduced` (4%), `Reduced` (10%), `General` (21%).

### `TaxId`
A tax identifier with country context and type classification.

```csharp
var nif     = TaxId.Create("12345678A", "ES");       // auto-detects type
var foreign = TaxId.Foreign("US-123456", "US");
var none    = TaxId.NotProvided("DE");
```

| Property | Description |
|---|---|
| `.Value` | Raw identifier string |
| `.CountryCode` | ISO country code |
| `.Type` | `TaxIdType` enum |
| `.IsSpanish` | True for ES country code |
| `.IsNotProvided` | True when no ID is available |

`TaxIdType` values: `NIF`, `NIE`, `CIF`, `EuVat`, `Foreign`, `NotProvided`, `Unknown`.

### `InvoiceNumber`
Structured invoice identifier with serie, year, and sequential number.

```csharp
var num = InvoiceNumber.Create("F", 2026, 42);  // F2026-0042
var parsed = InvoiceNumber.CreateFromFormatted("F2026-0042");

Console.WriteLine(num.Value); // "F2026-0042"
Console.WriteLine(num.Serie); // "F"
Console.WriteLine(num.Year);  // 2026
Console.WriteLine(num.Number);// 42
```

### `ExchangeRate`
A snapshot of a currency conversion rate at a point in time.

```csharp
var rate = ExchangeRate.Create(Currency.USD, Currency.EUR, 0.92m, DateTimeOffset.UtcNow);
Money eur = rate.Apply(Money.Of(100m, Currency.USD)); // 92 EUR

var identity = ExchangeRate.Identity(DateTimeOffset.UtcNow); // EUR→EUR at 1.0
```

### `PostalAddress`
Address value object with standard address fields.

### `BillingParty`
Snapshot of a party (issuer or recipient) at invoice creation time.

```csharp
var party = BillingParty.Create(
    legalName: "Acme S.L.",
    taxId: TaxId.Create("B12345678", "ES"),
    address: address,
    email: "billing@acme.com",
    tradeName: "Acme",    // optional
    phone: "+34600000000" // optional
);

Console.WriteLine(party.DisplayName); // "Acme" (trade name if set, else legal name)
```

---

## Aggregates

### `Invoice`
Root aggregate. Immutable once issued — lines and amounts cannot change after `Issue()` is called.

```csharp
// Creation is handled by CreateInvoiceService — use that, not this factory directly
var invoice = Invoice.Create(
    number, billingSource, issuer, recipient,
    lines, exchangeRate, hasher, previousHash,
    paymentMethod, paymentReference, ...
);

invoice.Issue(); // seals the invoice, sets status to Issued
bool ok = invoice.VerifyIntegrity(hasher); // SHA-256 chain check
```

Key properties:

| Property | Type | Description |
|---|---|---|
| `Id` | `Guid` | Unique identifier |
| `Number` | `InvoiceNumber` | Formatted invoice number |
| `BillingSource` | `string` | Logical billing tenant |
| `Issuer` | `BillingParty` | Issuer snapshot |
| `Recipient` | `BillingParty` | Recipient snapshot |
| `Lines` | `IReadOnlyList<InvoiceLine>` | Invoice lines |
| `TotalEur` | `Money` | Total in EUR |
| `TotalInOriginCurrency` | `Money` | Total in origin currency |
| `Hash` | `string` | SHA-256 chained hash |
| `PreviousHash` | `string?` | Previous invoice's hash |
| `Status` | `InvoiceStatus` | Draft / Issued / Rectified |
| `HasTamper` | `bool` | True if integrity check failed |

`InvoiceStatus` values: `Draft`, `Issued`, `Rectified`, `Cancelled`.

### `RectificativeInvoice`
A corrective document linked to an original invoice. Same structure as `Invoice` plus:

| Property | Type | Description |
|---|---|---|
| `OriginalInvoiceNumber` | `InvoiceNumber` | The invoice being corrected |
| `OriginalIssueDate` | `DateOnly` | Original issue date |
| `RectificationType` | `RectificationType` | Substitution or Difference |
| `RectificationReason` | `string` | Reason for correction |

`RectificationType` values: `Substitution` (replaces original), `Difference` (records only the delta).

### `InvoiceLine`
A single line item within an invoice.

```csharp
// Line in EUR only
var line = InvoiceLine.CreateInEur(1, "Consulting", 8, Money.Of(100m, Currency.EUR), TaxRate.General);

// Line with currency conversion
var line2 = InvoiceLine.CreateWithConversion(1, "Software", 1,
    unitPriceOrigin: Money.Of(500m, Currency.USD),
    unitPriceEur:    Money.Of(460m, Currency.EUR),
    TaxRate.Reduced);
```

| Property | Description |
|---|---|
| `LineNumber` | Position in invoice |
| `Description` | Service/product description |
| `Quantity` | Units |
| `UnitPriceEur` | Unit price in EUR |
| `TaxableBaseEur` | `Quantity × UnitPriceEur` |
| `TaxAmountEur` | Tax calculated amount |
| `TotalEur` | Taxable base + tax |
| `HasCurrencyConversion` | True if origin ≠ EUR |

---

## Domain Services

Services that coordinate domain logic requiring multiple aggregates or external queries.

### `CreateInvoiceService`
Orchestrates invoice creation: fetches exchange rate, computes hash chain, constructs the `Invoice` aggregate.

> Do not call this directly — use `ICreateInvoiceUseCase` from the Application layer.

### `RectifyInvoiceService`
Orchestrates rectificative invoice creation, linking it to its original.

---

## Interfaces — Extension Points

These are the contracts that external implementations can fulfill. **Implementing any of these is explicitly permitted by the license.**

---

### `IInvoiceVerificationUrlProvider`

> **This is the primary extension point for adding a new country/regulation.**

Returns a URL where the invoice can be verified against the tax authority's system.

```csharp
public interface IInvoiceVerificationUrlProvider
{
    string GetVerificationUrl(
        string invoiceNumber,
        DateOnly issueDate,
        decimal totalEurAmount,
        string issuerTaxId);
}
```

**Built-in implementations** (in the Infrastructure project):

| Class | Description |
|---|---|
| `SpanishAeatVerificationUrlProvider` | Points to Spain's AEAT VeriFactu system |
| `SystemInvoiceVerificationUrlProvider` | Points to CBS's own `/verify` endpoint |

**How to implement for a new country:**

```csharp
public class FrenchDgfipVerificationUrlProvider : IInvoiceVerificationUrlProvider
{
    public string GetVerificationUrl(
        string invoiceNumber,
        DateOnly issueDate,
        decimal totalEurAmount,
        string issuerTaxId)
    {
        return $"https://factur-x.dgfip.fr/verify?num={invoiceNumber}&nif={issuerTaxId}";
    }
}
```

Then register it in your DI setup, replacing the default:

```csharp
services.AddScoped<IInvoiceVerificationUrlProvider, FrenchDgfipVerificationUrlProvider>();
```

---

### `IInvoiceHasher`

Controls how invoice integrity is computed and verified. The default implementation uses SHA-256 with chained hashes (VeriFactu-compatible). You can replace this with your country's required algorithm.

```csharp
public interface IInvoiceHasher
{
    string Compute(InvoiceHashContent content, string? previousHash);
    bool Verify(InvoiceHashContent content, string? previousHash, string storedHash);
}
```

`InvoiceHashContent` carries the canonical fields that enter the hash calculation (invoice number, issuer tax ID, recipient tax ID, issue date, total amounts, etc.).

---

### `IExchangeRateProvider`

Supplies currency exchange rates for multi-currency invoices.

```csharp
public interface IExchangeRateProvider
{
    Task<ExchangeRate> GetRateAsync(Currency from, Currency to, CancellationToken cancellationToken);
    bool Supports(Currency from, Currency to);
}
```

Implement this to connect to your preferred exchange rate service (ECB, Fixer, OpenExchangeRates, etc.).

---

### `ICurrencyConvertion`

Higher-level currency conversion service, built on top of `IExchangeRateProvider`.

```csharp
public interface ICurrencyConvertion
{
    Task<decimal> GetRate(Currency origin, Currency destination);
    Task<Money> ConvertToCurrency(Money origin, Currency destination);
}
```

---

### `IInvoiceNumberGenerator`

Generates the raw invoice number string for a given billing source.

```csharp
public interface IInvoiceNumberGenerator
{
    Task<string> GenerateAsync(string billingSource, CancellationToken cancellationToken);
}
```

---

### `IIso9001`

Audit logging interface for ISO 9001 compliance tracking.

```csharp
public interface IIso9001
{
    Task Error<T, TData>(string operation, TData data, Exception ex, CancellationToken ct = default);
    Task Register<T, TData>(string operation, TData data, CancellationToken ct = default);
}
```

---

## Exceptions

| Exception | When thrown |
|---|---|
| `DomainException` | Base for all business rule violations |
| `ExchangeRateUnavailableException` | Rate cannot be obtained for the requested currency pair |
| `InvoiceTamperingDetectedException` | Hash verification fails — invoice data has been altered |

> `InvoiceTamperingDetectedException` is never thrown during normal read operations — instead, `Invoice.HasTamper` is set to `true` to allow the caller to decide how to handle it.

---

## Configuration Models

| Class | `SectionKey` | Key Properties |
|---|---|---|
| `CbsOptions` | `"Cbs"` | `BillingSources` collection |
| `BillingSourceConfig` | — | `Name`, `Secret`, `NumberProviderType`, `NumberProviderConfig`, `ResultQueueConfig`, `CallbackConfig` |
| `NumberProviderConfig` | — | Provider-specific settings |
| `ResultQueueConfig` | — | Queue connection and name |
| `CallbackConfig` | — | Callback URL and auth |

---

## DI Registration

```csharp
services.AddBillingDomain(options =>
{
    // bind from IConfiguration
    configuration.GetSection(CbsOptions.SectionKey).Bind(options);
});
```

This registers `BillingSourceRegistry`, `CreateInvoiceService`, `RectifyInvoiceService`, and the options model.
