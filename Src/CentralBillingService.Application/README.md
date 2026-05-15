# CentralBillingService.Application

The use-case layer. Orchestrates domain logic and defines the ports (interfaces) that infrastructure must implement. Contains no framework code, no SQL, no HTTP — only application flow.

**Depends on:** `CentralBillingService.Domain`
**Depended on by:** `CentralBillingService.Infrastructure`, `CentralBillingService.AzureFunction.API`, `CentralBillingService.WPF`

---

## Contents

- [Use Cases](#use-cases)
- [Application Ports (Interfaces)](#application-ports-interfaces)
- [Commands and Queries](#commands-and-queries)
- [Result DTOs](#result-dtos)
- [DI Registration](#di-registration)

---

## Use Cases

Each use case is a single operation with a single public method. They are the entry points from the API and the desktop app into the business logic.

### `ICreateInvoiceUseCase`

Creates a new invoice for a billing source.

```csharp
Task<InvoiceResult> ExecuteAsync(CreateInvoiceCommand command, CancellationToken ct);
```

Internal flow:
1. Validates the billing source and secret
2. Reserves the next invoice number (via `IInvoiceNumberProviderFactory`)
3. Reads the last hash for the serie (chain continuity)
4. Calls `CreateInvoiceService` in the Domain layer
5. Persists via `IInvoiceRepository`
6. Dispatches post-creation events (`IInvoiceEventDispatcher`)
7. Returns `InvoiceResult`

### `RectifyInvoiceUseCase`

Issues a rectificative invoice (credit note / correction) for an existing invoice.

```csharp
Task<RectificativeInvoiceResult> ExecuteAsync(RectifyInvoiceCommand command, CancellationToken ct);
```

### `GetInvoiceUseCase`

Retrieves a single invoice. Accepts either a `Guid` ID or a formatted invoice number string.

```csharp
Task<InvoiceResult?> ExecuteAsync(GetInvoiceQuery query, CancellationToken ct);
```

### `ListInvoicesUseCase`

Returns a paginated list of invoices with optional filters.

```csharp
Task<InvoiceListResult> ExecuteAsync(ListInvoicesQuery query, CancellationToken ct);
```

### `VerifyInvoiceIntegrityUseCase`

Verifies both the internal hash chain integrity and whether a provided document hash matches the stored one. Used for QR code / verification URL flows.

```csharp
Task<VerifyInvoiceResult> ExecuteAsync(VerifyInvoiceQuery query, CancellationToken ct);
```

Returns `DocumentHashMatches` (does the provided hash match?) and `IntegrityVerified` (is the internal chain intact?).

### `CheckInvoiceIntegrityUseCase`

Verifies only the internal hash chain without comparing to a provided hash. For administrative integrity audits.

```csharp
Task<CheckIntegrityResult> ExecuteAsync(CheckIntegrityQuery query, CancellationToken ct);
```

### `ProcessQueuedCreateInvoiceUseCase`

Handles an invoice creation request that arrived via a message queue (async flow).

```csharp
Task ExecuteAsync(CreateInvoiceCommand command, CancellationToken ct);
```

---

## Application Ports (Interfaces)

These are the contracts the Application layer requires from Infrastructure. They are defined here and implemented in `CentralBillingService.Infrastructure` and `CentralBillingService.Persistence.SqlServer`.

### `IInvoiceRepository`

The primary storage port for invoices.

```csharp
// Reads
Task<Invoice?> FindByIdAsync(string billingSource, Guid id, CancellationToken ct);
Task<Invoice?> FindByNumberAsync(string billingSource, InvoiceNumber number, CancellationToken ct);
Task<string?> GetLastHashAsync(string billingSource, string serie, int year, CancellationToken ct);
Task<InvoicePagedResult> ListAsync(InvoiceFilter filter, CancellationToken ct);
Task<RectificativeInvoice?> FindRectificativeByNumberAsync(string billingSource, InvoiceNumber number, CancellationToken ct);

// Writes
Task SaveAsync(Invoice invoice, CancellationToken ct);
Task SaveRectificativeAsync(RectificativeInvoice rectificative, Invoice updatedOriginal, CancellationToken ct);
Task SaveRectificativeFromRectificativeAsync(RectificativeInvoice rectificative, RectificativeInvoice updatedOriginal, CancellationToken ct);
```

### `IInvoiceNumberProvider`

Reserves the next sequential invoice number for a billing source and serie.

```csharp
Task<int> ReserveNextNumberAsync(string billingSource, string serie, int year, CancellationToken ct);
```

This operation must be atomic — no two concurrent calls should return the same number.

### `IInvoiceNumberProviderFactory`

Selects the correct `IInvoiceNumberProvider` implementation based on billing source configuration.

```csharp
IInvoiceNumberProvider GetFor(BillingSourceConfig config);
```

### `IInvoiceEventDispatcher`

Dispatches domain events after an invoice is created or updated.

```csharp
Task InvoiceCreatedAsync(Invoice invoice, CancellationToken ct);
```

Implementations may trigger PDF generation, email notifications, fiscal system notifications, queue publishing, etc.

### `IInvoiceResultQueuePublisher`

Publishes invoice operation results to a message queue (for async consumer notification).

```csharp
Task PublishAsync(InvoiceResult result, BillingSourceConfig config, CancellationToken ct);
```

### `IInvoiceResultCallbackNotifier`

Sends an HTTP callback with the invoice result to the configured webhook URL.

```csharp
Task NotifyAsync(InvoiceResult result, BillingSourceConfig config, CancellationToken ct);
```

---

## Commands and Queries

### `CreateInvoiceCommand`

```csharp
public class CreateInvoiceCommand
{
    public string BillingSource { get; set; }
    public string Secret { get; set; }
    public string Serie { get; set; }
    public RecipientDto Recipient { get; set; }
    public List<InvoiceLineDto> Lines { get; set; }
    public string? OriginCurrencyCode { get; set; }  // null = EUR
    public DateOnly? IssueDate { get; set; }          // null = today
    public DateOnly? ValueDate { get; set; }
    public string? Notes { get; set; }
    public string PaymentMethod { get; set; }
    public string PaymentReference { get; set; }
    public string? TransactionData { get; set; }
}
```

### `RecipientDto`

```csharp
public class RecipientDto
{
    public string LegalName { get; set; }
    public string? TradeName { get; set; }
    public string TaxId { get; set; }
    public string TaxIdCountryCode { get; set; }
    public string Email { get; set; }
    public string? Phone { get; set; }
    public string? Website { get; set; }
    public string? ExternalId { get; set; }
    // Address fields: Street, City, Province, PostalCode, CountryCode
}
```

### `InvoiceLineDto`

```csharp
public class InvoiceLineDto
{
    public string Description { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxRatePercentage { get; set; }
}
```

### `RectifyInvoiceCommand`

```csharp
public class RectifyInvoiceCommand
{
    public string BillingSource { get; set; }
    public string Secret { get; set; }
    public string OriginalInvoiceNumber { get; set; }  // formatted, e.g. "F2026-0042"
    public string RectificativeSerie { get; set; }
    public RectificationType RectificationType { get; set; } // Substitution | Difference
    public string Reason { get; set; }
    public List<InvoiceLineDto> Lines { get; set; }
    public string PaymentMethod { get; set; }
    public string PaymentReference { get; set; }
    public string? TransactionData { get; set; }
    public string? Notes { get; set; }
}
```

### `ListInvoicesQuery`

```csharp
public class ListInvoicesQuery
{
    public string BillingSource { get; set; }
    public string Secret { get; set; }
    public string? Serie { get; set; }
    public int? Year { get; set; }
    public DateOnly? IssuedFrom { get; set; }
    public DateOnly? IssuedTo { get; set; }
    public string? RecipientTaxId { get; set; }
    public string? RecipientExternalId { get; set; }
    public InvoiceStatus? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
```

### `GetInvoiceQuery`

```csharp
public class GetInvoiceQuery
{
    public string BillingSource { get; set; }
    public string Secret { get; set; }
    public Guid? Id { get; set; }
    public string? InvoiceNumber { get; set; }  // at least one must be provided
}
```

---

## Result DTOs

### `InvoiceResult`

```csharp
public class InvoiceResult
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; }      // "F2026-0042"
    public string BillingSource { get; set; }
    public string Status { get; set; }             // "Issued", "Rectified", ...
    public bool IsRectificative { get; set; }
    public bool HasTamper { get; set; }
    public PartyResult Issuer { get; set; }
    public PartyResult Recipient { get; set; }
    public DateOnly IssueDate { get; set; }
    public DateOnly? ValueDate { get; set; }
    public List<InvoiceLineResult> Lines { get; set; }
    public MoneyResult TaxableBaseEur { get; set; }
    public MoneyResult TotalTaxAmountEur { get; set; }
    public MoneyResult TotalEur { get; set; }
    public MoneyResult TotalInOriginCurrency { get; set; }
    public ExchangeRateResult AppliedExchangeRate { get; set; }
    public string Hash { get; set; }
    public string? PreviousHash { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    // Rectificative-only fields (when IsRectificative = true)
    public string? OriginalInvoiceNumber { get; set; }
    public string? RectificationReason { get; set; }
}
```

### `InvoiceListResult`

```csharp
public class InvoiceListResult
{
    public List<InvoiceSummaryResult> Items { get; set; }
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
```

### `VerifyInvoiceResult`

```csharp
public class VerifyInvoiceResult
{
    public string InvoiceNumber { get; set; }
    public string Hash { get; set; }
    public bool DocumentHashMatches { get; set; }  // provided hash matches stored hash
    public bool IntegrityVerified { get; set; }    // internal SHA-256 chain is intact
}
```

---

## DI Registration

```csharp
services.AddBillingApplication();
```

Registers all use case implementations. Must be called after `AddBillingDomain()`.
