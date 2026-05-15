# CentralBillingService.Infrastructure

Concrete implementations of the interfaces defined in the Domain and Application layers. This project wires CBS to the outside world: hashing, verification, exchange rates, event dispatching, HTTP callbacks, and queue publishing.

**Depends on:** `CentralBillingService.Application`, `CentralBillingService.Domain`
**Depended on by:** `CentralBillingService.AzureFunction.API`, `CentralBillingService.WPF`

---

## Contents

- [Invoice Hashing](#invoice-hashing)
- [Verification URL Providers](#verification-url-providers)
- [Exchange Rates](#exchange-rates)
- [Invoice Number Providers](#invoice-number-providers)
- [Blob Storage](#blob-storage)
- [QR Code Job Queue](#qr-code-job-queue)
- [Event Dispatching](#event-dispatching)
- [Result Publishing and Callbacks](#result-publishing-and-callbacks)
- [ISO 9001 Audit Logging](#iso-9001-audit-logging)
- [Replacing Implementations](#replacing-implementations)
- [DI Registration](#di-registration)

---

## Invoice Hashing

### `Sha256InvoiceHasher` → `IInvoiceHasher`

Default implementation of invoice integrity hashing. Computes a SHA-256 hash over a canonical string built from the invoice's key fields, chained with the previous invoice's hash (VeriFactu-compatible chain).

The canonical field order used in hashing:
- Issuer tax ID
- Invoice number (serie + year + number)
- Issue date
- Taxable base (EUR)
- Total tax amount (EUR)
- Total (EUR)
- Previous hash (or empty string for first invoice in chain)

This implementation satisfies Spain's VeriFactu requirements. To use a different algorithm or field set, implement `IInvoiceHasher` from the Domain project and replace the registration.

---

## Verification URL Providers

Implementations of `IInvoiceVerificationUrlProvider` (defined in Domain). Two are provided:

### `SpanishAeatVerificationUrlProvider`
Builds a verification URL pointing to Spain's AEAT VeriFactu system. Used when the billing source is configured for the Spanish fiscal regime.

### `SystemInvoiceVerificationUrlProvider`
Builds a URL pointing to CBS's own `/api/invoices/{number}/verify` endpoint. Useful for testing, for non-Spanish deployments, or as the default before a country-specific provider is configured.

**To add a provider for a new country**, see the [Domain README](../CentralBillingService.Domain/README.md#iinvoiceverificationurlprovider).

---

## Exchange Rates

### `ExchangeRateProviderAdapter` → `IExchangeRateProvider`
Adapter that fetches live exchange rates from an external source. Implements:
- `GetRateAsync(from, to, ct)` — returns an `ExchangeRate` snapshot
- `Supports(from, to)` — returns true if the pair can be fetched

### `CurrencyConvertion` → `ICurrencyConvertion`
Higher-level service that uses `IExchangeRateProvider` to perform actual `Money` conversions.

```csharp
Money eur = await currencyConvertion.ConvertToCurrency(
    Money.Of(500m, Currency.USD), Currency.EUR);
```

Both services are registered and use `HttpClient` under the hood. The base URL and credentials are configured in `CbsOptions`.

---

## Invoice Number Providers

Number reservation uses the **Strategy pattern**: the factory selects the right strategy based on the billing source configuration.

### `IInvoiceNumberProviderFactory` → `InvoiceNumberProviderFactory`
Given a `BillingSourceConfig`, returns the appropriate `IInvoiceNumberProvider`.

### Strategies (implement `IInvoiceNumberProviderStrategy`)

| Class | When used | How it works |
|---|---|---|
| `DatabaseNumberProviderStrategy` | `NumberProviderType = "Database"` | Reserves numbers atomically in SQL Server. VeriFactu-compliant; guarantees sequential, gap-free numbering. |
| `ExternalApiNumberProviderStrategy` | `NumberProviderType = "ExternalApi"` | Calls a configured external HTTP endpoint to obtain the next number. Use when the authoritative number sequence lives in another system. |

Configuration (under each billing source in `appsettings.json`):
```json
{
  "NumberProviderType": "Database",
  "NumberProviderConfig": {
    // provider-specific settings
  }
}
```

---

## Blob Storage

### `AzureBlobStorageService` → `IBlobStorageService`

Manages QR code images in Azure Blob Storage. Two operations:

- `GetBlobUrl(blobName)` — **pure URI computation, no network call**. Instantiates a `BlobContainerClient` locally and returns `container.GetBlobClient(blobName).Uri.ToString()`. Called by `CreateInvoiceUseCase` in the hot path to pre-attach the public URL before persisting the invoice.
- `UploadAsync(blobName, bytes, contentType, ct)` — uploads content and returns the public URL.

Connection string and container name come from `CbsOptions.QrBlobConnectionString` and `CbsOptions.QrBlobContainerName`.

---

## QR Code Job Queue

### `AzureQueueQrCodeJobQueue` → `IQrCodeJobQueue`

Sends QR generation jobs to an Azure Storage Queue. Called by `CreateInvoiceUseCase` after the invoice is persisted. Failure is swallowed — the invoice already has the correct blob URL stored; the background job merely materialises the PNG.

Message format: `GenerateInvoiceQrCommand` serialized as JSON.

Connection string: `CbsOptions.QrBlobConnectionString` (same storage account as blob storage).
Queue name: `CbsOptions.QrCodeQueueName` (default: `"qr-code-jobs"`).

---

## Event Dispatching

### `InvoiceEventDispatcher` → `IInvoiceEventDispatcher`

Called after a successful invoice creation. Coordinates a chain of post-creation actions:

1. Publishes result to the configured Azure Storage Queue (`IInvoiceResultQueuePublisher`)
2. Sends an HTTP callback to the configured webhook (`IInvoiceResultCallbackNotifier`)

Both steps are optional and skipped if not configured for the billing source.

---

## Result Publishing and Callbacks

### `InvoiceResultQueuePublisher` → `IInvoiceResultQueuePublisher`
Serializes the `InvoiceResult` and sends it to an Azure Storage Queue. The consumer (the service that created the invoice) polls the queue to get the result asynchronously.

Uses `Azure.Storage.Queues`. Connection string and queue name come from `ResultQueueConfig` on the billing source.

### `InvoiceResultCallbackNotifier` → `IInvoiceResultCallbackNotifier`
Posts the `InvoiceResult` as JSON to the callback URL configured on the billing source. Includes auth headers as configured in `CallbackConfig`.

---

## ISO 9001 Audit Logging

### `ISO9001Service` → `IIso9001`
Records operations and errors to the ISO 9001 audit log tables. Uses `AuditLogCommandDataContext` for writes and `AuditLogQueryDataContext` for reads.

```csharp
await iso9001.Register<CreateInvoiceUseCase, InvoiceResult>(
    "invoice.created", result, ct);

await iso9001.Error<CreateInvoiceUseCase, CreateInvoiceCommand>(
    "invoice.create.failed", command, exception, ct);
```

---

## Replacing Implementations

Any of the implementations in this project can be swapped without touching the Domain or Application layers. The only requirement is implementing the relevant interface from Domain or Application and registering it in DI.

| Interface (Domain/Application) | Default implementation | To replace |
|---|---|---|
| `IInvoiceHasher` | `Sha256InvoiceHasher` | Implement interface, register your class |
| `IInvoiceVerificationUrlProvider` | `SpanishAeatVerificationUrlProvider` | Implement interface, register your class |
| `IExchangeRateProvider` | `ExchangeRateProviderAdapter` | Implement interface, register your class |
| `ICurrencyConvertion` | `CurrencyConvertion` | Implement interface, register your class |
| `IInvoiceNumberProviderStrategy` | `DatabaseNumberProviderStrategy` | Implement interface, register as named strategy |
| `IBlobStorageService` | `AzureBlobStorageService` | Implement interface, register your class |
| `IQrCodeJobQueue` | `AzureQueueQrCodeJobQueue` | Implement interface, register your class |
| `IInvoiceEventDispatcher` | `InvoiceEventDispatcher` | Implement interface, register your class |
| `IInvoiceResultQueuePublisher` | `InvoiceResultQueuePublisher` | Implement interface, register your class |
| `IInvoiceResultCallbackNotifier` | `InvoiceResultCallbackNotifier` | Implement interface, register your class |
| `IIso9001` | `ISO9001Service` | Implement interface, register your class |

---

## DI Registration

```csharp
services.AddBillingInfrastructure();
```

Registers all implementations listed above. Must be called after `AddBillingDomain()` and `AddBillingApplication()`. Persistence (`IInvoiceRepository`, `IInvoiceReadContext`, `IInvoiceWriteContext`) is registered separately by `AddSqlServerPersistence()`.
