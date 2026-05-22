# CentralBillingService.AzureFunction.API

The main CBS backend, deployed as an Azure Functions v4 isolated-process app. Exposes a REST API for all billing operations and handles asynchronous invoice processing via Azure Storage Queues.

**Depends on:** All other CBS projects except `CentralBillingService.Client` and `CentralBillingService.WPF`

---

## Contents

- [Endpoints](#endpoints)
- [Authentication](#authentication)
- [Request / Response Format](#request--response-format)
- [Queue Processing](#queue-processing)
- [Local Development](#local-development)
- [Deployment](#deployment)
- [Configuration Reference](#configuration-reference)

---

## Endpoints

All routes are prefixed with `/api`.

### `POST /api/invoices`
Creates a new invoice.

**Headers:** `X-BillingSource`, `X-Secret`
**Body:** `CreateInvoiceCommand` (JSON)
**Response:** `201 Created` with `InvoiceResult`

```json
{
  "serie": "F",
  "recipient": {
    "legalName": "Acme Corp S.L.",
    "taxId": "B12345678",
    "taxIdCountryCode": "ES",
    "email": "billing@acme.com",
    "street": "Calle Mayor 1",
    "city": "Madrid",
    "postalCode": "28001",
    "countryCode": "ES"
  },
  "lines": [
    {
      "description": "Consulting — May 2026",
      "quantity": 1,
      "unitPrice": 3000.00,
      "taxRatePercentage": 21.0
    }
  ],
  "paymentMethod": "Transfer",
  "paymentReference": "PAY-2026-001"
}
```

---

### `GET /api/invoices/{invoiceNumber}`
Retrieves a single invoice. `invoiceNumber` can be a formatted number (`F2026-0001`) or a UUID.

**Headers:** `X-BillingSource`, `X-Secret`
**Response:** `200 OK` with `InvoiceResult` or `404 Not Found`

---

### `GET /api/invoices`
Returns a paginated list of invoices.

**Headers:** `X-BillingSource`, `X-Secret`

**Query parameters:**

| Parameter | Type | Default | Description |
|---|---|---|---|
| `serie` | string | — | Filter by invoice serie |
| `year` | int | — | Filter by year |
| `issuedFrom` | `yyyy-MM-dd` | — | Issue date lower bound |
| `issuedTo` | `yyyy-MM-dd` | — | Issue date upper bound |
| `recipientTaxId` | string | — | Filter by recipient tax ID |
| `recipientExternalId` | string | — | Filter by recipient's ID in your system |
| `status` | string | — | `Issued`, `Rectified`, `Cancelled` |
| `page` | int | `1` | Page number |
| `pageSize` | int | `25` | Results per page (max 100) |

**Response:** `200 OK` with `InvoiceListResult`

---

### `POST /api/invoices/{invoiceNumber}/rectify`
Issues a rectificative invoice (credit note / correction) for an existing invoice.

**Headers:** `X-BillingSource`, `X-Secret`
**Body:** `RectifyInvoiceCommand` (JSON)
**Response:** `201 Created` with `RectificativeInvoiceResult` or `404 Not Found`

```json
{
  "rectificativeSerie": "R",
  "rectificationType": "Substitution",
  "reason": "Incorrect recipient tax ID",
  "paymentMethod": "Transfer",
  "paymentReference": "REF-R-001",
  "lines": [...]
}
```

`rectificationType` values: `"Substitution"` | `"Difference"`

---

### `GET /api/invoices/{invoiceNumber}/verify?hash={documentHash}`
Verifies invoice integrity against a provided hash. This is the endpoint used by verification QR codes.

**Headers:** `X-BillingSource`, `X-Secret`
**Query:** `hash` — the SHA-256 hash to compare against
**Response:** `200 OK` with `VerifyInvoiceResult`

```json
{
  "invoiceNumber": "F2026-0001",
  "hash": "abc123...",
  "documentHashMatches": true,
  "integrityVerified": true
}
```

---

## Authentication

Each request must include two headers identifying the billing source and proving authorization:

| Header | Description |
|---|---|
| `X-BillingSource` | The logical billing tenant name (e.g., `"my-saas"`) |
| `X-Secret` | The shared secret configured for that billing source |

Billing sources and secrets are defined in the application configuration. Requests with an unknown source or incorrect secret are rejected with `401 Unauthorized`.

---

## Request / Response Format

- All request and response bodies are **JSON**
- Dates use **ISO 8601** format: `"2026-05-15"` for `DateOnly`, `"2026-05-15T10:00:00Z"` for timestamps
- `issueDate` is optional in `POST /api/invoices` and `POST /api/invoices/{id}/rectify`. When omitted, the server defaults to **today in UTC** (`DateTime.UtcNow`). Always provide the field explicitly if you need the invoice date to match the caller's local date.
- Money amounts are plain `decimal` values; currency is always specified in a sibling `currencyCode` field
- Error responses follow the **RFC 7807 Problem Details** format:

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Domain rule violation",
  "status": 422,
  "detail": "Invoice F2026-0001 is already rectified"
}
```

HTTP status codes used:

| Code | Meaning |
|---|---|
| `201 Created` | Invoice or rectificative created |
| `200 OK` | Successful read |
| `400 Bad Request` | Malformed request body or missing required fields |
| `401 Unauthorized` | Unknown billing source or wrong secret |
| `404 Not Found` | Invoice does not exist |
| `422 Unprocessable Entity` | Request is well-formed but violates a business rule |
| `500 Internal Server Error` | Unexpected error (including tamper detection) |

---

## Queue Processing

### `ProcessQueuedCreateInvoiceFunction`
A queue-triggered function that processes invoice creation requests published to an Azure Storage Queue. Used for fire-and-forget invoice creation where the caller does not need to wait for the synchronous response.

The queue name and connection string are configured per billing source in `ResultQueueConfig`.

Flow:
1. Consumer posts `CreateInvoiceCommand` to the queue
2. This function picks it up and calls `ProcessQueuedCreateInvoiceUseCase`
3. On success, the result is published to the result queue and/or sent to the callback URL

### `GenerateInvoiceQrFunction`
A queue-triggered function that materialises the QR code PNG for an invoice after it has been persisted. Decoupled from the HTTP creation path so that `POST /api/invoices` always responds as fast as possible.

Queue: configured via the `%QrCodeQueueName%` app setting (connection: `InvoiceCreateQueueStorage`).

Flow:
1. `CreateInvoiceUseCase` enqueues a `GenerateInvoiceQrCommand` message after persisting the invoice (the blob URL is already stored in the DB at this point)
2. This function deserialises the message and calls `GenerateInvoiceQrUseCase`
3. The use case builds the verification URL, generates the PNG, and uploads it to blob storage under `qr/{billingSource}/{invoiceNumber}.png`

**Error handling:**
- Deserialisation failure → message is discarded (poison message; retrying will never succeed)
- Generation/upload failure → exception is re-thrown so the Functions runtime retries with exponential backoff

---

## Local Development

### Prerequisites
- [Azure Functions Core Tools v4](https://learn.microsoft.com/en-us/azure/azure-functions/functions-run-local)
- [Azurite](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite) (local storage emulator) or a real Azure Storage account
- SQL Server (local instance or Docker)
- .NET 10 SDK

### Setup

1. Set user secrets (avoids committing connection strings):

```bash
cd Src/CentralBillingService.AzureFunction.API
dotnet user-secrets set "Database:ConnectionString" "Server=.;Database=CBS;Trusted_Connection=True;Encrypt=False;"
```

2. Apply database migrations:

```bash
cd Src/CentralBillingService.Persistence.SqlServer
dotnet ef database update --startup-project ../CentralBillingService.AzureFunction.API
```

3. Run the function app:

```bash
cd Src/CentralBillingService.AzureFunction.API
func start
```

The API will be available at `http://localhost:7071/api/`.

---

## Deployment

Deploy to Azure Functions (Consumption or Premium plan, Windows or Linux, .NET 10 isolated):

```bash
dotnet publish -c Release -o ./publish
func azure functionapp publish <your-function-app-name> --dotnet-isolated
```

Application Insights telemetry is pre-configured — set the `APPLICATIONINSIGHTS_CONNECTION_STRING` app setting in Azure.

---

## Configuration Reference

Full `local.settings.json` structure for local development:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
  },
  "ConnectionStrings": {
    "BillingDb": "Server=.;Database=CBS;Trusted_Connection=True;Encrypt=False;"
  },
  "Database": {
    "ConnectionString": "Server=.;Database=CBS;Trusted_Connection=True;Encrypt=False;"
  },
  "Cbs": {
    "QrBlobConnectionString": "UseDevelopmentStorage=true",
    "QrBlobContainerName": "qr-codes",
    "QrCodeQueueName": "qr-code-jobs",
    "VerifyUiBaseUrl": "https://verify.example.com/",
    "BillingSources": [
      {
        "Name": "my-saas",
        "Secret": "a-strong-secret",
        "NumberProviderType": "Database",
        "ResultQueueConfig": {
          "ConnectionString": "UseDevelopmentStorage=true",
          "QueueName": "invoice-results"
        },
        "CallbackConfig": {
          "Url": "https://my-saas.example.com/webhooks/invoice",
          "AuthHeader": "X-Api-Key",
          "AuthValue": "my-saas-api-key"
        }
      }
    ]
  }
}
```
