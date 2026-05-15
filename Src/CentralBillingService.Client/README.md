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
    BillingSource   = "my-saas",
    Secret          = "billing-secret",
    Serie           = "F",
    IssueDate       = DateOnly.FromDateTime(DateTime.Today),
    PaymentMethod   = "Transfer",
    PaymentReference = "PAY-2026-001",
    Recipient = new RecipientDto
    {
        LegalName        = "Acme Corp S.L.",
        TaxId            = "B12345678",
        TaxIdCountryCode = "ES",
        Email            = "billing@acme.com",
        Street           = "Calle Mayor 1",
        City             = "Madrid",
        Province         = "Madrid",
        PostalCode       = "28001",
        CountryCode      = "ES",
        ExternalId       = "{user_id}"
    },
    Lines = new List<InvoiceLineDto>
    {
        new()
        {
            Description       = "Software development — May 2026",
            Quantity          = 1,
            UnitPrice         = 3000.00m,
            TaxRatePercentage = 21m
        }
    }
});

Console.WriteLine(result.InvoiceNumber); // "F2026-0001"
Console.WriteLine(result.TotalEur);      // { Amount: 3630.00, CurrencyCode: "EUR" }
```

For invoices in a foreign currency, set `OriginCurrencyCode`:

```csharp
new CreateInvoiceCommand
{
    OriginCurrencyCode = "USD",
    Lines = new List<InvoiceLineDto>
    {
        new() { Description = "License fee", Quantity = 1, UnitPrice = 1000m, TaxRatePercentage = 0m }
    },
    // ...
}
```

CBS will fetch the current EUR exchange rate automatically and store both the origin and EUR amounts.

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
        PaymentMethod       = "Transfer",
        PaymentReference    = "REF-R-001",
        Lines = new List<InvoiceLineDto>
        {
            new() { Description = "Software development — May 2026", Quantity = 1, UnitPrice = 3000m, TaxRatePercentage = 21m }
        }
    });

Console.WriteLine(result.InvoiceNumber);         // "R2026-0001"
Console.WriteLine(result.OriginalInvoiceNumber); // "F2026-0001"
```

`RectificationType` values:
- `Substitution` — the rectificative fully replaces the original
- `Difference` — the rectificative records only the correction delta

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
