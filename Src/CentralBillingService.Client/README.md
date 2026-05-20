# CentralBillingService.Client

A .NET SDK for consuming the CBS API. Add this package to any .NET 9+ or .NET 10 application and interact with CBS without writing HTTP client code yourself.

**No dependencies on other CBS projects** — this project is fully standalone and is the only CBS package consumer applications should reference.

---

## Contents

- [Installation](#installation)
- [Configuration](#configuration)
- [Usage](#usage)
  - [Create an Invoice](#create-an-invoice)
  - [Get an Invoice](#get-an-invoice)
  - [List Invoices](#list-invoices)
  - [Rectify an Invoice](#rectify-an-invoice)
  - [Verify an Invoice](#verify-an-invoice)
- [Models Reference](#models-reference)

---

## Installation

Reference the project (or NuGet package once published):

```xml
<PackageReference Include="CentralBillingService.Client" Version="*" />
```

Targets: `net9.0` and `net10.0`.

---

## Configuration

Register the client in your DI container:

```csharp
services.AddCbsServices(options =>
{
    options.Uri = "https://your-cbs-instance.azurewebsites.net";
    // Additional auth options if required
});
```

Or bind from `appsettings.json`:

```json
{
  "Cbs": {
    "Uri": "https://your-cbs-instance.azurewebsites.net"
  }
}
```

```csharp
services.AddCbsServices(options =>
    configuration.GetSection(CbsOptions.SectionKey).Bind(options));
```

The `SectionKey` constant is `"Cbs"`.

---

## Usage

Inject `ICbsService` wherever you need to interact with CBS:

```csharp
public class BillingService(ICbsService cbs)
{
    // ...
}
```

All methods are `async` and accept an implicit or explicit `CancellationToken`.

---

### Create an Invoice

```csharp
var result = await cbs.CreateInvoiceAsync(new CreateInvoiceCommand
{
    BillingSource    = "my-saas",
    Secret           = "billing-secret",
    Serie            = "F",
    IssueDate        = DateOnly.FromDateTime(DateTime.Today),
    PaymentMethod    = "Transfer",
    PaymentReference = "PAY-2026-001",
    Recipient = new RecipientDto
    {
        LegalName        = "Acme Corp S.L.",
        TaxIdValue       = "B12345678",
        TaxIdCountryCode = "ES",
        Email            = "billing@acme.com",
        AddressLine1     = "Calle Mayor 1",
        City             = "Madrid",
        Province         = "Madrid",
        PostalCode       = "28001",
        AddressCountryCode = "ES",
        ExternalId       = "{user_id}"
    },
    Lines = new List<InvoiceLineDto>
    {
        new()
        {
            Description       = "Software development — May 2026",
            Quantity          = 1,
            UnitPrice         = 3000.00m,
            TaxRatePercentage = 21
        }
    }
});

Console.WriteLine(result.InvoiceNumber); // "F2026-0001"
Console.WriteLine(result.TotalEur);      // { Amount: 3630.00, CurrencyCode: "EUR" }
```

#### Per-line currency (multi-currency invoices)

Each line can specify its own currency via `CurrencyCode`. CBS fetches the exchange rate automatically and stores both the origin and EUR amounts per line.

```csharp
Lines = new List<InvoiceLineDto>
{
    new()
    {
        Description       = "License fee (USD)",
        Quantity          = 1,
        UnitPrice         = 1000m,
        TaxRatePercentage = 0,
        CurrencyCode      = "USD"   // converted to EUR at current rate
    },
    new()
    {
        Description       = "Support (EUR)",
        Quantity          = 1,
        UnitPrice         = 200m,
        TaxRatePercentage = 21,
        CurrencyCode      = "EUR"
    }
}
```

`OriginCurrencyCode` on the command is the fallback for lines without an explicit `CurrencyCode`. Omit it (or leave null) when using per-line currencies.

#### Tax rules

Spanish VAT is applied automatically based on two criteria:

| Condition | Tax applied |
|---|---|
| Line currency is EUR **and** recipient country is `ES` | Yes — rate from `TaxRatePercentage` |
| Line currency is non-EUR (international) | No — rate forced to 0% |
| Recipient country is not `ES` | No — rate forced to 0% |

You can still pass any `TaxRatePercentage` in the command for domestic EUR invoices; it will be used as-is.

---

### Get an Invoice

```csharp
// By formatted invoice number
InvoiceResult invoice = await cbs.GetInvoiceAsync("F2026-0001");

Console.WriteLine(invoice.Status);      // "Issued"
Console.WriteLine(invoice.HasTamper);   // false — integrity intact
Console.WriteLine(invoice.Hash);        // SHA-256 hash
```

---

### List Invoices

```csharp
InvoiceListResult page = await cbs.GetInvoicesAsync(new GetInvoicesQuery
{
    BillingSource = "my-saas",
    Secret        = "billing-secret",
    Year          = 2026,
    Serie         = "F",
    Page          = 1,
    PageSize      = 25
});

foreach (var summary in page.Items)
    Console.WriteLine($"{summary.InvoiceNumber} — {summary.TotalEur}");
```

All filter fields are optional. Without a query, returns the most recent page.

---

### Rectify an Invoice

Issues a corrective document (credit note) for an existing invoice.

**Substitution** — the rectificative fully cancels the original; lines are derived automatically:

```csharp
RectifyInvoiceResult result = await cbs.RectifyInvoiceAsync(
    invoiceNumber: "F2026-0001",
    new RectifyInvoiceCommand
    {
        BillingSource       = "my-saas",
        Secret              = "billing-secret",
        RectificativeSerie  = "R",
        RectificationType   = RectificationType.Substitution,
        Reason              = "Incorrect recipient tax ID",
        PaymentReference    = "REF-R-001"
    });

Console.WriteLine(result.InvoiceNumber);         // "R2026-0001"
Console.WriteLine(result.OriginalInvoiceNumber); // "F2026-0001"
```

**Difference** — the rectificative records only the correction delta; supply the adjusted lines:

```csharp
RectifyInvoiceResult result = await cbs.RectifyInvoiceAsync(
    invoiceNumber: "F2026-0001",
    new RectifyInvoiceCommand
    {
        BillingSource      = "my-saas",
        Secret             = "billing-secret",
        RectificativeSerie = "R",
        RectificationType  = RectificationType.Difference,
        Reason             = "Discount not applied",
        PaymentReference   = "REF-R-002",
        Lines = new List<InvoiceLineDto>
        {
            new()
            {
                Description       = "Discount adjustment",
                Quantity          = -1,
                UnitPrice         = 300m,
                TaxRatePercentage = 21,
                CurrencyCode      = "EUR"
            }
        }
    });
```

`RectificationType` values:
- `Substitution` — fully cancels the original (lines auto-derived, negated)
- `Difference` — records only the delta (lines must be provided)

---

### Verify an Invoice

Checks that a provided hash matches the stored invoice hash and that the internal chain integrity is intact. Used for QR-code verification flows.

```csharp
VerifyInvoiceResult result = await cbs.VerifyInvoiceAsync(
    invoiceNumber: "F2026-0001",
    documentHash: "abc123...");

Console.WriteLine(result.DocumentHashMatches); // true — the hash provided matches
Console.WriteLine(result.IntegrityVerified);   // true — the chain is intact
```

---

## Models Reference

### Commands

| Class | Used by |
|---|---|
| `CreateInvoiceCommand` | `CreateInvoiceAsync` |
| `RectifyInvoiceCommand` | `RectifyInvoiceAsync` |
| `GetInvoicesQuery` | `GetInvoicesAsync` |
| `RecipientDto` | Nested in commands |
| `InvoiceLineDto` | Nested in commands |

### `RecipientDto` fields

| Property | Required | Description |
|---|---|---|
| `LegalName` | ✓ | Full legal/fiscal name |
| `TradeName` | — | Commercial brand name (optional) |
| `TaxIdValue` | ✓ | Tax ID / NIF / VAT number |
| `TaxIdCountryCode` | ✓ | ISO 3166-1 alpha-2 country of the tax ID (e.g. `"ES"`, `"DE"`) |
| `Email` | ✓ | Billing contact email |
| `Phone` | — | Contact phone (optional) |
| `AddressLine1` | ✓ | Street address |
| `AddressLine2` | — | Apartment, suite, etc. (optional) |
| `City` | ✓ | City |
| `Province` | — | Province / state (optional) |
| `PostalCode` | ✓ | Postal code |
| `AddressCountryCode` | ✓ | ISO 3166-1 alpha-2 country of the address (e.g. `"ES"`) |
| `ExternalId` | — | Your internal customer ID (optional) |

### `InvoiceLineDto` fields

| Property | Required | Description |
|---|---|---|
| `Description` | ✓ | Line item description |
| `Quantity` | ✓ | Quantity (can be negative for credit lines) |
| `UnitPrice` | ✓ | Unit price in the line's currency |
| `TaxRatePercentage` | ✓ | VAT percentage (`0`, `4`, `10`, `21`). Ignored for international lines. |
| `CurrencyCode` | — | ISO 4217 currency code; null = inherit `OriginCurrencyCode` or `"EUR"` |

### Results

| Class | Description |
|---|---|
| `InvoiceResult` | Full invoice detail |
| `InvoiceListResult` | Paged list (`Items`, `TotalCount`, `Page`, `PageSize`) |
| `InvoiceSummaryResult` | Lightweight summary for list views |
| `RectifyInvoiceResult` | Rectificative invoice detail |
| `VerifyInvoiceResult` | `DocumentHashMatches`, `IntegrityVerified`, `Hash` |
| `PartyResult` | Issuer or recipient data |
| `InvoiceLineResult` | Single line item |
| `MoneyResult` | `Amount` (decimal) + `CurrencyCode` (string) |
| `ExchangeRateResult` | `FromCurrencyCode`, `ToCurrencyCode`, `Rate`, `FetchedAt` |
